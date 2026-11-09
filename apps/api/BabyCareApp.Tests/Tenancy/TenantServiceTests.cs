using BabyCareApp.Application.Exceptions;
using BabyCareApp.Application.Subscriptions;
using BabyCareApp.Application.Tenancy;
using BabyCareApp.Domain.Enums;
using BabyCareApp.Infrastructure.Audit;
using BabyCareApp.Infrastructure.Persistence;
using BabyCareApp.Infrastructure.Persistence.Configurations;
using BabyCareApp.Infrastructure.Subscriptions;
using BabyCareApp.Infrastructure.Tenancy;
using BabyCareApp.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BabyCareApp.Tests.Tenancy;

public class TenantServiceTests
{
    private static (TenantService Service, ApplicationDbContext Db, FakeTenantContext TenantContext, SubscriptionService SubscriptionService)
        CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new FakeTenantContext();
        var currentUser = new FakeCurrentUserContext();
        var db = new ApplicationDbContext(options, tenantContext);
        db.Database.EnsureCreated(); // applies HasData seed rows (e.g. system roles) — not automatic on InMemory
        var auditLogger = new AuditLogger(db);
        var subscriptionService = new SubscriptionService(db, tenantContext, currentUser, auditLogger);
        var service = new TenantService(db, tenantContext, auditLogger, subscriptionService);

