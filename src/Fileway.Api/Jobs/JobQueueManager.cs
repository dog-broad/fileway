using System.Collections.Concurrent;
using Fileway.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Fileway.Api.Jobs;

public enum AcquireSlotResult
{
    Success,
    ConcurrentLimitExceeded,
    QueueFull
}

public sealed class JobQueueManager(IOptions<ApiOptions> options)
{
    private int _activeJobCount;
    private readonly ConcurrentDictionary<string, int> _sessionCounts = new(StringComparer.Ordinal);

    public AcquireSlotResult TryAcquire(string sessionToken)
    {
        var opts = options.Value;

        var sessionCount = _sessionCounts.AddOrUpdate(sessionToken, 1, (_, c) => c + 1);
        if (sessionCount > opts.MaxJobsPerSession)
        {
            _sessionCounts.AddOrUpdate(sessionToken, 0, (_, c) => Math.Max(0, c - 1));
            return AcquireSlotResult.ConcurrentLimitExceeded;
        }

        var globalCount = Interlocked.Increment(ref _activeJobCount);
        if (globalCount > opts.MaxConcurrentJobs)
        {
            Interlocked.Decrement(ref _activeJobCount);
            _sessionCounts.AddOrUpdate(sessionToken, 0, (_, c) => Math.Max(0, c - 1));
            return AcquireSlotResult.QueueFull;
        }

        return AcquireSlotResult.Success;
    }

    public void Release(string sessionToken)
    {
        Interlocked.Decrement(ref _activeJobCount);
        _sessionCounts.AddOrUpdate(sessionToken, 0, (_, c) => Math.Max(0, c - 1));
    }
}
