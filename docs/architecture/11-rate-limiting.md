# Rate Limiting

**Library:** ASP.NET Core built-in (`Microsoft.AspNetCore.RateLimiting`)  
**Algorithm:** Sliding window  
**Store (v1):** In-memory — sufficient for single-server  
**v2 path:** Redis-compatible interface — swap store without changing policy logic

---

## Two Independent Policies

Both must pass. Either can reject a request independently.

### Policy 1 — Per Session Token

| Setting | Value |
|---|---|
| Window | 60 seconds sliding |
| Permit limit (free) | 20 requests per window |
| Permit limit (paid — v2) | 100 requests per window |
| Queue depth | 0 — immediate reject, no waiting |
| Key source | `X-Session-Token` header value |
| Rejection | 429 + `Retry-After` + `RateLimitExceeded` ProblemDetails |

### Policy 2 — Per IP Hash

| Setting | Value |
|---|---|
| Window | 60 seconds sliding |
| Permit limit | 60 requests per window |
| Queue depth | 0 — immediate reject |
| Key source | `SHA-256(RemoteIpAddress + dailySalt)` — computed in SessionTokenMiddleware |
| Rejection | 429 + `Retry-After` + `RateLimitExceeded` ProblemDetails |

---

## Job-Level Limits (enforced by JobQueueManager, not rate limiter)

| Limit | Value | Config key |
|---|---|---|
| Max concurrent jobs (server-wide) | 10 | `ApiOptions.MaxConcurrentJobs` |
| Max concurrent jobs per session | 3 | `ApiOptions.MaxJobsPerSession` |
| Max concurrent LibreOffice processes | 2 | `LibreOfficeOptions.MaxConcurrent` |
| Max concurrent ONNX jobs | 2 | `ApiOptions.MaxOnnxJobs` |
| Global job queue depth | 50 | `ApiOptions.MaxQueueDepth` |
| Hard job timeout | 60s | `ApiOptions.DefaultTimeoutSeconds` |

Beyond queue depth: 503 + `QueueFull` ProblemDetails.  
Concurrent job limit exceeded: 429 + `ConcurrentJobLimit` (distinct from `RateLimitExceeded`).

**All values are config-driven.** No hardcoded numbers in code.

---

## Retry-After UX

`Retry-After` header value is exact seconds until window allows next request (built-in middleware provides this).

Blazor `ErrorPanel` reads this value and shows a live countdown: **"Try again in 3s"**. Upload button re-enables automatically at zero. `ApiJobClient.cs` must expose the `Retry-After` header alongside the error response for the component to access.

---

## Freemium Hooks

`ITierResolver` interface in `Fileway.Api/Infrastructure/`:
```
Resolve(string sessionToken) → Tier (Free | Paid)
```

**v1:** `AlwaysFreeTierResolver` — always returns `Free`. One-line implementation.  
**v2:** Real implementation reads tier from persistent session token in database.

Rate limiting middleware calls `ITierResolver` to select policy set:
- `Free` → applies Policy 1 (20 req/min) + Policy 2 (60 req/min)
- `Paid` → applies Policy 1 (100 req/min) + Policy 2 (60 req/min)

`ToolDefinition.FreemiumLimitOverrides` — non-null means tool has per-tool size limit differentiation between tiers. Checked in `JobQueueManager` after tier is resolved.

**No code changes needed to launch freemium.** Only: implement `ITierResolver`, set paid tier limit values in config, ship.
