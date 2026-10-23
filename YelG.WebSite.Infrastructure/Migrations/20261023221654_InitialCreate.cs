using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YelG.WebSite.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Changes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FriendlyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Xml = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AboutValues",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Label_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_AboutValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AboutValues_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AboutValues_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CareerRoles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplyUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApplyEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Experience_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Experience_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_CareerRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerRoles_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerRoles_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IconKey = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ColorVariant = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_ClientCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientCategories_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientCategories_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientPortalSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PortalUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BadgeLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BadgeLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SignInLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SignInLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Text_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Text_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPortalSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientPortalSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientPortalSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsultSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsentText_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConsentText_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DirectContactLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DirectContactLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailureText_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailureText_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FormTitle_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FormTitle_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotSureLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NotSureLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredNote_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiredNote_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmitLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmitLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuccessText_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuccessText_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuccessTitle_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuccessTitle_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_ConsultSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsultSteps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    table.PrimaryKey("PK_ConsultSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultSteps_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultSteps_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Credentials",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_Credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Credentials_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Credentials_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EngageSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_EngageSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EngageSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EngageSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FaqItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsOpenByDefault = table.Column<bool>(type: "bit", nullable: false),
                    Answer_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Answer_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Question_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Question_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_FaqItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaqItems_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FaqItems_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FaqSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_FaqSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FaqSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FaqSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HeroStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChipCodes = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Caption_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Caption_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScreenReaderLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScreenReaderLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_HeroStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeroStats_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HeroStats_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HowSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_HowSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HowSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HowSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HowSteps",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_HowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HowSteps_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HowSteps_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IndustriesSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApprovalNote_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalNote_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_IndustriesSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndustriesSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IndustriesSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IndustrySectors",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsEmphasized = table.Column<bool>(type: "bit", nullable: false),
                    Label_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_IndustrySectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndustrySectors_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IndustrySectors_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InsightsSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FollowLinkUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FollowLinkLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FollowLinkLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_InsightsSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsightsSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsightsSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarqueeItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Label_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_MarqueeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarqueeItems_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarqueeItems_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MarqueeSettings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    ScreenReaderText_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScreenReaderText_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarqueeSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarqueeSettings_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarqueeSettings_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MediaAssets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ThumbnailPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: true),
                    Height = table.Column<int>(type: "int", nullable: true),
                    Alt_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Alt_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaAssets_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MediaAssets_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurposeGoals",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Label_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_PurposeGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurposeGoals_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurposeGoals_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SelfCheckBands",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinScore = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SelfCheckBands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelfCheckBands_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SelfCheckBands_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SelfCheckOptions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Label_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_SelfCheckOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelfCheckOptions_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SelfCheckOptions_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SelfCheckSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BackLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BackLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Disclaimer1_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Disclaimer1_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Disclaimer2_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Disclaimer2_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscussCtaLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscussCtaLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MethodNote_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MethodNote_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PickHint_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PickHint_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrefillTemplate_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrefillTemplate_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecommendationsLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecommendationsLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RestartLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RestartLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_SelfCheckSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelfCheckSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SelfCheckSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCategories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_ServiceCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCategories_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceCategories_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServicesSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AllTabLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AllTabLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CardLinkLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CardLinkLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscussCtaLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscussCtaLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScopeHeading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScopeHeading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_ServicesSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicesSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServicesSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WhyCards",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WhyCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhyCards_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhyCards_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareerRoleTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerRoleId = table.Column<long>(type: "bigint", nullable: false),
                    Label_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_CareerRoleTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerRoleTags_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerRoleTags_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerRoleTags_CareerRoles_CareerRoleId",
                        column: x => x.CareerRoleId,
                        principalTable: "CareerRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AboutSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Image1Id = table.Column<long>(type: "bigint", nullable: true),
                    Image2Id = table.Column<long>(type: "bigint", nullable: true),
                    BadgeRingText = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    CredentialsLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CredentialsLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Image1Alt_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Image1Alt_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Image2Alt_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Image2Alt_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_AboutSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AboutSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AboutSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AboutSections_MediaAssets_Image1Id",
                        column: x => x.Image1Id,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AboutSections_MediaAssets_Image2Id",
                        column: x => x.Image2Id,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareersSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageId = table.Column<long>(type: "bigint", nullable: true),
                    CvEmailSubject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    JobsUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageAlt_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageAlt_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JobsLinkLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JobsLinkLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SendCvLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SendCvLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_CareersSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareersSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareersSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareersSections_MediaAssets_ImageId",
                        column: x => x.ImageId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientLogos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogoId = table.Column<long>(type: "bigint", nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsApprovedForWeb = table.Column<bool>(type: "bit", nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_ClientLogos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientLogos_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientLogos_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientLogos_MediaAssets_LogoId",
                        column: x => x.LogoId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FounderSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    PhotoId = table.Column<long>(type: "bigint", nullable: true),
                    AvatarId = table.Column<long>(type: "bigint", nullable: true),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhotoAlt_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhotoAlt_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quote_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quote_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportingLine_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportingLine_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FounderSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FounderSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FounderSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FounderSections_MediaAssets_AvatarId",
                        column: x => x.AvatarId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FounderSections_MediaAssets_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HeroSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrimaryCtaTarget = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SecondaryCtaTarget = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ImageId = table.Column<long>(type: "bigint", nullable: true),
                    BadgeRingText = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageAlt_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageAlt_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryCtaLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryCtaLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecondaryCtaLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecondaryCtaLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SelfCheckLinkText_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SelfCheckLinkText_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_HeroSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeroSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HeroSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HeroSections_MediaAssets_ImageId",
                        column: x => x.ImageId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InsightPosts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageId = table.Column<long>(type: "bigint", nullable: true),
                    CategoryColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    PublishedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    ExternalUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CategoryLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageAlt_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageAlt_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_InsightPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsightPosts_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsightPosts_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsightPosts_MediaAssets_ImageId",
                        column: x => x.ImageId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurposeSections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VideoId = table.Column<long>(type: "bigint", nullable: true),
                    PosterId = table.Column<long>(type: "bigint", nullable: true),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GoalsTabLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GoalsTabLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mission_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mission_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MissionTabLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MissionTabLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VideoLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VideoLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Vision_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Vision_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisionTabLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisionTabLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_PurposeSections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurposeSections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurposeSections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurposeSections_MediaAssets_PosterId",
                        column: x => x.PosterId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurposeSections_MediaAssets_VideoId",
                        column: x => x.VideoId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogoId = table.Column<long>(type: "bigint", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NotificationEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LinkedInUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CareersEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DefaultLanguage = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    OgImageId = table.Column<long>(type: "bigint", nullable: true),
                    Copyright_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Copyright_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeoDescription_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeoDescription_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeoTitle_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SeoTitle_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tagline_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tagline_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AddedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedBy_PersonID = table.Column<long>(type: "bigint", nullable: true),
                    Deleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleteDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SiteSettings_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteSettings_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteSettings_MediaAssets_LogoId",
                        column: x => x.LogoId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SiteSettings_MediaAssets_OgImageId",
                        column: x => x.OgImageId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WhySections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageId = table.Column<long>(type: "bigint", nullable: true),
                    Eyebrow_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Eyebrow_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FloatingCardText_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FloatingCardText_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Heading_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageAlt_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageAlt_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Lead_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_WhySections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhySections_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhySections_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WhySections_MediaAssets_ImageId",
                        column: x => x.ImageId,
                        principalTable: "MediaAssets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    LongDescription_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LongDescription_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortDescription_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShortDescription_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Services_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Services_ServiceCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ServiceCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EngagementModels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IconKey = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PreselectServiceId = table.Column<long>(type: "bigint", nullable: true),
                    PreselectNotSure = table.Column<bool>(type: "bit", nullable: false),
                    IsHighlighted = table.Column<bool>(type: "bit", nullable: false),
                    CtaLabel_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CtaLabel_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_EngagementModels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EngagementModels_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EngagementModels_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EngagementModels_Services_PreselectServiceId",
                        column: x => x.PreselectServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enquiries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Culture = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Company = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    ServiceId = table.Column<long>(type: "bigint", nullable: true),
                    ServiceLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsNotSure = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    ConsentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsentText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SelfCheckScore = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Enquiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enquiries_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enquiries_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enquiries_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SelfCheckQuestions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LinkedServiceId = table.Column<long>(type: "bigint", nullable: true),
                    Area_Ar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Area_En = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.PrimaryKey("PK_SelfCheckQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SelfCheckQuestions_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SelfCheckQuestions_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SelfCheckQuestions_Services_LinkedServiceId",
                        column: x => x.LinkedServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceDeliverables",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_ServiceDeliverables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceDeliverables_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceDeliverables_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceDeliverables_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EngagementBullets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EngagementModelId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_EngagementBullets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EngagementBullets_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EngagementBullets_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EngagementBullets_EngagementModels_EngagementModelId",
                        column: x => x.EngagementModelId,
                        principalTable: "EngagementModels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnquiryNotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnquiryId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_EnquiryNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnquiryNotes_AspNetUsers_AddedBy_PersonID",
                        column: x => x.AddedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnquiryNotes_AspNetUsers_ModifiedBy_PersonID",
                        column: x => x.ModifiedBy_PersonID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnquiryNotes_Enquiries_EnquiryId",
                        column: x => x.EnquiryId,
                        principalTable: "Enquiries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AboutSections_AddedBy_PersonID",
                table: "AboutSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_AboutSections_Image1Id",
                table: "AboutSections",
                column: "Image1Id");

            migrationBuilder.CreateIndex(
                name: "IX_AboutSections_Image2Id",
                table: "AboutSections",
                column: "Image2Id");

            migrationBuilder.CreateIndex(
                name: "IX_AboutSections_ModifiedBy_PersonID",
                table: "AboutSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_AboutValues_AddedBy_PersonID",
                table: "AboutValues",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_AboutValues_ModifiedBy_PersonID",
                table: "AboutValues",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAt",
                table: "AuditLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_CareerRoles_AddedBy_PersonID",
                table: "CareerRoles",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_CareerRoles_ModifiedBy_PersonID",
                table: "CareerRoles",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_CareerRoleTags_AddedBy_PersonID",
                table: "CareerRoleTags",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_CareerRoleTags_CareerRoleId",
                table: "CareerRoleTags",
                column: "CareerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerRoleTags_ModifiedBy_PersonID",
                table: "CareerRoleTags",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_CareersSections_AddedBy_PersonID",
                table: "CareersSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_CareersSections_ImageId",
                table: "CareersSections",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_CareersSections_ModifiedBy_PersonID",
                table: "CareersSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCategories_AddedBy_PersonID",
                table: "ClientCategories",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ClientCategories_ModifiedBy_PersonID",
                table: "ClientCategories",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ClientLogos_AddedBy_PersonID",
                table: "ClientLogos",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ClientLogos_LogoId",
                table: "ClientLogos",
                column: "LogoId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientLogos_ModifiedBy_PersonID",
                table: "ClientLogos",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPortalSections_AddedBy_PersonID",
                table: "ClientPortalSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPortalSections_ModifiedBy_PersonID",
                table: "ClientPortalSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultSections_AddedBy_PersonID",
                table: "ConsultSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultSections_ModifiedBy_PersonID",
                table: "ConsultSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultSteps_AddedBy_PersonID",
                table: "ConsultSteps",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultSteps_ModifiedBy_PersonID",
                table: "ConsultSteps",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_AddedBy_PersonID",
                table: "Credentials",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_Credentials_ModifiedBy_PersonID",
                table: "Credentials",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_EngagementBullets_AddedBy_PersonID",
                table: "EngagementBullets",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_EngagementBullets_EngagementModelId",
                table: "EngagementBullets",
                column: "EngagementModelId");

            migrationBuilder.CreateIndex(
                name: "IX_EngagementBullets_ModifiedBy_PersonID",
                table: "EngagementBullets",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_EngagementModels_AddedBy_PersonID",
                table: "EngagementModels",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_EngagementModels_ModifiedBy_PersonID",
                table: "EngagementModels",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_EngagementModels_PreselectServiceId",
                table: "EngagementModels",
                column: "PreselectServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_EngageSections_AddedBy_PersonID",
                table: "EngageSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_EngageSections_ModifiedBy_PersonID",
                table: "EngageSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_Enquiries_AddedBy_PersonID",
                table: "Enquiries",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_Enquiries_AddedDate",
                table: "Enquiries",
                column: "AddedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Enquiries_ModifiedBy_PersonID",
                table: "Enquiries",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_Enquiries_ServiceId",
                table: "Enquiries",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Enquiries_Status",
                table: "Enquiries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EnquiryNotes_AddedBy_PersonID",
                table: "EnquiryNotes",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_EnquiryNotes_EnquiryId",
                table: "EnquiryNotes",
                column: "EnquiryId");

            migrationBuilder.CreateIndex(
                name: "IX_EnquiryNotes_ModifiedBy_PersonID",
                table: "EnquiryNotes",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_FaqItems_AddedBy_PersonID",
                table: "FaqItems",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_FaqItems_ModifiedBy_PersonID",
                table: "FaqItems",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_FaqSections_AddedBy_PersonID",
                table: "FaqSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_FaqSections_ModifiedBy_PersonID",
                table: "FaqSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_FounderSections_AddedBy_PersonID",
                table: "FounderSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_FounderSections_AvatarId",
                table: "FounderSections",
                column: "AvatarId");

            migrationBuilder.CreateIndex(
                name: "IX_FounderSections_ModifiedBy_PersonID",
                table: "FounderSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_FounderSections_PhotoId",
                table: "FounderSections",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_HeroSections_AddedBy_PersonID",
                table: "HeroSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_HeroSections_ImageId",
                table: "HeroSections",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_HeroSections_ModifiedBy_PersonID",
                table: "HeroSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_HeroStats_AddedBy_PersonID",
                table: "HeroStats",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_HeroStats_ModifiedBy_PersonID",
                table: "HeroStats",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_HowSections_AddedBy_PersonID",
                table: "HowSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_HowSections_ModifiedBy_PersonID",
                table: "HowSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_HowSteps_AddedBy_PersonID",
                table: "HowSteps",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_HowSteps_ModifiedBy_PersonID",
                table: "HowSteps",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_IndustriesSections_AddedBy_PersonID",
                table: "IndustriesSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_IndustriesSections_ModifiedBy_PersonID",
                table: "IndustriesSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_IndustrySectors_AddedBy_PersonID",
                table: "IndustrySectors",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_IndustrySectors_ModifiedBy_PersonID",
                table: "IndustrySectors",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_InsightPosts_AddedBy_PersonID",
                table: "InsightPosts",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_InsightPosts_ImageId",
                table: "InsightPosts",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_InsightPosts_ModifiedBy_PersonID",
                table: "InsightPosts",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_InsightsSections_AddedBy_PersonID",
                table: "InsightsSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_InsightsSections_ModifiedBy_PersonID",
                table: "InsightsSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_MarqueeItems_AddedBy_PersonID",
                table: "MarqueeItems",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_MarqueeItems_ModifiedBy_PersonID",
                table: "MarqueeItems",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_MarqueeSettings_AddedBy_PersonID",
                table: "MarqueeSettings",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_MarqueeSettings_ModifiedBy_PersonID",
                table: "MarqueeSettings",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_AddedBy_PersonID",
                table: "MediaAssets",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_ModifiedBy_PersonID",
                table: "MediaAssets",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_MediaAssets_Path",
                table: "MediaAssets",
                column: "Path",
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PurposeGoals_AddedBy_PersonID",
                table: "PurposeGoals",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_PurposeGoals_ModifiedBy_PersonID",
                table: "PurposeGoals",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_PurposeSections_AddedBy_PersonID",
                table: "PurposeSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_PurposeSections_ModifiedBy_PersonID",
                table: "PurposeSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_PurposeSections_PosterId",
                table: "PurposeSections",
                column: "PosterId");

            migrationBuilder.CreateIndex(
                name: "IX_PurposeSections_VideoId",
                table: "PurposeSections",
                column: "VideoId");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckBands_AddedBy_PersonID",
                table: "SelfCheckBands",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckBands_ModifiedBy_PersonID",
                table: "SelfCheckBands",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckOptions_AddedBy_PersonID",
                table: "SelfCheckOptions",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckOptions_ModifiedBy_PersonID",
                table: "SelfCheckOptions",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckQuestions_AddedBy_PersonID",
                table: "SelfCheckQuestions",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckQuestions_LinkedServiceId",
                table: "SelfCheckQuestions",
                column: "LinkedServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckQuestions_ModifiedBy_PersonID",
                table: "SelfCheckQuestions",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckSections_AddedBy_PersonID",
                table: "SelfCheckSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_SelfCheckSections_ModifiedBy_PersonID",
                table: "SelfCheckSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategories_AddedBy_PersonID",
                table: "ServiceCategories",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategories_Key",
                table: "ServiceCategories",
                column: "Key",
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCategories_ModifiedBy_PersonID",
                table: "ServiceCategories",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeliverables_AddedBy_PersonID",
                table: "ServiceDeliverables",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeliverables_ModifiedBy_PersonID",
                table: "ServiceDeliverables",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceDeliverables_ServiceId",
                table: "ServiceDeliverables",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_AddedBy_PersonID",
                table: "Services",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_Services_CategoryId",
                table: "Services",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_Key",
                table: "Services",
                column: "Key",
                unique: true,
                filter: "[Deleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Services_ModifiedBy_PersonID",
                table: "Services",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ServicesSections_AddedBy_PersonID",
                table: "ServicesSections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_ServicesSections_ModifiedBy_PersonID",
                table: "ServicesSections",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_AddedBy_PersonID",
                table: "SiteSettings",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_LogoId",
                table: "SiteSettings",
                column: "LogoId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_ModifiedBy_PersonID",
                table: "SiteSettings",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_OgImageId",
                table: "SiteSettings",
                column: "OgImageId");

            migrationBuilder.CreateIndex(
                name: "IX_WhyCards_AddedBy_PersonID",
                table: "WhyCards",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_WhyCards_ModifiedBy_PersonID",
                table: "WhyCards",
                column: "ModifiedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_WhySections_AddedBy_PersonID",
                table: "WhySections",
                column: "AddedBy_PersonID");

            migrationBuilder.CreateIndex(
                name: "IX_WhySections_ImageId",
                table: "WhySections",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_WhySections_ModifiedBy_PersonID",
                table: "WhySections",
                column: "ModifiedBy_PersonID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AboutSections");

            migrationBuilder.DropTable(
                name: "AboutValues");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CareerRoleTags");

            migrationBuilder.DropTable(
                name: "CareersSections");

            migrationBuilder.DropTable(
                name: "ClientCategories");

            migrationBuilder.DropTable(
                name: "ClientLogos");

            migrationBuilder.DropTable(
                name: "ClientPortalSections");

            migrationBuilder.DropTable(
                name: "ConsultSections");

            migrationBuilder.DropTable(
                name: "ConsultSteps");

            migrationBuilder.DropTable(
                name: "Credentials");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "EngagementBullets");

            migrationBuilder.DropTable(
                name: "EngageSections");

            migrationBuilder.DropTable(
                name: "EnquiryNotes");

            migrationBuilder.DropTable(
                name: "FaqItems");

            migrationBuilder.DropTable(
                name: "FaqSections");

            migrationBuilder.DropTable(
                name: "FounderSections");

            migrationBuilder.DropTable(
                name: "HeroSections");

            migrationBuilder.DropTable(
                name: "HeroStats");

            migrationBuilder.DropTable(
                name: "HowSections");

            migrationBuilder.DropTable(
                name: "HowSteps");

            migrationBuilder.DropTable(
                name: "IndustriesSections");

            migrationBuilder.DropTable(
                name: "IndustrySectors");

            migrationBuilder.DropTable(
                name: "InsightPosts");

            migrationBuilder.DropTable(
                name: "InsightsSections");

            migrationBuilder.DropTable(
                name: "MarqueeItems");

            migrationBuilder.DropTable(
                name: "MarqueeSettings");

            migrationBuilder.DropTable(
                name: "PurposeGoals");

            migrationBuilder.DropTable(
                name: "PurposeSections");

            migrationBuilder.DropTable(
                name: "SelfCheckBands");

            migrationBuilder.DropTable(
                name: "SelfCheckOptions");

            migrationBuilder.DropTable(
                name: "SelfCheckQuestions");

            migrationBuilder.DropTable(
                name: "SelfCheckSections");

            migrationBuilder.DropTable(
                name: "ServiceDeliverables");

            migrationBuilder.DropTable(
                name: "ServicesSections");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropTable(
                name: "WhyCards");

            migrationBuilder.DropTable(
                name: "WhySections");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "CareerRoles");

            migrationBuilder.DropTable(
                name: "EngagementModels");

            migrationBuilder.DropTable(
                name: "Enquiries");

            migrationBuilder.DropTable(
                name: "MediaAssets");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "ServiceCategories");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
