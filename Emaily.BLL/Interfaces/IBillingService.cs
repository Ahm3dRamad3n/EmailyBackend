using Emaily.BLL.DTOs;
using Emaily.BLL.DTOs.Billing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Emaily.BLL.Interfaces
{
    public interface IBillingService
    {
        Task<Result<IEnumerable<PlanDto>>> GetActivePlansAsync();
        Task<Result<SubscriptionDetailsDto>> GetCurrentSubscriptionAsync(Guid userId);
        Task<Result<string>> GetPlanNameByIdAsync(Guid planId);
        Task<Result<bool>> SubscribeAsync(Guid userId, SubscribeRequestDto dto);
        Task<Result<IEnumerable<InvoiceDto>>> GetUserInvoicesAsync(Guid userId);
    }
}