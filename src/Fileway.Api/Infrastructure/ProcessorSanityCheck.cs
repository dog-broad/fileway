using Fileway.Shared.Processors;
using Fileway.Shared.Registry;
using Fileway.Shared.Tools;

namespace Fileway.Api.Infrastructure;

public sealed class ProcessorSanityCheck(
    IToolRegistry toolRegistry,
    IServiceProvider serviceProvider,
    ILogger<ProcessorSanityCheck> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var tools = toolRegistry.GetAll();
        var slugsSeen = new HashSet<string>(StringComparer.Ordinal);
        var allSlugs = tools.Select(t => t.Slug).ToHashSet(StringComparer.Ordinal);

        foreach (var tool in tools)
        {
            // Duplicate slug check
            if (!slugsSeen.Add(tool.Slug))
                Fail($"Duplicate tool slug '{tool.Slug}'.");

            // Non-WasmOnly tools must have ProcessorType set
            if (tool.ProcessorKind != ProcessorKind.WasmOnly && tool.ProcessorType is null)
                Fail($"Tool '{tool.Slug}' (ProcessorKind={tool.ProcessorKind}) has null ProcessorType. " +
                     "Call InitializeProcessorTypes before the sanity check runs.");

            // ProcessorType must resolve to IApiProcessor
            if (tool.ProcessorType is not null)
            {
                object? instance;
                try
                {
                    instance = serviceProvider.GetService(tool.ProcessorType);
                }
                catch (Exception ex)
                {
                    Fail($"ProcessorType '{tool.ProcessorType.Name}' for tool '{tool.Slug}' threw during resolution: {ex.Message}");
                    return Task.CompletedTask;
                }

                if (instance is null)
                    Fail($"ProcessorType '{tool.ProcessorType.Name}' for tool '{tool.Slug}' is not registered in DI.");

                if (instance is not IApiProcessor)
                    Fail($"ProcessorType '{tool.ProcessorType.Name}' for tool '{tool.Slug}' does not implement IApiProcessor.");
            }

            // RelatedSlugs must all exist
            foreach (var relatedSlug in tool.RelatedSlugs)
            {
                if (!allSlugs.Contains(relatedSlug))
                    Fail($"Tool '{tool.Slug}' has RelatedSlug '{relatedSlug}' which is not a known tool slug.");
            }
        }

        logger.LogInformation("SanityCheckPassed — {ToolCount} tools verified", tools.Count);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void Fail(string message) =>
        throw new InvalidOperationException($"[ProcessorSanityCheck] {message}");
}
