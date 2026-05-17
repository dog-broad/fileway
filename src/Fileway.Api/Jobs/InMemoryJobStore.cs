using System.Collections.Concurrent;

namespace Fileway.Api.Jobs;

public sealed class InMemoryJobStore : IJobStore
{
    private readonly ConcurrentDictionary<Guid, JobRecord> _jobs = new();

    public void Add(JobRecord job) => _jobs[job.JobId] = job;
    public JobRecord? Get(Guid jobId) => _jobs.TryGetValue(jobId, out var job) ? job : null;
    public IEnumerable<JobRecord> GetAll() => _jobs.Values;
    public bool Remove(Guid jobId) => _jobs.TryRemove(jobId, out _);
}
