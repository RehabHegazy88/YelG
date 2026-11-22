using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YelG.WebSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WhySlides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WhySlides",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageId = table.Column<long>(type: "bigint", nullable: true),
                    ImageAlt_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageAlt_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Points_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Points_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Text_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Text_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhySlides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhySlides_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhySlides_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhySlides_MediaAssets_ImageId",
                        column: x => x.ImageId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WhySlides_AddedBy_PersonID",
                table: "WhySlides",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_WhySlides_ImageId",
                table: "WhySlides",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_WhySlides_ModifiedBy_PersonID",
                table: "WhySlides",
                column: "ModifiedBy_PersonID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WhySlides");
        }
    }
}
