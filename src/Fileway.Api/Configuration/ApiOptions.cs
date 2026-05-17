namespace Fileway.Api.Configuration;

public sealed class ApiOptions
{
    public const string SectionName = "Api";

    public int MaxConcurrentJobs { get; init; } = 10;
    public int MaxJobsPerSession { get; init; } = 3;
    public int MaxOnnxJobs { get; init; } = 2;
    public int MaxQueueDepth { get; init; } = 50;
    public int DefaultTimeoutSeconds { get; init; } = 60;
    public int JobSweepIntervalMinutes { get; init; } = 5;
    public int CompletedJobRetentionMinutes { get; init; } = 10;
    public long MaxRequestSizeBytes { get; init; } = 200 * 1024 * 1024;

    // Daily-rotating salt for IP hashing — must be overridden in production
    public string IpHashSalt { get; init; } = "change-in-production";
}
