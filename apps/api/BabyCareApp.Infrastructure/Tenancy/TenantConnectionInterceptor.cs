using System.Data.Common;
using BabyCareApp.Application.Abstractions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BabyCareApp.Infrastructure.Tenancy;

/// <summary>
/// Sets the `app.tenant_id` session variable on every connection open, for the
/// lifetime of the scoped DbContext's request — this is what PostgreSQL RLS
/// policies (migration EnableRowLevelSecurity) actually check. Runs on every
/// logical Open(), so a pooled physical connection reused across requests never
/// carries a stale tenant id forward.
/// </summary>
public class TenantConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantConnectionInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = CreateSetTenantCommand(connection);
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection, ConnectionEndEventData eventData, CancellationToken cancellationToken = default)
    {
        await using var command = CreateSetTenantCommand(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateSetTenantCommand(DbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = "SELECT set_config('app.tenant_id', @tenantId, false);";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "tenantId";
        parameter.Value = _tenantContext.TenantId?.ToString() ?? string.Empty;
        command.Parameters.Add(parameter);

        return command;
    }
}
