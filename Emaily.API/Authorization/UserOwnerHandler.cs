using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Emaily.API.Authorization
{
    public class UserOwnerHandler
     : AuthorizationHandler<UserOwnerRequirement, string>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            UserOwnerRequirement requirement,
            string Id)
        {
            // Ownership check
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == Id)
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}