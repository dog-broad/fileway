# SEO + Prerendering

**Milestone:** 5 — introduce just before release.

---

## The Problem

Blazor WASM serves an empty HTML shell. Search engines crawling `/tools/pdf-to-docx` see `<div id="app"></div>` and a loading spinner. No title. No meta description. No h1. Page is invisible to search engines.

---

## Solution: Static HTML at Publish Time

A standalone CLI tool (`tools/StaticGen/`) reads `ToolRegistry` at build time and generates one static HTML file per tool slug. Files are placed in `wwwroot` and served by ASP.NET's static files middleware. Blazor WASM hydrates the prerendered HTML transparently — seamless for human visitors, indexable for crawlers.

**No runtime SSR.** No Blazor Server. No external prerender service.

---

## StaticGen Tool

`tools/StaticGen/` — standalone .NET CLI app. **Not in the solution file.** Not a sixth project. References `Fileway.Shared` only (for ToolRegistry access — no network call).

**Trigger:** Post-publish CI step, after `dotnet publish`, before Docker image build.

**What it generates:**

| Output | Path |
|---|---|
| Per-tool HTML | `wwwroot/tools/{slug}/index.html` |
| Homepage HTML | `wwwroot/index.html` (with correct meta) |
| Tools directory HTML | `wwwroot/tools/index.html` |
| `sitemap.xml` | `wwwroot/sitemap.xml` |
| (robots.txt is static — in wwwroot directly) | |

**Adding a new tool → zero extra SEO work.** StaticGen reads ToolRegistry automatically. Next deploy generates the page.

---

## Per-Tool HTML Structure

### Head (example: pdf-to-docx)

```html
<title>PDF to Word Converter — Fileway</title>
<meta name="description" content="Convert PDF files to editable Word documents. Free, no signup.">
<link rel="canonical" href="https://fileway.io/tools/pdf-to-docx">
<meta property="og:title" content="PDF to Word Converter — Fileway">
<meta property="og:description" content="Convert PDF files to editable Word documents.">
<meta property="og:url" content="https://fileway.io/tools/pdf-to-docx">
<meta property="og:type" content="website">
<meta name="twitter:card" content="summary">
<script type="application/ld+json">{ JSON-LD structured data }</script>
```

### Body — semantic content for crawlers

```html
<div id="app">
  <main aria-label="PDF to Word Converter">
    <h1>PDF to Word Converter</h1>
    <p>Convert PDF files to editable Word documents. Free, no signup required.</p>
    <p class="loading-hint">Loading Fileway...</p>
  </main>
</div>
<script src="_framework/blazor.webassembly.js"></script>
```

Blazor replaces `#app` contents on load. Crawlers see h1 and p. Humans see full Blazor UI after hydration.

---

## JSON-LD Structured Data (per tool)

```json
{
  "@context": "https://schema.org",
  "@type": "WebApplication",
  "name": "PDF to Word Converter",
  "description": "Convert PDF files to editable Word documents.",
  "url": "https://fileway.io/tools/pdf-to-docx",
  "applicationCategory": "UtilitiesApplication",
  "operatingSystem": "Any",
  "offers": { "@type": "Offer", "price": "0", "priceCurrency": "USD" }
}
```

---

## Sitemap.xml

| URL | Priority | changefreq |
|---|---|---|
| `/` (homepage) | 1.0 | weekly |
| `/tools` | 0.9 | weekly |
| Popular tools (`IsPopular: true`) | 0.8 | monthly |
| Standard tools | 0.6 | monthly |

`lastmod` = build date. Updated on every deploy.

---

## Routing — Serving Prerendered HTML

Problem: Blazor WASM uses client-side routing. Direct navigation to `/tools/pdf-to-docx` must serve the prerendered HTML — not a 404 or `index.html`.

**Solution:** `StaticFileOptions` in `Program.cs` with a custom request handler:
- Known tool slug path → serve prerendered `wwwroot/tools/{slug}/index.html`
- Unknown path → serve `wwwroot/index.html` (Blazor catch-all)
- API paths (`/api/`, `/health`) → bypass static files, go to route handlers

---

## robots.txt (static file in wwwroot)

```
User-agent: *
Allow: /
Sitemap: https://fileway.io/sitemap.xml
```
