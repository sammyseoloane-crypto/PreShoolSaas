using BabyCareApp.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

/// <summary>"Manage subscription plans" — Super Admin only. Plan pricing/limits are a
/// business decision for the product owner, not invented here; this just gives them a way
/// to enter real values once decided, instead of a migration seeding fake numbers.</summary>
[ApiController]
[Route("api/v1/admin/subscription-plans")]
[Authorize(Policy = "PlatformSuperAdmin")]
public class AdminSubscriptionPlansController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminSubscriptionPlansController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubscriptionPlanResponse>>> List(CancellationToken cancellationToken)
    {
        var plans = await _adminService.ListSubscriptionPlansAsync(cancellationToken);
        return Ok(plans);
    }

    [HttpPost]
    public async Task<ActionResult<SubscriptionPlanResponse>> Create(
        CreateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _adminService.CreateSubscriptionPlanAsync(request, cancellationToken);
        return Ok(plan);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubscriptionPlanResponse>> Update(
        Guid id, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _adminService.UpdateSubscriptionPlanAsync(id, request, cancellationToken);
        return Ok(plan);
    }
}
