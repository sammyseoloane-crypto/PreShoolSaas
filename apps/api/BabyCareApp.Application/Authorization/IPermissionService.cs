namespace BabyCareApp.Application.Authorization;

public interface IPermissionService
{
    Task<bool> HasPermissionAsync(Guid userId, Guid tenantId, string permissionKey, CancellationToken cancellationToken = default);

    /// <summary>The caller's full effective permission set for that tenant — e.g. for the
    /// frontend to fetch at login/tenant-switch and drive UI, never as an enforcement boundary.</summary>
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);
}
