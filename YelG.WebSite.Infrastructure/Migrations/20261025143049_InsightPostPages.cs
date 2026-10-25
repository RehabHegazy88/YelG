using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YelG.WebSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InsightPostPages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Body_Ar",
                table: "InsightPosts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Body_En",
                table: "InsightPosts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "InsightPosts",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Summary_Ar",
                table: "InsightPosts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Summary_En",
                table: "InsightPosts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "VideoId",
                table: "InsightPosts",
                type: "bigint",
                nullable: true);

            // the posts already there get an address before the unique index ("post-12"; editable in the admin panel)
            migrationBuilder.Sql("UPDATE [InsightPosts] SET [Slug] = CONCAT(N'post-', [Id]) WHERE [Slug] = N''");

            migrationBuilder.CreateIndex(
                name: "IX_InsightPosts_Slug",
                table: "InsightPosts",
                column: "Slug",
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_InsightPosts_VideoId",
                table: "InsightPosts",
                column: "VideoId");

            migrationBuilder.AddForeignKey(
                name: "FK_InsightPosts_MediaAssets_VideoId",
                table: "InsightPosts",
                column: "VideoId",
                principalTable: "MediaAssets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsightPosts_MediaAssets_VideoId",
                table: "InsightPosts");

            migrationBuilder.DropIndex(
                name: "IX_InsightPosts_Slug",
                table: "InsightPosts");

            migrationBuilder.DropIndex(
                name: "IX_InsightPosts_VideoId",
                table: "InsightPosts");

            migrationBuilder.DropColumn(
                name: "Body_Ar",
                table: "InsightPosts");

            migrationBuilder.DropColumn(
                name: "Body_En",
                table: "InsightPosts");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "InsightPosts");

            migrationBuilder.DropColumn(
                name: "Summary_Ar",
                table: "InsightPosts");

            migrationBuilder.DropColumn(
                name: "Summary_En",
                table: "InsightPosts");

            migrationBuilder.DropColumn(
                name: "VideoId",
                table: "InsightPosts");
        }
    }
}
