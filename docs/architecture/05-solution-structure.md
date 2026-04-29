# Solution Structure

---

## Naming Conventions

| Item | Convention | Example |
|---|---|---|
| Interfaces | `I` prefix, PascalCase | `IToolRegistry` |
| Records | PascalCase, no suffix | `ToolDefinition` |
| Enums | PascalCase values | `ToolKind.Conversion` |
| Processors | `Processor` suffix | `PdfToDocxProcessor` |
| Services | `Service` suffix | `StorageService` |
| Endpoints | `Endpoints` suffix | `JobEndpoints` |
| Options records | `Options` suffix | `ApiOptions` |
| Private fields | `_camelCase` | `_jobStore` |
| Async methods | `Async` suffix | `ExecuteAsync` |
| Test classes | `Tests` suffix | `PdfToDocxProcessorTests` |
| Files | One type per file, filename = type name | `ToolDefinition.cs` |
| Namespaces | Matches folder path | `Fileway.Api.Processors.PdfManipulation` |
| JS interop files | camelCase | `sseClient.js` |

---

## Directory.Build.props — Applies to All Projects

```xml
TargetFramework: net9.0
Nullable: enable
ImplicitUsings: enable
TreatWarningsAsErrors: true (Release/CI only)
EnforceCodeStyleInBuild: true
Version: 0.1.0 (increment per milestone)
```

**Directory.Packages.props** — all NuGet package versions here. No version numbers in .csproj files.

---

## Fileway.Shared

```
src/Fileway.Shared/
  Formats/
    FileFormat.cs
    MagicSignature.cs
    FileFormats.cs          — static class: FileFormats.Pdf, .Docx, .Png, .Json etc.
    FormatCategory.cs
    PreviewKind.cs
  Tools/
    ToolDefinition.cs
    ToolKind.cs
    ToolCategory.cs
    ProcessorKind.cs
    JobTier.cs
    UiHints.cs
    ToolLimits.cs
    ToolSummary.cs          — API response shape for /api/v1/tools
    Definitions/
      DocumentTools.cs
      ImageTools.cs
      DataTools.cs
  Registry/
    IToolRegistry.cs
    ToolRegistry.cs
    SitemapEntry.cs
  Processors/
    IApiProcessor.cs
    IWasmProcessor.cs
    ProcessorContext.cs
    InputFile.cs
    ProcessorResult.cs
    ProcessorProgressEvent.cs
  Jobs/
    JobStatus.cs
    JobTier.cs
    JobEvent.cs
    JobEventType.cs
    Payloads/
      ProcessingPayload.cs
      CompletedPayload.cs
      FailedPayload.cs
      QueuedPayload.cs
  Api/
    JobOptions.cs
    SyncJobResult.cs
    AsyncJobAccepted.cs
    DetectRequest.cs
    DetectResponse.cs
  Detection/
    IFormatDetector.cs
    FormatDetector.cs
    DetectionConfidence.cs
  Errors/
    ErrorCodes.cs           — all 36 error code string constants
    ProcessorValidationException.cs
    ProcessorDomainException.cs
    ProcessorUnexpectedException.cs
  Fileway.Shared.csproj
```

---

## Fileway.Api

```
src/Fileway.Api/
  Endpoints/
    JobEndpoints.cs
    ToolEndpoints.cs
    DetectEndpoints.cs
    HealthEndpoints.cs
    EndpointExtensions.cs   — MapAllEndpoints()
  Jobs/
    JobDispatcher.cs
    JobRecord.cs
    IJobStore.cs
    InMemoryJobStore.cs
    JobSweepService.cs
    JobQueueManager.cs
  Processors/
    PdfManipulation/
      MergePdfProcessor.cs
      SplitPdfProcessor.cs
      ReorderPdfProcessor.cs
      RemovePdfPagesProcessor.cs
      RotatePdfProcessor.cs
      WatermarkPdfProcessor.cs
      ProtectPdfProcessor.cs
    Documents/
      PdfToDocxProcessor.cs
      DocxToPdfProcessor.cs
      PdfToImagesProcessor.cs
      ImagesToPdfProcessor.cs
      MarkdownToPdfProcessor.cs
    Images/
      CompressImageProcessor.cs
      RemoveBackgroundProcessor.cs
      SvgConvertProcessor.cs
    Data/
      CsvToXlsxProcessor.cs
    Base/
      LibreOfficeProcessor.cs
      PdfPigProcessor.cs
  Infrastructure/
    ProcessorExtensions.cs
    ProcessorSanityCheck.cs
    LibreOfficeManager.cs
    OnnxModelLoader.cs
    StorageService.cs
    IStorageService.cs
    PdfRenderer.cs
    IPdfRenderer.cs
    RateLimitingExtensions.cs
    SecurityHeadersMiddleware.cs
    SessionTokenMiddleware.cs
    ProblemDetailsExceptionHandler.cs
    SseStreamWriter.cs
  Configuration/
    ApiOptions.cs
    LibreOfficeOptions.cs
    StorageOptions.cs
    RateLimitOptions.cs
  Logging/
    LoggingExtensions.cs
    AuditLogService.cs
  Program.cs
  appsettings.json
  appsettings.Development.json
  Fileway.Api.csproj
```

