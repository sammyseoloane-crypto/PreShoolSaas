using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Exceptions;
using BabyCareApp.Application.Subscriptions;
using BabyCareApp.Domain.Entities;
using BabyCareApp.Domain.Enums;
using BabyCareApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BabyCareApp.Infrastructure.Subscriptions;

public class SubscriptionService : ISubscriptionService
{
    private static readonly SubscriptionStatus[] InEffectStatuses =
        [SubscriptionStatus.Trialing, SubscriptionStatus.Active, SubscriptionStatus.PastDue];

    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogger _auditLogger;

    public SubscriptionService(
        ApplicationDbContext db, ITenantContext tenantContext, ICurrentUserContext currentUserContext, IAuditLogger auditLogger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
        _auditLogger = auditLogger;
    }

    public async Task<SubscriptionResponse> SubscribeAsync(SubscribeRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var alreadyInEffect = await _db.Subscriptions
            .AnyAsync(s => s.TenantId == tenantId && InEffectStatuses.Contains(s.Status), cancellationToken);
        if (alreadyInEffect)
        {
            throw new ConflictException(
                "This tenant already has a subscription in effect. Cancel it before starting a new one.");
        }

        var plan = await _db.SubscriptionPlans.FindAsync([request.PlanId], cancellationToken)
            ?? throw new NotFoundException("Subscription plan not found.");
        if (!plan.IsActive)
        {
            throw new ConflictException("This subscription plan is not currently available.");
        }

        var now = DateTimeOffset.UtcNow;
        var trialEnd = now.AddDays(plan.TrialDays);

        var subscription = new Subscription
        {
            TenantId = tenantId,
            PlanId = plan.Id,
            Status = SubscriptionStatus.Trialing,
            BillingCycle = request.BillingCycle,
            TrialEndsAt = trialEnd,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = trialEnd,
        };

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "subscription.started", nameof(Subscription), subscription.Id, tenantId, _currentUserContext.UserId,
            metadata: new { plan.Name, request.BillingCycle, trialEnd }, cancellationToken);

        return ToResponse(subscription, plan);
    }

    public async Task<SubscriptionResponse> GetCurrentSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var subscription = await FindInEffectSubscriptionAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException("This tenant has not subscribed to a plan yet.");

        await ExpireIfTrialLapsedAsync(subscription, cancellationToken);

        return ToResponse(subscription, subscription.Plan);
    }

    public async Task<SubscriptionResponse> ChangePlanAsync(ChangePlanRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var subscription = await FindInEffectSubscriptionAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException("This tenant has not subscribed to a plan yet.");

        await ExpireIfTrialLapsedAsync(subscription, cancellationToken);
        if (subscription.Status == SubscriptionStatus.Expired)
        {
            throw new ConflictException("This tenant's trial has expired — subscribe to a plan again to continue.");
        }

        var newPlan = await _db.SubscriptionPlans.FindAsync([request.PlanId], cancellationToken)
            ?? throw new NotFoundException("Subscription plan not found.");
        if (!newPlan.IsActive)
        {
            throw new ConflictException("This subscription plan is not currently available.");
        }

        var previousPlanId = subscription.PlanId;
        subscription.PlanId = newPlan.Id;
        subscription.BillingCycle = request.BillingCycle;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "subscription.plan_changed", nameof(Subscription), subscription.Id, tenantId, _currentUserContext.UserId,
            metadata: new { previousPlanId, newPlanId = newPlan.Id, request.BillingCycle }, cancellationToken);

        return ToResponse(subscription, newPlan);
    }

    public async Task CancelSubscriptionAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var subscription = await FindInEffectSubscriptionAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException("This tenant has not subscribed to a plan yet.");

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.CancelledAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "subscription.cancelled", nameof(Subscription), subscription.Id, tenantId, _currentUserContext.UserId,
            cancellationToken: cancellationToken);
    }

    public async Task<SubscriptionPlanLimits?> GetEffectiveLimitsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var subscription = await FindInEffectSubscriptionAsync(tenantId, cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        await ExpireIfTrialLapsedAsync(subscription, cancellationToken);
        if (subscription.Status == SubscriptionStatus.Expired)
        {
            return null;
        }

        return new SubscriptionPlanLimits(
            subscription.Plan.MaxLearners, subscription.Plan.MaxTeachers, subscription.Plan.MaxBranches);
    }

    public async Task RecordPaymentSucceededAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var subscription = await FindInEffectSubscriptionAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException("This tenant has no subscription to confirm payment for.");

        var previousStatus = subscription.Status;
        subscription.Status = SubscriptionStatus.Active;

        var now = DateTimeOffset.UtcNow;
        var cycleLength = subscription.BillingCycle == BillingCycle.Annual ? TimeSpan.FromDays(365) : TimeSpan.FromDays(30);
        subscription.CurrentPeriodStart = now;
        subscription.CurrentPeriodEnd = now.Add(cycleLength);

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "subscription.payment_succeeded", nameof(Subscription), subscription.Id, tenantId, _currentUserContext.UserId,
            metadata: new { previousStatus }, cancellationToken);
    }

    public async Task RecordPaymentFailedAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var subscription = await FindInEffectSubscriptionAsync(tenantId, cancellationToken)
            ?? throw new NotFoundException("This tenant has no subscription to mark past due.");

        subscription.Status = SubscriptionStatus.PastDue;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "subscription.payment_failed", nameof(Subscription), subscription.Id, tenantId, _currentUserContext.UserId,
            cancellationToken: cancellationToken);
    }

    private async Task<Subscription?> FindInEffectSubscriptionAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _db.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.TenantId == tenantId && InEffectStatuses.Contains(s.Status))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// There is no background job infrastructure in this project yet, so trial expiry is
    /// evaluated lazily whenever a subscription is read or its limits are checked, rather
    /// than by a scheduled sweep. This is sufficient for correctness (nothing depends on the
    /// Expired transition happening at a precise instant) but means a trial that's expired
    /// and never looked at stays "Trialing" in the database until someone does look.
    /// </summary>
    private async Task ExpireIfTrialLapsedAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        if (subscription.Status == SubscriptionStatus.Trialing
            && subscription.TrialEndsAt is DateTimeOffset trialEndsAt
            && trialEndsAt <= DateTimeOffset.UtcNow)
        {
            subscription.Status = SubscriptionStatus.Expired;
            await _db.SaveChangesAsync(cancellationToken);

            await _auditLogger.LogAsync(
                "subscription.trial_expired", nameof(Subscription), subscription.Id, subscription.TenantId, userId: null,
                cancellationToken: cancellationToken);
        }
    }

    private Guid RequireTenantId() =>
        _tenantContext.TenantId ?? throw new TenantRequiredException(
            "This operation requires a resolved tenant — send a valid X-Tenant-Id header for a tenant you belong to.");

    private static SubscriptionResponse ToResponse(Subscription subscription, SubscriptionPlan plan) => new(
        subscription.Id, plan.Id, plan.Name, subscription.Status, subscription.BillingCycle,
        subscription.TrialEndsAt, subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd, subscription.CancelledAt);
}
