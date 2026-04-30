namespace Fileway.Shared.Formats;

public static class FileFormats
{
    private const long TenMb = 10 * 1024 * 1024;
    private const long FiftyMb = 50 * 1024 * 1024;

    public static readonly FileFormat Json = new()
    {
        Id = "json",
        DisplayName = "JSON",
        MimeTypes = ["application/json"],
        Extensions = ["json"],
        MagicBytes = [],
        FormatCategory = FormatCategory.Data,
        CanBeDetected = false,
        DetectionHints = [@"^\s*[\{\[]"],
        MaxFileSizeBytes = TenMb,
        IsTextBased = true,
        PreviewKind = PreviewKind.SyntaxHighlight
    };

    public static readonly FileFormat Yaml = new()
    {
        Id = "yaml",
        DisplayName = "YAML",
        MimeTypes = ["application/yaml", "text/yaml"],
        Extensions = ["yaml", "yml"],
        MagicBytes = [],
        FormatCategory = FormatCategory.Data,
        CanBeDetected = false,
        DetectionHints = [@"^---", @"^\w+:\s"],
        MaxFileSizeBytes = TenMb,
        IsTextBased = true,
        PreviewKind = PreviewKind.SyntaxHighlight
    };

    public static readonly FileFormat Csv = new()
    {
        Id = "csv",
        DisplayName = "CSV",
        MimeTypes = ["text/csv"],
        Extensions = ["csv"],
        MagicBytes = [],
        FormatCategory = FormatCategory.Data,
        CanBeDetected = false,
        DetectionHints = [@"^[^<\{\[]*,[^<\{\[]*"],
        MaxFileSizeBytes = TenMb,
        IsTextBased = true,
        PreviewKind = PreviewKind.SyntaxHighlight
    };

    public static readonly FileFormat Toml = new()
    {
        Id = "toml",
        DisplayName = "TOML",
        MimeTypes = ["application/toml"],
        Extensions = ["toml"],
        MagicBytes = [],
        FormatCategory = FormatCategory.Data,
        CanBeDetected = false,
        DetectionHints = [@"^\[[\w.]+\]", @"^\w+ = "],
        MaxFileSizeBytes = TenMb,
        IsTextBased = true,
        PreviewKind = PreviewKind.SyntaxHighlight
    };

    public static readonly FileFormat Xlsx = new()
    {
        Id = "xlsx",
        DisplayName = "Excel",
        MimeTypes = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
        Extensions = ["xlsx"],
        MagicBytes =
        [
            // ZIP local file header — triggers Pass 2 ZIP disambiguation
            new MagicSignature { Offset = 0, Bytes = [0x50, 0x4B, 0x03, 0x04], Mask = null }
        ],
        FormatCategory = FormatCategory.Data,
        CanBeDetected = true,
        DetectionHints = null,
        MaxFileSizeBytes = FiftyMb,
        IsTextBased = false,
        PreviewKind = PreviewKind.None
    };

    public static readonly FileFormat Txt = new()
    {
        Id = "txt",
        DisplayName = "Plain Text",
        MimeTypes = ["text/plain"],
        Extensions = ["txt"],
        MagicBytes = [],
        FormatCategory = FormatCategory.Data,
        CanBeDetected = false,
        DetectionHints = null,
        MaxFileSizeBytes = TenMb,
        IsTextBased = true,
        PreviewKind = PreviewKind.SyntaxHighlight
    };

    public static readonly FileFormat Md = new()
    {
        Id = "md",
        DisplayName = "Markdown",
        MimeTypes = ["text/markdown"],
        Extensions = ["md", "markdown"],
        MagicBytes = [],
        FormatCategory = FormatCategory.Data,
        CanBeDetected = false,
        DetectionHints = [@"^#", @"^---"],
        MaxFileSizeBytes = TenMb,
        IsTextBased = true,
        PreviewKind = PreviewKind.SyntaxHighlight
    };
}
