using BabyCareApp.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Middleware;

/// <summary>
/// Maps typed domain exceptions to RFC 7807 Problem Details. No controller should
/// ever catch an exception itself — this is the single place that translates
/// business failures into HTTP responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, title) = ex switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                AuthenticationException => (StatusCodes.Status401Unauthorized, "Authentication Failed"),
                TenantMismatchException => (StatusCodes.Status403Forbidden, "Forbidden"),
                TenantRequiredException => (StatusCodes.Status400BadRequest, "Tenant Required"),
                UsageLimitExceededException => (StatusCodes.Status402PaymentRequired, "Usage Limit Exceeded"),
                ValidationException => (StatusCodes.Status400BadRequest, "Validation Failed"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error"),
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError
                    ? "An unexpected error occurred."
                    : ex.Message,
                Type = $"https://httpstatuses.io/{statusCode}",
                Instance = context.Request.Path,
            };

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
