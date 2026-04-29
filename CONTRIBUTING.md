# Contributing to Fileway

Thank you for contributing. This document covers how the project is structured, how to add new tools or formats, and what the PR checklist expects.

---

## Project structure

\`\`\`
src/
  Fileway.Shared/     — shared types, interfaces, tool registry, format detection
  Fileway.Api/        — ASP.NET Core minimal API, processors, job dispatcher
  Fileway.Client/     — Blazor WASM frontend, components, WASM processors
tests/
  Fileway.Tests.Api/
  Fileway.Tests.Client/
docs/
  architecture/       — technical documentation, one file per concern
\`\`\`

Full detail in \`docs/architecture/05-solution-structure.md\`.

---

## Adding a new tool

Read \`docs/architecture/01-tool-registry.md\` and \`docs/architecture/04-processors.md\` before starting.

1. Add a \`ToolDefinition\` record to the correct file in \`src/Fileway.Shared/Tools/Definitions/\`
2. Confirm all referenced \`FileFormat\` values exist in \`FileFormats.cs\`
3. Create the processor in \`src/Fileway.Api/Processors/{Category}/\` implementing \`IApiProcessor\`
4. Register it in \`src/Fileway.Api/Infrastructure/ProcessorExtensions.cs\`
5. Write tests — see minimum bar below
6. Start the app — \`ProcessorSanityCheck\` crashes on misconfiguration with a clear message
7. If the tool has a WASM path, implement \`IWasmProcessor\` in \`src/Fileway.Client/Processors/{Category}/\`

Adding a tool automatically gives you: routing, detection suggestions, /tools discovery card, SEO meta, sitemap entry, API validation. No extra wiring needed.

---

## Adding a new file format

Read \`docs/architecture/06-detection.md\` before starting.

1. Add a \`FileFormat\` record to \`src/Fileway.Shared/Formats/FileFormats.cs\`
2. Add magic byte signatures with masks for formats with variable bytes
3. For text-ambiguous formats, add \`DetectionHints\` and set \`CanBeDetected = false\`
4. Update \`FormatDetector.cs\` if secondary detection logic is needed
5. Add detection tests

---

## Minimum test bar — per processor

Every processor must have tests covering:

1. Happy path — valid input produces output bytes that pass magic byte check for the output format
2. Corrupted input — produces \`ProcessorDomainException\` with the correct \`ErrorCode\`
3. Invalid options — \`ValidateOptions()\` throws \`ProcessorValidationException\`
4. Pre-cancelled token — \`ExecuteAsync()\` propagates \`OperationCanceledException\`
5. Progress events — correct stage order, \`OverallPercent\` non-decreasing 0→100
6. Output filename — non-empty, correct extension, no path separators

---

## Code conventions

- One type per file. Filename matches the type name exactly.
- Namespace matches the folder path
- All async methods end with \`Async\`
- Private fields use \`_camelCase\`
- All error codes are constants in \`Fileway.Shared/Errors/ErrorCodes.cs\` — no inline strings
- No hardcoded configuration values — everything via strongly typed options classes
- No file content, filenames, or raw IP addresses in log calls

---

## Pull request expectations

- \`dotnet build\` passes with zero warnings
- \`dotnet test\` passes
- Every new processor has all 6 required tests
- No deviation from \`docs/architecture/\` without documenting the change in the ADR table in \`00-overview.md\`
