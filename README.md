<div align="center">

# Emaily — Backend API

**The ASP.NET Core API behind Emaily** — a form-to-email routing platform that
renders and delivers submissions through the sending account you connect.

[**Live demo →**](https://emaily-plus.vercel.app/) · [**Frontend repository →**](https://github.com/Ahm3dRamad3n/Emaily)

</div>

---

## Overview

Emaily is a form-to-email routing backend. Users configure projects, templates,
and email-sending services, then submit form data to a public API that renders a
message and queues its delivery. Beyond routing, the API provides account and
subscription management, submission history, analytics, abuse controls, and
optional integrations for Telegram, Google Sheets, and AI summaries.

The [frontend](https://github.com/Ahm3dRamad3n/Emaily) in the companion
repository is the client for this API.

## Tech stack

| Area | Technology |
|---|---|
| **Runtime** | .NET 8 (`net8.0`) across all three projects; ASP.NET Core Web SDK |
| **Data access** | Entity Framework Core 8 with SQL Server |
| **Background jobs** | Hangfire 1.8 (SQL Server storage) plus hosted background services |
| **Authentication** | JWT Bearer (`System.IdentityModel.Tokens.Jwt`), plus Google and Microsoft account OAuth handlers |
| **Password hashing** | BCrypt (`BCrypt.Net-Next`) |
| **Email sending** | MailKit (SMTP) |
| **HTML safety** | HtmlSanitizer |
| **Storage / documents** | Azure Blob Storage (attachments and invoice PDFs), QuestPDF (invoice PDF generation) |
| **Integrations** | Google Sheets API v4 |
| **API docs** | Swashbuckle / Swagger — served in Development only |
| **Local config** | DotNetEnv loads a local `.env` file when present |

## Solution structure

A three-layer solution (`Emaily.sln`) with a generic repository and unit-of-work
pattern in the data layer. An API-only solution file, `Emaily.API.sln`, also
exists.

```
Emaily.sln
├── Emaily.API/            ASP.NET Core host: controllers, startup, middleware
│   ├── Controllers/         API route handlers
│   ├── Authorization/       Owner requirement and handler classes
│   ├── Extensions/          Claims and HTTP-context helpers
│   ├── Middlewares/         Ban checks and recording of forbidden requests
│   └── Properties/          Launch profiles and Azure Web Deploy template
├── Emaily.BLL/            Business logic and API-facing DTOs
│   ├── Attributes/          Custom validation attributes
│   ├── DTOs/                Request/response objects, grouped by area
│   ├── Helpers/             Email templates, exception handling, HTML validation,
│   │   ├── Interfaces/      hosted-service helpers
│   │   └── Services/        Email, encryption, logging, attachments, tokens, plans, bans
│   ├── Interfaces/          Business-service interfaces
│   └── Services/            Auth, projects, templates, billing, submissions,
│                            integrations, analytics, admin, users
└── Emaily.DAL/            EF Core data layer
    ├── Entities/            Entity classes and EmailyDbContext
    ├── Entities Constants/  Partial classes with role, provider, mode, status constants
    ├── Interfaces/          Generic repository and unit-of-work interfaces
    └── Repositories/        Generic repository and unit-of-work implementations
```

There is no separate `Jobs` or `Workers` folder; the hosted workers live in the
BLL helpers.

## Getting started

**Prerequisites:** the .NET 8 SDK, and a SQL Server instance with a compatible
schema already provisioned (see [Database setup](#database-setup)).

1. Clone the repository.
2. Provide configuration. DotNetEnv loads a local `.env` file when one is
   present; the required variable names are listed under
   [Configuration](#configuration). Never commit real values.
3. Run the API:
   ```bash
   dotnet run --project Emaily.API
   ```
4. Point the [frontend](https://github.com/Ahm3dRamad3n/Emaily) at it by setting
   `BASE_URL` in its `js/env.js` to this API's `/api` address (the frontend
   defaults to `https://localhost:7104/api`).

Swagger UI is available when the API runs in the Development environment.

## Data models

The EF model is declared in `EmailyDbContext.cs`. These are the entity properties and relationships represented by the C# model; some scalar properties are not configured as EF relationships.

- **User** — `int ClusterKey`, `Guid Id`, `string FullName`, `string Email`, `string PasswordHash`, `string DevNotificationEmail`, `DateTime CreatedAt`, `int RemainingQuota`, `int OverageEmails`, `string Roles`, `bool IsActive`, `bool IsDeleted`. One-to-many: projects, refresh tokens, services, subscriptions.
- **Plan** — `Guid Id`, `string Name`, `decimal MonthlyPrice`, `int MaxEmailsPerMonth`, `int MaxProjects`, `int MaxServices`, `int MaxTemplates`, `int MaxAttachmentsPerTemplate`, `bool CanUseGoogleSheets`, `bool CanUseAi`, `bool CanUseTelegramBot`, `bool IsActive`. One-to-many: subscriptions.
- **Subscription** — `int ClusterKey`, `Guid Id`, `Guid UserId`, `Guid PlanId`, `string Status`, `DateTime StartDate`, `DateTime EndDate`, `DateTime? RevokeAt`, `DateTime CreatedAt`. Many-to-one: user and plan. One-to-many: invoices. Status constants: `Active`, `Revoke`, `Expired`.
- **Invoice** — `int ClusterKey`, `Guid Id`, `Guid SubscriptionId`, `decimal Amount`, `string Status`, `DateTime InvoiceDate`, `string InvoicePdfUrl`. Many-to-one: subscription. Status constants: `Pending`, `Paid`, `Failed`, `Refunded`, `Void`.
- **Project** — `int ClusterKey`, `string Id`, `Guid UserId`, `string Name`, `string PublicApiKey`, `string PrivateApiKey`, `string? RestrictedDomains`, `string ProjectAccessMode`, `DateTime CreatedAt`, `bool IsActive`, `bool IsDeleted`, `bool IsLocked`. Many-to-one: user. One-to-many: integrations, project-service links, submissions, templates. Access-mode constants: `FrontendOnly`, `BackendOnly`, `Hybrid`.
- **Service** — `int ClusterKey`, `string Id`, `Guid UserId`, `string ProviderType`, `string FromEmail`, `string FromName`, `DateTime CreatedAt`, `bool IsActive`, `bool IsDeleted`, `bool IsLocked`. Many-to-one: user. One-to-many: templates and project-service links. Optional one-to-one credential records: API key, app password, OAuth. Provider types: `AppPassword`, `ApiKey`, `OAuth`.
- **ProjectService** — `int ClusterKey`, `string ProjectId`, `string ServiceId`, `DateTime AssignedAt`. Join entity for the many-to-many project/service relationship; composite key is project plus service.
- **ServiceApiKey** — `int ClusterKey`, `string ServiceId`, `string ProviderName`, `string SecretApiKey`. One-to-one with service. Provider names: `SendGrid`, `Resend`.
- **ServiceAppPassword** — `int ClusterKey`, `string ServiceId`, `string SmtpHost`, `int SmtpPort`, `string Username`, `string EncryptedPassword`. One-to-one with service.
- **ServiceOauth** — `int ClusterKey`, `string ServiceId`, `string OauthProvider`, `string AccessToken`, `string RefreshToken`, `DateTime TokenExpiry`. One-to-one with service. Provider constants: `Google`, `Microsoft`.
- **Template** — `int ClusterKey`, `string Id`, `string ProjectId`, `string? ServiceId`, `string Name`, `string Subject`, `string ContentHtml`, `bool DoSaveInHistory`, `bool EnableRecaptchaV2`, `string? RecaptchaSecretKey`, `bool EnableAppCheck`, `string? AppCheckSecret`, `string? Issuer`, `string ToEmail`, `string? ToName`, `string? ReplyTo`, `string? Bcc`, `string? Cc`, `bool EnableAutoReply`, `string? AutoReplyTemplateId`, `DateTime CreatedAt`, `bool IsActive`, `bool IsDeleted`, `bool IsLocked`. Many-to-one: project and optional service. Optional self-reference to auto-reply template. One-to-many: submissions and attachments.
- **TemplateAttachment** — `int ClusterKey`, `Guid Id`, `string TemplateId`, `string FileName`, `string FileUrl`, `long FileSizeInBytes`. Many-to-one: template.
- **Integration** — `int ClusterKey`, `Guid Id`, `string ProjectId`, `string IntegrationType`, `string ConfigJson`, `bool IsActive`, `bool IsDeleted`. Many-to-one: project. Types: `TelegramBot`, `GoogleSheets`, `AiSummary`.
- **Submission** — `int ClusterKey`, `Guid Id`, `string ProjectId`, `string TemplateId`, `string RecipientEmail`, `string? RecipientName`, `string Subject`, `string PayloadJson`, `string RawHtmlBody`, `DateTime ReceivedAt`, `string Status`, `bool IsPrivateData`, `string? AiSummary`, `string? ErrorMessage`, `DateTime? SentAt`. Many-to-one: project and template. The database mapping configures foreign keys to both.
- **RefreshToken** — `int ClusterKey`, `Guid Id`, `Guid UserId`, `string Token`, `string? JwtId`, `bool IsUsed`, `bool IsRevoked`, `DateTime CreatedAt`, `DateTime ExpiresAt`. Many-to-one: user.
- **SystemLog** — `long Id`, `string LogLevel`, `string ExecutionTrace`, `string Message`, `string? ExceptionDetails`, `Guid? UserId`, `string? ProjectId`, `string? IpAddress`, `DateTime CreatedAt`. `UserId` and `ProjectId` are scalar fields; no entity navigation/FK is configured.
- **Banned** — `int ClusterKey`, `string IpAddress`, `int BanLevel`, `DateTime BannedUntil`. The identifier can represent an IP address or authenticated user email. One-to-many: ban details, joined via `IpAddress`.
- **BanDetail** — `int ClusterKey`, `string IpAddress`, `string ActionType`, `int ViolationWeight`, `string Reason`, `string UserAgent`, `string Endpoint`, `Guid? AdminId`, `DateTime CreatedAt`. Many-to-one: banned record. `AdminId` is scalar; no admin-user relationship is configured.

## Authentication and authorization

- **Access tokens** are JWTs signed with HMAC-SHA256 and expire after
  **10 minutes**. Issuer and audience validation are enabled, with **zero clock
  skew**.
- **Refresh tokens** are random 32-byte values (Base64) that expire after
  **7 days** and **rotate on use**: refreshing marks the old token used and
  issues a new access/refresh pair. Missing, revoked, used, or expired refresh
  tokens are rejected.
- **Purpose-scoped tokens.** Access tokens carry the claims name identifier,
  email, name, role, JWT ID (`jti`), and a custom `purpose=login` claim. The
  bearer validation hook rejects any token without `purpose=login`, so
  password-reset (`password-reset`, 15 minutes) and email-verification
  (`email_verification`, 24 hours) tokens can never be used as sessions.
- **Logout** marks the supplied refresh token revoked and places the current
  access token in an **in-memory blacklist** until it expires. The blacklist is
  not persisted or shared between instances. **Account deletion** revokes the
  user's stored refresh tokens.
- **Passwords** are hashed with BCrypt.
- **OAuth.** Google and Microsoft handlers are configured. The explicitly added
  scopes are Google `gmail.send` and Microsoft `Mail.Send` plus `offline_access`.
  The code does not clear the providers' default scopes, so these additions
  should not be read as an exhaustive list of the effective scopes. The callback
  reads the access/refresh tokens from the temporary external cookie and posts
  them to the opener window; service creation then accepts and stores them.
- **Roles** are `User` and `Admin`. Admin routes use
  `[Authorize(Roles = "Admin")]`; most other private routes require
  `[Authorize]` without a role restriction. The auth controller is anonymous by
  default, with logout and account deletion individually requiring
  authentication.

## API reference

The tables below list every endpoint in the API. For worked request/response examples of the key endpoints, see the [API reference page](https://emaily-plus.vercel.app/app/api-reference.html) on the live site.

Route groups below use the controller's route prefix. 'User' means authenticated; 'Admin' means the `Admin` role; public endpoints are anonymous. Request/response details are included where directly visible in DTOs or controller code.

**Authentication — `AuthController.cs`**  
Controller default: anonymous; all routes use the `forOAuth` rate limit.

| Method | Route | Access | Request / response |
|---|---|---|---|
| POST | `/api/auth/send-verification-email` | Anonymous | Body `email`; generic 'if email exists' message. |
| POST | `/api/auth/register` | Anonymous | `fullName`, `token`, `password`; returns access/refresh tokens, expiry seconds, user ID, name, roles. |
| POST | `/api/auth/login` | Anonymous | `email`, `password`; same auth response shape. |
| POST | `/api/auth/refresh-token` | Anonymous | `token` (refresh token); returns new auth response. |
| POST | `/api/auth/logout` | User | `refreshToken`; access token is read from the authenticated request. |
| POST | `/api/auth/forgot-password` | Anonymous | `email`; generic response. |
| POST | `/api/auth/reset-password` | Anonymous | `token`, `newPassword`; success/failure message. |
| DELETE | `/api/auth/delete-account` | User | No body; soft-deletes account and revokes refresh tokens. |

**External OAuth — `OAuthController.cs`**  
Both routes are anonymous and use `forOAuth`.

| Method | Route | Access | Request / response |
|---|---|---|---|
| GET | `/api/oauth/authorize?provider=Google` or `Microsoft` | Anonymous | Redirects to the provider challenge; other provider values return 400. |
| GET | `/api/oauth/callback` | Anonymous | Returns HTML that sends `accessToken` and `refreshToken` to the opener via `postMessage`. |

**User — `UserController.cs`**  
All routes require a user token and use `perUser`.

| Method | Route | Access | Request / response |
|---|---|---|---|
| GET | `/api/user/me` | User | Profile DTO. |
| PUT | `/api/user/me` | User | `UpdateProfileDto`; updated profile. |
| DELETE | `/api/user/me` | User | No body; deletes account and revokes tokens. |
| PUT | `/api/user/password` | User | `ChangePasswordDto`; success message. |
| GET | `/api/user/quota` | User | Quota DTO. |

**Projects — `ProjectsController.cs`**  
All routes require a user token and use `perUser`.

| Method | Route | Access | Request / response |
|---|---|---|---|
| GET | `/api/projects` | User | User's project list. |
| POST | `/api/projects` | User | `name`, optional `restrictedDomains`; project details including API keys. |
| GET | `/api/projects/{id}` | User | Project details. |
| PUT | `/api/projects/{id}` | User | Project update DTO (`name`, `restrictedDomains`). |
| POST | `/api/projects/{id}/keys` | User | No body; regenerates keys. |
| DELETE | `/api/projects/{id}` | User | No body; soft-deletes project and related items. |
| PUT | `/api/projects/{id}/status` | User | `{ isActive }`. |
| PUT | `/api/projects/{id}/access-mode` | User | `{ accessMode }`. |
| PUT | `/api/projects/{id}/unlock` | User | No body; attempts to unlock project. |

**Services — `ServicesController.cs`**

| Method | Route | Access | Request / response |
|---|---|---|---|
| GET | `/api/services` | User | User's services; service credential DTOs are included for this owner-facing route. |
| POST | `/api/services` | User | `providerType`, `fromEmail`, `fromName`, plus the matching optional credential DTO (`serviceApiKey`, `serviceOauth`, or `serviceAppPassword`). |
| PUT | `/api/services/{serviceId}` | User | Service update DTO. |
| DELETE | `/api/services/{serviceId}` | User | No body; deletes service. |
| PUT | `/api/services/{serviceId}/status` | User | `{ isActive }`. |
| PUT | `/api/services/{serviceId}/unlock` | User | No body; attempts to unlock service. |

**Project services — `ProjectServicesController.cs`**

| Method | Route | Access | Request / response |
|---|---|---|---|
| GET | `/api/projects/{id}/services` | User | Project's linked services. |
| POST | `/api/projects/{id}/services/{serviceId}` | User | No body; links service to project. |
| DELETE | `/api/projects/{id}/services/{serviceId}` | User | No body; unlinks service. |

**Integrations — `ProjectIntegrationsController.cs` and `IntegrationsController.cs`**

| Method | Route | Access | Request / response |
|---|---|---|---|
| GET | `/api/projects/{id}/integrations` | User | Project integrations. |
| POST | `/api/projects/{id}/integrations` | User | `integrationType`, `configJson`. |
| PUT | `/api/integrations/{intId}` | User | `configJson`. |
| PUT | `/api/integrations/{intId}/status` | User | `{ isActive }`. |
| DELETE | `/api/integrations/{intId}` | User | No body; removes integration. |

**Templates — `ProjectTemplatesController.cs` and `TemplatesController.cs`**

| Method | Route | Access | Request / response |
|---|---|---|---|
| GET | `/api/projects/{id}/templates` | User | Project template list. |
| POST | `/api/projects/{id}/templates` | User | Template DTO: name, subject, HTML, recipient, optional service, history, CAPTCHA/App Check, CC/BCC/reply-to, auto-reply settings. |
| GET | `/api/templates/{id}` | User | Template details. |
| PUT | `/api/templates/{id}` | User | Same template DTO shape as create. |
| DELETE | `/api/templates/{id}` | User | No body; deletes template. |
| POST | `/api/templates/{id}/attachments` | User | Multipart form file `file`; returns attachment DTO. |
| DELETE | `/api/templates/{id}/attachments/{attachmentId}` | User | No body; deletes attachment. |
| PUT | `/api/templates/{id}/status` | User | `{ isActive }`. |
| PUT | `/api/templates/{id}/unlock` | User | No body; attempts to unlock template. |

**Submissions — `ProjectSubmissionsController.cs`, `SubmissionsController.cs`, and `SubmitController.cs`**

| Method | Route | Access | Request / response |
|---|---|---|---|
| POST | `/api/submit/{PublicApiKey}` | Anonymous | `templateId`, optional `privateKey`, `recipientEmail`, `recipientName`, `recaptchaToken`, `appCheckToken`, and `fields` dictionary. Returns `{ message, status }`. |
| POST | `/api/support` | Anonymous | Multipart form: name/email and exactly one complaint or suggestion; complaint may include an attachment. |
| GET | `/api/projects/{id}/submissions?page=1` | User | Paginated project submission history. |
| GET | `/api/submissions/{id}` | User | Submission details including status, payload/body when retained, error, and AI summary. |
| GET | `/api/submissions/{id}/status` | User | Submission status string. |
| POST | `/api/submissions/{id}/retry` | User | No body; queues retry and returns submission history DTO. |

**Billing — `BillingController.cs`**

| Method | Route | Access | Request / response |
|---|---|---|---|
| GET | `/api/billing/plans` | User | Active plans and limits/features. |
| GET | `/api/billing/subscription` | User | Current subscription and usage summary. |
| POST | `/api/billing/subscribe` | User | `planId`, optional `paymentMethodId`; controller permits only the `Free` plan. |
| GET | `/api/billing/invoices` | User | User invoices, including invoice PDF URL. |

**Analytics — `AnalyticsController.cs`**

| Method | Route | Access | Request / response |
|---|---|---|---|
| GET | `/api/analytics/overview` | User | Overview analytics DTO. |
| GET | `/api/analytics/projects/{id}` | User | Project analytics DTO. |

**Admin — `AdminController.cs`**  
All routes require the `Admin` role. The controller explicitly disables rate limiting.

| Method | Route | Access | Request / response |
|---|---|---|---|
| GET | `/api/admin/dashboard` | Admin | Dashboard statistics. |
| GET | `/api/admin/analytics` | Admin | System analytics. |
| GET | `/api/admin/users?page=1` | Admin | Paged users. |
| GET | `/api/admin/users/search?searchTerm=…&page=1` | Admin | Paged search results. |
| GET | `/api/admin/users/{userId}/details` | Admin | User details. |
| PUT | `/api/admin/users/{id}/status` | Admin | `{ isActive }`. |
| GET | `/api/admin/users/{id}/projects` | Admin | User's projects. |
| GET | `/api/admin/banned?page=1` | Admin | Paged bans. |
| POST | `/api/admin/banned` | Admin | `AddBanDto`; adds or updates a ban. |
| DELETE | `/api/admin/banned` | Admin | `BanDto`; removes a ban. |
| PUT | `/api/admin/banned` | Admin | `BanDto`; reduces ban level. |
| GET | `/api/admin/banned/{ip}` | Admin | Ban details for identifier. |
| GET | `/api/admin/banned/search?searchTerm=…&page=1` | Admin | Paged ban search results. |
| POST | `/api/admin/plans` | Admin | `CreatePlanDto`; creates or updates plan. |
| GET | `/api/admin/projects?page=1` | Admin | Paged projects. |
| GET | `/api/admin/projects/search?searchTerm=…&page=1` | Admin | Paged project search results. |
| GET | `/api/admin/projects/{id}/templates` | Admin | Project templates. |
| GET | `/api/admin/projects/{id}/services` | Admin | Project services. |
| GET | `/api/admin/templates?page=1` | Admin | Paged templates. |
| GET | `/api/admin/templates/search?searchTerm=…&page=1` | Admin | Paged template search results. |
| GET | `/api/admin/templates/{id}` | Admin | Template details. |
| GET | `/api/admin/services?page=1` | Admin | Paged services. |
| GET | `/api/admin/services/search?searchTerm=…&page=1` | Admin | Paged service search results. |
| GET | `/api/admin/services/{id}` | Admin | Service details. |
| GET | `/api/admin/logs?page=1` | Admin | Paged system logs. |
| GET | `/api/admin/logs/search?searchTerm=…&page=1` | Admin | Paged log search results. |
| POST | `/api/admin/subscribe` | Admin | `AdminSubscribeRequestDto`; subscribes a user to a plan. |

## Security

- **JWT validation:** Signature, issuer, audience, and expiry are validated; clock skew is zero. Only tokens with `purpose=login` are accepted as bearer credentials.
- **Refresh/access revocation:** Refresh-token use/revocation is stored in SQL. Access-token logout blacklist is in process memory and expires with the token; it is not shared across instances.
- **Password storage:** BCrypt hashes passwords.
- **Connected-service credential encryption:** App passwords and API keys use AES-GCM. The key is derived as SHA-256 of `ENCRYPTION_MASTER_KEY`; a random nonce and authentication tag are stored with ciphertext in Base64 form. OAuth access/refresh tokens, template CAPTCHA/App Check secrets, and integration `ConfigJson` are not encrypted by this helper. The service-list endpoint decrypts and returns API keys/app passwords to the authenticated owner.
- **Rate limits:** Fixed-window policies:
  - `forOAuth`: 3 requests/minute per remote IP; no queue. Applied to auth and OAuth controller routes.
  - `forSubmit`: 10 requests/minute per remote IP; no queue. Applied to submit/support routes.
  - `perUser`: 60 requests/minute, partitioned by user ID or remote IP when anonymous; queue limit 2. Applied to normal authenticated resource controllers.
  - Admin controller opts out.
  - Rejections return 429 and record a violation; a path containing `/login` gets weight 3, other rejected requests weight 1.
- **Ban escalation:** Identifiers are authenticated user emails or otherwise remote IP addresses. Violations add `weight / 5` points; the threshold is 5 points, with the point cache expiring two hours after the most recent update. Weights are 1 for submission/rate-limit spam, 3 for login rate limiting, and 5 for forbidden access. Each automatic ban raises the level up to 8; durations are 1 minute, 5 minutes, 15 minutes, 1 hour, 6 hours, 24 hours, 7 days, and 30 days. Active bans are cached in memory and warmed from SQL at startup. Admin can manually ban, pardon, or reduce a ban level.
- **Bot protection:** Templates may require Google reCAPTCHA or an App Check token for frontend submissions. reCAPTCHA verifies `success`; if a `hostname` is returned, it must be in the project's allowed-domain list; if a `score` is returned, it must be at least 0.5. App Check validates the token signature using the template's symmetric secret, validates lifetime with zero skew, validates issuer only when configured, and disables audience validation. Backend-mode submissions bypass both checks. The App Check logic is JWT validation in this code; it does not call a Firebase App Check verification endpoint.
- **CORS:** Startup defines a production frontend-origin policy with credentials, a Development localhost policy with credentials, and an any-origin policy. The submit controller explicitly uses the any-origin policy; production and Development otherwise select their respective frontend policies. CORS is not caller authentication.
- **HTML/input validation:** DTOs use `[ApiController]` model validation and custom validation attributes. Template HTML is checked on create/update and again after variable replacement; if `HtmlSanitizer` would remove a tag, attribute, style, or at-rule, the request/job is rejected. It does not silently replace unsafe content with sanitized output.
- **Request-size controls:** Kestrel has a 2 MB maximum configured; selected template and submit actions declare 3 MB limits, support declares 5 MB, and attachment storage rejects files over 10 MB.
- **Other:** HTTPS redirection is enabled. The Hangfire dashboard uses Basic Auth, but its configured `RequireSsl` option is `false`.

## Core business logic

- **Plan limits:** Plans cap monthly email quota, projects, services, templates, and attachments per template, and expose feature flags for Google Sheets, AI, and Telegram. Project creation counts all non-deleted projects for the user; template creation counts non-deleted templates per project; service creation counts non-deleted services across the user's account; attachment upload checks the per-template plan limit. Integrations are checked against the relevant feature flag.
- **Downgrades/upgrades:** Applying a plan ranks projects by submission count and unlocks up to `MaxProjects`; within unlocked projects, templates are ranked by submission count and unlocked up to `MaxTemplates`. Services are ranked by their templates in unlocked projects and then project links; only up to `MaxServices` are unlocked. On expired subscriptions, a hosted worker changes the old subscription to `Expired`, creates a Free subscription, resets `RemainingQuota`, and applies Free plan limits. User subscription handling currently allows only Free; no payment gateway is called by the subscribe path. Billing creates a PDF invoice and records it as `Paid`.
- **Quota enforcement:** Submission is persisted as `Pending` before quota checking. If quota is available, a background job is queued; quota is decremented only after successful email send. If quota is exhausted on a non-Free plan, the stored submission is changed to `QuotaExceeded`; if it is exhausted on Free, the call returns a 403 but the already-persisted submission remains `Pending`. `OverageEmails` is incremented on the exhausted-quota path.
- **Project access modes:** Enforced in `SubmissionService`:
  - `FrontendOnly`: private key is rejected; an Origin/Referer-derived origin is required. If restricted domains are configured, its hostname must match one of them.
  - `BackendOnly`: the private key must be present and equal the project private key; frontend-domain, reCAPTCHA, and App Check checks are bypassed.
  - `Hybrid`: treated as backend only when a private key is supplied and no origin is present; otherwise treated as frontend. Thus a request with both a private key and origin is treated as frontend and rejected for including a private key.
  - Frontend requests must include an origin even if no domain restriction is configured. Domain values are comma-separated and compared case-insensitively after trimming.
- **Template rendering:** Variables use exact, case-sensitive `{{VariableName}}` placeholders, replaced literally in subject and HTML. Final HTML is checked for unsafe content after substitution. Auto-reply templates are processed in a loop after successful send; each configured auto-reply creates another submission and consumes quota. No explicit chain-depth/cycle limit was found.
- **Submission lifecycle:** Defined statuses are `Pending`, `Sent`, `Failed`, `Resent`, `Discarded`, and `QuotaExceeded`.
  - `Pending` is set when a submission is created and when an eligible retry is queued.
  - Successful ordinary delivery becomes `Sent`; successful retry becomes `Resent`.
  - Rendering, provider failure, or exception becomes `Failed`.
  - Exhausted quota on a non-Free plan becomes `QuotaExceeded`.
  - `Discarded` is declared but nothing in the code currently assigns it.
  - With history disabled, payload/body are stored as empty strings and `IsPrivateData` is true; such a submission cannot be retried.

## Background processing

- **Hangfire:** Configured with SQL Server storage using `DB_CONNECTION_STRING`; `AddHangfireServer()` starts workers. The dashboard is at `/hangfire`, protected by Basic Auth using `HANGFIRE_LOGIN` and `HANGFIRE_PASSWORD`. Jobs are enqueued for public email submissions, retries, support/admin notification emails, and some ban/exception notifications. Email delivery and integrations occur in `ProcessInBackgroundAsync`, not inline in the public submit request.
- **Retries:** No explicit Hangfire retry policy or custom retry attributes are configured. The code does expose a user-triggered submission retry endpoint; that is separate from Hangfire's job retry configuration.
- **Hosted workers:**
  - `SubscriptionMonitorHelper` scans for paid subscriptions expiring in three days and expired subscriptions, then sends notices/downgrades to Free; it loops every 24 hours.
  - `LogDatabaseProcessor` consumes an in-memory channel (capacity 10,000, configured to drop oldest when full) and writes logs to SQL in batches up to 100.
  - `BanCacheWarmupHelper` loads currently active bans into memory at startup.
- **Integration timing:** Integrations execute in the submission background processing path after successful email delivery. Errors are logged and do not change the email result.

## Third-party integrations

- **System and user SMTP:** MailKit `SmtpClient` with STARTTLS. System email uses the `SystemEmail` settings and system SMTP credentials; user App Password services use their stored SMTP details. No SMTP failover is implemented.
- **Resend and SendGrid:** Direct HTTP calls to each provider's API using `HttpClient`; credentials are stored as encrypted API keys. No provider failover is implemented.
- **Google and Microsoft email OAuth:** Google sends through Gmail API; Microsoft sends through Microsoft Graph. OAuth refresh is attempted when token expiry is within one minute. Provider errors are returned/logged; no cross-provider failover is implemented.
- **Google/Microsoft OAuth authorization:** ASP.NET Core provider handlers and temporary external cookie; see OAuth behavior in section 5.
- **Google Sheets:** Google Sheets v4 SDK with service-account JSON credentials; appends a row containing timestamp, project, recipient, subject, result, and error.
- **Telegram:** HTTP call to Telegram Bot API `sendMessage`; configured client timeout is 10 seconds.
- **AI summaries:** HTTP calls use OpenAI-compatible endpoints for Groq (`qwen/qwen3.8-27b`), Gemini (`gemini-flash-latest`), and GitHub Models (`gpt-4o-mini`). The code tries them sequentially, skips providers without a configured key, uses a 30-second client timeout, and returns the first non-empty successful response. This is the only explicit provider fallback/failover found.
- **reCAPTCHA:** HTTP POST to Google's site verification endpoint.
- **Azure Blob Storage:** Azure SDK stores template attachments and invoice PDFs in separate containers; container creation requests public blob read access.
- **QuestPDF:** Used to generate invoice PDFs.
- No external payment processor, SMS provider, or other LLM provider is integrated.

## Configuration

Names only; no configured secret values are reproduced.

**Required for startup or core feature construction:**

- `DB_CONNECTION_STRING` — required by BLL registration; used by SQL Server EF Core and Hangfire.
- `JWT_SECRET_KEY` — required directly by API startup and token service.
- `JWT_ISSUER`, `JWT_AUDIENCE` — required by the token service for token creation/validation; API validation reads these from environment variables.
- `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `MICROSOFT_CLIENT_ID`, `MICROSOFT_CLIENT_SECRET` — required by API startup, even if OAuth is not being used.
- `HANGFIRE_LOGIN`, `HANGFIRE_PASSWORD` — required at startup for dashboard authorization.
- `ClientUrl` — required by auth and subscription-monitor services.
- `ENCRYPTION_MASTER_KEY` — required when encryption helper is constructed.
- `BS_CONNECTION_STRING` — Azure Blob Storage connection string; required when attachment manager is constructed.
- `GOOGLE_SHEETS_CREDENTIALS`, `TELEGRAM_BOT_TOKEN` — required when submission/integration services are constructed, even if those integrations are not used.
- `SYSTEM_SMTP_USERNAME`, `SYSTEM_SMTP_ENCRYPTED_PASSWORD`, `ADMIN_EMAIL` — required when the system email sender is constructed.
- `SystemEmail:SmtpHost`, `SystemEmail:SmtpPort`, `SystemEmail:FromEmail` — required configuration values for system SMTP.
- `PageSize` — read for pagination; a value is present in the app configuration.

**Optional or fallback-backed:**

- `SystemEmail:FromName` — falls back to an application default if absent.
- `AiSettings:GroqKey`, `AiSettings:GeminiKey`, `AiSettings:GithubKey` — optional; missing keys cause that AI provider to be skipped.
- `ASPNETCORE_ENVIRONMENT` — selects Development vs production middleware/Swagger behavior; launch profiles set it for local runs.
- Standard ASP.NET configuration keys such as `AllowedHosts` and `Logging` are also present in appsettings.

If a `.env` file exists in the working directory, the API loads it through DotNetEnv. The repository's `.gitignore` excludes `.env` files.

## Database setup

- SQL Server is accessed through EF Core and the `EmailyDbContext` model.
- The repository does **not** include EF migrations, schema scripts, or seed
  data, and the application does not create the schema on startup. A compatible
  SQL Server schema must be provisioned separately before the API can run.
- The free plan is looked up by `MonthlyPrice == 0`, so a plan with a zero
  monthly price must exist in the database.

## Testing

There is currently no automated test project.

## Deployment

- `profile.arm.json` is an Azure ARM deployment profile for a Windows Azure App
  Service target with HTTPS-only enabled, a system-assigned identity, and an App
  Service plan.
- `launchSettings.json` contains local HTTP/HTTPS and IIS Express profiles.
- The ARM template does not provision the SQL Server database or configure the
  application's required environment settings.
- There is no Dockerfile, docker-compose file, or CI/CD workflow in the
  repository.

## Known limitations and hardening notes

**Production hardening items**

- The OAuth callback posts the tokens to the opener window with a `postMessage`
  target of `*`; an inline comment says to replace it with the frontend origin
  for production (`OAuthController.cs`).
- The Hangfire dashboard sets `RequireSsl = false`; a nearby comment recommends
  enabling SSL in production (`Program.cs`).
- Google OAuth's `Scope.Clear()` call is commented out, so the provider's default
  scopes are not cleared before the Gmail send scope is added (`Program.cs`).
- Not every stored secret is encrypted: OAuth tokens, template CAPTCHA/App Check
  secrets, and integration configuration are not covered by the AES-GCM helper
  (see [Security](#security)).
- The access-token blacklist is in memory, so it is not persisted or shared
  between instances (see [Authentication](#authentication-and-authorization)).

**Other known gaps**

- `Discarded` is a declared submission status that nothing currently assigns.
- No explicit Hangfire retry policy is configured; the user-triggered retry
  endpoint is separate from Hangfire's job retries.
- There are no database migrations and no automated tests.
- `SearchSystemLogsAsync` relies on an assumption, recorded in a code comment,
  that EF Core translates its null-forgiving LINQ expressions safely; this is
  untested (`AdminService.cs`).

## Author

**Ahmed Ramadan** — built as a personal project to practice production-style
full-stack development.
[Portfolio / contact →](https://ahm3dramad3n.github.io/portfolio/en/contact.html)

## License

Released under the **Emaily Personal & Educational License** — see
[`LICENSE`](./LICENSE) for the authoritative text. In short:

- You are welcome to read, study, and learn from the code, and to use
  snippets for personal, educational, and learning purposes.
- You may **not** use the Emaily name, logos, the "Industrial Blueprint"
  design system, or any original branding assets.
- You may **not** host or deploy a clone of this software and present it as
  your own product, project, or service.
- You may **not** use it for commercial purposes or monetization.

The software is provided "as is", without warranty of any kind.
