# Fileway — Build Progress

---

## M1 — Shell + Data Tools

**Done when:** All 5 data tools (json-to-yaml, json-to-csv, json-to-toml, validate, csv-to-xlsx) work end-to-end in WASM; API boots and `ProcessorSanityCheck` passes; format detection identifies all M1 formats; CI passes.

---

### Shared Types

> Read `docs/architecture/01-tool-registry.md` before starting this section.

- [x] Add enums: `FormatCategory`, `ToolKind`, `ToolCategory`, `ProcessorKind`, `JobTier`, `PreviewKind`, `UiHints` — one enum per file in `Fileway.Shared/Formats/` and `Fileway.Shared/Tools/`
- [x] Add `MagicSignature` record to `Fileway.Shared/Formats/MagicSignature.cs`
- [x] Add `FileFormat` record to `Fileway.Shared/Formats/FileFormat.cs`
- [x] Add M1 `FileFormat` instances to `Fileway.Shared/Formats/FileFormats.cs`: json, yaml, csv, toml, xlsx, txt, md
  > JSON/YAML/CSV/TOML/MD have `CanBeDetected = false` and use `DetectionHints` only — no magic bytes (ref: `06-detection.md`)
- [x] Add `ToolDefinition` record to `Fileway.Shared/Tools/ToolDefinition.cs`
- [x] Add `ToolLimits` record to `Fileway.Shared/Tools/ToolLimits.cs`
- [x] Create `Fileway.Shared/Tools/Definitions/DataTools.cs` with all 5 data tool `ToolDefinition` records
  > `ProcessorType` is null for WasmOnly tools; set it to `typeof(CsvToXlsxProcessor)` only for the csv-to-xlsx API fallback path
- [x] Create `Fileway.Shared/Registry/ToolRegistry.cs` with all query methods (ref: `01-tool-registry.md`)
  > ToolRegistry is a singleton built once at startup from a fixed list — it does not scan assemblies
- [x] Add all error code string constants to `Fileway.Shared/Errors/ErrorCodes.cs` — every error code from the taxonomy (ref: `03-api-surface.md`)
- [x] Add exception classes to `Fileway.Shared/Errors/`: `ProcessorValidationException`, `ProcessorDomainException`, `ProcessorUnexpectedException`
- [x] Add `IApiProcessor` and `IWasmProcessor` interfaces to `Fileway.Shared/Processors/`
- [x] Add `ProcessorContext`, `InputFile`, `ProcessorResult`, `ProcessorProgressEvent` records to `Fileway.Shared/Processors/`
- [x] Add SSE/job wire types: `JobEvent` + `JobEventType` to `Fileway.Shared/Jobs/`; `FailedPayload` to `Jobs/Payloads/`; `SyncJobResult` + `AsyncJobAccepted` to `Fileway.Shared/Api/`; `SitemapEntry` to `Fileway.Shared/Registry/`
- [x] Add `ToolSummary` record to `Fileway.Shared/Tools/ToolSummary.cs` — the slimmed-down API response type for `GET /api/v1/tools`; omits `ProcessorType`, `ProgressStages`, `UiHints`, and internal fields (ref: `03-api-surface.md`)
- [x] Add `JobOptions` record to `Fileway.Shared/Api/JobOptions.cs` — deserialised from the `options` multipart part; fields: `ToolSlug`, `OutputFormat`, `InlineContent`, `ToolOptions` as `JsonElement` (ref: `03-api-surface.md`)

---

### Format Detection

> Read `docs/architecture/06-detection.md` before starting this section.

- [x] Add `DetectionConfidence` enum and `IFormatDetector` interface to `Fileway.Shared/Detection/`
- [x] Implement `FormatDetector.cs` in `Fileway.Shared/Detection/` — three-pass pipeline: magic bytes, ZIP disambiguation, text heuristics
  > Magic byte comparison must apply `MagicSignature.Mask` via AND before comparing; null mask means compare directly
- [x] Implement Pass 2 ZIP disambiguation for XLSX in M1 — scans ZIP local file entry headers (at start of file) for `[Content_Types].xml` + `xl/`; central directory scanning deferred until M3 when DOCX/PPTX are added
- [x] Register `IFormatDetector` / `FormatDetector` as a singleton in both API and WASM `Program.cs`
- [x] Ensure `FormatDetector` has zero I/O or platform dependencies — it must compile to WASM unchanged

---

### API Infrastructure

> Read `docs/architecture/03-api-surface.md`, `docs/architecture/10-observability.md`, and `docs/architecture/11-rate-limiting.md` before starting this section.

- [ ] Configure Serilog with compact JSON formatter to stdout in `Fileway.Api/Program.cs`; `Information` in production, `Debug` in development via `appsettings.json`
- [ ] Add `ApiOptions` strongly-typed config class to `Fileway.Api/Config/` covering all concurrency and timeout values from `02-job-model.md`
- [ ] Add stub config classes: `LibreOfficeOptions`, `StorageOptions` — needed at startup even if unused until M4
- [ ] Implement session token middleware: validate `X-Session-Token` as UUID → 400 if missing/invalid; compute `ipHash = SHA-256(rawIp + dailySalt)` for rate limit keying
  > Log only the first 8 chars of the token as `sessionPrefix` — never the full token (ref: `10-observability.md`)
- [ ] Configure security headers middleware (CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy)
- [ ] Configure CORS — `fileway.io` and `localhost` only, no wildcard
- [ ] Add `ITierResolver` interface and `AlwaysFreeTierResolver` stub to `Fileway.Api/Infrastructure/`
- [ ] Configure `Microsoft.AspNetCore.RateLimiting` with session-token policy (20 req/60s) and IP-hash policy (60 req/60s), both calling `ITierResolver` (ref: `11-rate-limiting.md`)
- [ ] Configure global 200MB request size limit via `KestrelServerOptions` or `IFormOptions`
- [ ] Configure `ProblemDetails` exception handler middleware — catches all unhandled exceptions → 500; never leaks stack traces
- [ ] Register `ToolRegistry` singleton in API DI
- [ ] Add health endpoints: `GET /health/live` and `GET /health/ready` via `app.MapHealthChecks()`
- [ ] **Checkpoint** — `dotnet run --project src/Fileway.Api` starts without exception; `curl http://localhost:5000/health/live` and `/health/ready` both return 200; startup logs contain no error-level events

