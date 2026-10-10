using Emaily.API.Extensions;
using Emaily.DAL.Interfaces;
using Emaily.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Emaily.API.Authorization
{
    public class TemplateOwnerHandler(IUnitOfWork unitOfWork)
     : AuthorizationHandler<TemplateOwnerRequirement, string>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            TemplateOwnerRequirement requirement,
            string Id)
        {
            // Ownership check
            Guid userId = context.User.GetUserId();

            if (await unitOfWork.TemplateRepository.IsOwnerAsync(userId, Id))
            {
                context.Succeed(requirement);
            }
        }
    }
}