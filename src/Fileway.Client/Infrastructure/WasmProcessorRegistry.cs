namespace Fileway.Client.Infrastructure;

public sealed class WasmProcessorRegistry
{
    private readonly Dictionary<string, Type> _map = new(StringComparer.Ordinal);

    public void Register(string slug, Type processorType)
    {
        _map[slug] = processorType;
    }

    public Type? Get(string slug) => _map.TryGetValue(slug, out var t) ? t : null;

    public IReadOnlyDictionary<string, Type> All => _map;
}
