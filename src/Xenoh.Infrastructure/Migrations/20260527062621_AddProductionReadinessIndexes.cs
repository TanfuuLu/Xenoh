using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionReadinessIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyWorkouts_PlanId",
                table: "WeeklyWorkouts");

            migrationBuilder.DropIndex(
                name: "IX_Plans_CreatedByCoachId",
                table: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseSets_ExerciseId",
                table: "ExerciseSets");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_DailyWorkoutId",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_DailyWorkouts_WeeklyWorkoutId",
                table: "DailyWorkouts");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyWorkouts_PlanId_StartDate_EndDate",
                table: "WeeklyWorkouts",
                columns: new[] { "PlanId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyWorkouts_PlanId_WeekNumber",
                table: "WeeklyWorkouts",
                columns: new[] { "PlanId", "WeekNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_CreatedByCoachId_PlanType_CreatedAt",
                table: "Plans",
                columns: new[] { "CreatedByCoachId", "PlanType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Plans_OwnerId_StartDate_EndDate",
                table: "Plans",
                columns: new[] { "OwnerId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId_CreatedAt",
                table: "Notifications",
                columns: new[] { "RecipientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Messages_RelationshipId_IsRead_SenderId",
                table: "Messages",
                columns: new[] { "RelationshipId", "IsRead", "SenderId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSets_CompletedAt",
                table: "ExerciseSets",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSets_ExerciseId_IsCompleted",
                table: "ExerciseSets",
                columns: new[] { "ExerciseId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSets_ExerciseId_SetNumber",
                table: "ExerciseSets",
                columns: new[] { "ExerciseId", "SetNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_DailyWorkoutId_IsCompleted",
                table: "Exercises",
                columns: new[] { "DailyWorkoutId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_DailyWorkoutId_SortOrder",
                table: "Exercises",
                columns: new[] { "DailyWorkoutId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkouts_WeeklyWorkoutId_Date",
                table: "DailyWorkouts",
                columns: new[] { "WeeklyWorkoutId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkouts_WeeklyWorkoutId_Status",
                table: "DailyWorkouts",
                columns: new[] { "WeeklyWorkoutId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WeeklyWorkouts_PlanId_StartDate_EndDate",
                table: "WeeklyWorkouts");

            migrationBuilder.DropIndex(
                name: "IX_WeeklyWorkouts_PlanId_WeekNumber",
                table: "WeeklyWorkouts");

            migrationBuilder.DropIndex(
                name: "IX_Plans_CreatedByCoachId_PlanType_CreatedAt",
                table: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_Plans_OwnerId_StartDate_EndDate",
                table: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_RecipientId_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Messages_RelationshipId_IsRead_SenderId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseSets_CompletedAt",
                table: "ExerciseSets");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseSets_ExerciseId_IsCompleted",
                table: "ExerciseSets");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseSets_ExerciseId_SetNumber",
                table: "ExerciseSets");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_DailyWorkoutId_IsCompleted",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_DailyWorkoutId_SortOrder",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_DailyWorkouts_WeeklyWorkoutId_Date",
                table: "DailyWorkouts");

            migrationBuilder.DropIndex(
                name: "IX_DailyWorkouts_WeeklyWorkoutId_Status",
                table: "DailyWorkouts");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyWorkouts_PlanId",
                table: "WeeklyWorkouts",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_CreatedByCoachId",
                table: "Plans",
                column: "CreatedByCoachId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSets_ExerciseId",
                table: "ExerciseSets",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_DailyWorkoutId",
                table: "Exercises",
                column: "DailyWorkoutId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyWorkouts_WeeklyWorkoutId",
                table: "DailyWorkouts",
                column: "WeeklyWorkoutId");
        }
    }
}
