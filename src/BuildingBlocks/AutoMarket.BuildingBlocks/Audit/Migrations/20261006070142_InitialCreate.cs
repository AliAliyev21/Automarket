using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMarket.BuildingBlocks.Audit.Migrations
{
    /// <inheritdoc />
    internal partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    target_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inbox",
                schema: "audit",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox", x => new { x.message_id, x.consumer });
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    trace_parent = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_actor_id_occurred_at",
                schema: "audit",
                table: "audit_log",
                columns: new[] { "actor_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_event_type_occurred_at",
                schema: "audit",
                table: "audit_log",
                columns: new[] { "event_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_occurred_at",
                schema: "audit",
                table: "audit_log",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_occurred_at",
                schema: "audit",
                table: "outbox",
                column: "occurred_at",
                filter: "processed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_processed_at",
                schema: "audit",
                table: "outbox",
                column: "processed_at");

            // SEC-LOG-03, ARCHITECTURE §7.2: audit jurnalı dəyişdirilə və silinə bilməz. Retention job-u (1 il)
            // ayrıca DB rolu ilə işləyəcək və bu trigger-i həmin mərhələdə nəzərə alacaq
            migrationBuilder.Sql("""
                CREATE FUNCTION audit.prevent_audit_log_modification() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'audit.audit_log is append-only';
                END;
                $$;

                CREATE TRIGGER audit_log_append_only
                BEFORE UPDATE OR DELETE ON audit.audit_log
                FOR EACH ROW EXECUTE FUNCTION audit.prevent_audit_log_modification();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS audit_log_append_only ON audit.audit_log;
                DROP FUNCTION IF EXISTS audit.prevent_audit_log_modification();
                """);

            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "inbox",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "audit");
        }
    }
}
