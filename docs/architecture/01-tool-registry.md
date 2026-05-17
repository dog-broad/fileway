# Tool Registry

The tool registry is the single source of truth for every tool in the app. Every route, detection suggestion, SEO tag, nav item, and /tools page card is derived from it. No tool exists without a ToolDefinition record.

---

## FileFormat Record

`Fileway.Shared/Formats/FileFormat.cs`

| Field | Type | Notes |
|---|---|---|
| `Id` | `string` | Lowercase, stable, never changes. Used in URLs and logs. |
| `DisplayName` | `string` | Shown in UI chips |
| `MimeTypes` | `string[]` | First entry = canonical Content-Type for output |
| `Extensions` | `string[]` | Lowercase, no dot. First = canonical for output filenames. |
| `MagicBytes` | `MagicSignature[]` | See detection doc. Multiple signatures for ambiguous formats. |
| `FormatCategory` | `FormatCategory` | Document \| Image \| Data \| Archive |
| `CanBeDetected` | `bool` | False for text-ambiguous formats (JSON, YAML, CSV etc.) |
| `DetectionHints` | `string[]?` | Structural patterns for text heuristic pass |
| `MaxFileSizeBytes` | `long` | Format-level ceiling. Tool-level limits can only be lower. |
| `IsTextBased` | `bool` | True for JSON, YAML, CSV, MD, TXT, HTML, SVG |
| `PreviewKind` | `PreviewKind` | Drives preview component selection |

### MagicSignature Record

| Field | Type | Notes |
|---|---|---|
| `Offset` | `int` | Byte offset into file where signature begins |
| `Bytes` | `byte[]` | Expected bytes at that offset |
| `Mask` | `byte[]?` | Applied via AND before comparison. 0x00 = wildcard. Null = no mask. |

---

## ToolDefinition Record

`Fileway.Shared/Tools/ToolDefinition.cs` — sealed record, immutable, created at startup.

### Core Identity

| Field | Type | Notes |
|---|---|---|
| `Slug` | `string` | URL-safe kebab-case. `/tools/{slug}`. Never change after first publish. |
| `DisplayName` | `string` | Tool cards, page titles |
| `Description` | `string` | /tools cards, SEO meta. Max 160 chars. |
| `ShortDescription` | `string` | Chips, mobile nav. Max 24 chars. |
| `Kind` | `ToolKind` | Conversion \| Manipulation |
| `Category` | `ToolCategory` | Document \| Image \| Data \| Archive |
| `Tags` | `string[]` | Powers /tools search box |

### Format Contract

| Field | Type | Notes |
|---|---|---|
| `AcceptedFormats` | `FileFormat[]` | Detection engine queries this to build suggestion lists |
| `OutputFormats` | `FileFormat[]` | For conversion tools user picks one. Manipulation = same as AcceptedFormats. |
| `DefaultOutputFormat` | `FileFormat?` | Pre-selected when arriving via direct URL |
| `AcceptsMultipleFiles` | `bool` | True for merge-pdf, images-to-pdf → activates MultiFileDropZone |
| `RequiresFileInput` | `bool` | False for inline data tools (JSON↔YAML) |

### Processing Configuration

| Field | Type | Notes |
|---|---|---|
| `ProcessorKind` | `ProcessorKind` | WasmOnly \| ApiOnly \| WasmPreferred |
| `WasmSizeThresholdBytes` | `long?` | WasmPreferred only. Files above → routed to API. |
| `JobTier` | `JobTier` | Synchronous \| Async |
| `ProcessorType` | `Type?` | Concrete API processor class. Null on WASM-only tools and on WASM side. |
| `ProgressStages` | `string[]` | Must exactly match what the processor emits. Verified by ProcessorSanityCheck. |
| `TimeoutSeconds` | `int` | Per-tool override. Default 60. |

### Limits

| Field | Type | Notes |
|---|---|---|
| `MaxInputSizeBytes` | `long` | Enforced at API boundary and processor entry |
| `MaxInputFileCount` | `int` | Multi-file tools only |
| `FreemiumLimitOverrides` | `ToolLimits?` | Null = no freemium differentiation |

