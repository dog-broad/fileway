# UI Design System

No component library. Custom CSS with design tokens. Full control. Zero bundle overhead.

---

## Dark Mode

Implemented via `[data-theme]` attribute on `<html>`. Set by `ThemeInterop.js` before Blazor loads (prevents flash of unstyled content). All CSS custom properties defined under `[data-theme="dark"]` and `[data-theme="light"]` selectors. One attribute change flips every token.

`ThemeService.cs` in Blazor exposes `Toggle()`. Persists preference to localStorage.

---

## Design Personality

Linear meets Vercel. Clean, precise, confident. Not playful, not corporate. Dark mode is the hero treatment. Utility with taste.

---

## CSS Custom Properties — Full Token Set

### Colours

| Token | Dark | Light |
|---|---|---|
| `--color-bg-primary` | `#0f172a` | `#ffffff` |
| `--color-bg-secondary` | `#1e293b` | `#f8fafc` |
| `--color-bg-elevated` | `#1e293b` | `#ffffff` |
| `--color-accent` | `#2563eb` | `#2563eb` |
| `--color-accent-hover` | `#1d4ed8` | `#1d4ed8` |
| `--color-success` | `#16a34a` | `#16a34a` |
| `--color-danger` | `#dc2626` | `#dc2626` |
| `--color-warning` | `#d97706` | `#d97706` |
| `--color-border` | `#334155` | `#e2e8f0` |
| `--color-text-primary` | `#f1f5f9` | `#0f172a` |
| `--color-text-secondary` | `#94a3b8` | `#64748b` |
| `--color-text-muted` | `#475569` | `#94a3b8` |

### Spacing

`--space-1: 4px` `--space-2: 8px` `--space-3: 12px` `--space-4: 16px` `--space-6: 24px` `--space-8: 32px` `--space-12: 48px` `--space-16: 64px`

### Border Radius

`--radius-sm: 4px` `--radius-md: 8px` `--radius-lg: 12px` `--radius-xl: 16px` `--radius-full: 9999px`

### Typography

`--font-sans: Inter, system-ui, sans-serif` (self-hosted — no Google Fonts CDN)  
`--font-mono: JetBrains Mono, monospace` (self-hosted)

| Token | Size / Line height |
|---|---|
| `--text-xs` | 11px / 1.5 |
| `--text-sm` | 13px / 1.5 |
| `--text-base` | 15px / 1.6 |
| `--text-lg` | 18px / 1.4 |
| `--text-xl` | 22px / 1.3 |
| `--text-2xl` | 28px / 1.2 |
| `--font-weight-normal` | 400 |
| `--font-weight-medium` | 500 |
| `--font-weight-bold` | 600 |

---

## Mobile-First

**Baseline viewport:** 375px. Design here first, then expand.  
**Media query direction:** `min-width` only. Never `max-width`.

| Breakpoint | Token | Width |
|---|---|---|
| Mobile (base) | — | 375px |
| Small | `--bp-sm` | 640px |
| Medium | `--bp-md` | 768px |
| Large | `--bp-lg` | 1024px |
| XL | `--bp-xl` | 1280px |

---

## Accessibility Rules (WCAG 2.1 AA target)

- All tap targets minimum **48×48px** (WCAG 2.5.5)
- All interactive elements have **visible focus styles**: `outline: 2px solid var(--color-accent)`
- No hover-only interactions — all hover states have equivalent keyboard/touch behaviour
- Drag-and-drop always has a **tap-to-browse fallback**
- PdfPageEditor drag-and-drop has a **reorder-by-tap alternative** on mobile
- **ARIA live regions** on progress events — screen readers announce stage changes
- Color is **never the only indicator of state** — always paired with text or icon
- Semantic HTML: `<main>`, `<nav>`, `<button>`, `<h1>`-`<h6>` used correctly

---

## Format Identity System

Every file format has a **visual identity** — a distinct SVG icon and a tint color pair — used consistently everywhere a format appears in the UI. The format pair `[A] → [B]` is the primary visual identity of each conversion tool.

### Format Tint Tokens

Defined in `app.css` under both `[data-theme]` blocks. One pair per format:

| Token | Purpose |
|---|---|
| `--format-{id}-bg` | Soft background (low opacity tint) |
| `--format-{id}-fg` | Foreground — icon, label, and border color |

**Complete V1 format identity plan** (tokens + icon branch added when the format is registered in `FileFormats.cs`):

