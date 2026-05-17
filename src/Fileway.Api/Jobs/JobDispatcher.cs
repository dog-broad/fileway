using System.Diagnostics;
using Fileway.Api.Configuration;
using Fileway.Api.Infrastructure;
using Fileway.Shared.Api;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Shared.Processors;
using Fileway.Shared.Registry;
using Fileway.Shared.Tools;
using Microsoft.Extensions.Options;

namespace Fileway.Api.Jobs;

public sealed class JobDispatcher(
    IToolRegistry toolRegistry,
    IJobStore jobStore,
    JobQueueManager queueManager,
    IStorageService storageService,
    IServiceProvider serviceProvider,
    IOptions<ApiOptions> apiOptions,
    ILogger<JobDispatcher> logger)
{
    private const long InlineThresholdBytes = 5 * 1024 * 1024;

    public async Task<DispatchResult> DispatchAsync(
        JobOptions options,
        IReadOnlyList<InputFile> inputFiles,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        var tool = toolRegistry.GetBySlug(options.ToolSlug)
            ?? throw new JobDispatchException(400, ErrorCodes.UnknownToolSlug, $"Unknown tool: {options.ToolSlug}");

        if (tool.ProcessorType is null)
            throw new JobDispatchException(400, ErrorCodes.UnknownToolSlug,
                $"Tool '{options.ToolSlug}' has no server-side processor.");

        var processor = (IApiProcessor)serviceProvider.GetRequiredService(tool.ProcessorType);

        processor.ValidateOptions(options.ToolOptions);

        var acquire = queueManager.TryAcquire(sessionToken);
        if (acquire == AcquireSlotResult.ConcurrentLimitExceeded)
            throw new JobDispatchException(429, ErrorCodes.ConcurrentJobLimit,
                "Too many concurrent jobs for this session.");
        if (acquire == AcquireSlotResult.QueueFull)
            throw new JobDispatchException(503, ErrorCodes.QueueFull, "Server is at capacity.");

        var outputFormat = ResolveOutputFormat(tool, options.OutputFormat);

        if (tool.JobTier == JobTier.Synchronous)
            return await RunSyncAsync(tool, processor, inputFiles, outputFormat, options, sessionToken, cancellationToken)
                .ConfigureAwait(false);

        return await RunAsyncJobAsync(tool, processor, inputFiles, outputFormat, options, sessionToken, cancellationToken)
            .ConfigureAwait(false);
    }

    private static FileFormat ResolveOutputFormat(ToolDefinition tool, string? requestedFormat)
    {
        if (!string.IsNullOrEmpty(requestedFormat))
        {
            var match = tool.OutputFormats.FirstOrDefault(f => f.Id == requestedFormat);
            if (match is not null) return match;
        }

        return tool.DefaultOutputFormat ?? tool.OutputFormats[0];
    }

    private async Task<DispatchResult> RunSyncAsync(
        ToolDefinition tool,
        IApiProcessor processor,
        IReadOnlyList<InputFile> inputFiles,
        FileFormat outputFormat,
        JobOptions options,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        logger.LogDebug("Dispatching sync job {ToolSlug}", tool.Slug);
        try
        {
            var timeoutSeconds = tool.TimeoutSeconds > 0
                ? tool.TimeoutSeconds
                : apiOptions.Value.DefaultTimeoutSeconds;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var context = new ProcessorContext
            {
                ToolSlug = tool.Slug,
                InputFiles = inputFiles,
                OutputFormat = outputFormat,
                ToolOptions = options.ToolOptions,
                CancellationToken = cts.Token,
                Progress = new Progress<ProcessorProgressEvent>()
            };

            var result = await processor.ExecuteAsync(context, cts.Token).ConfigureAwait(false);
            sw.Stop();

            if (result.OutputContent.Length < InlineThresholdBytes)
            {
                return new SyncDispatchResult(new SyncJobResult
                {
                    ToolSlug = tool.Slug,
                    OutputFormat = result.OutputFormat.Id,
                    OutputMimeType = result.OutputFormat.MimeTypes[0],
                    OutputSizeBytes = result.OutputContent.Length,
                    DurationMs = sw.ElapsedMilliseconds,
                    DeliveryKind = DeliveryKind.Inline,
                    InlineContent = Convert.ToBase64String(result.OutputContent.Span)
                });
            }

            var storageKey = await storageService.SaveAsync(
                result.OutputContent, result.OutputFilename, result.OutputFormat.MimeTypes[0], cancellationToken)
                .ConfigureAwait(false);
            var (signedUrl, expiresAt) = await storageService
                .GetSignedUrlAsync(storageKey, cancellationToken).ConfigureAwait(false);

            return new SyncDispatchResult(new SyncJobResult
            {
                ToolSlug = tool.Slug,
                OutputFormat = result.OutputFormat.Id,
                OutputMimeType = result.OutputFormat.MimeTypes[0],
                OutputSizeBytes = result.OutputContent.Length,
                DurationMs = sw.ElapsedMilliseconds,
                DeliveryKind = DeliveryKind.SignedUrl,
                SignedUrl = signedUrl,
                ExpiresAt = expiresAt
            });
        }
        finally
        {
            queueManager.Release(sessionToken);
        }
    }

    private Task<DispatchResult> RunAsyncJobAsync(
        ToolDefinition tool,
        IApiProcessor processor,
        IReadOnlyList<InputFile> inputFiles,
        FileFormat outputFormat,
        JobOptions options,
        string sessionToken,
        CancellationToken cancellationToken)
    {
        // Async job infrastructure (Channel, JobRecord, background Task) is wired in M3+
        // when the first Async-tier tool is added. The job store is ready to use.
        _ = jobStore;
        queueManager.Release(sessionToken);
        throw new NotImplementedException("Async job dispatch is implemented in M3+.");
    }
}
