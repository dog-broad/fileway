using System.Text.Json;
using Fileway.Shared.Formats;
using Fileway.Shared.Processors;
using Fileway.Tests.Api.Helpers;

namespace Fileway.Tests.Api.Fixtures;

/// <summary>
/// Fluent builder for ProcessorContext. Defaults to sensible values so tests only set what matters.
/// </summary>
public sealed class ProcessorContextBuilder
{
    private string _slug = "test-tool";
    private IReadOnlyList<InputFile> _inputFiles = [];
    private FileFormat _outputFormat = FileFormats.Json;
    private JsonElement _toolOptions = JsonDocument.Parse("{}").RootElement;
    private CancellationToken _ct = CancellationToken.None;
    private IProgress<ProcessorProgressEvent> _progress = new TestProgressCollector();

    public ProcessorContextBuilder WithSlug(string slug) { _slug = slug; return this; }

    public ProcessorContextBuilder WithInputFile(InputFile file)
    {
        _inputFiles = [file];
        return this;
    }

    public ProcessorContextBuilder WithInputFiles(IReadOnlyList<InputFile> files)
    {
        _inputFiles = files;
        return this;
    }

    public ProcessorContextBuilder WithOutputFormat(FileFormat format) { _outputFormat = format; return this; }

    public ProcessorContextBuilder WithToolOptions(JsonElement options) { _toolOptions = options; return this; }

    public ProcessorContextBuilder WithCancellationToken(CancellationToken ct) { _ct = ct; return this; }

    public ProcessorContextBuilder WithProgress(IProgress<ProcessorProgressEvent> progress) { _progress = progress; return this; }

    public ProcessorContext Build() => new()
    {
        ToolSlug = _slug,
        InputFiles = _inputFiles,
        OutputFormat = _outputFormat,
        ToolOptions = _toolOptions,
        CancellationToken = _ct,
        Progress = _progress
    };
}
