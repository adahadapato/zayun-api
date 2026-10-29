using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zayun.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExperienceAreaCount",
                table: "Tags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProjectCount",
                table: "Tags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SearchKeys",
                table: "Tags",
                type: "nvarchar(1892)",
                maxLength: 1892,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SkillGroupCount",
                table: "Tags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tags_Name",
                table: "Tags",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tags_Name",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "ExperienceAreaCount",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "ProjectCount",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "SearchKeys",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "SkillGroupCount",
                table: "Tags");
        }
    }
}
