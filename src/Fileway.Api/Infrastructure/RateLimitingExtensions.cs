using System.Threading.RateLimiting;
using Fileway.Api.Configuration;
using Fileway.Shared.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fileway.Api.Infrastructure;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddFilewayRateLimiting(
        this IServiceCollection services,
        RateLimitOptions options)
    {
        services.AddRateLimiter(limiter =>
        {
            limiter.OnRejected = async (ctx, ct) =>
            {
                var retryAfter = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryVal)
                    ? (int)retryVal.TotalSeconds
                    : options.SessionTokenWindowSeconds;

                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                ctx.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString();

                var problem = new ProblemDetails
                {
                    Status = 429,
                    Title = "Rate limit exceeded.",
                    Type = "https://fileway.io/errors/ratelimitexceeded"
                };
                problem.Extensions["errorCode"] = ErrorCodes.RateLimitExceeded;
                problem.Extensions["userMessage"] = "You have made too many requests. Please wait before trying again.";
                problem.Extensions["suggestedAction"] = $"Try again in {retryAfter} seconds.";
                problem.Extensions["retryable"] = (object)true;

                await ctx.HttpContext.Response.WriteAsJsonAsync(problem, ct);
            };

            limiter.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                // Policy 1: per session token
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var token = context.Items[SessionTokenMiddleware.SessionTokenKey] as string;

                    // Unauthenticated endpoints are not session-token rate limited
                    if (string.IsNullOrEmpty(token))
                        return RateLimitPartition.GetNoLimiter("no-token");

                    var tier = context.RequestServices
                        .GetRequiredService<ITierResolver>()
                        .Resolve(token);

                    var limit = tier == Tier.Paid
                        ? options.SessionTokenPaidPermitLimit
                        : options.SessionTokenFreePermitLimit;

                    return RateLimitPartition.GetSlidingWindowLimiter(token, _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = limit,
                        Window = TimeSpan.FromSeconds(options.SessionTokenWindowSeconds),
                        SegmentsPerWindow = 4,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
                }),

                // Policy 2: per IP hash
                PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var ipHash = context.Items[SessionTokenMiddleware.IpHashKey] as string ?? "unknown";

                    return RateLimitPartition.GetSlidingWindowLimiter(ipHash, _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = options.IpHashPermitLimit,
                        Window = TimeSpan.FromSeconds(options.IpHashWindowSeconds),
                        SegmentsPerWindow = 4,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    });
                })
            );
        });

        return services;
    }
}
