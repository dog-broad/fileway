namespace Fileway.Shared.Api;

public sealed record SyncJobResult
{
    public required string ToolSlug { get; init; }
    public required string OutputFormat { get; init; }
    public required string OutputMimeType { get; init; }
    public required long OutputSizeBytes { get; init; }
    public required long DurationMs { get; init; }
    public required DeliveryKind DeliveryKind { get; init; }
    public string? InlineContent { get; init; }
    public string? SignedUrl { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
