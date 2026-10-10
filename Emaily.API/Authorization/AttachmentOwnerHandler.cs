using Emaily.API.Extensions;
using Emaily.DAL.Interfaces;
using Emaily.DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Emaily.API.Authorization
{
    public class AttachmentOwnerHandler(IUnitOfWork unitOfWork)
     : AuthorizationHandler<AttachmentOwnerRequirement, string>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AttachmentOwnerRequirement requirement,
            string Id)
        {
            // Ownership check
            Guid userId = context.User.GetUserId();
            Guid attachmentId = Guid.Parse(Id);

            if (await unitOfWork.AttachmentRepository.IsOwnerAsync(userId, attachmentId))
            {
                context.Succeed(requirement);
            }
        }
    }
}