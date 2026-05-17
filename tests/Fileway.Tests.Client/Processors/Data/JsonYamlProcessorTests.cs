using System.Text;
using System.Text.Json;
using Fileway.Client.Processors.DataFormats;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Tests.Client.Fixtures;

namespace Fileway.Tests.Client.Processors.Data;

public sealed class JsonYamlProcessorTests
{
    private readonly JsonYamlProcessor _processor = new();

    // --- CanHandleSize ---

    [Fact]
    public void CanHandleSize_AnySize_ReturnsTrue()
    {
        _processor.CanHandleSize(0).Should().BeTrue();
        _processor.CanHandleSize(1024).Should().BeTrue();
        _processor.CanHandleSize(100 * 1024 * 1024).Should().BeTrue();
    }

    // --- ValidateOptions ---

    [Fact]
    public void ValidateOptions_EmptyObject_DoesNotThrow()
    {
        var options = JsonDocument.Parse("{}").RootElement;
        var act = () => _processor.ValidateOptions(options);
        act.Should().NotThrow();
    }

    // --- JSON → YAML ---

    [Fact]
    public async Task ExecuteAsync_JsonInput_ProducesYamlOutput()
    {
        const string json = """{"name":"test","value":42}""";
        var context = BuildContext(json, FileFormats.Json, FileFormats.Yaml);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFormat.Id.Should().Be(FileFormats.Yaml.Id);
    }

    [Fact]
    public async Task ExecuteAsync_JsonInput_OutputContentIsNonEmpty()
    {
        const string json = """{"name":"test","value":42}""";
        var context = BuildContext(json, FileFormats.Json, FileFormats.Yaml);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputContent.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_JsonInput_OutputFilenameHasYamlExtension()
    {
        const string json = """{"name":"test"}""";
        var context = BuildContextWithFilename(json, FileFormats.Json, "data.json", FileFormats.Yaml);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFilename.Should().EndWith(".yaml");
        result.OutputFilename.Should().NotContain("/");
        result.OutputFilename.Should().NotContain("\\");
    }

    [Fact]
    public async Task ExecuteAsync_JsonInput_YamlOutputContainsExpectedKeys()
    {
        const string json = """{"name":"alice","score":100}""";
        var context = BuildContext(json, FileFormats.Json, FileFormats.Yaml);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        var yaml = Encoding.UTF8.GetString(result.OutputContent.Span);
        yaml.Should().Contain("name");
        yaml.Should().Contain("alice");
        yaml.Should().Contain("score");
    }

    // --- YAML → JSON ---

    [Fact]
    public async Task ExecuteAsync_YamlInput_ProducesJsonOutput()
    {
        const string yaml = "name: test\nvalue: 42\n";
        var context = BuildContext(yaml, FileFormats.Yaml, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFormat.Id.Should().Be(FileFormats.Json.Id);
    }

    [Fact]
    public async Task ExecuteAsync_YamlInput_OutputContentIsNonEmpty()
    {
        const string yaml = "name: test\nvalue: 42\n";
        var context = BuildContext(yaml, FileFormats.Yaml, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputContent.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_YamlInput_OutputFilenameHasJsonExtension()
    {
        const string yaml = "name: test\n";
        var context = BuildContextWithFilename(yaml, FileFormats.Yaml, "config.yaml", FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFilename.Should().EndWith(".json");
    }

    [Fact]
    public async Task ExecuteAsync_YamlInput_JsonOutputIsValidJson()
    {
        const string yaml = "name: test\nvalue: 42\n";
        var context = BuildContext(yaml, FileFormats.Yaml, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        var json = Encoding.UTF8.GetString(result.OutputContent.Span);
        var act = () => JsonDocument.Parse(json);
        act.Should().NotThrow("output must be valid JSON");
    }

    // --- Round-trip ---

    [Fact]
    public async Task ExecuteAsync_RoundTripJsonYamlJson_PreservesData()
    {
        const string originalJson = """{"name":"alice","score":100,"active":true}""";

        // JSON → YAML
        var toYaml = BuildContext(originalJson, FileFormats.Json, FileFormats.Yaml);
        var yamlResult = await _processor.ExecuteAsync(toYaml, CancellationToken.None);
        var yaml = Encoding.UTF8.GetString(yamlResult.OutputContent.Span);

        // YAML → JSON
        var toJson = BuildContext(yaml, FileFormats.Yaml, FileFormats.Json);
        var jsonResult = await _processor.ExecuteAsync(toJson, CancellationToken.None);
        var roundTripJson = Encoding.UTF8.GetString(jsonResult.OutputContent.Span);

        using var origDoc = JsonDocument.Parse(originalJson);
        using var rtDoc = JsonDocument.Parse(roundTripJson);
        rtDoc.RootElement.GetProperty("name").GetString().Should().Be(
            origDoc.RootElement.GetProperty("name").GetString());
        rtDoc.RootElement.GetProperty("score").GetInt32().Should().Be(
            origDoc.RootElement.GetProperty("score").GetInt32());
    }

    // --- Error cases ---

    [Fact]
    public async Task ExecuteAsync_MalformedJson_ThrowsProcessorDomainException()
    {
        const string badJson = "{ this is not valid json }";
        var context = BuildContext(badJson, FileFormats.Json, FileFormats.Yaml);

        var act = async () => await _processor.ExecuteAsync(context, CancellationToken.None);

        await act.Should().ThrowAsync<ProcessorDomainException>()
            .WithMessage("*JSON*");
    }

    [Fact]
    public async Task ExecuteAsync_MalformedJson_ErrorCodeIsMalformedJson()
    {
        const string badJson = "{ BROKEN";
        var context = BuildContext(badJson, FileFormats.Json, FileFormats.Yaml);

        var ex = await Assert.ThrowsAsync<ProcessorDomainException>(
            () => _processor.ExecuteAsync(context, CancellationToken.None));

        ex.ErrorCode.Should().Be(ErrorCodes.MalformedJson);
    }

    [Fact]
    public async Task ExecuteAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        const string json = """{"name":"test"}""";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var context = BuildContext(json, FileFormats.Json, FileFormats.Yaml, cts.Token);

        var act = async () => await _processor.ExecuteAsync(context, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_NoFilenameProvided_OutputFilenameIsNonEmpty()
    {
        const string json = """{"x":1}""";
        var context = BuildContext(json, FileFormats.Json, FileFormats.Yaml);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFilename.Should().NotBeNullOrWhiteSpace();
    }

    // --- Helpers ---

    private static ProcessorContext BuildContext(
        string inputText,
        FileFormat inputFormat,
        FileFormat outputFormat,
        CancellationToken ct = default)
    {
        var file = TestFileFactory.FromText(inputText, inputFormat);
        return new ProcessorContextBuilder()
            .WithInputFile(file)
            .WithOutputFormat(outputFormat)
            .WithCancellationToken(ct)
            .Build();
    }

    private static ProcessorContext BuildContextWithFilename(
        string inputText,
        FileFormat inputFormat,
        string filename,
        FileFormat outputFormat)
    {
        var file = TestFileFactory.FromText(inputText, inputFormat, filename: filename);
        return new ProcessorContextBuilder()
            .WithInputFile(file)
            .WithOutputFormat(outputFormat)
            .Build();
    }
}
