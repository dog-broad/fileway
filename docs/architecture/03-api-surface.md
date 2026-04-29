# API Surface

**Base:** `/api/v1` | **Style:** ASP.NET Core minimal API | **Versioning:** URL path

---

## Session Token

Every request carries `X-Session-Token: {uuid-v4}`.  
Generated in browser on first page load. Stored in `sessionStorage` (gone on tab close).  
Missing/invalid → 400 immediately, before reading any body.  
Server rate-limits per token. Job store associates jobs to token. Token never stored in database.

---

## All 7 Endpoints

| Method | Path | Auth | Purpose |
|---|---|---|---|
| POST | `/api/v1/jobs` | Session token required | Submit any job (all 23 tools) |
| GET | `/api/v1/jobs/{jobId}/progress` | Session token required | SSE stream for async job |
| GET | `/api/v1/tools` | None | List all tools (filterable) |
| GET | `/api/v1/tools/{slug}` | None | Single tool detail |
| POST | `/api/v1/detect` | None | Server-side format detection fallback |
| GET | `/health/live` | None | Liveness probe |
| GET | `/health/ready` | None | Readiness probe |

**Adding a tool adds zero new endpoints.** toolSlug in the request routes to the right processor.

---

## POST /api/v1/jobs

**Content-Type:** `multipart/form-data`

### Multipart Parts (options part MUST be first)

| Part | Content-Type | Required | Notes |
|---|---|---|---|
| `options` | `application/json` | Yes | Read before any file bytes. JobOptions schema below. |
| `file` | `application/octet-stream` | Yes* | *Omit for inline data tools using options.inlineContent |
| `file_N` | `application/octet-stream` | No | file_1, file_2 … for multi-file tools (merge-pdf, images-to-pdf) |

### JobOptions Schema

```json
{
  "toolSlug": "pdf-to-docx",
  "outputFormat": "docx",
  "inlineContent": null,
  "toolOptions": {
    "quality": 85,
    "pageOrder": [3, 1, 2],
    "pagesToRemove": [2, 5],
    "splitAtPages": [3, 6],
    "targetWidth": 1920,
    "targetHeight": 1080,
    "maintainAspectRatio": true,
    "watermarkText": "Confidential",
    "rotation": 90,
    "password": "secret"
  }
}
```

`toolOptions` is free-form JSON — validated by the processor, not the endpoint. Unknown fields ignored.

### Validation Order (fail fast — reject at first failure)

1. Parse `X-Session-Token` — invalid UUID → 400
2. Read `Content-Length` — exceeds 200MB global ceiling → 413
3. Read `options` part — malformed JSON → 400
4. `ValidateSlug(toolSlug)` — unknown slug → 400
5. Validate `outputFormat` in `ToolDefinition.OutputFormats` — 400
6. Rate limit check — 429
7. Concurrent job check — 429
8. Stream file(s)
9. Magic bytes detection — format mismatch → 422
10. Size vs `ToolDefinition.MaxInputSizeBytes` — 413
11. Zip bomb check (archives) — 422
12. Create job + dispatch

### Responses

**Synchronous (JobTier.Synchronous):**
```json
{
  "toolSlug": "json-to-yaml",
  "outputFormat": "yaml",
  "outputMimeType": "text/yaml",
  "outputSizeBytes": 1024,
  "durationMs": 42,
  "deliveryKind": "Inline",
  "inlineContent": "base64...",
  "signedUrl": null,
  "expiresAt": null
}
```
`deliveryKind`: `Inline` (< 5MB, base64) or `SignedUrl` (≥ 5MB, R2 URL, 30-min TTL)

**Async (JobTier.Async):**
```json
{
  "jobId": "3fa85f64-...",
  "toolSlug": "pdf-to-docx",
  "status": "Created",
  "progressUrl": "/api/v1/jobs/{id}/progress",
  "estimatedStages": ["Parsing structure", "Extracting content", "Building document", "Saving result"],
  "timeoutAt": "2025-01-01T12:01:00Z"
}
```

### Status Codes