---

### API Job Model + Storage

> Read `docs/architecture/02-job-model.md` before starting this section.

- [ ] Add `JobStatus` enum and `JobRecord.cs` to `Fileway.Api/Jobs/`
- [ ] Add `IJobStore` interface and implement `InMemoryJobStore.cs` using `ConcurrentDictionary<Guid, JobRecord>`
- [ ] Implement `JobSweepService` as `IHostedService` — every 5 min, delete completed/failed jobs older than 10 min; scan `/tmp/fileway/` for orphaned temp dirs older than 15 min
- [ ] Implement `JobQueueManager` — enforces `MaxConcurrentJobs` (server-wide) and `MaxJobsPerSession`; returns 429 `ConcurrentJobLimit` or 503 `QueueFull`
- [ ] Add `IStorageService` interface to `Fileway.Api/Infrastructure/`
- [ ] Implement `LocalFileStorageService` — writes output to a configured temp dir, returns a local path; used in development and CI when R2 credentials are absent
- [ ] Register `IStorageService` → `LocalFileStorageService` for now; M4 adds the R2 implementation behind a config flag
- [ ] Implement `JobDispatcher` in `Fileway.Api/Jobs/` — ValidateOptions → check limits → create `JobRecord` → return JobId → background `Task` runs full lifecycle and emits SSE events via `Channel<JobEvent>` (ref: `04-processors.md`)
  > The route handler calls `DispatchAsync()` and returns 202 immediately — the entire job lifecycle runs in a background `Task.Run`
- [ ] **Checkpoint** — API restarts cleanly; startup logs contain no `NullReferenceException` or missing-service errors; `JobSweepService` start is visible in the log output

---

### API Endpoints

- [ ] Implement `POST /api/v1/jobs` in `Fileway.Api/Endpoints/JobEndpoints.cs` following the exact validation order from `03-api-surface.md`
  > Read the `options` multipart part before touching any file bytes — part order in multipart is required by the spec
- [ ] Implement `GET /api/v1/jobs/{jobId}/progress` SSE endpoint — reads `Channel<JobEvent>.Reader`, sends `: ping\n\n` keepalive every 15s, handles `Last-Event-ID` reconnect replay, returns 403/404 where appropriate
- [ ] Implement `GET /api/v1/tools` — returns `ToolSummary[]`, supports `category` and `q` query params, sets `Cache-Control: public, max-age=3600` + `ETag`
- [ ] Implement `GET /api/v1/tools/{slug}` — resolves via `ToolRegistry.GetBySlug()`, returns 404 if null
- [ ] Implement `POST /api/v1/detect` — calls `IFormatDetector.Detect()` on received header bytes; returns detected format, confidence, and suggested tool slugs
- [ ] Add `AuditLogService` to `Fileway.Api/Logging/` — writes conversion audit events with no filenames, no file content, no raw IPs (ref: `10-observability.md`)
- [ ] **Checkpoint** — `curl http://localhost:5000/api/v1/tools` returns a JSON array with exactly 5 objects and correct slugs; `curl -X POST http://localhost:5000/api/v1/jobs` with no body returns 400 with a JSON `ProblemDetails` response body — not 500 and not plain text

---

### API Processor Infrastructure

> Read `docs/architecture/04-processors.md` before starting this section.

- [ ] Create `Fileway.Api/Infrastructure/ProcessorExtensions.cs` — explicit `services.AddTransient<T>()` per processor, no scanning
- [ ] Implement `ProcessorSanityCheck` as `IHostedService` — crashes startup with `InvalidOperationException` on any misconfiguration; runs before the server begins accepting requests (ref: `04-processors.md`)
  > Intentionally fatal — do not add try/catch; loud failure at startup prevents silent misconfiguration in production
- [ ] Add API-side `ToolRegistry` second-pass initialisation at startup — after DI is configured, iterate all non-WasmOnly `ToolDefinition` records and populate their `ProcessorType` field from the DI container; `ProcessorType` is null in the shared definition and only set on the API side (ref: `04-processors.md`)
- [ ] **Checkpoint** — API startup logs show `SanityCheckPassed` event; if `SanityCheckFailed` appears instead, stop and fix before proceeding — the server will not accept requests

---

### Data Format Processors (API)

- [ ] Implement `CsvToXlsxProcessor` in `Fileway.Api/Processors/DataFormats/` using `CsvHelper` + `ClosedXML` — this is the API fallback for the `WasmPreferred` csv-to-xlsx tool
- [ ] Register `CsvToXlsxProcessor` in `ProcessorExtensions.cs`
- [ ] **Checkpoint** — POST a small CSV with `X-Session-Token` header and `toolSlug: "csv-to-xlsx"` to `/api/v1/jobs` as multipart; response is 200 with `deliveryKind: "Inline"` and non-empty `inlineContent`

---

### Client (WASM) Infrastructure

> Read `docs/architecture/02-job-model.md` and `docs/architecture/04-processors.md` before starting this section.

- [ ] Implement `SseClient.js` in `Fileway.Client/wwwroot/js/` — wraps browser `EventSource`, forwards messages via `DotNet.invokeMethodAsync`, exposes `open(url)` and `close()` methods; no business logic
  > Blazor WASM has no native `EventSource` — this JS bridge is the only way to consume SSE (ref: `02-job-model.md`)
- [ ] Implement `SseClient.cs` in `Fileway.Client/Interop/` — wraps JS interop, deserialises JSON strings to typed `JobEvent` records, exposes `IAsyncEnumerable<JobEvent>`, registered as Scoped DI
- [ ] Implement `ApiJobClient.cs` in `Fileway.Client/Services/` — builds multipart POST to `/api/v1/jobs`, handles sync (200) and async (202) responses, exposes `Retry-After` header value for error countdown
- [ ] Implement `ProcessorRouter` in `Fileway.Client/Services/` — routes WasmOnly/ApiOnly/WasmPreferred; falls back to API on `ProcessorUnexpectedException` from WASM with "Switching to server…" indicator (ref: `04-processors.md`)
- [ ] Implement `DetectionService.cs` in `Fileway.Client/Services/` — wraps `IFormatDetector`, called on file drop, surfaces confidence to DropZone; falls back to `POST /api/v1/detect` when WASM detection returns Low confidence or null
  > The server `/api/v1/detect` endpoint is the authoritative fallback — even if WASM says Unknown, the server can identify via the same magic byte logic (ref: `06-detection.md`)
