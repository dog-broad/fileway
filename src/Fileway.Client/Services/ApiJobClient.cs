using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Fileway.Shared.Api;
using Fileway.Shared.Formats;

namespace Fileway.Client.Services;

public sealed class ApiJobClient
{
    private readonly HttpClient _http;
    private readonly SessionTokenProvider _sessionToken;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public int? RetryAfterSeconds { get; private set; }

    public ApiJobClient(HttpClient http, SessionTokenProvider sessionToken)
    {
        _http = http;
        _sessionToken = sessionToken;
    }

    public async Task<ApiJobResult> SubmitAsync(
        string toolSlug,
        ReadOnlyMemory<byte> fileContent,
        string filename,
        FileFormat detectedFormat,
        string? outputFormat,
        JsonElement toolOptions,
        CancellationToken ct)
    {
        RetryAfterSeconds = null;

        var options = new JobOptions
        {
            ToolSlug = toolSlug,
            OutputFormat = outputFormat,
            ToolOptions = toolOptions
        };
        var optionsJson = JsonSerializer.Serialize(options, _jsonOptions);

        using var form = new MultipartFormDataContent();
        var optionsPart = new StringContent(optionsJson, Encoding.UTF8, "application/json");
        form.Add(optionsPart, "options");

        var filePart = new ByteArrayContent(fileContent.ToArray());
        filePart.Headers.ContentType = new MediaTypeHeaderValue(
            detectedFormat.MimeTypes.Length > 0 ? detectedFormat.MimeTypes[0] : "application/octet-stream");
        form.Add(filePart, "file", filename);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/jobs");
        request.Headers.Add("X-Session-Token", _sessionToken.Token);
        request.Content = form;

        using var response = await _http.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            RetryAfterSeconds = (int?)response.Headers.RetryAfter?.Delta?.TotalSeconds;
            var pd = await ReadProblemDetailsAsync(response, ct);
            return ApiJobResult.Failure(pd);
        }

        if (!response.IsSuccessStatusCode)
        {
            var pd = await ReadProblemDetailsAsync(response, ct);
            return ApiJobResult.Failure(pd);
        }

        var body = await response.Content.ReadAsStringAsync(ct);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = JsonSerializer.Deserialize<SyncJobResult>(body, _jsonOptions)!;
            return ApiJobResult.Sync(result);
        }

        // 202 Accepted
        var accepted = JsonSerializer.Deserialize<AsyncJobAccepted>(body, _jsonOptions)!;
        return ApiJobResult.Async(accepted);
    }

    private static async Task<ProblemDetailsResponse> ReadProblemDetailsAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            return new ProblemDetailsResponse(
                (int)response.StatusCode,
                root.TryGetProperty("errorCode", out var ec) ? ec.GetString() : null,
                root.TryGetProperty("userMessage", out var um) ? um.GetString() : null,
                root.TryGetProperty("retryable", out var r) && r.GetBoolean());
        }
        catch
        {
            return new ProblemDetailsResponse((int)response.StatusCode, null, null, false);
        }
    }
}

public sealed record ProblemDetailsResponse(int StatusCode, string? ErrorCode, string? UserMessage, bool Retryable);

public abstract record ApiJobResult
{
    public static ApiJobResult Sync(SyncJobResult result) => new SyncResult(result);
    public static ApiJobResult Async(AsyncJobAccepted accepted) => new AsyncResult(accepted);
    public static ApiJobResult Failure(ProblemDetailsResponse problem) => new FailureResult(problem);
}

public sealed record SyncResult(SyncJobResult Result) : ApiJobResult;
public sealed record AsyncResult(AsyncJobAccepted Accepted) : ApiJobResult;
public sealed record FailureResult(ProblemDetailsResponse Problem) : ApiJobResult;
