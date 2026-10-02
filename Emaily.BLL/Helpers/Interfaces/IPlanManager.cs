using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emaily.DAL.Entities;

namespace Emaily.BLL.Helpers.Interfaces
{
    public interface IPlanManager
    {
        Task ApplyPlanLimitsAsync(Guid userId, Plan targetPlan);
    }
}
