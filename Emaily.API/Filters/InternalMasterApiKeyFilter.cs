using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public class InternalMasterApiKeyFilter(IConfiguration config) : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "x-internal-key";
    private readonly string ValidKey = config["INTERNALM2M_MASTER_KEY"] ?? throw new Exception("INTERNALM2M_MASTER_KEY is not configured in appsettings.json or environment variables.");

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedKey))
        {
            context.Result = new UnauthorizedObjectResult("API Key is missing");
            return;
        }

        if (!ValidKey.Equals(extractedKey))
        {
            context.Result = new UnauthorizedObjectResult("Invalid API Key");
            return;
        }

        await next();
    }
}