using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Emaily.Api.Controllers
{
    [Route("api/oauth")]
    [ApiController]
    [EnableRateLimiting("forOAuth")]
    public class OAuthController : ControllerBase
    {
        [HttpGet("authorize")]
        public IActionResult Authorize([FromQuery] string provider)
        {
            if (provider != "Google" && provider != "Microsoft")
            {
                return BadRequest("Invalid provider. Supported providers are Google and Microsoft.");
            }

            var properties = new AuthenticationProperties
            {
                // رابط العودة لاستلام الـ Tokens بعد موافقة المستخدم على الصلاحيات
                RedirectUri = Url.Action(nameof(Callback)),
                Items = { { "LoginProvider", provider } }
            };

            // توجيه المستخدم لصفحة الموافقة الخاصة بالمزود
            return Challenge(properties, provider);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback()
        {
            // اقرأ النتيجة من الحاوية المؤقتة التي حددناها
            var authenticateResult = await HttpContext.AuthenticateAsync("ExternalCookie");

            if (!authenticateResult.Succeeded)
            {
                return BadRequest("OAuth authentication failed or was canceled by the user.");
            }

            var accessToken = authenticateResult.Properties.GetTokenValue("access_token") ?? string.Empty;
            var refreshToken = authenticateResult.Properties.GetTokenValue("refresh_token") ?? string.Empty;

            // (اختياري ولكن يُفضل) مسح الكوكي المؤقت بعد أن أخذنا ما نريده منه
            await HttpContext.SignOutAsync("ExternalCookie");

            // بناء كود HTML للـ Popup ليرسل التوكنز إلى نافذة المشروع ويغلق نفسه
            var html = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Authorization Successful</title>
                <style>
                    body {{ font-family: sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; background-color: #0e1526; color: #fff; }}
                </style>
            </head>
            <body>
                <p>Authorization successful. Closing window...</p>
                <script>
                    if (window.opener) {{
                        window.opener.postMessage({{
                            type: 'oauth_success',
                            accessToken: '{accessToken}',
                            refreshToken: '{refreshToken}'
                        }}, '*'); // في بيئة الإنتاج، ضع رابط الفرونت اند بدلاً من النجمة للحد من الثغرات الأمنية
                        
                        // إغلاق النافذة المنبثقة فوراً
                        window.close();
                    }} else {{
                        document.write('Please close this tab and return to the application.');
                    }}
                </script>
            </body>
            </html>";

            return Content(html, "text/html");
        }
    }
}