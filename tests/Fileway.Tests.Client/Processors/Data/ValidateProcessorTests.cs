using System.Text;
using System.Text.Json;
using Fileway.Client.Processors.DataFormats;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Tests.Client.Fixtures;

namespace Fileway.Tests.Client.Processors.Data;

public sealed class ValidateProcessorTests
{
    private readonly ValidateProcessor _processor = new();

    // --- CanHandleSize ---

    [Fact]
    public void CanHandleSize_AnySize_ReturnsTrue()
    {
        _processor.CanHandleSize(0).Should().BeTrue();
        _processor.CanHandleSize(10 * 1024 * 1024).Should().BeTrue();
    }

    // --- Valid inputs pass through unchanged ---

    [Fact]
    public async Task ExecuteAsync_ValidJson_ReturnsInputUnchanged()
    {
        const string json = """{"name":"test","value":42}""";
        var context = BuildContext(json, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        var output = Encoding.UTF8.GetString(result.OutputContent.Span);
        output.Should().Be(json);
    }

    [Fact]
    public async Task ExecuteAsync_ValidJson_ReturnsSameFormat()
    {
        const string json = """{"name":"test"}""";
        var context = BuildContext(json, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFormat.Id.Should().Be(FileFormats.Json.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ValidYaml_ReturnsInputUnchanged()
    {
        const string yaml = "name: test\nvalue: 42\n";
        var context = BuildContext(yaml, FileFormats.Yaml);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        var output = Encoding.UTF8.GetString(result.OutputContent.Span);
        output.Should().Be(yaml);
    }

    [Fact]
    public async Task ExecuteAsync_ValidCsv_ReturnsSameFormat()
    {
        const string csv = "id,name\n1,alice\n2,bob\n";
        var context = BuildContext(csv, FileFormats.Csv);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFormat.Id.Should().Be(FileFormats.Csv.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ValidToml_ReturnsSameFormat()
    {
        const string toml = "[package]\nname = \"test\"\nversion = \"1.0\"\n";
        var context = BuildContext(toml, FileFormats.Toml);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFormat.Id.Should().Be(FileFormats.Toml.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ValidJson_OutputFilenameHasJsonExtension()
    {
        const string json = """{"x":1}""";
        var context = BuildContextWithFilename(json, FileFormats.Json, "data.json");

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFilename.Should().EndWith(".json");
        result.OutputFilename.Should().NotContain("/");
        result.OutputFilename.Should().NotContain("\\");
    }

    // --- Error cases: invalid content ---

    [Fact]
    public async Task ExecuteAsync_InvalidJson_ThrowsProcessorDomainExceptionWithMalformedJsonCode()
    {
        const string badJson = "{ this is broken }";
        var context = BuildContext(badJson, FileFormats.Json);

        var ex = await Assert.ThrowsAsync<ProcessorDomainException>(
            () => _processor.ExecuteAsync(context, CancellationToken.None));

        ex.ErrorCode.Should().Be(ErrorCodes.MalformedJson);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidYaml_ThrowsProcessorDomainExceptionWithMalformedYamlCode()
    {
        // Deliberately invalid YAML — tab indentation is not allowed
        const string badYaml = "key:\n\t- invalid_tab_indent\n";
        var context = BuildContext(badYaml, FileFormats.Yaml);

        var ex = await Assert.ThrowsAsync<ProcessorDomainException>(
            () => _processor.ExecuteAsync(context, CancellationToken.None));

        ex.ErrorCode.Should().Be(ErrorCodes.MalformedYaml);
    }

    [Fact]
    public async Task ExecuteAsync_InvalidToml_ThrowsProcessorDomainExceptionWithMalformedTomlCode()
    {
        const string badToml = "[bad section\nkey = no_close_bracket\n";
        var context = BuildContext(badToml, FileFormats.Toml);

        var ex = await Assert.ThrowsAsync<ProcessorDomainException>(
            () => _processor.ExecuteAsync(context, CancellationToken.None));

        ex.ErrorCode.Should().Be(ErrorCodes.MalformedToml);
    }

    [Fact]
    public async Task ExecuteAsync_UnsupportedFormat_ThrowsProcessorDomainExceptionWithUnsupportedMediaTypeCode()
    {
        // Txt is not a supported validation format
        const string text = "some random text";
        var context = BuildContext(text, FileFormats.Txt);

        var ex = await Assert.ThrowsAsync<ProcessorDomainException>(
            () => _processor.ExecuteAsync(context, CancellationToken.None));

        ex.ErrorCode.Should().Be(ErrorCodes.UnsupportedMediaType);
    }

    // --- Cancellation ---

    [Fact]
    public async Task ExecuteAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        const string json = """{"name":"test"}""";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var context = BuildContext(json, FileFormats.Json, cts.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _processor.ExecuteAsync(context, cts.Token));
    }

    // --- Output content is non-empty for valid input ---

    [Fact]
    public async Task ExecuteAsync_ValidJson_OutputContentIsNonEmpty()
    {
        const string json = """{"test":true}""";
        var context = BuildContext(json, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputContent.IsEmpty.Should().BeFalse();
    }

    // --- Helpers ---

    private static ProcessorContext BuildContext(
        string inputText,
        FileFormat inputFormat,
        CancellationToken ct = default)
    {
        var file = TestFileFactory.FromText(inputText, inputFormat);
        return new ProcessorContextBuilder()
            .WithInputFile(file)
            .WithOutputFormat(inputFormat)
            .WithCancellationToken(ct)
            .Build();
    }

    private static ProcessorContext BuildContextWithFilename(
        string inputText,
        FileFormat inputFormat,
        string filename)
    {
        var file = TestFileFactory.FromText(inputText, inputFormat, filename: filename);
        return new ProcessorContextBuilder()
            .WithInputFile(file)
            .WithOutputFormat(inputFormat)
            .Build();
    }
}
