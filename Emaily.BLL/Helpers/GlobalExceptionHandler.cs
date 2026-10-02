using Emaily.BLL.DTOs;
using Emaily.BLL.Helpers.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Emaily.BLL.Helpers
{
    public class GlobalExceptionHandler() : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var _loggerService = httpContext.RequestServices.GetRequiredService<ILoggerService>();
            var _backgroundJobs = httpContext.RequestServices.GetRequiredService<IBackgroundJobClient>();

            // 1. محاولة جلب ID المستخدم إذا كان مسجل الدخول (لنعرف من الذي واجه الخطأ)
            Guid userId = Guid.Empty; // قيمة افتراضية إذا لم يكن المستخدم مسجل الدخول
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var parsedId))
            {
                userId = parsedId;
            }

            // 2. تسجيل الخطأ في قاعدة البيانات (في الخلفية باستخدام الطابور الذي صممناه)
            // استخدام LogCritical لأن هذا خطأ أدى لانهيار الريكويست
            var logDto = new Log(
                userId,
                "Unhandled Exception Occurred: " + exception.Message[..Math.Min(exception.Message.Length, 400)],
                null, // ProjectId غير معروف هنا
                exception.ToString()
            );

            _loggerService.LogCritical(logDto, callerName: "GlobalExceptionHandler", filePath: "GlobalExceptionHandler.cs");

            // 3. ارسال اشعار للادمن عبر البريد الالكتروني 
            string body = EmailTemplateBuilder.GenerateCriticalErrorTemplate(logDto.Message, logDto.ExceptionDetails, logDto.UserId.ToString(), logDto.ProjectId);
            _backgroundJobs.Enqueue<IEmailSenderService>(email =>
                    email.NotifyAdminAsync("Critical Error Occurred in Emaily Application", body, null, cancellationToken, null));

            // 4. تهيئة الرد (Result) للمستخدم
            // إرجاع 500 Internal Server Error
            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/json";

            // 5. رسالة JSON أنيقة وموحدة
            var response = new
            {
                success = false,
                message = "An unexpected error occurred. Our technical team has been notified and is working on it.",
                errorCode = "INTERNAL_SERVER_ERROR"
            };

            await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

            // إرجاع true يعني: "لقد تعاملت مع الخطأ، لا ترميه للسيرفر وتجعله ينهار"
            return true;
        }
    }
}