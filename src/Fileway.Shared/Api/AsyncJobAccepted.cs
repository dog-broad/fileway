namespace Fileway.Shared.Api;

public sealed record AsyncJobAccepted
{
    public required Guid JobId { get; init; }
    public required string ToolSlug { get; init; }
    public required string Status { get; init; }
    public required string ProgressUrl { get; init; }
    public required string[] EstimatedStages { get; init; }
    public required DateTimeOffset TimeoutAt { get; init; }
}
