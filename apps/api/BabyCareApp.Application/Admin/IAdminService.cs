using BabyCareApp.Application.Common;

namespace BabyCareApp.Application.Admin;

/// <summary>
/// Platform-level, cross-tenant operations — the Super Admin dashboard. Distinct from
/// ITenantService, which is a tenant's own self-service operations on itself. Every
/// implementation method here deliberately ignores the resolved ITenantContext (it reads
/// across all tenants) — the RLS bypass that makes this safe to do from the database's
/// perspective is migration AllowPlatformAdminRlsBypass, gated on a server-verified claim.
/// </summary>
public interface IAdminService
{
    Task<PagedResult<AdminTenantResponse>> ListTenantsAsync(TenantListQuery query, CancellationToken cancellationToken = default);

    Task<AdminTenantResponse> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Also serves as "school verification" when applied to a PendingVerification tenant.</summary>
    Task ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task SuspendTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPlanResponse>> ListSubscriptionPlansAsync(CancellationToken cancellationToken = default);

    Task<SubscriptionPlanResponse> CreateSubscriptionPlanAsync(CreateSubscriptionPlanRequest request, CancellationToken cancellationToken = default);

    Task<SubscriptionPlanResponse> UpdateSubscriptionPlanAsync(Guid planId, UpdateSubscriptionPlanRequest request, CancellationToken cancellationToken = default);

    /// <summary>Will be empty until Slice 7 (Subscription Engine) adds a flow that creates Subscription rows.</summary>
    Task<IReadOnlyList<SubscriptionResponse>> ListSubscriptionsAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<AuditLogResponse>> ListAuditLogsAsync(AuditLogQuery query, CancellationToken cancellationToken = default);

    Task<PlatformStatisticsResponse> GetStatisticsAsync(CancellationToken cancellationToken = default);
}
