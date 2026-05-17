using Fileway.Shared.Formats;

namespace Fileway.Shared.Tools;

public sealed record ToolSlugAlias
{
    public required string Slug { get; init; }
    public required FileFormat PresetOutputFormat { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string SeoTitle { get; init; }
    public required string SeoDescription { get; init; }
    public ToolExample[] Examples { get; init; } = [];
}
