using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace Library.ControllerApi.Filters;

//Middleware are essentialy global - we can write filters and then active
public class TimingFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var sw = Stopwatch.StartNew();
        var executed = await next();
        sw.Stop();
        executed.HttpContext.Response.Headers["X-Elapsed-ms"] = sw.ElapsedMilliseconds.ToString();
    }
}