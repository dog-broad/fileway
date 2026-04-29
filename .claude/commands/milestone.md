# /project:milestone

Show the current milestone status, what has been completed, what remains, and what to build next.

## Usage

- `/project:milestone` — show current milestone status
- `/project:milestone M1` — show M1 status specifically
- `/project:milestone next` — show the next unstarted item to build

## Milestone definitions

### Milestone 1 — Shell + Data Tools
**Goal:** End-to-end working architecture with data format tools. Proves the full stack before adding complexity.

Tools to ship:
- [ ] JSON → YAML (and reverse) — `json-to-yaml` / `yaml-to-json`
- [ ] JSON → CSV (and reverse) — `json-to-csv` / `csv-to-json`
- [ ] JSON → TOML (and reverse) — `json-to-toml` / `toml-to-json`
- [ ] JSON / YAML Validate — `validate`
- [ ] CSV → Excel — `csv-to-xlsx`

Infrastructure to build:
- [ ] Repo scaffold — solution file, all 5 projects, Directory.*.props
- [ ] devcontainer — Dockerfile, devcontainer.json, post-create.sh
- [ ] Fileway.Shared — all types, interfaces, enums, ToolRegistry, FormatDetector
- [ ] Fileway.Api — Program.cs, middleware pipeline, all 7 endpoints (stubs OK for non-data tools)
- [ ] Fileway.Client — App.razor, routing, MainLayout, NavBar, Index page
- [ ] SingleFileDropZone component
- [ ] FormatSuggestionChips component
- [ ] InlineEditorPreview component (split-pane for data tools)
- [ ] SyncProgressSpinner component
- [ ] ErrorPanel component
- [ ] DownloadPanel component
- [ ] SseClient.js + SseClient.cs (needed even if not used by data tools — for M2)
- [ ] All WASM data processors: JsonYamlProcessor, JsonCsvProcessor, JsonTomlProcessor, ValidateProcessor
- [ ] CsvToXlsxProcessor (WASM path)
- [ ] ProcessorSanityCheck passing
- [ ] All processor tests written and passing

**M1 complete when:** All 5 data tools work end-to-end in the browser with correct UX. ProcessorSanityCheck passes. All tests pass.

---

### Milestone 2 — Image Tools
**Goal:** WASM image processing. Proves ImageSharp in WASM and the before/after preview pattern.

Tools to ship:
- [ ] Image format convert — `image-convert`
- [ ] Crop / resize — `image-resize`
- [ ] Rotate / flip — `image-rotate`
- [ ] Compress image — `compress-image`
- [ ] SVG → PNG/PDF — `svg-convert`

Infrastructure to build:
- [ ] ImageSharp WASM integration
- [ ] SideBySideImagePreview component
- [ ] QualitySlider component (UiHints.ShowQualitySlider)
- [ ] DimensionInputs component (UiHints.ShowDimensionInputs)
- [ ] ToolOptionsPanel router component
- [ ] API path for compress-image (large files >20MB)
- [ ] API path for svg-convert (large SVGs)
- [ ] All image WASM processors with tests

**M2 complete when:** All 5 image tools work in the browser. Before/after preview renders correctly. Quality slider updates preview live.

---

### Milestone 3 — PdfPageEditor + PDF Manipulation
**Goal:** The most complex UI component. Proves PDF thumbnail streaming and drag-drop architecture.

Requires first: Docnet.Core (PDFium) integration — read `docs/architecture/12-pdf-rendering.md`
Requires first: LibreOffice in devcontainer — read `docs/architecture/13-libreoffice.md`

Tools to ship:
- [ ] Merge PDFs — `merge-pdf`
- [ ] Split PDF — `split-pdf`
- [ ] Reorder pages — `reorder-pdf`
- [ ] Remove pages — `remove-pdf-pages`
- [ ] Rotate pages — `rotate-pdf`
- [ ] Add watermark — `watermark-pdf`
- [ ] Protect / unlock — `protect-pdf`
- [ ] Compress PDF — `compress-pdf`

Infrastructure to build:
- [ ] Docnet.Core (PDFium) integration — IPdfRenderer, PdfRenderer.cs
- [ ] LibreOffice headless in devcontainer + LibreOfficeManager
- [ ] PdfPageEditor.razor component — drag-drop thumbnail grid
- [ ] PdfPageThumbnail.razor component
- [ ] PdfFirstPagePreview.razor component
- [ ] Thumbnail streaming via SSE (internal job type)
- [ ] PageRangeSelector component (UiHints.ShowPageSelector)
- [ ] SplitControls component (UiHints.ShowSplitControls)
- [ ] WatermarkOptions component
- [ ] All PDF manipulation processors with tests
- [ ] MultiFileDropZone component (for merge-pdf)

**M3 complete when:** All 8 PDF manipulation tools work. PdfPageEditor drag-and-drop reorders pages and the result is correctly applied to the output PDF.

---

### Milestone 4 — Document Conversion + Remove Background
**Goal:** Heavy API-side processing. The most impressive tools.

Tools to ship:
- [ ] PDF → Word — `pdf-to-docx`
- [ ] Word → PDF — `docx-to-pdf`
- [ ] PDF → Images — `pdf-to-images`
- [ ] Images → PDF — `images-to-pdf`
- [ ] Markdown → PDF — `md-to-pdf`
- [ ] Remove background — `remove-bg`

Infrastructure to build:
- [ ] ONNX Runtime integration + RMBG model loading (OnnxModelLoader.cs)
- [ ] Markdig + Puppeteer headless for MD→PDF
- [ ] AlsoTryPanel.razor (post-conversion suggestions)
- [ ] All document conversion processors with tests

**M4 complete when:** All 6 document conversion tools work. Remove-bg produces clean transparent PNG output.

---

### Milestone 5 — Polish + Release Readiness
**Goal:** Production-quality release. Everything hardened.

Tasks:
- [ ] Security hardening — zip bomb detection, polyglot rejection, output validation
- [ ] StaticGen tool — build and integrate into CI
- [ ] sitemap.xml and robots.txt generation
- [ ] Per-tool SEO prerendered HTML
- [ ] JSON-LD structured data per tool
- [ ] /tools discovery page with search and filtering
- [ ] Full mobile UX pass — all components at 375px
- [ ] Accessibility audit — WCAG 2.1 AA
- [ ] Dark mode verification across all components
- [ ] Performance audit — WASM bundle size, first load time
- [ ] GitHub Actions CI pipeline — build, test, publish
- [ ] GitHub Actions release pipeline — Docker image on tag
- [ ] Production Dockerfile finalised
- [ ] README.md complete — setup, contributing, architecture link
- [ ] SECURITY.md — vulnerability reporting instructions
- [ ] All tests passing

**M5 complete when:** App is deployable, all tools work, SEO pages are generated, CI passes, security is hardened.

---

## How to assess current status

To determine what milestone you are in and what to do next:

1. Check which M1 infrastructure items exist in the codebase
2. Check which processor classes exist and have passing tests
3. The next item to build is the first unchecked box in the current milestone

Report format:
```
Current milestone: M{N}
Completed items:   {count}/{total}
Next to build:     {specific item}
Blockers:          {any deviations found by check-sot}
```
