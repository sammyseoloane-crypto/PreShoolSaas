using System.Data.Common;
using BabyCareApp.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BabyCareApp.Infrastructure.Tenancy;

/// <summary>
/// Sets the `app.tenant_id` and `app.is_platform_super_admin` session variables on every
/// connection open, for the lifetime of the scoped DbContext's request — this is what the
/// PostgreSQL RLS policies (migrations EnableRowLevelSecurity, AllowPlatformAdminRlsBypass)
/// actually check. Runs on every logical Open(), so a pooled physical connection reused
/// across requests never carries a stale value forward.
///
/// The super-admin flag is set here — server-side, from the already-validated JWT claim —
/// never derived from anything the client can influence at the SQL level, so it's a real
/// bypass only for genuinely verified platform admins, not a general RLS hole.
/// </summary>
public class TenantConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserContext _currentUserContext;

    public TenantConnectionInterceptor(ITenantContext tenantContext, ICurrentUserContext currentUserContext)
    {
        _tenantContext = tenantContext;
        _currentUserContext = currentUserContext;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = CreateSetSessionVarsCommand(connection);
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var command = CreateSetSessionVarsCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateSetSessionVarsCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText =
            "SELECT set_config('app.tenant_id', @tenantId, false), " +
            "set_config('app.is_platform_super_admin', @isSuperAdmin, false);";

        var tenantIdParam = command.CreateParameter();
        tenantIdParam.ParameterName = "tenantId";
        tenantIdParam.Value = _tenantContext.TenantId?.ToString() ?? string.Empty;
        command.Parameters.Add(tenantIdParam);

        var isSuperAdminParam = command.CreateParameter();
        isSuperAdminParam.ParameterName = "isSuperAdmin";
        isSuperAdminParam.Value = _currentUserContext.IsPlatformSuperAdmin ? "true" : "false";
        command.Parameters.Add(isSuperAdminParam);

        return command;
    }
}
