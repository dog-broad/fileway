namespace Fileway.Api.Logging;

public sealed class AuditLogService(ILogger<AuditLogService> logger)
{
    public void LogJobDispatched(string toolSlug, string? sessionPrefix)
    {
        logger.LogInformation("JobDispatched {ToolSlug} {SessionPrefix}",
            toolSlug, sessionPrefix ?? "unknown");
    }

    public void LogJobCompleted(string toolSlug, string? sessionPrefix, long durationMs, long outputSizeBytes, string deliveryKind)
    {
        logger.LogInformation("JobCompleted {ToolSlug} {SessionPrefix} {DurationMs}ms {OutputSizeBytes}bytes {DeliveryKind}",
            toolSlug, sessionPrefix ?? "unknown", durationMs, outputSizeBytes, deliveryKind);
    }

    public void LogJobFailed(string toolSlug, string? sessionPrefix, string errorCode)
    {
        logger.LogWarning("JobFailed {ToolSlug} {SessionPrefix} {ErrorCode}",
            toolSlug, sessionPrefix ?? "unknown", errorCode);
    }
}
