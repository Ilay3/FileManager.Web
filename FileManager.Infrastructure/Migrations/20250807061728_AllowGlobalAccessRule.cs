using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllowGlobalAccessRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessRules_FileOrFolder",
                table: "access_rules");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessRules_FileOrFolder",
                table: "access_rules",
                sql: "(\"FileId\" IS NOT NULL AND \"FolderId\" IS NULL) OR (\"FileId\" IS NULL AND \"FolderId\" IS NOT NULL) OR (\"FileId\" IS NULL AND \"FolderId\" IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AccessRules_FileOrFolder",
                table: "access_rules");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AccessRules_FileOrFolder",
                table: "access_rules",
                sql: "(\"FileId\" IS NOT NULL AND \"FolderId\" IS NULL) OR (\"FileId\" IS NULL AND \"FolderId\" IS NOT NULL)");
        }
    }
}
