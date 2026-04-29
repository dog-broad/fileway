# PDF Rendering Strategy

**Milestone:** 3 — introduce when PDF manipulation tools are built.

PdfPig handles PDF structure (merge, split, reorder, extract text, write). It **cannot render pages to pixels**. Docnet.Core handles rendering.

---

## Library: Docnet.Core (PDFium bindings)

**License:** Apache 2.0 — compatible with MIT project  
**Engine:** PDFium — same renderer used by Chrome  
**Integration:** In-process .NET bindings — no process spawning  
**Native binary:** ~25MB. Included automatically via NuGet for `linux-x64` RuntimeIdentifier.

**Critical:** `Fileway.Api.csproj` must set `<RuntimeIdentifier>linux-x64</RuntimeIdentifier>`. Without this, the native PDFium binary is not included in publish output → `DllNotFoundException` at runtime inside Docker.

**Pixel → image conversion:** ImageSharp (already a dependency) converts raw pixel buffers to PNG or JPEG.

---

## IPdfRenderer Interface

`Fileway.Api/Infrastructure/IPdfRenderer.cs`

| Method | Returns | Notes |
|---|---|---|
| `RenderFirstPage(bytes, dpi, format)` | `byte[]` | 150 DPI, JPEG 85%. Optimised for preview latency. |
| `RenderPage(bytes, pageIndex, dpi, format)` | `byte[]` | Single page. Used by pdf-to-images tool. |
| `RenderAllPages(bytes, dpi, format, ct)` | `IAsyncEnumerable<RenderedPage>` | Streams pages one by one. Processor writes to ZIP progressively. |
| `RenderThumbnails(bytes, width, ct)` | `IAsyncEnumerable<PageThumbnail>` | 72 DPI, 160px wide, JPEG 70%. Streams thumbnails for PdfPageEditor. |
| `GetPageCount(bytes)` | `int` | Fast metadata read — no rendering. Validates page ranges. |

---

## RenderedPage and PageThumbnail

**RenderedPage:** `PageIndex` (int), `ImageBytes` (byte[]), `WidthPx` (int), `HeightPx` (int), `Format` (FileFormat)

**PageThumbnail:** `PageIndex` (int), `PageNumber` (int — 1-based, shown in UI), `Base64Jpeg` (string — directly usable as `img src=` in Blazor), `WidthPx` (int — always 160), `HeightPx` (int — varies by aspect ratio)

---

## DPI Settings

| Use case | DPI | Format | Quality |
|---|---|---|---|
| PdfPageEditor thumbnails | 72 | JPEG | 70% |
| First-page inline preview | 150 | JPEG | 85% |
| PDF→images (screen quality) | 150 | PNG | — |
| PDF→images (print quality) | 300 | PNG | — (user-selectable) |

---

## PdfPageEditor Thumbnail Streaming

Thumbnail rendering is an internal API call, not a public tool in the ToolRegistry.

```
User drops PDF on reorder-pdf / remove-pdf-pages / split-pdf / rotate-pdf
  → ToolPage.razor calls thumbnail render internally
    → POST to internal thumbnail endpoint (not /api/v1/jobs — separate internal route)
      → RenderThumbnails() streams PageThumbnail via SSE
        → Each thumbnail arrives → Blazor appends PdfPageThumbnail card to grid
          → Grid fills progressively — no wait for full render
```

Thumbnails use the same SSE infrastructure as regular jobs. `Base64Jpeg` in `PageThumbnail` is directly set as `img src=` — no additional encoding step in Blazor.

---

## Concurrency

`Parallel.ForEachAsync` with degree of parallelism = 4 per render job. Multiple pages rendered simultaneously. `IAsyncEnumerable` yields each completed page immediately — fastest page to render appears first in the Blazor grid (not necessarily page 1 first, but page thumbnails display in correct order client-side).
