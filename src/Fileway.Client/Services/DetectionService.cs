using System.Text.Json;
using Fileway.Shared.Detection;
using Fileway.Shared.Formats;

namespace Fileway.Client.Services;

public sealed class DetectionService
{
    private readonly IFormatDetector _detector;
    private readonly HttpClient _http;
    private readonly SessionTokenProvider _sessionToken;

    public DetectionService(IFormatDetector detector, HttpClient http, SessionTokenProvider sessionToken)
    {
        _detector = detector;
        _http = http;
        _sessionToken = sessionToken;
    }

    public async Task<(FileFormat? Format, DetectionConfidence Confidence)> DetectAsync(
        ReadOnlyMemory<byte> fileContent,
        string? filename,
        CancellationToken ct = default)
    {
        var header = fileContent.Length > 512
            ? fileContent[..512].Span
            : fileContent.Span;

        var (format, confidence) = _detector.Detect(header, filename);

        if (confidence != DetectionConfidence.Low && format is not null)
            return (format, confidence);

        return await DetectServerAsync(fileContent, filename, ct);
    }

    private async Task<(FileFormat? Format, DetectionConfidence Confidence)> DetectServerAsync(
        ReadOnlyMemory<byte> fileContent,
        string? filename,
        CancellationToken ct)
    {
        try
        {
            var headerBytes = Convert.ToBase64String(
                fileContent.Length > 512 ? fileContent[..512].Span : fileContent.Span);

            var body = JsonSerializer.Serialize(new
            {
                headerBytes,
                filename,
                declaredMimeType = (string?)null
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/detect");
            request.Headers.Add("X-Session-Token", _sessionToken.Token);
            request.Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return (null, DetectionConfidence.Low);

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            var formatId = doc.RootElement.TryGetProperty("detectedFormat", out var f) ? f.GetString() : null;
            var confidenceStr = doc.RootElement.TryGetProperty("confidence", out var c) ? c.GetString() : null;

            if (string.IsNullOrEmpty(formatId))
                return (null, DetectionConfidence.Low);

            var detected = FileFormats.All.FirstOrDefault(ff => ff.Id == formatId);
            var conf = Enum.TryParse<DetectionConfidence>(confidenceStr, ignoreCase: true, out var parsedConf)
                ? parsedConf
                : DetectionConfidence.Low;

            return (detected, conf);
        }
        catch
        {
            return (null, DetectionConfidence.Low);
        }
    }
}
