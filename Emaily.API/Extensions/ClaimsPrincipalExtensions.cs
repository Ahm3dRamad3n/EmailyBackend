using System.Security.Claims;

namespace Emaily.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
        => Guid.Parse(principal.GetUserIdString());
    public static string GetUserIdString(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("User ID claim not found.");

    public static string GetUserEmail(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Email) ?? throw new InvalidOperationException("User email claim not found.");

    public static string GetUserName(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Name) ?? throw new InvalidOperationException("User name claim not found.");
}