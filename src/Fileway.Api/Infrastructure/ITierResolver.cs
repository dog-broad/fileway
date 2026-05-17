namespace Fileway.Api.Infrastructure;

public interface ITierResolver
{
    Tier Resolve(string sessionToken);
}
