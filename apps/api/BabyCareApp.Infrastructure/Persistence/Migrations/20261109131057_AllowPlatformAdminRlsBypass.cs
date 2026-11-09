using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BabyCareApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowPlatformAdminRlsBypass : Migration
    {
        // Same table groupings as migration EnableRowLevelSecurity.
        private static readonly string[] StrictTenantTables = { "branches", "tenant_memberships", "subscriptions" };
        private static readonly string[] NullableTenantTables = { "roles", "audit_logs" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in StrictTenantTables)
            {
                migrationBuilder.Sql($"DROP POLICY tenant_isolation ON {table};");
                migrationBuilder.Sql($@"
                    CREATE POLICY tenant_isolation ON {table}
                    USING (
                        current_setting('app.is_platform_super_admin', true) = 'true'
                        OR (
                            current_setting('app.tenant_id', true) IS NOT NULL
                            AND current_setting('app.tenant_id', true) <> ''
                            AND ""TenantId"" = current_setting('app.tenant_id', true)::uuid
                        )
                    );");
            }

            foreach (var table in NullableTenantTables)
            {
                migrationBuilder.Sql($"DROP POLICY tenant_isolation ON {table};");
                migrationBuilder.Sql($@"
                    CREATE POLICY tenant_isolation ON {table}
                    USING (
                        current_setting('app.is_platform_super_admin', true) = 'true'
                        OR ""TenantId"" IS NULL
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
            foreach (var table in StrictTenantTables)
            {
                migrationBuilder.Sql($"DROP POLICY tenant_isolation ON {table};");
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
                migrationBuilder.Sql($"DROP POLICY tenant_isolation ON {table};");
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
    }
}
