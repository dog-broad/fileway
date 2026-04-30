using System.Threading.Channels;
using Fileway.Shared.Jobs;

namespace Fileway.Api.Jobs;

public sealed class JobRecord
{
    public required Guid JobId { get; init; }
    public JobStatus Status { get; set; }
    public required Channel<JobEvent> EventChannel { get; init; }
    public required string SessionToken { get; init; }
    public required CancellationTokenSource CancellationTokenSource { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string ToolSlug { get; init; }
    public DateTimeOffset? CompletedAt { get; set; }
}
