# Observability + Structured Logging

**Library:** Serilog + Serilog.AspNetCore + Serilog.Sinks.Console  
**Format:** Structured JSON lines to stdout — compact JSON formatter  
**v1 sink:** stdout only. Container runtime aggregates. No Seq, no Loki, no external service.  
**v2 path:** Add `Serilog.Sinks.OpenTelemetry` — one line change, zero other changes.

---

## Log Entry Shape

```json
{
  "timestamp": "2025-01-01T12:00:05.123Z",
  "level": "Information",
  "event": "JobCompleted",
  "jobId": "3fa85f64-...",
  "toolSlug": "pdf-to-docx",
  "durationMs": 23400,
  "outputSizeBytes": 204800,
  "sessionPrefix": "3fa85f64"
}
```

**Correlation:** `JobId` pushed to `LogContext` for all job-scoped events. `grep JobId` → full job trace.

**Minimum log level:** `Information` in production. `Debug` in development.

---

## Privacy Hard Rules — NEVER Log These

- File content — not even partial bytes
- Original filenames
- Raw IP addresses — only `SHA-256(IP + daily rotating salt)`
- Full session tokens — only first 8 characters (`sessionPrefix`)
- Signed R2 URLs — contain credentials
- `toolOptions` content — may contain passwords, watermark text
- User-Agent strings

---

## Instrumented Events

### Request Lifecycle
| Event | Key fields |
|---|---|
| `RequestReceived` | method, path, sessionPrefix, contentLength |
| `SessionTokenInvalid` | path, reason |
| `RateLimitHit` | sessionPrefix, ipHash, limitType, retryAfter |
| `RequestCompleted` | statusCode, durationMs |

### Job Lifecycle
| Event | Key fields |
|---|---|
| `JobCreated` | jobId, toolSlug, fileSizeBytes, sessionPrefix |
| `JobQueued` | jobId, queuePosition |
| `JobStarted` | jobId, processorType |
| `JobCompleted` | jobId, toolSlug, durationMs, outputSizeBytes |
| `JobFailed` | jobId, errorCode, durationMs |
| `JobTimedOut` | jobId, toolSlug, timeoutSeconds |
| `JobCancelledByDisconnect` | jobId |

### Infrastructure
| Event | Key fields |
|---|---|
| `LibreOfficeStarted` | jobId, sanitisedArgs (no file paths) |
| `LibreOfficeCompleted` | jobId, exitCode, durationMs |
| `LibreOfficeFailed` | jobId, exitCode, stderr (truncated 200 chars) |
| `StorageUploadStarted` | jobId, outputSizeBytes |
| `StorageUploadCompleted` | jobId, durationMs (no URL) |
| `JobSweeperRun` | jobsSwept, timestamp |

### Startup
| Event | Key fields |
|---|---|
| `SanityCheckStarted` | toolCount |
| `SanityCheckPassed` | toolCount, durationMs |
| `SanityCheckFailed` | failureReason — throws, crashes startup |
| `LibreOfficeVerified` | version |
| `OnnxModelLoaded` | modelName, loadDurationMs |

---

## IP Hashing

```
ipHash = SHA-256(rawIp + dailySalt)
dailySalt = SHA-256(date.ToString("yyyyMMdd") + secretSalt)
```

`secretSalt` from configuration. Rotates daily — logs from yesterday cannot be re-correlated with today's. Raw IP never reaches the logger.

---

## AuditLogService

`Fileway.Api/Logging/AuditLogService.cs` — separate from general logging.

Writes structured audit events for compliance reference:
- Conversion event: `{timestamp, sessionPrefix, toolSlug, inputFormatId, inputSizeBytes, outputSizeBytes, durationMs, success}`
- No file content, no filenames, no IPs

Audit log is separate Serilog sink (file-based in v2 if needed). In v1: same stdout sink, different event type field.
