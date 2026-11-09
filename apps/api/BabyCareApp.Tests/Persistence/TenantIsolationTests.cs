using BabyCareApp.Application.Exceptions;
using BabyCareApp.Domain.Entities;
using BabyCareApp.Domain.Enums;
using BabyCareApp.Infrastructure.Persistence;
using BabyCareApp.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BabyCareApp.Tests.Persistence;

/// <summary>
/// Covers the application-layer half of tenant isolation (ApplicationDbContext's
/// SaveChanges guard). The PostgreSQL row-level-security policies added in the
/// EnableRowLevelSecurity migration are the second, database-enforced layer and
/// require a live Postgres instance to verify — not exercisable against the
/// EF Core InMemory provider used here.
/// </summary>
public class TenantIsolationTests
{
    private static ApplicationDbContext CreateContext(FakeTenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, tenantContext);
    }

    [Fact]
    public async Task SaveChanges_CreatingTenantOwnedEntity_WithNoResolvedTenant_Throws()
    {
        var tenantContext = new FakeTenantContext { TenantId = null };
        await using var db = CreateContext(tenantContext);

        db.Branches.Add(new Branch { Name = "Little Stars Main Branch" });

        await Assert.ThrowsAsync<TenantMismatchException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_CreatingTenantOwnedEntity_WithResolvedTenant_AssignsTenantIdAutomatically()
    {
        var tenantId = Guid.NewGuid();
        var tenantContext = new FakeTenantContext { TenantId = tenantId };
        await using var db = CreateContext(tenantContext);

        var branch = new Branch { Name = "Little Stars Main Branch" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        Assert.Equal(tenantId, branch.TenantId);
    }

    [Fact]
    public async Task SaveChanges_CreatingTenantOwnedEntity_ForAnotherTenant_Throws()
    {
        var resolvedTenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var tenantContext = new FakeTenantContext { TenantId = resolvedTenantId };
        await using var db = CreateContext(tenantContext);

        db.Branches.Add(new Branch { Name = "Cross-tenant attempt", TenantId = otherTenantId });

        await Assert.ThrowsAsync<TenantMismatchException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_ModifyingEntity_FromAnotherResolvedTenant_Throws()
    {
        var ownerTenantId = Guid.NewGuid();
        var tenantContext = new FakeTenantContext { TenantId = ownerTenantId };

        Guid branchId;
        await using (var seedDb = CreateContext(tenantContext))
        {
            var branch = new Branch { Name = "Original" };
            seedDb.Branches.Add(branch);
            await seedDb.SaveChangesAsync();
            branchId = branch.Id;
        }

        var attackerTenantId = Guid.NewGuid();
        tenantContext.TenantId = attackerTenantId;

        await using var db = CreateContext(tenantContext);
        // InMemory provider needs the dataset to be shared across contexts to simulate
        // a cross-request attempt; attach the known-good entity directly instead.
        var crossTenantBranch = new Branch { Id = branchId, TenantId = ownerTenantId, Name = "Tampered" };
        db.Attach(crossTenantBranch).State = EntityState.Modified;

        await Assert.ThrowsAsync<TenantMismatchException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_PlatformLevelEntity_WithNullTenant_DoesNotThrow()
    {
        var tenantContext = new FakeTenantContext { TenantId = null };
        await using var db = CreateContext(tenantContext);

        db.AuditLogs.Add(new AuditLog
        {
            TenantId = null,
            Action = "platform.tenant.suspended",
            EntityType = nameof(Tenant),
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var affected = await db.SaveChangesAsync();

        Assert.Equal(1, affected);
    }
}
