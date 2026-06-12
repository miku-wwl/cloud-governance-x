using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCloudResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cloud_resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    account_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    resource_id = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    resource_id_normalized = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    resource_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    region = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    resource_group = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    tags_json = table.Column<string>(type: "jsonb", nullable: false),
                    first_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_seen_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cloud_resources", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_cloud_resources_provider_resource_id",
                table: "cloud_resources",
                columns: new[] { "provider", "resource_id_normalized" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cloud_resources");
        }
    }
}
