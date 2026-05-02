using Fileway.Shared.Formats;

namespace Fileway.Shared.Tools;

public sealed record ToolDefinition
{
    // Core identity
    public required string Slug { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required string ShortDescription { get; init; }
    public required ToolKind Kind { get; init; }
    public required ToolCategory Category { get; init; }
    public required string[] Tags { get; init; }

    // Format contract
    public required FileFormat[] AcceptedFormats { get; init; }
    public required FileFormat[] OutputFormats { get; init; }
    public FileFormat? DefaultOutputFormat { get; init; }
    public required bool AcceptsMultipleFiles { get; init; }
    public required bool RequiresFileInput { get; init; }

    // Processing configuration
    public required ProcessorKind ProcessorKind { get; init; }
    public long? WasmSizeThresholdBytes { get; init; }
    public required JobTier JobTier { get; init; }
    public Type? ProcessorType { get; set; }
    public required string[] ProgressStages { get; init; }
    public required int TimeoutSeconds { get; init; }

    // Limits
    public required long MaxInputSizeBytes { get; init; }
    public required int MaxInputFileCount { get; init; }
    public ToolLimits? FreemiumLimitOverrides { get; init; }

    // UX and presentation
    public required PreviewKind InputPreviewKind { get; init; }
    public required PreviewKind OutputPreviewKind { get; init; }
    public required UiHints UiHints { get; init; }
    public required bool IsNew { get; init; }
    public required bool IsPopular { get; init; }
    public required int SortOrder { get; init; }

    // SEO
    public required string SeoTitle { get; init; }
    public required string SeoDescription { get; init; }
    public required string[] SeoKeywords { get; init; }
    public string CanonicalPath => $"/tools/{Slug}";

    // Suggestion engine
    public required string[] RelatedSlugs { get; init; }
    public required int SuggestionWeight { get; init; }

    // Examples
    public ToolExample[] Examples { get; init; } = [];
}
