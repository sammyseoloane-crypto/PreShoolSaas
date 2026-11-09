using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Exceptions;
using BabyCareApp.Application.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BabyCareApp.Api.Controllers;

[ApiController]
[Route("api/v1/tenants")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserContext _currentUserContext;

    public TenantsController(ITenantService tenantService, ICurrentUserContext currentUserContext)
    {
        _tenantService = tenantService;
        _currentUserContext = currentUserContext;
    }

    [HttpPost]
    public async Task<ActionResult<TenantResponse>> Register(RegisterTenantRequest request, CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        var tenant = await _tenantService.RegisterTenantAsync(userId, request, cancellationToken);
        return Ok(tenant);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<TenantSummaryResponse>>> ListMine(CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        var tenants = await _tenantService.ListMyTenantsAsync(userId, cancellationToken);
        return Ok(tenants);
    }

    [HttpGet("current")]
    public async Task<ActionResult<TenantResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetCurrentTenantAsync(cancellationToken);
        return Ok(tenant);
    }

    [HttpPut("current")]
    public async Task<ActionResult<TenantResponse>> UpdateCurrent(UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.UpdateCurrentTenantAsync(request, cancellationToken);
        return Ok(tenant);
    }

    private Guid RequireCurrentUserId() =>
        _currentUserContext.UserId ?? throw new AuthenticationException("No authenticated user resolved from the access token.");
}
