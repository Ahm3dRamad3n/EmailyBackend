using Emaily.BLL.DTOs.Submission;
using System;
using System.Net;

namespace Emaily.BLL.Helpers
{
    public static class EmailTemplateBuilder
    {
        // ==========================================
        // ثوابت الألوان (Brutalist Design Tokens)
        // ==========================================
        private const string BgVoid = "#0b1120";
        private const string GlassSurface = "#131c2f";
        private const string AuroraPurple = "#fbbf24";
        private const string GlassBorder = "#334155";
        private const string TextPrimary = "#e2e8f0";
        private const string TextSecondary = "#94a3b8";
        private const string DangerRed = "#ef4444";
        private const string SuccessGreen = "#34d399";
        private const string ShadowGlow = "#b45309";

        // ==========================================
        // 2. قالب تنبيه انتهاء الاشتراك (Subscription Downgraded)
        // ==========================================
        public static string GenerateSubscriptionDowngradedTemplate(string fullName, string planName, int overageEmails, string upgradeLink)
        {
            string template = $"""
    <div style="font-family: 'Inter', Arial, sans-serif; background-color: #f4f5f7; padding: 40px 20px; color: #0f172a;">
        <div style="max-width: 600px; margin: 0 auto; background-color: #ffffff; border: 3px solid #0f172a; border-radius: 0; box-shadow: 8px 8px 0px #0f172a;">

            <div style="background-color: #ef4444; color: #ffffff; padding: 20px; border-bottom: 3px solid #0f172a;">
                <h2 style="margin: 0; font-family: 'Sora', 'Arial Black', sans-serif; text-transform: uppercase; font-size: 20px; letter-spacing: 1px;">
                    🔒 Workspace Downgraded
                </h2>
            </div>

            <div style="padding: 30px 20px;">

                <p style="font-size: 16px; margin-top: 0;">
                    <strong>Hello {fullName},</strong>
                </p>

                <p style="font-size: 15px; line-height: 1.6;">
                    Your subscription to the
                    <strong style="color: #ef4444;">{planName}</strong>
                    plan has officially expired. Your account has been automatically downgraded to the Free tier.
                </p>

                <div style="background-color: #fff1f2; border: 2px solid #ef4444; padding: 20px; margin: 25px 0;">

                    <h3 style="margin-top: 0; color: #be123c; font-size: 16px; text-transform: uppercase;">
                        System Update Status:
                    </h3>

                    <ul style="font-size: 14px; line-height: 1.6; color: #9f1239; padding-left: 20px; margin-bottom: 0;">

                        <li>
                            <strong>Data Safe, but Locked:</strong>
                            Projects exceeding the Free limit are now in
                            <em>Read-Only Mode</em>. No data has been deleted.
                        </li>

                        <li>
                            <strong>Automations Halted:</strong>
                            Premium integrations (Google Sheets, AI, Telegram) are currently suspended.
                        </li>

                        <li>
                            <strong>Lost Overage:</strong>
                            <span style="font-family: 'JetBrains Mono', monospace; font-weight: bold; font-size: 16px;">
                                {overageEmails:N0}
                            </span>
                            unused quota emails have been forfeited.
                        </li>

                    </ul>
                </div>

                <p style="font-size: 15px; margin-top: 25px;">
                    Don't let your workflow stall. You can immediately unlock your restricted workspaces
                    and reactivate your premium integrations by upgrading your plan.
                </p>

                <div style="margin-top: 35px;">
                    <a href="{upgradeLink}"
                       style="display: inline-block; background-color: #0f172a; color: #ffffff; text-decoration: none; padding: 12px 24px; border: 2px solid #0f172a; box-shadow: 4px 4px 0px #ef4444; font-family: 'Sora', Arial, sans-serif; font-weight: bold; text-transform: uppercase; font-size: 14px;">
                        Unlock Workspaces Now &rarr;
                    </a>
                </div>

            </div>
        </div>

        <div style="max-width: 600px; margin: 25px auto 0; color: #64748b; font-size: 11px; text-align: center; font-family: 'JetBrains Mono', monospace; line-height: 1.5;">
            CONFIDENTIALITY NOTICE: This system-generated email is for authorized users only.
            <br>
            © {DateTime.UtcNow.Year} Emaily Industrial Blueprint. All systems operational.
        </div>
    </div>
    """;

            return template;
        }

