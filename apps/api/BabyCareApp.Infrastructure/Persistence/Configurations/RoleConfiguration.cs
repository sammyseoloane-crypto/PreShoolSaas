using BabyCareApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BabyCareApp.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");

        builder.Property(r => r.Name).IsRequired().HasMaxLength(100);

        builder.HasOne(r => r.Tenant)
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.TenantId);

        builder.HasData(SystemRoles.All);
    }
}

public static class SystemRoles
{
    public static readonly Guid PlatformSuperAdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid PreschoolOwnerId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid SchoolAdministratorId = Guid.Parse("00000000-0000-0000-0000-000000000003");
    public static readonly Guid TeacherId = Guid.Parse("00000000-0000-0000-0000-000000000004");
    public static readonly Guid FinanceOfficerId = Guid.Parse("00000000-0000-0000-0000-000000000005");
    public static readonly Guid ParentGuardianId = Guid.Parse("00000000-0000-0000-0000-000000000006");

    private static readonly DateTimeOffset SeedTimestamp = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly Role[] All =
    [
        new() { Id = PlatformSuperAdminId, TenantId = null, Name = "Platform Super Admin", IsSystemRole = true, CreatedAt = SeedTimestamp },
        new() { Id = PreschoolOwnerId, TenantId = null, Name = "Preschool Owner", IsSystemRole = true, CreatedAt = SeedTimestamp },
        new() { Id = SchoolAdministratorId, TenantId = null, Name = "School Administrator", IsSystemRole = true, CreatedAt = SeedTimestamp },
        new() { Id = TeacherId, TenantId = null, Name = "Teacher", IsSystemRole = true, CreatedAt = SeedTimestamp },
        new() { Id = FinanceOfficerId, TenantId = null, Name = "Finance Officer", IsSystemRole = true, CreatedAt = SeedTimestamp },
        new() { Id = ParentGuardianId, TenantId = null, Name = "Parent/Guardian", IsSystemRole = true, CreatedAt = SeedTimestamp },
    ];
}
