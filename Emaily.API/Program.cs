using Emaily.API.Extensions;
using Emaily.API.Middlewares;
using Emaily.BLL;
using Emaily.BLL.Helpers.Interfaces;
using Hangfire;
using Hangfire.Dashboard.BasicAuthorization;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using static Emaily.DAL.Entities.BanDetail;

if (File.Exists(".env"))
{
    DotNetEnv.Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    // First policy: Allow specific origins (for frontend)
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://emaily-plus.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });

    // Second policy: Allow all origins (for 'submit' endpoint)
    options.AddPolicy("AllowAllOrigins", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
        // ملاحظة: لا يمكن استخدام AllowCredentials() مع AllowAnyOrigin() لدواعي أمنية
    });

    // Third policy: Allow localhost origin in Development
    options.AddPolicy("AllowFrontendInDevelopment", policy =>
    {
        policy.WithOrigins("http://127.0.0.1:5500")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        var httpContext = context.HttpContext;
        var banManager = httpContext.RequestServices.GetRequiredService<IBanManagerService>();
        var identifier = httpContext.GetUserIdentifier();
        var userAgent = httpContext.GetUserAgent();
        var endpoint = httpContext.GetEndpointPath();

        int weight = BanViolationWeights.SubmitSpam;
        string reason = "Rate Limit Exceeded";

        if (endpoint.Contains("/login", StringComparison.OrdinalIgnoreCase))
        {
            weight = BanViolationWeights.LoginBruteForce;
            reason = "Login Rate Limit Exceeded (Possible Brute Force)";
        }

        await banManager.RecordViolationAsync(identifier, reason, weight, userAgent, endpoint);

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/json";
        await httpContext.Response.WriteAsync("{\"error\": \"Too many requests. Please try again later.\"}", token);
    };

    options.AddPolicy("forOAuth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3, // 3 طلبات فقط لمنع التخمة أو الهجمات على الـ OAuth endpoints
                Window = TimeSpan.FromMinutes(1), 
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // لا تضعهم في طابور، ارفض فوراً
            }));
    options.AddPolicy("forSubmit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("perUser", httpContext =>
    {
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var partitionKey = !string.IsNullOrEmpty(userId) ? userId : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 2 // السماح بوضع طلبين في الطابور لو تعدى الحد لحظياً
            });
    });
});

var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? throw new Exception("JWT Secret Key is missing");

var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID") ?? throw new Exception("Google Client ID is missing");
var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET") ?? throw new Exception("Google Client Secret is missing");

var microsoftClientId = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_ID") ?? throw new Exception("Microsoft Client ID is missing");
var microsoftClientSecret = Environment.GetEnvironmentVariable("MICROSOFT_CLIENT_SECRET") ?? throw new Exception("Microsoft Client Secret is missing");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
        ValidateAudience = true,
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // لو المسار هو الخاص بالـ OAuth وفيه توكن في الرابط، استخدمه
            if (!string.IsNullOrEmpty(accessToken) &&
                context.HttpContext.Request.Path.StartsWithSegments("/api/oauth")) // عدل المسار حسب الـ API بتاعك
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            if (context.SecurityToken is JwtSecurityToken accessToken)
            {
                var purposeClaim = context.Principal?.Claims.FirstOrDefault(c => c.Type == "purpose")?.Value;

                if (string.IsNullOrEmpty(purposeClaim) || purposeClaim != "login")
                {
                    context.Fail("Unauthorized token purpose. This token is not valid for authentication.");
                }

                var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                if (cache.TryGetValue($"blacklist:{accessToken.RawData}", out _))
                {
                    context.Fail("This token has been revoked (Logged out).");
                }
            }
            return Task.CompletedTask;
        }
    };
})
.AddCookie("ExternalCookie")
.AddGoogle(options =>
{
    options.ClientId = googleClientId;
    options.ClientSecret = googleClientSecret;
    options.SaveTokens = true;

    // 1. مسح الصلاحيات الافتراضية (التي تطلب بيانات الملف الشخصي) إذا لم تكن بحاجة إليها إطلاقاً
    // options.Scope.Clear(); 

    // 2. إضافة صلاحية إرسال الإيميلات فقط عبر Gmail API
    options.Scope.Add("https://www.googleapis.com/auth/gmail.send");
    //options.Scope.Add("https://mail.google.com/");

    options.AccessType = "offline"; // هذه الخاصية الصحيحة لطلب الـ Refresh Token

    options.SignInScheme = "ExternalCookie";

    options.Events.OnRedirectToAuthorizationEndpoint = context =>
    {
        // إضافة prompt=consent لضمان إرجاع الـ Refresh Token في كل مرة (حتى لو اليوزر سجل دخول قبل كده)
        context.Response.Redirect(context.RedirectUri + "&prompt=consent");
        return Task.CompletedTask;
    };
})
.AddMicrosoftAccount(options =>
{
    options.ClientId = microsoftClientId;
    options.ClientSecret = microsoftClientSecret;
    options.SaveTokens = true;

    // 1. إضافة صلاحية إرسال الإيميلات فقط عبر Microsoft Graph API
    options.Scope.Add("Mail.Send");

    // 2. طلب صلاحية الحصول على Refresh Token
    options.Scope.Add("offline_access");

    options.SignInScheme = "ExternalCookie";
});

builder.Services.AddBusinessLogicLayer(builder.Configuration);

// API (JSON Payload)
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 2 * 1024 * 1024;
});

var app = builder.Build();

app.UseExceptionHandler();

string? hangfireLogin = builder.Configuration["HANGFIRE_LOGIN"];
string? hangfirePassword = builder.Configuration["HANGFIRE_PASSWORD"];

if (string.IsNullOrEmpty(hangfireLogin) || string.IsNullOrEmpty(hangfirePassword))
{
    throw new Exception("Hangfire login credentials are missing in configuration.");
}

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // إعداد فلتر الحماية
    Authorization =
    [
        new BasicAuthAuthorizationFilter(new BasicAuthAuthorizationFilterOptions
        {
            RequireSsl = false, // يُفضل جعلها true عند الرفع على السيرفر إذا كان لديك شهادة SSL (HTTPS)
            SslRedirect = false,
            LoginCaseSensitive = true,
            Users =
            [
                new BasicAuthAuthorizationUser
                {
                    Login = hangfireLogin,
                    PasswordClear = hangfirePassword
                }
            ]
        })
    ]
});

if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowFrontendInDevelopment");
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseCors("AllowFrontend");
}

app.UseHttpsRedirection();

app.UseMiddleware<ForbiddenMiddleware>();

app.UseAuthentication();

app.UseMiddleware<BanCheckMiddleware>();

app.UseRateLimiter();

app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.Run();