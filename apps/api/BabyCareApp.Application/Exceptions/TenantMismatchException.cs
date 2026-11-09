namespace BabyCareApp.Application.Exceptions;

/// <summary>
/// Thrown when code attempts to persist a tenant-owned row for a tenant other than
/// the one resolved for the current request — the application-layer half of tenant
/// isolation, backed by PostgreSQL row-level security as the second layer.
/// </summary>
public class TenantMismatchException : Exception
{
    public TenantMismatchException(string message) : base(message)
    {
    }
}
