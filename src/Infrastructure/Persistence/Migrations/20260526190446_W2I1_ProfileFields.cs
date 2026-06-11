using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class W2I1_ProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bio",
                schema: "identity",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "handle",
                schema: "identity",
                table: "users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location",
                schema: "identity",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pronouns",
                schema: "identity",
                table: "users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "two_factor_enabled_at",
                schema: "identity",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_handle",
                schema: "identity",
                table: "users",
                column: "handle",
                unique: true,
                filter: "handle IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_handle",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "bio",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "handle",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "location",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "pronouns",
                schema: "identity",
                table: "users");

            migrationBuilder.DropColumn(
                name: "two_factor_enabled_at",
                schema: "identity",
                table: "users");
        }
    }
}
