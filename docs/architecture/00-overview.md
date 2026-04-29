# Fileway — Architecture Overview

**Tagline:** Any file. Any format. One way. | **License:** MIT | **.NET 9**

---

## Project Dependency Graph

```
Fileway.Tests.Api      Fileway.Tests.Client
        ↓                       ↓
  Fileway.Api           Fileway.Client
        ↘                      ↙
              Fileway.Shared
```

| Project | SDK | Root namespace |
|---|---|---|
| `Fileway.Shared` | classlib | `Fileway.Shared` |
| `Fileway.Api` | web | `Fileway.Api` |
| `Fileway.Client` | blazorwasm | `Fileway.Client` |
| `Fileway.Tests.Api` | xunit | `Fileway.Tests.Api` |
| `Fileway.Tests.Client` | xunit | `Fileway.Tests.Client` |

`tools/StaticGen/` — standalone publish-time CLI. Not in solution file. References Fileway.Shared only.

**Hard rules:** Shared has zero project references. Api ↔ Client never reference each other.

---

## Processing Model

**Hybrid:** WASM fast path for simple/small ops, API for heavy ops. Decision made client-side by `ProcessorRouter`.

| Tier | Duration | Transport | Output delivery |
|---|---|---|---|
| Synchronous | < 2s | HTTP POST → 200 with result | base64 inline (< 5MB) or R2 signed URL (≥ 5MB) |
| Async | > 2s | POST → 202 + JobId, then SSE stream | R2 signed URL, 30-min TTL |

---

## V1 Tool Scope — 23 Tools, 5 Categories

| Category | Count | Processing |
|---|---|---|
| PDF Manipulation | 7 | ApiOnly |
| Image Manipulation | 4 | WASM / WasmPreferred |
| Document Conversion | 5 | ApiOnly |
| Image Conversion | 2 | WASM / WasmPreferred |
| Data Formats | 5 | WasmOnly / WasmPreferred |

**PDF Manipulation:** merge-pdf, split-pdf, reorder-pdf, remove-pdf-pages, rotate-pdf, watermark-pdf, protect-pdf  
**Image Manipulation:** image-resize, image-rotate, compress-image, remove-bg  
**Document Conversion:** pdf-to-docx, docx-to-pdf, pdf-to-images, images-to-pdf, md-to-pdf  
**Image Conversion:** image-convert, svg-convert  
**Data Formats:** json-to-yaml (bidirectional), json-to-csv (bidirectional), json-to-toml (bidirectional), validate, csv-to-xlsx

---

## Milestone Order

| Milestone | Scope | Gate |
|---|---|---|
| M1 | Scaffold + data tools | 5 data tools working end-to-end |
| M2 | Image tools | 5 image tools, before/after preview |
| M3 | PdfPageEditor + PDF manipulation | Drag-drop thumbnails, 8 PDF tools |
| M4 | Document conversion + remove-bg | LibreOffice, ONNX, 6 tools |
| M5 | Polish + release | SEO, security hardening, CI/CD |

---

## Key Decisions — One-Line Summary

| Item | Decision |
|---|---|
| Frontend | Blazor WASM — no server-side rendering |
| Progress delivery | Server-Sent Events (SSE) — not SignalR, not polling |
| Job store (v1) | ConcurrentDictionary in-memory — IJobStore interface for Redis in v2 |
| PDF manipulation | PdfPig (Apache 2.0) — not iText (AGPL) |
| PDF rendering | Docnet.Core / PDFium (Apache 2.0) — not Ghostscript (AGPL) |
| Document conversion | LibreOffice headless — fresh process per job, baked into API image |
| Object storage | Cloudflare R2 — zero egress fees |
| Auth | Anonymous only — ephemeral UUID session token in sessionStorage |
| UI | Custom CSS design tokens — no component library |
| Logging | Serilog → stdout JSON — no external sink in v1 |
| Rate limiting | ASP.NET Core built-in — sliding window, dual-keyed (session + IP hash) |
| License | MIT |
| SEO | Static HTML prerendering at publish time via StaticGen CLI |

---

## Repo Root Structure

```
fileway/
  .claude/              — AI working instructions and commands
  .devcontainer/        — devcontainer.json, Dockerfile, post-create.sh
  .github/              — workflows/, ISSUE_TEMPLATE/, pull_request_template.md
  src/
    Fileway.Shared/
    Fileway.Api/
    Fileway.Client/
  tests/
    Fileway.Tests.Api/
    Fileway.Tests.Client/
  tools/
    StaticGen/          — publish-time SEO HTML generator
  docker/
    Dockerfile.api      — production image
    .dockerignore
  docs/
    architecture/       — these files
  Fileway.sln
  Directory.Build.props
  Directory.Packages.props
  .editorconfig
  LICENSE               — MIT
  README.md
  CONTRIBUTING.md
  SECURITY.md
  CODE_OF_CONDUCT.md
```

---

## Architecture Decision Records

_Append new ADRs here when intentional deviations from original planning are approved._

| Date | Change | Reason | Doc updated |
|---|---|---|---|
| — | — | — | — |
