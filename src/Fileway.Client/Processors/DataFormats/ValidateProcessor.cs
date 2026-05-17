using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Shared.Processors;
using Tomlyn;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Fileway.Client.Processors.DataFormats;

public sealed class ValidateProcessor : IWasmProcessor
{
    public bool CanHandleSize(long fileSizeBytes) => true;

    public void ValidateOptions(JsonElement toolOptions) { }

    public async Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken ct)
    {
        var input = context.InputFiles[0];
        var text = Encoding.UTF8.GetString(input.Content.Span);

        await Task.Yield();
        ct.ThrowIfCancellationRequested();

        var format = input.DetectedFormat;
        ValidateContent(text, format);

        // Return original content unchanged — validation passed
        return new ProcessorResult
        {
            OutputContent = input.Content,
            OutputFormat = format,
            OutputFilename = BuildFilename(input.OriginalFilename, format)
        };
    }

    private static void ValidateContent(string text, FileFormat format)
    {
        if (format.Id == FileFormats.Json.Id)
        {
            try { JsonDocument.Parse(text); }
            catch (JsonException ex)
            {
                throw new ProcessorDomainException(ErrorCodes.MalformedJson, "Invalid JSON: " + ex.Message, ex);
            }
        }
        else if (format.Id == FileFormats.Yaml.Id)
        {
            try { new DeserializerBuilder().Build().Deserialize<object>(text); }
            catch (YamlException ex)
            {
                throw new ProcessorDomainException(ErrorCodes.MalformedYaml, "Invalid YAML: " + ex.Message, ex);
            }
        }
        else if (format.Id == FileFormats.Csv.Id)
        {
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture) { BadDataFound = null };
                using var reader = new StringReader(text);
                using var csv = new CsvReader(reader, config);
                while (csv.Read()) { }
            }
            catch (CsvHelperException ex)
            {
                throw new ProcessorDomainException(ErrorCodes.InvalidCsv, "Invalid CSV: " + ex.Message, ex);
            }
        }
        else if (format.Id == FileFormats.Toml.Id)
        {
            var doc = Toml.Parse(text);
            if (doc.HasErrors)
            {
                var msg = doc.Diagnostics.Count > 0 ? doc.Diagnostics[0].Message : "Unknown error";
                throw new ProcessorDomainException(ErrorCodes.MalformedToml, "Invalid TOML: " + msg);
            }
        }
        else
        {
            throw new ProcessorDomainException(ErrorCodes.UnsupportedMediaType,
                $"Validation is not supported for format '{format.DisplayName}'.");
        }
    }

    private static string BuildFilename(string? original, FileFormat format)
    {
        var ext = format.Extensions[0];
        if (string.IsNullOrWhiteSpace(original)) return $"output.{ext}";
        return $"{Path.GetFileNameWithoutExtension(original)}.{ext}";
    }
}