| Format | Color family | Rationale | Milestone |
|---|---|---|---|
| `json` | Amber `#fbbf24` | Warm, ubiquitous | M1 ✓ |
| `yaml` | Violet `#a78bfa` | Structured config | M1 ✓ |
| `csv` | Emerald `#34d399` | Tabular data | M1 ✓ |
| `toml` | Sky `#38bdf8` | Config, Rust-adjacent | M1 ✓ |
| `xlsx` | Teal `#2dd4bf` | Excel-adjacent | M1 ✓ |
| `txt` | Zinc `#a1a1aa` | Plain, unformatted | M1 (no tool yet) |
| `md` | Purple `#c084fc` | Docs, Markdown hash | M4 |
| `pdf` | Red `#f87171` | Adobe brand | M3 |
| `png` | Rose `#fb7185` | Lossless raster | M2 |
| `jpg` | Orange `#fb923c` | Photographic warmth | M2 |
| `webp` | Lime `#a3e635` | Modern, Google | M2 |
| `gif` | Fuchsia `#e879f9` | Animated | M2 |
| `svg` | Indigo `#818cf8` | Vector, precise | M2 |
| `docx` | Blue `#60a5fa` | Word brand | M4 |

Fallback when a format has no tokens: `color-mix(in srgb, var(--color-accent) 12%, transparent)` for bg, `var(--color-accent)` for fg.

### Format Icons

`FormatIcon.razor` — renders a 24×24-viewBox SVG whose paths encode the format's **structure**, not a logo:

| Format | Icon concept | Milestone |
|---|---|---|
| `json` | Curly braces `{ }` | M1 ✓ |
| `yaml` | Three staggered-indent lines | M1 ✓ |
| `csv` | 3×3 table grid | M1 ✓ |
| `toml` | `[ ]` bracket wrapping key=value lines | M1 ✓ |
| `xlsx` | Spreadsheet grid with filled header row | M1 ✓ |
| `txt` | Three plain horizontal lines | M1 |
| `md` | `#` hash symbol (two vertical + two horizontal bars) | M4 |
| `pdf` | Document with fold + two content lines | M3 |
| `png` | Raster image frame — mountain + circle | M2 |
| `jpg` | Same raster frame — color differentiates | M2 |
| `webp` | Same raster frame — color differentiates | M2 |
| `gif` | Same raster frame — color differentiates | M2 |
| `svg` | Two anchor points with a bezier curve between them | M2 |
| `docx` | Document with fold + three text lines | M4 |
| fallback | Generic file icon | always |

**Raster image formats** (`png`, `jpg`, `webp`, `gif`) intentionally share the same icon path — the tint color is the primary differentiator, and the format label in the badge makes it unambiguous. This is semantically honest: all four ARE raster images.

**SVG** gets a distinct icon (bezier curve with anchor dots) because it is structurally different from raster formats.

Parameters: `FormatId` (string, required), `Width` (int, default 16).

### FormatBadge Component

`FormatBadge.razor` — a pill chip: `[icon] FORMAT-NAME`. Sets `--badge-bg` and `--badge-fg` as inline CSS variables resolved from the format tint tokens.

Parameter `Size`: `BadgeSize.Sm` (20px, 12px icon) · `Md` (24px, 14px icon) · `Lg` (32px, 18px icon). `BadgeSize` enum lives in `BadgeSize.cs`.

### ConversionPair Component

`ConversionPair.razor` — renders `[FormatBadge A] → [FormatBadge B]`. Same `Size` parameter.

### Where Format Identity Appears

| Surface | Component | Size |
|---|---|---|
| Tool cards (`/tools`) | `ConversionPair` or format cluster | `Md` |
| Tool page header | `ConversionPair` or format cluster | `Lg` |
| Inline editor panes | `FormatBadge` (detected / selected) | `Sm` |

**Conversion tools** (`ToolKind.Conversion`): show `ConversionPair`. Input format is computed as the first accepted format whose ID doesn't match the current selected output format — this makes alias URLs (e.g. `yaml-to-json`) automatically show `[YAML] → [JSON]`.

**Manipulation tools** (`ToolKind.Manipulation`): show a `tool-format-cluster` flex row of `FormatBadge` components for each accepted format.

### Adding a New Format

When a new `FileFormat` is registered in `FileFormats.cs`, follow this checklist to give it visual identity:

1. Look up the format's planned color in the table above — do not invent a new color without updating the table
2. Add `--format-{id}-bg` and `--format-{id}-fg` tokens to **both** themes in `app.css`
3. Add an `else if (FormatId == "{id}")` branch to `FormatIcon.razor` with the icon paths from the table above
4. The badge and pair components pick up the new format automatically via the inline variable pattern
5. No other files need changes — `FormatBadge` and `ConversionPair` are generic

---

## Syntax Highlighting — Output Pane

`SyntaxHighlightPreview.razor` uses **Prism.js** (self-hosted, no CDN) for all text-based formats that have a Prism grammar.

| Format | Prism grammar | Highlighted |
|---|---|---|
| JSON | `language-json` | ✓ |
| YAML | `language-yaml` | ✓ |
| TOML | `language-toml` | ✓ |
| CSV | none | plain pre-formatted text |

**Loading:** `prism.min.js` (core + grammars concatenated, ~11 KB) loads in `<body>` before `blazor.webassembly.js`. `prism-theme.css` loads in `<head>`.

