# Format Detection

`Fileway.Shared/Detection/FormatDetector.cs` — runs identically on WASM and API.

---

## Three-Pass Pipeline

```
Pass 1: Magic bytes (read first 16 bytes)
  → Match? Return FileFormat + High confidence

Pass 2: ZIP disambiguation (if Pass 1 matched ZIP family)
  → Scan central directory for characteristic filenames
  → Identify DOCX vs XLSX vs PPTX vs plain ZIP → High confidence

Pass 3: Text heuristics (if Pass 1+2 returned null)
  → Read first 512 bytes as UTF-8
  → Apply structural patterns per format → Medium or Low confidence

All pass? → null (unknown format)
```

No external library. Custom implementation. Zero extra NuGet dependencies. Compiles to WASM.

---

## Magic Byte Signatures

| Format | Offset | Bytes (hex) | Notes |
|---|---|---|---|
| PDF | 0 | `25 50 44 46` | %PDF |
| PNG | 0 | `89 50 4E 47 0D 0A 1A 0A` | 8-byte signature |
| JPEG | 0 | `FF D8 FF` | SOI marker |
| WEBP | 0 | `52 49 46 46 ?? ?? ?? ?? 57 45 42 50` | RIFF....WEBP — 4 wildcard size bytes |
| GIF | 0 | `47 49 46 38` | GIF8 |
| BMP | 0 | `42 4D` | BM |
| TIFF | 0 | `49 49 2A 00` OR `4D 4D 00 2A` | LE or BE variants |
| ICO | 0 | `00 00 01 00` | |
| HEIC | 4 | `66 74 79 70 68 65 69 63` | ftyp heic — offset 4, 4 variable bytes before |
| ZIP family | 0 | `50 4B 03 04` | PK local file header → triggers Pass 2 |
| RTF | 0 | `7B 5C 72 74 66` | {\rtf |

**Mask:** `0x00` = wildcard. Applied via AND before comparison. `MagicSignature.Mask` is nullable — null means no mask.

---

## ZIP Family Disambiguation (Pass 2)

DOCX, XLSX, PPTX, and ZIP all start with `PK`. After magic byte match as ZIP-family, scan central directory for characteristic filenames:

| Filename present | Detected as |
|---|---|
| `[Content_Types].xml` + `word/` | DOCX |
| `[Content_Types].xml` + `xl/` | XLSX |
| `[Content_Types].xml` + `ppt/` | PPTX |
| `[Content_Types].xml` (no office dir) | Generic Office Open XML |
| No `[Content_Types].xml` | ZIP |

---

## Text Heuristics (Pass 3)

Runs only when magic bytes returned null. Reads first 512 bytes as UTF-8.

| Format | Primary signals | Confidence |
|---|---|---|
| JSON | Starts with `{` or `[` after optional BOM/whitespace. Contains balanced `:` and `"` in first 256 chars. | High if starts with `{` or `[`, Medium otherwise |
| YAML | Starts with `---` OR lines matching `/^\w+:\s/` pattern. Not valid JSON. | Medium |
| TOML | Contains `[section]` headers matching `/^\[[\w.]+\]/` or key=value pairs `/^\w+ = /` | Medium |
| CSV | First line contains commas. Comma count consistent across first 3 lines. No `{` or `:` patterns. | Medium |
| SVG | Contains `<svg` anywhere in first 512 bytes | High |
| HTML | Contains `<!DOCTYPE html` or `<html` in first 256 bytes | High |
| Markdown | Contains `#` heading patterns or `---` frontmatter | Low — last resort |

---

## On Detection Failure

**WASM (after file drop):** Drop zone shows "We couldn't identify this file type." User can still manually pick a tool from /tools. No error state.

**API (during job submission):** Detected format doesn't match AcceptedFormats → 422 `FormatMismatch`. The server is the authoritative validator even if WASM detection passed.

**Confidence levels:** `High`, `Medium`, `Low` — carried on `DetectResponse` for the `/api/v1/detect` endpoint. UI may show a warning for Low confidence detections before conversion.

---

## IFormatDetector Interface

```
Detect(ReadOnlySpan<byte> header, string? filename) → (FileFormat? format, DetectionConfidence confidence)
```

`header` — first 512 bytes of file (only first 16 used for magic bytes, rest for text heuristics).  
`filename` — hint only. Used for tiebreaking only when confidence is Low. Never trusted alone.

Used by:
- `DetectionService.cs` (WASM) — called on file drop in DropZone
- `JobEndpoints.cs` (API) — called after file buffering, before processor dispatch
- `DetectEndpoints.cs` (API) — serves `/api/v1/detect`