- [ ] Add session token initialisation to WASM `Program.cs` — generate UUID v4 if absent from `sessionStorage`, persist there; `ApiJobClient` reads it and adds it as `X-Session-Token` on every request
  > Use `sessionStorage`, not `localStorage` — tab-scoped by design; token disappears on tab close (ref: `03-api-surface.md`)
- [ ] Implement `ToolStateService` in `Fileway.Client/Services/` as a Scoped DI service — tracks per-tab state machine (Idle → Submitting → Processing → Completed | Failed); `SseClient.cs` calls into it on each event; `ProgressPanel` and `ErrorPanel` read from it (ref: `07-error-model.md`)
- [ ] Create `Fileway.Client/Infrastructure/WasmProcessorExtensions.cs` — per-processor `AddTransient<>` + slug→`Type` dictionary entry
- [ ] Implement `ThemeInterop.js` in `Fileway.Client/wwwroot/js/` — reads/writes `[data-theme]` on `<html>`, persists preference to `localStorage`
  > Load this script *before* `blazor.webassembly.js` in `index.html` to prevent flash of unstyled content
- [ ] Implement `ThemeService.cs` in `Fileway.Client/Services/` — exposes `Toggle()`, delegates to `ThemeInterop.js` via JS interop
- [ ] **Checkpoint** — App loads in browser with no console errors; DevTools → Application → Session Storage shows a UUID under `sessionToken`; DevTools → Network shows `X-Session-Token` header present on any request to `/api/v1/tools`

---

### Data Format Processors (WASM)

- [ ] Implement `JsonYamlProcessor` in `Fileway.Client/Processors/DataFormats/` using `YamlDotNet` — bidirectional; direction from `context.InputFiles[0].DetectedFormat`
  > Call `await Task.Yield()` before heavy parse loops to yield control back to the Blazor render thread
- [ ] Implement `JsonCsvProcessor` in `Fileway.Client/Processors/DataFormats/` using `CsvHelper` — bidirectional
- [ ] Implement `JsonTomlProcessor` in `Fileway.Client/Processors/DataFormats/` using `Tomlyn` — bidirectional
- [ ] Implement `ValidateProcessor` in `Fileway.Client/Processors/DataFormats/` — validates JSON/YAML/CSV/TOML structure; throws `ProcessorDomainException` with the appropriate `ErrorCode` on malformed input
- [ ] Implement `CsvToXlsxProcessor` (WASM path) in `Fileway.Client/Processors/DataFormats/` using `ClosedXML`
  > Verify `ClosedXML` compiles to WASM before wiring up — known risk; if it does not, mark `CanHandleSize` always false to force the API path
- [ ] Register all WASM data processors in `WasmProcessorExtensions.cs`
- [ ] **Checkpoint** — Navigate to `/tools/json-to-yaml`; enter `{"key": "value"}`; output pane shows `key: value`; navigate to `/tools/validate`; enter `{invalid`; `ErrorPanel` shows the malformed-JSON copy from `ErrorMessages.cs`

---

### Blazor UI Shell

> Read `docs/architecture/09-ui-design.md` before starting this section.

- [ ] Add all CSS custom property tokens (colours, spacing, radius, typography) to `Fileway.Client/wwwroot/css/app.css` under both `[data-theme="dark"]` and `[data-theme="light"]` selectors
  > No hex values in any component `.razor.css` file — only `var(--token-name)`; full token list in `09-ui-design.md`
- [ ] Self-host Inter and JetBrains Mono fonts — add to `wwwroot/fonts/`, declare via `@font-face` in `app.css`; no Google Fonts CDN
- [ ] Add `App.razor` with Blazor Router and 404 fallback
- [ ] Add `MainLayout.razor` — CSS grid/flex layout using design tokens, includes `<NavBar>` and `<main>`
- [ ] Add `NavBar.razor` — logo, category nav links, `<ThemeToggle>` — mobile-first, fully responsive from 375px
- [ ] Add `ThemeToggle.razor` — calls `ThemeService.Toggle()` on click, reflects current theme state
- [ ] **Checkpoint** — App loads; inspect `<html>` in DevTools and confirm `data-theme` is set before Blazor hydrates (theme applies without flash); clicking ThemeToggle switches the attribute and a page refresh retains the choice

---

### Core UI Components

> Read `docs/architecture/09-ui-design.md` before starting this section. Every component has its own `.razor.css` scoped stylesheet.

- [ ] Add `DropZone.razor` — file drop target; calls `DetectionService` on drop; tap-to-browse fallback via `<input type="file">`; shows format name or "couldn't identify" text
  > Drag-and-drop must always have a keyboard/tap fallback — no hover-only interaction (WCAG 2.5.5)
