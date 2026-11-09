using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BabyCareApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPreschoolProfileAndDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "tenants",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "tenants",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "tenants",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "tenants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "tenants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "branches",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "branches",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "branches",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "branches",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "branch_facilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_facilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_branch_facilities_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branch_operating_hours",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    OpenTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    CloseTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branch_operating_hours", x => x.Id);
                    table.ForeignKey(
                        name: "FK_branch_operating_hours_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "school_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_school_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_school_documents_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_school_documents_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branch_facilities_BranchId",
                table: "branch_facilities",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_branch_facilities_TenantId",
                table: "branch_facilities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_branch_operating_hours_BranchId_DayOfWeek",
                table: "branch_operating_hours",
                columns: new[] { "BranchId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_branch_operating_hours_TenantId",
                table: "branch_operating_hours",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_school_documents_BranchId",
                table: "school_documents",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_school_documents_TenantId",
                table: "school_documents",
                column: "TenantId");

            // RLS for the three new tenant-owned tables, with the admin bypass built in from
            // the start this time (migration AllowPlatformAdminRlsBypass added it retroactively
            // for the Slice 2/4 tables).
            foreach (var table in new[] { "branch_facilities", "branch_operating_hours", "school_documents" })
            {
                migrationBuilder.Sql($"ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;");
                migrationBuilder.Sql($"ALTER TABLE {table} FORCE ROW LEVEL SECURITY;");
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "branch_facilities", "branch_operating_hours", "school_documents" })
            {
                migrationBuilder.Sql($"DROP POLICY IF EXISTS tenant_isolation ON {table};");
            }

            migrationBuilder.DropTable(
                name: "branch_facilities");

            migrationBuilder.DropTable(
                name: "branch_operating_hours");

            migrationBuilder.DropTable(
                name: "school_documents");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "tenants");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "City",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "branches");
        }
    }
}
