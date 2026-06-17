using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFriendCommunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserBId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddresseeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Friendships_AspNetUsers_AddresseeId",
                        column: x => x.AddresseeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Friendships_AspNetUsers_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Friendships_AspNetUsers_UserAId",
                        column: x => x.UserAId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Friendships_AspNetUsers_UserBId",
                        column: x => x.UserBId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingDayShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceDailyWorkoutId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    DayStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExerciseCount = table.Column<int>(type: "integer", nullable: false),
                    CompletedSets = table.Column<int>(type: "integer", nullable: false),
                    TotalVolume = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    TotalDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    AverageRpe = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingDayShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingDayShares_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingDayShareExercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingDayShareId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryMuscleGroup = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ExerciseKind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsSkipped = table.Column<bool>(type: "boolean", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingDayShareExercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingDayShareExercises_TrainingDayShares_TrainingDayShar~",
                        column: x => x.TrainingDayShareId,
                        principalTable: "TrainingDayShares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingDayShareSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingDayShareExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    SetNumber = table.Column<int>(type: "integer", nullable: false),
                    ActualReps = table.Column<int>(type: "integer", nullable: true),
                    ActualWeight = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    Rpe = table.Column<decimal>(type: "numeric(4,2)", nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingDayShareSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingDayShareSets_TrainingDayShareExercises_TrainingDayS~",
                        column: x => x.TrainingDayShareExerciseId,
                        principalTable: "TrainingDayShareExercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_AddresseeId_Status",
                table: "Friendships",
                columns: new[] { "AddresseeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_RequesterId_Status",
                table: "Friendships",
                columns: new[] { "RequesterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_UserAId_Status",
                table: "Friendships",
                columns: new[] { "UserAId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_UserAId_UserBId",
                table: "Friendships",
                columns: new[] { "UserAId", "UserBId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_UserBId_Status",
                table: "Friendships",
                columns: new[] { "UserBId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDayShareExercises_TrainingDayShareId_SortOrder",
                table: "TrainingDayShareExercises",
                columns: new[] { "TrainingDayShareId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDayShares_SourceDailyWorkoutId",
                table: "TrainingDayShares",
                column: "SourceDailyWorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDayShares_UserId_CreatedAt",
                table: "TrainingDayShares",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDayShareSets_TrainingDayShareExerciseId_SetNumber",
                table: "TrainingDayShareSets",
                columns: new[] { "TrainingDayShareExerciseId", "SetNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Friendships");

            migrationBuilder.DropTable(
                name: "TrainingDayShareSets");

            migrationBuilder.DropTable(
                name: "TrainingDayShareExercises");

            migrationBuilder.DropTable(
                name: "TrainingDayShares");
        }
    }
}
