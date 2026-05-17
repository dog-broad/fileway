using System.Text;
using System.Text.Json;
using Fileway.Client.Processors.DataFormats;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Tests.Client.Fixtures;

namespace Fileway.Tests.Client.Processors.Data;

public sealed class JsonTomlProcessorTests
{
    private readonly JsonTomlProcessor _processor = new();

    // --- CanHandleSize ---

    [Fact]
    public void CanHandleSize_AnySize_ReturnsTrue()
    {
        _processor.CanHandleSize(0).Should().BeTrue();
        _processor.CanHandleSize(10 * 1024 * 1024).Should().BeTrue();
    }

    // --- JSON → TOML ---

    [Fact]
    public async Task ExecuteAsync_JsonObjectInput_ProducesTomlOutput()
    {
        const string json = """{"name":"test","port":8080}""";
        var context = BuildContext(json, FileFormats.Json, FileFormats.Toml);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFormat.Id.Should().Be(FileFormats.Toml.Id);
    }

    [Fact]
    public async Task ExecuteAsync_JsonObjectInput_OutputContentIsNonEmpty()
    {
        const string json = """{"name":"test","port":8080}""";
        var context = BuildContext(json, FileFormats.Json, FileFormats.Toml);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputContent.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_JsonObjectInput_TomlOutputContainsExpectedKey()
    {
        const string json = """{"name":"myapp","port":8080}""";
        var context = BuildContext(json, FileFormats.Json, FileFormats.Toml);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        var toml = Encoding.UTF8.GetString(result.OutputContent.Span);
        toml.Should().Contain("name");
        toml.Should().Contain("myapp");
        toml.Should().Contain("port");
    }

    [Fact]
    public async Task ExecuteAsync_JsonObjectInput_OutputFilenameHasTomlExtension()
    {
        const string json = """{"x":1}""";
        var context = BuildContextWithFilename(json, FileFormats.Json, "config.json", FileFormats.Toml);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFilename.Should().EndWith(".toml");
        result.OutputFilename.Should().NotContain("/");
        result.OutputFilename.Should().NotContain("\\");
    }

    // --- TOML → JSON ---

    [Fact]
    public async Task ExecuteAsync_TomlInput_ProducesJsonOutput()
    {
        const string toml = "[package]\nname = \"myapp\"\nversion = \"1.0.0\"\n";
        var context = BuildContext(toml, FileFormats.Toml, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFormat.Id.Should().Be(FileFormats.Json.Id);
    }

    [Fact]
    public async Task ExecuteAsync_TomlInput_OutputContentIsNonEmpty()
    {
        const string toml = "name = \"test\"\nvalue = 42\n";
        var context = BuildContext(toml, FileFormats.Toml, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputContent.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_TomlInput_JsonOutputIsValidJson()
    {
        const string toml = "name = \"test\"\nvalue = 42\n";
        var context = BuildContext(toml, FileFormats.Toml, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        var json = Encoding.UTF8.GetString(result.OutputContent.Span);
        var act = () => JsonDocument.Parse(json);
        act.Should().NotThrow("output must be valid JSON");
    }

    [Fact]
    public async Task ExecuteAsync_TomlInput_OutputFilenameHasJsonExtension()
    {
        const string toml = "name = \"test\"\n";
        var context = BuildContextWithFilename(toml, FileFormats.Toml, "Cargo.toml", FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFilename.Should().EndWith(".json");
    }

    // --- Round-trip ---

    [Fact]
    public async Task ExecuteAsync_RoundTripJsonTomlJson_PreservesScalarValues()
    {
        const string originalJson = """{"appName":"fileway","port":8080,"enabled":true}""";

        // JSON → TOML
        var toToml = BuildContext(originalJson, FileFormats.Json, FileFormats.Toml);
        var tomlResult = await _processor.ExecuteAsync(toToml, CancellationToken.None);
        var toml = Encoding.UTF8.GetString(tomlResult.OutputContent.Span);

        // TOML → JSON
        var toJson = BuildContext(toml, FileFormats.Toml, FileFormats.Json);
        var jsonResult = await _processor.ExecuteAsync(toJson, CancellationToken.None);
        var roundTripJson = Encoding.UTF8.GetString(jsonResult.OutputContent.Span);

        using var doc = JsonDocument.Parse(roundTripJson);
        doc.RootElement.GetProperty("appName").GetString().Should().Be("fileway");
        doc.RootElement.GetProperty("port").GetInt64().Should().Be(8080);
    }

    // --- Error cases ---

    [Fact]
    public async Task ExecuteAsync_MalformedJson_ThrowsProcessorDomainExceptionWithMalformedJsonCode()
    {
        const string badJson = "{ this is broken }";
        var context = BuildContext(badJson, FileFormats.Json, FileFormats.Toml);

        var ex = await Assert.ThrowsAsync<ProcessorDomainException>(
            () => _processor.ExecuteAsync(context, CancellationToken.None));

        ex.ErrorCode.Should().Be(ErrorCodes.MalformedJson);
    }

    [Fact]
    public async Task ExecuteAsync_JsonArrayRoot_ThrowsProcessorDomainException()
    {
        // TOML requires a root table — arrays are not valid TOML roots
        const string json = """[{"id":1},{"id":2}]""";
        var context = BuildContext(json, FileFormats.Json, FileFormats.Toml);

        var ex = await Assert.ThrowsAsync<ProcessorDomainException>(
            () => _processor.ExecuteAsync(context, CancellationToken.None));

        ex.ErrorCode.Should().Be(ErrorCodes.ConversionFailed);
    }

    [Fact]
    public async Task ExecuteAsync_MalformedToml_ThrowsProcessorDomainExceptionWithMalformedTomlCode()
    {
        const string badToml = "[invalid toml\nkey =\n";
        var context = BuildContext(badToml, FileFormats.Toml, FileFormats.Json);

        var ex = await Assert.ThrowsAsync<ProcessorDomainException>(
            () => _processor.ExecuteAsync(context, CancellationToken.None));

        ex.ErrorCode.Should().Be(ErrorCodes.MalformedToml);
    }

    [Fact]
    public async Task ExecuteAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        const string json = """{"name":"test"}""";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var context = BuildContext(json, FileFormats.Json, FileFormats.Toml, cts.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _processor.ExecuteAsync(context, cts.Token));
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
