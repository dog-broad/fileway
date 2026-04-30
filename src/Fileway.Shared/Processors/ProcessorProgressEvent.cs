namespace Fileway.Shared.Processors;

public sealed record ProcessorProgressEvent
{
    public required string Stage { get; init; }
    public required int StageIndex { get; init; }
    public required int StageTotalCount { get; init; }
    public required int OverallPercent { get; init; }
    public string? Detail { get; init; }
}
