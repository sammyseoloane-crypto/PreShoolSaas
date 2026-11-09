using BabyCareApp.Application.Admin;
using BabyCareApp.Application.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

/// <summary>"Subscription overview" — Super Admin only. The confirm-payment/mark-past-due
/// actions are billing-integration preparation: no real payment gateway is integrated yet,
/// so these exist today for manual/EFT payment confirmation, common in South Africa
/// alongside card gateways like PayFast/Yoco — a future webhook handler would call the
/// same ISubscriptionService methods these delegate to.</summary>
[ApiController]
[Route("api/v1/admin/subscriptions")]
[Authorize(Policy = "PlatformSuperAdmin")]
public class AdminSubscriptionsController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ISubscriptionService _subscriptionService;

    public AdminSubscriptionsController(IAdminService adminService, ISubscriptionService subscriptionService)
    {
        _adminService = adminService;
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Application.Admin.SubscriptionResponse>>> List(CancellationToken cancellationToken)
    {
        var subscriptions = await _adminService.ListSubscriptionsAsync(cancellationToken);
        return Ok(subscriptions);
    }

    [HttpPost("{tenantId:guid}/confirm-payment")]
    public async Task<IActionResult> ConfirmPayment(Guid tenantId, CancellationToken cancellationToken)
    {
        await _subscriptionService.RecordPaymentSucceededAsync(tenantId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{tenantId:guid}/mark-past-due")]
    public async Task<IActionResult> MarkPastDue(Guid tenantId, CancellationToken cancellationToken)
    {
        await _subscriptionService.RecordPaymentFailedAsync(tenantId, cancellationToken);
        return NoContent();
    }
}
