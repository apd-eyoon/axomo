using Microsoft.AspNetCore.Mvc;

namespace StoreCreditor.Web.Middleware;

public sealed class ProblemDetailsExceptionMiddleware(RequestDelegate next, ILogger<ProblemDetailsExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled request exception.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "An unexpected error occurred.",
                Detail = "The request could not be completed.",
                Status = StatusCodes.Status500InternalServerError,
                Instance = context.Request.Path
            });
        }
    }
}
