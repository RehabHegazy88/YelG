using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YelG.WebSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InsightPostYouTube : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "YouTubeUrl",
                table: "InsightPosts",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YouTubeUrl",
                table: "InsightPosts");
        }
    }
}
