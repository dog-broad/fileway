using System.Text;
using Fileway.Shared.Formats;

namespace Fileway.Shared.Detection;

public sealed class FormatDetector : IFormatDetector
{
    private readonly IReadOnlyList<FileFormat> _formats;
    private readonly Dictionary<string, FileFormat> _byId;

    public FormatDetector(IEnumerable<FileFormat> formats)
    {
        var list = formats.ToList();
        _formats = list;
        _byId = list.ToDictionary(f => f.Id);
    }

    public (FileFormat? Format, DetectionConfidence Confidence) Detect(
        ReadOnlySpan<byte> header, string? filename)
    {
        if (header.IsEmpty)
            return (null, DetectionConfidence.Low);

        // Pass 1: magic bytes
        foreach (var format in _formats)
        {
            foreach (var sig in format.MagicBytes)
            {
                if (!MatchesSignature(header, sig))
                    continue;

                // Pass 2: ZIP family needs disambiguation
                if (IsZipSignature(sig))
                    return DisambiguateZip(header);

                return (format, DetectionConfidence.High);
            }
        }

        // Pass 3: text heuristics — decode first 512 bytes as UTF-8
        var slice = header.Length > 512 ? header[..512] : header;
        // Strip UTF-8 BOM if present
        if (slice.Length >= 3 && slice[0] == 0xEF && slice[1] == 0xBB && slice[2] == 0xBF)
            slice = slice[3..];

        var text = Encoding.UTF8.GetString(slice);
        return ApplyTextHeuristics(text, filename);
    }

    // --- Pass 1 helpers ---

    private static bool MatchesSignature(ReadOnlySpan<byte> data, MagicSignature sig)
    {
        if (data.Length < sig.Offset + sig.Bytes.Length)
            return false;

        var window = data.Slice(sig.Offset, sig.Bytes.Length);

        if (sig.Mask is null)
            return window.SequenceEqual(sig.Bytes);

        for (var i = 0; i < sig.Bytes.Length; i++)
        {
            if ((window[i] & sig.Mask[i]) != sig.Bytes[i])
                return false;
        }
        return true;
    }

    private static bool IsZipSignature(MagicSignature sig) =>
        sig.Offset == 0 &&
        sig.Bytes.Length >= 4 &&
        sig.Bytes[0] == 0x50 && sig.Bytes[1] == 0x4B &&
        sig.Bytes[2] == 0x03 && sig.Bytes[3] == 0x04;

    // --- Pass 2: ZIP disambiguation ---

    private (FileFormat?, DetectionConfidence) DisambiguateZip(ReadOnlySpan<byte> header)
    {
        var names = ScanZipLocalEntryNames(header);

        bool hasContentTypes = names.Contains("[Content_Types].xml");
        bool hasXlDir        = names.Any(n => n.StartsWith("xl/", StringComparison.Ordinal));

        if (hasContentTypes && hasXlDir && _byId.TryGetValue("xlsx", out var xlsx))
            return (xlsx, DetectionConfidence.High);

        // Future milestones will add DOCX/PPTX disambiguation here
        return (null, DetectionConfidence.Low);
    }

    // Scans ZIP local file entry headers within the available bytes and returns filenames.
    // The central directory lives at the end of a ZIP, but local entries are at the start —
    // first 512 bytes cover the leading entries of well-formed OOXML files.
    private static List<string> ScanZipLocalEntryNames(ReadOnlySpan<byte> data)
    {
        var names = new List<string>();
        var pos = 0;

        while (pos + 30 <= data.Length)
        {
            // Local file header signature: PK\x03\x04
            if (data[pos] != 0x50 || data[pos + 1] != 0x4B ||
                data[pos + 2] != 0x03 || data[pos + 3] != 0x04)
            {
                pos++;
                continue;
            }

            var nameLen        = data[pos + 26] | (data[pos + 27] << 8);
            var extraLen       = data[pos + 28] | (data[pos + 29] << 8);
            var compressedSize = data[pos + 18] | (data[pos + 19] << 8)
                               | (data[pos + 20] << 16) | (data[pos + 21] << 24);

            if (pos + 30 + nameLen > data.Length)
                break;

            names.Add(Encoding.UTF8.GetString(data.Slice(pos + 30, nameLen)));
            pos += 30 + nameLen + extraLen + compressedSize;
        }

        return names;
    }