        // ==========================================
        // 1. قالب تنبيه انتهاء الاشتراك (Subscription Expiration Alert)
        // ==========================================
        public static string GenerateSubscriptionExpiringTemplate(string fullName, string planName, DateTime endDate, string renewalLink)
        {
            string template = $"""
    <div style="font-family: 'Inter', Arial, sans-serif; background-color: #f4f5f7; padding: 40px 20px; color: #0f172a;">
        <div style="max-width: 600px; margin: 0 auto; background-color: #ffffff; border: 3px solid #0f172a; border-radius: 0; box-shadow: 8px 8px 0px #0f172a;">

            <div style="background-color: #f59e0b; color: #0f172a; padding: 20px; border-bottom: 3px solid #0f172a;">
                <h2 style="margin: 0; font-family: 'Sora', 'Arial Black', sans-serif; text-transform: uppercase; font-size: 20px; letter-spacing: 1px;">
                    ⏳ Action Required: Renewal Pending
                </h2>
            </div>

            <div style="padding: 30px 20px;">

                <p style="font-size: 16px; margin-top: 0;">
                    <strong>Hello {fullName},</strong>
                </p>

                <p style="font-size: 15px; line-height: 1.6;">
                    This is a system alert regarding your Emaily workspace.
                    Your subscription to the
                    <strong style="background-color: #0f172a; color: #34d399; padding: 2px 6px;">
                        {planName}
                    </strong>
                    plan is scheduled to expire in exactly 3 days.
                </p>

                <div style="background-color: #f8fafc; border: 2px dashed #0f172a; padding: 15px; margin: 25px 0; text-align: center;">

                    <span style="color: #64748b; font-size: 12px; font-weight: bold; letter-spacing: 1px; text-transform: uppercase;">
                        Target Expiration Date (UTC)
                    </span>

                    <br>

                    <strong style="color: #0f172a; font-size: 22px; font-family: 'JetBrains Mono', Consolas, monospace;">
                        {endDate:yyyy-MM-dd HH:mm}
                    </strong>

                </div>

                <p style="font-size: 15px; font-weight: bold; margin-bottom: 10px;">
                    If your plan expires, the following limits will be enforced:
                </p>

                <ul style="font-size: 14px; line-height: 1.6; color: #334155; padding-left: 20px;">

                    <li>
                        <strong>Workspace Lock:</strong>
                        Projects exceeding the Free tier limit will be placed in
                        <em>Read-Only Mode</em>.
                    </li>

                    <li>
                        <strong>Integration Suspension:</strong>
                        Premium automations (Google Sheets, AI Summaries, Telegram Bots) will be paused.
                    </li>

                </ul>

                <p style="font-size: 15px; margin-top: 25px;">
                    Please renew your active plan to maintain full operational capacity
                    and avoid automated service interruption.
                </p>

                <div style="margin-top: 35px;">
                    <a href="{renewalLink}"
                       style="display: inline-block; background-color: #0f172a; color: #ffffff; text-decoration: none; padding: 12px 24px; border: 2px solid #0f172a; box-shadow: 4px 4px 0px #34d399; font-family: 'Sora', Arial, sans-serif; font-weight: bold; text-transform: uppercase; font-size: 14px;">
                        Renew Subscription &rarr;
                    </a>
                </div>

            </div>
        </div>

        <div style="max-width: 600px; margin: 25px auto 0; color: #64748b; font-size: 11px; text-align: center; font-family: 'JetBrains Mono', monospace; line-height: 1.5;">
            CONFIDENTIALITY NOTICE: This system-generated email is for authorized users only.
            <br>
            © {DateTime.UtcNow.Year} Emaily Industrial Blueprint. All systems operational.
        </div>
    </div>
    """;

            return template;
        }

