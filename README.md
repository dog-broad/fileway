# Fileway

**Any file. Any format. One way.**

A file conversion and manipulation tool. PDF, DOCX, images, data formats — convert, compress, reorder, and transform anything in one place. No signup. No storage. Your files never leave your session.

---

## What it does

- **PDF** — merge, split, reorder pages, rotate, compress, watermark, convert to/from Word
- **Images** — convert formats, resize, crop, compress, remove background, convert SVG
- **Documents** — DOCX ↔ PDF, Markdown → PDF, images → PDF
- **Data** — JSON ↔ YAML ↔ TOML ↔ CSV, validate, CSV → Excel

Drop a file. The app detects its type and shows everything it can become. Pick one. Done.

---

## Tech stack

- Blazor WASM + ASP.NET Core — .NET 9
- PdfPig — PDF manipulation
- Docnet.Core (PDFium) — PDF rendering
- ImageSharp — image processing
- LibreOffice headless — document conversion
- Cloudflare R2 — ephemeral output storage

---

## Running locally

Requires a devcontainer-compatible environment (Docker + VS Code Dev Containers, or GitHub Codespaces).

\`\`\`bash
git clone https://github.com/yourusername/fileway
cd fileway
# Open in devcontainer or Codespace
# Once the container is ready:
dotnet run --project src/Fileway.Api     # API on :5000
dotnet run --project src/Fileway.Client  # Client on :5001
\`\`\`

The devcontainer installs .NET 9, LibreOffice, the Blazor WASM workload, and all dependencies automatically.

---

## Architecture

Full technical documentation lives in \`docs/architecture/\`. Start with \`00-overview.md\`.

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

---

## License

MIT — see [LICENSE](LICENSE).
