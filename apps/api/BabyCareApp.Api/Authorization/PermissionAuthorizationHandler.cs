using System.IdentityModel.Tokens.Jwt;
using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace BabyCareApp.Api.Authorization;

/// <summary>
/// Platform Super Admins bypass permission checks entirely (they operate across tenants,
/// not within the tenant-scoped role/permission model). Everyone else needs an active
/// membership, in the resolved tenant, whose role carries the required permission —
/// looked up fresh from the database every time, never trusted from the JWT.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly ITenantContext _tenantContext;

    public PermissionAuthorizationHandler(IPermissionService permissionService, ITenantContext tenantContext)
    {
        _permissionService = permissionService;
        _tenantContext = tenantContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.FindFirst("is_platform_super_admin")?.Value == bool.TrueString)
        {
            context.Succeed(requirement);
            return;
        }

        var subClaim = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (_tenantContext.TenantId is not Guid tenantId || !Guid.TryParse(subClaim, out var userId))
        {
            return;
        }

        if (await _permissionService.HasPermissionAsync(userId, tenantId, requirement.PermissionKey))
        {
            context.Succeed(requirement);
        }
    }
}
