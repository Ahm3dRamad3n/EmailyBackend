using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Emaily.API.Extensions;
using Emaily.BLL.Helpers.Interfaces;

namespace Emaily.API.Middlewares
{
    public class BanCheckMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context, IBanManagerService banManager)
        {
            var identifier = context.GetUserIdentifier();

            if (banManager.IsBanned(identifier))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{\"error\": \"You are temporarily banned due to suspicious activity.\"}");
                return;
            }

            await _next(context);
        }
    }
}