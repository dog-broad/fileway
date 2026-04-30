using Fileway.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Fileway.Api.Jobs;

public sealed class JobSweepService(
    IJobStore jobStore,
    IOptions<ApiOptions> apiOptions,
    IOptions<LibreOfficeOptions> libreOfficeOptions,
    ILogger<JobSweepService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("JobSweepService started");

        var opts = apiOptions.Value;
        var sweepInterval = TimeSpan.FromMinutes(opts.JobSweepIntervalMinutes);
        var retentionWindow = TimeSpan.FromMinutes(opts.CompletedJobRetentionMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(sweepInterval, stoppingToken).ConfigureAwait(false);
            SweepExpiredJobs(retentionWindow);
            SweepOrphanedTempDirs(libreOfficeOptions.Value.TempBasePath);
        }
    }

    private void SweepExpiredJobs(TimeSpan retentionWindow)
    {
        var cutoff = DateTimeOffset.UtcNow - retentionWindow;
        var swept = 0;

        foreach (var job in jobStore.GetAll())
        {
            if (job.Status is not (JobStatus.Completed or JobStatus.Failed)) continue;
            if (!job.CompletedAt.HasValue || job.CompletedAt.Value >= cutoff) continue;
            if (!jobStore.Remove(job.JobId)) continue;

            job.CancellationTokenSource.Dispose();
            swept++;
        }

        if (swept > 0)
            logger.LogInformation("Swept {Count} expired jobs", swept);
    }

    private void SweepOrphanedTempDirs(string tempBasePath)
    {
        if (!Directory.Exists(tempBasePath)) return;

        var cutoff = DateTime.UtcNow.AddMinutes(-15);
        foreach (var dir in Directory.GetDirectories(tempBasePath))
        {
            try
            {
                if (Directory.GetCreationTimeUtc(dir) < cutoff)
                    Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to clean orphaned temp directory");
            }
        }
    }
}