| Code | Meaning |
|---|---|
| 200 | Sync job completed — result in body |
| 202 | Async job accepted — open SSE stream |
| 400 | Invalid token, malformed options, unknown slug, invalid format |
| 413 | File too large |
| 415 | Not multipart/form-data |
| 422 | Format mismatch, corrupted file, invalid options values |
| 429 | Rate limit or concurrent job limit exceeded |
| 500 | Processor unexpected error |
| 503 | Job queue full |
| 504 | Job timeout |

---

## GET /api/v1/jobs/{jobId}/progress

Returns `text/event-stream`. Session token must match the token used to submit the job.

`Last-Event-ID` header on reconnect → server replays missed events.  
`: ping\n\n` sent every 15 seconds.  
Stream closes when job reaches Completed or Failed.

**Status codes:** 200 (stream open), 403 (wrong session), 404 (job not found/expired)

---

## GET /api/v1/tools

Query params: `category` (document|image|data|archive), `q` (search, min 2 chars)  
Response: `ToolSummary[]`  
Headers: `Cache-Control: public, max-age=3600` + `ETag`

### ToolSummary fields (not full ToolDefinition — omits ProcessorType, ProgressStages, UiHints, etc.)

`slug, displayName, shortDescription, description, category, kind, acceptedFormats[], outputFormats[], isNew, isPopular, maxInputSizeBytes, acceptsMultipleFiles, canonicalPath`

Blazor uses the in-process ToolRegistry directly — this endpoint is for external consumers and the discovery page.

---

## POST /api/v1/detect

Rarely called — only when WASM detection is inconclusive.

**Request:**
```json
{ "headerBytes": "base64-first-512-bytes", "filename": "report.pdf", "declaredMimeType": "application/pdf" }
```
`filename` and `declaredMimeType` are hints only — not trusted for format determination.

**Response:**
```json
{ "detectedFormat": "pdf", "confidence": "High", "suggestedTools": ["pdf-to-docx", "compress-pdf"] }
```

---

## Error Schema — RFC 9457 ProblemDetails Extended

Every 4xx and 5xx is a ProblemDetails body. No plain-text errors. No empty error bodies.

```json
{
  "type": "https://fileway.io/errors/corrupted-file",
  "title": "File appears to be corrupted",
  "status": 422,
  "detail": "Internal technical detail — for logs only",
  "instance": "/api/v1/jobs",
  "errorCode": "CorruptedFile",
  "userMessage": "This file appears to be corrupted or incomplete.",
  "suggestedAction": "Try re-exporting from the original application.",
  "retryable": false
}
```

`errorCode` is the stable machine-readable string Blazor switches on.  
`userMessage` + `suggestedAction` are shown directly in the ErrorPanel UI.  
`retryable: true` → show "Try again" button. `retryable: false` → show RelatedSlugs chips.

### Error Code Taxonomy

**Validation (4xx):** InvalidSessionToken, MalformedOptions, UnknownToolSlug, InvalidOutputFormat, MissingFilePart, TooManyFiles, JobNotFound, JobNotOwned, FileTooLarge, UnsupportedMediaType, RateLimitExceeded, QueueFull, ConcurrentJobLimit

**File/Format (422):** FormatMismatch, CorruptedFile, EncryptedFile, EmptyFile, ZipBombDetected, PolyglotDetected, UnsupportedEncoding, InvalidPageRange, TooManyPages, InvalidPageOrder, ImageTooLarge, InvalidDimensions, MalformedJson, MalformedYaml, InvalidCsv

**Processing (5xx):** JobTimeout, ProcessorUnexpectedError, StorageWriteFailed, LibreOfficeUnavailable, LibreOfficeTimeout, OnnxModelUnavailable, OutputValidationFailed, ConversionFailed

All codes defined as string constants in `Fileway.Shared/Errors/ErrorCodes.cs`.

---

## Middleware Pipeline Order

1. HTTPS redirection
2. Security headers (CSP, HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy)
3. CORS (fileway.io + localhost only — no wildcard)
4. Request size limit (200MB global ceiling)
5. Rate limiting (sliding window, dual-keyed)
6. Structured logging enrichment (SessionToken prefix, RequestId)
7. ProblemDetails exception handler (catches unhandled exceptions → 500, never leaks stack traces)
8. Routing + endpoints
