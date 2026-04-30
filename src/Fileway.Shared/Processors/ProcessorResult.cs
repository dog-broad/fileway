using Fileway.Shared.Formats;

namespace Fileway.Shared.Processors;

public sealed record ProcessorResult
{
    public required ReadOnlyMemory<byte> OutputContent { get; init; }
    public required FileFormat OutputFormat { get; init; }
    public required string OutputFilename { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
