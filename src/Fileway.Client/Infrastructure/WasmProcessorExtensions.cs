using Fileway.Client.Processors.DataFormats;
using Microsoft.Extensions.DependencyInjection;

namespace Fileway.Client.Infrastructure;

public static class WasmProcessorExtensions
{
    public static IServiceCollection AddWasmProcessors(this IServiceCollection services)
    {
        var registry = new WasmProcessorRegistry();

        services.AddTransient<JsonYamlProcessor>();
        registry.Register("json-to-yaml", typeof(JsonYamlProcessor));

        services.AddTransient<JsonCsvProcessor>();
        registry.Register("json-to-csv", typeof(JsonCsvProcessor));

        services.AddTransient<JsonTomlProcessor>();
        registry.Register("json-to-toml", typeof(JsonTomlProcessor));

        services.AddTransient<ValidateProcessor>();
        registry.Register("validate", typeof(ValidateProcessor));

        services.AddTransient<CsvToXlsxProcessor>();
        registry.Register("csv-to-xlsx", typeof(CsvToXlsxProcessor));

        services.AddSingleton(registry);
        return services;
    }
}
