using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantAwareCoreData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_etl_job_runs_job_name_started_at",
                table: "etl_job_runs");

            migrationBuilder.DropIndex(
                name: "ux_cloud_resources_provider_resource_id",
                table: "cloud_resources");

            migrationBuilder.DropIndex(
                name: "ux_cloud_cost_daily_identity",
                table: "cloud_cost_daily");

            migrationBuilder.DropIndex(
                name: "ux_cloud_accounts_tenant_provider_external",
                table: "cloud_accounts");

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "etl_job_runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "cloud_resources",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_id",
                table: "cloud_cost_daily",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_cloud_accounts_tenant_provider_external",
                table: "cloud_accounts",
                columns: new[] { "tenant_id", "provider", "external_account_id" });

            migrationBuilder.CreateIndex(
                name: "ix_etl_job_runs_tenant_job_name_started_at",
                table: "etl_job_runs",
                columns: new[] { "tenant_id", "job_name", "started_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_cloud_resources_tenant_id_provider_account_id",
                table: "cloud_resources",
                columns: new[] { "tenant_id", "provider", "account_id" });

            migrationBuilder.CreateIndex(
                name: "ux_cloud_resources_legacy_provider_resource_id",
                table: "cloud_resources",
                columns: new[] { "provider", "resource_id_normalized" },
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_cloud_resources_tenant_provider_resource_id",
                table: "cloud_resources",
                columns: new[] { "tenant_id", "provider", "resource_id_normalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_cloud_cost_daily_legacy_identity",
                table: "cloud_cost_daily",
                columns: new[] { "provider", "account_id", "usage_date", "service_name", "resource_group", "currency" },
                unique: true,
                filter: "tenant_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_cloud_cost_daily_tenant_identity",
                table: "cloud_cost_daily",
                columns: new[] { "tenant_id", "provider", "account_id", "usage_date", "service_name", "resource_group", "currency" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_cloud_cost_daily_tenants_tenant_id",
                table: "cloud_cost_daily",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cloud_cost_daily_cloud_account_scope",
                table: "cloud_cost_daily",
                columns: new[] { "tenant_id", "provider", "account_id" },
                principalTable: "cloud_accounts",
                principalColumns: new[] { "tenant_id", "provider", "external_account_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_cloud_resources_tenants_tenant_id",
                table: "cloud_resources",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_cloud_resources_cloud_account_scope",
                table: "cloud_resources",
                columns: new[] { "tenant_id", "provider", "account_id" },
                principalTable: "cloud_accounts",
                principalColumns: new[] { "tenant_id", "provider", "external_account_id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_etl_job_runs_tenants_tenant_id",
                table: "etl_job_runs",
                column: "tenant_id",
                principalTable: "tenants",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cloud_cost_daily_tenants_tenant_id",
                table: "cloud_cost_daily");

            migrationBuilder.DropForeignKey(
                name: "fk_cloud_cost_daily_cloud_account_scope",
                table: "cloud_cost_daily");

            migrationBuilder.DropForeignKey(
                name: "FK_cloud_resources_tenants_tenant_id",
                table: "cloud_resources");

            migrationBuilder.DropForeignKey(
                name: "fk_cloud_resources_cloud_account_scope",
                table: "cloud_resources");

            migrationBuilder.DropForeignKey(
                name: "FK_etl_job_runs_tenants_tenant_id",
                table: "etl_job_runs");

            migrationBuilder.DropIndex(
                name: "ix_etl_job_runs_tenant_job_name_started_at",
                table: "etl_job_runs");

            migrationBuilder.DropIndex(
                name: "IX_cloud_resources_tenant_id_provider_account_id",
                table: "cloud_resources");

            migrationBuilder.DropIndex(
                name: "ux_cloud_resources_legacy_provider_resource_id",
                table: "cloud_resources");

            migrationBuilder.DropIndex(
                name: "ux_cloud_resources_tenant_provider_resource_id",
                table: "cloud_resources");

            migrationBuilder.DropIndex(
                name: "ux_cloud_cost_daily_legacy_identity",
                table: "cloud_cost_daily");

            migrationBuilder.DropIndex(
                name: "ux_cloud_cost_daily_tenant_identity",
                table: "cloud_cost_daily");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_cloud_accounts_tenant_provider_external",
                table: "cloud_accounts");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "etl_job_runs");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "cloud_resources");

            migrationBuilder.DropColumn(
                name: "tenant_id",
                table: "cloud_cost_daily");

            migrationBuilder.CreateIndex(
                name: "ix_etl_job_runs_job_name_started_at",
                table: "etl_job_runs",
                columns: new[] { "job_name", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_cloud_resources_provider_resource_id",
                table: "cloud_resources",
                columns: new[] { "provider", "resource_id_normalized" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_cloud_cost_daily_identity",
                table: "cloud_cost_daily",
                columns: new[] { "provider", "account_id", "usage_date", "service_name", "resource_group", "currency" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_cloud_accounts_tenant_provider_external",
                table: "cloud_accounts",
                columns: new[] { "tenant_id", "provider", "external_account_id" },
                unique: true);
        }
    }
}
