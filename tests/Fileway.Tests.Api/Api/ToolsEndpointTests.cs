using System.Net;
using System.Text.Json;
using Fileway.Tests.Api.Helpers;

namespace Fileway.Tests.Api.Api;

/// <summary>
/// Integration tests for GET /api/v1/tools and GET /api/v1/tools/{slug}.
/// Uses the real API pipeline via WebApplicationFactory.
/// </summary>
public sealed class ToolsEndpointTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public ToolsEndpointTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTools_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/tools");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTools_ReturnsAtLeastFiveTools()
    {
        var response = await _client.GetAsync("/api/v1/tools");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var tools = doc.RootElement.EnumerateArray().ToList();

        tools.Should().HaveCountGreaterThanOrEqualTo(5,
            "M1 defines 5 data tools");
    }

    [Fact]
    public async Task GetTools_EachToolHasSlugAndDisplayName()
    {
        var response = await _client.GetAsync("/api/v1/tools");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        foreach (var tool in doc.RootElement.EnumerateArray())
        {
            tool.TryGetProperty("slug", out var slugProp).Should().BeTrue();
            slugProp.GetString().Should().NotBeNullOrWhiteSpace();

            tool.TryGetProperty("displayName", out var nameProp).Should().BeTrue();
            nameProp.GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task GetTools_ResponseHasCacheControlHeader()
    {
        var response = await _client.GetAsync("/api/v1/tools");

        response.Headers.CacheControl.Should().NotBeNull();
        response.Headers.CacheControl!.MaxAge.Should().BePositive();
    }

    [Fact]
    public async Task GetToolBySlug_JsonToYaml_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/tools/json-to-yaml");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetToolBySlug_JsonToYaml_ReturnsCorrectSlug()
    {
        var response = await _client.GetAsync("/api/v1/tools/json-to-yaml");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("slug").GetString().Should().Be("json-to-yaml");
    }

    [Fact]
    public async Task GetToolBySlug_YamlToJson_AliasSlug_ReturnsOk()
    {
        // yaml-to-json is an alias — the endpoint resolves it via ToolRegistry.GetBySlug
        var response = await _client.GetAsync("/api/v1/tools/yaml-to-json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetToolBySlug_NonExistentSlug_Returns404()
    {
        var response = await _client.GetAsync("/api/v1/tools/totally-nonexistent-slug");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetToolBySlug_NonExistentSlug_ReturnsProblemDetailsJson()
    {
        var response = await _client.GetAsync("/api/v1/tools/totally-nonexistent-slug");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        // ProblemDetails extensions are inlined as top-level JSON properties by ASP.NET Core
        // The ToolEndpoints.Problem() method sets problem.Extensions["errorCode"] and ["userMessage"]
        doc.RootElement.TryGetProperty("errorCode", out _).Should().BeTrue(
            "ProblemDetails extensions are serialized as top-level properties");
    }
}
