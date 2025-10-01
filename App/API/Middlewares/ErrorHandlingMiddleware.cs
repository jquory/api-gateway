using System.Net;
using System.Text.Json;
using App.Core.Exceptions;
using App.Shared.Models;

namespace App.API.Middlewares;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = exception switch
        {
            GatewayException gateException => gateException.StatusCode,
            HttpRequestException => (int)HttpStatusCode.BadGateway,
            TaskCanceledException => (int)HttpStatusCode.RequestTimeout,
            _ => (int)HttpStatusCode.InternalServerError
        };
        
        var response = GatewayResponse<object>.ErrorResponse(exception.Message, statusCode);
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        
        await context.Response.WriteAsync(json);
    }
}