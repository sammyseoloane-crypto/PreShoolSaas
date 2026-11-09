using BabyCareApp.Application.Exceptions;
using BabyCareApp.Application.Subscriptions;
using BabyCareApp.Domain.Entities;
using BabyCareApp.Domain.Enums;
using BabyCareApp.Infrastructure.Audit;
using BabyCareApp.Infrastructure.Persistence;
using BabyCareApp.Infrastructure.Subscriptions;
using BabyCareApp.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BabyCareApp.Tests.Subscriptions;

public class SubscriptionServiceTests
{
    private static (SubscriptionService Service, ApplicationDbContext Db, FakeTenantContext TenantContext) CreateService()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var tenantContext = new FakeTenantContext { TenantId = Guid.NewGuid() };
        var currentUser = new FakeCurrentUserContext { UserId = Guid.NewGuid() };
        var db = new ApplicationDbContext(options, tenantContext);
        db.Database.EnsureCreated();

        var service = new SubscriptionService(db, tenantContext, currentUser, new AuditLogger(db));
        return (service, db, tenantContext);
    }

    private static async Task<SubscriptionPlan> SeedPlanAsync(
        ApplicationDbContext db, int trialDays = 14, int? maxBranches = null, bool isActive = true)
    {
        var plan = new SubscriptionPlan
        {
            Name = "Standard", Tier = SubscriptionTier.Standard, TrialDays = trialDays,
            MaxBranches = maxBranches, IsActive = isActive,
        };
        db.SubscriptionPlans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    [Fact]
    public async Task Subscribe_StartsTrialingSubscription_WithPlanTrialLength()
    {
        var (service, db, _) = CreateService();
        var plan = await SeedPlanAsync(db, trialDays: 21);

        var subscription = await service.SubscribeAsync(new SubscribeRequest { PlanId = plan.Id, BillingCycle = BillingCycle.Monthly });

        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
        Assert.NotNull(subscription.TrialEndsAt);
        Assert.True(subscription.TrialEndsAt > DateTimeOffset.UtcNow.AddDays(20));
    }

    [Fact]
    public async Task Subscribe_WhenAlreadyInEffect_ThrowsConflict()
    {
        var (service, db, _) = CreateService();
        var plan = await SeedPlanAsync(db);
        await service.SubscribeAsync(new SubscribeRequest { PlanId = plan.Id, BillingCycle = BillingCycle.Monthly });

        await Assert.ThrowsAsync<ConflictException>(
            () => service.SubscribeAsync(new SubscribeRequest { PlanId = plan.Id, BillingCycle = BillingCycle.Monthly }));
    }

    [Fact]
    public async Task Subscribe_ToInactivePlan_ThrowsConflict()
    {
        var (service, db, _) = CreateService();
        var plan = await SeedPlanAsync(db, isActive: false);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.SubscribeAsync(new SubscribeRequest { PlanId = plan.Id, BillingCycle = BillingCycle.Monthly }));
    }

    [Fact]
    public async Task Subscribe_ToUnknownPlan_ThrowsNotFound()
    {
        var (service, _, _) = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.SubscribeAsync(new SubscribeRequest { PlanId = Guid.NewGuid(), BillingCycle = BillingCycle.Monthly }));
    }

    [Fact]
    public async Task GetCurrentSubscription_WithNoneSubscribed_ThrowsNotFound()
    {
        var (service, _, _) = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetCurrentSubscriptionAsync());
    }

    [Fact]
    public async Task GetCurrentSubscription_LapsedTrial_TransitionsToExpired()
    {
        var (service, db, tenantContext) = CreateService();
        var plan = await SeedPlanAsync(db);
        var subscription = await service.SubscribeAsync(new SubscribeRequest { PlanId = plan.Id, BillingCycle = BillingCycle.Monthly });

        // Simulate the trial having already lapsed.
        var entity = await db.Subscriptions.SingleAsync(s => s.Id == subscription.Id);
        entity.TrialEndsAt = DateTimeOffset.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        // GetCurrentSubscriptionAsync still returns it — showing the true Expired state is
        // more useful to the tenant than a bare 404; ChangePlanAsync (tested separately)
        // is where an expired trial actually blocks further action.
        var current = await service.GetCurrentSubscriptionAsync();
        Assert.Equal(SubscriptionStatus.Expired, current.Status);

        var reloaded = await db.Subscriptions.SingleAsync(s => s.Id == subscription.Id);
        Assert.Equal(SubscriptionStatus.Expired, reloaded.Status);
    }

    [Fact]
    public async Task ChangePlan_Succeeds_AndUpdatesBillingCycle()
    {
        var (service, db, _) = CreateService();
        var plan1 = await SeedPlanAsync(db);
        var plan2 = await SeedPlanAsync(db);
        await service.SubscribeAsync(new SubscribeRequest { PlanId = plan1.Id, BillingCycle = BillingCycle.Monthly });

        var updated = await service.ChangePlanAsync(new ChangePlanRequest { PlanId = plan2.Id, BillingCycle = BillingCycle.Annual });

        Assert.Equal(plan2.Id, updated.PlanId);
        Assert.Equal(BillingCycle.Annual, updated.BillingCycle);
    }

    [Fact]
    public async Task ChangePlan_WithNoSubscription_ThrowsNotFound()
    {
        var (service, db, _) = CreateService();
        var plan = await SeedPlanAsync(db);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.ChangePlanAsync(new ChangePlanRequest { PlanId = plan.Id, BillingCycle = BillingCycle.Monthly }));
    }

    [Fact]
    public async Task CancelSubscription_SetsStatusAndCancelledAt()
    {
        var (service, db, _) = CreateService();
        var plan = await SeedPlanAsync(db);
        var subscription = await service.SubscribeAsync(new SubscribeRequest { PlanId = plan.Id, BillingCycle = BillingCycle.Monthly });

        await service.CancelSubscriptionAsync();

        var reloaded = await db.Subscriptions.SingleAsync(s => s.Id == subscription.Id);
        Assert.Equal(SubscriptionStatus.Cancelled, reloaded.Status);
        Assert.NotNull(reloaded.CancelledAt);
    }

    [Fact]
    public async Task GetEffectiveLimits_WithNoSubscription_ReturnsNull()
    {
        var (service, _, tenantContext) = CreateService();

        var limits = await service.GetEffectiveLimitsAsync(tenantContext.TenantId!.Value);

        Assert.Null(limits);
    }

    [Fact]
    public async Task GetEffectiveLimits_WithTrialingSubscription_ReturnsPlanLimits()
    {
        var (service, db, tenantContext) = CreateService();
        var plan = await SeedPlanAsync(db, maxBranches: 3);
        await service.SubscribeAsync(new SubscribeRequest { PlanId = plan.Id, BillingCycle = BillingCycle.Monthly });

        var limits = await service.GetEffectiveLimitsAsync(tenantContext.TenantId!.Value);

        Assert.Equal(3, limits?.MaxBranches);
    }

    [Fact]
    public async Task GetEffectiveLimits_WithLapsedTrial_ReturnsNull()
    {
        var (service, db, tenantContext) = CreateService();
        var plan = await SeedPlanAsync(db, maxBranches: 3);
        var subscription = await service.SubscribeAsync(new SubscribeRequest { PlanId = plan.Id, BillingCycle = BillingCycle.Monthly });

        var entity = await db.Subscriptions.SingleAsync(s => s.Id == subscription.Id);
        entity.TrialEndsAt = DateTimeOffset.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();

        var limits = await service.GetEffectiveLimitsAsync(tenantContext.TenantId!.Value);

        Assert.Null(limits);
    }

    [Fact]
    public async Task RecordPaymentSucceeded_TransitionsToActive_AndAdvancesPeriod()
    {
        var (service, db, tenantContext) = CreateService();
        var plan = await SeedPlanAsync(db);
        await service.SubscribeAsync(new SubscribeRequest { PlanId = plan.Id, BillingCycle = BillingCycle.Monthly });

        await service.RecordPaymentSucceededAsync(tenantContext.TenantId!.Value);

        var current = await service.GetCurrentSubscriptionAsync();
        Assert.Equal(SubscriptionStatus.Active, current.Status);
        Assert.True(current.CurrentPeriodEnd > DateTimeOffset.UtcNow.AddDays(25));
    }

    [Fact]
    public async Task RecordPaymentFailed_TransitionsToPastDue()
    {
        var (service, db, tenantContext) = CreateService();
        var plan = await SeedPlanAsync(db);
        await service.SubscribeAsync(new SubscribeRequest { PlanId = plan.Id, BillingCycle = BillingCycle.Monthly });
        await service.RecordPaymentSucceededAsync(tenantContext.TenantId!.Value);

        await service.RecordPaymentFailedAsync(tenantContext.TenantId!.Value);

        var current = await service.GetCurrentSubscriptionAsync();
        Assert.Equal(SubscriptionStatus.PastDue, current.Status);
    }
}
