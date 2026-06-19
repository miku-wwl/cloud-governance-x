using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyTenantBackfillControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE legacy_tenant_backfill_control
                (
                    operation_name text PRIMARY KEY,
                    tenant_id uuid NOT NULL,
                    completed_at timestamp with time zone NOT NULL,
                    resource_rows bigint NOT NULL,
                    cost_rows bigint NOT NULL,
                    etl_run_rows bigint NOT NULL
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM legacy_tenant_backfill_control
                        WHERE operation_name =
                            'day24-development-tenant-backfill'
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot roll back tenant-aware schema after Day24 legacy Tenant backfill. Restore the pre-backfill database recovery point instead.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.Sql(
                "DROP TABLE legacy_tenant_backfill_control;");
        }
    }
}
