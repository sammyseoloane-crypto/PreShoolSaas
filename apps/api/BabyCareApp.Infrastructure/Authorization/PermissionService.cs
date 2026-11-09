using BabyCareApp.Application.Authorization;
using BabyCareApp.Domain.Enums;
using BabyCareApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BabyCareApp.Infrastructure.Authorization;

public class PermissionService : IPermissionService
{
    private readonly ApplicationDbContext _db;

    public PermissionService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId, Guid tenantId, string permissionKey, CancellationToken cancellationToken = default)
    {
        var permissions = await GetEffectivePermissionsAsync(userId, tenantId, cancellationToken);
        return permissions.Contains(permissionKey, StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(
        Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var memberships = await _db.TenantMemberships
            .Include(m => m.Role).ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .Where(m => m.UserId == userId && m.TenantId == tenantId && m.Status == MembershipStatus.Active)
            .ToListAsync(cancellationToken);

        return memberships
            .SelectMany(m => m.Role.RolePermissions.Select(rp => rp.Permission.Key))
            .Distinct()
            .ToList();
    }
}
