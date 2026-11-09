using BabyCareApp.Application.Abstractions;

namespace BabyCareApp.Infrastructure.Tenancy;

/// <summary>
/// Placeholder registered until Slice 4 wires up the real HTTP-request-scoped tenant
/// resolution middleware. Always resolves to no tenant.
/// </summary>
public class NullTenantContext : ITenantContext
{
    public Guid? TenantId => null;
}
