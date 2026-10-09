using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Billing;
using Emaily.BLL.Helpers.Interfaces;
using Emaily.BLL.Interfaces;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;

namespace Emaily.BLL.Services
{
    public class BillingService(IUnitOfWork uow, IPlanManager planManager, IAttachmentManager attachmentManager, ILoggerService Logger
        ) : IBillingService
    {
        private readonly IUnitOfWork _uow = uow;
        private readonly ILoggerService _logger = Logger;
        private readonly IPlanManager _planManager = planManager;
        private readonly IAttachmentManager _am = attachmentManager;

        public async Task<Result<IEnumerable<PlanDto>>> GetActivePlansAsync()
        {
            var plans = await _uow.Plans.FindAllAsync(p => p.IsActive);
            return Result<IEnumerable<PlanDto>>.Success(plans.Select(p => new PlanDto
            {
                Id = p.Id.ToString(),
                Name = p.Name,
                MonthlyPrice = p.MonthlyPrice,
                MaxEmailsPerMonth = p.MaxEmailsPerMonth,
                MaxProjects = p.MaxProjects,
                MaxServices = p.MaxServices,
                MaxTemplates = p.MaxTemplates,
                MaxAttachmentsPerTemplate = p.MaxAttachmentsPerTemplate,
                CanUseGoogleSheets = p.CanUseGoogleSheets,
                CanUseAI = p.CanUseAi,
                CanUseTelegramBot = p.CanUseTelegramBot
            }).OrderBy(p => p.MonthlyPrice));
        }

        public async Task<Result<SubscriptionDetailsDto>> GetCurrentSubscriptionAsync(Guid userId)
        {
            var user = await _uow.Users.FindAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null) return Result<SubscriptionDetailsDto>.Failure("User not found.", StatusCodes.Status404NotFound);

            var subscriptions = await _uow.Subscriptions.FindAllAsync(s => s.UserId == userId && s.Status == Subscription.Statuses.Active);
            var activeSub = subscriptions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();

            if (activeSub == null) return Result<SubscriptionDetailsDto>.Failure("No active subscription found.", StatusCodes.Status404NotFound);

            var plan = await _uow.Plans.FindAsync(p => p.Id == activeSub.PlanId);
            if (plan == null) return Result<SubscriptionDetailsDto>.Failure("Plan not found.", StatusCodes.Status404NotFound);

            // حساب الاستهلاك الحالي
            var projects = await _uow.Projects.FindAllAsync(p => p.UserId == userId && !p.IsDeleted);
            var projectIds = projects.Select(p => p.Id).ToList();

            var servicesCount = await _uow.Services.CountAsync(s => s.UserId == userId && !s.IsDeleted);
            var templatesCount = await _uow.Templates.CountAsync(t => projectIds.Contains(t.ProjectId) && !t.IsDeleted);

            return Result<SubscriptionDetailsDto>.Success(new SubscriptionDetailsDto
            {
                UserId = userId.ToString(),
                SubscriptionId = activeSub.Id.ToString(),
                PlanId = plan.Id.ToString(),
                PlanName = plan.Name,
                Status = activeSub.Status,
                StartDate = activeSub.StartDate,
                EndDate = activeSub.EndDate,
                RemainingEmails = user.RemainingQuota,
                OverageEmails = user.OverageEmails,
                UsedProjects = projects.Count(),
                MaxProjects = plan.MaxProjects,
                UsedServices = servicesCount,
                MaxServices = plan.MaxServices,
                UsedTemplates = templatesCount,
                MaxTemplates = plan.MaxTemplates
            });
        }

