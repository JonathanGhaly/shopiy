using System.Net;
using System.Text.Json;
using Shopiy.Application.Common.Exceptions;
using NotFoundException = Shopiy.Application.Common.Exceptions.NotFoundException;
using ValidationException = Shopiy.Application.Common.Exceptions.ValidationException;

namespace Shopiy.Api.Middleware;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, code, message, details) = exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                "One or more validation errors occurred.",
                (object)ve.Errors),

            NotFoundException nfe => (
                HttpStatusCode.NotFound,
                "NOT_FOUND",
                nfe.Message,
                (object)new { }),

            ConflictException ce => (
                HttpStatusCode.Conflict,
                "CONFLICT",
                ce.Message,
                (object)new { }),

            ForbiddenAccessException fae => (
                HttpStatusCode.Forbidden,
                "FORBIDDEN",
                fae.Message,
                (object)new { }),

            UnauthorizedAccessException ue => (
                HttpStatusCode.Unauthorized,
                "AUTH_INVALID",
                ue.Message.Length > 0 ? ue.Message : "Authentication is required.",
                (object)new { }),

            InvalidOperationException ioe => (
                HttpStatusCode.BadRequest,
                "VALIDATION_ERROR",
                ioe.Message,
                (object)new { }),

            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred. Please try again later.",
                (object)new { })
        };

        var response = new
        {
            error = new
            {
                code,
                message,
                details
            }
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
