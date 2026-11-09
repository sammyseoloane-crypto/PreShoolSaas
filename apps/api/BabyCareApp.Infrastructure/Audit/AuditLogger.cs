using System.Text.Json;
using BabyCareApp.Application.Abstractions;
using BabyCareApp.Domain.Entities;
using BabyCareApp.Infrastructure.Persistence;

namespace BabyCareApp.Infrastructure.Audit;

public class AuditLogger : IAuditLogger
{
    private readonly ApplicationDbContext _db;

    public AuditLogger(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        Guid? entityId,
        Guid? tenantId,
        Guid? userId,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            TenantId = tenantId,
            UserId = userId,
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata),
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
