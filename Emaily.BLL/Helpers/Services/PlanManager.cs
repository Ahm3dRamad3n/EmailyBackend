using Emaily.BLL.Helpers.Interfaces;
using Emaily.DAL.Entities;
using Emaily.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.Helpers.Services
{
    public class PlanManager(IUnitOfWork uow) : IPlanManager
        {
            private readonly IUnitOfWork _uow = uow;

                 public async Task ApplyPlanLimitsAsync(Guid userId, Plan targetPlan)
            {
                // ========================================================
                // 1 & 2. معالجة المشاريع والقوالب (Top-Down Approach)
                // ========================================================

                var projects = await _uow.Projects.FindAllAsync(
                    p => p.UserId == userId,
                    includes: q => q.Include(p => p.Templates)
                                    .ThenInclude(t => t.Submissions),
                    disableTracking: false
                );

                // الترتيب حسب إجمالي الاستخدام للمشروع
                var orderedProjects = projects
                    .OrderByDescending(p => p.Templates.Sum(t => t.Submissions?.Count ?? 0))
                    .ToList();

                for (int pIndex = 0; pIndex < orderedProjects.Count; pIndex++)
                {
                    var project = orderedProjects[pIndex];

                    if (pIndex < targetPlan.MaxProjects)
                    {
                        project.IsLocked = false;

                        // ترتيب القوالب داخل المشروع الناجي
                        var orderedTemplates = project.Templates
                            .OrderByDescending(t => t.Submissions?.Count ?? 0)
                            .ToList();

                        for (int tIndex = 0; tIndex < orderedTemplates.Count; tIndex++)
                        {
                            orderedTemplates[tIndex].IsLocked = tIndex >= targetPlan.MaxTemplates;
                        }
                    }
                    else
                    {
                        project.IsLocked = true;

                        foreach (var template in project.Templates)
                        {
                            template.IsLocked = true;
                        }
                    }

                    //_uow.Projects.Update(project);
                }


            // ========================================================
            // 3. معالجة الخدمات (Global Usage Dual-Scoring)
            // بناءً على علاقة One-to-Many مع Template و Many-to-Many مع Project
            // ========================================================

            var services = await _uow.Services.FindAllAsync(
                    s => s.UserId == userId,
                    includes: q => q.Include(s => s.Templates)         // One-to-Many مباشرة
                                    .ThenInclude(t => t.Project)       // نحتاج المشروع لنتأكد أنه ناجي
                                    .Include(s => s.ProjectServices)   // Many-to-Many
                                    .ThenInclude(ps => ps.Project),
                    disableTracking: false
                );

                // التقييم الذكي المزدوج (Dual-Scoring):
                // 1. الأولوية الكبرى: الخدمة المربوطة بأكبر عدد من (القوالب المفتوحة داخل مشاريع مفتوحة)
                // 2. الفاصل عند التعادل (ThenBy): الخدمة المربوطة بأكبر عدد من (المشاريع المفتوحة)
                var orderedServices = services
                    .OrderByDescending(s => s.Templates.Count(t =>
                        !t.IsLocked &&
                        t.Project != null &&
                        !t.Project.IsLocked))
                    .ThenByDescending(s => s.ProjectServices.Count(ps =>
                        ps.Project != null &&
                        !ps.Project.IsLocked))
                    .ToList();

                for (int sIndex = 0; sIndex < orderedServices.Count; sIndex++)
                {
                    // ترك الخدمات الأعلى تقييماً نشطة، وقفل الباقي (Zero-Conflict)
                    orderedServices[sIndex].IsLocked = sIndex >= targetPlan.MaxServices;
                    //_uow.Services.Update(orderedServices[sIndex]);
                }

                // ========================================================
                // 4. سيتم حفظ التغييرات بعد الخروج من جميع المعالجات لضمان الكفاءة وتقليل عمليات الكتابة على قاعدة البيانات.
                // ========================================================
                // await _uow.CompleteAsync();
        }
    }
    
}
