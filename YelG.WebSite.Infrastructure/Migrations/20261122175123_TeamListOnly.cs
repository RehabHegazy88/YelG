using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YelG.WebSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TeamListOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CenterLabel_Ar",
                table: "TeamSections");

            migrationBuilder.DropColumn(
                name: "CenterLabel_En",
                table: "TeamSections");

            migrationBuilder.DropColumn(
                name: "PracticesLabel_Ar",
                table: "TeamSections");

            migrationBuilder.DropColumn(
                name: "PracticesLabel_En",
                table: "TeamSections");

            migrationBuilder.DropColumn(
                name: "IconKey",
                table: "TeamPractices");

            migrationBuilder.DropColumn(
                name: "Text_Ar",
                table: "TeamPractices");

            migrationBuilder.DropColumn(
                name: "Text_En",
                table: "TeamPractices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CenterLabel_Ar",
                table: "TeamSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CenterLabel_En",
                table: "TeamSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PracticesLabel_Ar",
                table: "TeamSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PracticesLabel_En",
                table: "TeamSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IconKey",
                table: "TeamPractices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Text_Ar",
                table: "TeamPractices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Text_En",
                table: "TeamPractices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
