# /project:add-format

Register a new FileFormat in Fileway. Run this before adding any tool that references a format not already in FileFormats.cs.

## Before you start

Read `docs/architecture/06-detection.md` fully — especially the magic bytes table and text heuristics section.

## What to collect

Ask the developer for:
1. Format name (e.g. AVIF, HEIC, MP4)
2. MIME type(s)
3. File extension(s)
4. Whether magic bytes detection is reliable for this format
5. If text-based: structural patterns that identify it

## Steps

### Step 1 — Research magic bytes
Look up the format's magic byte signature. Key questions:
- Offset: does the signature start at byte 0?
- Are any bytes variable (file size fields, version fields)? → needs a mask
- Are there multiple valid signatures (e.g. TIFF little/big endian)?

Common patterns needing masks:
- WEBP: `52 49 46 46 ?? ?? ?? ?? 57 45 42 50` (4 variable size bytes)
- HEIC: `?? ?? ?? ?? 66 74 79 70 68 65 69 63` (4 variable size bytes before ftyp)
- ZIP family: `50 4B 03 04` → triggers secondary disambiguation pass

### Step 2 — Add FileFormat record to FileFormats.cs
File: `src/Fileway.Shared/Formats/FileFormats.cs`

Required fields:
```
Id               — lowercase, stable, never changes (used in URLs/logs)
DisplayName      — shown in UI
MimeTypes        — first entry is canonical for Content-Type output
Extensions       — lowercase without dot, first is canonical for filenames
MagicBytes       — MagicSignature[] with Offset, Bytes[], Mask?
FormatCategory   — Document | Image | Data | Archive
CanBeDetected    — false if text-ambiguous (JSON, YAML, CSV, etc.)
DetectionHints   — string[] patterns for text heuristic pass (if CanBeDetected=false)
MaxFileSizeBytes — format-level ceiling (tool-level limits can be lower, never higher)
IsTextBased      — true for JSON, YAML, CSV, MD, TXT, HTML, SVG
PreviewKind      — None | FirstPageRender | SideBySideImage | SyntaxHighlight | PageThumbnails | InlineEditor
```

### Step 3 — Update FormatDetector if needed
File: `src/Fileway.Shared/Detection/FormatDetector.cs`

- If the format needs ZIP-family disambiguation (Office formats): add to the central directory scan logic
- If text-based: ensure the text heuristic pass handles it
- If the magic bytes use a mask: verify the mask comparison logic handles the new mask pattern

### Step 4 — Add detection tests
File: `tests/Fileway.Tests.Api/` or `tests/Fileway.Tests.Client/` — wherever FormatDetector tests live

Required test cases:
- Known-good file bytes → detect returns this FileFormat with High confidence
- Wrong extension but correct magic bytes → still detects correctly
- Truncated file (first 16 bytes only) → still detects if magic bytes are in first 16
- If text-ambiguous: valid text content → detects with Medium confidence
- If text-ambiguous: invalid/empty content → returns null or Low confidence

### Step 5 — Verify no collision
Run all existing detection tests to ensure the new signature doesn't shadow an existing format:
```bash
dotnet test tests/Fileway.Tests.Api --filter "FormatDetector"
```

## Done checklist

- [ ] FileFormat record added to FileFormats.cs with all required fields
- [ ] MagicSignature correctly specifies offset, bytes, and mask where needed
- [ ] FormatDetector updated if format requires special handling
- [ ] Detection tests added and passing
- [ ] No collision with existing format signatures
