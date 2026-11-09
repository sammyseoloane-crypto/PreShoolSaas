using BabyCareApp.Application.Admin;
using BabyCareApp.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

/// <summary>"Platform activity" — Super Admin only.</summary>
[ApiController]
[Route("api/v1/admin/audit-logs")]
[Authorize(Policy = "PlatformSuperAdmin")]
public class AdminAuditLogsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminAuditLogsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogResponse>>> List(
        [FromQuery] AuditLogQuery query, CancellationToken cancellationToken)
    {
        var result = await _adminService.ListAuditLogsAsync(query, cancellationToken);
        return Ok(result);
    }
}
