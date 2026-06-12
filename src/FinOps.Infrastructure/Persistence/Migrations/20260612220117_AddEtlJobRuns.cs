using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEtlJobRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "etl_job_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    records_processed = table.Column<int>(type: "integer", nullable: false),
                    error_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etl_job_runs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_etl_job_runs_job_name_started_at",
                table: "etl_job_runs",
                columns: new[] { "job_name", "started_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "etl_job_runs");
        }
    }
}
