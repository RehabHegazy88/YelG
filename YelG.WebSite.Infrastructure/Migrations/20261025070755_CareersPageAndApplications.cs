using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YelG.WebSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CareersPageAndApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplyConsentText_Ar",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplyConsentText_En",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplySuccessText_Ar",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplySuccessText_En",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplySuccessTitle_Ar",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplySuccessTitle_En",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplyTitle_Ar",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ApplyTitle_En",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NoOpeningsText_Ar",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NoOpeningsText_En",
                table: "CareersSections",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "ClosingOn",
                table: "CareerRoles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department_Ar",
                table: "CareerRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Department_En",
                table: "CareerRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description_Ar",
                table: "CareerRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description_En",
                table: "CareerRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EmploymentType",
                table: "CareerRoles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PostedOn",
                table: "CareerRoles",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Requirements_Ar",
                table: "CareerRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Requirements_En",
                table: "CareerRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Responsibilities_Ar",
                table: "CareerRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Responsibilities_En",
                table: "CareerRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "CareerRoles",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Summary_Ar",
                table: "CareerRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Summary_En",
                table: "CareerRoles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "JobApplications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerRoleId = table.Column<long>(type: "bigint", nullable: true),
                    RoleTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Culture = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    LinkedInUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    CvFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    CvStoredPath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CvContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CvSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ConsentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsentText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificationAttempts = table.Column<int>(type: "int", nullable: false),
                    NotificationError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IpHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobApplications_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobApplications_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobApplications_CareerRoles_CareerRoleId",
                        column: x => x.CareerRoleId,
                        principalTable: "CareerRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobApplicationNotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApplicationNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobApplicationNotes_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobApplicationNotes_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobApplicationNotes_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // roles added before this migration get an address from their id ("role-12"), editable later
            migrationBuilder.Sql("UPDATE [CareerRoles] SET [Slug] = CONCAT('role-', [Id]) WHERE [Slug] = ''");

            migrationBuilder.CreateIndex(
                name: "IX_CareerRoles_Slug",
                table: "CareerRoles",
                column: "Slug",
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplicationNotes_AddedBy_PersonID",
                table: "JobApplicationNotes",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplicationNotes_JobApplicationId",
                table: "JobApplicationNotes",
                column: "JobApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplicationNotes_ModifiedBy_PersonID",
                table: "JobApplicationNotes",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_AddedBy_PersonID",
                table: "JobApplications",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_AddedDate",
                table: "JobApplications",
                column: "AddedDate");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_CareerRoleId",
                table: "JobApplications",
                column: "CareerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_ModifiedBy_PersonID",
                table: "JobApplications",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_Status",
                table: "JobApplications",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobApplicationNotes");

            migrationBuilder.DropTable(
                name: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_CareerRoles_Slug",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "ApplyConsentText_Ar",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "ApplyConsentText_En",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "ApplySuccessText_Ar",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "ApplySuccessText_En",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "ApplySuccessTitle_Ar",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "ApplySuccessTitle_En",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "ApplyTitle_Ar",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "ApplyTitle_En",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "NoOpeningsText_Ar",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "NoOpeningsText_En",
                table: "CareersSections");

            migrationBuilder.DropColumn(
                name: "ClosingOn",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "Department_Ar",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "Department_En",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "Description_Ar",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "Description_En",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "PostedOn",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "Requirements_Ar",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "Requirements_En",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "Responsibilities_Ar",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "Responsibilities_En",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "Summary_Ar",
                table: "CareerRoles");

            migrationBuilder.DropColumn(
                name: "Summary_En",
                table: "CareerRoles");
        }
    }
}
