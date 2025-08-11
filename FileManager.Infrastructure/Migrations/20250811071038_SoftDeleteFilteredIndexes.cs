using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteFilteredIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Folders_YandexPath",
                table: "folders");

            migrationBuilder.DropIndex(
                name: "IX_Files_YandexPath",
                table: "files");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_YandexPath",
                table: "folders",
                column: "YandexPath",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Files_YandexPath",
                table: "files",
                column: "YandexPath",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Folders_YandexPath",
                table: "folders");

            migrationBuilder.DropIndex(
                name: "IX_Files_YandexPath",
                table: "files");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_YandexPath",
                table: "folders",
                column: "YandexPath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Files_YandexPath",
                table: "files",
                column: "YandexPath",
                unique: true);
        }
    }
}
