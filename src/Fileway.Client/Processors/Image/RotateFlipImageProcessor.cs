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

namespace Fileway.Client.Processors.Image;

public sealed class RotateFlipImageProcessor : IWasmProcessor
{
    private static readonly HashSet<int> _validAngles = [0, 90, 180, 270];

    public bool CanHandleSize(long fileSizeBytes) => true;

    public void ValidateOptions(JsonElement toolOptions)
    {
        if (toolOptions.ValueKind == JsonValueKind.Undefined || toolOptions.ValueKind == JsonValueKind.Null)
            return;

        if (toolOptions.TryGetProperty("Angle", out var angleProp))
        {
            if (angleProp.ValueKind != JsonValueKind.Number || !angleProp.TryGetInt32(out var angle) || !_validAngles.Contains(angle))
                throw new ProcessorValidationException(ErrorCodes.MalformedOptions, "Angle must be 0, 90, 180, or 270.");
        }
    }

    public async Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken ct)
    {
        var input = context.InputFiles[0];

        if (input.Content.IsEmpty)
            throw new ProcessorValidationException(ErrorCodes.EmptyFile, "The input file is empty.");

        var opts = context.ToolOptions;
        int angle = 90;
        if (opts.ValueKind != JsonValueKind.Undefined && opts.ValueKind != JsonValueKind.Null &&
            opts.TryGetProperty("Angle", out var angleProp) && angleProp.TryGetInt32(out var a))
        {
            angle = a;
        }

        await Task.Yield();
        ct.ThrowIfCancellationRequested();

        var encoderFormat = input.DetectedFormat;
        byte[] outputBytes;

        try
        {
            using var inputStream = new MemoryStream(input.Content.ToArray());
            using var image = await global::SixLabors.ImageSharp.Image.LoadAsync(inputStream, ct);
            await Task.Yield();
            ct.ThrowIfCancellationRequested();

            if (angle != 0)
            {
                var rotateMode = angle switch
                {
                    90  => RotateMode.Rotate90,
                    180 => RotateMode.Rotate180,
                    270 => RotateMode.Rotate270,
                    _   => RotateMode.None
                };
                image.Mutate(x => x.Rotate(rotateMode));
            }

            var encoder = ResolveEncoder(encoderFormat);
            using var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, encoder, ct);
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
            throw new ProcessorUnexpectedException("Image rotation failed unexpectedly.", ex);
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
        _ => new PngEncoder()
    };

    private static string BuildFilename(string? original, FileFormat format)
    {
        var ext = format.Extensions[0];
        if (string.IsNullOrWhiteSpace(original)) return $"output.{ext}";
        return $"{Path.GetFileNameWithoutExtension(original)}.{ext}";
    }
}
