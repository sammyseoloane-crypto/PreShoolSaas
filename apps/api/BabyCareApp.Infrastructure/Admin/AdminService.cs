using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Admin;
using BabyCareApp.Application.Common;
using BabyCareApp.Application.Exceptions;
using BabyCareApp.Domain.Entities;
using BabyCareApp.Domain.Enums;
using BabyCareApp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BabyCareApp.Infrastructure.Admin;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUserContext;

    public AdminService(ApplicationDbContext db, IAuditLogger auditLogger, ICurrentUserContext currentUserContext)
    {
        _db = db;
        _auditLogger = auditLogger;
        _currentUserContext = currentUserContext;
    }

    public async Task<PagedResult<AdminTenantResponse>> ListTenantsAsync(
        TenantListQuery query, CancellationToken cancellationToken = default)
    {
        var queryable = _db.Tenants.AsQueryable();
        if (query.Status is TenantStatus status)
        {
            queryable = queryable.Where(t => t.Status == status);
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var tenants = await queryable
            .OrderByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = new List<AdminTenantResponse>(tenants.Count);
        foreach (var tenant in tenants)
        {
            items.Add(await ToAdminResponseAsync(tenant, cancellationToken));
        }

        return new PagedResult<AdminTenantResponse>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<AdminTenantResponse> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.FindAsync([tenantId], cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        return await ToAdminResponseAsync(tenant, cancellationToken);
    }

    public async Task ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.FindAsync([tenantId], cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        var previousStatus = tenant.Status;
        tenant.Status = TenantStatus.Active;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "tenant.activated", nameof(Tenant), tenant.Id, tenant.Id, _currentUserContext.UserId,
            metadata: new { previousStatus }, cancellationToken);
    }

    public async Task SuspendTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.FindAsync([tenantId], cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        var previousStatus = tenant.Status;
        tenant.Status = TenantStatus.Suspended;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "tenant.suspended", nameof(Tenant), tenant.Id, tenant.Id, _currentUserContext.UserId,
            metadata: new { previousStatus }, cancellationToken);
    }

    public async Task<IReadOnlyList<SubscriptionPlanResponse>> ListSubscriptionPlansAsync(
        CancellationToken cancellationToken = default)
    {
        var plans = await _db.SubscriptionPlans
            .OrderBy(p => p.Tier)
            .ToListAsync(cancellationToken);

        return plans.Select(ToPlanResponse).ToList();
    }

    public async Task<SubscriptionPlanResponse> CreateSubscriptionPlanAsync(
        CreateSubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = new SubscriptionPlan
        {
            Name = request.Name.Trim(),
            Tier = request.Tier,
            MonthlyPrice = request.MonthlyPrice,
            AnnualPrice = request.AnnualPrice,
            MaxLearners = request.MaxLearners,
            MaxTeachers = request.MaxTeachers,
            MaxBranches = request.MaxBranches,
            FeatureFlags = request.FeatureFlags,
            IsActive = true,
        };

        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "subscription_plan.created", nameof(SubscriptionPlan), plan.Id, tenantId: null, _currentUserContext.UserId,
            metadata: new { plan.Name, plan.Tier }, cancellationToken);

        return ToPlanResponse(plan);
    }

    public async Task<SubscriptionPlanResponse> UpdateSubscriptionPlanAsync(
        Guid planId, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _db.SubscriptionPlans.FindAsync([planId], cancellationToken)
            ?? throw new NotFoundException("Subscription plan not found.");

        plan.Name = request.Name.Trim();
        plan.MonthlyPrice = request.MonthlyPrice;
        plan.AnnualPrice = request.AnnualPrice;
        plan.MaxLearners = request.MaxLearners;
        plan.MaxTeachers = request.MaxTeachers;
        plan.MaxBranches = request.MaxBranches;
        plan.FeatureFlags = request.FeatureFlags;
        plan.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "subscription_plan.updated", nameof(SubscriptionPlan), plan.Id, tenantId: null, _currentUserContext.UserId,
            cancellationToken: cancellationToken);

        return ToPlanResponse(plan);
    }

    public async Task<IReadOnlyList<SubscriptionResponse>> ListSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await _db.Subscriptions
            .Include(s => s.Tenant)
            .Include(s => s.Plan)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        return subscriptions
            .Select(s => new SubscriptionResponse(
                s.Id, s.TenantId, s.Tenant.Name, s.PlanId, s.Plan.Name,
                s.Status, s.BillingCycle, s.CurrentPeriodStart, s.CurrentPeriodEnd))
            .ToList();
    }

    public async Task<PagedResult<AuditLogResponse>> ListAuditLogsAsync(
        AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        var queryable = _db.AuditLogs.AsQueryable();

        if (query.TenantId is Guid tenantId)
        {
            queryable = queryable.Where(a => a.TenantId == tenantId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            queryable = queryable.Where(a => a.Action == query.Action);
        }

        if (query.From is DateTimeOffset from)
        {
            queryable = queryable.Where(a => a.CreatedAt >= from);
        }

        if (query.To is DateTimeOffset to)
        {
            queryable = queryable.Where(a => a.CreatedAt <= to);
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .OrderByDescending(a => a.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AuditLogResponse(
                a.Id, a.TenantId, a.UserId, a.Action, a.EntityType, a.EntityId, a.MetadataJson, a.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogResponse>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<PlatformStatisticsResponse> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var tenantsByStatus = await _db.Tenants
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalTenants = tenantsByStatus.Sum(g => g.Count);
        var totalUsers = await _db.Users.CountAsync(cancellationToken);
        var totalBranches = await _db.Branches.CountAsync(cancellationToken);
        var totalActiveSubscriptions = await _db.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active, cancellationToken);

        return new PlatformStatisticsResponse(
            totalTenants,
            tenantsByStatus.ToDictionary(g => g.Status.ToString(), g => g.Count),
            totalUsers,
            totalBranches,
            totalActiveSubscriptions);
    }

    private async Task<AdminTenantResponse> ToAdminResponseAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var branchCount = await _db.Branches.CountAsync(b => b.TenantId == tenant.Id, cancellationToken);
        var membershipCount = await _db.TenantMemberships
            .CountAsync(m => m.TenantId == tenant.Id && m.Status == MembershipStatus.Active, cancellationToken);

        return new AdminTenantResponse(tenant.Id, tenant.Name, tenant.Slug, tenant.Status, tenant.CreatedAt, branchCount, membershipCount);
    }

    private static SubscriptionPlanResponse ToPlanResponse(SubscriptionPlan plan) => new(
        plan.Id, plan.Name, plan.Tier, plan.MonthlyPrice, plan.AnnualPrice,
        plan.MaxLearners, plan.MaxTeachers, plan.MaxBranches, plan.FeatureFlags, plan.IsActive);
}