    // --- Pass 3: text heuristics ---

    private (FileFormat?, DetectionConfidence) ApplyTextHeuristics(string text, string? filename)
    {
        var trimmed = text.TrimStart();
        if (trimmed.Length == 0)
            return (null, DetectionConfidence.Low);

        // JSON: starts with { or [ and contains : and " within first 256 chars
        if (trimmed[0] == '{' || trimmed[0] == '[')
        {
            var sample = trimmed.Length > 256 ? trimmed[..256] : trimmed;
            if (sample.Contains(':') && sample.Contains('"') && _byId.TryGetValue("json", out var json))
                return (json, DetectionConfidence.High);
        }

        // YAML: starts with --- or has ≥2 key: value lines (not JSON)
        if (_byId.TryGetValue("yaml", out var yaml))
        {
            if (trimmed.StartsWith("---", StringComparison.Ordinal) ||
                (CountKeyValueLines(trimmed) >= 2 &&
                 trimmed[0] != '{' && trimmed[0] != '['))
                return (yaml, DetectionConfidence.Medium);
        }

        // TOML: has [section] header or key = value assignments
        if (_byId.TryGetValue("toml", out var toml))
        {
            if (HasTomlSection(trimmed) || HasTomlKeyValue(trimmed))
                return (toml, DetectionConfidence.Medium);
        }

        // CSV: consistent comma counts across first 3 lines, no JSON/YAML/TOML markers
        if (_byId.TryGetValue("csv", out var csv))
        {
            if (trimmed[0] != '{' && trimmed[0] != '[' &&
                !trimmed.StartsWith("---", StringComparison.Ordinal) &&
                HasConsistentCsvCommas(trimmed))
                return (csv, DetectionConfidence.Medium);
        }

        // Markdown: ATX heading — last resort; filename hint can promote confidence
        if (_byId.TryGetValue("md", out var md) && HasMarkdownHeading(trimmed))
            return (md, DetectionConfidence.Low);

        return (null, DetectionConfidence.Low);
    }

    private static int CountKeyValueLines(string text)
    {
        var count = 0;
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length > 2 &&
                char.IsLetterOrDigit(line[0]) &&
                line.Contains(':') &&
                !line.Contains('{'))
            {
                count++;
            }
            if (count >= 2) return count;
        }
        return count;
    }

    private static bool HasTomlSection(string text)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r').TrimStart();
            if (line.Length < 3 || line[0] != '[' || line.StartsWith("[[", StringComparison.Ordinal))
                continue;

            var closeIdx = line.IndexOf(']');
            if (closeIdx < 2) continue;

            var inner = line[1..closeIdx];
            if (inner.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-'))
                return true;
        }
        return false;
    }

    private static bool HasTomlKeyValue(string text)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length > 4 &&
                char.IsLetter(line[0]) &&
                line.Contains(" = "))
                return true;
        }
        return false;
    }

    private static bool HasConsistentCsvCommas(string text)
    {
        var commas = new int[3];
        var lineIdx = 0;

        foreach (var rawLine in text.Split('\n'))
        {
            if (lineIdx >= 3) break;
            var line = rawLine.TrimEnd('\r');
            var count = 0;
            foreach (var c in line) if (c == ',') count++;
            commas[lineIdx++] = count;
        }

        if (lineIdx < 2 || commas[0] < 1) return false;

        for (var i = 1; i < lineIdx; i++)
        {
            if (Math.Abs(commas[i] - commas[0]) > 1) return false;
        }
        return true;
    }

    private static bool HasMarkdownHeading(string text)
    {
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("# ", StringComparison.Ordinal)  ||
                line.StartsWith("## ", StringComparison.Ordinal) ||
                line.StartsWith("### ", StringComparison.Ordinal))
                return true;
        }
        return false;
    }
}
