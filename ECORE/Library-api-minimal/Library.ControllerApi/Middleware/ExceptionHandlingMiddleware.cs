using System.Text.Json; // JsonSerializer lives here

namespace Library.ControllerApi.Middleware;

// Just a class - doesn't inherit from any special class
public class ExceptionHandlingMiddleware
{
    // represents the call to that next middleware in the chain
    // passed in by whatever middleware preceded this one
    private readonly RequestDelegate _next; 
    private readonly ILogger<ExceptionHandlingMiddleware> _log;
    
    // This stuff comes in from ASP.NET  and the previous middleware delegate
    public ExceptionHandlingMiddleware(
        RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log
    )
    {
        _next = next;
        _log = log;
    }

    // Contain one method 
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception on {Path}", ctx.Request.Path);
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "application/json";

            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "An unexpected error occurred.",
                traceId = ctx.TraceIdentifier // the stack trace
            }));

        }
    }

}