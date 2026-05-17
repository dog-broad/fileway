namespace Fileway.Shared.Jobs.Payloads;

public sealed record FailedPayload
{
    public required string ErrorCode { get; init; }
    public required string UserMessage { get; init; }
    public required string SuggestedAction { get; init; }
    public required bool Retryable { get; init; }
}
