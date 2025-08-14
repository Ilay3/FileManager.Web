using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileManager.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddUniqueFileNameIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Files_FolderId_Name",
            table: "files",
            columns: new[] { "FolderId", "Name" },
            unique: true,
            filter: "\"IsDeleted\" = false");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Files_FolderId_Name",
            table: "files");
    }
}
