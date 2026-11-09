using BabyCareApp.Application.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

/// <summary>"Platform statistics" — Super Admin only.</summary>
[ApiController]
[Route("api/v1/admin/statistics")]
[Authorize(Policy = "PlatformSuperAdmin")]
public class AdminStatisticsController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminStatisticsController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    public async Task<ActionResult<PlatformStatisticsResponse>> Get(CancellationToken cancellationToken)
    {
        var statistics = await _adminService.GetStatisticsAsync(cancellationToken);
        return Ok(statistics);
    }
}
