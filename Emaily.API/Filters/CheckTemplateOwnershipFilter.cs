using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Emaily.API.Filters
{
    public class CheckTemplateOwnershipFilter : IAsyncActionFilter
    {
        private readonly IAuthorizationService _authService;
        public CheckTemplateOwnershipFilter(IAuthorizationService authService)
        {
            _authService = authService;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.RouteData.Values.TryGetValue("templateId", out var idObj) && idObj is string templateId)
            {
                var authResult = await _authService.AuthorizeAsync(context.HttpContext.User, templateId, "IsTemplateOwner");
                if (!authResult.Succeeded)
                {
                    context.Result = new ObjectResult(new
                    {
                        success = false,
                        message = "You do not have permission to access or modify this template. It may belong to another user."
                    })
                    { StatusCode = StatusCodes.Status403Forbidden };
                    return;
                }
            }
            else
            {
                context.Result = new BadRequestObjectResult(new
                {
                    success = false,
                    message = "The 'templateId' or 'id' parameter is missing or invalid in the request URL."
                });
                return;
            }
            await next();
        }
    }
}
