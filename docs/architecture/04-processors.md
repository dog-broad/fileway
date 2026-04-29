# Processor Architecture

A processor is a pure transformation unit. It knows nothing about HTTP, SSE, Blazor, or the job store. It receives bytes and options, reports progress to a channel, and returns bytes.

---

## Core Types (all in Fileway.Shared/Processors/)

### ProcessorContext — what every processor receives

| Field | Type | Notes |
|---|---|---|
| `ToolSlug` | `string` | For logging only — don't branch on it |
| `InputFiles` | `IReadOnlyList<InputFile>` | Single-file tools always have exactly one |
| `OutputFormat` | `FileFormat` | Already validated against OutputFormats |
| `ToolOptions` | `JsonElement` | Processor deserialises to its own options record |
| `CancellationToken` | `CancellationToken` | Linked to SSE lifetime + job timeout. Observe everywhere. |
| `Progress` | `IProgress<ProcessorProgressEvent>` | Never null. No-op for sync tools. |

### InputFile

| Field | Type | Notes |
|---|---|---|
| `Content` | `ReadOnlyMemory<byte>` | Full file bytes — buffered before processor runs |
| `DetectedFormat` | `FileFormat` | Already validated. Trust this — do not re-detect. |
| `SizeBytes` | `long` | Convenience — same as Content.Length |
| `OriginalFilename` | `string?` | Untrusted. Only for output filename suggestion. Never for paths or detection. |
| `Index` | `int` | Zero-based. For multi-file tools (merge-pdf respects order). |

### ProcessorResult — what every processor returns

| Field | Type | Notes |
|---|---|---|
| `OutputContent` | `ReadOnlyMemory<byte>` | Never empty on success |
| `OutputFormat` | `FileFormat` | Confirms actual output format |
| `OutputFilename` | `string` | Sanitised. Convention: `{inputNameWithoutExt}.{outputExt}`. No path separators. |
| `Metadata` | `IReadOnlyDictionary<string,string>?` | Page count, dimensions, compression ratio etc. Shown in preview panel. |

### ProcessorProgressEvent

| Field | Type | Notes |
|---|---|---|
| `Stage` | `string` | Must be one of ToolDefinition.ProgressStages. Validated at startup. |
| `StageIndex` | `int` | Zero-based index into ProgressStages |
| `StageTotalCount` | `int` | Always equals ProgressStages.Length |
| `OverallPercent` | `int` | 0–100. Must be non-decreasing. 0 on first event, 100 on last before Completed. |
| `Detail` | `string?` | Sub-stage detail. "Page 3 of 12", "Analysing layer 2 of 4" |

---

## Interfaces

### IApiProcessor (Fileway.Shared/Processors/)

```
ValidateOptions(JsonElement toolOptions) → void
  Called before job creation. Synchronous. Fast.
  Throw ProcessorValidationException for any invalid option.

ExecuteAsync(ProcessorContext context, CancellationToken ct) → Task<ProcessorResult>
  Throw ProcessorDomainException for known failures.
  Throw ProcessorUnexpectedException (wrapping inner) for anything else.
  Never catch all exceptions silently.
  Observe ct at every await and every long loop.
```

### IWasmProcessor (Fileway.Shared/Processors/)

Same as IApiProcessor plus:

```
CanHandleSize(long fileSizeBytes) → bool
  Default: return fileSizeBytes <= ToolDefinition.WasmSizeThresholdBytes
  ProcessorRouter calls this for WasmPreferred tools.
```

Also: call `Task.Yield()` before heavy computation loops to keep Blazor UI responsive. No System.IO.File. No network calls. No Process. Pure in-memory only.

---

## Exception Model

| Exception | Thrown by | Caught by | Result |
|---|---|---|---|
| `ProcessorValidationException` | `ValidateOptions` | JobDispatcher (before job creation) | 422 ProblemDetails immediately |
| `ProcessorDomainException` | `ExecuteAsync` | JobDispatcher | Failed SSE event with domain errorCode |
| `ProcessorUnexpectedException` | `ExecuteAsync` | JobDispatcher | Failed SSE event with ProcessorUnexpectedError. Inner exception logged, never sent to client. |

**Rule:** Processors throw typed exceptions — never return null or error codes. A `ProcessorResult` always means success. Do not catch library exceptions unless you can meaningfully classify them — let them bubble as `ProcessorUnexpectedException`.

---

## ProcessorRouter (Fileway.Client/Services/)

Client-side only. Decision made before any network call.

```
WasmOnly           → WasmProcessor always
ApiOnly            → ApiProcessor always
WasmPreferred:
  WasmProcessor not registered for slug → ApiProcessor
  wasmProcessor.CanHandleSize() = false → ApiProcessor
  wasmProcessor.CanHandleSize() = true  → WasmProcessor
  WasmProcessor throws ProcessorUnexpectedException → fallback to ApiProcessor + show "Switching to server..."
  WasmProcessor throws ProcessorDomainException → propagate (file is the problem, not the environment)
```