        // ==========================================
        // 2. قالب استعادة كلمة المرور (Security Notice)
        // ==========================================
        public static string GeneratePasswordResetTemplate(string fullName, string resetLink)
        {
            string template = $"""
    <div style="background-color: {BgVoid}; padding: 50px 20px; font-family: 'Inter', Arial, sans-serif; text-align: center;">
        <div style="max-width: 550px; margin: 0 auto; background-color: {GlassSurface}; border: 2px solid {GlassBorder}; box-shadow: 6px 6px 0px #000000; text-align: left;">

            <div style="padding: 25px 30px; border-bottom: 2px solid {DangerRed}; background-color: #0e1526;">
                <h2 style="margin: 0; color: #ffffff; font-family: 'Sora', sans-serif; font-size: 20px; text-transform: uppercase; letter-spacing: 2px;">
                    Password Reset
                </h2>
            </div>

            <div style="padding: 30px; color: {TextPrimary}; font-size: 15px; line-height: 1.6;">

                <p style="margin-top: 0;">
                    <strong>Hello {fullName},</strong>
                </p>

                <p>
                    A password reset request was initiated for your workspace account.
                </p>

                <div style="background-color: #0e1526; border: 1px dashed #475569; padding: 15px; margin: 20px 0; font-family: 'JetBrains Mono', monospace;">
                    <span style="color: #94a3b8;">
                        SECURITY PROTOCOL:
                    </span>
                    <br>
                    <strong style="color: #34d399; font-size: 14px;">
                        TOKEN EXPIRES IN 15 MINUTES
                    </strong>
                </div>

                <p>
                    If you did not request this reset, please ignore this communication.
                    Your operational security remains uncompromised.
                </p>

                <div style="margin-top: 30px; text-align: center;">
                    <a href="{resetLink}"
                       style="display: inline-block; background-color: {DangerRed}; color: #ffffff; padding: 14px 30px; text-decoration: none; font-weight: 800; font-size: 14px; text-transform: uppercase; border: 2px solid {DangerRed}; box-shadow: 4px 4px 0px #000000; font-family: 'JetBrains Mono', monospace; letter-spacing: 1px;">
                        RESET PASSWORD
                    </a>
                </div>

            </div>
        </div>

        <div style="max-width: 550px; margin: 20px auto 0; color: {TextSecondary}; font-size: 11px; text-align: center; font-family: 'JetBrains Mono', monospace; line-height: 1.5;">
            EMaily SECURITY NOTIFICATION
            <br>
            © {DateTime.UtcNow.Year} Blueprint Architecture.
        </div>
    </div>
    """;

            return template;
        }

