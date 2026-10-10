using Emaily.API.Extensions;
using Emaily.DAL.Interfaces;
using Emaily.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Emaily.API.Authorization
{
    public class ServiceOwnerHandler(IUnitOfWork unitOfWork)
     : AuthorizationHandler<ServiceOwnerRequirement, string>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ServiceOwnerRequirement requirement,
            string Id)
        {
            // Ownership check
            Guid userId = context.User.GetUserId();

            if (await unitOfWork.ServiceRepository.IsOwnerAsync(userId, Id))
            {
                context.Succeed(requirement);
            }
        }
    }
}