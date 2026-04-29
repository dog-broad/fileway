# Testing Strategy

---

## Frameworks

| Concern | Framework |
|---|---|
| All unit and integration tests | xUnit |
| Blazor component tests | bUnit |
| API integration tests | WebApplicationFactory (in-process, real HTTP) |
| Mocking | NSubstitute — interfaces only, never concrete classes |
| Assertions | FluentAssertions |
| Coverage collection | coverlet |

---

## Three-Layer Pyramid

### Layer 1 — Unit Tests (most tests, run in < 60s)

**Covers:** Processors (API and WASM), FormatDetector, ToolRegistry queries, ErrorMessages mapping, ProcessorRouter routing decisions, JobQueueManager, rate limit logic.

**Rules:** No I/O. No HTTP. No database. No file system. Fast. Isolated.

### Layer 2 — Integration Tests (moderate, run in < 5 min)

**Covers:** Full API endpoint tests via WebApplicationFactory. Real HTTP. Real job dispatch. Real processor execution. Real in-memory job store.

**Storage in CI:** `IStorageService` → `LocalFileStorageService` (writes to temp dir). No R2 credentials needed.  
**LibreOffice in CI:** Installed in CI runner image (same as devcontainer).

### Layer 3 — Component Tests (Blazor)

**Covers:** DropZone detection flow, PdfPageEditor reorder state, ToolOptionsPanel hint-driven rendering, ErrorPanel copy by errorCode.

**Rules:** Test behaviour, not markup. bUnit's `JSInterop` mock — no real browser. Services registered in bUnit's DI via `ctx.Services`.

---

## Minimum Test Bar — Per Processor (mandatory before merge)

| Test | Verifies |
|---|---|
| `HappyPath_ValidInput_ReturnsCorrectOutputFormat` | Output bytes pass magic byte check for output format |
| `CorruptedInput_ThrowsProcessorDomainException_WithCorrectErrorCode` | Correct ErrorCode on bad bytes |
| `InvalidOptions_ValidateOptions_ThrowsProcessorValidationException` | Options validation works |
| `PreCancelledToken_ThrowsOperationCanceledException` | CT observed at entry |
| `Progress_EventsInCorrectStageOrderWithNonDecreasingPercent` | Progress contract upheld |
| `Result_OutputFilenameIsNonEmptyWithCorrectExtensionAndNoPathSeparators` | Safe filename output |

WASM processors additionally require:
- `CanHandleSize_ReturnsTrueBelow_FalseAboveThreshold`
- For bidirectional processors: test both directions independently

---

## Shared Test Fixtures

Located in `tests/Fileway.Tests.Api/Fixtures/` and `tests/Fileway.Tests.Client/Fixtures/` (duplicated — no shared test project).

| Fixture | Purpose |
|---|---|
| `TestProgressCollector` | `IProgress<ProcessorProgressEvent>` that stores all events in a list. Use for all progress assertions. |
| `TestFileFactory` | Builds `InputFile` from `byte[]` or embedded resource. Sets DetectedFormat, SizeBytes, Index. |
| `ProcessorContextBuilder` | Fluent builder for `ProcessorContext`. Defaults to sensible values. |
| `CorruptedFileFactory` | Generates deliberately invalid bytes per format. PDF with valid magic but invalid xref. |
| `EmbeddedTestFiles` | Small known-good test files embedded as resources. Max 50KB each: `minimal.pdf` (3 pages), `minimal.docx`, `sample.png` (100×100), `valid.json`, `valid.yaml`, `sample.csv`. |
| `LocalFileStorageService` | `IStorageService` impl that writes to temp dir. Used in integration tests when R2 unavailable. |

---

## CI Enforcement (GitHub Actions)

| Rule | Enforcement |
|---|---|
| Tests run on | Every push to any branch, every PR to main |
| Unit test timeout | 60 seconds total — failure means I/O in unit tests |
| Integration test timeout | 5 minutes |
| Build | `TreatWarningsAsErrors: true` — must pass before tests run |
| Coverage | Collected by coverlet. No minimum threshold in v1. Reported as artifact. |
| Processor coverage | Every processor class must have a test class — enforced by PR checklist, not tooling |
| No `Thread.Sleep` | Async tests only — use `await Task.Delay()` if timing is needed |