- [ ] Add `ErrorMessages.cs` to `Fileway.Client/Components/Errors/` with all error code → (userMessage, suggestedAction, retryable) mappings (ref: `07-error-model.md`)
- [ ] Add `ErrorPanel.razor` — renders `userMessage` + `suggestedAction`; shows "Try again" button on retryable; shows `RelatedSlugs` chips on non-retryable; shows live countdown on `RateLimitExceeded` using `Retry-After` value
- [ ] Add `ProgressPanel.razor` — stage name, `stageIndex`/`stageTotalCount`, overall percent bar; wrap progress announcements in an ARIA live region
- [ ] Add `SyntaxHighlightPreview.razor` — syntax-highlighted output for JSON/YAML/CSV/TOML
- [ ] Add `InlineEditorPreview.razor` — split-pane editor for data tools where `RequiresFileInput = false`
- [ ] Add `PreviewPanel.razor` — reads `InputPreviewKind`/`OutputPreviewKind` from `ToolDefinition` and renders the matching preview sub-component; no per-tool conditional logic here
- [ ] Add `ToolOptionsPanel.razor` — reads `UiHints` flags and conditionally renders sub-components; M1 data tools require no sub-components
- [ ] Add `ToolCard.razor` — used on `/tools` listing page; shows `DisplayName`, `ShortDescription`, "New"/"Popular" badge if set
- [ ] Add `OutputFormatSelector.razor` — renders a format picker (dropdown or button group) from `ToolDefinition.OutputFormats`; pre-selects `DefaultOutputFormat`; hidden when there is only one output format
- [ ] Add `OutputPanel.razor` — shown when `ToolStateService` reaches Completed; triggers browser download for signed URL (`window.location.href`) or saves inline base64 via JS interop; shows copy-to-clipboard button for text outputs (JSON/YAML/CSV/TOML)
- [ ] **Checkpoint** — Drop a `.json` file on the DropZone: format chip shows "JSON"; drop an unrecognised file: "couldn't identify" text appears with no crash; manually trigger `ErrorPanel` with a known `errorCode` and confirm the copy matches `ErrorMessages.cs` exactly

---

### Tool Pages

- [ ] Add `/tools` listing page (`Fileway.Client/Pages/ToolsPage.razor`) — calls `ToolRegistry.GetAll()`, groups by category, renders `<ToolCard>` per tool; supports `?category=` and `?q=` query params
- [ ] Add generic tool page (`Fileway.Client/Pages/ToolPage.razor`) at route `/tools/{slug}` — resolves `ToolDefinition`, renders `<DropZone>` (or `<InlineEditorPreview>` when `RequiresFileInput = false`), `<OutputFormatSelector>`, `<ToolOptionsPanel>`, `<PreviewPanel>`, `<ProgressPanel>`, `<ErrorPanel>`, `<OutputPanel>`
  > The tool page never knows which preview or options sub-component is shown — everything is driven by `ToolDefinition.UiHints`, `InputPreviewKind`, `OutputPreviewKind`; `<OutputPanel>` is only visible when `ToolStateService.State == Completed`
- [ ] Add homepage (`/`) — tagline, search input linking to `/tools?q=`, category cards
- [ ] **Checkpoint** — `/tools` lists all 5 data tools grouped under Data with no missing cards; `/tools/json-to-yaml` renders with inline editor and performs an end-to-end conversion; `OutputPanel` appears on completion with a working copy trigger; `/tools/nonexistent` shows the 404 fallback page — no unhandled exception

---

### Tests — M1

> Read `docs/architecture/08-testing.md` before starting this section.

- [ ] Add shared test fixtures to both `tests/Fileway.Tests.Api/Fixtures/` and `tests/Fileway.Tests.Client/Fixtures/`: `TestProgressCollector`, `TestFileFactory`, `ProcessorContextBuilder`, `CorruptedFileFactory`, `EmbeddedTestFiles`
  > Fixtures are duplicated — there is no shared test project (ref: `08-testing.md`)
- [ ] Add embedded test resource files to both test projects as `EmbeddedResource` items: `minimal.pdf` (3 pages), `minimal.docx`, `sample.png` (100×100px), `valid.json`, `valid.yaml`, `sample.csv` — max 50KB each; these are what `EmbeddedTestFiles` fixture serves (ref: `08-testing.md`)
- [ ] Add `FormatDetector` unit tests — cover every magic byte signature, ZIP disambiguation (DOCX/XLSX/PPTX/ZIP), all text heuristic formats, unknown format returns null
- [ ] Add `ToolRegistry` unit tests — `GetBySlug`, `GetAll`, `GetByCategory`, `GetSuggestionsFor`, `Search`, `GetRelated`
- [ ] Add processor unit tests for each WASM data processor — minimum 6 tests per class (ref: `08-testing.md`); test bidirectional processors in both directions independently
- [ ] Add processor unit tests for `CsvToXlsxProcessor` (API path) — same 6-test minimum bar
- [ ] Add `ProcessorRouter` unit tests — verify all three `ProcessorKind` routing paths; verify WASM `ProcessorUnexpectedException` fallback to API
- [ ] Add API integration tests via `WebApplicationFactory` — POST `/api/v1/jobs` for each data tool, assert 200 + correct output format; bind `IStorageService` → `LocalFileStorageService`
- [ ] Add `ErrorPanel` bUnit component tests — assert correct `userMessage` renders for each code in `ErrorMessages.cs`
- [ ] **Checkpoint** — `dotnet test` exits 0 with zero failures; confirm no test is using `Thread.Sleep` (grep before marking done)

---

### CI

- [ ] Add `.github/workflows/ci.yml` — build + test on every push and every PR to main; unit test timeout 60s, integration test timeout 5 min; collect coverlet coverage as artifact; fail if build has warnings (`TreatWarningsAsErrors: true`)

---

## M2 — Image Tools

**Done when:** image-resize, image-rotate, compress-image, image-convert, and svg-convert all work end-to-end; before/after preview renders correctly; large images route to the API fallback path; CI passes.

---

### Shared Types — Image Formats

> Read `docs/architecture/01-tool-registry.md` and `docs/architecture/06-detection.md` before starting this section.

- [ ] Add image `FileFormat` records to `FileFormats.cs`: png, jpeg, webp, gif, bmp, tiff, ico, heic, svg
  > WEBP requires a mask on the 4-byte variable size field in the RIFF header — set `MagicSignature.Mask` accordingly (ref: `06-detection.md`)
- [ ] Create `Fileway.Shared/Tools/Definitions/ImageTools.cs` with `ToolDefinition` records for: image-resize (`WasmOnly`), image-rotate (`WasmOnly`), compress-image (`WasmPreferred`), image-convert (`WasmOnly`), svg-convert (`WasmPreferred`)
  > Set `ProcessorType` only for the two `WasmPreferred` tools; null for the three `WasmOnly` tools
- [ ] **Checkpoint** — `dotnet build` exits 0; drop a WEBP file on the DropZone and confirm format chip shows "WEBP" — a wrong or missing mask returns null detection, so this catches mask errors that a build cannot
  > HEIC detection (offset 4) is also worth checking here — drop a `.heic` file and confirm it is not misidentified as unknown

