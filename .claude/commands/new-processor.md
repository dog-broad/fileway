# /project:new-processor

Scaffold a new processor implementation with correct structure, registration, and test stubs. Run this as part of `/project:new-tool` or standalone when adding a processor for an already-registered tool.

## Before you start

Read `docs/architecture/04-processors.md` fully. Know the answer to:
- Is this an API processor, WASM processor, or both?
- What are the exact ProgressStages (must match ToolDefinition)?
- What tool-specific options does this processor accept?
- What domain errors can this processor produce?

## What to collect

1. Tool slug (must exist in ToolRegistry)
2. Processor kind: ApiOnly | WasmOnly | WasmPreferred
3. Category folder: PdfManipulation | Documents | Images | Data
4. Tool-specific options (what fields does toolOptions contain?)
5. Known domain error conditions (what can go wrong with this specific format?)

## API Processor template

File: `src/Fileway.Api/Processors/{Category}/{ToolName}Processor.cs`

Structure to follow:
```
- Implements IApiProcessor
- Constructor: inject only what is needed (IPdfRenderer, LibreOfficeManager, etc.)
- ValidateOptions(JsonElement toolOptions):
    - Deserialise to private strongly-typed options record
    - Validate each field — throw ProcessorValidationException(ErrorCodes.X) for each failure
    - Return void (no return value)
- ExecuteAsync(ProcessorContext context, CancellationToken cancellationToken):
    - Deserialise toolOptions first
    - Report progress at each stage: context.Progress.Report(new ProcessorProgressEvent { Stage = ProgressStages[N], StageIndex = N, StageTotalCount = total, OverallPercent = N * (100/total) })
    - Observe cancellationToken at every await
    - Throw ProcessorDomainException(ErrorCodes.X) for known failures
    - Return ProcessorResult with OutputContent, OutputFormat, OutputFilename, Metadata
```

Registration in `src/Fileway.Api/Infrastructure/ProcessorExtensions.cs`:
```csharp
services.AddTransient<{ToolName}Processor>();
```

Set in ToolDefinition:
```csharp
ProcessorType = typeof({ToolName}Processor)
```

## WASM Processor template

File: `src/Fileway.Client/Processors/{Category}/{ToolName}Processor.cs`

Structure to follow:
```
- Implements IWasmProcessor
- ValidateOptions: same as API processor
- CanHandleSize(long fileSizeBytes):
    - return fileSizeBytes <= {threshold from ToolDefinition.WasmSizeThresholdBytes}
- ExecuteAsync:
    - Same contract as API processor
    - Call Task.Yield() before heavy computation loops to keep Blazor UI responsive
    - No System.IO.File, no network calls, no Process — pure in-memory only
```

Registration in `src/Fileway.Client/Infrastructure/WasmProcessorExtensions.cs`:
```csharp
services.AddTransient<{ToolName}Processor>();
// AND add to slug→Type dictionary:
processors["{tool-slug}"] = typeof({ToolName}Processor);
```

## Test template

File: `tests/Fileway.Tests.Api/Processors/{Category}/{ToolName}ProcessorTests.cs`
(or `tests/Fileway.Tests.Client/Processors/{Category}/` for WASM)

Required test methods — all six are mandatory:

```
[Fact] HappyPath_ValidInput_ReturnsCorrectOutputFormat()
[Fact] CorruptedInput_ThrowsProcessorDomainException_WithCorrectErrorCode()
[Fact] InvalidOptions_ValidateOptions_ThrowsProcessorValidationException()
[Fact] PreCancelledToken_ExecuteAsync_ThrowsOperationCanceledException()
[Fact] Progress_EventsArrive_InCorrectStageOrderWithNonDecreasingPercent()
[Fact] Result_OutputFilename_IsNonEmptyWithCorrectExtensionAndNoPathSeparators()
```

Use fixtures from `tests/Fileway.Tests.Api/Fixtures/`:
- `TestProgressCollector` — captures all IProgress events
- `TestFileFactory.CreateFrom(byte[], FileFormat)` — builds InputFile
- `ProcessorContextBuilder` — fluent builder for ProcessorContext
- `CorruptedFileFactory.For(FileFormat)` — generates bad bytes per format
- `EmbeddedTestFiles.Get(string filename)` — loads embedded test resource

## LibreOffice-backed processors

If this processor uses LibreOffice, extend `LibreOfficeProcessor` base class instead of implementing `IApiProcessor` directly:
- Override `GetConvertToFormat()` — return the LibreOffice format string
- Override `GetProgressStages()` — return your stage names
- The base class handles: temp dir, process management, timeout, cleanup, logging
- Inject `LibreOfficeManager` via base class — do not instantiate it yourself

## PdfPig-backed processors

For PDF manipulation (merge, split, reorder, etc.), extend `PdfPigProcessor` base class:
- Access `PdfDocument` via `OpenDocument(bytes)` — base class handles disposal
- Call `context.Progress.Report(...)` between major operations
- Return output bytes from `BuildOutput(PdfDocumentBuilder)` helper

## Done checklist

- [ ] Processor class created in correct folder
- [ ] Implements correct interface (IApiProcessor or IWasmProcessor)
- [ ] ValidateOptions validates all toolOptions fields with correct error codes
- [ ] ExecuteAsync observes CancellationToken at every await
- [ ] Progress stages exactly match ToolDefinition.ProgressStages
- [ ] OverallPercent goes from ~0 to 100 across stages
- [ ] All domain failure modes throw ProcessorDomainException with correct ErrorCode
- [ ] Processor registered in ProcessorExtensions.cs or WasmProcessorExtensions.cs
- [ ] ProcessorType set in ToolDefinition (API processors)
- [ ] All 6 required test methods written and passing
- [ ] ProcessorSanityCheck passes at startup
