using Fileway.Shared.Detection;
using Fileway.Shared.Registry;
using Microsoft.AspNetCore.Mvc;

namespace Fileway.Api.Endpoints;

public static class DetectEndpoints
{
    public static void MapDetectEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/detect", Detect).DisableRateLimiting();
    }

    private static IResult Detect(
        DetectRequest request,
        IFormatDetector formatDetector,
        IToolRegistry toolRegistry)
    {
        byte[] headerBytes;
        try
        {
            headerBytes = Convert.FromBase64String(request.HeaderBytes);
        }
        catch (FormatException)
        {
            return Results.Problem(new ProblemDetails
            {
                Status = 400,
                Title = "headerBytes is not valid base64.",
                Type = "https://fileway.io/errors/malformedoptions"
            });
        }

        var (format, confidence) = formatDetector.Detect(headerBytes, request.Filename);

        string? detectedFormatId = format?.Id;
        var suggestedTools = format is not null
            ? toolRegistry.GetAccepting(format).Select(t => t.Slug).ToArray()
            : Array.Empty<string>();

        return Results.Ok(new DetectResponse(detectedFormatId, confidence.ToString(), suggestedTools));
    }

    private sealed record DetectRequest(string HeaderBytes, string? Filename, string? DeclaredMimeType);
    private sealed record DetectResponse(string? DetectedFormat, string Confidence, string[] SuggestedTools);
}
