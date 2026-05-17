namespace Fileway.Shared.Formats;

public sealed record FileFormat
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string[] MimeTypes { get; init; }
    public required string[] Extensions { get; init; }
    public required MagicSignature[] MagicBytes { get; init; }
    public required FormatCategory FormatCategory { get; init; }
    public required bool CanBeDetected { get; init; }
    public string[]? DetectionHints { get; init; }
    public required long MaxFileSizeBytes { get; init; }
    public required bool IsTextBased { get; init; }
    public required PreviewKind PreviewKind { get; init; }
}
