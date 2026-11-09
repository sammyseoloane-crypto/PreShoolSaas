using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Authorization;
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
    private readonly IPermissionService _permissionService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ITenantContext _tenantContext;

    public TenantsController(
        ITenantService tenantService,
        IPermissionService permissionService,
        ICurrentUserContext currentUserContext,
        ITenantContext tenantContext)
    {
        _tenantService = tenantService;
        _permissionService = permissionService;
        _currentUserContext = currentUserContext;
        _tenantContext = tenantContext;
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
    [Authorize(Policy = "Permission:tenants.read")]
    public async Task<ActionResult<TenantResponse>> GetCurrent(CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetCurrentTenantAsync(cancellationToken);
        return Ok(tenant);
    }

    [HttpPut("current")]
    [Authorize(Policy = "Permission:tenants.write")]
    public async Task<ActionResult<TenantResponse>> UpdateCurrent(UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.UpdateCurrentTenantAsync(request, cancellationToken);
        return Ok(tenant);
    }

    /// <summary>
    /// The caller's effective permissions for the resolved tenant — for the frontend to
    /// fetch at login/tenant-switch and drive UI. Never the enforcement boundary itself;
    /// every protected endpoint re-checks via PermissionAuthorizationHandler regardless.
    /// </summary>
    [HttpGet("current/permissions")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCurrentPermissions(CancellationToken cancellationToken)
    {
        var userId = RequireCurrentUserId();
        var tenantId = _tenantContext.TenantId
            ?? throw new TenantRequiredException("Send a valid X-Tenant-Id header for a tenant you belong to.");

        var permissions = await _permissionService.GetEffectivePermissionsAsync(userId, tenantId, cancellationToken);
        return Ok(permissions);
    }

    private Guid RequireCurrentUserId() =>
        _currentUserContext.UserId ?? throw new AuthenticationException("No authenticated user resolved from the access token.");
}
