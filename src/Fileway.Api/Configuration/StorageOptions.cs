namespace Fileway.Api.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public bool UseLocalStorage { get; init; } = true;
    public string LocalStoragePath { get; init; } = "/tmp/fileway/output/";
    public string BucketName { get; init; } = string.Empty;
    public string AccountId { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public int SignedUrlTtlMinutes { get; init; } = 30;
}
