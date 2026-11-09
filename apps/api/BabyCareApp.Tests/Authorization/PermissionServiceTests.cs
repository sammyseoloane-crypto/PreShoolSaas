using BabyCareApp.Domain.Entities;
using BabyCareApp.Domain.Enums;
using BabyCareApp.Infrastructure.Authorization;
using BabyCareApp.Infrastructure.Persistence;
using BabyCareApp.Infrastructure.Persistence.Configurations;
using BabyCareApp.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BabyCareApp.Tests.Authorization;

public class PermissionServiceTests
{
    private static (PermissionService Service, ApplicationDbContext Db) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new ApplicationDbContext(options, new FakeTenantContext());
        db.Database.EnsureCreated(); // applies HasData seed rows (system roles + permissions)

        return (new PermissionService(db), db);
    }

    private static async Task<(Guid TenantId, Guid UserId)> SeedMembershipAsync(
        ApplicationDbContext db, Guid roleId, MembershipStatus status = MembershipStatus.Active)
    {
        var tenant = new Tenant { Name = "Test School", Slug = $"test-school-{Guid.NewGuid():N}" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var userId = Guid.NewGuid();
        db.TenantMemberships.Add(new TenantMembership
        {
            TenantId = tenant.Id,
            UserId = userId,
            RoleId = roleId,
            Status = status,
        });
        await db.SaveChangesAsync();

        return (tenant.Id, userId);
    }

    [Fact]
    public async Task PreschoolOwner_HasFullReadWriteOnTenantsAndBranches()
    {
        var (service, db) = CreateService();
        var (tenantId, userId) = await SeedMembershipAsync(db, SystemRoles.PreschoolOwnerId);

        var permissions = await service.GetEffectivePermissionsAsync(userId, tenantId);

        Assert.Contains("tenants.read", permissions);
        Assert.Contains("tenants.write", permissions);
        Assert.Contains("branches.read", permissions);
        Assert.Contains("branches.write", permissions);
    }

    [Fact]
    public async Task SchoolAdministrator_HasReadOnly_NotWrite()
    {
        var (service, db) = CreateService();
        var (tenantId, userId) = await SeedMembershipAsync(db, SystemRoles.SchoolAdministratorId);

        var permissions = await service.GetEffectivePermissionsAsync(userId, tenantId);

        Assert.Contains("tenants.read", permissions);
        Assert.Contains("branches.read", permissions);
        Assert.DoesNotContain("tenants.write", permissions);
        Assert.DoesNotContain("branches.write", permissions);
    }

    [Fact]
    public async Task Teacher_HasNoneOfThesePermissions()
    {
        var (service, db) = CreateService();
        var (tenantId, userId) = await SeedMembershipAsync(db, SystemRoles.TeacherId);

        var permissions = await service.GetEffectivePermissionsAsync(userId, tenantId);

        Assert.Empty(permissions);
    }

    [Fact]
    public async Task HasPermission_PreschoolOwner_BranchesWrite_ReturnsTrue()
    {
        var (service, db) = CreateService();
        var (tenantId, userId) = await SeedMembershipAsync(db, SystemRoles.PreschoolOwnerId);

        Assert.True(await service.HasPermissionAsync(userId, tenantId, "branches.write"));
    }

    [Fact]
    public async Task HasPermission_Teacher_BranchesWrite_ReturnsFalse()
    {
        var (service, db) = CreateService();
        var (tenantId, userId) = await SeedMembershipAsync(db, SystemRoles.TeacherId);

        Assert.False(await service.HasPermissionAsync(userId, tenantId, "branches.write"));
    }

    [Fact]
    public async Task SuspendedMembership_GrantsNoPermissions()
    {
        var (service, db) = CreateService();
        var (tenantId, userId) = await SeedMembershipAsync(db, SystemRoles.PreschoolOwnerId, MembershipStatus.Suspended);

        var permissions = await service.GetEffectivePermissionsAsync(userId, tenantId);

        Assert.Empty(permissions);
    }

    [Fact]
    public async Task UnknownUserOrTenant_ReturnsNoPermissions()
    {
        var (service, _) = CreateService();

        var permissions = await service.GetEffectivePermissionsAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Empty(permissions);
    }
}
