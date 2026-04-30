using System.Text.Json;

namespace Fileway.Shared.Jobs;

public sealed record JobEvent
{
    public required JobEventType EventType { get; init; }
    public required string EventId { get; init; }
    public JsonElement Payload { get; init; }
}
