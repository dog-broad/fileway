using Fileway.Api.Jobs;
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
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var error = feature?.Error;

            int statusCode;
            ProblemDetails problem;

            switch (error)
            {
                case ProcessorValidationException vex:
                    statusCode = 422;
                    problem = Build(statusCode, "Validation failed.", vex.ErrorCode, vex.Message, false);
                    break;

                case ProcessorDomainException dex:
                    statusCode = 422;
                    problem = Build(statusCode, "Processing failed.", dex.ErrorCode, dex.Message, false);
                    break;

                case ProcessorUnexpectedException uex:
                    logger.LogError(uex.InnerException ?? uex, "Unexpected processor error on {Method} {Path}",
                        context.Request.Method, context.Request.Path);
                    statusCode = 500;
                    problem = Build(statusCode, "An unexpected error occurred.",
                        ErrorCodes.ProcessorUnexpectedError, "Something went wrong. Please try again.", true);
                    break;

                case JobDispatchException jex:
                    statusCode = jex.HttpStatusCode;
                    problem = Build(statusCode, jex.Message, jex.ErrorCode, jex.Message,
                        jex.HttpStatusCode is 429 or 503);
                    break;

                default:
                    if (error is not null)
                        logger.LogError(error, "Unhandled exception on {Method} {Path}",
                            context.Request.Method, context.Request.Path);
                    statusCode = 500;
                    problem = Build(statusCode, "An unexpected error occurred.",
                        ErrorCodes.ProcessorUnexpectedError, "Something went wrong. Please try again.", true);
                    break;
            }

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(problem);
        });
    }

    private static ProblemDetails Build(
        int status, string title, string errorCode, string userMessage, bool retryable)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://fileway.io/errors/{errorCode.ToLowerInvariant()}"
        };
        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["userMessage"] = userMessage;
        problem.Extensions["retryable"] = (object)retryable;
        return problem;
    }
}
