using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zayun.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slot = table.Column<int>(type: "int", nullable: false),
                    Image_Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Image_ThumbnailPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Image_ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Image_FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Image_Dimensions_Width = table.Column<int>(type: "int", nullable: false),
                    Image_Dimensions_Height = table.Column<int>(type: "int", nullable: false),
                    Image_Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteImages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SiteImages_Slot",
                table: "SiteImages",
                column: "Slot",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteImages");
        }
    }
}
