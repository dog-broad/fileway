# CLAUDE.md — Fileway AI Working Instructions

This file is the contract between you (Claude) and this project. Read it fully at the start of every session. Every decision referenced here has been planned and locked. Do not improvise architecture — consult the source of truth documents.

---

## Project Identity

**Name:** Fileway  
**Tagline:** Any file. Any format. One way.  
**Purpose:** Document and file conversion/manipulation Swiss-army tool  
**License:** MIT — open source from day one  
**Stack:** Blazor WASM (frontend) + ASP.NET Core minimal API (backend) + .NET 9  
**Dev environment:** GitHub Codespaces, Linux (Debian Bookworm), devcontainer

---

## Source of Truth — Document Map

Every architectural decision lives in one of these files. When you are unsure about anything, read the relevant document before writing code. Never guess at architecture.

| Document | Covers |
|---|---|
| `docs/architecture/00-overview.md` | System map, project graph, milestone order, key decisions summary |
| `docs/architecture/01-tool-registry.md` | ToolDefinition record, FileFormat, all enums, ToolRegistry query API |
| `docs/architecture/02-job-model.md` | SSE, job lifecycle states, Channel pipe, two tiers, job store |
| `docs/architecture/03-api-surface.md` | All 7 endpoints, request/response shapes, error codes, session token |
| `docs/architecture/04-processors.md` | IApiProcessor, IWasmProcessor, JobDispatcher, ProcessorRouter, exceptions |
| `docs/architecture/05-solution-structure.md` | Projects, folder layout, namespaces, naming conventions, devcontainer |
| `docs/architecture/06-detection.md` | FormatDetector, magic bytes table, text heuristics, 3-pass pipeline |
| `docs/architecture/07-error-model.md` | Error copy, propagation pipeline, retryable UX, ErrorMessages.cs |
| `docs/architecture/08-testing.md` | Test pyramid, frameworks, fixtures, coverage bar, CI enforcement |
| `docs/architecture/09-ui-design.md` | CSS tokens, dark mode, mobile-first, breakpoints, accessibility rules |
| `docs/architecture/10-observability.md` | Serilog, structured events, privacy rules, correlation, log shape |
| `docs/architecture/11-rate-limiting.md` | Policies, limits, dual-keyed, freemium hooks, ITierResolver stub |
| `docs/architecture/12-pdf-rendering.md` | Docnet.Core, IPdfRenderer interface, DPI table, streaming thumbnails |
| `docs/architecture/13-libreoffice.md` | Containerisation, invocation model, UserInstallation isolation, cleanup |
| `docs/architecture/14-seo.md` | StaticGen tool, prerendering, sitemap, JSON-LD, Blazor hydration |

---

## The Prime Directive

**The source of truth documents are locked decisions. Code must conform to them — not the other way around.**

When you encounter a conflict between what is in a source of truth document and what exists in the codebase:

1. **Stop.** Do not patch around the deviation.
2. **Identify** which document the deviation conflicts with.
3. **Report** the deviation clearly: what the document says, what the code does, and why they differ.
4. **Ask** the developer: fix the code to match the document, or update the document to reflect an intentional change?
5. **Never** silently update a source of truth document without the developer explicitly approving the change.
6. **Never** proceed past a deviation without resolving it — deviations compound.

---

## What You Must Always Do

- **Read the relevant SoT doc before writing any code** in that domain. If building a processor, read `04-processors.md`. If building an endpoint, read `03-api-surface.md`. No exceptions.
- **One type per file.** Filename must match the type name exactly. `ToolDefinition.cs` contains only `ToolDefinition`.
- **Namespace = folder path.** `Fileway.Api/Processors/PdfManipulation/` → namespace `Fileway.Api.Processors.PdfManipulation`.
- **Async suffix on all async methods.** `ExecuteAsync`, `DispatchAsync`, `RenderAsync`. No exceptions.
- **Underscore prefix on private fields.** `_jobStore`, `_logger`, `_registry`.
- **Processors throw typed exceptions only.** `ProcessorValidationException`, `ProcessorDomainException`, `ProcessorUnexpectedException`. Never return null or error codes. Never swallow exceptions.
- **All error codes come from `ErrorCodes.cs`.** No inline error code strings anywhere else.
- **Every processor must have a test class before it is considered complete.** Tests live in `Fileway.Tests.Api/Processors/` or `Fileway.Tests.Client/Processors/`.
- **Respect CancellationToken everywhere.** Pass it through at every await and check it at every long loop.
- **No file content, filenames, or raw IPs in logs.** Privacy rules are non-negotiable. See `10-observability.md`.

