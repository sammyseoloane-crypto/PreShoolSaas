using System.Text;
using System.Text.RegularExpressions;
using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Exceptions;
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

    public TenantService(ApplicationDbContext db, ITenantContext tenantContext, IAuditLogger auditLogger)
    {
        _db = db;
        _tenantContext = tenantContext;
        _auditLogger = auditLogger;
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
        await _db.SaveChangesAsync(cancellationToken);

        return ToResponse(tenant);
    }

    public async Task<IReadOnlyList<BranchResponse>> ListBranchesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        return await _db.Branches
            .Where(b => b.TenantId == tenantId)
            .Select(b => new BranchResponse(b.Id, b.Name, b.IsMainBranch, b.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<BranchResponse> CreateBranchAsync(
        CreateBranchRequest request, CancellationToken cancellationToken = default)
    {
        RequireTenantId();

        var branch = new Branch { Name = request.Name.Trim(), IsActive = true };
        _db.Branches.Add(branch);
        await _db.SaveChangesAsync(cancellationToken);

        return new BranchResponse(branch.Id, branch.Name, branch.IsMainBranch, branch.IsActive);
    }

    public async Task<BranchResponse> GetBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var branch = await FindOwnedBranchAsync(branchId, cancellationToken);
        return new BranchResponse(branch.Id, branch.Name, branch.IsMainBranch, branch.IsActive);
    }

    public async Task<BranchResponse> UpdateBranchAsync(
        Guid branchId, UpdateBranchRequest request, CancellationToken cancellationToken = default)
    {
        var branch = await FindOwnedBranchAsync(branchId, cancellationToken);

        branch.Name = request.Name.Trim();
        branch.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        return new BranchResponse(branch.Id, branch.Name, branch.IsMainBranch, branch.IsActive);
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

    private static TenantResponse ToResponse(Tenant tenant) =>
        new(tenant.Id, tenant.Name, tenant.Slug, tenant.Status);

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
