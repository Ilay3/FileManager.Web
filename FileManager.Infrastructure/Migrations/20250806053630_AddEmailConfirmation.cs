using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationCode",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailConfirmed",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsEmailConfirmed",
                table: "users",
                column: "IsEmailConfirmed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_IsEmailConfirmed",
                table: "users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationCode",
                table: "users");

            migrationBuilder.DropColumn(
                name: "IsEmailConfirmed",
                table: "users");
        }
    }
}
