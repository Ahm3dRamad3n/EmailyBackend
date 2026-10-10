using Emaily.API.Extensions;
using Emaily.DAL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Emaily.API.Authorization
{
    public class IntegrationOwnerHandler(IUnitOfWork unitOfWork)
     : AuthorizationHandler<IntegrationOwnerRequirement, string>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            IntegrationOwnerRequirement requirement,
            string Id)
        {
            // Ownership check
            Guid userId = context.User.GetUserId();
            Guid integrationId = Guid.Parse(Id);

            if (await unitOfWork.IntegrationRepository.IsOwnerAsync(userId, integrationId))
            {
                context.Succeed(requirement);
            }
        }
    }
}