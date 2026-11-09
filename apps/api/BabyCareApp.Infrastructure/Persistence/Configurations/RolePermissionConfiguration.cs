using BabyCareApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BabyCareApp.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(SystemRolePermissions.All);
    }
}

/// <summary>
/// Initial grants per section 5 of the master spec. Preschool Owner "manages their
/// organisation" and "manages branches" -> full read/write on both. School Administrator
/// "manages daily school operations" -> read-only visibility, not tenant/branch-settings
/// authority (not described as having that in section 5). Teacher, Finance Officer, and
/// Parent/Guardian get none of these — their own permissions arrive with the modules
/// section 5 actually describes them working in (academics, fees, parent portal).
/// Platform Super Admin needs none here either: PermissionAuthorizationHandler bypasses
/// permission checks entirely for that role via the is_platform_super_admin claim.
/// </summary>
public static class SystemRolePermissions
{
    public static readonly RolePermission[] All =
    [
        new() { RoleId = SystemRoles.PreschoolOwnerId, PermissionId = SystemPermissions.TenantsRead },
        new() { RoleId = SystemRoles.PreschoolOwnerId, PermissionId = SystemPermissions.TenantsWrite },
        new() { RoleId = SystemRoles.PreschoolOwnerId, PermissionId = SystemPermissions.BranchesRead },
        new() { RoleId = SystemRoles.PreschoolOwnerId, PermissionId = SystemPermissions.BranchesWrite },
        new() { RoleId = SystemRoles.SchoolAdministratorId, PermissionId = SystemPermissions.TenantsRead },
        new() { RoleId = SystemRoles.SchoolAdministratorId, PermissionId = SystemPermissions.BranchesRead },
    ];
}
