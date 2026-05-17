namespace Fileway.Api.Jobs;

public interface IJobStore
{
    void Add(JobRecord job);
    JobRecord? Get(Guid jobId);
    IEnumerable<JobRecord> GetAll();
    bool Remove(Guid jobId);
}