---

### WASM Image Processors

- [ ] Add `ImageSharpProcessor` base class to `Fileway.Client/Processors/Base/` — shared ImageSharp decode/encode helpers; call `await Task.Yield()` between processing steps
  > Verify `SixLabors.ImageSharp` version in `Directory.Packages.props` supports WASM compilation before writing any processor code
- [ ] Implement `ConvertImageProcessor` extending `ImageSharpProcessor` in `Fileway.Client/Processors/ImageManipulation/` — converts between png/jpeg/webp/gif/bmp/tiff
- [ ] Implement `CropResizeImageProcessor` extending `ImageSharpProcessor` — options: `targetWidth`, `targetHeight`, `maintainAspectRatio`; throw `ProcessorValidationException` on zero or negative dimensions
- [ ] Implement `RotateFlipImageProcessor` extending `ImageSharpProcessor` — options: `rotation` (0/90/180/270); throw `ProcessorValidationException` on unsupported value
- [ ] Implement `CompressImageProcessor` (WASM path) extending `ImageSharpProcessor` — options: `quality` (1–100); output format same as input; implements `CanHandleSize` using `WasmSizeThresholdBytes`
- [ ] Implement `SvgConvertProcessor` (WASM path) in `Fileway.Client/Processors/ImageManipulation/` using `Svg.Skia` — converts SVG to png/jpeg/webp; implements `CanHandleSize`
- [ ] Register all WASM image processors in `WasmProcessorExtensions.cs`
- [ ] **Checkpoint** — Navigate to `/tools/image-convert`; drop a PNG and select JPEG output; confirm a JPEG is downloaded; navigate to `/tools/image-resize`; set 100×100 with aspect lock; confirm output dimensions are exactly 100×100 (or constrained correctly)

---

### API Image Processors (WasmPreferred Fallback)

- [ ] Implement `CompressImageProcessor` (API path) in `Fileway.Api/Processors/ImageManipulation/` — same options and behaviour as the WASM path
- [ ] Implement `SvgConvertProcessor` (API path) in `Fileway.Api/Processors/ImageManipulation/` using `Svg.Skia`
- [ ] Register both in `ProcessorExtensions.cs`
- [ ] **Checkpoint** — POST a PNG larger than `WasmSizeThresholdBytes` for `compress-image`; DevTools Network tab shows the request routed to `/api/v1/jobs` (not handled in-browser); compressed output is returned with a smaller byte size

---

### Image UI Components

> Read `docs/architecture/09-ui-design.md` before starting this section.

- [ ] Add `SideBySideImagePreview.razor` — before/after comparison with a drag divider; keyboard `ArrowLeft`/`ArrowRight` as drag-divider alternative
- [ ] Add `DimensionInputs.razor` — width/height number inputs with aspect ratio lock toggle; rendered by `ToolOptionsPanel` when `UiHints.ShowDimensionInputs`
- [ ] Add `QualitySlider.razor` — range slider 1–100 with live value display and estimated output size comparison; rendered when `UiHints.ShowQualitySlider`
- [ ] Update `PreviewPanel.razor` to handle `PreviewKind.SideBySideImage` → renders `<SideBySideImagePreview>`
- [ ] **Checkpoint** — `compress-image` tool page shows `QualitySlider` and adjusting it updates the live value; `image-resize` shows `DimensionInputs` with aspect lock toggle; `SideBySideImagePreview` drag divider moves and responds to `ArrowLeft`/`ArrowRight` keys

---

### Tests — M2

- [ ] Add processor unit tests for all 5 WASM image processor classes — 6-test minimum bar each; include `CanHandleSize` tests for `CompressImageProcessor` and `SvgConvertProcessor`
- [ ] Add processor unit tests for `CompressImageProcessor` and `SvgConvertProcessor` API paths
- [ ] Add API integration tests for the two WasmPreferred tools hitting the API fallback path
- [ ] **Checkpoint** — `dotnet test` exits 0 with zero failures

---

## M3 — PdfPageEditor + PDF Manipulation

**Done when:** All 7 PDF manipulation tools work end-to-end; thumbnails stream progressively into PdfPageEditor; drag-drop reorder works on desktop; tap-to-reorder works on mobile; CI passes.

---

### Shared Types — PDF + Document Formats

> Read `docs/architecture/01-tool-registry.md` and `docs/architecture/06-detection.md` before starting this section.

- [ ] Add `FileFormat` records to `FileFormats.cs`: pdf, docx, zip (split-pdf output), rtf
  > DOCX detection requires ZIP magic bytes + Pass 2 disambiguation on `[Content_Types].xml` + `word/` entry (ref: `06-detection.md`)
- [ ] Update `FormatDetector` ZIP disambiguation logic to identify DOCX, XLSX, PPTX
- [ ] Create PDF manipulation `ToolDefinition` records in `Fileway.Shared/Tools/Definitions/DocumentTools.cs`: merge-pdf, split-pdf, reorder-pdf, remove-pdf-pages, rotate-pdf, watermark-pdf, protect-pdf
  > All 7 are `ApiOnly` + `JobTier.Async`; `AcceptsMultipleFiles = true` for merge-pdf only; `ProgressStages` must exactly match what each processor emits
- [ ] **Checkpoint** — Drop a `.docx` file on the DropZone: format chip shows "Word Document" (DOCX), not "ZIP" — confirms Pass 2 disambiguation is working; `dotnet build` exits 0

---

### PDF Rendering Infrastructure

> Read `docs/architecture/12-pdf-rendering.md` before starting this section.

- [ ] Add `IPdfRenderer` interface, `RenderedPage` record, and `PageThumbnail` record to `Fileway.Api/Infrastructure/` (ref: `12-pdf-rendering.md`)
- [ ] Implement `DocnetPdfRenderer` in `Fileway.Api/Infrastructure/` using `Docnet.Core`
  > Set `<RuntimeIdentifier>linux-x64</RuntimeIdentifier>` in `Fileway.Api.csproj` — without this, the native PDFium binary is absent from publish output and causes `DllNotFoundException` in Docker
