using BabyCareApp.Application.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

/// <summary>
/// Platform-level tenant administration — Super Admin only. Deliberately minimal here
/// (just the activate/suspend actions Slice 4 calls for); the full Super Admin dashboard
/// (subscription overview, platform activity, statistics) is Slice 6.
/// </summary>
[ApiController]
[Route("api/v1/admin/tenants")]
[Authorize(Policy = "PlatformSuperAdmin")]
public class AdminTenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public AdminTenantsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _tenantService.ActivateTenantAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken cancellationToken)
    {
        await _tenantService.SuspendTenantAsync(id, cancellationToken);
        return NoContent();
    }
}
