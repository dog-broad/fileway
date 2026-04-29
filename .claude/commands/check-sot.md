# /project:check-sot

Audit the current state of the codebase against all source of truth documents. Run this at the start of any session, before a PR, or whenever something feels off.

## What this command does

Systematically compares what exists in the codebase against what the architecture documents specify. Reports every deviation with its severity and resolution options.

## How to run

Provide a scope:
- `/project:check-sot` — full audit of everything built so far
- `/project:check-sot processors` — audit processor implementations only
- `/project:check-sot api` — audit API endpoints and contracts only
- `/project:check-sot registry` — audit ToolDefinition records only
- `/project:check-sot naming` — audit naming conventions only

## Full audit checklist

### Project structure (ref: 05-solution-structure.md)
- [ ] Five projects exist: Shared, Api, Client, Tests.Api, Tests.Client
- [ ] No project references violate the dependency graph (Shared ← Api/Client only)
- [ ] Directory.Build.props and Directory.Packages.props exist at root
- [ ] No version numbers in individual .csproj files
- [ ] Folder structure matches the spec for each project

### Naming conventions (ref: 05-solution-structure.md)
- [ ] One type per file — no files containing multiple types
- [ ] Filename matches type name exactly
- [ ] Namespace matches folder path (Fileway.Api.Processors.PdfManipulation etc.)
- [ ] All async methods end with Async suffix
- [ ] All private fields use _camelCase prefix
- [ ] All processors end with Processor suffix
- [ ] All services end with Service suffix
- [ ] All endpoint classes end with Endpoints suffix
- [ ] No hardcoded configuration values — all via options classes

### Tool registry (ref: 01-tool-registry.md)
- [ ] Every registered tool has a ToolDefinition record in Definitions/
- [ ] Every ToolDefinition has all required fields populated (no nulls where not allowed)
- [ ] ProcessorType is set for all non-WasmOnly tools
- [ ] ProgressStages is non-empty for all Async JobTier tools
- [ ] No duplicate slugs in the registry
- [ ] All RelatedSlugs resolve to known slugs
- [ ] All AcceptedFormats and OutputFormats reference existing FileFormat values

### Processors (ref: 04-processors.md)
- [ ] Every non-WasmOnly tool has a registered IApiProcessor
- [ ] Every WasmOnly tool has a registered IWasmProcessor
- [ ] All processors registered in ProcessorExtensions.cs or WasmProcessorExtensions.cs
- [ ] No processor contains System.Diagnostics.Process (only LibreOfficeManager allowed)
- [ ] No processor catches all exceptions without rethrowing typed
- [ ] CancellationToken passed through at every async call site
- [ ] ProgressStages emitted by processor match ToolDefinition exactly

### API surface (ref: 03-api-surface.md)
- [ ] Exactly 7 endpoints, no more, no less
- [ ] All endpoints under /api/v1 prefix (except /health)
- [ ] X-Session-Token validated on all job endpoints
- [ ] All error responses use ProblemDetails with errorCode, userMessage, suggestedAction, retryable
- [ ] No business logic in route handlers — delegated to services/dispatcher
- [ ] Middleware order matches spec: HTTPS → security headers → CORS → size limit → rate limit → logging → exception handler → routing

### Error handling (ref: 07-error-model.md)
- [ ] All error codes used in code exist in ErrorCodes.cs
- [ ] No inline error code strings outside ErrorCodes.cs
- [ ] All user-facing copy lives in ErrorMessages.cs in Fileway.Client
- [ ] No stack traces or internal exception details returned to client

### Security (ref: CLAUDE.md — never do list)
- [ ] No iText references anywhere
- [ ] No Ghostscript references anywhere
- [ ] No UseShellExecute: true in any Process usage
- [ ] No raw IP addresses in any log call
- [ ] No file content in any log call
- [ ] No localStorage usage anywhere (sessionStorage only)
- [ ] No hardcoded secrets or credentials

### Testing (ref: 08-testing.md)
- [ ] Every processor class has a corresponding test class
- [ ] All 6 minimum tests exist per processor (happy path, corrupted, invalid options, cancelled, progress order, filename)
- [ ] TestProgressCollector used for all progress assertions
- [ ] No test classes use Thread.Sleep — async tests only

### UI (ref: 09-ui-design.md)
- [ ] No component library imports (no MudBlazor, Radzen, etc.)
- [ ] No hardcoded colour values — all via CSS custom properties
- [ ] All interactive elements have visible focus styles
- [ ] All tap targets are minimum 48×48px
- [ ] No hover-only interactions without keyboard/touch equivalent
- [ ] Dark mode uses [data-theme] attribute, not JavaScript colour switching

## Reporting format

For each deviation found, report:

```
⚠️ DEVIATION — {severity: Critical | Major | Minor}

Location:  {file path}
Document:  docs/architecture/{XX-name.md}
Expected:  {what the document says}
Found:     {what the code actually does}
Risk:      {what breaks or could break}

Resolution:
  A) Fix code to match document
  B) Update document (requires explicit developer approval)
```

After reporting all deviations, provide a summary count by severity and recommended resolution order (Critical first).

## After the audit

- All Critical deviations must be resolved before any new feature work
- Major deviations should be resolved before the current milestone is considered complete
- Minor deviations should be tracked and resolved before release
