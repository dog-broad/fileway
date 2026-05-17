using Fileway.Shared.Formats;

namespace Fileway.Shared.Tools;

public sealed record ToolSummary
{
    public required string Slug { get; init; }
    public required string DisplayName { get; init; }
    public required string ShortDescription { get; init; }
    public required string Description { get; init; }
    public required ToolCategory Category { get; init; }
    public required ToolKind Kind { get; init; }
    public required FileFormat[] AcceptedFormats { get; init; }
    public required FileFormat[] OutputFormats { get; init; }
    public required bool IsNew { get; init; }
    public required bool IsPopular { get; init; }
    public required long MaxInputSizeBytes { get; init; }
    public required bool AcceptsMultipleFiles { get; init; }
    public required string CanonicalPath { get; init; }
}
