using BabyCareApp.Application.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

[ApiController]
[Route("api/v1/branches")]
[Authorize]
public class BranchesController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public BranchesController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet]
    [Authorize(Policy = "Permission:branches.read")]
    public async Task<ActionResult<IReadOnlyList<BranchResponse>>> List(CancellationToken cancellationToken)
    {
        var branches = await _tenantService.ListBranchesAsync(cancellationToken);
        return Ok(branches);
    }

    [HttpPost]
    [Authorize(Policy = "Permission:branches.write")]
    public async Task<ActionResult<BranchResponse>> Create(CreateBranchRequest request, CancellationToken cancellationToken)
    {
        var branch = await _tenantService.CreateBranchAsync(request, cancellationToken);
        return Ok(branch);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Permission:branches.read")]
    public async Task<ActionResult<BranchResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var branch = await _tenantService.GetBranchAsync(id, cancellationToken);
        return Ok(branch);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Permission:branches.write")]
    public async Task<ActionResult<BranchResponse>> Update(Guid id, UpdateBranchRequest request, CancellationToken cancellationToken)
    {
        var branch = await _tenantService.UpdateBranchAsync(id, request, cancellationToken);
        return Ok(branch);
    }
}
