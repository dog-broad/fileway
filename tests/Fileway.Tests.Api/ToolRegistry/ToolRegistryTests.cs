using Fileway.Shared.Registry;
using Fileway.Shared.Tools.Definitions;

namespace Fileway.Tests.Api.ToolRegistry;

public sealed class ToolRegistryTests
{
    private static readonly IToolRegistry Registry = new Fileway.Shared.Registry.ToolRegistry(DataTools.All);

    // --- GetBySlug: canonical slugs ---

    [Fact]
    public void GetBySlug_JsonToYaml_ReturnsNonNull()
    {
        var tool = Registry.GetBySlug("json-to-yaml");

        tool.Should().NotBeNull();
        tool!.Slug.Should().Be("json-to-yaml");
    }

    [Fact]
    public void GetBySlug_JsonToCsv_ReturnsNonNull()
    {
        var tool = Registry.GetBySlug("json-to-csv");

        tool.Should().NotBeNull();
        tool!.Slug.Should().Be("json-to-csv");
    }

    [Fact]
    public void GetBySlug_JsonToToml_ReturnsNonNull()
    {
        var tool = Registry.GetBySlug("json-to-toml");

        tool.Should().NotBeNull();
    }

    [Fact]
    public void GetBySlug_Validate_ReturnsNonNull()
    {
        var tool = Registry.GetBySlug("validate");

        tool.Should().NotBeNull();
    }

    [Fact]
    public void GetBySlug_CsvToXlsx_ReturnsNonNull()
    {
        var tool = Registry.GetBySlug("csv-to-xlsx");

        tool.Should().NotBeNull();
    }

    // --- GetBySlug: alias slugs (bidirectional reverse-direction URLs) ---

    [Fact]
    public void GetBySlug_YamlToJson_AliasResolvesToCanonicalTool()
    {
        var tool = Registry.GetBySlug("yaml-to-json");

        tool.Should().NotBeNull("yaml-to-json is a registered alias slug");
        // Canonical slug is json-to-yaml — alias resolves to the same ToolDefinition
        tool!.Slug.Should().Be("json-to-yaml");
    }

    [Fact]
    public void GetBySlug_CsvToJson_AliasResolvesToJsonToCsvTool()
    {
        var tool = Registry.GetBySlug("csv-to-json");

        tool.Should().NotBeNull();
        tool!.Slug.Should().Be("json-to-csv");
    }

    [Fact]
    public void GetBySlug_TomlToJson_AliasResolvesToJsonToTomlTool()
    {
        var tool = Registry.GetBySlug("toml-to-json");

        tool.Should().NotBeNull();
        tool!.Slug.Should().Be("json-to-toml");
    }

    // --- GetBySlug: unknown slug ---

    [Fact]
    public void GetBySlug_NonExistentSlug_ReturnsNull()
    {
        var tool = Registry.GetBySlug("nonexistent-slug-xyz");

        tool.Should().BeNull();
    }

    [Fact]
    public void GetBySlug_EmptyString_ReturnsNull()
    {
        var tool = Registry.GetBySlug(string.Empty);

        tool.Should().BeNull();
    }

    // --- GetAll ---

    [Fact]
    public void GetAll_ReturnsAtLeastFiveTools()
    {
        var tools = Registry.GetAll();

        tools.Should().HaveCountGreaterThanOrEqualTo(5,
            "M1 defines 5 data tools: json-to-yaml, json-to-csv, json-to-toml, validate, csv-to-xlsx");
    }

    // --- Data integrity: every tool must have non-empty identity fields ---

    [Fact]
    public void AllTools_HaveNonEmptySlug()
    {
        var tools = Registry.GetAll();

        foreach (var tool in tools)
            tool.Slug.Should().NotBeNullOrWhiteSpace("every tool must have a non-empty Slug");
    }

    [Fact]
    public void AllTools_HaveNonEmptyDisplayName()
    {
        var tools = Registry.GetAll();

        foreach (var tool in tools)
            tool.DisplayName.Should().NotBeNullOrWhiteSpace($"tool '{tool.Slug}' has empty DisplayName");
    }

    [Fact]
    public void AllTools_HaveNonEmptyDescription()
    {
        var tools = Registry.GetAll();

        foreach (var tool in tools)
            tool.Description.Should().NotBeNullOrWhiteSpace($"tool '{tool.Slug}' has empty Description");
    }

    [Fact]
    public void AllTools_HaveNonEmptySeoTitle()
    {
        var tools = Registry.GetAll();

        foreach (var tool in tools)
            tool.SeoTitle.Should().NotBeNullOrWhiteSpace($"tool '{tool.Slug}' has empty SeoTitle");
    }

    [Fact]
    public void AllTools_HaveAtLeastOneAcceptedFormat()
    {
        var tools = Registry.GetAll();

        foreach (var tool in tools)
            tool.AcceptedFormats.Should().NotBeEmpty($"tool '{tool.Slug}' has no AcceptedFormats");
    }

    // --- Alias round-trip integrity ---

    [Fact]
    public void GetAlias_YamlToJson_ReturnsAliasMetadata()
    {
        var alias = Registry.GetAlias("yaml-to-json");

        alias.Should().NotBeNull();
        alias!.Slug.Should().Be("yaml-to-json");
    }

    [Fact]
    public void GetAlias_CanonicalSlug_ReturnsNull()
    {
        // A canonical slug is not an alias
        var alias = Registry.GetAlias("json-to-yaml");

        alias.Should().BeNull("canonical slugs are not aliases");
    }

    // --- ValidateSlug ---

    [Fact]
    public void ValidateSlug_KnownCanonical_ReturnsTrue()
    {
        Registry.ValidateSlug("json-to-yaml").Should().BeTrue();
    }

    [Fact]
    public void ValidateSlug_KnownAlias_ReturnsTrue()
    {
        Registry.ValidateSlug("yaml-to-json").Should().BeTrue();
    }

    [Fact]
    public void ValidateSlug_Unknown_ReturnsFalse()
    {
        Registry.ValidateSlug("does-not-exist").Should().BeFalse();
    }

    // --- GetSitemapEntries ---

    [Fact]
    public void GetSitemapEntries_IncludesCanonicalAndAliasSlugs()
    {
        var entries = Registry.GetSitemapEntries();

        entries.Should().Contain(e => e.Slug == "json-to-yaml");
        entries.Should().Contain(e => e.Slug == "yaml-to-json",
            "yaml-to-json alias must appear in sitemap");
    }
}
