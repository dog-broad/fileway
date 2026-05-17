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

namespace Fileway.Client.Processors.Image;

public sealed class ConvertImageProcessor : IWasmProcessor
{
    public bool CanHandleSize(long fileSizeBytes) => true;

    public void ValidateOptions(JsonElement toolOptions) { }

    public async Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken ct)
    {
        var input = context.InputFiles[0];

        if (input.Content.IsEmpty)
            throw new ProcessorValidationException(ErrorCodes.EmptyFile, "The input file is empty.");

        await Task.Yield();
        ct.ThrowIfCancellationRequested();

        var outputFormat = context.OutputFormat;
        var encoder = ResolveEncoder(outputFormat);

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
            throw new ProcessorUnexpectedException("Image conversion failed unexpectedly.", ex);
        }

        return new ProcessorResult
        {
            OutputContent = outputBytes,
            OutputFormat = outputFormat,
            OutputFilename = BuildFilename(input.OriginalFilename, outputFormat)
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
        _ => throw new ProcessorValidationException(ErrorCodes.InvalidOutputFormat,
                 $"Unsupported output format '{format.Id}'.")
    };

    private static string BuildFilename(string? original, FileFormat format)
    {
        var ext = format.Extensions[0];
        if (string.IsNullOrWhiteSpace(original)) return $"output.{ext}";
        return $"{Path.GetFileNameWithoutExtension(original)}.{ext}";
    }
}
