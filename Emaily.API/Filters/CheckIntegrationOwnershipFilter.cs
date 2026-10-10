using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Emaily.API.Filters
{
    public class CheckIntegrationOwnershipFilter : IAsyncActionFilter
    {
        private readonly IAuthorizationService _authService;

        public CheckIntegrationOwnershipFilter(IAuthorizationService authService)
        {
            _authService = authService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.RouteData.Values.TryGetValue("integrationId", out var idObj) && idObj is string integrationId)
            {
                var authResult = await _authService.AuthorizeAsync(context.HttpContext.User, integrationId, "IsIntegrationOwner");

                if (!authResult.Succeeded)
                {
                    context.Result = new ObjectResult(new
                    {
                        success = false,
                        message = "You do not have permission to access or modify this integration. It may belong to another user."
                    })
                    {
                        StatusCode = StatusCodes.Status403Forbidden
                    };
                    return;
                }
            }
            else
            {
                context.Result = new BadRequestObjectResult(new
                {
                    success = false,
                    message = "The 'integrationId' parameter is missing or invalid in the request URL."
                });
                return;
            }

            await next();
        }
    }
}
