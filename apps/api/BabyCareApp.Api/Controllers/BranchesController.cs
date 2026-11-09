using BabyCareApp.Application.Preschool;
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
    private readonly IPreschoolProfileService _preschoolProfileService;

    public BranchesController(ITenantService tenantService, IPreschoolProfileService preschoolProfileService)
    {
        _tenantService = tenantService;
        _preschoolProfileService = preschoolProfileService;
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

    [HttpGet("{id:guid}/operating-hours")]
    [Authorize(Policy = "Permission:branches.read")]
    public async Task<ActionResult<IReadOnlyList<OperatingHourEntry>>> GetOperatingHours(Guid id, CancellationToken cancellationToken)
    {
        var hours = await _preschoolProfileService.GetOperatingHoursAsync(id, cancellationToken);
        return Ok(hours);
    }

    [HttpPut("{id:guid}/operating-hours")]
    [Authorize(Policy = "Permission:branches.write")]
    public async Task<ActionResult<IReadOnlyList<OperatingHourEntry>>> SetOperatingHours(
        Guid id, UpdateOperatingHoursRequest request, CancellationToken cancellationToken)
    {
        var hours = await _preschoolProfileService.SetOperatingHoursAsync(id, request, cancellationToken);
        return Ok(hours);
    }

    [HttpGet("{id:guid}/facilities")]
    [Authorize(Policy = "Permission:branches.read")]
    public async Task<ActionResult<IReadOnlyList<FacilityResponse>>> ListFacilities(Guid id, CancellationToken cancellationToken)
    {
        var facilities = await _preschoolProfileService.ListFacilitiesAsync(id, cancellationToken);
        return Ok(facilities);
    }

    [HttpPost("{id:guid}/facilities")]
    [Authorize(Policy = "Permission:branches.write")]
    public async Task<ActionResult<FacilityResponse>> AddFacility(
        Guid id, AddFacilityRequest request, CancellationToken cancellationToken)
    {
        var facility = await _preschoolProfileService.AddFacilityAsync(id, request, cancellationToken);
        return Ok(facility);
    }

    [HttpDelete("{id:guid}/facilities/{facilityId:guid}")]
    [Authorize(Policy = "Permission:branches.write")]
    public async Task<IActionResult> RemoveFacility(Guid id, Guid facilityId, CancellationToken cancellationToken)
    {
        await _preschoolProfileService.RemoveFacilityAsync(id, facilityId, cancellationToken);
        return NoContent();
    }
}
