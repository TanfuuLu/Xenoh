using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseSkipState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSkipped",
                table: "Exercises",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_DailyWorkoutId_IsSkipped",
                table: "Exercises",
                columns: new[] { "DailyWorkoutId", "IsSkipped" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Exercises_DailyWorkoutId_IsSkipped",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "IsSkipped",
                table: "Exercises");
        }
    }
}
