using System.Diagnostics;

namespace App.API.Middlewares;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();
        
        context.Request.Headers.Add("X-Request-Id", requestId);
        
        logger.LogInformation(
            "Request Id: {RequestId} Started: {Method} {Path}", 
            requestId, 
            context.Request.Method, 
            context.Request.Path);

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            logger.LogInformation(
                "Request {Request} completed: {Method} - {Path} - Status {StatusCode} - Duration {Duration}ms",
                requestId,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}