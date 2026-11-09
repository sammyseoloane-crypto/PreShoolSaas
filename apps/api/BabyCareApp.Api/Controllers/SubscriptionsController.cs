using BabyCareApp.Application.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

/// <summary>Tenant self-service subscription management — the Preschool Owner's own SaaS
/// subscription, entirely separate from preschool school-fee billing to parents.</summary>
[ApiController]
[Route("api/v1/tenants/current/subscription")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:subscription.read")]
    public async Task<ActionResult<SubscriptionResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionService.GetCurrentSubscriptionAsync(cancellationToken);
        return Ok(subscription);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:subscription.write")]
    public async Task<ActionResult<SubscriptionResponse>> Subscribe(SubscribeRequest request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionService.SubscribeAsync(request, cancellationToken);
        return Ok(subscription);
    }

    [HttpPut("plan")]
    [Authorize(Policy = "Permission:subscription.write")]
    public async Task<ActionResult<SubscriptionResponse>> ChangePlan(ChangePlanRequest request, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionService.ChangePlanAsync(request, cancellationToken);
        return Ok(subscription);
    }

    [HttpDelete]
    [Authorize(Policy = "Permission:subscription.write")]
    public async Task<IActionResult> Cancel(CancellationToken cancellationToken)
    {
        await _subscriptionService.CancelSubscriptionAsync(cancellationToken);
        return NoContent();
    }
}
