using Fileway.Shared.Formats;

namespace Fileway.Shared.Detection;

public interface IFormatDetector
{
    (FileFormat? Format, DetectionConfidence Confidence) Detect(ReadOnlySpan<byte> header, string? filename);
}
