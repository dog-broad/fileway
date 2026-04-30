using Fileway.Shared.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fileway.Api.Infrastructure;

public static class ProblemDetailsExceptionHandler
{
    public static void Configure(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            var logger = context.RequestServices
                .GetRequiredService<ILogger<Program>>();

            if (feature?.Error is not null)
            {
                // Log the full exception internally — never send it to the client
                logger.LogError(feature.Error, "Unhandled exception on {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }

            var problem = new ProblemDetails
            {
                Status = 500,
                Title = "An unexpected error occurred.",
                Type = "https://fileway.io/errors/unexpected"
            };
            problem.Extensions["errorCode"] = ErrorCodes.ProcessorUnexpectedError;
            problem.Extensions["userMessage"] = "Something went wrong. Please try again.";
            problem.Extensions["suggestedAction"] = "If the problem persists, try a different file.";
            problem.Extensions["retryable"] = (object)true;

            context.Response.StatusCode = 500;
            await context.Response.WriteAsJsonAsync(problem);
        });
    }
}