        // ==========================================
        // 3. قالب التحقق من الإيميل (Identity Verification)
        // ==========================================
        public static string GenerateEmailVerificationTemplate(string email, string verificationLink)
        {
            string template = $"""
    <div style="background-color: {BgVoid}; padding: 50px 20px; font-family: 'Inter', Arial, sans-serif; text-align: center;">
        <div style="max-width: 550px; margin: 0 auto; background-color: {GlassSurface}; border: 2px solid {GlassBorder}; box-shadow: 6px 6px 0px #000000; text-align: left;">

            <div style="padding: 25px 30px; border-bottom: 2px solid {AuroraPurple}; background-color: #0e1526;">
                <h2 style="margin: 0; color: #ffffff; font-family: 'Sora', sans-serif; font-size: 20px; text-transform: uppercase; letter-spacing: 2px;">
                    Identity Verification
                </h2>
            </div>

            <div style="padding: 30px; color: {TextPrimary}; font-size: 15px; line-height: 1.6;">

                <p style="margin-top: 0;">
                    To continue your process for <strong>{email}</strong>,
                    please verify your email address by clicking the button below.
                </p>

                <div style="background-color: #000000; border: 2px dashed {AuroraPurple}; padding: 30px; margin: 30px 0; text-align: center; box-shadow: inset 0 0 10px rgba(0,0,0,0.5);">

                    <span style="color: {TextSecondary}; font-family: 'Inter', sans-serif; font-size: 11px; text-transform: uppercase; letter-spacing: 2px; display: block; margin-bottom: 20px;">
                        Email Verification
                    </span>

                    <a href="{verificationLink}"
                       style="display: inline-block; background-color: {AuroraPurple}; color: #000000; text-decoration: none; padding: 14px 30px; font-family: 'Inter', Arial, sans-serif; font-size: 14px; font-weight: 700; text-transform: uppercase; letter-spacing: 1px; border: 2px solid #000000; box-shadow: 4px 4px 0px #000000;">
                        Verify Email
                    </a>

                </div>

                <div style="background-color: #0e1526; border: 1px dashed #475569; padding: 15px 20px; margin: 25px 0; font-family: 'JetBrains Mono', monospace;">
                    <span style="color: {TextSecondary}; font-size: 12px;">
                        VERIFICATION LINK VALIDITY:
                    </span>
                    <br>
                    <strong style="color: {AuroraPurple}; font-size: 14px; letter-spacing: 1px;">
                        VALID FOR 24 HOURS ONLY
                    </strong>
                </div>

                <p style="font-size: 13px; color: {TextSecondary};">
                    If you did not create this account, you can safely ignore this email.
                </p>

                <p style="font-size: 13px; color: {TextSecondary};">
                    For security reasons,
                    <strong style="color: {DangerRed};">
                        never share your verification link with anyone.
                    </strong>
                </p>

            </div>
        </div>

        <div style="max-width: 550px; margin: 20px auto 0; color: {TextSecondary}; font-size: 11px; text-align: center; font-family: 'JetBrains Mono', monospace;">
            EMAILY SYSTEM NOTIFICATION
            <br />
            © {DateTime.UtcNow.Year} Blueprint Architecture.
        </div>
    </div>
    """;

            return template;
        }

        // ==========================================
        // 4. قالب تنبيهات الأخطاء الحرجة (Critical Error Incident)
        // ==========================================
        public static string GenerateCriticalErrorTemplate(string message, string? exceptionDetails, string userId, string? projectId)
        {
            static string FormatText(string input, int maxLength)
            {
                if (string.IsNullOrWhiteSpace(input)) return "N/A";
                string truncated = input.Length <= maxLength ? input : input[..maxLength] + "\n\n... [TRUNCATED]";
                return WebUtility.HtmlEncode(truncated);
            }

            string safeMessage = FormatText(message, 500);
            string safeException = string.IsNullOrEmpty(exceptionDetails) ? "No exception details available." : FormatText(exceptionDetails, 2000);
            userId = string.IsNullOrWhiteSpace(userId) ? "System Error" : userId;
            projectId = string.IsNullOrWhiteSpace(projectId) ? "N/A" : projectId;

            string template = $"""
        <div style="background-color: #f8fafc; padding: 40px 20px; font-family: 'Inter', Arial, sans-serif;">
            <div style="max-width: 700px; margin: 0 auto; background-color: #ffffff; border: 3px solid #0f172a; box-shadow: 8px 8px 0px #0f172a;">
                
                <div style="background-color: {DangerRed}; color: #ffffff; padding: 25px; border-bottom: 3px solid #0f172a;">
                    <h2 style="margin: 0; font-family: 'Sora', sans-serif; text-transform: uppercase; font-size: 22px; letter-spacing: 1px;">
                        ⚠️ CRITICAL INCIDENT REPORT
                    </h2>
                </div>
                
                <div style="padding: 30px; color: #0f172a;">
                    <p style="font-size: 16px; font-weight: bold; margin-top: 0; margin-bottom: 25px; color: {DangerRed};">
                        An unhandled exception has occurred in the production environment. Immediate engineering attention is required.
                    </p>
                    
                    <table style="width: 100%; border-collapse: collapse; margin-bottom: 30px;">
                        <tr>
                            <td style="width: 25%; padding: 12px; border: 2px solid #0f172a; font-weight: bold; background-color: #f1f5f9;">Timestamp (UTC)</td>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-family: 'JetBrains Mono', monospace; font-size: 14px;">{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}</td>
                        </tr>
                        <tr>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-weight: bold; background-color: #f1f5f9;">User ID</td>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-family: 'JetBrains Mono', monospace; font-size: 14px;">{userId}</td>
                        </tr>
                        <tr>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-weight: bold; background-color: #f1f5f9;">Project ID</td>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-family: 'JetBrains Mono', monospace; font-size: 14px;">{projectId}</td>
                        </tr>
                        <tr>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-weight: bold; background-color: #f1f5f9;">Message</td>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-size: 14px; font-weight: 600;">{safeMessage}</td>
                        </tr>
                    </table>
                    
                    <h3 style="margin: 0 0 10px 0; font-family: 'Sora', sans-serif; font-size: 15px; text-transform: uppercase;">Stack Trace / Details:</h3>
                    <div style="background-color: #0f172a; color: {SuccessGreen}; padding: 20px; border: 2px solid #0f172a; font-family: 'JetBrains Mono', monospace; font-size: 13px; line-height: 1.6; white-space: pre-wrap; word-wrap: break-word; overflow-x: auto; box-shadow: inset 0 0 15px rgba(0,0,0,0.8);">
                        {safeException}
                    </div>
                </div>
            </div>
        </div>
        """;
            return template;
        }

