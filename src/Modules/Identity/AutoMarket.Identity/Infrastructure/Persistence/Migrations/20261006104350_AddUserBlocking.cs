using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoMarket.Identity.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddUserBlocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "block_reason",
                schema: "identity",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "blocked_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "block_reason",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "blocked_at",
                schema: "identity",
                table: "users");
        }
    }
}
