using Fileway.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Fileway.Tests.Api.Helpers;

/// <summary>
/// WebApplicationFactory for in-process integration tests against the full API.
/// Removes ProcessorSanityCheck (which crashes when M2+ processors are not yet registered)
/// so M1 endpoint tests can run without image processor implementations.
/// </summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove ProcessorSanityCheck so tests are not blocked by M2+ unimplemented processors.
            // The sanity check verifies processor wiring at startup — it is already tested
            // in the real application startup; we don't need it in integration test runs.
            var descriptor = services.SingleOrDefault(
                d => d.ImplementationType == typeof(ProcessorSanityCheck));
            if (descriptor is not null)
                services.Remove(descriptor);
        });
    }
}
