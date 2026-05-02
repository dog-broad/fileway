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

## Syntax Highlighting

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

## Component Rules

**Blazor CSS isolation:** Each component has its own `.razor.css` scoped file. No global style pollution.  
**No hardcoded colours:** Every colour value is a CSS custom property. No hex values in component styles.  
**No component library:** No MudBlazor, Radzen, or any third-party component CSS/JS.  
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