### UX and Presentation

| Field | Type | Notes |
|---|---|---|
| `InputPreviewKind` | `PreviewKind` | How to preview input file before conversion |
| `OutputPreviewKind` | `PreviewKind` | How to preview output after conversion |
| `UiHints` | `UiHints` | [Flags] enum — drives conditional sub-component rendering |
| `IsNew` | `bool` | Shows "New" badge on tool cards |
| `IsPopular` | `bool` | Shows "Popular" badge — manually curated |
| `SortOrder` | `int` | Display order within category on /tools page |

### SEO

| Field | Type | Notes |
|---|---|---|
| `SeoTitle` | `string` | `<title>` and og:title. Max 60 chars. Format: `{Action} — Fileway` |
| `SeoDescription` | `string` | Meta description and og:description. Max 160 chars. |
| `SeoKeywords` | `string[]` | Internal /tools search. Not used in deprecated meta keywords tag. |
| `CanonicalPath` | `string` | Computed: `/tools/{Slug}`. Not stored. |

### Suggestion Engine

| Field | Type | Notes |
|---|---|---|
| `RelatedSlugs` | `string[]` | "Also try" panel after conversion. Order matters. |
| `SuggestionWeight` | `int` | Detection-driven suggestions. Higher = shown first. |

### Alias Slugs

| Field | Type | Notes |
|---|---|---|
| `SlugAliases` | `ToolSlugAlias[]` | Not required. Default is empty. Reverse-direction URLs for bidirectional tools. |

**`ToolSlugAlias` record** (`Fileway.Shared/Tools/ToolSlugAlias.cs`):

| Field | Type | Notes |
|---|---|---|
| `Slug` | `string` | The alias URL: `yaml-to-json`, `csv-to-json`, `toml-to-json`. Stable — never change after first publish. |
| `PresetOutputFormat` | `FileFormat` | Pre-selected output format when the page loads via this slug. |
| `DisplayName` | `string` | Shown as page title when loaded via alias: `YAML → JSON`. |
| `Description` | `string` | Tool description copy for the alias direction. |
| `SeoTitle` | `string` | `<title>` for the alias URL. |
| `SeoDescription` | `string` | Meta description for the alias URL. |
| `Examples` | `ToolExample[]` | Examples appropriate for this direction (e.g. YAML input when on `yaml-to-json`). |

**How it works:**
- `ToolRegistry.GetBySlug(slug)` resolves both canonical slugs and alias slugs, returning the canonical `ToolDefinition`.
- `ToolRegistry.GetAlias(slug)` returns the `ToolSlugAlias` if the slug is an alias; null if canonical.
- `ToolPage` calls both. If an alias is active, it applies `PresetOutputFormat`, `DisplayName`, `Description`, and `Examples` from the alias. The output format selector is still shown so the user can switch directions manually.
- `GetSitemapEntries()` includes alias slugs as separate entries with their own SEO fields.
- `ValidateSlug` accepts both canonical and alias slugs.

**Which tools have aliases (M1):**

| Canonical slug | Alias slug | Preset output |
|---|---|---|
| `json-to-yaml` | `yaml-to-json` | JSON |
| `json-to-csv` | `csv-to-json` | JSON |
| `json-to-toml` | `toml-to-json` | JSON |

**Rules:**
- Only bidirectional tools get aliases. One-way tools (`csv-to-xlsx`) do not.
- Each alias must supply its own `Examples` covering the reverse direction's typical input format.
- Alias slugs follow the same naming convention: `{source}-to-{target}`.
- Alias slugs are stable URLs — treat them the same as canonical slugs once published.

### Examples

| Field | Type | Notes |
|---|---|---|
| `Examples` | `ToolExample[]` | Not required. Default is empty. Only relevant for `RequiresFileInput = false` tools. |

**`ToolExample` record** (`Fileway.Shared/Tools/ToolExample.cs`):