        return (service, db, tenantContext, subscriptionService);
    }

    [Fact]
    public async Task RegisterTenant_CreatesTenantMainBranchAndOwnerMembership()
    {
        var (service, db, _, _) = CreateService();
        var ownerId = Guid.NewGuid();

        var tenant = await service.RegisterTenantAsync(ownerId, new RegisterTenantRequest { Name = "Little Stars Preschool" });

        Assert.Equal("little-stars-preschool", tenant.Slug);
        Assert.Equal(TenantStatus.PendingVerification, tenant.Status);

        var branch = await db.Branches.SingleAsync(b => b.TenantId == tenant.Id);
        Assert.True(branch.IsMainBranch);
        Assert.Equal("Main Branch", branch.Name);

        var membership = await db.TenantMemberships.SingleAsync(m => m.TenantId == tenant.Id && m.UserId == ownerId);
        Assert.Equal(SystemRoles.PreschoolOwnerId, membership.RoleId);
        Assert.Equal(MembershipStatus.Active, membership.Status);
    }

    [Fact]
    public async Task RegisterTenant_DuplicateName_GetsUniqueSuffixedSlug()
    {
        var (service, _, _, _) = CreateService();

        var first = await service.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Happy Kids Academy" });
        var second = await service.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Happy Kids Academy" });

        Assert.Equal("happy-kids-academy", first.Slug);
        Assert.Equal("happy-kids-academy-2", second.Slug);
    }

    [Fact]
    public async Task ListMyTenants_ReturnsOnlyActiveMembershipsForThatUser()
    {
        var (service, _, _, _) = CreateService();
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();

        await service.RegisterTenantAsync(ownerId, new RegisterTenantRequest { Name = "Bright Future ECD" });
        await service.RegisterTenantAsync(otherOwnerId, new RegisterTenantRequest { Name = "Another Preschool" });

        var mine = await service.ListMyTenantsAsync(ownerId);

        var tenant = Assert.Single(mine);
        Assert.Equal("Bright Future ECD", tenant.Name);
        Assert.Equal("Preschool Owner", tenant.RoleName);
    }

    [Fact]
    public async Task GetCurrentTenant_WithNoResolvedTenant_ThrowsTenantRequired()
    {
        var (service, _, tenantContext, _) = CreateService();
        tenantContext.TenantId = null;

        await Assert.ThrowsAsync<TenantRequiredException>(() => service.GetCurrentTenantAsync());
    }

    [Fact]
    public async Task GetCurrentTenant_WithResolvedTenant_ReturnsIt()
    {
        var (service, _, tenantContext, _) = CreateService();
        var tenant = await service.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Sunshine Daycare" });
        tenantContext.TenantId = tenant.Id;

        var current = await service.GetCurrentTenantAsync();

        Assert.Equal(tenant.Id, current.Id);
    }

    [Fact]
    public async Task UpdateCurrentTenant_ChangesName()
    {
        var (service, _, tenantContext, _) = CreateService();
        var tenant = await service.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Old Name" });
        tenantContext.TenantId = tenant.Id;

        var updated = await service.UpdateCurrentTenantAsync(new UpdateTenantRequest { Name = "New Name" });

        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task ListBranches_WithNoResolvedTenant_ThrowsTenantRequired()
    {
        var (service, _, tenantContext, _) = CreateService();
        tenantContext.TenantId = null;

        await Assert.ThrowsAsync<TenantRequiredException>(() => service.ListBranchesAsync());
    }

    [Fact]
    public async Task CreateBranch_AddsToResolvedTenant_AndIsNotMainBranch()
    {
        var (service, _, tenantContext, _) = CreateService();
        var tenant = await service.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Multi Branch School" });
        tenantContext.TenantId = tenant.Id;

        var branch = await service.CreateBranchAsync(new CreateBranchRequest { Name = "North Branch" });

        Assert.False(branch.IsMainBranch);
        var branches = await service.ListBranchesAsync();
        Assert.Equal(2, branches.Count); // main branch (from registration) + this one
    }

    [Fact]
    public async Task GetBranch_FromAnotherTenant_ThrowsNotFound_NotLeaked()
    {
        var (service, _, tenantContext, _) = CreateService();
        var tenantA = await service.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Tenant A" });
        var tenantB = await service.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Tenant B" });

        tenantContext.TenantId = tenantA.Id;
        var tenantAMainBranch = (await service.ListBranchesAsync()).Single();

        // Switch to tenant B's context and try to fetch tenant A's branch by its real id.
        tenantContext.TenantId = tenantB.Id;

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetBranchAsync(tenantAMainBranch.Id));
    }

    [Fact]
    public async Task UpdateBranch_ChangesNameAndActiveFlag()
    {
        var (service, _, tenantContext, _) = CreateService();
        var tenant = await service.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Editable School" });
        tenantContext.TenantId = tenant.Id;
        var mainBranch = (await service.ListBranchesAsync()).Single();

        var updated = await service.UpdateBranchAsync(mainBranch.Id, new UpdateBranchRequest { Name = "Renamed Branch", IsActive = false });

        Assert.Equal("Renamed Branch", updated.Name);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task CreateBranch_WithoutSubscription_IsUnlimited()
    {
        // Preserves pre-Slice-7 behavior for tenants that haven't subscribed to a plan yet.
        var (service, db, tenantContext, _) = CreateService();
        var tenant = await service.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "No Plan School" });
        tenantContext.TenantId = tenant.Id;

        for (var i = 0; i < 5; i++)
        {
            await service.CreateBranchAsync(new CreateBranchRequest { Name = $"Branch {i}" });
        }

        Assert.Equal(6, await db.Branches.CountAsync(b => b.TenantId == tenant.Id)); // 5 + the main branch
    }

    [Fact]
    public async Task CreateBranch_AtPlanLimit_ThrowsUsageLimitExceeded()
    {
        var (service, db, tenantContext, subscriptionService) = CreateService();
        var tenant = await service.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Capped School" });
        tenantContext.TenantId = tenant.Id;

        var plan = new BabyCareApp.Domain.Entities.SubscriptionPlan
        {
            Name = "Basic", Tier = SubscriptionTier.Basic, MaxBranches = 1,
        };
        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync();
        await subscriptionService.SubscribeAsync(new SubscribeRequest { PlanId = plan.Id, BillingCycle = BillingCycle.Monthly });

        // The main branch from registration already counts as 1 — at the limit.
        await Assert.ThrowsAsync<UsageLimitExceededException>(
            () => service.CreateBranchAsync(new CreateBranchRequest { Name = "One Too Many" }));
    }
}
