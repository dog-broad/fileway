# Job Model + Async Progress

---

## Two Tiers

| Tier | When | Transport | Response |
|---|---|---|---|
| Synchronous | < ~2s, WasmOnly tools, small files | HTTP POST → 200 | SyncJobResult (inline base64 < 5MB, or signed URL ≥ 5MB) |
| Async | > ~2s, ApiOnly, large files | POST → 202 + JobId, then SSE | R2 signed URL, 30-min TTL |

Tier determined by `ToolDefinition.JobTier`. WasmOnly tools are always Synchronous. The client never decides — ProcessorRouter decides.

---

## Progress Delivery — Server-Sent Events

Chosen over SignalR (overkill, stateful hub, large WASM bundle) and polling (stale progress, server hammering).

- **Server:** `GET /api/v1/jobs/{jobId}/progress` → `text/event-stream`
- **Client:** Blazor WASM has no native EventSource → JS interop via `SseClient.js` + `SseClient.cs`
- **Keepalive:** `: ping\n\n` every 15 seconds — prevents proxy timeout
- **Reconnect:** `Last-Event-ID` header on reconnect → server replays missed events from Channel if still available
- **Cancellation:** SSE connection drop → CancellationToken cancelled → processor killed → job swept

---

## Job State Machine (Async jobs only)

```
Created → [Queued] → Uploading → Processing → UploadingOutput → Completed
                                                              ↘
                                                              Failed
```

| State | SSE event type | Key payload fields |
|---|---|---|
| Created | `created` | jobId, toolSlug, createdAt |
| Queued | `queued` | queuePosition, estimatedWaitSeconds? |
| Uploading | `uploading` | bytesReceived, totalBytes, percentComplete |
| Processing | `processing` | stage, stageIndex, stageTotalCount, overallPercent, detail? |
| UploadingOutput | `uploading_output` | outputSizeBytes |
| Completed | `completed` | signedUrl, outputSizeBytes, outputFormat, durationMs, expiresAt |
| Failed | `failed` | errorCode, reason, suggestedAction, retryable |

No explicit Cancelled state in v1. Implicit cancellation via CancellationToken on SSE disconnect.

---

## SSE Event Envelope

All events share this shape:

```json
{
  "type": "processing",
  "jobId": "3fa85f64-...",
  "timestamp": "2025-01-01T12:00:05Z",
  "payload": { /* state-specific fields */ }
}
```

Wire format: `id: {monotonic counter}\ndata: {json}\n\n`

The event `type` is inside the JSON payload — not the SSE `event:` header. Uniform deserialisation on Blazor side.

---

## Channel<JobEvent> — The Processor-to-SSE Pipe

```
Processor → context.Progress.Report(event) → IProgress impl → Channel<JobEvent>.Writer.TryWrite()
SSE endpoint ← Channel<JobEvent>.Reader → streams to browser
```

The processor never knows about SSE, HTTP, or the job store. It calls `Report()` and does its work. The Channel decouples the two halves completely.

In tests: `IProgress` is a `TestProgressCollector` (list). No Channel, no SSE, no HTTP.

---

## JobRecord (in-memory store)

`Fileway.Api/Jobs/JobRecord.cs`

| Field | Type |
|---|---|
| `JobId` | `Guid` |
| `Status` | `JobStatus` |
| `Channel` | `Channel<JobEvent>` |
| `SessionToken` | `string` |
| `CancellationTokenSource` | `CancellationTokenSource` |
| `CreatedAt` | `DateTimeOffset` |
| `ToolSlug` | `string` |

**Store:** `ConcurrentDictionary<Guid, JobRecord>` in `InMemoryJobStore.cs`.  
**Interface:** `IJobStore` — Redis-compatible implementation in v2.  
**Sweep:** `JobSweepService` (IHostedService) — every 5 minutes, deletes completed/failed jobs older than 10 minutes.

---

## Concurrency Limits (all config-driven via ApiOptions)

| Limit | Value |
|---|---|
| Max concurrent jobs (server-wide) | 10 |
| Max concurrent jobs per session | 3 |
| Max concurrent LibreOffice processes | 2 |
| Max concurrent ONNX jobs | 2 |
| Global job queue depth | 50 (beyond → 503 QueueFull) |
| Hard job timeout | 60 seconds (per-tool override via ToolDefinition.TimeoutSeconds) |
| Job store sweep interval | 5 minutes |
| Completed job retention | 10 minutes |

---

## Blazor SSE Client

Blazor WASM has no native `EventSource`. Two-part implementation:

**`Fileway.Client/Interop/SseClient.js`**
- Wraps browser EventSource
- Forwards messages to .NET via `DotNet.invokeMethodAsync`
- Exposes `open(url)` and `close()` methods
- No business logic — pure transport layer

**`Fileway.Client/Services/SseClient.cs`**
- Wraps JS interop behind a clean C# async API
- Deserialises raw JSON strings to typed `JobEvent` records
- Exposes `IAsyncEnumerable<JobEvent>` — components `await foreach`
- Registered as Scoped DI service (one per tab)
- Opens on job start, closes on terminal event (Completed or Failed)

---

## Progress Stages per Tool Class

All Tier 2 tools emit exactly 4 stages. Stage 4 ("Saving result") is always the last — the R2 upload step.

| Tool class | Stages |
|---|---|
| LibreOffice conversions | Preparing document → Converting format → Optimising output → Saving result |
| PDF manipulation | Reading document → Processing pages → Building output → Saving result |
| PDF→DOCX | Parsing structure → Extracting content → Building document → Saving result |
| Remove background | Loading image → Detecting subject → Removing background → Saving result |
| Images→PDF | Processing images → Composing pages → Building PDF → Saving result |
| Large image ops | Decoding image → Processing → Encoding output → Saving result |