| Field | Type | Notes |
|---|---|---|
| `Label` | `string` | Short name shown on the chip: "Service config", "Sales orders". Max ~20 chars. |
| `Input` | `string` | The full example text. Should be realistic — non-trivial nesting and field variety. |

Examples appear as chips in the input pane header. Clicking one loads the content into the editor and triggers conversion immediately, giving users a live before/after demo without typing anything.

**Rules:**
- Examples must be valid input for the tool — they will auto-convert on click.
- For bidirectional tools (JSON↔YAML, JSON↔CSV), supply examples in the primary input format (the `DefaultOutputFormat`'s counterpart).
- For `json-to-csv`, examples **must** be flat JSON arrays — nested objects will hit `JsonNotCsvCompatible`.
- Target 2 examples per tool. One domain (configs, ops) + one data (records, events) is the pattern.
- Every new `RequiresFileInput = false` tool added to the registry must include at least one example.

---

## Enums

### ToolKind
- `Conversion` — Format A → Format B
- `Manipulation` — Format A → Format A (modified)

### ToolCategory
- `Document`, `Image`, `Data`, `Archive`

### ProcessorKind
- `WasmOnly` — always runs in browser
- `ApiOnly` — always runs on server
- `WasmPreferred` — WASM if CanHandleSize, else API

### JobTier
- `Synchronous` — result in HTTP response body
- `Async` — JobId + SSE stream

### PreviewKind
- `None`, `FirstPageRender`, `SideBySideImage`, `SyntaxHighlight`, `PageThumbnails`, `InlineEditor`

### UiHints [Flags]
- `None`, `ShowQualitySlider`, `ShowPageSelector`, `ShowDimensionInputs`, `ShowOrderableList`, `ShowSplitControls`

### FormatCategory
- `Document`, `Image`, `Data`, `Archive`

---

## ToolRegistry

`Fileway.Shared/Registry/ToolRegistry.cs` — singleton, built at startup, immutable.

Lives in Fileway.Shared. Identical on WASM and API. ProcessorType is null on WASM side (populated via server-side second pass).

### Query API — consumers use only these methods, never the raw list

| Method | Returns | Used by |
|---|---|---|
| `GetBySlug(string slug)` | `ToolDefinition?` | Router — resolves route param. Null → 404. |
| `GetAll()` | `IReadOnlyList<ToolDefinition>` | /tools discovery page |
| `GetByCategory(ToolCategory)` | `IReadOnlyList<ToolDefinition>` | Nav category pages |
| `GetSuggestionsFor(FileFormat, int limit)` | `IReadOnlyList<ToolDefinition>` | Detection engine on file drop. Ordered by SuggestionWeight. |
| `GetRelated(string slug, int limit)` | `IReadOnlyList<ToolDefinition>` | "Also try" panel |
| `Search(string query)` | `IReadOnlyList<ToolDefinition>` | /tools search box |
| `GetAccepting(FileFormat)` | `IReadOnlyList<ToolDefinition>` | All tools accepting a given format |
| `GetSitemapEntries()` | `IReadOnlyList<SitemapEntry>` | StaticGen SEO tool |
| `ValidateSlug(string slug)` | `bool` | API endpoint validation before touching files |

### Definition Files

```
Fileway.Shared/Tools/Definitions/
  DocumentTools.cs    — PDF manipulation + document conversion tools
  ImageTools.cs       — image manipulation + image conversion tools
  DataTools.cs        — data format tools
```

---

## Adding a New Tool — Required Steps

1. Add ToolDefinition to correct Definitions/ file
2. Verify all FileFormats referenced exist in FileFormats.cs
3. Set ProcessorType = typeof(YourProcessor) — null only for WasmOnly
4. ProgressStages must exactly match what the processor will emit (verified at startup)
5. Add RelatedSlugs — and update related tools to include this slug back
6. Create processor (see `04-processors.md`)
7. Run ProcessorSanityCheck by starting the app — crashes loudly on misconfiguration

**What is automatic (no extra work):** /tools card, route /tools/{slug}, detection suggestion, SEO meta, sitemap entry, API slug validation, nav category grouping.