- [ ] Register `DocnetPdfRenderer` as `IPdfRenderer` singleton in DI
- [ ] Add internal thumbnail streaming endpoint `POST /internal/thumbnails` — accepts PDF bytes, streams `PageThumbnail` objects via SSE; not a public ToolRegistry tool; same SSE infrastructure as regular jobs
  > This endpoint is internal-only; no `X-Session-Token` validation needed, but restrict to same-origin requests via CORS policy
- [ ] **Checkpoint** — After `dotnet publish src/Fileway.Api -r linux-x64 --no-build`, confirm a native PDFium binary exists under `publish/runtimes/linux-x64/native/`; call `IPdfRenderer.GetPageCount(minimalPdfBytes)` in a scratch test and confirm it returns 3
  > A missing native binary only explodes at runtime in Docker — `dotnet build` passes regardless

---

### PDF Manipulation Processors (API)

> Read `docs/architecture/04-processors.md` before starting this section.

- [ ] Add `PdfPigProcessor` base class to `Fileway.Api/Processors/Base/` — provides `OpenDocument(bytes)` and `BuildOutput(PdfDocumentBuilder)` helpers; `OpenDocument` checks for password-protection and throws `ProcessorDomainException(ErrorCodes.EncryptedFile)` before any work begins
  > PdfPig throws when opening a password-protected PDF; catch it here in the base class so every subclass gets the check for free
- [ ] Implement `MergePdfProcessor` in `Fileway.Api/Processors/PdfManipulation/` — merges all `InputFiles` in `Index` order
- [ ] Implement `SplitPdfProcessor` — splits at `toolOptions.splitAtPages`; output is a ZIP of individual PDF parts
- [ ] Implement `ReorderPdfProcessor` — reorders pages per `toolOptions.pageOrder`; throw `ProcessorValidationException` on duplicate or out-of-range page numbers
- [ ] Implement `RemovePdfPagesProcessor` — removes pages in `toolOptions.pagesToRemove`; at least one page must remain after removal
- [ ] Implement `RotatePdfProcessor` — rotates pages by `toolOptions.rotation` (0/90/180/270); optionally targets a page range
- [ ] Implement `WatermarkPdfProcessor` — overlays text watermark from `toolOptions.watermarkText`
  > Never log `toolOptions.watermarkText` — it is a `toolOptions` value (ref: `10-observability.md`)
- [ ] Implement `ProtectPdfProcessor` — sets PDF open password from `toolOptions.password` using PdfPig
  > Never log `toolOptions.password` value
- [ ] Register all 7 processors in `ProcessorExtensions.cs`
- [ ] **Checkpoint** — Submit merge-pdf with 2 PDFs; SSE stream emits all 4 stages in order with non-decreasing `overallPercent`; Completed event contains a non-empty output; submit a password-protected PDF to any manipulation tool and confirm SSE emits `Failed` with `errorCode: "EncryptedFile"`

---

### PdfPageEditor Component

> Read `docs/architecture/09-ui-design.md` and `docs/architecture/12-pdf-rendering.md` before starting this section.

- [ ] Add `MultiFileDropZone.razor` — multi-file variant of `DropZone`; shows an ordered file list with per-file remove controls; activated when `ToolDefinition.AcceptsMultipleFiles = true`; enforces `MaxInputFileCount`
  > File order in the list maps to `InputFile.Index` — the processor relies on this for merge order; allow drag reorder of the list
- [ ] Add `PdfPageThumbnail.razor` — renders one thumbnail card with `Base64Jpeg` directly as `<img src=`; includes page number badge and selectable/removable state
- [ ] Add `PdfPageEditor.razor` — drag-drop thumbnail grid that populates progressively as thumbnails stream in from the internal SSE endpoint; maintains page order state; mobile alternative: tap-to-select + tap-to-insert-before
  > Thumbnails may arrive out of page order (fastest-rendered first); display them in correct order client-side regardless of arrival order (ref: `12-pdf-rendering.md`)
- [ ] Add `PdfFirstPagePreview.razor` — calls the thumbnail endpoint for page index 0 only; used as inline preview for PDF input/output
- [ ] Add `PageRangeSelector.razor` — from/to page number inputs with visual range indicator; rendered by `ToolOptionsPanel` when `UiHints.ShowPageSelector`
- [ ] Add `SplitControls.razor` — split point selector with resulting page count preview; rendered when `UiHints.ShowSplitControls`
- [ ] Update `PreviewPanel.razor` to handle `PreviewKind.FirstPageRender` and `PreviewKind.PageThumbnails`
- [ ] Update `ToolOptionsPanel.razor` to handle `ShowOrderableList`, `ShowPageSelector`, `ShowSplitControls` UiHints
- [ ] **Checkpoint** — Drop a PDF on `reorder-pdf`; thumbnails stream into the grid progressively (not all at once after a delay); drag page 3 before page 1; submit the job; confirm output PDF has pages in the new order (open it to verify)

---

### Tests — M3

- [ ] Add processor unit tests for all 7 PDF manipulation processors — 6-test minimum bar each; use `EmbeddedTestFiles.MinimalPdf` (3 pages)
- [ ] Add a `DocnetPdfRenderer.RenderFirstPage` unit test — assert non-empty JPEG bytes returned for a known-good PDF
- [ ] Add API integration tests for merge-pdf and split-pdf end-to-end via `WebApplicationFactory`
- [ ] Add `PdfPageEditor` bUnit component test — assert page order state updates correctly on reorder; assert thumbnail cards append as stream events arrive
- [ ] **Checkpoint** — `dotnet test` exits 0 with zero failures

---

## M4 — Document Conversion + Remove Background

**Done when:** pdf-to-docx, docx-to-pdf, pdf-to-images, images-to-pdf, md-to-pdf, and remove-bg all work end-to-end; LibreOffice converts DOCX↔PDF correctly in Docker; ONNX model loads at startup; R2 storage delivers large outputs via signed URL; CI passes.

---

### Storage — R2

- [ ] Implement `R2StorageService` in `Fileway.Api/Infrastructure/` using `AWSSDK.S3`
  > R2 uses the S3-compatible API; set `ServiceURL` to `https://{accountId}.r2.cloudflarestorage.com` in `StorageOptions`
