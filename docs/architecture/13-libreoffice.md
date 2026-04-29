# LibreOffice Containerisation

**Milestone:** 3 — introduce when DOCX↔PDF tools are built.

---

## Strategy: Baked into API Image, Fresh Process Per Job

LibreOffice headless is installed directly in the API Docker image. Each conversion starts a fresh LibreOffice process, waits for completion, then kills it. No persistent daemon. No sidecar container.

**Why not sidecar:** Adds Docker Compose/Kubernetes ops complexity, inter-container file sharing friction, doubles deployment cost on most platforms.  
**Why not UNO API:** Complex, poorly documented for .NET, persistent process leaks memory, unreliable kill on timeout.  
**Why not iText/Ghostscript for conversion:** AGPL license — incompatible.

---

## Production Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS base

# LibreOffice layer — single RUN, cleanup at end
RUN apt-get update && apt-get install -y --no-install-recommends \
    libreoffice-nogui \
    fonts-liberation \
    fonts-dejavu \
    libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
# Copy project files → restore (cache layer) → copy source → publish
COPY Directory.*.props ./
COPY src/**/*.csproj ...
RUN dotnet restore
COPY src/ ./src/
RUN dotnet publish Fileway.Api -c Release -r linux-x64 --no-self-contained -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
USER app
ENTRYPOINT ["dotnet", "Fileway.Api.dll"]
```

**Package:** `libreoffice-nogui` — headless only, ~300MB. Not `libreoffice` (adds GUI, X11, GTK — unnecessary ~600MB).  
**Fonts:** `fonts-liberation` + `fonts-dejavu` — document rendering fidelity. Missing fonts cause layout differences between dev and prod.  
**Final image estimate:** ~600MB (base + LO + PDFium + app).

---

## LibreOfficeManager

`Fileway.Api/Infrastructure/LibreOfficeManager.cs` — all process lifecycle here. **Processors never touch `System.Diagnostics.Process` directly.**

### Responsibilities
- `SemaphoreSlim(2)` — max 2 concurrent LibreOffice processes
- Creates isolated temp directory: `/tmp/fileway/{jobId}/`
- Writes input file as `{jobId}.{inputExt}` — never uses original filename
- Constructs command (see below)
- Starts `Process` with `UseShellExecute: false`, redirected stdout/stderr
- Awaits exit with CancellationToken — kills on cancellation or timeout
- Reads output file from temp dir
- Deletes temp dir in `finally` block — **guaranteed cleanup even on failure**
- Logs exit code and truncated stderr (max 200 chars) on non-zero exit

### LibreOffice Command

```
soffice
  --headless
  --norestore
  --nofirststartwizard
  --convert-to {outputFormat}
  --outdir {tempDir}
  -env:UserInstallation=file://{tempDir}/profile
  {tempDir}/{jobId}.{inputExt}
```

**`-env:UserInstallation` is critical.** Two concurrent LibreOffice processes writing to the same default profile directory corrupt each other's state → intermittent conversion failures. This flag gives each process an isolated profile inside its own temp directory. Without it, concurrent conversions fail in ways that are very hard to diagnose.

**`UseShellExecute: false`** — no shell, no shell injection possible.

**Output format strings for LibreOffice:**
| Target | `--convert-to` value |
|---|---|
| PDF | `pdf` |
| DOCX | `docx:MS Word 2007 XML` |
| HTML | `html` |
| TXT | `txt:Text` |

### Temp Directory Lifecycle

| Event | Action |
|---|---|
| Job start | Create `/tmp/fileway/{jobId}/` |
| After creation | Write `{jobId}.{ext}` input file |
| After invocation | LO writes output + `profile/` subdirectory |
| After reading output | `finally` block deletes entire temp dir |
| Orphan cleanup | `JobSweepService` scans `/tmp/fileway/` every 5 min, deletes dirs older than 15 min |

---

## Security Constraints

- `UseShellExecute: false` — no shell, no PATH injection
- Input filename is always `{jobId}.{ext}` — original filename never used
- Output format string validated against FileFormats whitelist before passing to CLI
- Temp path uses `Path.Combine` — no string concatenation with user input
- Process runs as non-root `app` user in Docker
- stderr captured and truncated — never returned to client

---

## Devcontainer Parity

Same OS (Debian Bookworm), same package (`libreoffice-nogui`), same fonts, same ENV vars, same temp directory convention. What works in the Codespace works in the production container.

`LibreOfficeOptions.ExecutablePath` — configured via `appsettings.Development.json` and `appsettings.json` identically (both point to `soffice`).

---

## LibreOfficeProcessor Base Class

`Fileway.Api/Processors/Base/LibreOfficeProcessor.cs` — extend for: DocxToPdfProcessor, PdfToImagesProcessor, MarkdownToPdfProcessor.

Subclasses only define:
- `GetConvertToFormat()` — the `--convert-to` argument string
- `GetProgressStages()` — override if stages differ from default

Base class owns: temp management, process invocation, timeout, cleanup, stderr logging. No duplication across processors.
