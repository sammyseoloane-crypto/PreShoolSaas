using BabyCareApp.Application.Admin;
using BabyCareApp.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

/// <summary>Platform-level tenant administration — Super Admin only.</summary>
[ApiController]
[Route("api/v1/admin/tenants")]
[Authorize(Policy = "PlatformSuperAdmin")]
public class AdminTenantsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminTenantsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminTenantResponse>>> List(
        [FromQuery] TenantListQuery query, CancellationToken cancellationToken)
    {
        var result = await _adminService.ListTenantsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminTenantResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _adminService.GetTenantAsync(id, cancellationToken);
        return Ok(tenant);
    }

    /// <summary>Also serves as "school verification" when applied to a tenant pending verification.</summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.ActivateTenantAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, CancellationToken cancellationToken)
    {
        await _adminService.SuspendTenantAsync(id, cancellationToken);
        return NoContent();
    }
}
