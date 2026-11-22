using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YelG.WebSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TeamSection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TeamPractices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IconKey = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_TeamPractices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamPractices_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamPractices_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatValue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CenterLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CenterLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PracticesLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PracticesLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    HideHeadingBreakOnMobile = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Credentials = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhotoId = table.Column<long>(type: "bigint", nullable: true),
                    LinkedInUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TeamPracticeId = table.Column<long>(type: "bigint", nullable: true),
                    Bio_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Bio_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMembers_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMembers_MediaAssets_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeamMembers_TeamPractices_TeamPracticeId",
                        column: x => x.TeamPracticeId,
                        principalTable: "TeamPractices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_AddedBy_PersonID",
                table: "TeamMembers",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_ModifiedBy_PersonID",
                table: "TeamMembers",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_PhotoId",
                table: "TeamMembers",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamPracticeId",
                table: "TeamMembers",
                column: "TeamPracticeId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamPractices_AddedBy_PersonID",
                table: "TeamPractices",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamPractices_ModifiedBy_PersonID",
                table: "TeamPractices",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSections_AddedBy_PersonID",
                table: "TeamSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_TeamSections_ModifiedBy_PersonID",
                table: "TeamSections",
                column: "ModifiedBy_PersonID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "TeamSections");

            migrationBuilder.DropTable(
                name: "TeamPractices");
        }
    }
}