        // ==========================================
        // 5. قالب تنبيه الحظر الأمني (Security Ban Alert)
        // ==========================================
        public static string GenerateBanLevelTemplate(string identifier, int level, string fullReason)
        {
            string template = $"""
        <div style="background-color: #f8fafc; padding: 40px 20px; font-family: 'Inter', Arial, sans-serif;">
            <div style="max-width: 600px; margin: 0 auto; background-color: #ffffff; border: 3px solid #0f172a; box-shadow: 8px 8px 0px #0f172a;">
                
                <div style="background-color: #000000; color: {AuroraPurple}; padding: 25px; border-bottom: 3px solid #0f172a;">
                    <h2 style="margin: 0; font-family: 'Sora', sans-serif; text-transform: uppercase; font-size: 22px; letter-spacing: 1px;">
                        🛡️ SECURITY BAN TRIGGERED
                    </h2>
                </div>

                <div style="padding: 30px; color: #0f172a;">
                    <p style="font-size: 16px; font-weight: bold; margin-top: 0; color: #000000;">
                        System abuse threshold exceeded. An automated defense mechanism has been activated.
                    </p>
                    
                    <table style="width: 100%; border-collapse: collapse; margin-top: 25px;">
                        <tr>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-weight: bold; background-color: #f1f5f9; width: 30%;">Target IP</td>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-family: 'JetBrains Mono', monospace; font-weight: bold;">{identifier}</td>
                        </tr>
                        <tr>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-weight: bold; background-color: #f1f5f9;">Ban Level</td>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-family: 'JetBrains Mono', monospace; color: {DangerRed}; font-weight: 900; font-size: 16px;">LEVEL {level}</td>
                        </tr>
                        <tr>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-weight: bold; background-color: #f1f5f9;">Violation Log</td>
                            <td style="padding: 12px; border: 2px solid #0f172a; font-family: 'Inter', sans-serif; font-size: 14px;">{WebUtility.HtmlEncode(fullReason)}</td>
                        </tr>
                    </table>
                </div>
            </div>
        </div>
        """;
            return template;
        }

