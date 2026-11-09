namespace BabyCareApp.Application.Admin;

public record AuditLogResponse(
    Guid Id,
    Guid? TenantId,
    Guid? UserId,
    string Action,
    string EntityType,
    Guid? EntityId,
    string? MetadataJson,
    DateTimeOffset CreatedAt);
