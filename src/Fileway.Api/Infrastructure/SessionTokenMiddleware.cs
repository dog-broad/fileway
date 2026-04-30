using System.Security.Cryptography;
using System.Text;
using Fileway.Api.Configuration;
using Fileway.Shared.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Fileway.Api.Infrastructure;

public sealed class SessionTokenMiddleware
{
    // HttpContext.Items keys consumed by rate limiter and audit log
    public const string SessionTokenKey = "SessionToken";
    public const string SessionPrefixKey = "SessionPrefix";
    public const string IpHashKey = "IpHash";

    private readonly RequestDelegate _next;
    private readonly ApiOptions _options;

    public SessionTokenMiddleware(RequestDelegate next, IOptions<ApiOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Always compute ipHash — rate limiter needs it for every request
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var dailySalt = ComputeDailySalt(_options.IpHashSalt);
        context.Items[IpHashKey] = ComputeHash(remoteIp + dailySalt);

        // Session token validation applies only to /api/v1/jobs paths
        if (context.Request.Path.StartsWithSegments("/api/v1/jobs", StringComparison.OrdinalIgnoreCase))
        {
            if (!context.Request.Headers.TryGetValue("X-Session-Token", out var tokenValues) ||
                string.IsNullOrWhiteSpace(tokenValues.FirstOrDefault()))
            {
                await WriteProblemAsync(context, 400, ErrorCodes.InvalidSessionToken,
                    "Session token is missing.", "Ensure X-Session-Token header is present.");
                return;
            }

            var token = tokenValues.First()!;

            if (!Guid.TryParse(token, out _))
            {
                await WriteProblemAsync(context, 400, ErrorCodes.InvalidSessionToken,
                    "Session token is not a valid UUID.", "Generate a new UUID v4 and include it as X-Session-Token.");
                return;
            }

            // Store token and prefix — never log the full token
            context.Items[SessionTokenKey] = token;
            context.Items[SessionPrefixKey] = token[..8];
        }

        await _next(context);
    }

    private static string ComputeDailySalt(string secretSalt)
    {
        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        return ComputeHash(dateStr + secretSalt);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static async Task WriteProblemAsync(
        HttpContext context, int status, string errorCode, string userMessage, string suggestedAction)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = userMessage,
            Type = $"https://fileway.io/errors/{errorCode.ToLowerInvariant()}"
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["userMessage"] = userMessage;
        problem.Extensions["suggestedAction"] = suggestedAction;
        problem.Extensions["retryable"] = false;

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem);
    }
}

public static class SessionTokenMiddlewareExtensions
{
    public static IApplicationBuilder UseSessionToken(this IApplicationBuilder app) =>
        app.UseMiddleware<SessionTokenMiddleware>();
}
