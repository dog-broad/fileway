using Fileway.Client.Infrastructure;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Shared.Processors;
using Fileway.Shared.Registry;
using Fileway.Shared.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace Fileway.Client.Services;

public sealed class ProcessorRouter
{
    private readonly WasmProcessorRegistry _wasmRegistry;
    private readonly IToolRegistry _toolRegistry;
    private readonly IServiceProvider _serviceProvider;

    public bool SwitchedToServer { get; private set; }

    public ProcessorRouter(
        WasmProcessorRegistry wasmRegistry,
        IToolRegistry toolRegistry,
        IServiceProvider serviceProvider)
    {
        _wasmRegistry = wasmRegistry;
        _toolRegistry = toolRegistry;
        _serviceProvider = serviceProvider;
    }

    public async Task<ProcessorResult?> TryRunWasmAsync(
        string toolSlug,
        IReadOnlyList<InputFile> inputFiles,
        FileFormat outputFormat,
        System.Text.Json.JsonElement toolOptions,
        IProgress<ProcessorProgressEvent> progress,
        CancellationToken ct)
    {
        SwitchedToServer = false;

        var tool = _toolRegistry.GetBySlug(toolSlug);
        if (tool is null) return null;

        if (tool.ProcessorKind == ProcessorKind.ApiOnly)
            return null;

        var wasmType = _wasmRegistry.Get(toolSlug);

        if (tool.ProcessorKind == ProcessorKind.WasmOnly)
        {
            if (wasmType is null) return null;
            var processor = (IWasmProcessor)_serviceProvider.GetRequiredService(wasmType);
            processor.ValidateOptions(toolOptions);
            var context = BuildContext(toolSlug, inputFiles, outputFormat, toolOptions, progress, ct);
            return await processor.ExecuteAsync(context, ct);
        }

        // WasmPreferred
        if (wasmType is null) return null;

        var wasmProcessor = (IWasmProcessor)_serviceProvider.GetRequiredService(wasmType);

        var totalBytes = inputFiles.Sum(f => f.SizeBytes);
        if (!wasmProcessor.CanHandleSize(totalBytes)) return null;

        try
        {
            wasmProcessor.ValidateOptions(toolOptions);
            var context = BuildContext(toolSlug, inputFiles, outputFormat, toolOptions, progress, ct);
            return await wasmProcessor.ExecuteAsync(context, ct);
        }
        catch (ProcessorDomainException)
        {
            throw;
        }
        catch (ProcessorUnexpectedException)
        {
            SwitchedToServer = true;
            return null;
        }
    }

    private static ProcessorContext BuildContext(
        string toolSlug,
        IReadOnlyList<InputFile> inputFiles,
        FileFormat outputFormat,
        System.Text.Json.JsonElement toolOptions,
        IProgress<ProcessorProgressEvent> progress,
        CancellationToken ct) => new()
    {
        ToolSlug = toolSlug,
        InputFiles = inputFiles,
        OutputFormat = outputFormat,
        ToolOptions = toolOptions,
        CancellationToken = ct,
        Progress = progress
    };
}
