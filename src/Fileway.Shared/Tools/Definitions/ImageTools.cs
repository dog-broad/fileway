using Fileway.Shared.Formats;

namespace Fileway.Shared.Tools.Definitions;

public static class ImageTools
{
    private const long FiftyMb = 50 * 1024 * 1024;
    private const long TwentyMb = 20 * 1024 * 1024;
    private const long FiveMb = 5 * 1024 * 1024;

    // Raster formats accepted by most tools
    private static readonly FileFormat[] RasterFormats =
    [
        FileFormats.Png, FileFormats.Jpg, FileFormats.Webp, FileFormats.Gif, FileFormats.Bmp
    ];

    public static readonly ToolDefinition ImageResize = new()
    {
        Slug = "image-resize",
        DisplayName = "Resize Image",
        Description = "Resize images to exact dimensions or by percentage while preserving quality. Runs in your browser for instant results.",
        ShortDescription = "Resize image",
        Kind = ToolKind.Manipulation,
        Category = ToolCategory.Image,
        Tags = ["resize", "image", "dimensions", "width", "height", "scale", "png", "jpeg", "webp", "gif", "bmp"],
        AcceptedFormats = [FileFormats.Png, FileFormats.Jpg, FileFormats.Webp, FileFormats.Gif, FileFormats.Bmp],
        OutputFormats = [FileFormats.Png, FileFormats.Jpg, FileFormats.Webp, FileFormats.Gif, FileFormats.Bmp],
        DefaultOutputFormat = null,
        AcceptsMultipleFiles = false,
        RequiresFileInput = true,
        ProcessorKind = ProcessorKind.WasmPreferred,
        WasmSizeThresholdBytes = TwentyMb,
        JobTier = JobTier.Synchronous,
        ProcessorType = null,
        ProgressStages = [],
        TimeoutSeconds = 60,
        MaxInputSizeBytes = FiftyMb,
        MaxInputFileCount = 1,
        FreemiumLimitOverrides = null,
        InputPreviewKind = PreviewKind.SideBySideImage,
        OutputPreviewKind = PreviewKind.SideBySideImage,
        UiHints = UiHints.ShowDimensionInputs,
        IsNew = true,
        IsPopular = true,
        SortOrder = 1,
        SeoTitle = "Resize Image — Fileway",
        SeoDescription = "Resize PNG, JPEG, WebP, GIF, and BMP images to exact dimensions or by percentage. Free, private, runs in your browser.",
        SeoKeywords = ["resize image", "image resizer", "resize png", "resize jpeg", "resize webp", "scale image"],
        RelatedSlugs = ["image-rotate", "compress-image", "image-convert"],
        SuggestionWeight = 90
    };

    public static readonly ToolDefinition ImageRotate = new()
    {
        Slug = "image-rotate",
        DisplayName = "Rotate Image",
        Description = "Rotate images by 90°, 180°, or 270°, or flip horizontally and vertically. Runs entirely in your browser.",
        ShortDescription = "Rotate image",
        Kind = ToolKind.Manipulation,
        Category = ToolCategory.Image,
        Tags = ["rotate", "flip", "image", "png", "jpeg", "webp", "gif", "bmp"],
        AcceptedFormats = [FileFormats.Png, FileFormats.Jpg, FileFormats.Webp, FileFormats.Gif, FileFormats.Bmp],
        OutputFormats = [FileFormats.Png, FileFormats.Jpg, FileFormats.Webp, FileFormats.Gif, FileFormats.Bmp],
        DefaultOutputFormat = null,
        AcceptsMultipleFiles = false,
        RequiresFileInput = true,
        ProcessorKind = ProcessorKind.WasmOnly,
        WasmSizeThresholdBytes = null,
        JobTier = JobTier.Synchronous,
        ProcessorType = null,
        ProgressStages = [],
        TimeoutSeconds = 60,
        MaxInputSizeBytes = FiftyMb,
        MaxInputFileCount = 1,
        FreemiumLimitOverrides = null,
        InputPreviewKind = PreviewKind.SideBySideImage,
        OutputPreviewKind = PreviewKind.SideBySideImage,
        UiHints = UiHints.None,
        IsNew = true,
        IsPopular = false,
        SortOrder = 2,
        SeoTitle = "Rotate Image — Fileway",
        SeoDescription = "Rotate or flip PNG, JPEG, WebP, GIF, and BMP images in your browser. Free and instant.",
        SeoKeywords = ["rotate image", "flip image", "rotate png", "rotate jpeg", "rotate photo"],
        RelatedSlugs = ["image-resize", "compress-image", "image-convert"],
        SuggestionWeight = 75
    };

