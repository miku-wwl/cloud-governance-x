using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinOps.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizationAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "authorization_audit_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    permission = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    scope_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cloud_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_allowed = table.Column<bool>(type: "boolean", nullable: false),
                    is_high_privilege = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    actor_issuer = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    actor_subject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    http_method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    status_code = table.Column<int>(type: "integer", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_authorization_audit_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_authorization_audit_events_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_authorization_audit_events_high_privilege_occurred_at",
                table: "authorization_audit_events",
                columns: new[] { "is_high_privilege", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_authorization_audit_events_tenant_occurred_at",
                table: "authorization_audit_events",
                columns: new[] { "tenant_id", "occurred_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authorization_audit_events");
        }
    }
}
