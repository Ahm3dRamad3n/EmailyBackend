using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Emaily.API.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetUserIdentifier(this HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var email = context.User.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrEmpty(email))
                {
                    return email;
                }
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        public static string GetUserAgent(this HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("User-Agent", out var userAgent))
            {
                return userAgent.ToString();
            }
            return "unknown";
        }

        public static string GetOrigin(this HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("Origin", out var origin))
            {
                return origin.ToString();
            }
            else if (context.Request.Headers.TryGetValue("Referer", out var referer))
            {
                return referer.ToString();
            }
            return "unknown";
        }

        public static string GetClientIpAddress(this HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        public static string GetEndpointPath(this HttpContext context)
        {
            return context.Request.Path.ToString();
        }
    }
}