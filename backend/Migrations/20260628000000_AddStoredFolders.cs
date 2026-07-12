using System;
using backend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260628000000_AddStoredFolders")]
    public partial class AddStoredFolders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FolderId",
                table: "StoredFiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StoredFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentFolderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoredFolders_StoredFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "StoredFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StoredFolders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_FolderId",
                table: "StoredFiles",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFolders_ParentFolderId",
                table: "StoredFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFolders_UserId",
                table: "StoredFolders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFolders_UserId_ParentFolderId_Name",
                table: "StoredFolders",
                columns: new[] { "UserId", "ParentFolderId", "Name" });

            migrationBuilder.AddForeignKey(
                name: "FK_StoredFiles_StoredFolders_FolderId",
                table: "StoredFiles",
                column: "FolderId",
                principalTable: "StoredFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoredFiles_StoredFolders_FolderId",
                table: "StoredFiles");

            migrationBuilder.DropTable(
                name: "StoredFolders");

            migrationBuilder.DropIndex(
                name: "IX_StoredFiles_FolderId",
                table: "StoredFiles");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "StoredFiles");
        }
    }
}
