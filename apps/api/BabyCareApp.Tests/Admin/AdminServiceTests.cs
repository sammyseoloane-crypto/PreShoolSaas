using BabyCareApp.Application.Admin;
using BabyCareApp.Application.Exceptions;
using BabyCareApp.Application.Tenancy;
using BabyCareApp.Domain.Entities;
using BabyCareApp.Domain.Enums;
using BabyCareApp.Infrastructure.Admin;
using BabyCareApp.Infrastructure.Audit;
using BabyCareApp.Infrastructure.Persistence;
using BabyCareApp.Infrastructure.Tenancy;
using BabyCareApp.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BabyCareApp.Tests.Admin;

public class AdminServiceTests
{
    private static (AdminService AdminService, TenantService TenantService, ApplicationDbContext Db, FakeCurrentUserContext CurrentUser)
        CreateServices()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new FakeTenantContext();
        var currentUser = new FakeCurrentUserContext();
        var db = new ApplicationDbContext(options, tenantContext);
        db.Database.EnsureCreated();

        var auditLogger = new AuditLogger(db);
        var tenantService = new TenantService(db, tenantContext, auditLogger);
        var adminService = new AdminService(db, auditLogger, currentUser);

        return (adminService, tenantService, db, currentUser);
    }

    [Fact]
    public async Task ListTenants_ReturnsAllAcrossTenants_WithCounts()
    {
        var (adminService, tenantService, _, _) = CreateServices();
        await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Tenant One" });
        await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Tenant Two" });

        var result = await adminService.ListTenantsAsync(new TenantListQuery());

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, t => Assert.Equal(1, t.BranchCount));
        Assert.All(result.Items, t => Assert.Equal(1, t.MembershipCount));
    }

    [Fact]
    public async Task ListTenants_FiltersByStatus()
    {
        var (adminService, tenantService, _, _) = CreateServices();
        var pending = await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Still Pending" });
        var toActivate = await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "To Activate" });
        await adminService.ActivateTenantAsync(toActivate.Id);

        var activeOnly = await adminService.ListTenantsAsync(new TenantListQuery { Status = TenantStatus.Active });

        var tenant = Assert.Single(activeOnly.Items);
        Assert.Equal(toActivate.Id, tenant.Id);
        Assert.NotEqual(pending.Id, tenant.Id);
    }

    [Fact]
    public async Task ListTenants_Paginates()
    {
        var (adminService, tenantService, _, _) = CreateServices();
        for (var i = 0; i < 5; i++)
        {
            await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = $"School {i}" });
        }

        var page1 = await adminService.ListTenantsAsync(new TenantListQuery { Page = 1, PageSize = 2 });
        var page2 = await adminService.ListTenantsAsync(new TenantListQuery { Page = 2, PageSize = 2 });

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(2, page2.Items.Count);
        Assert.DoesNotContain(page2.Items, t => page1.Items.Any(p => p.Id == t.Id));
    }

    [Fact]
    public async Task ActivateTenant_SetsStatus_AndWritesAuditLog()
    {
        var (adminService, tenantService, db, currentUser) = CreateServices();
        currentUser.UserId = Guid.NewGuid();
        var tenant = await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Pending School" });

        await adminService.ActivateTenantAsync(tenant.Id);

        var reloaded = await db.Tenants.SingleAsync(t => t.Id == tenant.Id);
        Assert.Equal(TenantStatus.Active, reloaded.Status);

        var auditEntry = await db.AuditLogs.SingleAsync(a => a.Action == "tenant.activated" && a.TenantId == tenant.Id);
        Assert.Equal(currentUser.UserId, auditEntry.UserId);
    }

    [Fact]
    public async Task SuspendTenant_SetsStatus_AndWritesAuditLog()
    {
        var (adminService, tenantService, db, _) = CreateServices();
        var tenant = await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Naughty School" });

        await adminService.SuspendTenantAsync(tenant.Id);

        var reloaded = await db.Tenants.SingleAsync(t => t.Id == tenant.Id);
        Assert.Equal(TenantStatus.Suspended, reloaded.Status);
        Assert.True(await db.AuditLogs.AnyAsync(a => a.Action == "tenant.suspended" && a.TenantId == tenant.Id));
    }

    [Fact]
    public async Task ActivateTenant_UnknownId_ThrowsNotFound()
    {
        var (adminService, _, _, _) = CreateServices();

        await Assert.ThrowsAsync<NotFoundException>(() => adminService.ActivateTenantAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateSubscriptionPlan_ThenList_ReturnsIt()
    {
        var (adminService, _, _, _) = CreateServices();

        var created = await adminService.CreateSubscriptionPlanAsync(new CreateSubscriptionPlanRequest
        {
            Name = "Standard",
            Tier = SubscriptionTier.Standard,
            MonthlyPrice = 499m,
            AnnualPrice = 4990m,
            MaxLearners = 100,
        });

        var plans = await adminService.ListSubscriptionPlansAsync();

        var plan = Assert.Single(plans);
        Assert.Equal(created.Id, plan.Id);
        Assert.Equal("Standard", plan.Name);
        Assert.Equal(100, plan.MaxLearners);
    }

    [Fact]
    public async Task UpdateSubscriptionPlan_ChangesFields()
    {
        var (adminService, _, _, _) = CreateServices();
        var created = await adminService.CreateSubscriptionPlanAsync(new CreateSubscriptionPlanRequest
        {
            Name = "Basic",
            Tier = SubscriptionTier.Basic,
            MonthlyPrice = 199m,
            AnnualPrice = 1990m,
        });

        var updated = await adminService.UpdateSubscriptionPlanAsync(created.Id, new UpdateSubscriptionPlanRequest
        {
            Name = "Basic Plus",
            MonthlyPrice = 249m,
            AnnualPrice = 2490m,
            IsActive = false,
        });

        Assert.Equal("Basic Plus", updated.Name);
        Assert.Equal(249m, updated.MonthlyPrice);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task ListSubscriptions_WithNoneCreatedYet_ReturnsEmpty()
    {
        var (adminService, _, _, _) = CreateServices();

        var subscriptions = await adminService.ListSubscriptionsAsync();

        Assert.Empty(subscriptions);
    }

    [Fact]
    public async Task ListSubscriptions_ReturnsTenantAndPlanNames()
    {
        var (adminService, tenantService, db, _) = CreateServices();
        var tenant = await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Subscribed School" });
        var plan = await adminService.CreateSubscriptionPlanAsync(new CreateSubscriptionPlanRequest
        {
            Name = "Premium",
            Tier = SubscriptionTier.Premium,
            MonthlyPrice = 999m,
            AnnualPrice = 9990m,
        });

        db.Subscriptions.Add(new Subscription
        {
            TenantId = tenant.Id,
            PlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            BillingCycle = BillingCycle.Monthly,
            CurrentPeriodStart = DateTimeOffset.UtcNow,
            CurrentPeriodEnd = DateTimeOffset.UtcNow.AddMonths(1),
        });
        await db.SaveChangesAsync();

        var subscriptions = await adminService.ListSubscriptionsAsync();

        var subscription = Assert.Single(subscriptions);
        Assert.Equal("Subscribed School", subscription.TenantName);
        Assert.Equal("Premium", subscription.PlanName);
    }

    [Fact]
    public async Task ListAuditLogs_FiltersByTenantAndAction()
    {
        var (adminService, tenantService, _, _) = CreateServices();
        var tenantA = await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "A" });
        var tenantB = await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "B" });
        await adminService.ActivateTenantAsync(tenantA.Id);

        var tenantAActivations = await adminService.ListAuditLogsAsync(new AuditLogQuery { TenantId = tenantA.Id, Action = "tenant.activated" });
        var tenantBLogs = await adminService.ListAuditLogsAsync(new AuditLogQuery { TenantId = tenantB.Id, Action = "tenant.activated" });

        Assert.Single(tenantAActivations.Items);
        Assert.Empty(tenantBLogs.Items);
    }

    [Fact]
    public async Task GetStatistics_CountsAcrossPlatform()
    {
        var (adminService, tenantService, _, _) = CreateServices();
        var tenant1 = await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Stat School 1" });
        await tenantService.RegisterTenantAsync(Guid.NewGuid(), new RegisterTenantRequest { Name = "Stat School 2" });
        await adminService.ActivateTenantAsync(tenant1.Id);

        var stats = await adminService.GetStatisticsAsync();

        Assert.Equal(2, stats.TotalTenants);
        Assert.Equal(1, stats.TenantsByStatus[nameof(TenantStatus.Active)]);
        Assert.Equal(1, stats.TenantsByStatus[nameof(TenantStatus.PendingVerification)]);
        Assert.Equal(2, stats.TotalBranches);
    }
}
