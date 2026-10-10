using Emaily.API.Extensions;
using Emaily.DAL.Interfaces;
using Emaily.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Emaily.API.Authorization
{
    public class ProjectOwnerHandler(IUnitOfWork unitOfWork)
     : AuthorizationHandler<ProjectOwnerRequirement, string>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ProjectOwnerRequirement requirement,
            string Id)
        {
            // Ownership check
            Guid userId = context.User.GetUserId();

            if (await unitOfWork.ProjectRepository.IsOwnerAsync(userId, Id))
            {
                context.Succeed(requirement);
            }
        }
    }
}