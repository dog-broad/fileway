using Fileway.Client.Processors.DataFormats;
using Fileway.Client.Processors.Image;
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

        services.AddTransient<ConvertImageProcessor>();
        registry.Register("image-convert", typeof(ConvertImageProcessor));

        services.AddTransient<ResizeImageProcessor>();
        registry.Register("image-resize", typeof(ResizeImageProcessor));

        services.AddTransient<RotateFlipImageProcessor>();
        registry.Register("image-rotate", typeof(RotateFlipImageProcessor));

        services.AddTransient<CompressImageProcessor>();
        registry.Register("compress-image", typeof(CompressImageProcessor));

        services.AddSingleton(registry);
        return services;
    }
}
