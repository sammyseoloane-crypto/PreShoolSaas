namespace BabyCareApp.Application.Abstractions;

/// <summary>
/// Resolved per-request by tenant resolution middleware (Slice 4) from a validated
/// membership lookup — never trusted directly from client-supplied input. Null when
/// no tenant has been resolved for the current request (e.g. platform admin routes).
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
}
