using System.Text.Json;
using Fileway.Client.Processors.DataFormats;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Tests.Client.Fixtures;

namespace Fileway.Tests.Client.Processors.Data;

public sealed class CsvToXlsxProcessorTests
{
    private const long WasmThresholdBytes = 5 * 1024 * 1024; // 5 MB

    private readonly CsvToXlsxProcessor _processor = new();

    // --- CanHandleSize ---

    [Fact]
    public void CanHandleSize_BelowThreshold_ReturnsTrue()
    {
        _processor.CanHandleSize(WasmThresholdBytes - 1).Should().BeTrue();
    }

    [Fact]
    public void CanHandleSize_AtThreshold_ReturnsTrue()
    {
        _processor.CanHandleSize(WasmThresholdBytes).Should().BeTrue();
    }

    [Fact]
    public void CanHandleSize_AboveThreshold_ReturnsFalse()
    {
        _processor.CanHandleSize(WasmThresholdBytes + 1).Should().BeFalse();
    }

    // --- ValidateOptions ---

    [Fact]
    public void ValidateOptions_EmptyObject_DoesNotThrow()
    {
        var options = JsonDocument.Parse("{}").RootElement;
        var act = () => _processor.ValidateOptions(options);
        act.Should().NotThrow();
    }

    // --- Happy path ---

    [Fact]
    public async Task ExecuteAsync_ValidCsv_ProducesXlsxOutput()
    {
        const string csv = "id,name,score\n1,alice,95\n2,bob,87\n";
        var context = BuildContext(csv, "data.csv");

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFormat.Id.Should().Be(FileFormats.Xlsx.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ValidCsv_OutputContentIsNonEmpty()
    {
        const string csv = "col1,col2\nval1,val2\n";
        var context = BuildContext(csv);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputContent.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ValidCsv_OutputBytesStartWithZipSignature()
    {
        // XLSX files are ZIP archives — first 4 bytes are PK\x03\x04
        const string csv = "a,b\n1,2\n";
        var context = BuildContext(csv);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        var bytes = result.OutputContent.Span;
        bytes.Length.Should().BeGreaterThan(4);
        bytes[0].Should().Be(0x50); // P
        bytes[1].Should().Be(0x4B); // K
        bytes[2].Should().Be(0x03);
        bytes[3].Should().Be(0x04);
    }

    [Fact]
    public async Task ExecuteAsync_ValidCsv_OutputFilenameHasXlsxExtension()
    {
        const string csv = "a,b\n1,2\n";
        var context = BuildContext(csv, "report.csv");

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFilename.Should().EndWith(".xlsx");
        result.OutputFilename.Should().NotContain("/");
        result.OutputFilename.Should().NotContain("\\");
    }

    [Fact]
    public async Task ExecuteAsync_CsvWithNoFilename_OutputFilenameIsDefaultXlsx()
    {
        const string csv = "a,b\n1,2\n";
        var context = BuildContext(csv, filename: null);

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFilename.Should().Be("output.xlsx");
    }

    [Fact]
    public async Task ExecuteAsync_CsvWithNumericValues_ProducesValidXlsx()
    {
        const string csv = "id,value,price\n1,100,9.99\n2,200,19.99\n3,300,29.99\n";
        var context = BuildContext(csv, "metrics.csv");

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        // If execution didn't throw and output is non-empty, numeric parsing succeeded
        result.OutputContent.IsEmpty.Should().BeFalse();
        result.OutputFormat.Id.Should().Be(FileFormats.Xlsx.Id);
    }

    // --- Cancellation ---

    [Fact]
    public async Task ExecuteAsync_PreCancelledToken_ThrowsOperationCanceledException()
    {
        const string csv = "a,b\n1,2\n";
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var context = BuildContext(csv, cancellationToken: cts.Token);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _processor.ExecuteAsync(context, cts.Token));
    }

    // --- Filename sanitisation ---

    [Fact]
    public async Task ExecuteAsync_FilenameWithPathSeparators_OutputFilenameIsClean()
    {
        const string csv = "a,b\n1,2\n";
        // Attempt to inject path separators — output filename must be clean
        var context = BuildContext(csv, "../../etc/passwd.csv");

        var result = await _processor.ExecuteAsync(context, CancellationToken.None);

        result.OutputFilename.Should().NotContain("/");
        result.OutputFilename.Should().NotContain("\\");
        result.OutputFilename.Should().NotContain("..");
    }

    // --- Helpers ---

    private ProcessorContext BuildContext(
        string csvText,
        string? filename = "input.csv",
        CancellationToken cancellationToken = default)
    {
        var file = TestFileFactory.FromText(csvText, FileFormats.Csv, filename: filename);
        return new ProcessorContextBuilder()
            .WithInputFile(file)
            .WithOutputFormat(FileFormats.Xlsx)
            .WithCancellationToken(cancellationToken)
            .Build();
    }
}
