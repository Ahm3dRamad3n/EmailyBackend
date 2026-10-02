using Emaily.API.Extensions;
using Emaily.BLL.DTOs.Billing;
using Emaily.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Emaily.API.Controllers
{
    [Route("api/billing")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("perUser")]
    public class BillingController(IBillingService billingService) : ControllerBase
    {
        private readonly IBillingService _billingService = billingService;

        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans()
        {
            var plans = await _billingService.GetActivePlansAsync();
            if (!plans.IsSuccess) return StatusCode(plans.ErrorCode, new { success = false, message = plans.ErrorMessage });
            return Ok(plans.Data);
        }

        [HttpGet("subscription")]
        public async Task<IActionResult> GetCurrentSubscription()
        {
            var subscription = await _billingService.GetCurrentSubscriptionAsync(User.GetUserId());
            if (!subscription.IsSuccess) return StatusCode(subscription.ErrorCode, new { success = false, message = subscription.ErrorMessage });
            return Ok(subscription.Data);
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto dto)
        {
            ///////////////////////////////////////////////////////////////////////////////////////////////
            var result = await _billingService.GetPlanNameByIdAsync(dto.PlanId);
            if (!result.IsSuccess)
                return StatusCode(result.ErrorCode, new { success = false, message = result.ErrorMessage });
            if (result.Data != "Free")
                return BadRequest(new { message = "Only the free plan is available for subscription by user" });
            ///////////////////////////////////////////////////////////////////////////////////////////////
            var response = await _billingService.SubscribeAsync(User.GetUserId(), dto);
            if (!response.IsSuccess) return StatusCode(response.ErrorCode, new { success = false, message = response.ErrorMessage });
            return Ok(response.Data);

        }

        [HttpGet("invoices")]
        public async Task<IActionResult> GetInvoices()
        {
            var invoices = await _billingService.GetUserInvoicesAsync(User.GetUserId());
            if (!invoices.IsSuccess) return StatusCode(invoices.ErrorCode, new { success = false, message = invoices.ErrorMessage });
            return Ok(invoices.Data);
        }
    }
}