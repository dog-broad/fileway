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
        SuggestionWeight = 90,
        Examples =
        [
            new()
            {
                Label = "Service config",
                Input = """
                    {
                      "service": "api-gateway",
                      "version": "2.4.1",
                      "environment": "production",
                      "server": {
                        "host": "0.0.0.0",
                        "port": 8080,
                        "tls": {
                          "enabled": true,
                          "cert": "/etc/certs/server.crt",
                          "key": "/etc/certs/server.key"
                        }
                      },
                      "database": {
                        "primary": {
                          "host": "db-primary.internal",
                          "port": 5432,
                          "name": "appdb",
                          "pool": { "min": 5, "max": 20, "idleTimeoutMs": 30000 }
                        },
                        "replicas": [
                          { "host": "db-replica-1.internal", "port": 5432, "weight": 70 },
                          { "host": "db-replica-2.internal", "port": 5432, "weight": 30 }
                        ]
                      },
                      "features": {
                        "rateLimit": { "enabled": true, "requestsPerMinute": 120 },
                        "caching": { "enabled": true, "ttlSeconds": 300 },
                        "auth": { "provider": "oauth2", "introspectionUrl": "https://auth.internal/introspect" }
                      },
                      "logging": {
                        "level": "info",
                        "format": "json",
                        "sinks": ["stdout", "datadog"]
                      }
                    }
                    """
            },
            new()
            {
                Label = "CI pipeline",
                Input = """
                    {
                      "pipeline": {
                        "name": "build-and-deploy",
                        "trigger": {
                          "branches": ["main", "release/*"],
                          "paths": ["src/**", "tests/**"],
                          "exclude": ["docs/**", "*.md"]
                        },
                        "stages": [
                          {
                            "name": "test",
                            "jobs": [
                              {
                                "name": "unit-tests",
                                "runner": "ubuntu-22.04",
                                "timeout_minutes": 15,
                                "steps": [
                                  { "name": "checkout", "uses": "actions/checkout@v4" },
                                  { "name": "setup", "uses": "actions/setup-dotnet@v4", "with": { "dotnet-version": "9.0" } },
                                  { "name": "restore", "run": "dotnet restore" },
                                  { "name": "test", "run": "dotnet test --no-restore" }
                                ]
                              }
                            ]
                          },
                          {
                            "name": "publish",
                            "depends_on": ["test"],
                            "jobs": [
                              {
                                "name": "docker-push",
                                "runner": "ubuntu-22.04",
                                "steps": [
                                  { "name": "login", "run": "echo $TOKEN | docker login -u $USER --password-stdin" },
                                  { "name": "build", "run": "docker build -f docker/Dockerfile.api -t myapp:$SHA ." },
                                  { "name": "push", "run": "docker push myapp:$SHA" }
                                ]
                              }
                            ]
                          }
                        ]
                      }
                    }
                    """
            }
        ]
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
        SuggestionWeight = 80,
        Examples =
        [
            new()
            {
                Label = "Sales orders",
                Input = """
                    [
                      { "order_id": "ORD-2026-0001", "date": "2026-01-15", "customer": "Acme Corp", "product": "Widget Pro", "sku": "WGT-PRO-12", "qty": 12, "unit_price": 49.99, "discount": 0.05, "total": 569.89, "status": "shipped", "region": "EMEA" },
                      { "order_id": "ORD-2026-0002", "date": "2026-01-17", "customer": "Globex Ltd", "product": "Widget Lite", "sku": "WGT-LIT-06", "qty": 5, "unit_price": 19.99, "discount": 0, "total": 99.95, "status": "pending", "region": "APAC" },
                      { "order_id": "ORD-2026-0003", "date": "2026-01-18", "customer": "Initech", "product": "Widget Max", "sku": "WGT-MAX-24", "qty": 3, "unit_price": 129.99, "discount": 0.1, "total": 350.97, "status": "processing", "region": "AMER" },
                      { "order_id": "ORD-2026-0004", "date": "2026-01-20", "customer": "Umbrella Inc", "product": "Widget Pro", "sku": "WGT-PRO-12", "qty": 20, "unit_price": 49.99, "discount": 0.15, "total": 849.83, "status": "shipped", "region": "EMEA" },
                      { "order_id": "ORD-2026-0005", "date": "2026-01-22", "customer": "Stark Industries", "product": "Widget Max", "sku": "WGT-MAX-24", "qty": 8, "unit_price": 129.99, "discount": 0.2, "total": 831.94, "status": "delivered", "region": "AMER" },
                      { "order_id": "ORD-2026-0006", "date": "2026-01-25", "customer": "Wayne Enterprises", "product": "Widget Lite", "sku": "WGT-LIT-06", "qty": 50, "unit_price": 19.99, "discount": 0.2, "total": 799.60, "status": "shipped", "region": "AMER" }
                    ]
                    """
            },
            new()
            {
                Label = "Telemetry events",
                Input = """
                    [
                      { "event_id": "EVT-00001", "timestamp": "2026-05-01T08:12:03Z", "user_id": "USR-4521", "session": "SES-9901", "event": "page_view", "path": "/dashboard", "duration_ms": 342, "country": "DE", "device": "desktop", "ab_variant": "B" },
                      { "event_id": "EVT-00002", "timestamp": "2026-05-01T08:12:45Z", "user_id": "USR-4521", "session": "SES-9901", "event": "button_click", "path": "/dashboard", "duration_ms": 0, "country": "DE", "device": "desktop", "ab_variant": "B" },
                      { "event_id": "EVT-00003", "timestamp": "2026-05-01T08:13:10Z", "user_id": "USR-7734", "session": "SES-1120", "event": "page_view", "path": "/tools/json-to-yaml", "duration_ms": 520, "country": "US", "device": "mobile", "ab_variant": "A" },
                      { "event_id": "EVT-00004", "timestamp": "2026-05-01T08:13:44Z", "user_id": "USR-7734", "session": "SES-1120", "event": "conversion", "path": "/tools/json-to-yaml", "duration_ms": 1240, "country": "US", "device": "mobile", "ab_variant": "A" },
                      { "event_id": "EVT-00005", "timestamp": "2026-05-01T08:14:01Z", "user_id": "USR-0012", "session": "SES-3307", "event": "page_view", "path": "/", "duration_ms": 211, "country": "JP", "device": "tablet", "ab_variant": "A" }
                    ]
                    """
            }
        ]
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
        SuggestionWeight = 70,
        Examples =
        [
            new()
            {
                Label = "App config",
                Input = """
                    {
                      "package": {
                        "name": "my-service",
                        "version": "1.3.0",
                        "authors": ["alice@example.com", "bob@example.com"]
                      },
                      "server": {
                        "host": "127.0.0.1",
                        "port": 8080,
                        "workers": 4,
                        "tls_enabled": false
                      },
                      "database": {
                        "url": "postgres://localhost:5432/mydb",
                        "max_connections": 25,
                        "timeout_secs": 30,
                        "ssl_mode": "prefer"
                      },
                      "cache": {
                        "backend": "redis",
                        "url": "redis://localhost:6379",
                        "ttl_secs": 600,
                        "max_entries": 10000
                      },
                      "features": {
                        "dark_mode": true,
                        "beta_ui": false,
                        "analytics": true,
                        "rate_limiting": true
                      },
                      "logging": {
                        "level": "info",
                        "format": "json",
                        "file": "/var/log/my-service.log"
                      }
                    }
                    """
            },
            new()
            {
                Label = "Build config",
                Input = """
                    {
                      "workspace": {
                        "name": "ecommerce-platform",
                        "version": "0.9.0",
                        "members": ["api", "worker", "shared"]
                      },
                      "profile": {
                        "release": {
                          "opt_level": 3,
                          "lto": true,
                          "codegen_units": 1,
                          "strip": "symbols",
                          "panic": "abort"
                        },
                        "dev": {
                          "opt_level": 0,
                          "debug": true,
                          "incremental": true
                        }
                      },
                      "env": {
                        "RUST_LOG": "info",
                        "DATABASE_URL": "postgres://localhost/ecommerce",
                        "REDIS_URL": "redis://localhost:6379"
                      },
                      "test": {
                        "threads": 4,
                        "timeout_secs": 120,
                        "fail_fast": false
                      }
                    }
                    """
            }
        ]
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
        SuggestionWeight = 85,
        Examples =
        [
            new()
            {
                Label = "Kubernetes manifest",
                Input = """
                    {
                      "apiVersion": "apps/v1",
                      "kind": "Deployment",
                      "metadata": {
                        "name": "api-gateway",
                        "namespace": "production",
                        "labels": { "app": "api-gateway", "version": "2.4.1", "tier": "backend" }
                      },
                      "spec": {
                        "replicas": 3,
                        "selector": { "matchLabels": { "app": "api-gateway" } },
                        "template": {
                          "metadata": { "labels": { "app": "api-gateway", "version": "2.4.1" } },
                          "spec": {
                            "containers": [
                              {
                                "name": "api-gateway",
                                "image": "registry.io/api-gateway:2.4.1",
                                "ports": [{ "containerPort": 8080, "protocol": "TCP" }],
                                "env": [
                                  { "name": "ENV", "value": "production" },
                                  { "name": "DB_URL", "valueFrom": { "secretKeyRef": { "name": "db-secret", "key": "url" } } }
                                ],
                                "resources": {
                                  "requests": { "cpu": "250m", "memory": "256Mi" },
                                  "limits": { "cpu": "1000m", "memory": "512Mi" }
                                },
                                "livenessProbe": {
                                  "httpGet": { "path": "/health/live", "port": 8080 },
                                  "initialDelaySeconds": 10,
                                  "periodSeconds": 15
                                }
                              }
                            ]
                          }
                        }
                      }
                    }
                    """
            },
            new()
            {
                Label = "YAML service config",
                Input = """
                    service:
                      name: payment-processor
                      version: "1.0.0"
                      replicas: 3

                    database:
                      host: db.internal
                      port: 5432
                      name: payments
                      pool:
                        min: 2
                        max: 10
                        idle_timeout: 30s

                    queues:
                      - name: payment.created
                        workers: 5
                        retry:
                          max_attempts: 3
                          backoff: exponential
                          initial_delay: 1s
                      - name: payment.failed
                        workers: 2
                        retry:
                          max_attempts: 1
                          backoff: none

                    logging:
                      level: info
                      format: json
                      fields:
                        service: payment-processor
                        env: production
                    """
            }
        ]
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
        SuggestionWeight = 88,
        Examples =
        [
            new()
            {
                Label = "Sales pipeline",
                Input = """
                    deal_id,created_at,owner,company,country,stage,arr_usd,probability,close_date,source,notes
                    DEAL-001,2026-01-05,Alice Chen,Acme Corp,US,Proposal,48000,70,2026-02-28,Inbound,Champion is VP Engineering
                    DEAL-002,2026-01-08,Bob Martinez,Globex Ltd,UK,Negotiation,120000,85,2026-02-15,Outbound,Procurement involved — needs legal review
                    DEAL-003,2026-01-10,Alice Chen,Initech,DE,Discovery,24000,30,2026-03-31,Referral,Early stage — budget not confirmed
                    DEAL-004,2026-01-12,Carol Singh,Umbrella Inc,FR,Proposal,72000,60,2026-02-28,Event,Competing with Vendor X
                    DEAL-005,2026-01-15,Bob Martinez,Stark Industries,US,Closed Won,200000,100,2026-01-15,Outbound,Multi-year deal — 3 years prepaid
                    DEAL-006,2026-01-18,Carol Singh,Wayne Enterprises,US,Qualification,36000,20,2026-04-30,Inbound,New logo — high-value target
                    DEAL-007,2026-01-20,Alice Chen,Oscorp,JP,Negotiation,90000,75,2026-02-28,Partner,Partner sourced — split commission
                    DEAL-008,2026-01-22,Bob Martinez,Cyberdyne,US,Closed Lost,0,0,2026-01-22,Outbound,Lost to competitor on pricing
                    """
            },
            new()
            {
                Label = "Server metrics",
                Input = """
                    timestamp,host,cpu_pct,mem_pct,disk_read_mb,disk_write_mb,net_in_mb,net_out_mb,http_rps,p50_ms,p95_ms,p99_ms,errors,status
                    2026-05-01T08:00:00Z,web-01,14.2,62.3,0.8,1.2,45.1,32.4,1240,12,48,120,0,healthy
                    2026-05-01T08:00:00Z,web-02,18.7,58.9,0.6,0.9,41.3,28.7,1180,14,52,134,2,healthy
                    2026-05-01T08:05:00Z,web-01,22.4,64.1,1.1,1.4,52.8,38.2,1380,13,50,128,0,healthy
                    2026-05-01T08:05:00Z,web-02,31.2,61.7,0.9,1.3,48.5,35.1,1290,15,58,149,5,healthy
                    2026-05-01T08:10:00Z,web-01,67.8,71.4,2.3,2.8,98.2,71.4,2140,24,89,210,12,degraded
                    2026-05-01T08:10:00Z,web-02,72.1,69.8,2.1,2.5,94.7,68.9,2080,28,102,240,18,degraded
                    2026-05-01T08:15:00Z,web-01,45.3,68.2,1.5,1.9,65.4,47.8,1620,18,65,162,3,healthy
                    2026-05-01T08:15:00Z,web-02,41.9,66.5,1.4,1.7,62.1,45.3,1540,17,61,155,1,healthy
                    """
            }
        ]
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
