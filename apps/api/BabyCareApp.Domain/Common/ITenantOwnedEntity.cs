namespace BabyCareApp.Domain.Common;

/// <summary>
/// Marks an entity as tenant-owned so the DbContext's SaveChanges pipeline can assign
/// and validate <see cref="TenantId"/> from the ambient tenant context, as a second
/// layer of defense alongside PostgreSQL row-level security.
/// </summary>
public interface ITenantOwnedEntity
{
    Guid TenantId { get; set; }
}
