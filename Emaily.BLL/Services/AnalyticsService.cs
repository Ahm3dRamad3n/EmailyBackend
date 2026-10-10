using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Analytics;
using Emaily.BLL.Interfaces;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emaily.BLL.Services
{
    public class AnalyticsService(IUnitOfWork uow) : IAnalyticsService
    {
        private readonly IUnitOfWork _uow = uow;

        public async Task<OverviewAnalyticsDto> GetOverviewAnalyticsAsync(Guid userId)
        {
            // جلب كل المشاريع الفعالة للمستخدم
            var projects = await _uow.Projects.FindAllAsync(p => p.UserId == userId && !p.IsDeleted);
            var projectIds = projects.Select(p => p.Id).ToList();

            var overview = new OverviewAnalyticsDto();

            if (projectIds.Count == 0)
                return overview; // هيرجع أصفار وقوائم فارغة تلقائياً

            // جلب كل عمليات الإرسال التابعة لمشاريع المستخدم
            var submissions = await _uow.Submissions.FindAllAsync(s => projectIds.Contains(s.ProjectId));

            // الإحصائيات العلوية
            overview.Total = submissions.Count();
            overview.TotalSent = submissions.Count(s => s.Status == Submission.Statuses.Sent || s.Status == Submission.Statuses.Resent);
            overview.TotalFailed = submissions.Count(s => s.Status == Submission.Statuses.Failed);
            overview.SuccessRate = overview.Total > 0
                ? Math.Round((double)overview.TotalSent / overview.Total * 100, 1)
                : 0;

            // حساب الإرسالات لآخر 14 يوم (للرسم البياني)
            // نبدأ من 13 يوم لورا لحد النهارده عشان يبقوا 14 يوم بالترتيب
            var last14Days = Enumerable.Range(0, 14).Select(i => DateTime.UtcNow.Date.AddDays(-13 + i)).ToList();

            overview.VolumeByDay = [.. last14Days.Select(day => new VolumeByDayDto
            {
                Date = day.ToString("yyyy-MM-dd"), // الصيغة المطلوبة في الفرونت إند
                Sent = submissions.Count(s => (s.Status == Submission.Statuses.Sent || s.Status == Submission.Statuses.Resent) && s.ReceivedAt.Date == day),
                Failed = submissions.Count(s => s.Status == Submission.Statuses.Failed && s.ReceivedAt.Date == day),
                Pending = submissions.Count(s => s.Status == Submission.Statuses.Pending && s.ReceivedAt.Date == day)
            })];

            // إحصائيات كل مشروع على حدة (للجدول السفلي)
            overview.ByProject = [.. projects.Select(p =>
            {
                var projSubs = submissions.Where(s => s.ProjectId == p.Id).ToList();
                var total = projSubs.Count;
                var sent = projSubs.Count(s => s.Status == Submission.Statuses.Sent || s.Status == Submission.Statuses.Resent);
                var failed = projSubs.Count(s => s.Status == Submission.Statuses.Failed);

                return new ProjectSummaryDto
                {
                    ProjectId = p.Id,
                    ProjectName = p.Name,
                    Total = total,
                    Sent = sent,
                    Failed = failed,
                    SuccessRate = total > 0 ? Math.Round((double)sent / total * 100, 1) : 0
                };
            })
            .OrderByDescending(p => p.Total)];

            return overview;
        }
        public async Task<Result<ProjectAnalyticsDto>> GetProjectAnalyticsAsync(string projectId)
        {
            var project = await _uow.Projects.FindAsync(p => p.Id == projectId);
            if (project == null) return Result<ProjectAnalyticsDto>.Failure("Project not found", StatusCodes.Status403Forbidden);

            var templates = await _uow.Templates.FindAllAsync(t => t.ProjectId == projectId && !t.IsDeleted);
            var submissions = await _uow.Submissions.FindAllAsync(s => s.ProjectId == projectId);

            var analytics = new ProjectAnalyticsDto
            {
                ProjectName = project.Name,
                Total = submissions.Count(),
                TotalSent = submissions.Count(s => s.Status == Submission.Statuses.Sent || s.Status == Submission.Statuses.Resent),
                TotalFailed = submissions.Count(s => s.Status == Submission.Statuses.Failed)
            };

            analytics.SuccessRate = analytics.Total > 0
                ? Math.Round((double)analytics.TotalSent / analytics.Total * 100, 1)
                : 0;

            // حساب الإرسالات لآخر 14 يوم (للرسم البياني)
            var last14Days = Enumerable.Range(0, 14).Select(i => DateTime.UtcNow.Date.AddDays(-13 + i)).ToList();

            analytics.VolumeByDay = [.. last14Days.Select(day => new VolumeByDayDto
            {
                Date = day.ToString("yyyy-MM-dd"),
                Sent = submissions.Count(s => (s.Status == Submission.Statuses.Sent || s.Status == Submission.Statuses.Resent) && s.ReceivedAt.Date == day),
                Failed = submissions.Count(s => s.Status == Submission.Statuses.Failed && s.ReceivedAt.Date == day),
                Pending = submissions.Count(s => s.Status == Submission.Statuses.Pending && s.ReceivedAt.Date == day)
            })];

            // إحصائيات كل قالب داخل المشروع (للجدول السفلي)
            analytics.ByTemplate = [.. templates.Select(t =>
            {
                var tempSubs = submissions.Where(s => s.TemplateId == t.Id).ToList();
                var total = tempSubs.Count;
                var sent = tempSubs.Count(s => s.Status == Submission.Statuses.Sent || s.Status == Submission.Statuses.Resent);
                var failed = tempSubs.Count(s => s.Status == Submission.Statuses.Failed);

                return new TemplateSummaryDto
                {
                    TemplateName = t.Name,
                    Total = total,
                    Sent = sent,
                    Failed = failed,
                    SuccessRate = total > 0 ? Math.Round((double)sent / total * 100, 1) : 0
                };
            })
            .Where(t => t.Total > 0) // فلترة لعرض القوالب التي تم استخدامها فقط
            .OrderByDescending(t => t.Total)];

            return Result<ProjectAnalyticsDto>.Success(analytics);
        }
    }
}