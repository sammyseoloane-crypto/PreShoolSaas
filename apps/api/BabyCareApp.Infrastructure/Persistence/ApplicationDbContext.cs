using BabyCareApp.Application.Abstractions;
using BabyCareApp.Application.Exceptions;
using BabyCareApp.Domain.Common;
using BabyCareApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BabyCareApp.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditingAndTenantRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditingAndTenantRules();
        return base.SaveChanges();
    }

    private void ApplyAuditingAndTenantRules()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ITenantOwnedEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId == Guid.Empty)
                {
                    if (_tenantContext.TenantId is null)
                    {
                        throw new TenantMismatchException(
                            $"Cannot create a {entry.Entity.GetType().Name} without a resolved tenant context.");
                    }

                    entry.Entity.TenantId = _tenantContext.TenantId.Value;
                }
                else if (_tenantContext.TenantId is Guid resolvedTenantId && entry.Entity.TenantId != resolvedTenantId)
                {
                    throw new TenantMismatchException(
                        $"Attempted to create a {entry.Entity.GetType().Name} for tenant {entry.Entity.TenantId} " +
                        $"while the resolved request tenant is {resolvedTenantId}.");
                }
            }
            else if (entry.State == EntityState.Modified
                     && _tenantContext.TenantId is Guid currentTenantId
                     && entry.Entity.TenantId != currentTenantId)
            {
                throw new TenantMismatchException(
                    $"Attempted to modify a {entry.Entity.GetType().Name} belonging to tenant " +
                    $"{entry.Entity.TenantId} from a request resolved to tenant {currentTenantId}.");
            }
        }
    }
}
