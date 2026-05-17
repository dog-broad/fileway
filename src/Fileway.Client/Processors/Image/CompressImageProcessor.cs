using System.Text.Json;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Shared.Processors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;

namespace Fileway.Client.Processors.Image;

public sealed class CompressImageProcessor : IWasmProcessor
{
    private const long WasmThresholdBytes = 20 * 1024 * 1024;

    public bool CanHandleSize(long fileSizeBytes) => fileSizeBytes <= WasmThresholdBytes;

    public void ValidateOptions(JsonElement toolOptions)
    {
        if (toolOptions.ValueKind == JsonValueKind.Undefined || toolOptions.ValueKind == JsonValueKind.Null)
            return;

        if (toolOptions.TryGetProperty("Quality", out var q))
        {
            if (q.ValueKind != JsonValueKind.Number || !q.TryGetInt32(out var qi) || qi < 1 || qi > 100)
                throw new ProcessorValidationException(ErrorCodes.MalformedOptions, "Quality must be an integer between 1 and 100.");
        }
    }

    public async Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken ct)
    {
        var input = context.InputFiles[0];

        if (input.Content.IsEmpty)
            throw new ProcessorValidationException(ErrorCodes.EmptyFile, "The input file is empty.");

        var opts = context.ToolOptions;
        int quality = 85;
        if (opts.ValueKind != JsonValueKind.Undefined && opts.ValueKind != JsonValueKind.Null &&
            opts.TryGetProperty("Quality", out var qProp) && qProp.TryGetInt32(out var q) && q >= 1 && q <= 100)
        {
            quality = q;
        }

        await Task.Yield();
        ct.ThrowIfCancellationRequested();

        var encoderFormat = input.DetectedFormat;
        var encoder = ResolveEncoder(encoderFormat, quality);

        byte[] outputBytes;
        try
        {
            using var inputStream = new MemoryStream(input.Content.ToArray());
            using var image = await global::SixLabors.ImageSharp.Image.LoadAsync(inputStream, ct);
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
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
            throw new ProcessorUnexpectedException("Image compression failed unexpectedly.", ex);
        }

        var inputSize = (long)input.Content.Length;
        var outputSize = (long)outputBytes.Length;
        var ratio = inputSize > 0 ? (int)Math.Round((1.0 - (double)outputSize / inputSize) * 100) : 0;

        return new ProcessorResult
        {
            OutputContent = outputBytes,
            OutputFormat = encoderFormat,
            OutputFilename = BuildFilename(input.OriginalFilename, encoderFormat),
            Metadata = new Dictionary<string, string>
            {
                ["InputSizeBytes"] = inputSize.ToString(),
                ["OutputSizeBytes"] = outputSize.ToString(),
                ["CompressionRatioPercent"] = ratio.ToString()
            }
        };
    }

    private static IImageEncoder ResolveEncoder(FileFormat format, int quality) => format.Id switch
    {
        "jpg"  => new JpegEncoder { Quality = quality },
        "jpeg" => new JpegEncoder { Quality = quality },
        "webp" => new WebpEncoder { Quality = quality },
        "png"  => new PngEncoder { CompressionLevel = QualityToPngCompression(quality) },
        // BMP and GIF don't support lossy quality — fall back to lossless
        _ => new PngEncoder()
    };

    // PNG compression level: 0 = no compression (fastest), 9 = maximum compression (slowest)
    // Quality 100 → level 0, Quality 1 → level 9
    private static PngCompressionLevel QualityToPngCompression(int quality)
    {
        // Map quality 1–100 linearly to compression level 9–0
        int level = (int)Math.Round(9.0 - (quality - 1) * 9.0 / 99.0);
        level = Math.Clamp(level, 0, 9);
        return level switch
        {
            0 => PngCompressionLevel.NoCompression,
            1 => PngCompressionLevel.Level1,
            2 => PngCompressionLevel.Level2,
            3 => PngCompressionLevel.Level3,
            4 => PngCompressionLevel.Level4,
            5 => PngCompressionLevel.Level5,
            6 => PngCompressionLevel.Level6,
            7 => PngCompressionLevel.Level7,
            8 => PngCompressionLevel.Level8,
            _ => PngCompressionLevel.BestCompression
        };
    }

    private static string BuildFilename(string? original, FileFormat format)
    {
        var ext = format.Extensions[0];
        if (string.IsNullOrWhiteSpace(original)) return $"output.{ext}";
        return $"{Path.GetFileNameWithoutExtension(original)}.{ext}";
    }
}
