using System.Text;
using System.Text.RegularExpressions;
using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Exceptions;
using BabyCareApp.Application.Subscriptions;
using BabyCareApp.Application.Tenancy;
using BabyCareApp.Domain.Entities;
using BabyCareApp.Domain.Enums;
using BabyCareApp.Infrastructure.Persistence;
using BabyCareApp.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BabyCareApp.Infrastructure.Tenancy;

public partial class TenantService : ITenantService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IAuditLogger _auditLogger;
    private readonly ISubscriptionService _subscriptionService;

    public TenantService(
        ApplicationDbContext db, ITenantContext tenantContext, IAuditLogger auditLogger, ISubscriptionService subscriptionService)
    {
        _db = db;
        _tenantContext = tenantContext;
        _auditLogger = auditLogger;
        _subscriptionService = subscriptionService;
    }

    public async Task<TenantResponse> RegisterTenantAsync(
        Guid ownerUserId, RegisterTenantRequest request, CancellationToken cancellationToken = default)
    {
        var slug = await ResolveUniqueSlugAsync(request.Slug ?? request.Name, cancellationToken);

        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            Slug = slug,
            Status = TenantStatus.PendingVerification,
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(cancellationToken);

        // No tenant context is resolvable yet (the tenant didn't exist a moment ago), so the
        // RLS policies' WITH CHECK would otherwise reject these inserts. See
        // ApplicationDbContext.SetSessionTenantIdAsync for why this is safe and necessary.
        await _db.SetSessionTenantIdAsync(tenant.Id, cancellationToken);

        _db.Branches.Add(new Branch
        {
            TenantId = tenant.Id,
            Name = "Main Branch",
            IsMainBranch = true,
            IsActive = true,
        });

        _db.TenantMemberships.Add(new TenantMembership
        {
            TenantId = tenant.Id,
            UserId = ownerUserId,
            RoleId = SystemRoles.PreschoolOwnerId,
            Status = MembershipStatus.Active,
        });

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogger.LogAsync(
            "tenant.registered", nameof(Tenant), tenant.Id, tenant.Id, ownerUserId,
            metadata: new { tenant.Name, tenant.Slug }, cancellationToken);

        return ToResponse(tenant);
    }

    public async Task<IReadOnlyList<TenantSummaryResponse>> ListMyTenantsAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var memberships = await _db.TenantMemberships
            .Include(m => m.Tenant)
            .Include(m => m.Role)
            .Where(m => m.UserId == userId && m.Status == MembershipStatus.Active)
            .ToListAsync(cancellationToken);

        return memberships
            .Select(m => new TenantSummaryResponse(m.Tenant.Id, m.Tenant.Name, m.Tenant.Slug, m.Tenant.Status, m.Role.Name))
            .ToList();
    }

    public async Task<TenantResponse> GetCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.FindAsync([RequireTenantId()], cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        return ToResponse(tenant);
    }

    public async Task<TenantResponse> UpdateCurrentTenantAsync(
        UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await _db.Tenants.FindAsync([RequireTenantId()], cancellationToken)
            ?? throw new NotFoundException("Tenant not found.");

        tenant.Name = request.Name.Trim();
        tenant.Description = request.Description?.Trim();
        tenant.ContactEmail = request.ContactEmail?.Trim();
        tenant.ContactPhone = request.ContactPhone?.Trim();
        tenant.Website = request.Website?.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(tenant);
    }

    public async Task<IReadOnlyList<BranchResponse>> ListBranchesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var branches = await _db.Branches
            .Where(b => b.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return branches.Select(ToBranchResponse).ToList();
    }

    public async Task<BranchResponse> CreateBranchAsync(
        CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        // Null means no Trialing/Active/PastDue subscription is in effect — preserves
        // pre-Slice-7 behavior (unlimited) for tenants that haven't subscribed to a plan yet.
        var limits = await _subscriptionService.GetEffectiveLimitsAsync(tenantId, cancellationToken);
        if (limits?.MaxBranches is int maxBranches)
        {
            var currentCount = await _db.Branches.CountAsync(b => b.TenantId == tenantId, cancellationToken);
            if (currentCount >= maxBranches)
            {
                throw new UsageLimitExceededException(
                    $"Your current subscription plan allows up to {maxBranches} branches. Upgrade to add more.");
            }
        }

        var branch = new Branch
        {
            Name = request.Name.Trim(),
            IsActive = true,
            AddressLine = request.AddressLine?.Trim(),
            City = request.City?.Trim(),
            Province = request.Province?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            Country = request.Country?.Trim() ?? "South Africa",
            ContactEmail = request.ContactEmail?.Trim(),
            ContactPhone = request.ContactPhone?.Trim(),
        };
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync(cancellationToken);

        return ToBranchResponse(branch);
    }

    public async Task<BranchResponse> GetBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var branch = await FindOwnedBranchAsync(branchId, cancellationToken);
        return ToBranchResponse(branch);
    }

    public async Task<BranchResponse> UpdateBranchAsync(
        Guid branchId, UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await FindOwnedBranchAsync(branchId, cancellationToken);

        branch.Name = request.Name.Trim();
        branch.IsActive = request.IsActive;
        branch.AddressLine = request.AddressLine?.Trim();
        branch.City = request.City?.Trim();
        branch.Province = request.Province?.Trim();
        branch.PostalCode = request.PostalCode?.Trim();
        branch.Country = request.Country?.Trim();
        branch.ContactEmail = request.ContactEmail?.Trim();
        branch.ContactPhone = request.ContactPhone?.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        return ToBranchResponse(branch);
    }

    /// <summary>
    /// Looks the branch up scoped to the resolved tenant, so a guessed id belonging to
    /// another tenant returns the same NotFoundException as a nonexistent id — this is
    /// explicit defense-in-depth alongside RLS, not a replacement for it, and it's what
    /// makes cross-tenant access actually testable against the InMemory provider (which
    /// doesn't run PostgreSQL's row-level security at all).
    /// </summary>
    private async Task<Branch> FindOwnedBranchAsync(Guid branchId, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();

        return await _db.Branches.SingleOrDefaultAsync(b => b.Id == branchId && b.TenantId == tenantId, cancellationToken)
            ?? throw new NotFoundException("Branch not found.");
    }

    private Guid RequireTenantId() =>
        _tenantContext.TenantId ?? throw new TenantRequiredException(
            "This operation requires a resolved tenant — send a valid X-Tenant-Id header for a tenant you belong to.");

    private static TenantResponse ToResponse(Tenant tenant) => new(
        tenant.Id, tenant.Name, tenant.Slug, tenant.Status,
        tenant.Description, tenant.ContactEmail, tenant.ContactPhone, tenant.Website, tenant.LogoUrl);

    private static BranchResponse ToBranchResponse(Branch branch) => new(
        branch.Id, branch.Name, branch.IsMainBranch, branch.IsActive,
        branch.AddressLine, branch.City, branch.Province, branch.PostalCode, branch.Country,
        branch.ContactEmail, branch.ContactPhone);

    private async Task<string> ResolveUniqueSlugAsync(string source, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify(source);
        var candidate = baseSlug;
        var suffix = 1;

        while (await _db.Tenants.AnyAsync(t => t.Slug == candidate, cancellationToken))
        {
            suffix++;
            candidate = $"{baseSlug}-{suffix}";
        }

        return candidate;
    }

    private static string Slugify(string value)
    {
        var lowercase = value.Trim().ToLowerInvariant();
        var withoutDiacritics = RemoveDiacritics(lowercase);
        var slug = NonAlphanumericRegex().Replace(withoutDiacritics, "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "preschool" : slug;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRegex();
}