---

## What You Must Never Do

- **Never reference Fileway.Api from Fileway.Client or vice versa.** They both reference Fileway.Shared only.
- **Never reference Fileway.Api or Fileway.Client from Fileway.Shared.** Shared has zero project references.
- **Never hardcode rate limit values, timeout values, or size limits.** All come from config via strongly typed options classes.
- **Never use iText for PDF operations.** It is AGPL — license incompatible. Use PdfPig.
- **Never use Ghostscript for rendering.** AGPL. Use Docnet.Core (PDFium).
- **Never log file content, original filenames, raw IP addresses, full session tokens, signed URLs, or toolOptions values.**
- **Never use UseShellExecute: true when spawning LibreOffice.** Shell injection risk.
- **Never let a processor touch System.Diagnostics.Process directly.** All process management goes through LibreOfficeManager.
- **Never add version numbers to individual .csproj files.** All package versions live in Directory.Packages.props.
- **Never write business logic in API route handlers.** Route handlers validate, delegate to a service/dispatcher, return. Nothing more.
- **Never use MudBlazor, Radzen, or any component library.** UI is custom CSS with design tokens. See `09-ui-design.md`.
- **Never use localStorage.** Session tokens live in sessionStorage. Tab-scoped by design.
- **Never create a tool without a ToolDefinition record in the registry.** No tool exists in the app without a registry entry.
- **Never use string concatenation to build file paths.** Always Path.Combine().

---

## How to Add a New Tool — Mandatory Sequence

Read `01-tool-registry.md` and `04-processors.md` fully before starting.

1. Add `ToolDefinition` record to the correct category file in `Fileway.Shared/Tools/Definitions/`
2. Ensure all referenced `FileFormat` values exist in `FileFormats.cs`
3. Set `ProcessorType` field — matches the processor class you will create
4. Set `ProgressStages` — must exactly match what the processor will emit
5. Add `RelatedSlugs` — and update related tools to include this slug back
6. Create processor class in `Fileway.Api/Processors/{Category}/` implementing `IApiProcessor`
7. Register processor in `Fileway.Api/Infrastructure/ProcessorExtensions.cs`
8. Write unit tests — minimum bar defined in `08-testing.md`
9. Run the app — `ProcessorSanityCheck` will crash startup if anything is misconfigured
10. If WASM path needed: create `IWasmProcessor` in `Fileway.Client/Processors/{Category}/`, register in `WasmProcessorExtensions.cs`

Nothing is automatic. Each step is required.

---

## How to Add a New FileFormat — Mandatory Sequence

Read `06-detection.md` before starting.

1. Add `FileFormat` record to `FileFormats.cs` with all fields: Id, DisplayName, MimeTypes, Extensions, MagicBytes (MagicSignature[]), FormatCategory, CanBeDetected, DetectionHints (if text-ambiguous), MaxFileSizeBytes, IsTextBased, PreviewKind
2. If magic bytes detection applies, add the MagicSignature with correct offset, bytes, and mask
3. If text-ambiguous (JSON/YAML/CSV/TOML etc.), add DetectionHints patterns and set CanBeDetected = false
4. Update FormatDetector if secondary detection logic is needed for this format
5. Add test cases to FormatDetector tests covering detection of this format

---

## How to Handle a Deviation from Source of Truth

When you find code that does not match a SoT document:

