using Fileway.Shared.Detection;
using Fileway.Shared.Formats;

namespace Fileway.Client.Components;

public sealed record DropZoneFile(
    string Name,
    long Size,
    ReadOnlyMemory<byte> Content,
    FileFormat? DetectedFormat,
    DetectionConfidence Confidence);
