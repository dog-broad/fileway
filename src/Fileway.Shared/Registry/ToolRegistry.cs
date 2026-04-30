using Fileway.Shared.Formats;
using Fileway.Shared.Tools;

namespace Fileway.Shared.Registry;

public sealed class ToolRegistry : IToolRegistry
{
    private readonly IReadOnlyList<ToolDefinition> _all;
    private readonly Dictionary<string, ToolDefinition> _bySlug;

    public ToolRegistry(IEnumerable<ToolDefinition> tools)
    {
        var ordered = tools
            .OrderBy(t => t.Category)
            .ThenBy(t => t.SortOrder)
            .ToList();

        _all = ordered;
        _bySlug = ordered.ToDictionary(t => t.Slug);
    }

    public ToolDefinition? GetBySlug(string slug) =>
        _bySlug.GetValueOrDefault(slug);

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
        if (!_bySlug.TryGetValue(slug, out var tool))
            return [];

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

    public IReadOnlyList<SitemapEntry> GetSitemapEntries() =>
        _all
            .Select(t => new SitemapEntry
            {
                Slug = t.Slug,
                CanonicalPath = t.CanonicalPath,
                SeoTitle = t.SeoTitle,
                SeoDescription = t.SeoDescription
            })
            .ToList();

    public bool ValidateSlug(string slug) => _bySlug.ContainsKey(slug);
}
