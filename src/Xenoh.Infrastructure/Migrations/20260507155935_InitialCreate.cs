using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Height = table.Column<decimal>(type: "numeric", nullable: true),
                    Gender = table.Column<int>(type: "integer", nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AvatarUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    TotalXp = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
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
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
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
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
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
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
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
                name: "BodyweightLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(6,2)", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodyweightLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BodyweightLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachClientRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TerminationRequestedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachClientRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachClientRelationships_AspNetUsers_ClientId",
                        column: x => x.ClientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachClientRelationships_AspNetUsers_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoachRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachRatings_AspNetUsers_ClientId",
                        column: x => x.ClientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachRatings_AspNetUsers_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrimaryMuscleGroup = table.Column<int>(type: "integer", nullable: false),
                    SecondaryMuscleGroups = table.Column<string>(type: "jsonb", nullable: false),
                    ExerciseKind = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EstimatedMet = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 5.0m),
                    IsCompetitionLift = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CompetitionLiftType = table.Column<int>(type: "integer", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseTemplates_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalAuthTickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalAuthTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalAuthTickets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NutritionDailyLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Calories = table.Column<int>(type: "integer", nullable: false),
                    ProteinG = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    CarbsG = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    FatG = table.Column<decimal>(type: "numeric(7,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionDailyLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NutritionDailyLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NutritionProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityLevel = table.Column<int>(type: "integer", nullable: false),
                    Goal = table.Column<int>(type: "integer", nullable: false),
                    TargetWeightKg = table.Column<decimal>(type: "numeric(6,2)", nullable: true),
                    CustomCalorieTarget = table.Column<int>(type: "integer", nullable: true),
                    ProteinPerKg = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    FatPerKg = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NutritionProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResetToken = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResetCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResetCodes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlanType = table.Column<int>(type: "integer", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByCoachId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plans_AspNetUsers_CreatedByCoachId",
                        column: x => x.CreatedByCoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Plans_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AdminNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserReports_AspNetUsers_ReportedUserId",
                        column: x => x.ReportedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserReports_AspNetUsers_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserReports_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tier = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkoutHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserExercisePRHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    AchievedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExercisePRHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserExercisePRHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserExercisePRHistories_ExerciseTemplates_ExerciseTemplateId",
                        column: x => x.ExerciseTemplateId,
                        principalTable: "ExerciseTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserExercisePRs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: false),
                    AchievedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExercisePRs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserExercisePRs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserExercisePRs_ExerciseTemplates_ExerciseTemplateId",
                        column: x => x.ExerciseTemplateId,
                        principalTable: "ExerciseTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanComments_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanComments_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyWorkouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeekNumber = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyWorkouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyWorkouts_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedTier = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TransferCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DurationMonths = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SePayTransactionId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SePayReferenceCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentOrders_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentOrders_UserSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "UserSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyWorkouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    WeeklyWorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyWorkouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyWorkouts_WeeklyWorkouts_WeeklyWorkoutId",
                        column: x => x.WeeklyWorkoutId,
                        principalTable: "WeeklyWorkouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyWorkoutComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WeeklyWorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyWorkoutComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeeklyWorkoutComments_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WeeklyWorkoutComments_WeeklyWorkouts_WeeklyWorkoutId",
                        column: x => x.WeeklyWorkoutId,
                        principalTable: "WeeklyWorkouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryMuscleGroup = table.Column<int>(type: "integer", nullable: false),
                    SecondaryMuscleGroups = table.Column<string>(type: "jsonb", nullable: false),
                    ExerciseKind = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EstimatedMet = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 5.0m),
                    ExerciseTemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlannedSets = table.Column<int>(type: "integer", nullable: false),
                    PlannedReps = table.Column<int>(type: "integer", nullable: false),
                    PlannedWeight = table.Column<decimal>(type: "numeric", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    DailyWorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exercises_DailyWorkouts_DailyWorkoutId",
                        column: x => x.DailyWorkoutId,
                        principalTable: "DailyWorkouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Exercises_ExerciseTemplates_ExerciseTemplateId",
                        column: x => x.ExerciseTemplateId,
                        principalTable: "ExerciseTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    PlannedReps = table.Column<int>(type: "integer", nullable: false),
                    PlannedWeight = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    ActualReps = table.Column<int>(type: "integer", nullable: true),
                    ActualWeight = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Rpe = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseSets_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

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
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BodyweightLogs_UserId_Date",
                table: "BodyweightLogs",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_ClientId",
                table: "CoachClientRelationships",
                column: "ClientId",
                unique: true,
                filter: "\"Status\" <> 2");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_CoachId",
                table: "CoachClientRelationships",
                column: "CoachId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachRatings_ClientId",
                table: "CoachRatings",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachRatings_CoachId_ClientId",
                table: "CoachRatings",
                columns: new[] { "CoachId", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkouts_WeeklyWorkoutId",
                table: "DailyWorkouts",
                column: "WeeklyWorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_DailyWorkoutId",
                table: "Exercises",
                column: "DailyWorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ExerciseTemplateId",
                table: "Exercises",
                column: "ExerciseTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSets_ExerciseId",
                table: "ExerciseSets",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseTemplates_OwnerId_IsArchived",
                table: "ExerciseTemplates",
                columns: new[] { "OwnerId", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthTickets_TicketHash",
                table: "ExternalAuthTickets",
                column: "TicketHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalAuthTickets_UserId_UsedAt_ExpiresAt",
                table: "ExternalAuthTickets",
                columns: new[] { "UserId", "UsedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "RecipientId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NutritionDailyLogs_UserId_Date",
                table: "NutritionDailyLogs",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NutritionProfiles_UserId",
                table: "NutritionProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetCodes_Email_UsedAt_ExpiresAt",
                table: "PasswordResetCodes",
                columns: new[] { "Email", "UsedAt", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetCodes_UserId",
                table: "PasswordResetCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_SePayTransactionId",
                table: "PaymentOrders",
                column: "SePayTransactionId",
                unique: true,
                filter: "\"SePayTransactionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_SubscriptionId",
                table: "PaymentOrders",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_TransferCode",
                table: "PaymentOrders",
                column: "TransferCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_UserId_Status",
                table: "PaymentOrders",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanComments_AuthorId",
                table: "PlanComments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanComments_PlanId_CreatedAt",
                table: "PlanComments",
                columns: new[] { "PlanId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_CreatedByCoachId",
                table: "Plans",
                column: "CreatedByCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_OwnerId_IsActive",
                table: "Plans",
                columns: new[] { "OwnerId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserExercisePRHistories_ExerciseTemplateId",
                table: "UserExercisePRHistories",
                column: "ExerciseTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserExercisePRHistories_UserId_ExerciseTemplateId_AchievedAt",
                table: "UserExercisePRHistories",
                columns: new[] { "UserId", "ExerciseTemplateId", "AchievedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserExercisePRs_ExerciseTemplateId",
                table: "UserExercisePRs",
                column: "ExerciseTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_UserExercisePRs_UserId_ExerciseTemplateId",
                table: "UserExercisePRs",
                columns: new[] { "UserId", "ExerciseTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ReportedUserId",
                table: "UserReports",
                column: "ReportedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ReporterId",
                table: "UserReports",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_ReviewedById",
                table: "UserReports",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_UserReports_Status_CreatedAt",
                table: "UserReports",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyWorkoutComments_AuthorId",
                table: "WeeklyWorkoutComments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyWorkoutComments_WeeklyWorkoutId_CreatedAt",
                table: "WeeklyWorkoutComments",
                columns: new[] { "WeeklyWorkoutId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyWorkouts_PlanId",
                table: "WeeklyWorkouts",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutHistories_UserId_Date",
                table: "WorkoutHistories",
                columns: new[] { "UserId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "BodyweightLogs");

            migrationBuilder.DropTable(
                name: "CoachClientRelationships");

            migrationBuilder.DropTable(
                name: "CoachRatings");

            migrationBuilder.DropTable(
                name: "ExerciseSets");

            migrationBuilder.DropTable(
                name: "ExternalAuthTickets");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NutritionDailyLogs");

            migrationBuilder.DropTable(
                name: "NutritionProfiles");

            migrationBuilder.DropTable(
                name: "PasswordResetCodes");

            migrationBuilder.DropTable(
                name: "PaymentOrders");

            migrationBuilder.DropTable(
                name: "PlanComments");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "UserExercisePRHistories");

            migrationBuilder.DropTable(
                name: "UserExercisePRs");

            migrationBuilder.DropTable(
                name: "UserReports");

            migrationBuilder.DropTable(
                name: "WeeklyWorkoutComments");

            migrationBuilder.DropTable(
                name: "WorkoutHistories");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Exercises");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "DailyWorkouts");

            migrationBuilder.DropTable(
                name: "ExerciseTemplates");

            migrationBuilder.DropTable(
                name: "WeeklyWorkouts");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