```
⚠️ DEVIATION DETECTED

Document: docs/architecture/[XX-name.md]
Expected: [what the document specifies]
Found:    [what the code actually does]
Impact:   [what breaks or risks arise from this]

Options:
  A) Fix the code to match the document (recommended if unintentional)
  B) Update the document to reflect an intentional architectural change

Which do you want to do?
```

Do not proceed with new work until the deviation is resolved.

---

## Milestone Order

| Milestone | Focus | Key deliverable |
|---|---|---|
| M1 | Shell + data tools | Scaffold, routing, detection, tool registry, JSON/YAML/CSV/TOML tools |
| M2 | Image tools | ImageSharp WASM, convert/resize/compress/rotate, before-after preview |
| M3 | PdfPageEditor + PDF manipulation | Thumbnail streaming, drag-drop grid, merge/split/reorder/rotate/remove |
| M4 | Document conversion + remove-bg | LibreOffice, DOCX↔PDF, PDF→images, ONNX remove-bg |
| M5 | Polish + release | Security hardening, SEO prerendering, StaticGen, deploy pipeline |

---

## Tech Stack Quick Reference

| Layer | Technology |
|---|---|
| Frontend runtime | Blazor WASM, .NET 9 |
| Backend runtime | ASP.NET Core minimal API, .NET 9 |
| PDF manipulation | PdfPig (Apache 2.0) |
| PDF rendering | Docnet.Core / PDFium (Apache 2.0) |
| Image processing | ImageSharp (Six Labors Split License — free for OSS) |
| SVG rendering | Svg.Skia / SkiaSharp |
| DOCX/PDF conversion | LibreOffice headless (process-level) |
| Data formats | YamlDotNet, CsvHelper, Tomlyn, ClosedXML |
| AI/ML (remove-bg) | ONNX Runtime + RMBG model |
| Object storage | Cloudflare R2 (via AWSSDK.S3) |
| Logging | Serilog → stdout JSON |
| Rate limiting | ASP.NET Core built-in middleware |
| Testing | xUnit, bUnit, WebApplicationFactory, NSubstitute, FluentAssertions |
| CSS | Custom design tokens — no component library |

---

## Session Start Checklist

At the start of every working session, before writing any code:

1. Identify what you are building (which milestone, which tool, which layer)
2. Read the relevant SoT document(s) for that domain
3. Check if any existing code deviates from those documents
4. Only then begin implementing

---

## CSS Design Token Quick Reference

Dark mode via `[data-theme]` on `<html>`. Set by `ThemeInterop.js` before Blazor loads.  
Mobile-first: 375px base, `min-width` media queries only. All tap targets ≥ 48×48px.  
Full token definitions: `docs/architecture/09-ui-design.md`

| Token | Dark | Light |
|---|---|---|
| `--color-bg-primary` | `#0f172a` | `#ffffff` |
| `--color-bg-secondary` | `#1e293b` | `#f8fafc` |
| `--color-accent` | `#2563eb` | `#2563eb` |
| `--color-text-primary` | `#f1f5f9` | `#0f172a` |
| `--color-text-secondary` | `#94a3b8` | `#64748b` |
| `--color-border` | `#334155` | `#e2e8f0` |
| `--font-sans` | `Inter, system-ui, sans-serif` | same |
| `--font-mono` | `JetBrains Mono, monospace` | same |

---

## API Quick Reference

Base: `/api/v1`  
Session token: `X-Session-Token: {uuid-v4}` header on every request  
Errors: RFC 9457 ProblemDetails + `errorCode` + `userMessage` + `suggestedAction` + `retryable`

| Method | Path | Purpose |
|---|---|---|
| POST | `/api/v1/jobs` | Submit job (all tools) |
| GET | `/api/v1/jobs/{id}/progress` | SSE stream |
| GET | `/api/v1/tools` | List tools |
| GET | `/api/v1/tools/{slug}` | Single tool |
| POST | `/api/v1/detect` | Server-side format detection |
| GET | `/health/live` | Liveness probe |
| GET | `/health/ready` | Readiness probe |

Full contracts: `docs/architecture/03-api-surface.md`
