using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260705000000_AddStarredFilesAndFolders")]
    public partial class AddStarredFilesAndFolders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStarred",
                table: "StoredFolders",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsStarred",
                table: "StoredFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_StoredFolders_UserId_IsStarred",
                table: "StoredFolders",
                columns: new[] { "UserId", "IsStarred" });

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_UserId_IsStarred",
                table: "StoredFiles",
                columns: new[] { "UserId", "IsStarred" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StoredFolders_UserId_IsStarred",
                table: "StoredFolders");

            migrationBuilder.DropIndex(
                name: "IX_StoredFiles_UserId_IsStarred",
                table: "StoredFiles");

            migrationBuilder.DropColumn(
                name: "IsStarred",
                table: "StoredFolders");

            migrationBuilder.DropColumn(
                name: "IsStarred",
                table: "StoredFiles");
        }
    }
}