- [ ] Extend `StorageOptions` with: `BucketName`, `AccountId`, `AccessKey`, `SecretKey`, `SignedUrlTtlMinutes` (30), `UseLocalStorage` flag
- [ ] Register `IStorageService` → `R2StorageService` when `UseLocalStorage = false`; keep `LocalFileStorageService` binding when `true`
  > Never log the signed URL — it contains access credentials (ref: `10-observability.md`)
- [ ] **Checkpoint** — With `UseLocalStorage: false` and valid R2 credentials: complete an async job and confirm the SSE `Completed` event `signedUrl` begins with `https://` and the URL resolves (GET returns 200); with `UseLocalStorage: true`: no R2-related log entries appear

---

### LibreOffice Integration

> Read `docs/architecture/13-libreoffice.md` before starting this section.

- [ ] Extend `LibreOfficeOptions` with: `ExecutablePath` (`soffice`), `MaxConcurrent` (2), `TempBasePath` (`/tmp/fileway/`)
- [ ] Implement `LibreOfficeManager` in `Fileway.Api/Infrastructure/` — `SemaphoreSlim(2)`, per-job temp dir, `Process` with `UseShellExecute: false`, kill on cancellation, `finally` cleanup
  > Pass `-env:UserInstallation=file://{tempDir}/profile` — without this, two concurrent LO processes corrupt each other's profile (ref: `13-libreoffice.md`)
- [ ] Log `LibreOfficeVerified` startup event after confirming `soffice --version` succeeds
- [ ] Add `LibreOfficeProcessor` base class to `Fileway.Api/Processors/Base/` — subclasses implement only `GetConvertToFormat()`; base owns all process lifecycle
- [ ] Update `docker/Dockerfile.api` to install `libreoffice-nogui`, `fonts-liberation`, `fonts-dejavu`, `libfontconfig1` in a single `RUN` layer; clean apt cache in the same layer
- [ ] **Checkpoint** — API startup logs show a `LibreOfficeVerified` event with a version string; submit a `.docx` to `docx-to-pdf` via `/api/v1/jobs`; output PDF is non-zero bytes and opens correctly; submit two concurrent DOCX conversions and confirm both complete without errors — logs show two distinct `UserInstallation` temp paths, not the same one

---

### Document Conversion Processors (API)

- [ ] Add remaining document `FileFormat` records to `FileFormats.cs` if not already present: md, html, txt (needed for LibreOffice output)
- [ ] Add document conversion `ToolDefinition` records to `DocumentTools.cs`: pdf-to-docx, docx-to-pdf, pdf-to-images, images-to-pdf, md-to-pdf — all `ApiOnly` + `JobTier.Async`
- [ ] Implement `DocxToPdfProcessor` extending `LibreOfficeProcessor` — `GetConvertToFormat()` returns `"pdf"`
- [ ] Implement `MarkdownToPdfProcessor` extending `LibreOfficeProcessor` — convert MD → HTML in-process first, then write the HTML to the temp dir and pass it to LibreOffice
  > Only the HTML → PDF step goes through LibreOffice; the MD → HTML conversion is in-process using a library (no extra process)
- [ ] Implement `PdfToDocxProcessor` in `Fileway.Api/Processors/DocumentConversion/` using PdfPig text extraction — not LibreOffice
  > Use PdfPig for PDF → DOCX; LO's PDF import quality is poor; this tool reconstructs layout from extracted text and positioning
- [ ] Implement `PdfToImagesProcessor` using `IPdfRenderer.RenderAllPages()` — output is a ZIP of PNG files; supports screen (150 DPI) and print (300 DPI) quality via `toolOptions.quality`
  > Uses Docnet.Core/PDFium via `IPdfRenderer`, not LibreOffice; `04-processors.md` lists this under `LibreOfficeProcessor` subclasses but `12-pdf-rendering.md` defines the dedicated rendering interface for this case
- [ ] Implement `ImagesToPdfProcessor` using PdfPig — accepts multi-file upload; embeds each image as a full-page PDF; respects `InputFile.Index` for page order
- [ ] Register all 5 processors in `ProcessorExtensions.cs`
- [ ] **Checkpoint** — Submit a real `.docx` to `docx-to-pdf`; the output PDF renders readable text; submit a real `.md` file to `md-to-pdf`; submit a PDF to `pdf-to-images`; the downloaded ZIP contains one PNG per page; submit a PDF to `pdf-to-docx`; the resulting `.docx` opens without corruption; submit two images to `images-to-pdf`; output PDF has exactly two pages in index order

---

### Remove Background (ONNX)

- [ ] Add `remove-bg` `ToolDefinition` to `ImageTools.cs` — `ApiOnly`, `JobTier.Async`, output format png
- [ ] Implement `OnnxModelLoader` as `IHostedService` — loads RMBG ONNX model file at startup, blocks the readiness probe until loaded; logs `OnnxModelLoaded` event
  > Bundle the ONNX model file inside the Docker image — do not download at runtime; add to `docker/` and `COPY` it in `Dockerfile.api`
- [ ] Implement `RemoveBackgroundProcessor` in `Fileway.Api/Processors/ImageManipulation/` using `Microsoft.ML.OnnxRuntime`
  > Run inference inside a `SemaphoreSlim` pool sized by `ApiOptions.MaxOnnxJobs`; check `CancellationToken` between pre/post-processing steps
- [ ] Register `RemoveBackgroundProcessor` in `ProcessorExtensions.cs`
- [ ] **Checkpoint** — `GET /health/ready` returns 200 only after `OnnxModelLoaded` is logged (readiness blocks until model is ready); submit an image with a plain background to `remove-bg`; output PNG has an alpha channel (transparent background); submit the same job twice concurrently and confirm neither job errors due to model contention
  > If the ONNX model file is missing from the Docker image the readiness check will hang — check `Dockerfile.api` `COPY` step first

---

### Tests — M4

