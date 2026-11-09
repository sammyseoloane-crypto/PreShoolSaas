namespace BabyCareApp.Application.Exceptions;

/// <summary>Thrown when a tenant-scoped operation is called without a resolved X-Tenant-Id.</summary>
public class TenantRequiredException : Exception
{
    public TenantRequiredException(string message) : base(message)
    {
    }
}
