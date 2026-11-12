using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zayun.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Animation_ContentType",
                table: "Quotes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Animation_FileSizeBytes",
                table: "Quotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Animation_Hash",
                table: "Quotes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Animation_Path",
                table: "Quotes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PictureAlt",
                table: "Quotes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Picture_ContentType",
                table: "Quotes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Picture_Dimensions_Height",
                table: "Quotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Picture_Dimensions_Width",
                table: "Quotes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Picture_FileSizeBytes",
                table: "Quotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Picture_Hash",
                table: "Quotes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Picture_Path",
                table: "Quotes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Picture_ThumbnailPath",
                table: "Quotes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Animation_ContentType",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Animation_FileSizeBytes",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Animation_Hash",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Animation_Path",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "PictureAlt",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Picture_ContentType",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Picture_Dimensions_Height",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Picture_Dimensions_Width",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Picture_FileSizeBytes",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Picture_Hash",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Picture_Path",
                table: "Quotes");

            migrationBuilder.DropColumn(
                name: "Picture_ThumbnailPath",
                table: "Quotes");
        }
    }
}