        public static string GenerateComplaintEmailBody(ComplaintDto complaint, string name, string email, string originDomain)
        {
            return $"""
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>New Complaint</title>
    </head>

    <body style="
        margin:0;
        padding:40px 20px;
        background:#0b1120;
        color:#e2e8f0;
        font-family:Inter,Arial,sans-serif;
    ">
        <div style="
            max-width:720px;
            margin:0 auto;
            background:#131c2f;
            border:1px solid #334155;
            border-radius:16px;
            overflow:hidden;
        ">

            <div style="
                padding:28px 32px;
                border-bottom:1px solid #334155;
            ">
                <div style="
                    font-size:12px;
                    font-weight:700;
                    letter-spacing:2px;
                    color:#fbbf24;
                    margin-bottom:10px;
                ">
                    EMAILY SUPPORT
                </div>

                <h1 style="
                    margin:0;
                    font-size:26px;
                    color:#e2e8f0;
                ">
                    🚨 New Complaint
                </h1>

                <p style="
                    margin:10px 0 0;
                    color:#94a3b8;
                    font-size:14px;
                ">
                    A new complaint has been submitted through the support system.
                </p>
            </div>

            <div style="padding:32px;">

                <div style="
                    background:#0b1120;
                    border:1px solid #334155;
                    border-radius:12px;
                    padding:20px;
                    margin-bottom:20px;
                ">
                    <div style="
                        color:#94a3b8;
                        font-size:11px;
                        font-weight:700;
                        letter-spacing:1px;
                        margin-bottom:16px;
                    ">
                        COMPLAINT INFORMATION
                    </div>

                    <table style="width:100%;border-collapse:collapse;">
                        <tr>
                            <td style="padding:8px 0;color:#94a3b8;width:140px;">Name</td>
                            <td style="padding:8px 0;color:#e2e8f0;">
                                {System.Net.WebUtility.HtmlEncode(name)}
                            </td>
                        </tr>

                        <tr>
                            <td style="padding:8px 0;color:#94a3b8;">Email</td>
                            <td style="padding:8px 0;color:#e2e8f0;">
                                {System.Net.WebUtility.HtmlEncode(email)}
                            </td>
                        </tr>

                        <tr>
                            <td style="padding:8px 0;color:#94a3b8;">Origin Domain</td>
                            <td style="padding:8px 0;color:#e2e8f0;">
                                {System.Net.WebUtility.HtmlEncode(originDomain)}
                            </td>
                        </tr>

                        <tr>
                            <td style="padding:8px 0;color:#94a3b8;">Project ID</td>
                            <td style="
                                padding:8px 0;
                                color:#e2e8f0;
                                font-family:'JetBrains Mono',monospace;
                                font-size:13px;
                            ">
                                {System.Net.WebUtility.HtmlEncode(complaint.ProjectID ?? "-")}
                            </td>
                        </tr>

                        <tr>
                            <td style="padding:8px 0;color:#94a3b8;">Category</td>
                            <td style="padding:8px 0;color:#e2e8f0;">
                                {System.Net.WebUtility.HtmlEncode(complaint.Category)}
                            </td>
                        </tr>
                    </table>
                </div>

                <div style="
                    background:#1c1515;
                    border:1px solid #7f1d1d;
                    border-radius:12px;
                    padding:20px;
                ">
                    <div style="
                        color:#ef4444;
                        font-size:11px;
                        font-weight:700;
                        letter-spacing:1px;
                        margin-bottom:12px;
                    ">
                        COMPLAINT DETAILS
                    </div>

                    <div style="
                        color:#e2e8f0;
                        font-size:14px;
                        line-height:1.8;
                        white-space:pre-wrap;
                        overflow-wrap:anywhere;
                        word-break:break-word;
                    ">
                        {System.Net.WebUtility.HtmlEncode(complaint.Details)}
                    </div>
                </div>

            </div>

            <div style="
                padding:18px 32px;
                border-top:1px solid #334155;
                color:#64748b;
                font-size:11px;
                text-align:center;
                letter-spacing:1px;
            ">
                EMAILY SUPPORT NOTIFICATION
            </div>

        </div>
    </body>
    </html>
    """;
        }

