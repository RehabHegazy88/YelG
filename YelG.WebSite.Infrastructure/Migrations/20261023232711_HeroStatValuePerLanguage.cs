using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YelG.WebSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HeroStatValuePerLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Value",
                table: "HeroStats",
                newName: "Value_En");

            migrationBuilder.AddColumn<string>(
                name: "Value_Ar",
                table: "HeroStats",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            // Existing rows keep their value in both languages; the design writes "+10" in Arabic and "10+" in English.
            migrationBuilder.Sql("UPDATE [HeroStats] SET [Value_Ar] = [Value_En]");
            migrationBuilder.Sql("UPDATE [HeroStats] SET [Value_En] = SUBSTRING([Value_En], 2, 19) + '+' WHERE [Value_En] LIKE '+%'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Value_Ar",
                table: "HeroStats");

            migrationBuilder.RenameColumn(
                name: "Value_En",
                table: "HeroStats",
                newName: "Value");
        }
    }
}
