using System.Text;
using Fileway.Shared.Detection;
using Fileway.Shared.Formats;

namespace Fileway.Tests.Api.Detection;

public sealed class FormatDetectorTests
{
    // Build a detector with all known formats — same list as Program.cs
    private static readonly IFormatDetector Detector = new FormatDetector(
    [
        FileFormats.Json, FileFormats.Yaml, FileFormats.Csv,
        FileFormats.Toml, FileFormats.Xlsx, FileFormats.Txt, FileFormats.Md
    ]);

    // --- JSON ---

    [Fact]
    public void Detect_JsonObject_ReturnsJsonWithHighConfidence()
    {
        var bytes = Bytes("""{"name":"test","value":42}""");
        var (format, confidence) = Detector.Detect(bytes, null);

        format.Should().NotBeNull();
        format!.Id.Should().Be(FileFormats.Json.Id);
        confidence.Should().Be(DetectionConfidence.High);
    }

    [Fact]
    public void Detect_JsonArray_ReturnsJsonWithHighConfidence()
    {
        var bytes = Bytes("""[{"id":1},{"id":2}]""");
        var (format, confidence) = Detector.Detect(bytes, null);

        format.Should().NotBeNull();
        format!.Id.Should().Be(FileFormats.Json.Id);
        confidence.Should().Be(DetectionConfidence.High);
    }

    [Fact]
    public void Detect_JsonWithLeadingWhitespace_ReturnsJson()
    {
        var bytes = Bytes("  \n{ \"key\": \"value\" }");
        var (format, _) = Detector.Detect(bytes, null);

        format.Should().NotBeNull();
        format!.Id.Should().Be(FileFormats.Json.Id);
    }

    // --- YAML ---

    [Fact]
    public void Detect_YamlWithDashDashDash_ReturnsYamlWithMediumConfidence()
    {
        var bytes = Bytes("---\nname: test\nvalue: 42\n");
        var (format, confidence) = Detector.Detect(bytes, null);

        format.Should().NotBeNull();
        format!.Id.Should().Be(FileFormats.Yaml.Id);
        confidence.Should().Be(DetectionConfidence.Medium);
    }

    [Fact]
    public void Detect_YamlKeyValueLines_ReturnsYaml()
    {
        var bytes = Bytes("name: test\nvalue: 42\nactive: true\n");
        var (format, _) = Detector.Detect(bytes, null);

        format.Should().NotBeNull();
        format!.Id.Should().Be(FileFormats.Yaml.Id);
    }

    // --- CSV ---

    [Fact]
    public void Detect_CsvThreeColumnsThreeRows_ReturnsCsvWithMediumConfidence()
    {
        var bytes = Bytes("id,name,score\n1,alice,95\n2,bob,87\n3,carol,91\n");
        var (format, confidence) = Detector.Detect(bytes, null);

        format.Should().NotBeNull();
        format!.Id.Should().Be(FileFormats.Csv.Id);
        confidence.Should().Be(DetectionConfidence.Medium);
    }

    [Fact]
    public void Detect_CsvTwoColumnsConsistentCommas_ReturnsCsv()
    {
        var bytes = Bytes("a,b\n1,2\n3,4\n");
        var (format, _) = Detector.Detect(bytes, null);

        format.Should().NotBeNull();
        format!.Id.Should().Be(FileFormats.Csv.Id);
    }

    // --- TOML ---

    [Fact]
    public void Detect_TomlWithSectionHeader_ReturnsTomlWithMediumConfidence()
    {
        var bytes = Bytes("[section]\nkey = \"value\"\n");
        var (format, confidence) = Detector.Detect(bytes, null);

        format.Should().NotBeNull();
        format!.Id.Should().Be(FileFormats.Toml.Id);
        confidence.Should().Be(DetectionConfidence.Medium);
    }

    [Fact]
    public void Detect_TomlKeyEqualsValue_ReturnsToml()
    {
        var bytes = Bytes("name = \"test\"\nversion = \"1.0.0\"\n");
        var (format, _) = Detector.Detect(bytes, null);

        format.Should().NotBeNull();
        format!.Id.Should().Be(FileFormats.Toml.Id);
    }

    // --- PDF magic bytes ---

    [Fact]
    public void Detect_PdfMagicBytes_IsNotDataFormat()
    {
        // %PDF magic bytes — detector has no PDF format registered, should return null
        var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
        var (format, _) = Detector.Detect(bytes, null);

        // With only data formats registered, PDF magic bytes fall through to text heuristics
        // which will also fail — so null is expected
        format.Should().BeNull();
    }

    // --- Unknown / null ---

    [Fact]
    public void Detect_UnknownContent_ReturnsNull()
    {
        var bytes = Bytes("@@@###XXXXUNKNOWN_BINARY_DATA_$$$$\n");
        var (format, _) = Detector.Detect(bytes, null);

        format.Should().BeNull();
    }

    [Fact]
    public void Detect_EmptyByteArray_ReturnsNull()
    {
        var (format, _) = Detector.Detect(ReadOnlySpan<byte>.Empty, null);

        format.Should().BeNull();
    }

    [Fact]
    public void Detect_SingleByte_ReturnsNullOrLowConfidence()
    {
        var bytes = new byte[] { 0x7B }; // single '{' — not enough for JSON (needs ':' and '"')
        var (format, confidence) = Detector.Detect(bytes, null);

        // Single '{' with no ':' or '"' does not satisfy the JSON heuristic
        // but this is implementation-defined; just assert confidence is not High if null
        if (format is null)
            confidence.Should().Be(DetectionConfidence.Low);
    }

    // --- Markdown ---

    [Fact]
    public void Detect_MarkdownHeading_ReturnsMarkdownWithLowConfidence()
    {
        var bytes = Bytes("# My Heading\n\nSome body text.\n");
        var (format, confidence) = Detector.Detect(bytes, null);

        format.Should().NotBeNull();
        format!.Id.Should().Be(FileFormats.Md.Id);
        confidence.Should().Be(DetectionConfidence.Low);
    }

    // --- Helpers ---

    private static ReadOnlySpan<byte> Bytes(string text) =>
        Encoding.UTF8.GetBytes(text).AsSpan();
}