---

## JobDispatcher (Fileway.Api/Jobs/)

Orchestrates the full async job lifecycle. Route handler calls `DispatchAsync()` and immediately returns 202.

```
1. Resolve IApiProcessor via DI using ToolDefinition.ProcessorType
2. Call processor.ValidateOptions() → throws → 422 response
3. Check concurrent job limit → throws → 429
4. Create JobRecord in IJobStore
5. Return JobId to route handler
6. Background Task:
   - Emit Created event
   - Link CancellationToken to timeout CTS
   - Create IProgress wrapping Channel writes
   - Build ProcessorContext
   - Emit Processing event (stage 0)
   - await processor.ExecuteAsync(context, ct)
   - On success → upload to R2 → emit Completed → close Channel
   - On ProcessorDomainException → emit Failed (domain errorCode) → close Channel
   - On ProcessorUnexpectedException → log inner → emit Failed (ProcessorUnexpectedError) → close Channel
   - On OperationCanceledException:
       timeout → emit Failed (JobTimeout)
       SSE disconnect → silently close Channel
```

---

## DI Registration

**API processors** — `Fileway.Api/Infrastructure/ProcessorExtensions.cs`
```csharp
services.AddTransient<PdfToDocxProcessor>();
// one line per processor — explicit, no scanning
```

**WASM processors** — `Fileway.Client/Infrastructure/WasmProcessorExtensions.cs`
```csharp
services.AddTransient<JsonYamlProcessor>();
processors["json-to-yaml"] = typeof(JsonYamlProcessor);
// DI registration + slug→Type dictionary entry
```

**Lifetime:** Transient — new instance per job. No shared state between jobs.

**ProcessorType in ToolDefinition** — null on WASM side. Server-side only. Populated via second initialisation pass at startup.

---

## ProcessorSanityCheck (IHostedService — runs at startup before accepting requests)

Crashes startup with `InvalidOperationException` if:
- Any non-WasmOnly tool has null ProcessorType
- Any ProcessorType is not registered in DI
- Any ProcessorType does not resolve to IApiProcessor
- Any processor's emitted ProgressStages don't match ToolDefinition.ProgressStages
- Any two ToolDefinitions share the same Slug
- Any RelatedSlug doesn't resolve to a known slug
- Any FileFormat referenced by any ToolDefinition doesn't exist

WASM equivalent logs warnings (not crashes) for:
- WasmOnly tool with no WASM processor registered
- WasmPreferred tool with no WASM processor (uses API fallback — logged as info)
- WASM processor registered for an unknown slug

---

## Base Classes

**`LibreOfficeProcessor` (Fileway.Api/Processors/Base/)** — extend for: DocxToPdfProcessor, PdfToImagesProcessor, MarkdownToPdfProcessor  
- Owns: temp dir creation, process spawn, UserInstallation isolation, timeout, cleanup, stderr logging  
- Subclasses define: `GetConvertToFormat()`, `GetProgressStages()`

**`PdfPigProcessor` (Fileway.Api/Processors/Base/)** — extend for all PDF manipulation processors  
- Provides: `OpenDocument(bytes)`, `BuildOutput(PdfDocumentBuilder)` helpers

**`ImageSharpProcessor` (Fileway.Client/Processors/Base/)** — extend for all WASM image processors  
- Provides: shared ImageSharp decode/encode helpers

---

## Bidirectional Processors

JSON↔YAML, JSON↔CSV, JSON↔TOML — one class, two slug registrations.  
Direction inferred from `context.InputFiles[0].DetectedFormat`.  
Both directions tested independently.

---

## Complete Processor Map

**PDF Manipulation (ApiOnly):** MergePdfProcessor, SplitPdfProcessor, ReorderPdfProcessor, RemovePdfPagesProcessor, RotatePdfProcessor, WatermarkPdfProcessor, ProtectPdfProcessor  
**Document Conversion (ApiOnly):** PdfToDocxProcessor, DocxToPdfProcessor, PdfToImagesProcessor, ImagesToPdfProcessor, MarkdownToPdfProcessor  
**Images (ApiOnly):** RemoveBackgroundProcessor  
**Images (WasmPreferred):** CompressImageProcessor×2, SvgConvertProcessor×2  
**Images (WasmOnly):** ConvertImageProcessor, CropResizeImageProcessor, RotateFlipImageProcessor  
**Data (WasmOnly):** JsonYamlProcessor, JsonCsvProcessor, JsonTomlProcessor, ValidateProcessor  
**Data (WasmPreferred):** CsvToXlsxProcessor×2

Total: 26 processor classes for 23 tools (3 tools have both WASM and API implementations).
