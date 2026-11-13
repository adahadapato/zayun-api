using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zayun.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteSlug : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Quotes",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_Slug",
                table: "Quotes",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0 AND [Slug] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Quotes_Slug",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Quotes");
        }
    }
}
