namespace Fileway.Shared.Tools;

public sealed record ToolLimits
{
    public long MaxInputSizeBytes { get; init; }
    public int MaxInputFileCount { get; init; }
    public int TimeoutSeconds { get; init; }
}