        public static string GenerateSuggestionEmailBody(SuggestionDto suggestion, string name, string email, string originDomain)
        {
            return $"""
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <title>New Suggestion</title>
    </head>

    <body style="
        margin:0;
        padding:40px 20px;
        background:#0b1120;
        color:#e2e8f0;
        font-family:Inter,Arial,sans-serif;
    ">
        <div style="
            max-width:720px;
            margin:0 auto;
            background:#131c2f;
            border:1px solid #334155;
            border-radius:16px;
            overflow:hidden;
        ">

            <div style="
                padding:28px 32px;
                border-bottom:1px solid #334155;
            ">
                <div style="
                    font-size:12px;
                    font-weight:700;
                    letter-spacing:2px;
                    color:#fbbf24;
                    margin-bottom:10px;
                ">
                    EMAILY SUPPORT
                </div>

                <h1 style="
                    margin:0;
                    font-size:26px;
                    color:#e2e8f0;
                ">
                    💡 New Suggestion
                </h1>

                <p style="
                    margin:10px 0 0;
                    color:#94a3b8;
                    font-size:14px;
                ">
                    A new suggestion has been submitted through the support system.
                </p>
            </div>

            <div style="padding:32px;">

                <div style="
                    background:#0b1120;
                    border:1px solid #334155;
                    border-radius:12px;
                    padding:20px;
                    margin-bottom:20px;
                ">
                    <div style="
                        color:#94a3b8;
                        font-size:11px;
                        font-weight:700;
                        letter-spacing:1px;
                        margin-bottom:16px;
                    ">
                        SUGGESTION INFORMATION
                    </div>

                    <table style="width:100%;border-collapse:collapse;">
                        <tr>
                            <td style="padding:8px 0;color:#94a3b8;width:140px;">Name</td>
                            <td style="padding:8px 0;color:#e2e8f0;">
                                {System.Net.WebUtility.HtmlEncode(name)}
                            </td>
                        </tr>

                        <tr>
                            <td style="padding:8px 0;color:#94a3b8;">Email</td>
                            <td style="padding:8px 0;color:#e2e8f0;">
                                {System.Net.WebUtility.HtmlEncode(email)}
                            </td>
                        </tr>

                        <tr>
                            <td style="padding:8px 0;color:#94a3b8;">Origin Domain</td>
                            <td style="padding:8px 0;color:#e2e8f0;">
                                {System.Net.WebUtility.HtmlEncode(originDomain)}
                            </td>
                        </tr>

                        <tr>
                            <td style="padding:8px 0;color:#94a3b8;">Type</td>
                            <td style="padding:8px 0;color:#e2e8f0;">
                                {System.Net.WebUtility.HtmlEncode(suggestion.Type)}
                            </td>
                        </tr>

                        <tr>
                            <td style="padding:8px 0;color:#94a3b8;">Expected Impact</td>
                            <td style="padding:8px 0;color:#e2e8f0;">
                                {System.Net.WebUtility.HtmlEncode(suggestion.ExpectedImpact ?? "-")}
                            </td>
                        </tr>
                    </table>
                </div>

                <div style="
                    background:#15152a;
                    border:1px solid #4c1d95;
                    border-radius:12px;
                    padding:20px;
                ">
                    <div style="
                        color:#a78bfa;
                        font-size:11px;
                        font-weight:700;
                        letter-spacing:1px;
                        margin-bottom:12px;
                    ">
                        SUGGESTION DETAILS
                    </div>

                    <div style="
                        color:#e2e8f0;
                        font-size:14px;
                        line-height:1.8;
                        white-space:pre-wrap;
                        overflow-wrap:anywhere;
                        word-break:break-word;
                    ">
                        {System.Net.WebUtility.HtmlEncode(suggestion.Details)}
                    </div>
                </div>

            </div>

            <div style="
                padding:18px 32px;
                border-top:1px solid #334155;
                color:#64748b;
                font-size:11px;
                text-align:center;
                letter-spacing:1px;
            ">
                EMAILY SUPPORT NOTIFICATION
            </div>

        </div>
    </body>
    </html>
    """;
        }
    }

}

