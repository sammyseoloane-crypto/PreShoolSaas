using BabyCareApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BabyCareApp.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(p => p.Key).IsRequired().HasMaxLength(150);
        builder.HasIndex(p => p.Key).IsUnique();

        builder.Property(p => p.Description).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Category).HasMaxLength(100);

        builder.HasData(SystemPermissions.All);
    }
}

/// <summary>
/// Grounded only in modules that actually exist (tenant settings, branch management —
/// Slice 4) plus what section 5 of the master spec describes those roles as responsible
/// for. Deliberately does NOT invent permissions for modules not yet built (learners,
/// finance, academics, ...) — those arrive with their own slices.
/// </summary>
public static class SystemPermissions
{
    public static readonly Guid TenantsRead = Guid.Parse("00000000-0000-0000-0000-000000000101");
    public static readonly Guid TenantsWrite = Guid.Parse("00000000-0000-0000-0000-000000000102");
    public static readonly Guid BranchesRead = Guid.Parse("00000000-0000-0000-0000-000000000103");
    public static readonly Guid BranchesWrite = Guid.Parse("00000000-0000-0000-0000-000000000104");

    public static readonly Permission[] All =
    [
        new() { Id = TenantsRead, Key = "tenants.read", Category = "Tenant", Description = "View the organisation's own tenant settings." },
        new() { Id = TenantsWrite, Key = "tenants.write", Category = "Tenant", Description = "Update the organisation's own tenant settings." },
        new() { Id = BranchesRead, Key = "branches.read", Category = "Tenant", Description = "View the organisation's branches." },
        new() { Id = BranchesWrite, Key = "branches.write", Category = "Tenant", Description = "Create and update the organisation's branches." },
    ];
}
