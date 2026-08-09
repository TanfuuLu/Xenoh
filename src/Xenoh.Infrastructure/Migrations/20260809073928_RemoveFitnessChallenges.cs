using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFitnessChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM "Notifications"
                WHERE "RelatedEntityType" = 'FitnessChallenge'
                   OR "Type" LIKE 'FitnessChallenge%';
                """);

            migrationBuilder.DropTable(
                name: "FitnessChallengeCheckIns");

            migrationBuilder.DropTable(
                name: "FitnessChallengeMembers");

            migrationBuilder.DropTable(
                name: "FitnessChallenges");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FitnessChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    CheckInPrompt = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    CompletionNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MetricType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    SelectedLifts = table.Column<string>(type: "jsonb", nullable: false),
                    StartNotifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetSessionsPerWeek = table.Column<int>(type: "integer", nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FitnessChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FitnessChallenges_AspNetUsers_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FitnessChallengeCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FitnessChallengeCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FitnessChallengeCheckIns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FitnessChallengeCheckIns_FitnessChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "FitnessChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FitnessChallengeMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastBehindReminderWeekStart = table.Column<DateOnly>(type: "date", nullable: true),
                    LastCompletionNotificationWeekStart = table.Column<DateOnly>(type: "date", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FitnessChallengeMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FitnessChallengeMembers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FitnessChallengeMembers_FitnessChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "FitnessChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FitnessChallengeCheckIns_ChallengeId_UserId_LocalDate",
                table: "FitnessChallengeCheckIns",
                columns: new[] { "ChallengeId", "UserId", "LocalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FitnessChallengeCheckIns_UserId_LocalDate",
                table: "FitnessChallengeCheckIns",
                columns: new[] { "UserId", "LocalDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FitnessChallengeMembers_ChallengeId_UserId",
                table: "FitnessChallengeMembers",
                columns: new[] { "ChallengeId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FitnessChallengeMembers_UserId_Status",
                table: "FitnessChallengeMembers",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FitnessChallenges_AccessType_StartsAtUtc_EndsAtUtc",
                table: "FitnessChallenges",
                columns: new[] { "AccessType", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FitnessChallenges_CreatorId_Status",
                table: "FitnessChallenges",
                columns: new[] { "CreatorId", "Status" });
        }
    }
}
