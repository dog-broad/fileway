using Fileway.Api.Processors.DataFormats;
using Fileway.Api.Processors.ImageManipulation;
using Fileway.Shared.Registry;

namespace Fileway.Api.Infrastructure;

public static class ProcessorExtensions
{
    public static IServiceCollection AddApiProcessors(this IServiceCollection services)
    {
        services.AddTransient<CsvToXlsxProcessor>();
        services.AddTransient<ResizeImageProcessor>();
        services.AddTransient<CompressImageProcessor>();
        services.AddTransient<SvgConvertProcessor>();
        return services;
    }

    public static void InitializeProcessorTypes(this WebApplication app)
    {
        var toolRegistry = app.Services.GetRequiredService<IToolRegistry>();
        var mapping = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["csv-to-xlsx"]   = typeof(CsvToXlsxProcessor),
            ["image-resize"]  = typeof(ResizeImageProcessor),
            ["compress-image"] = typeof(CompressImageProcessor),
            ["svg-convert"]   = typeof(SvgConvertProcessor)
        };

        foreach (var tool in toolRegistry.GetAll())
        {
            if (mapping.TryGetValue(tool.Slug, out var type))
                tool.ProcessorType = type;
        }
    }
}
