using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudCostDaily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cloud_cost_daily",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    account_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    usage_date = table.Column<DateOnly>(type: "date", nullable: false),
                    service_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    resource_group = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    cost = table.Column<decimal>(type: "numeric(20,8)", precision: 20, scale: 8, nullable: false),
                    currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    raw_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cloud_cost_daily", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_cloud_cost_daily_identity",
                table: "cloud_cost_daily",
                columns: new[] { "provider", "account_id", "usage_date", "service_name", "resource_group", "currency" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cloud_cost_daily");
        }
    }
}
