using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BabyCareApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnableRowLevelSecurity : Migration
    {
        // Tables where tenant_id is required — a missing session tenant blocks all rows.
        private static readonly string[] StrictTenantTables = { "branches", "tenant_memberships", "subscriptions" };

        // Tables where tenant_id is nullable (platform-level rows pass through).
        private static readonly string[] NullableTenantTables = { "roles", "audit_logs" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in StrictTenantTables)
            {
                migrationBuilder.Sql($"ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"ALTER TABLE {table} FORCE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($@"
                    CREATE POLICY tenant_isolation ON {table}
                    USING (
                        current_setting('app.tenant_id', true) IS NOT NULL
                        AND current_setting('app.tenant_id', true) <> ''
                        AND ""TenantId"" = current_setting('app.tenant_id', true)::uuid
                    );");
            }

            foreach (var table in NullableTenantTables)
            {
                migrationBuilder.Sql($"ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"ALTER TABLE {table} FORCE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($@"
                    CREATE POLICY tenant_isolation ON {table}
                    USING (
                        ""TenantId"" IS NULL
                        OR (
                            current_setting('app.tenant_id', true) IS NOT NULL
                            AND current_setting('app.tenant_id', true) <> ''
                            AND ""TenantId"" = current_setting('app.tenant_id', true)::uuid
                        )
                    );");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in StrictTenantTables.Concat(NullableTenantTables))
            {
                migrationBuilder.Sql($"DROP POLICY IF EXISTS tenant_isolation ON {table};");
                migrationBuilder.Sql($"ALTER TABLE {table} NO FORCE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"ALTER TABLE {table} DISABLE ROW LEVEL SECURITY;");
            }
        }
    }
}
