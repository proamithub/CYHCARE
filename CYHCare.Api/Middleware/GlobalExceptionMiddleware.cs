using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace CYHCare.Api.Middleware;

/// <summary>
/// Global exception handling middleware returning consistent JSON error envelopes.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var statusCode = (int)HttpStatusCode.InternalServerError;
        var message = "An internal server error occurred.";
        string? details = null;

        switch (exception)
        {
            case ArgumentException argEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = argEx.Message;
                break;

            case UnauthorizedAccessException authEx:
                statusCode = (int)HttpStatusCode.Unauthorized;
                message = authEx.Message;
                break;

            case KeyNotFoundException notFoundEx:
                statusCode = (int)HttpStatusCode.NotFound;
                message = notFoundEx.Message;
                break;

            case InvalidOperationException invalidOpEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                message = invalidOpEx.Message;
                break;

            case SqlException sqlEx:
                // Custom SP THROW errors (50001 - 50020)
                if (sqlEx.Number >= 50000 && sqlEx.Number <= 59999)
                {
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = sqlEx.Message;
                }
                else
                {
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    message = "A database operation failed.";
                    details = _env.IsDevelopment() ? sqlEx.Message : null;
                }
                break;

            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                message = "An unexpected error occurred.";
                details = _env.IsDevelopment() ? exception.Message : null;
                break;
        }

        context.Response.StatusCode = statusCode;

        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = message,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}
