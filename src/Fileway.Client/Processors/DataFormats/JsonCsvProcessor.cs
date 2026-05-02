using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Shared.Processors;

namespace Fileway.Client.Processors.DataFormats;

public sealed class JsonCsvProcessor : IWasmProcessor
{
    public bool CanHandleSize(long fileSizeBytes) => true;

    public void ValidateOptions(JsonElement toolOptions) { }

    public async Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken ct)
    {
        var input = context.InputFiles[0];
        var text = Encoding.UTF8.GetString(input.Content.Span);

        await Task.Yield();
        ct.ThrowIfCancellationRequested();

        bool toCsv = input.DetectedFormat.Id == FileFormats.Json.Id;

        var (outputText, outputFormat) = toCsv
            ? (JsonToCsv(text), FileFormats.Csv)
            : (CsvToJson(text), FileFormats.Json);

        return new ProcessorResult
        {
            OutputContent = Encoding.UTF8.GetBytes(outputText),
            OutputFormat = outputFormat,
            OutputFilename = BuildFilename(input.OriginalFilename, outputFormat)
        };
    }

    private static string JsonToCsv(string json)
    {
        JsonDocument doc;
        try { doc = JsonDocument.Parse(json); }
        catch (JsonException ex)
        {
            throw new ProcessorDomainException(ErrorCodes.MalformedJson, "Could not parse JSON input.", ex);
        }

        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Array)
            throw new ProcessorDomainException(ErrorCodes.ConversionFailed,
                "JSON input must be an array of objects to convert to CSV.");

        var rows = root.EnumerateArray().ToList();
        if (rows.Count == 0) return string.Empty;

        if (rows[0].ValueKind != JsonValueKind.Object)
            throw new ProcessorDomainException(ErrorCodes.ConversionFailed,
                "JSON array elements must be objects to convert to CSV.");

        var headers = rows[0].EnumerateObject().Select(p => p.Name).ToList();

        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

        foreach (var h in headers) csv.WriteField(h);
        csv.NextRecord();

        foreach (var row in rows)
        {
            foreach (var h in headers)
            {
                if (row.TryGetProperty(h, out var val))
                    csv.WriteField(val.ValueKind == JsonValueKind.String ? val.GetString() : val.ToString());
                else
                    csv.WriteField(string.Empty);
            }
            csv.NextRecord();
        }

        return writer.ToString();
    }

    private static string CsvToJson(string csvText)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            BadDataFound = null
        };

        try
        {
            using var reader = new StringReader(csvText);
            using var csv = new CsvReader(reader, config);

            if (!csv.Read() || !csv.ReadHeader())
                return "[]";

            var headers = csv.HeaderRecord!;
            var rows = new List<Dictionary<string, string>>();

            while (csv.Read())
            {
                var row = new Dictionary<string, string>();
                foreach (var h in headers)
                    row[h] = csv.GetField(h) ?? string.Empty;
                rows.Add(row);
            }

            return JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (CsvHelperException ex)
        {
            throw new ProcessorDomainException(ErrorCodes.InvalidCsv, "Could not parse CSV input.", ex);
        }
    }

    private static string BuildFilename(string? original, FileFormat format)
    {
        var ext = format.Extensions[0];
        if (string.IsNullOrWhiteSpace(original)) return $"output.{ext}";
        return $"{Path.GetFileNameWithoutExtension(original)}.{ext}";
    }
}
