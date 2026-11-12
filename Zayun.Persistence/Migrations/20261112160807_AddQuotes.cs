using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zayun.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Author = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Language = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Translation = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Series = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Story = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsSourceChecked = table.Column<bool>(type: "bit", nullable: false),
                    PinnedMonth = table.Column<int>(type: "int", nullable: true),
                    PinnedDay = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_IsPublished_DisplayOrder",
                table: "Quotes",
                columns: new[] { "IsPublished", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_PinnedMonth_PinnedDay",
                table: "Quotes",
                columns: new[] { "PinnedMonth", "PinnedDay" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Quotes");
        }
    }
}
