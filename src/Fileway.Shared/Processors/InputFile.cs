using Fileway.Shared.Formats;

namespace Fileway.Shared.Processors;

public sealed record InputFile
{
    public required ReadOnlyMemory<byte> Content { get; init; }
    public required FileFormat DetectedFormat { get; init; }
    public required long SizeBytes { get; init; }
    public string? OriginalFilename { get; init; }
    public required int Index { get; init; }
}
