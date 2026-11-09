namespace BabyCareApp.Application.Abstractions;

/// <summary>
/// Scope note: this slice wires up audit logging for the tenant-lifecycle events the
/// Platform Admin dashboard needs to show (registered/activated/suspended). Extending
/// coverage to other modules is an ongoing concern for whichever slice adds them, not a
/// one-shot blanket task here.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string entityType,
        Guid? entityId,
        Guid? tenantId,
        Guid? userId,
        object? metadata = null,
        CancellationToken cancellationToken = default);
}
