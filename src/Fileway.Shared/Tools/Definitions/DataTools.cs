using Fileway.Shared.Formats;

namespace Fileway.Shared.Tools.Definitions;

public static class DataTools
{
    private const long TenMb = 10 * 1024 * 1024;
    private const long FiftyMb = 50 * 1024 * 1024;
    private const long FiveMb = 5 * 1024 * 1024;

    public static readonly ToolDefinition JsonToYaml = new()
    {
        Slug = "json-to-yaml",
        DisplayName = "JSON ↔ YAML",
        Description = "Convert between JSON and YAML formats instantly. Paste JSON to get YAML, or paste YAML to get JSON. Runs entirely in your browser.",
        ShortDescription = "JSON ↔ YAML",
        Kind = ToolKind.Conversion,
        Category = ToolCategory.Data,
        Tags = ["json", "yaml", "convert", "format", "data", "serialisation"],
        AcceptedFormats = [FileFormats.Json, FileFormats.Yaml],
        OutputFormats = [FileFormats.Yaml, FileFormats.Json],
        DefaultOutputFormat = FileFormats.Yaml,
        AcceptsMultipleFiles = false,
        RequiresFileInput = false,
        ProcessorKind = ProcessorKind.WasmOnly,
        WasmSizeThresholdBytes = null,
        JobTier = JobTier.Synchronous,
        ProcessorType = null,
        ProgressStages = [],
        TimeoutSeconds = 60,
        MaxInputSizeBytes = TenMb,
        MaxInputFileCount = 1,
        FreemiumLimitOverrides = null,
        InputPreviewKind = PreviewKind.SyntaxHighlight,
        OutputPreviewKind = PreviewKind.SyntaxHighlight,
        UiHints = UiHints.None,
        IsNew = true,
        IsPopular = true,
        SortOrder = 1,
        SeoTitle = "JSON to YAML Converter — Fileway",
        SeoDescription = "Convert JSON to YAML or YAML to JSON instantly in your browser. Free, private, no upload required.",
        SeoKeywords = ["json to yaml", "yaml to json", "json yaml converter", "yaml converter"],
        RelatedSlugs = ["json-to-csv", "json-to-toml", "validate"],
        SuggestionWeight = 90
    };

    public static readonly ToolDefinition JsonToCsv = new()
    {
        Slug = "json-to-csv",
        DisplayName = "JSON ↔ CSV",
        Description = "Convert between JSON arrays and CSV. Paste a JSON array to get CSV rows, or paste CSV to get a JSON array. Runs entirely in your browser.",
        ShortDescription = "JSON ↔ CSV",
        Kind = ToolKind.Conversion,
        Category = ToolCategory.Data,
        Tags = ["json", "csv", "convert", "format", "data", "spreadsheet"],
        AcceptedFormats = [FileFormats.Json, FileFormats.Csv],
        OutputFormats = [FileFormats.Csv, FileFormats.Json],
        DefaultOutputFormat = FileFormats.Csv,
        AcceptsMultipleFiles = false,
        RequiresFileInput = false,
        ProcessorKind = ProcessorKind.WasmOnly,
        WasmSizeThresholdBytes = null,
        JobTier = JobTier.Synchronous,
        ProcessorType = null,
        ProgressStages = [],
        TimeoutSeconds = 60,
        MaxInputSizeBytes = TenMb,
        MaxInputFileCount = 1,
        FreemiumLimitOverrides = null,
        InputPreviewKind = PreviewKind.SyntaxHighlight,
        OutputPreviewKind = PreviewKind.SyntaxHighlight,
        UiHints = UiHints.None,
        IsNew = true,
        IsPopular = false,
        SortOrder = 2,
        SeoTitle = "JSON to CSV Converter — Fileway",
        SeoDescription = "Convert JSON arrays to CSV or CSV to JSON in your browser. Free, private, instant conversion.",
        SeoKeywords = ["json to csv", "csv to json", "json csv converter"],
        RelatedSlugs = ["json-to-yaml", "json-to-toml", "csv-to-xlsx", "validate"],
        SuggestionWeight = 80
    };