        public async Task<Result<bool>> SubscribeAsync(Guid userId, SubscribeRequestDto dto)
        {
            var user = await _uow.Users.FindAsync(u => u.Id == userId && !u.IsDeleted);
            if (user == null) return Result<bool>.Failure("User not found.", StatusCodes.Status404NotFound);

            var plan = await _uow.Plans.FindAsync(p => p.Id == dto.PlanId && p.IsActive);
            if (plan == null) return Result<bool>.Failure("Plan not found or inactive.", StatusCodes.Status404NotFound);

            var currentSubs = await _uow.Subscriptions.FindAllAsync(s => s.UserId == userId && s.Status == Subscription.Statuses.Active);
            foreach (var sub in currentSubs)
            {
                sub.Status = Subscription.Statuses.Revoke;
                sub.RevokeAt = DateTime.UtcNow;
                _uow.Subscriptions.Update(sub);
            }

            var activeSubscription = currentSubs.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
            if (activeSubscription != null && activeSubscription.PlanId == plan.Id && plan.Name == "Free")
            {
                return Result<bool>.Failure("You are already subscribed to the free plan, and it renews automatically every month.", StatusCodes.Status400BadRequest);
            }

            var newSubscription = new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PlanId = plan.Id,
                Status = Subscription.Statuses.Active,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Subscriptions.AddAsync(newSubscription);

            IFormFile file =  GenerateInvoicePdfAsync(user, plan, newSubscription);
            var invoiceUrl = _am.Add(file, Helpers.Services.AttachmentManager.AttachmentSource.Invoice);
            if (string.IsNullOrEmpty(invoiceUrl))
            {
                _logger.LogError(new Log(Guid.Empty, $"Failed to generate or upload the invoice PDF for user {userId} and subscription {newSubscription.Id}."));
                return Result<bool>.Failure("Failed to generate or upload the invoice PDF.", StatusCodes.Status500InternalServerError);
            }


            // 4. حفظ الرابط في قاعدة البيانات
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                SubscriptionId = newSubscription.Id,
                Amount = plan.MonthlyPrice,
                Status = Invoice.Statuses.Paid,
                InvoiceDate = DateTime.UtcNow,
                InvoicePdfUrl = invoiceUrl
            };
            await _uow.Invoices.AddAsync(invoice);

            user.RemainingQuota = plan.MaxEmailsPerMonth;
            _uow.Users.Update(user);

            await _planManager.ApplyPlanLimitsAsync(userId, plan);

            // حفظ كل التعديلات في قاعدة البيانات دفعة واحدة
            await _uow.CompleteAsync();

