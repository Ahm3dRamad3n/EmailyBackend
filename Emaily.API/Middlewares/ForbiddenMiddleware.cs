using Emaily.API.Extensions;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.BLL.Helpers.Services;
using Microsoft.AspNetCore.Http;
using Superpower.Parsers;
using System.Net;
using System.Threading.Tasks;
using static Emaily.DAL.Entities.BanDetail;

namespace Emaily.API.Middlewares
{
    public class ForbiddenMiddleware
    {
        private readonly RequestDelegate _next;

        public ForbiddenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IBanManagerService banManager)
        {
            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
            {
                // 3. نتأكد إننا مسجلناش المخالفة دي قبل كده في نفس الريكويست عشان ميتعملش حظر مرتين
                if (!context.Items.ContainsKey("ViolationRecorded"))
                {
                    var identifier = context.GetUserIdentifier();
                    var userAgent = context.GetUserAgent();
                    var endpoint = context.GetEndpointPath();

                    int weight = (int)BanViolationWeights.UnauthorizedAccess;
                    string reason = "Unauthorized Access Attempt (403 Forbidden)";

                    // تسجيل الحظر
                    await banManager.RecordViolationAsync(identifier, reason, weight, userAgent, endpoint);

                    // نضع علامة أننا سجلنا المخالفة
                    context.Items["ViolationRecorded"] = true;
                }
            }
        }
    }
}