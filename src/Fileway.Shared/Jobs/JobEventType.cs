namespace Fileway.Shared.Jobs;

public enum JobEventType
{
    Created,
    Queued,
    Processing,
    Completed,
    Failed
}
