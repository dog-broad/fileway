# /project:new-tool

Scaffold a complete new Fileway tool from scratch. Run this when adding any new conversion or manipulation tool to the system.

## Before you start

Read these documents in full before writing any code:
- `docs/architecture/01-tool-registry.md` — ToolDefinition fields and registry
- `docs/architecture/04-processors.md` — IApiProcessor / IWasmProcessor interfaces
- `docs/architecture/08-testing.md` — minimum test bar

## What to collect first

Ask the developer for:
1. Tool slug (kebab-case, e.g. `pdf-to-docx`)
2. Display name (e.g. `PDF to Word`)
3. Tool kind: Conversion or Manipulation
4. Tool category: Document | Image | Data | Archive
5. Accepted input format(s)
6. Output format(s)
7. Processor kind: WasmOnly | ApiOnly | WasmPreferred
8. Short description (max 160 chars, used for SEO)
9. Related tool slugs (for "also try" panel)

## Steps — execute in order, do not skip

### Step 1 — ToolDefinition record
Add to the correct file in `src/Fileway.Shared/Tools/Definitions/`:
- Set all ~25 fields per `01-tool-registry.md`
- ProgressStages must exactly match what the processor will emit
- ProcessorType: set to `typeof(YourProcessor)` — leave null if WASM-only
- SeoTitle format: `{Action} — Fileway` (max 60 chars)

### Step 2 — Verify FileFormats
Confirm all referenced FileFormat values exist in `src/Fileway.Shared/Formats/FileFormats.cs`.
If a format is missing, run `/project:add-format` first.

### Step 3 — API Processor (if ApiOnly or WasmPreferred)
Create `{ToolName}Processor.cs` in `src/Fileway.Api/Processors/{Category}/`:
- Implement `IApiProcessor`
- Implement `ValidateOptions(JsonElement)` — throw `ProcessorValidationException` for bad options
- Implement `ExecuteAsync(ProcessorContext, CancellationToken)` — throw typed exceptions only
- Emit progress events matching ProgressStages exactly
- Observe CancellationToken at every await and loop iteration
- Clean up any temp resources in finally blocks

Register in `src/Fileway.Api/Infrastructure/ProcessorExtensions.cs`:
```csharp
services.AddTransient<YourProcessor>();
```

### Step 4 — WASM Processor (if WasmOnly or WasmPreferred)
Create `{ToolName}Processor.cs` in `src/Fileway.Client/Processors/{Category}/`:
- Implement `IWasmProcessor`
- Implement `CanHandleSize(long)` — return `fileSizeBytes <= WasmSizeThresholdBytes`
- Use `Task.Yield()` at heavy computation points to keep UI responsive

Register in `src/Fileway.Client/Infrastructure/WasmProcessorExtensions.cs`:
- Add DI registration
- Add slug → Type entry to the router dictionary

### Step 5 — Tests
Create `{ToolName}ProcessorTests.cs` in `tests/Fileway.Tests.Api/Processors/{Category}/` (API) or `tests/Fileway.Tests.Client/Processors/{Category}/` (WASM).

Minimum required tests (per `08-testing.md`):
- Happy path: known-good input → output bytes pass magic byte check for output format
- Corrupted input → `ProcessorDomainException` with `CorruptedFile` error code
- Invalid options → `ProcessorValidationException` from `ValidateOptions`
- Pre-cancelled token → `OperationCanceledException` propagates
- Progress events arrive in correct stage order, OverallPercent non-decreasing 0→100
- OutputFilename is non-empty, correct extension, no path separators

### Step 6 — Update RelatedSlugs
For each slug listed in this tool's `RelatedSlugs`, open that tool's ToolDefinition and add this tool's slug to its `RelatedSlugs` list.

### Step 7 — Run sanity check
```bash
dotnet run --project src/Fileway.Api
```
`ProcessorSanityCheck` runs at startup. If misconfigured (wrong ProgressStages, unregistered processor, bad slug), it crashes with a clear message. Fix before continuing.

### Step 8 — Run tests
```bash
dotnet test tests/Fileway.Tests.Api
dotnet test tests/Fileway.Tests.Client
```
All tests must pass before the tool is considered complete.

## Done checklist

- [ ] ToolDefinition record added to Definitions/ file
- [ ] All FileFormats referenced exist in FileFormats.cs
- [ ] API processor created and registered (if applicable)
- [ ] WASM processor created and registered (if applicable)
- [ ] All minimum tests written and passing
- [ ] RelatedSlugs updated bidirectionally
- [ ] ProcessorSanityCheck passes at startup
- [ ] No deviation from source of truth documents
