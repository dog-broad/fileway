using System.Text;
using System.Text.Json;
using Fileway.Client.Processors.DataFormats;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Tests.Client.Fixtures;

namespace Fileway.Tests.Client.Processors.Data;

public sealed class JsonCsvProcessorTests
{
    private readonly JsonCsvProcessor _processor = new();

    // --- CanHandleSize ---

    [Fact]
    public void CanHandleSize_AnySize_ReturnsTrue()
    {
        _processor.CanHandleSize(0).Should().BeTrue();
        _processor.CanHandleSize(5 * 1024 * 1024).Should().BeTrue();
    }

    // --- JSON → CSV ---

    [Fact]
    public async Task ExecuteAsync_JsonArrayInput_ProducesCsvOutput()
    {
        const string json = """[{"id":1,"name":"alice"},{"id":2,"name":"bob"}]""";
        var context = BuildContext(json, FileFormats.Json, FileFormats.Csv);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFormat.Id.Should().Be(FileFormats.Csv.Id);
    }

    [Fact]
    public async Task ExecuteAsync_JsonArrayInput_OutputContentIsNonEmpty()
    {
        const string json = """[{"id":1,"name":"alice"},{"id":2,"name":"bob"}]""";
        var context = BuildContext(json, FileFormats.Json, FileFormats.Csv);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputContent.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_JsonArrayInput_CsvOutputContainsHeaders()
    {
        const string json = """[{"id":1,"name":"alice"},{"id":2,"name":"bob"}]""";
        var context = BuildContext(json, FileFormats.Json, FileFormats.Csv);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        var csv = Encoding.UTF8.GetString(result.OutputContent.Span);
        csv.Should().Contain("id");
        csv.Should().Contain("name");
    }

    [Fact]
    public async Task ExecuteAsync_JsonArrayInput_OutputFilenameHasCsvExtension()
    {
        const string json = """[{"id":1,"name":"alice"}]""";
        var context = BuildContextWithFilename(json, FileFormats.Json, "data.json", FileFormats.Csv);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFilename.Should().EndWith(".csv");
        result.OutputFilename.Should().NotContain("/");
        result.OutputFilename.Should().NotContain("\\");
    }

    // --- CSV → JSON ---

    [Fact]
    public async Task ExecuteAsync_CsvInput_ProducesJsonOutput()
    {
        const string csv = "id,name\n1,alice\n2,bob\n";
        var context = BuildContext(csv, FileFormats.Csv, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFormat.Id.Should().Be(FileFormats.Json.Id);
    }

    [Fact]
    public async Task ExecuteAsync_CsvInput_OutputContentIsNonEmpty()
    {
        const string csv = "id,name\n1,alice\n2,bob\n";
        var context = BuildContext(csv, FileFormats.Csv, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputContent.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_CsvInput_JsonOutputIsValidJsonArray()
    {
        const string csv = "id,name\n1,alice\n2,bob\n";
        var context = BuildContext(csv, FileFormats.Csv, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        var json = Encoding.UTF8.GetString(result.OutputContent.Span);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task ExecuteAsync_CsvInput_JsonArrayContainsRowData()
    {
        const string csv = "id,name\n1,alice\n2,bob\n";
        var context = BuildContext(csv, FileFormats.Csv, FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        var json = Encoding.UTF8.GetString(result.OutputContent.Span);
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetArrayLength().Should().Be(2);

        var first = doc.RootElement[0];
        first.GetProperty("name").GetString().Should().Be("alice");
    }

    [Fact]
    public async Task ExecuteAsync_CsvInput_OutputFilenameHasJsonExtension()
    {
        const string csv = "a,b\n1,2\n";
        var context = BuildContextWithFilename(csv, FileFormats.Csv, "report.csv", FileFormats.Json);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFilename.Should().EndWith(".json");
    }

    // --- Round-trip ---

    [Fact]
    public async Task ExecuteAsync_RoundTripJsonCsvJson_PreservesRowCount()
    {
        const string originalJson = """[{"id":1,"name":"alice"},{"id":2,"name":"bob"},{"id":3,"name":"carol"}]""";

        // JSON → CSV
        var toCsv = BuildContext(originalJson, FileFormats.Json, FileFormats.Csv);
        var csvResult = await _processor.ExecuteAsync(toCsv, CancellationToken.None);
        var csv = Encoding.UTF8.GetString(csvResult.OutputContent.Span);

        // CSV → JSON
        var toJson = BuildContext(csv, FileFormats.Csv, FileFormats.Json);
        var jsonResult = await _processor.ExecuteAsync(toJson, CancellationToken.None);
        var roundTripJson = Encoding.UTF8.GetString(jsonResult.OutputContent.Span);

        using var doc = JsonDocument.Parse(roundTripJson);
        doc.RootElement.GetArrayLength().Should().Be(3);
    }

    // --- Error cases ---

    [Fact]
    public async Task ExecuteAsync_MalformedJson_ThrowsProcessorDomainException()
    {
        const string badJson = "{ not valid json ]";
        var context = BuildContext(badJson, FileFormats.Json, FileFormats.Csv);

        await Assert.ThrowsAsync<ProcessorDomainException>(
            () => _processor.ExecuteAsync(context, CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_MalformedJson_ErrorCodeIsMalformedJson()
    {
        const string badJson = "BROKEN";
        var context = BuildContext(badJson, FileFormats.Json, FileFormats.Csv);

        var ex = await Assert.ThrowsAsync<ProcessorDomainException>(
            () => _processor.ExecuteAsync(context, CancellationToken.None));

        ex.ErrorCode.Should().Be(ErrorCodes.MalformedJson);
    }

    [Fact]
    public async Task ExecuteAsync_JsonObjectInsteadOfArray_ThrowsProcessorDomainException()
    {
        // JSON root must be an array for CSV conversion
        const string json = """{"name":"alice"}""";
        var context = BuildContext(json, FileFormats.Json, FileFormats.Csv);

        var ex = await Assert.ThrowsAsync<ProcessorDomainException>(
            () => _processor.ExecuteAsync(context, CancellationToken.None));

        ex.ErrorCode.Should().Be(ErrorCodes.JsonNotCsvCompatible);
    }

    [Fact]
    public async Task ExecuteAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        const string json = """[{"id":1}]""";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var context = BuildContext(json, FileFormats.Json, FileFormats.Csv, cts.Token);

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
