namespace Fileway.Shared.Registry;

public sealed record SitemapEntry
{
    public required string Slug { get; init; }
    public required string CanonicalPath { get; init; }
    public required string SeoTitle { get; init; }
    public required string SeoDescription { get; init; }
}