**Theme:** Custom CSS in `wwwroot/css/prism-theme.css`. All token colours use `[data-theme="dark"]` / `[data-theme="light"]` selectors so they flip with the global theme toggle. No hex values — all colours chosen from the project's slate/green/amber/violet palette.

**Triggering highlight:** `SyntaxHighlightPreview` calls `ClientInterop.highlightElement(el)` in `OnAfterRenderAsync` whenever the content changes. Prism reads `element.textContent`, tokenises it, and sets `element.innerHTML` with highlighted spans. Blazor re-renders overwrite the spans; `OnAfterRenderAsync` re-highlights after every such render.

**Rule:** Any new text-format added to Fileway with a Prism grammar must have a `language-*` mapping in `SyntaxHighlightPreview.LangId`. Formats without a Prism grammar (CSV) render as unstyled monospace — acceptable.

---

## Input Editor — CodeMirror 6

`CodeMirrorEditor.razor` wraps **CodeMirror 6** (self-hosted IIFE bundle, no CDN) for the input pane on data-tool pages.

**Bundle:** `codemirror-fileway.js` (~427 KB minified) built with esbuild from source packages:
- `codemirror` (core + basicSetup)
- `@codemirror/lang-json` — JSON grammar
- `@codemirror/lang-yaml` — YAML grammar
- `@codemirror/legacy-modes` (TOML mode via `StreamLanguage.define`)

Exposes `window.FilewayEditor` with four methods: `create`, `setContent`, `setLanguage`, `destroy`.

**Language switching:** Uses a CodeMirror `Compartment` to swap grammars without recreating the editor. Language is updated via `setLanguage` when `DetectedFormat` changes.

**Theme:** `EditorView.theme()` uses CSS custom properties (`var(--color-bg-primary)` etc.). Syntax token colours are separate `--cm-*` tokens defined in `app.css` under `[data-theme="dark"]` / `[data-theme="light"]` selectors — same flip behaviour as all other tokens.

**Blazor integration:** `CodeMirrorEditor.razor` creates the editor on `OnAfterRenderAsync(firstRender: true)` and tracks `_lastContent` to suppress echo — when the editor reports a change, the parent sets `Value` back, `OnParametersSetAsync` sees it matches `_lastContent` and skips `setContent`. This preserves cursor position. Component implements `IAsyncDisposable`; `DisposeAsync` calls `FilewayEditor.destroy`.

**Formats supported in input pane:**

| Format | Language | Highlighted |
|---|---|---|
| JSON | `json` | ✓ |
| YAML | `yaml` | ✓ |
| TOML | `toml` | ✓ |
| CSV | `""` (empty) | plain monospace |

**Rule:** To rebuild the bundle (e.g. to add a new language), run `esbuild` from `/tmp/cm-build/` after updating `entry.js`. Copy output to `wwwroot/js/codemirror-fileway.js`. Build sources are in `entry.js` alongside the tmp build.

---

## Component Rules

**Blazor CSS isolation:** Each component has its own `.razor.css` scoped file. No global style pollution.  
**No hardcoded colours:** Every colour value is a CSS custom property. No hex values in component styles.  
**No Blazor component libraries:** No MudBlazor, Radzen, or similar. UI is custom CSS with design tokens.  
**JS utility libraries permitted when self-hosted:** Prism.js and CodeMirror 6 are allowed — they are utility libraries, not component libraries, and all assets are in `wwwroot/`.  
**No CDN at runtime:** All JS and font assets are self-hosted in `wwwroot/`.

---

## Key Components and Their UiHints

| UiHints flag | Component rendered |
|---|---|
| `ShowQualitySlider` | `QualitySlider.razor` — live value display, before/after size comparison |
| `ShowDimensionInputs` | `DimensionInputs.razor` — width/height, aspect ratio lock toggle |
| `ShowPageSelector` | `PageRangeSelector.razor` — from/to page, visual range indicator |
| `ShowOrderableList` | `PdfPageEditor.razor` — drag-drop thumbnail grid (see `12-pdf-rendering.md`) |
| `ShowSplitControls` | `SplitControls.razor` — split point selector with page count preview |

`ToolOptionsPanel.razor` reads UiHints flags and conditionally renders the correct sub-components. No per-tool conditional logic outside this component.

---

## Preview Components by PreviewKind

| PreviewKind | Component | When |
|---|---|---|
| `FirstPageRender` | `PdfFirstPagePreview.razor` | PDF input/output inline preview |
| `SideBySideImage` | `SideBySideImagePreview.razor` | Image before/after comparison |
| `SyntaxHighlight` | `SyntaxHighlightPreview.razor` | Data format output (JSON, YAML etc.) |
| `InlineEditor` | `InlineEditorPreview.razor` | Split-pane editor for data tools |
| `PageThumbnails` | `PdfPageEditor.razor` | PDF manipulation tools |
| `None` | — | No preview shown |

`PreviewPanel.razor` reads `InputPreviewKind` and `OutputPreviewKind` from ToolDefinition and renders the correct component. Tool page never knows which preview is shown.