- [ ] Add processor unit tests for all 5 document conversion processors — 6-test minimum bar each; use NSubstitute mock of `LibreOfficeManager` for unit tests; use real LibreOffice in integration tests
- [ ] Add `LibreOfficeManager` unit tests — verify temp dir cleanup in `finally` block; verify process kill on `CancellationToken` cancellation
- [ ] Add processor unit tests for `RemoveBackgroundProcessor` — mock ONNX inference session for unit tests
- [ ] Add API integration tests for docx-to-pdf and pdf-to-images end-to-end via `WebApplicationFactory`
- [ ] **Checkpoint** — `dotnet test` exits 0 with zero failures; confirm `LibreOfficeManager` tests exercise the `CancellationToken` kill path (a test that never cancels provides false assurance)

---

## M5 — Polish + Release Readiness

**Done when:** StaticGen generates correct HTML and `sitemap.xml` for all 23 tools; security headers pass a baseline check; Docker image builds cleanly with LibreOffice and PDFium; all 23 tools manually verified in the devcontainer; CI passes end-to-end.

---

### SEO Prerendering

> Read `docs/architecture/14-seo.md` before starting this section.

- [ ] Create `tools/StaticGen/` as a standalone .NET CLI project — not added to `Fileway.sln`; references `Fileway.Shared` only
- [ ] Implement HTML generation: per-tool `wwwroot/tools/{slug}/index.html`, `/tools/index.html`, root `index.html`, `sitemap.xml`
  > Call `ToolRegistry.GetSitemapEntries()` — no hardcoded slug lists; a new tool's SEO page is generated automatically on the next build
- [ ] Add JSON-LD `WebApplication` structured data block to each per-tool HTML file (ref: `14-seo.md`)
- [ ] Add `StaticFileOptions` routing in `Fileway.Api/Program.cs` — known tool slug paths serve prerendered `wwwroot/tools/{slug}/index.html`; unknown paths serve Blazor `index.html`; `/api/` and `/health/` bypass static files entirely
- [ ] Add `robots.txt` to `Fileway.Client/wwwroot/` with `Allow: /` and correct `Sitemap:` URL
- [ ] Add a CI post-publish step in `.github/workflows/ci.yml` that runs `dotnet run --project tools/StaticGen/` after `dotnet publish` and before the Docker build
- [ ] **Checkpoint** — Run `dotnet run --project tools/StaticGen/`; confirm `wwwroot/tools/{slug}/index.html` exists for every tool slug in the registry (no hardcoded list — a missing slug means the generator is reading ToolRegistry correctly); open one generated file and confirm it contains a `<script type="application/ld+json">` block; confirm `sitemap.xml` lists all tool slugs; confirm StaticGen exits 0 with no warnings

---

### Security Hardening

- [ ] Audit and tighten CSP — `script-src` allows only `'self'` and the Blazor framework hash; remove any `'unsafe-inline'` for scripts
- [ ] Implement zip bomb detection in `POST /api/v1/jobs` for archive inputs — reject with `ZipBombDetected` (ref: `03-api-surface.md`)
- [ ] Add polyglot detection — reject inputs where magic bytes simultaneously match two distinct formats; return `PolyglotDetected`
- [ ] Grep the entire codebase for any log calls that reference `toolOptions` values — ensure none are present
- [ ] Verify `UseLocalStorage: false` is the default in `appsettings.json` (not `appsettings.Development.json`) so production Docker never writes to local disk
- [ ] **Checkpoint** — Run the app through a browser security scanner (e.g. `curl -I` and inspect response headers); confirm `Content-Security-Policy` header is present with no `'unsafe-inline'` for scripts; upload a ZIP that expands to >1 GB and confirm the API returns `400` with `errorCode: "ZipBombDetected"` before extracting; grep the codebase for `toolOptions` in any log call and confirm zero hits

---

### Observability Completeness

- [ ] Verify all structured log events listed in `10-observability.md` are emitted with the correct field names
- [ ] Verify `ipHash` is always `SHA-256(rawIp + dailySalt)` — no raw IP reachable via any log path
- [ ] Verify `sessionPrefix` (first 8 chars) is used everywhere; grep for full token logging
- [ ] Confirm `AuditLogService` events appear in stdout with a distinct `event` field type
- [ ] **Checkpoint** — Submit a job and pipe the API stdout through `jq`; confirm every log line parses as valid JSON; confirm the `ipHash` field is a 64-character hex string (SHA-256), never a raw IP; confirm `sessionPrefix` is 8 characters; grep stdout for any occurrence of the full session token UUID and confirm zero hits

---

### Docker + Final Build

- [ ] Finalise `docker/Dockerfile.api` — multi-stage, `<RuntimeIdentifier>linux-x64</RuntimeIdentifier>`, LibreOffice layer, non-root `app` user, `EXPOSE 8080`, `HEALTHCHECK` hitting `/health/live`
- [ ] Verify native PDFium binary is present after publish: confirm `libdocnet.so` (or equivalent) is in `runtimes/linux-x64/native/` in the publish output
- [ ] Ensure `.dockerignore` excludes `tests/`, `.git/`, `tools/`, `*.user`, local config files
- [ ] Add a CI step to run `docker build -f docker/Dockerfile.api .` and confirm it exits 0
- [ ] **Checkpoint** — `docker build -f docker/Dockerfile.api .` exits 0 with no layer errors; `docker run --rm <image> /health/live` returns `200`; `docker run --rm <image> find /app/runtimes/linux-x64/native -name "*.so"` prints the PDFium native binary — if it is missing, a `DllNotFoundException` will only appear at runtime, not during build

---

### Final QA Checklist

- [ ] `ProcessorSanityCheck` passes at startup with all 23 tools registered — zero `InvalidOperationException` at boot
- [ ] All `RelatedSlugs` on all 23 `ToolDefinition` records resolve to real slugs (SanityCheck enforces this)
- [ ] `dotnet test` passes with no `Thread.Sleep` in any async test
- [ ] Manually run each of the 23 tools in the devcontainer with a real file
- [ ] Test at 375px viewport width — all tap targets are ≥ 48×48px; no hover-only interactions
- [ ] Verify dark mode preference survives tab close and reopen (stored in `localStorage` via `ThemeInterop.js`)
- [ ] Verify session token is gone after tab close (stored in `sessionStorage` — not `localStorage`)
