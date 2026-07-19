namespace BloomRush.Api.Middleware;

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        // Process the rest of the pipeline (including EF Core calls)
        await _next(context);

        stopwatch.Stop();
        
        _logger.LogInformation("Request {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms", 
            context.Request.Method,
            context.Request.Path, 
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds
            );
    }
}