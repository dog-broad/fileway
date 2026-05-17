using System.Text.Json;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Shared.Processors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Fileway.Api.Processors.ImageManipulation;

public sealed class ResizeImageProcessor : IApiProcessor
{
    public void ValidateOptions(JsonElement toolOptions)
    {
        if (toolOptions.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return;

        if (toolOptions.TryGetProperty("Width", out var w) && w.ValueKind != JsonValueKind.Null)
        {
            if (w.ValueKind != JsonValueKind.Number || !w.TryGetInt32(out var wi) || wi <= 0 || wi > 16000)
                throw new ProcessorValidationException(ErrorCodes.InvalidDimensions, "Width must be a positive integer no greater than 16000.");
        }

        if (toolOptions.TryGetProperty("Height", out var h) && h.ValueKind != JsonValueKind.Null)
        {
            if (h.ValueKind != JsonValueKind.Number || !h.TryGetInt32(out var hi) || hi <= 0 || hi > 16000)
                throw new ProcessorValidationException(ErrorCodes.InvalidDimensions, "Height must be a positive integer no greater than 16000.");
        }
    }

    public async Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var input = context.InputFiles[0];

        if (input.Content.IsEmpty)
            throw new ProcessorValidationException(ErrorCodes.EmptyFile, "The input file is empty.");

        var opts = context.ToolOptions;
        int? requestedWidth = TryGetPositiveInt(opts, "Width");
        int? requestedHeight = TryGetPositiveInt(opts, "Height");
        bool lockAspect = TryGetBool(opts, "LockAspectRatio", defaultValue: true);

        byte[] outputBytes;
        var encoderFormat = input.DetectedFormat;

        try
        {
            using var inputStream = new MemoryStream(input.Content.ToArray());
            using var image = await Image.LoadAsync(inputStream, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (requestedWidth.HasValue || requestedHeight.HasValue)
            {
                int targetWidth = requestedWidth ?? 0;
                int targetHeight = requestedHeight ?? 0;

                if (lockAspect)
                {
                    if (requestedWidth.HasValue && !requestedHeight.HasValue)
                    {
                        targetHeight = (int)Math.Round((double)image.Height * requestedWidth.Value / image.Width);
                        if (targetHeight < 1) targetHeight = 1;
                    }
                    else if (requestedHeight.HasValue && !requestedWidth.HasValue)
                    {
                        targetWidth = (int)Math.Round((double)image.Width * requestedHeight.Value / image.Height);
                        if (targetWidth < 1) targetWidth = 1;
                    }
                    image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(targetWidth, targetHeight), Mode = ResizeMode.Max }));
                }
                else
                {
                    if (targetWidth == 0) targetWidth = image.Width;
                    if (targetHeight == 0) targetHeight = image.Height;
                    image.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(targetWidth, targetHeight), Mode = ResizeMode.Stretch }));
                }
            }

            var encoder = ResolveEncoder(encoderFormat);
            using var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, encoder, cancellationToken);
            outputBytes = outputStream.ToArray();
        }
        catch (InvalidImageContentException ex)
        {
            throw new ProcessorDomainException(ErrorCodes.CorruptedFile, "The image file could not be decoded.", ex);
        }
        catch (UnknownImageFormatException ex)
        {
            throw new ProcessorDomainException(ErrorCodes.UnsupportedMediaType, "The image format is not supported.", ex);
        }
        catch (Exception ex) when (ex is not ProcessorDomainException and not ProcessorValidationException and not OperationCanceledException)
        {
            throw new ProcessorUnexpectedException("Image resize failed unexpectedly.", ex);
        }

        return new ProcessorResult
        {
            OutputContent = outputBytes,
            OutputFormat = encoderFormat,
            OutputFilename = BuildFilename(input.OriginalFilename, encoderFormat)
        };
    }

    private static IImageEncoder ResolveEncoder(FileFormat format) => format.Id switch
    {
        "png"  => new PngEncoder(),
        "jpg"  => new JpegEncoder { Quality = 85 },
        "jpeg" => new JpegEncoder { Quality = 85 },
        "webp" => new WebpEncoder(),
        "gif"  => new GifEncoder(),
        "bmp"  => new BmpEncoder(),
        _      => new PngEncoder()
    };

    private static int? TryGetPositiveInt(JsonElement opts, string key)
    {
        if (opts.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return null;
        if (!opts.TryGetProperty(key, out var prop) || prop.ValueKind == JsonValueKind.Null) return null;
        return prop.TryGetInt32(out var v) && v > 0 ? v : null;
    }

    private static bool TryGetBool(JsonElement opts, string key, bool defaultValue)
    {
        if (opts.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return defaultValue;
        if (!opts.TryGetProperty(key, out var prop)) return defaultValue;
        return prop.ValueKind switch { JsonValueKind.True => true, JsonValueKind.False => false, _ => defaultValue };
    }

    private static string BuildFilename(string? original, FileFormat format)
    {
        var ext = format.Extensions[0];
        return string.IsNullOrWhiteSpace(original) ? $"output.{ext}" : $"{Path.GetFileNameWithoutExtension(original)}.{ext}";
    }
}
