using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YelG.WebSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CareersPageHeading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PageHeading_Ar",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PageHeading_En",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PageLead_Ar",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PageLead_En",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PageHeading_Ar",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "PageHeading_En",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "PageLead_Ar",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "PageLead_En",
                table: "CareersSections");
        }
    }
}
