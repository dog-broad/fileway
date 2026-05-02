using Microsoft.Extensions.DependencyInjection;

namespace Fileway.Client.Infrastructure;

public static class WasmProcessorExtensions
{
    public static IServiceCollection AddWasmProcessors(this IServiceCollection services)
    {
        var registry = new WasmProcessorRegistry();
        // Processors registered here in subsequent milestones:
        // services.AddTransient<JsonYamlProcessor>();
        // registry.Register("json-to-yaml", typeof(JsonYamlProcessor));
        services.AddSingleton(registry);
        return services;
    }
}
