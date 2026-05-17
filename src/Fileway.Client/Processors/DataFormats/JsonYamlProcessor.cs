using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Fileway.Shared.Errors;
using Fileway.Shared.Formats;
using Fileway.Shared.Processors;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Fileway.Client.Processors.DataFormats;

public sealed class JsonYamlProcessor : IWasmProcessor
{
    public bool CanHandleSize(long fileSizeBytes) => true;

    public void ValidateOptions(JsonElement toolOptions) { }

    public async Task<ProcessorResult> ExecuteAsync(ProcessorContext context, CancellationToken ct)
    {
        var input = context.InputFiles[0];
        var text = Encoding.UTF8.GetString(input.Content.Span);

        await Task.Yield();
        ct.ThrowIfCancellationRequested();

        bool toYaml = input.DetectedFormat.Id == FileFormats.Json.Id;

        var (outputText, outputFormat) = toYaml
            ? (JsonToYaml(text), FileFormats.Yaml)
            : (YamlToJson(text), FileFormats.Json);

        return new ProcessorResult
        {
            OutputContent = Encoding.UTF8.GetBytes(outputText),
            OutputFormat = outputFormat,
            OutputFilename = BuildFilename(input.OriginalFilename, outputFormat)
        };
    }

    private static string JsonToYaml(string json)
    {
        JsonDocument doc;
        try { doc = JsonDocument.Parse(json); }
        catch (JsonException ex)
        {
            throw new ProcessorDomainException(ErrorCodes.MalformedJson, "Could not parse JSON input.", ex);
        }

        var obj = ElementToObject(doc.RootElement);
        return new SerializerBuilder().Build().Serialize(obj);
    }

    private static string YamlToJson(string yaml)
    {
        object? obj;
        try { obj = new DeserializerBuilder().Build().Deserialize<object>(yaml); }
        catch (YamlException ex)
        {
            throw new ProcessorDomainException(ErrorCodes.MalformedYaml, "Could not parse YAML input.", ex);
        }

        var node = ToJsonNode(obj);
        return node?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "null";
    }

    private static object? ElementToObject(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Object => e.EnumerateObject()
            .ToDictionary(p => p.Name, p => ElementToObject(p.Value)),
        JsonValueKind.Array => e.EnumerateArray().Select(ElementToObject).ToList(),
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.TryGetInt64(out var l) ? (object?)l : e.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };

    private static JsonNode? ToJsonNode(object? obj) => obj switch
    {
        null => null,
        bool b => JsonValue.Create(b),
        int i => JsonValue.Create(i),
        long l => JsonValue.Create(l),
        double d => JsonValue.Create(d),
        string s when s is "true" or "True" => JsonValue.Create(true),
        string s when s is "false" or "False" => JsonValue.Create(false),
        string s when long.TryParse(s, out var l) => JsonValue.Create(l),
        string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => JsonValue.Create(d),
        string s => JsonValue.Create(s),
        IDictionary<object, object> dict => MappingToObject(dict),
        IList<object> list => SequenceToArray(list),
        _ => JsonValue.Create(obj.ToString())
    };

    private static JsonObject MappingToObject(IDictionary<object, object> dict)
    {
        var node = new JsonObject();
        foreach (var (k, v) in dict)
            node[k.ToString()!] = ToJsonNode(v);
        return node;
    }

    private static JsonArray SequenceToArray(IList<object> list)
    {
        var arr = new JsonArray();
        foreach (var item in list)
            arr.Add(ToJsonNode(item));
        return arr;
    }

    private static string BuildFilename(string? original, FileFormat format)
    {
        var ext = format.Extensions[0];
        if (string.IsNullOrWhiteSpace(original)) return $"output.{ext}";
        return $"{Path.GetFileNameWithoutExtension(original)}.{ext}";
    }
}