    public static readonly ToolDefinition JsonToToml = new()
    {
        Slug = "json-to-toml",
        DisplayName = "JSON ↔ TOML",
        Description = "Convert between JSON and TOML configuration formats. Paste JSON to get TOML, or paste TOML to get JSON. Runs entirely in your browser.",
        ShortDescription = "JSON ↔ TOML",
        Kind = ToolKind.Conversion,
        Category = ToolCategory.Data,
        Tags = ["json", "toml", "convert", "format", "data", "config"],
        AcceptedFormats = [FileFormats.Json, FileFormats.Toml],
        OutputFormats = [FileFormats.Toml, FileFormats.Json],
        DefaultOutputFormat = FileFormats.Toml,
        AcceptsMultipleFiles = false,
        RequiresFileInput = false,
        ProcessorKind = ProcessorKind.WasmOnly,
        WasmSizeThresholdBytes = null,
        JobTier = JobTier.Synchronous,
        ProcessorType = null,
        ProgressStages = [],
        TimeoutSeconds = 60,
        MaxInputSizeBytes = TenMb,
        MaxInputFileCount = 1,
        FreemiumLimitOverrides = null,
        InputPreviewKind = PreviewKind.SyntaxHighlight,
        OutputPreviewKind = PreviewKind.SyntaxHighlight,
        UiHints = UiHints.None,
        IsNew = true,
        IsPopular = false,
        SortOrder = 3,
        SeoTitle = "JSON to TOML Converter — Fileway",
        SeoDescription = "Convert JSON to TOML or TOML to JSON in your browser. Ideal for configuration file conversion.",
        SeoKeywords = ["json to toml", "toml to json", "json toml converter", "toml converter"],
        RelatedSlugs = ["json-to-yaml", "json-to-csv", "validate"],
        SuggestionWeight = 70
    };

    public static readonly ToolDefinition Validate = new()
    {
        Slug = "validate",
        DisplayName = "Validate Format",
        Description = "Validate the structure of JSON, YAML, CSV, or TOML. Instantly highlights syntax errors and structural problems. Runs entirely in your browser.",
        ShortDescription = "Validate JSON/YAML",
        Kind = ToolKind.Manipulation,
        Category = ToolCategory.Data,
        Tags = ["validate", "json", "yaml", "csv", "toml", "lint", "syntax", "check"],
        AcceptedFormats = [FileFormats.Json, FileFormats.Yaml, FileFormats.Csv, FileFormats.Toml],
        OutputFormats = [FileFormats.Json, FileFormats.Yaml, FileFormats.Csv, FileFormats.Toml],
        DefaultOutputFormat = null,
        AcceptsMultipleFiles = false,
        RequiresFileInput = false,
        ProcessorKind = ProcessorKind.WasmOnly,
        WasmSizeThresholdBytes = null,
        JobTier = JobTier.Synchronous,
        ProcessorType = null,
        ProgressStages = [],
        TimeoutSeconds = 60,
        MaxInputSizeBytes = TenMb,
        MaxInputFileCount = 1,
        FreemiumLimitOverrides = null,
        InputPreviewKind = PreviewKind.InlineEditor,
        OutputPreviewKind = PreviewKind.SyntaxHighlight,
        UiHints = UiHints.None,
        IsNew = true,
        IsPopular = true,
        SortOrder = 4,
        SeoTitle = "JSON & YAML Validator — Fileway",
        SeoDescription = "Validate JSON, YAML, CSV, and TOML in your browser. Instant syntax checking with clear error messages.",
        SeoKeywords = ["json validator", "yaml validator", "csv validator", "toml validator", "validate json"],
        RelatedSlugs = ["json-to-yaml", "json-to-csv", "json-to-toml"],
        SuggestionWeight = 85
    };

    public static readonly ToolDefinition CsvToXlsx = new()
    {
        Slug = "csv-to-xlsx",
        DisplayName = "CSV to Excel",
        Description = "Convert a CSV file to an Excel spreadsheet (.xlsx). Runs in your browser for small files, or falls back to the server for large files.",
        ShortDescription = "CSV to Excel",
        Kind = ToolKind.Conversion,
        Category = ToolCategory.Data,
        Tags = ["csv", "xlsx", "excel", "spreadsheet", "convert"],
        AcceptedFormats = [FileFormats.Csv],
        OutputFormats = [FileFormats.Xlsx],
        DefaultOutputFormat = FileFormats.Xlsx,
        AcceptsMultipleFiles = false,
        RequiresFileInput = false,
        ProcessorKind = ProcessorKind.WasmPreferred,
        WasmSizeThresholdBytes = FiveMb,
        JobTier = JobTier.Synchronous,
        ProcessorType = null,
        ProgressStages = [],
        TimeoutSeconds = 60,
        MaxInputSizeBytes = FiftyMb,
        MaxInputFileCount = 1,
        FreemiumLimitOverrides = null,
        InputPreviewKind = PreviewKind.SyntaxHighlight,
        OutputPreviewKind = PreviewKind.None,
        UiHints = UiHints.None,
        IsNew = true,
        IsPopular = true,
        SortOrder = 5,
        SeoTitle = "CSV to Excel Converter — Fileway",
        SeoDescription = "Convert CSV files to Excel .xlsx format instantly. Free, no sign-up. Runs in your browser.",
        SeoKeywords = ["csv to excel", "csv to xlsx", "csv excel converter", "convert csv"],
        RelatedSlugs = ["json-to-csv", "validate"],
        SuggestionWeight = 88
    };

    public static readonly IReadOnlyList<ToolDefinition> All =
    [
        JsonToYaml,
        JsonToCsv,
        JsonToToml,
        Validate,
        CsvToXlsx
    ];
}
