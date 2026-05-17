namespace Fileway.Shared.Formats;

public static class FileFormats
{
    private const long TenMb = 10 * 1024 * 1024;
    private const long TwentyMb = 20 * 1024 * 1024;
    private const long FiftyMb = 50 * 1024 * 1024;
    private const long FiveMb = 5 * 1024 * 1024;

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

    // ── Image formats ────────────────────────────────────────────────

    public static readonly FileFormat Png = new()
    {
        Id = "png",
        DisplayName = "PNG",
        MimeTypes = ["image/png"],
        Extensions = ["png"],
        MagicBytes =
        [
            new MagicSignature { Offset = 0, Bytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], Mask = null }
        ],
        FormatCategory = FormatCategory.Image,
        CanBeDetected = true,
        DetectionHints = null,
        MaxFileSizeBytes = FiftyMb,
        IsTextBased = false,
        PreviewKind = PreviewKind.SideBySideImage
    };

    public static readonly FileFormat Jpg = new()
    {
        Id = "jpg",
        DisplayName = "JPEG",
        MimeTypes = ["image/jpeg"],
        Extensions = ["jpg", "jpeg"],
        MagicBytes =
        [
            new MagicSignature { Offset = 0, Bytes = [0xFF, 0xD8, 0xFF], Mask = null }
        ],
        FormatCategory = FormatCategory.Image,
        CanBeDetected = true,
        DetectionHints = null,
        MaxFileSizeBytes = FiftyMb,
        IsTextBased = false,
        PreviewKind = PreviewKind.SideBySideImage
    };

    public static readonly FileFormat Webp = new()
    {
        Id = "webp",
        DisplayName = "WebP",
        MimeTypes = ["image/webp"],
        Extensions = ["webp"],
        // RIFF????WEBP — 4 wildcard size bytes at offset 4, so two signatures with mask
        MagicBytes =
        [
            new MagicSignature
            {
                Offset = 0,
                Bytes = [0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50],
                Mask  = [0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF]
            }
        ],
        FormatCategory = FormatCategory.Image,
        CanBeDetected = true,
        DetectionHints = null,
        MaxFileSizeBytes = FiftyMb,
        IsTextBased = false,
        PreviewKind = PreviewKind.SideBySideImage
    };

    public static readonly FileFormat Gif = new()
    {
        Id = "gif",
        DisplayName = "GIF",
        MimeTypes = ["image/gif"],
        Extensions = ["gif"],
        MagicBytes =
        [
            new MagicSignature { Offset = 0, Bytes = [0x47, 0x49, 0x46, 0x38], Mask = null }
        ],
        FormatCategory = FormatCategory.Image,
        CanBeDetected = true,
        DetectionHints = null,
        MaxFileSizeBytes = TwentyMb,
        IsTextBased = false,
        PreviewKind = PreviewKind.SideBySideImage
    };

    public static readonly FileFormat Bmp = new()
    {
        Id = "bmp",
        DisplayName = "BMP",
        MimeTypes = ["image/bmp"],
        Extensions = ["bmp"],
        MagicBytes =
        [
            new MagicSignature { Offset = 0, Bytes = [0x42, 0x4D], Mask = null }
        ],
        FormatCategory = FormatCategory.Image,
        CanBeDetected = true,
        DetectionHints = null,
        MaxFileSizeBytes = TwentyMb,
        IsTextBased = false,
        PreviewKind = PreviewKind.SideBySideImage
    };

    public static readonly FileFormat Svg = new()
    {
        Id = "svg",
        DisplayName = "SVG",
        MimeTypes = ["image/svg+xml"],
        Extensions = ["svg"],
        MagicBytes = [],
        FormatCategory = FormatCategory.Image,
        CanBeDetected = true,
        DetectionHints = ["<svg"],
        MaxFileSizeBytes = FiveMb,
        IsTextBased = true,
        PreviewKind = PreviewKind.SideBySideImage
    };

    public static readonly IReadOnlyList<FileFormat> All =
    [
        Json, Yaml, Csv, Toml, Xlsx, Txt, Md,
        Png, Jpg, Webp, Gif, Bmp, Svg
    ];
}