            return Result<bool>.Success(true);
        }

        private static IFormFile GenerateInvoicePdfAsync(User user, Plan plan, Subscription sub)
        {
            // إعداد الترخيص
            QuestPDF.Settings.License = LicenseType.Community;

            // ألوان الهوية البصرية (متناسقة مع تصميم Emaily)
            var primaryColor = "#0b1120"; // Dark Navy
            var accentColor = "#fbbf24";  // Amber / Mustard
            var textMuted = Colors.Grey.Medium;
            var bgLight = Colors.Grey.Lighten4;

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor(Colors.Black));

                    // ================= HEADER =================
                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            // لوجو واسم الشركة (يسار)
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Emaily").FontSize(28).Black().FontColor(primaryColor);
                                col.Item().Text("Cloud Email Services").FontSize(10).FontColor(accentColor).SemiBold();
                            });

                            // بيانات الفاتورة (يمين)
                            row.ConstantItem(150).AlignRight().Column(col =>
                            {
                                col.Item().Text("INVOICE").FontSize(20).Bold().FontColor(primaryColor);
                                col.Item().Text($"# {sub.Id.ToString()[..8].ToUpper()}").FontSize(12).SemiBold();
                                col.Item().Text($"Date: {DateTime.UtcNow:dd MMM yyyy}").FontColor(textMuted);
                            });
                        });

                        // خط فاصل ديكوري
                        header.Item().PaddingTop(15).LineHorizontal(1).LineColor(bgLight);
                    });

                    // ================= CONTENT =================
                    page.Content().PaddingVertical(20).Column(content =>
                    {
                        // 1. بيانات العميل والشركة المفوترة (Billed To / Billed From)
                        content.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("BILLED TO:").FontSize(9).FontColor(textMuted).SemiBold();
                                col.Item().Text(user.FullName).FontSize(12).Bold().FontColor(primaryColor);
                                col.Item().Text(user.Email);
                            });

                            row.RelativeItem().AlignRight().Column(col =>
                            {
                                col.Item().Text("BILLED FROM:").FontSize(9).FontColor(textMuted).SemiBold();
                                col.Item().Text("Emaily Inc.").FontSize(12).Bold().FontColor(primaryColor);
                                col.Item().Text("123 Tech Boulevard");
                                col.Item().Text("Cloud City, Web 10101");
                                col.Item().Text("support@emaily.com");
                            });
                        });

                        // 2. جدول تفاصيل الدفع (Invoice Table)
                        content.Item().PaddingTop(30).Table(table =>
                        {
                            // تعريف أعمدة الجدول
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // الوصف (يأخذ مساحة أكبر)
                                columns.RelativeColumn(2); // فترة الاشتراك
                                columns.RelativeColumn(1); // السعر
                            });

                            // هيدر الجدول
                            table.Header(header =>
                            {
                                header.Cell().Background(primaryColor).Padding(5).Text("Description").FontColor(Colors.White).SemiBold();
                                header.Cell().Background(primaryColor).Padding(5).Text("Billing Period").FontColor(Colors.White).SemiBold();
                                header.Cell().Background(primaryColor).Padding(5).AlignRight().Text("Amount").FontColor(Colors.White).SemiBold();
                            });

                            // سطر بيانات الاشتراك
                            table.Cell().BorderBottom(1).BorderColor(bgLight).Padding(5).Column(col =>
                            {
                                col.Item().Text($"Emaily Subscription - {plan.Name} Plan").SemiBold();
                                col.Item().Text("Monthly recurring charge").FontSize(9).FontColor(textMuted);
                            });

                            table.Cell().BorderBottom(1).BorderColor(bgLight).Padding(5)
                                 .Text($"{sub.StartDate:MMM dd, yyyy} - {sub.EndDate:MMM dd, yyyy}");

                            table.Cell().BorderBottom(1).BorderColor(bgLight).Padding(5).AlignRight()
                                 .Text($"${plan.MonthlyPrice:0.00}").SemiBold();

                            // سطر الإجمالي (Total)
                            table.Cell().ColumnSpan(2).Padding(5).AlignRight().Text("TOTAL AMOUNT PAID:").Bold().FontColor(primaryColor);
                            table.Cell().Padding(5).AlignRight().Text($"${plan.MonthlyPrice:0.00}").FontSize(14).Bold().FontColor(accentColor);
                        });

                        // 3. قسم مميزات الخطة
                        content.Item().PaddingTop(30).Background(bgLight).Padding(15).Column(col =>
                        {
                            col.Item().PaddingBottom(5).Text("Plan Includes:").SemiBold().FontColor(primaryColor);

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Column(leftCol =>
                                {
                                    leftCol.Item().Text($"• {plan.MaxEmailsPerMonth:N0} Emails/month");
                                    leftCol.Item().Text($"• {plan.MaxProjects} Projects");
                                    leftCol.Item().Text($"• {plan.MaxServices} Services");
                                });

                                row.RelativeItem().Column(rightCol =>
                                {
                                    rightCol.Item().Text($"• {plan.MaxTemplates} Templates");
                                    rightCol.Item().Text(plan.CanUseAi ? "• AI Features Enabled" : "• No AI Features");
                                    rightCol.Item().Text(plan.CanUseGoogleSheets ? "• Google Sheets Integration" : "");
                                });
                            });
                        });
                    });

                    // ================= FOOTER =================
                    page.Footer().Column(footer =>
                    {
                        footer.Item().LineHorizontal(1).LineColor(bgLight);
                        footer.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text("Thank you for choosing Emaily!").FontSize(10).FontColor(textMuted).SemiBold();
                            row.RelativeItem().AlignRight().Text(x =>
                            {
                                x.Span("Page ");
                                x.CurrentPageNumber();
                                x.Span(" of ");
                                x.TotalPages();
                            });
                        });
                    });
                });
            });

            // توليد الـ PDF في الذاكرة (Memory) كـ Byte Array بدلاً من حفظه في مسار
            byte[] pdfBytes = document.GeneratePdf();

            // وضع الـ Bytes في MemoryStream
            var stream = new MemoryStream(pdfBytes);

            // إنشاء اسم فريد ومعبر للملف
            string fileName = $"Invoice_{sub.Id.ToString()[..8].ToUpper()}.pdf";

            // تحويله إلى IFormFile
            IFormFile formFile = new FormFile(stream, 0, stream.Length, "invoice", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            // إرجاع الملف لكي يتم إرساله إلى دالة الرفع على Blob Storage
            return formFile;
        }

        public async Task<Result<string>> GetPlanNameByIdAsync(Guid planId)
        {
            var planName = await _uow.Plans.SelectWhereAsync(selector: p => p.Name, criteria: p => p.Id == planId && p.IsActive);
            if (!planName.Any())
                return Result<string>.Failure("Plan not found or inactive.", StatusCodes.Status404NotFound);
            return Result<string>.Success(planName.First());
        }

        public async Task<Result<IEnumerable<InvoiceDto>>> GetUserInvoicesAsync(Guid userId)
        {
            var subscriptions = await _uow.Subscriptions.FindAllAsync(s => s.UserId == userId);
            var subIds = subscriptions.Select(s => s.Id).ToList();

            if (subIds.Count == 0) return Result<IEnumerable<InvoiceDto>>.Success([]);

            var invoices = await _uow.Invoices.FindAllAsync(i => subIds.Contains(i.SubscriptionId));

            return Result<IEnumerable<InvoiceDto>>.Success(invoices.Select(i => new InvoiceDto
            {
                Id = i.Id.ToString(),
                Amount = i.Amount,
                Status = i.Status,
                InvoiceDate = i.InvoiceDate,
                InvoicePdfUrl = i.InvoicePdfUrl
            }).OrderByDescending(i => i.InvoiceDate));
        }
    }
}