    public static readonly ToolDefinition CompressImage = new()
    {
        Slug = "compress-image",
        DisplayName = "Compress Image",
        Description = "Reduce image file size while maintaining visual quality. Adjust the quality slider to balance size and fidelity.",
        ShortDescription = "Compress image",
        Kind = ToolKind.Manipulation,
        Category = ToolCategory.Image,
        Tags = ["compress", "image", "optimise", "optimize", "quality", "png", "jpeg", "webp"],
        AcceptedFormats = [FileFormats.Png, FileFormats.Jpg, FileFormats.Webp],
        OutputFormats = [FileFormats.Png, FileFormats.Jpg, FileFormats.Webp],
        DefaultOutputFormat = null,
        AcceptsMultipleFiles = false,
        RequiresFileInput = true,
        ProcessorKind = ProcessorKind.WasmPreferred,
        WasmSizeThresholdBytes = TwentyMb,
        JobTier = JobTier.Synchronous,
        ProcessorType = null,
        ProgressStages = [],
        TimeoutSeconds = 60,
        MaxInputSizeBytes = FiftyMb,
        MaxInputFileCount = 1,
        FreemiumLimitOverrides = null,
        InputPreviewKind = PreviewKind.SideBySideImage,
        OutputPreviewKind = PreviewKind.SideBySideImage,
        UiHints = UiHints.ShowQualitySlider,
        IsNew = true,
        IsPopular = true,
        SortOrder = 3,
        SeoTitle = "Compress Image — Fileway",
        SeoDescription = "Compress PNG, JPEG, and WebP images to reduce file size without sacrificing quality. Free, private, runs in your browser.",
        SeoKeywords = ["compress image", "reduce image size", "image compression", "compress png", "compress jpeg", "compress webp"],
        RelatedSlugs = ["image-resize", "image-convert"],
        SuggestionWeight = 88
    };

    public static readonly ToolDefinition ImageConvert = new()
    {
        Slug = "image-convert",
        DisplayName = "Convert Image",
        Description = "Convert images between PNG, JPEG, WebP, GIF, and BMP formats. Runs entirely in your browser.",
        ShortDescription = "Convert image",
        Kind = ToolKind.Conversion,
        Category = ToolCategory.Image,
        Tags = ["convert", "image", "format", "png", "jpeg", "webp", "gif", "bmp", "svg"],
        AcceptedFormats = [FileFormats.Png, FileFormats.Jpg, FileFormats.Webp, FileFormats.Gif, FileFormats.Bmp, FileFormats.Svg],
        OutputFormats = [FileFormats.Png, FileFormats.Jpg, FileFormats.Webp, FileFormats.Gif, FileFormats.Bmp],
        DefaultOutputFormat = FileFormats.Png,
        AcceptsMultipleFiles = false,
        RequiresFileInput = true,
        ProcessorKind = ProcessorKind.WasmOnly,
        WasmSizeThresholdBytes = null,
        JobTier = JobTier.Synchronous,
        ProcessorType = null,
        ProgressStages = [],
        TimeoutSeconds = 60,
        MaxInputSizeBytes = FiftyMb,
        MaxInputFileCount = 1,
        FreemiumLimitOverrides = null,
        InputPreviewKind = PreviewKind.SideBySideImage,
        OutputPreviewKind = PreviewKind.SideBySideImage,
        UiHints = UiHints.None,
        IsNew = true,
        IsPopular = false,
        SortOrder = 4,
        SeoTitle = "Convert Image Format — Fileway",
        SeoDescription = "Convert images between PNG, JPEG, WebP, GIF, BMP, and SVG formats in your browser. Free and instant.",
        SeoKeywords = ["convert image", "image format converter", "png to jpeg", "jpg to webp", "svg to png", "image converter"],
        RelatedSlugs = ["compress-image", "image-resize"],
        SuggestionWeight = 85
    };

    public static readonly ToolDefinition SvgConvert = new()
    {
        Slug = "svg-convert",
        DisplayName = "SVG to Raster",
        Description = "Convert SVG vector graphics to PNG, JPEG, or WebP raster images at any resolution. Runs in your browser for small files.",
        ShortDescription = "SVG to PNG/JPEG",
        Kind = ToolKind.Conversion,
        Category = ToolCategory.Image,
        Tags = ["svg", "convert", "png", "jpeg", "webp", "vector", "raster"],
        AcceptedFormats = [FileFormats.Svg],
        OutputFormats = [FileFormats.Png, FileFormats.Jpg, FileFormats.Webp],
        DefaultOutputFormat = FileFormats.Png,
        AcceptsMultipleFiles = false,
        RequiresFileInput = true,
        ProcessorKind = ProcessorKind.WasmPreferred,
        WasmSizeThresholdBytes = FiveMb,
        JobTier = JobTier.Synchronous,
        ProcessorType = null,
        ProgressStages = [],
        TimeoutSeconds = 60,
        MaxInputSizeBytes = FiveMb,
        MaxInputFileCount = 1,
        FreemiumLimitOverrides = null,
        InputPreviewKind = PreviewKind.SideBySideImage,
        OutputPreviewKind = PreviewKind.SideBySideImage,
        UiHints = UiHints.None,
        IsNew = true,
        IsPopular = false,
        SortOrder = 5,
        SeoTitle = "SVG to PNG Converter — Fileway",
        SeoDescription = "Convert SVG vector files to PNG, JPEG, or WebP raster images. Free, private, runs in your browser.",
        SeoKeywords = ["svg to png", "svg to jpeg", "svg to webp", "svg converter", "vector to raster"],
        RelatedSlugs = ["image-convert", "compress-image"],
        SuggestionWeight = 80
    };

    public static readonly IReadOnlyList<ToolDefinition> All =
    [
        ImageResize,
        ImageRotate,
        CompressImage,
        ImageConvert,
        SvgConvert
    ];
}
