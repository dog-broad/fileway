using Fileway.Shared.Api;
using Fileway.Shared.Jobs;

namespace Fileway.Client.Services;

public enum ToolState
{
    Idle,
    Submitting,
    Processing,
    Completed,
    Failed
}

public sealed class ToolStateService
{
    public ToolState State { get; private set; } = ToolState.Idle;
    public SyncJobResult? SyncResult { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool IsRetryable { get; private set; }
    public int? RetryAfterSeconds { get; private set; }
    public int OverallPercent { get; private set; }
    public string? CurrentStage { get; private set; }
    public int StageIndex { get; private set; }
    public int StageTotalCount { get; private set; }

    public event Action? StateChanged;

    public void BeginSubmitting()
    {
        Reset();
        State = ToolState.Submitting;
        NotifyStateChanged();
    }

    public void BeginProcessing()
    {
        State = ToolState.Processing;
        NotifyStateChanged();
    }

    public void UpdateProgress(string stage, int stageIndex, int stageTotalCount, int overallPercent)
    {
        State = ToolState.Processing;
        CurrentStage = stage;
        StageIndex = stageIndex;
        StageTotalCount = stageTotalCount;
        OverallPercent = overallPercent;
        NotifyStateChanged();
    }

    public void CompleteSync(SyncJobResult result)
    {
        State = ToolState.Completed;
        SyncResult = result;
        OverallPercent = 100;
        NotifyStateChanged();
    }

    public void Fail(string? errorCode, string? message, bool retryable, int? retryAfterSeconds = null)
    {
        State = ToolState.Failed;
        ErrorCode = errorCode;
        ErrorMessage = message;
        IsRetryable = retryable;
        RetryAfterSeconds = retryAfterSeconds;
        NotifyStateChanged();
    }

    public void OnJobEvent(JobEvent jobEvent)
    {
        switch (jobEvent.EventType)
        {
            case JobEventType.Processing:
                var stage = jobEvent.Payload.TryGetProperty("stage", out var s) ? s.GetString() : null;
                var idx = jobEvent.Payload.TryGetProperty("stageIndex", out var si) ? si.GetInt32() : 0;
                var total = jobEvent.Payload.TryGetProperty("stageTotalCount", out var st) ? st.GetInt32() : 1;
                var pct = jobEvent.Payload.TryGetProperty("overallPercent", out var op) ? op.GetInt32() : 0;
                UpdateProgress(stage ?? string.Empty, idx, total, pct);
                break;

            case JobEventType.Completed:
                State = ToolState.Completed;
                OverallPercent = 100;
                NotifyStateChanged();
                break;

            case JobEventType.Failed:
                var code = jobEvent.Payload.TryGetProperty("errorCode", out var ec) ? ec.GetString() : null;
                var reason = jobEvent.Payload.TryGetProperty("reason", out var r) ? r.GetString() : null;
                var retryable = jobEvent.Payload.TryGetProperty("retryable", out var ret) && ret.GetBoolean();
                Fail(code, reason, retryable);
                break;
        }
    }

    public void Reset()
    {
        State = ToolState.Idle;
        SyncResult = null;
        ErrorCode = null;
        ErrorMessage = null;
        IsRetryable = false;
        RetryAfterSeconds = null;
        OverallPercent = 0;
        CurrentStage = null;
        StageIndex = 0;
        StageTotalCount = 0;
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
