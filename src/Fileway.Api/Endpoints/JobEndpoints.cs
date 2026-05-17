using System.Text;
using System.Text.Json;
using Fileway.Api.Infrastructure;
using Fileway.Api.Jobs;
using Fileway.Api.Logging;
using Fileway.Shared.Api;
using Fileway.Shared.Detection;
using Fileway.Shared.Errors;
using Fileway.Shared.Jobs;
using Fileway.Shared.Processors;
using Fileway.Shared.Registry;
using Microsoft.AspNetCore.Mvc;

namespace Fileway.Api.Endpoints;

public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/jobs", SubmitJob);
        app.MapGet("/api/v1/jobs/{jobId}/progress", StreamProgress);
    }

    private static async Task<IResult> SubmitJob(
        HttpContext context,
        JobDispatcher dispatcher,
        IFormatDetector formatDetector,
        IToolRegistry toolRegistry,
        AuditLogService auditLog,
        CancellationToken ct)
    {
        // Step 2: Content-Type must be multipart/form-data
        if (!context.Request.HasFormContentType)
            return Problem(415, ErrorCodes.UnsupportedMediaType,
                "Request must be multipart/form-data.", retryable: false);

        // Step 3: Read options part (must be first per spec)
        IFormCollection form;
        try
        {
            form = await context.Request.ReadFormAsync(ct);
        }
        catch (Exception)
        {
            return Problem(400, ErrorCodes.MalformedOptions, "Could not read multipart form.");
        }

        var optionsJson = form["options"].FirstOrDefault();
        if (string.IsNullOrEmpty(optionsJson))
            return Problem(400, ErrorCodes.MalformedOptions,
                "Missing 'options' part. It must be the first multipart field with Content-Type: application/json.");

        // Step 3 cont: parse options JSON
        JobOptions jobOptions;
        try
        {
            jobOptions = JsonSerializer.Deserialize<JobOptions>(optionsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new JsonException("Null deserialization result.");
        }
        catch (JsonException)
        {
            return Problem(400, ErrorCodes.MalformedOptions, "Invalid JSON in 'options' part.");
        }

        // Step 4: Validate tool slug
        var tool = toolRegistry.GetBySlug(jobOptions.ToolSlug ?? "");
        if (tool is null)
            return Problem(400, ErrorCodes.UnknownToolSlug,
                $"Unknown tool '{jobOptions.ToolSlug}'.",
                "Check GET /api/v1/tools for valid tool slugs.");

        // Step 5: Validate output format
        if (!string.IsNullOrEmpty(jobOptions.OutputFormat) &&
            !tool.OutputFormats.Any(f => f.Id == jobOptions.OutputFormat))
            return Problem(400, ErrorCodes.InvalidOutputFormat,
                $"Output format '{jobOptions.OutputFormat}' is not supported by this tool.");

        // Step 8-11: Build input files from multipart parts or inline content
        var inputFiles = new List<InputFile>();

        var filePartNames = new[] { "file" }
            .Concat(Enumerable.Range(1, tool.MaxInputFileCount - 1).Select(i => $"file_{i}"));

        int index = 0;
        foreach (var partName in filePartNames)
        {
            var filePart = form.Files[partName];
            if (filePart is null) continue;

            // Step 10: Size check before reading
            if (filePart.Length > tool.MaxInputSizeBytes)
                return Problem(413, ErrorCodes.FileTooLarge,
                    $"File exceeds the {tool.MaxInputSizeBytes / 1024 / 1024}MB limit for this tool.");

            using var ms = new MemoryStream((int)filePart.Length);
            await filePart.CopyToAsync(ms, ct);
            var bytes = ms.ToArray();

            // Step 9: Format detection
            var (detected, confidence) = formatDetector.Detect(bytes.AsSpan()[..Math.Min(512, bytes.Length)],
                filePart.FileName);

            if (detected is null || !tool.AcceptedFormats.Any(f => f.Id == detected.Id))
                return Problem(422, ErrorCodes.FormatMismatch,
                    "File format does not match the accepted formats for this tool.",
                    "Check that you are uploading the correct file type.");

            inputFiles.Add(new InputFile
            {
                Content = bytes,
                DetectedFormat = detected,
                SizeBytes = bytes.LongLength,
                OriginalFilename = null, // never trust — not stored or logged
                Index = index++
            });
        }

        // Inline content for tools where RequiresFileInput = false (e.g. paste-in-browser)
        if (inputFiles.Count == 0 && !string.IsNullOrEmpty(jobOptions.InlineContent))
        {
            var contentBytes = Encoding.UTF8.GetBytes(jobOptions.InlineContent);

            if (contentBytes.Length > tool.MaxInputSizeBytes)
                return Problem(413, ErrorCodes.FileTooLarge,
                    $"Content exceeds the {tool.MaxInputSizeBytes / 1024 / 1024}MB limit for this tool.");

            var (detected, _) = formatDetector.Detect(
                contentBytes.AsSpan()[..Math.Min(512, contentBytes.Length)], filename: null);

            var fmt = (detected is not null && tool.AcceptedFormats.Any(f => f.Id == detected.Id))
                ? detected
                : tool.AcceptedFormats[0];

            inputFiles.Add(new InputFile
            {
                Content = contentBytes,
                DetectedFormat = fmt,
                SizeBytes = contentBytes.LongLength,
                OriginalFilename = null,
                Index = 0
            });
        }

        // Require at least one input
        if (inputFiles.Count == 0)
            return Problem(400, ErrorCodes.MissingFilePart,
                "No file or inline content was provided.",
                "Include a 'file' form part or set 'inlineContent' in the options.");

        // Step 12: Dispatch (may throw JobDispatchException or ProcessorValidationException — bubbles to exception handler)
        var sessionToken = context.Items[SessionTokenMiddleware.SessionTokenKey] as string ?? "";
        var sessionPrefix = context.Items[SessionTokenMiddleware.SessionPrefixKey] as string;

        var result = await dispatcher.DispatchAsync(jobOptions, inputFiles, sessionToken, ct);

        auditLog.LogJobDispatched(tool.Slug, sessionPrefix);

        return result switch
        {
            SyncDispatchResult sync => Results.Ok(sync.Result),
            AsyncDispatchResult async => Results.Accepted(value: async.Accepted),
            _ => Results.Problem()
        };
    }

    private static async Task StreamProgress(
        HttpContext context,
        string jobId,
        IJobStore jobStore,
        CancellationToken ct)
    {
        if (!Guid.TryParse(jobId, out var id))
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(
                SimpleProblem(404, ErrorCodes.JobNotFound, "Job not found."), ct);
            return;
        }

        var job = jobStore.Get(id);
        if (job is null)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(
                SimpleProblem(404, ErrorCodes.JobNotFound, "Job not found or expired."), ct);
            return;
        }

        var sessionToken = context.Items[SessionTokenMiddleware.SessionTokenKey] as string ?? "";
        if (!string.Equals(job.SessionToken, sessionToken, StringComparison.Ordinal))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(
                SimpleProblem(403, ErrorCodes.JobNotOwned, "You do not own this job."), ct);
            return;
        }

        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        var writer = context.Response.BodyWriter;
        var reader = job.EventChannel.Reader;

        var lastEventId = context.Request.Headers["Last-Event-ID"].FirstOrDefault();
        long eventCounter = 0;

        using var keepAliveTimer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, job.CancellationTokenSource.Token);

        var linkedCt = linked.Token;

        while (!linkedCt.IsCancellationRequested)
        {
            if (reader.TryRead(out var jobEvent))
            {
                var json = JsonSerializer.Serialize(jobEvent);
                var line = $"id: {++eventCounter}\ndata: {json}\n\n";
                await writer.WriteAsync(Encoding.UTF8.GetBytes(line), linkedCt);
                await writer.FlushAsync(linkedCt);

                if (jobEvent.EventType is JobEventType.Completed or JobEventType.Failed)
                    break;
            }
            else
            {
                // Wait for either a new event or keepalive timer
                var readTask = reader.WaitToReadAsync(linkedCt).AsTask();
                var timerTask = keepAliveTimer.WaitForNextTickAsync(linkedCt).AsTask();

                var completed = await Task.WhenAny(readTask, timerTask);
                if (completed == timerTask)
                {
                    await writer.WriteAsync(Encoding.UTF8.GetBytes(": ping\n\n"), linkedCt);
                    await writer.FlushAsync(linkedCt);
                }
            }
        }
    }

    private static IResult Problem(
        int status, string errorCode, string userMessage,
        string? suggestedAction = null, bool retryable = false)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = userMessage,
            Type = $"https://fileway.io/errors/{errorCode.ToLowerInvariant()}"
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["userMessage"] = userMessage;
        if (suggestedAction is not null)
            problem.Extensions["suggestedAction"] = suggestedAction;
        problem.Extensions["retryable"] = (object)retryable;
        return Results.Json(problem, statusCode: status);
    }

    private static ProblemDetails SimpleProblem(int status, string errorCode, string userMessage)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = userMessage,
            Type = $"https://fileway.io/errors/{errorCode.ToLowerInvariant()}"
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["userMessage"] = userMessage;
        return problem;
    }
}
