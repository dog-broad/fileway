namespace Fileway.Api.Configuration;

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int SessionTokenWindowSeconds { get; init; } = 60;
    public int SessionTokenFreePermitLimit { get; init; } = 20;
    public int SessionTokenPaidPermitLimit { get; init; } = 100;
    public int IpHashWindowSeconds { get; init; } = 60;
    public int IpHashPermitLimit { get; init; } = 60;
}
