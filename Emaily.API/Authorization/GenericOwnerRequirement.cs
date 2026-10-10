using Microsoft.AspNetCore.Authorization;

namespace Emaily.API.Authorization
{
    public class UserOwnerRequirement : IAuthorizationRequirement
    {
    }

    public class ProjectOwnerRequirement : IAuthorizationRequirement
    {
    }

    public class ServiceOwnerRequirement : IAuthorizationRequirement
    {
    }

    public class TemplateOwnerRequirement : IAuthorizationRequirement
    {
    }

    public class IntegrationOwnerRequirement : IAuthorizationRequirement
    {
    }

    public class SubmissionOwnerRequirement : IAuthorizationRequirement
    {
    }

    public class AttachmentOwnerRequirement : IAuthorizationRequirement
    {
    }
}