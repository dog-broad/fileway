using Fileway.Shared.Formats;
using Fileway.Shared.Registry;
using Fileway.Shared.Tools;
using Microsoft.AspNetCore.Mvc;

namespace Fileway.Api.Endpoints;

public static class ToolEndpoints
{
    public static void MapToolEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/tools", GetTools).DisableRateLimiting();
        app.MapGet("/api/v1/tools/{slug}", GetTool).DisableRateLimiting();
    }

    private static IResult GetTools(
        HttpContext context,
        IToolRegistry toolRegistry,
        string? category = null,
        string? q = null)
    {
        IEnumerable<ToolDefinition> tools = toolRegistry.GetAll();

        if (!string.IsNullOrEmpty(category))
        {
            if (!Enum.TryParse<ToolCategory>(category, ignoreCase: true, out var cat))
                return Problem(400, "invalid-category", "Invalid category value.");
            tools = toolRegistry.GetByCategory(cat);
        }

        if (!string.IsNullOrEmpty(q) && q.Length >= 2)
        {
            var searchSlugs = toolRegistry.Search(q).Select(t => t.Slug).ToHashSet();
            tools = tools.Where(t => searchSlugs.Contains(t.Slug));
        }

        var summaries = tools.Select(ToSummary).ToArray();
        var etag = $"\"{string.Concat(summaries.Select(s => s.Slug)).GetHashCode():X}\"";

        context.Response.Headers.CacheControl = "public, max-age=3600";
        context.Response.Headers.ETag = etag;

        return Results.Ok(summaries);
    }

    private static IResult GetTool(string slug, IToolRegistry toolRegistry)
    {
        var tool = toolRegistry.GetBySlug(slug);
        return tool is null
            ? Problem(404, "unknowntoolslug", "Tool not found.")
            : Results.Ok(ToSummary(tool));
    }

    private static ToolSummary ToSummary(ToolDefinition t) => new()
    {
        Slug = t.Slug,
        DisplayName = t.DisplayName,
        ShortDescription = t.ShortDescription,
        Description = t.Description,
        Category = t.Category,
        Kind = t.Kind,
        AcceptedFormats = t.AcceptedFormats,
        OutputFormats = t.OutputFormats,
        IsNew = t.IsNew,
        IsPopular = t.IsPopular,
        MaxInputSizeBytes = t.MaxInputSizeBytes,
        AcceptsMultipleFiles = t.AcceptsMultipleFiles,
        CanonicalPath = t.CanonicalPath
    };

    private static IResult Problem(int status, string errorCode, string userMessage)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = userMessage,
            Type = $"https://fileway.io/errors/{errorCode}"
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["userMessage"] = userMessage;
        return Results.Json(problem, statusCode: status);
    }
}
