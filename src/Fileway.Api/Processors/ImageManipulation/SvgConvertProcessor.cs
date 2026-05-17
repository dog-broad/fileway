using System.Text.Json;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Shared.Processors;
using SkiaSharp;
using Svg.Skia;

namespace Fileway.Api.Processors.ImageManipulation;

public sealed class SvgConvertProcessor : IApiProcessor
{
    public void ValidateOptions(JsonElement toolOptions) { }

    public async Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var input = context.InputFiles[0];

        if (input.Content.IsEmpty)
            throw new ProcessorValidationException(ErrorCodes.EmptyFile, "The input SVG file is empty.");

        var outputFormat = context.OutputFormat;

        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        byte[] outputBytes;
        try
        {
            using var svg = new SKSvg();
            using var inputStream = new MemoryStream(input.Content.ToArray());
            var picture = svg.Load(inputStream);

            if (picture is null)
                throw new ProcessorDomainException(ErrorCodes.CorruptedFile, "The SVG file could not be parsed.");

            var bounds = picture.CullRect;
            int width  = (int)Math.Ceiling(bounds.Width  > 0 ? bounds.Width  : 800);
            int height = (int)Math.Ceiling(bounds.Height > 0 ? bounds.Height : 600);

            var imageInfo = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(imageInfo);
            using var canvas   = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            canvas.DrawPicture(picture);
            canvas.Flush();

            using var skImage = surface.Snapshot();
            var (skFormat, quality) = outputFormat.Id switch
            {
                "jpg" or "jpeg" => (SKEncodedImageFormat.Jpeg, 90),
                "webp"          => (SKEncodedImageFormat.Webp, 90),
                _               => (SKEncodedImageFormat.Png, 100)
            };

            using var encoded = skImage.Encode(skFormat, quality);
            outputBytes = encoded.ToArray();
        }
        catch (Exception ex) when (ex is not ProcessorDomainException and not ProcessorValidationException and not OperationCanceledException)
        {
            throw new ProcessorUnexpectedException("SVG conversion failed unexpectedly.", ex);
        }

        return new ProcessorResult
        {
            OutputContent = outputBytes,
            OutputFormat = outputFormat,
            OutputFilename = BuildFilename(input.OriginalFilename, outputFormat)
        };
    }

    private static string BuildFilename(string? original, FileFormat format)
    {
        var ext = format.Extensions[0];
        return string.IsNullOrWhiteSpace(original) ? $"output.{ext}" : $"{Path.GetFileNameWithoutExtension(original)}.{ext}";
    }
}
