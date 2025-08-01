using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdeteUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastActivityAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastFailedLoginAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastIpAddress",
                table: "users",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockReason",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LockedById",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetAt",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetTokenExpires",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsLocked",
                table: "users",
                column: "IsLocked");

            migrationBuilder.CreateIndex(
                name: "IX_Users_LastActivityAt",
                table: "users",
                column: "LastActivityAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PasswordResetToken",
                table: "users",
                column: "PasswordResetToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsLocked",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_Users_LastActivityAt",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PasswordResetToken",
                table: "users");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastActivityAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastFailedLoginAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastIpAddress",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LockReason",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LockedById",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordResetAt",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "users");

            migrationBuilder.DropColumn(
                name: "PasswordResetTokenExpires",
                table: "users");
        }
    }
}