---

## Fileway.Client

```
src/Fileway.Client/
  Pages/
    Index.razor               — @page "/" — homepage drop zone
    ToolPage.razor            — @page "/tools/{Slug}" — universal tool page
    ToolsDirectory.razor      — @page "/tools" — discovery grid
    NotFound.razor
  Components/
    DropZone/
      SingleFileDropZone.razor
      MultiFileDropZone.razor
      DropZoneBase.cs
    Progress/
      JobProgressBar.razor
      QueuePosition.razor
      SyncProgressSpinner.razor
    Preview/
      PreviewPanel.razor      — routes by PreviewKind
      PdfFirstPagePreview.razor
      SideBySideImagePreview.razor
      SyntaxHighlightPreview.razor
      InlineEditorPreview.razor
    PdfEditor/
      PdfPageEditor.razor
      PdfPageThumbnail.razor
      PdfPageEditorMode.cs
    ToolOptions/
      QualitySlider.razor
      DimensionInputs.razor
      PageRangeSelector.razor
      SplitControls.razor
      WatermarkOptions.razor
      ToolOptionsPanel.razor  — routes by UiHints flags
    Suggestions/
      FormatSuggestionChips.razor
      AlsoTryPanel.razor
    Layout/
      MainLayout.razor
      NavBar.razor
      Footer.razor
    Errors/
      ErrorPanel.razor
      ErrorMessages.cs        — errorCode → (userMessage, suggestedAction) dictionary
    Download/
      DownloadPanel.razor
      DownloadService.cs
  Processors/
    Images/
      ConvertImageProcessor.cs
      CropResizeImageProcessor.cs
      RotateFlipImageProcessor.cs
      CompressImageProcessor.cs
      SvgConvertProcessor.cs
    Data/
      JsonYamlProcessor.cs
      JsonCsvProcessor.cs
      JsonTomlProcessor.cs
      ValidateProcessor.cs
      CsvToXlsxProcessor.cs
    Base/
      ImageSharpProcessor.cs
  Services/
    ProcessorRouter.cs
    ApiJobClient.cs
    SseClient.cs
    DetectionService.cs
    ToolStateService.cs
    ThemeService.cs
  Infrastructure/
    WasmProcessorExtensions.cs
    WasmSanityCheck.cs
  Interop/
    SseClient.js
    FilePicker.js
    ClipboardInterop.js
    DownloadInterop.js
    ThemeInterop.js
  Styles/
    app.css                   — CSS custom properties, reset, base tokens
    components.css
  wwwroot/
    index.html
    favicon.svg
    manifest.webmanifest
  App.razor
  _Imports.razor
  Program.cs
  Fileway.Client.csproj
```

---

## Test Projects

```
tests/Fileway.Tests.Api/
  Processors/
    PdfManipulation/    — one test class per processor
    Documents/
    Images/
    Data/
  Endpoints/
    JobEndpointTests.cs
    ToolEndpointTests.cs
    DetectEndpointTests.cs
  Infrastructure/
    JobDispatcherTests.cs
    JobStoreTests.cs
    RateLimitingTests.cs
  Fixtures/
    TestProgressCollector.cs
    TestFileFactory.cs
    ProcessorContextBuilder.cs
    CorruptedFileFactory.cs
    EmbeddedTestFiles/

tests/Fileway.Tests.Client/
  Processors/
    Images/
    Data/
  Components/
    DropZoneTests.cs
    PdfPageEditorTests.cs
    ToolOptionsPanelTests.cs
    ErrorPanelTests.cs
  Services/
    ProcessorRouterTests.cs
    DetectionServiceTests.cs
  Fixtures/
    TestProgressCollector.cs
    TestFileFactory.cs
    ProcessorContextBuilder.cs
```

---

## Devcontainer

`.devcontainer/devcontainer.json`

| Setting | Value |
|---|---|
| Base Dockerfile | `.devcontainer/Dockerfile` |
| Forwarded ports | 5000 (API), 5001 (Client dev server), 5229 (hot reload) |
| Post-create | `.devcontainer/post-create.sh` |
| Remote user | `vscode` |
| Features | github-cli, git-lfs |

**Extensions (pre-installed):** ms-dotnettools.csharp, ms-dotnettools.csdevkit, ms-dotnettools.blazorwasm-companion, editorconfig.editorconfig, ms-azuretools.vscode-docker, github.vscode-pull-request-github, streetsidesoftware.code-spell-checker

**Dockerfile layers:**
1. `mcr.microsoft.com/devcontainers/dotnet:9.0` base
2. `apt-get install libreoffice-nogui fonts-liberation fonts-dejavu libfontconfig1`
3. Node.js LTS
4. `dotnet workload install wasm-tools`
5. Package restore layer (csproj files copied first for cache efficiency)

**post-create.sh:** `dotnet restore` → `dotnet build` → `dotnet dev-certs https --trust` → `libreoffice --version` (verification)

**ENV vars:** `ASPNETCORE_ENVIRONMENT=Development`, `DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true`
