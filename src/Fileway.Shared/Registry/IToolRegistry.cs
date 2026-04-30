using Fileway.Shared.Formats;
using Fileway.Shared.Tools;

namespace Fileway.Shared.Registry;

public interface IToolRegistry
{
    ToolDefinition? GetBySlug(string slug);
    IReadOnlyList<ToolDefinition> GetAll();
    IReadOnlyList<ToolDefinition> GetByCategory(ToolCategory category);
    IReadOnlyList<ToolDefinition> GetSuggestionsFor(FileFormat format, int limit);
    IReadOnlyList<ToolDefinition> GetRelated(string slug, int limit);
    IReadOnlyList<ToolDefinition> Search(string query);
    IReadOnlyList<ToolDefinition> GetAccepting(FileFormat format);
    IReadOnlyList<SitemapEntry> GetSitemapEntries();
    bool ValidateSlug(string slug);
}
