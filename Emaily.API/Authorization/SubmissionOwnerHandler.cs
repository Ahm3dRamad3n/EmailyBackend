using Emaily.API.Extensions;
using Emaily.DAL.Interfaces;
using Emaily.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Emaily.API.Authorization
{
    public class SubmissionOwnerHandler(IUnitOfWork unitOfWork)
     : AuthorizationHandler<SubmissionOwnerRequirement, string>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            SubmissionOwnerRequirement requirement,
            string Id)
        {
            // Ownership check
            Guid userId = context.User.GetUserId();
            Guid submissionId = Guid.Parse(Id);

            if (await unitOfWork.SubmissionRepository.IsOwnerAsync(userId, submissionId))
            {
                context.Succeed(requirement);
            }
        }
    }
}