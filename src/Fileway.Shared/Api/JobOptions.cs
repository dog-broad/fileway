using System.Text.Json;

namespace Fileway.Shared.Api;

public sealed record JobOptions
{
    public required string ToolSlug { get; init; }
    public string? OutputFormat { get; init; }
    public string? InlineContent { get; init; }
    public JsonElement ToolOptions { get; init; }
}
