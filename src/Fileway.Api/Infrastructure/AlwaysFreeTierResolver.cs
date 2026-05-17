namespace Fileway.Api.Infrastructure;

public sealed class AlwaysFreeTierResolver : ITierResolver
{
    public Tier Resolve(string sessionToken) => Tier.Free;
}
