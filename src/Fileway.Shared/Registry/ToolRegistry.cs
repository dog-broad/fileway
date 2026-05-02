using Fileway.Shared.Formats;
using Fileway.Shared.Tools;

namespace Fileway.Shared.Registry;

public sealed class ToolRegistry : IToolRegistry
{
    private readonly IReadOnlyList<ToolDefinition> _all;
    private readonly Dictionary<string, ToolDefinition> _bySlug;
    private readonly Dictionary<string, ToolDefinition> _byAlias;
    private readonly Dictionary<string, ToolSlugAlias> _aliases;

    public ToolRegistry(IEnumerable<ToolDefinition> tools)
    {
        var ordered = tools
            .OrderBy(t => t.Category)
            .ThenBy(t => t.SortOrder)
            .ToList();

        _all = ordered;
        _bySlug = ordered.ToDictionary(t => t.Slug);

        _byAlias = [];
        _aliases = [];
        foreach (var tool in ordered)
        {
            foreach (var alias in tool.SlugAliases)
            {
                _byAlias[alias.Slug] = tool;
                _aliases[alias.Slug] = alias;
            }
        }
    }

    public ToolDefinition? GetBySlug(string slug) =>
        _bySlug.GetValueOrDefault(slug) ?? _byAlias.GetValueOrDefault(slug);

    public ToolSlugAlias? GetAlias(string slug) =>
        _aliases.GetValueOrDefault(slug);

    public IReadOnlyList<ToolDefinition> GetAll() => _all;

    public IReadOnlyList<ToolDefinition> GetByCategory(ToolCategory category) =>
        _all.Where(t => t.Category == category).ToList();

    public IReadOnlyList<ToolDefinition> GetSuggestionsFor(FileFormat format, int limit) =>
        _all
            .Where(t => t.AcceptedFormats.Any(f => f.Id == format.Id))
            .OrderByDescending(t => t.SuggestionWeight)
            .Take(limit)
            .ToList();

    public IReadOnlyList<ToolDefinition> GetRelated(string slug, int limit)
    {
        var tool = GetBySlug(slug);
        if (tool is null) return [];

        return tool.RelatedSlugs
            .Select(s => _bySlug.GetValueOrDefault(s))
            .Where(t => t is not null)
            .Cast<ToolDefinition>()
            .Take(limit)
            .ToList();
    }

    public IReadOnlyList<ToolDefinition> Search(string query) =>
        _all
            .Where(t =>
                t.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Tags.Any(tag => tag.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                t.SeoKeywords.Any(kw => kw.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    public IReadOnlyList<ToolDefinition> GetAccepting(FileFormat format) =>
        _all.Where(t => t.AcceptedFormats.Any(f => f.Id == format.Id)).ToList();

    public IReadOnlyList<SitemapEntry> GetSitemapEntries()
    {
        var entries = new List<SitemapEntry>();
        foreach (var tool in _all)
        {
            entries.Add(new SitemapEntry
            {
                Slug = tool.Slug,
                CanonicalPath = tool.CanonicalPath,
                SeoTitle = tool.SeoTitle,
                SeoDescription = tool.SeoDescription
            });
            foreach (var alias in tool.SlugAliases)
            {
                entries.Add(new SitemapEntry
                {
                    Slug = alias.Slug,
                    CanonicalPath = $"/tools/{alias.Slug}",
                    SeoTitle = alias.SeoTitle,
                    SeoDescription = alias.SeoDescription
                });
            }
        }
        return entries;
    }

    public bool ValidateSlug(string slug) =>
        _bySlug.ContainsKey(slug) || _byAlias.ContainsKey(slug);
}
