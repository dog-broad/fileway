using Fileway.Shared.Formats;
using Fileway.Shared.Tools;

namespace Fileway.Shared.Registry;

public interface IToolRegistry
{
    /// <summary>Resolves both canonical slugs and alias slugs. Returns the canonical ToolDefinition.</summary>
    ToolDefinition? GetBySlug(string slug);
    /// <summary>Returns alias metadata if <paramref name="slug"/> is an alias; null if canonical or unknown.</summary>
    ToolSlugAlias? GetAlias(string slug);
    IReadOnlyList<ToolDefinition> GetAll();
    IReadOnlyList<ToolDefinition> GetByCategory(ToolCategory category);
    IReadOnlyList<ToolDefinition> GetSuggestionsFor(FileFormat format, int limit);
    IReadOnlyList<ToolDefinition> GetRelated(string slug, int limit);
    IReadOnlyList<ToolDefinition> Search(string query);
    IReadOnlyList<ToolDefinition> GetAccepting(FileFormat format);
    IReadOnlyList<SitemapEntry> GetSitemapEntries();
    bool ValidateSlug(string slug);
}
