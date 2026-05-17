using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Shared.Processors;
using Tomlyn;
using Tomlyn.Model;

namespace Fileway.Client.Processors.DataFormats;

public sealed class JsonTomlProcessor : IWasmProcessor
{
    public bool CanHandleSize(long fileSizeBytes) => true;

    public void ValidateOptions(JsonElement toolOptions) { }

    public async Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken ct)
    {
        var input = context.InputFiles[0];
        var text = Encoding.UTF8.GetString(input.Content.Span);

        await Task.Yield();
        ct.ThrowIfCancellationRequested();

        bool toToml = input.DetectedFormat.Id == FileFormats.Json.Id;

        var (outputText, outputFormat) = toToml
            ? (JsonToToml(text), FileFormats.Toml)
            : (TomlToJson(text), FileFormats.Json);

        return new ProcessorResult
        {
            OutputContent = Encoding.UTF8.GetBytes(outputText),
            OutputFormat = outputFormat,
            OutputFilename = BuildFilename(input.OriginalFilename, outputFormat)
        };
    }

    private static string JsonToToml(string json)
    {
        JsonDocument doc;
        try { doc = JsonDocument.Parse(json); }
        catch (JsonException ex)
        {
            throw new ProcessorDomainException(ErrorCodes.MalformedJson, "Could not parse JSON input.", ex);
        }

        var root = doc.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new ProcessorDomainException(ErrorCodes.ConversionFailed,
                "TOML requires a root table — JSON input must be an object, not an array or scalar.");

        var table = ElementToTomlTable(root);
        return Toml.FromModel(table);
    }

    private static string TomlToJson(string toml)
    {
        TomlTable model;
        try { model = Toml.ToModel(toml); }
        catch (Exception ex)
        {
            throw new ProcessorDomainException(ErrorCodes.MalformedToml, "Could not parse TOML input.", ex);
        }

        var node = TableToJsonNode(model);
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static TomlTable ElementToTomlTable(JsonElement element)
    {
        var table = new TomlTable();
        foreach (var prop in element.EnumerateObject())
            table[prop.Name] = ElementToTomlValue(prop.Value);
        return table;
    }

    private static object ElementToTomlValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ElementToTomlTable(element),
            JsonValueKind.Array => ElementToTomlArray(element),
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.TryGetInt64(out var l) ? (object)l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => throw new ProcessorDomainException(ErrorCodes.ConversionFailed,
                "TOML does not support null values. Remove null fields before converting.")
        };
    }

    private static TomlArray ElementToTomlArray(JsonElement element)
    {
        var arr = new TomlArray();
        foreach (var item in element.EnumerateArray())
            arr.Add(ElementToTomlValue(item));
        return arr;
    }

    private static JsonObject TableToJsonNode(TomlTable table)
    {
        var node = new JsonObject();
        foreach (var (key, value) in table)
            node[key] = TomlValueToJsonNode(value);
        return node;
    }

    private static JsonNode? TomlValueToJsonNode(object? value) => value switch
    {
        null => null,
        bool b => JsonValue.Create(b),
        long l => JsonValue.Create(l),
        int i => JsonValue.Create(i),
        double d => JsonValue.Create(d),
        float f => JsonValue.Create((double)f),
        string s => JsonValue.Create(s),
        DateTime dt => JsonValue.Create(dt.ToString("o")),
        DateTimeOffset dto => JsonValue.Create(dto.ToString("o")),
        TomlTable t => TableToJsonNode(t),
        TomlArray arr => ArrayToJsonNode(arr),
        _ => JsonValue.Create(value.ToString())
    };

    private static JsonArray ArrayToJsonNode(TomlArray arr)
    {
        var jsonArr = new JsonArray();
        foreach (var item in arr)
            jsonArr.Add(TomlValueToJsonNode(item));
        return jsonArr;
    }

    private static string BuildFilename(string? original, FileFormat format)
    {
        var ext = format.Extensions[0];
        if (string.IsNullOrWhiteSpace(original)) return $"output.{ext}";
        return $"{Path.GetFileNameWithoutExtension(original)}.{ext}";
    }
}
