using System.Text.Json;
using Fileway.Shared.Formats;

namespace Fileway.Shared.Processors;

public sealed record ProcessorContext
{
    public required string ToolSlug { get; init; }
    public required IReadOnlyList<InputFile> InputFiles { get; init; }
    public required FileFormat OutputFormat { get; init; }
    public required JsonElement ToolOptions { get; init; }
    public required CancellationToken CancellationToken { get; init; }
    public required IProgress<ProcessorProgressEvent> Progress { get; init; }
}
