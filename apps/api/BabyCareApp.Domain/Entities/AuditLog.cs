namespace BabyCareApp.Domain.Entities;

/// <summary>
/// TenantId is nullable to allow platform-level events (e.g. a Super Admin suspending
/// a tenant) alongside tenant-scoped events.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Tenant? Tenant { get; set; }
}
