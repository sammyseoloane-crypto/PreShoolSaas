using BabyCareApp.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

/// <summary>"Subscription overview" — Super Admin only. Will be empty until Slice 7
/// (Subscription Engine) adds a flow that actually creates Subscription rows.</summary>
[ApiController]
[Route("api/v1/admin/subscriptions")]
[Authorize(Policy = "PlatformSuperAdmin")]
public class AdminSubscriptionsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminSubscriptionsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubscriptionResponse>>> List(CancellationToken cancellationToken)
    {
        var subscriptions = await _adminService.ListSubscriptionsAsync(cancellationToken);
        return Ok(subscriptions);
    }
}
