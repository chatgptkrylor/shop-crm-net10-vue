using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ShopApi.Infrastructure;

public class XRequestedWithFilter : IAsyncActionFilter
{
    private static readonly string[] MutatingMethods = { "POST", "PUT", "DELETE", "PATCH" };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var method = context.HttpContext.Request.Method;
        if (MutatingMethods.Contains(method, StringComparer.OrdinalIgnoreCase))
        {
            var header = context.HttpContext.Request.Headers["X-Requested-With"].ToString();
            if (!string.Equals(header, "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new BadRequestObjectResult(new ProblemDetails
                {
                    Title = "Bad Request",
                    Status = 400,
                    Detail = "Missing or invalid X-Requested-With header",
                });
                return;
            }
        }
        await next();
    }
}