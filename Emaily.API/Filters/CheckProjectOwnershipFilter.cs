using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Emaily.API.Filters
{
    public class CheckProjectOwnershipFilter : IAsyncActionFilter
    {
        private readonly IAuthorizationService _authService;

        public CheckProjectOwnershipFilter(IAuthorizationService authService)
        {
            _authService = authService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.RouteData.Values.TryGetValue("projectId", out var idObj) && idObj is string projectId)
            {
                var authResult = await _authService.AuthorizeAsync(context.HttpContext.User, projectId, "IsProjectOwner");

                if (!authResult.Succeeded)
                {
                    context.Result = new ObjectResult(new
                    {
                        success = false,
                        message = "You do not have permission to access or modify this project. It may belong to another user."
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
                    message = "The 'projectId' or 'id' parameter is missing or invalid in the request URL."
                });
                return;
            }

            await next();
        }
    }
}
