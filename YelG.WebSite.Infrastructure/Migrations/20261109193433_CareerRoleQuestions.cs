using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YelG.WebSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CareerRoleQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerRoleQuestions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerRoleId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Hint_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Hint_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Options_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Options_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Text_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Text_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_CareerRoleQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerRoleQuestions_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerRoleQuestions_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerRoleQuestions_CareerRoles_CareerRoleId",
                        column: x => x.CareerRoleId,
                        principalTable: "CareerRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobApplicationAnswers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    CareerRoleQuestionId = table.Column<long>(type: "bigint", nullable: true),
                    Question = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApplicationAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobApplicationAnswers_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobApplicationAnswers_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobApplicationAnswers_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerRoleQuestions_AddedBy_PersonID",
                table: "CareerRoleQuestions",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_CareerRoleQuestions_CareerRoleId",
                table: "CareerRoleQuestions",
                column: "CareerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerRoleQuestions_ModifiedBy_PersonID",
                table: "CareerRoleQuestions",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplicationAnswers_AddedBy_PersonID",
                table: "JobApplicationAnswers",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplicationAnswers_CareerRoleQuestionId_JobApplicationId",
                table: "JobApplicationAnswers",
                columns: new[] { "CareerRoleQuestionId", "JobApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_JobApplicationAnswers_JobApplicationId",
                table: "JobApplicationAnswers",
                column: "JobApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplicationAnswers_ModifiedBy_PersonID",
                table: "JobApplicationAnswers",
                column: "ModifiedBy_PersonID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerRoleQuestions");

            migrationBuilder.DropTable(
                name: "JobApplicationAnswers");
        }
    }
}
