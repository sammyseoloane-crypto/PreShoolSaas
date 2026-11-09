namespace BabyCareApp.Application.Tenancy;

public interface ITenantService
{
    /// <summary>
    /// Creates a new tenant, its first ("main") branch, and a TenantMembership assigning
    /// the registering user as Preschool Owner — all in one unit of work, since there is
    /// no tenant context to resolve until the tenant itself exists.
    /// </summary>
    Task<TenantResponse> RegisterTenantAsync(Guid ownerUserId, RegisterTenantRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TenantSummaryResponse>> ListMyTenantsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Resolves the tenant from the current request's ITenantContext.</summary>
    Task<TenantResponse> GetCurrentTenantAsync(CancellationToken cancellationToken = default);

    Task<TenantResponse> UpdateCurrentTenantAsync(UpdateTenantRequest request, CancellationToken cancellationToken = default);

    /// <summary>Platform Super Admin only — not gated here, the caller enforces the policy.</summary>
    Task ActivateTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task SuspendTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BranchResponse>> ListBranchesAsync(CancellationToken cancellationToken = default);

    Task<BranchResponse> CreateBranchAsync(CreateBranchRequest request, CancellationToken cancellationToken = default);

    Task<BranchResponse> GetBranchAsync(Guid branchId, CancellationToken cancellationToken = default);

    Task<BranchResponse> UpdateBranchAsync(Guid branchId, UpdateBranchRequest request, CancellationToken cancellationToken = default);
}
