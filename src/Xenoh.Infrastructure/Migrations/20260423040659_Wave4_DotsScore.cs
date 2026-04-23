using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Wave4_DotsScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompetitionLiftType",
                table: "ExerciseTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCompetitionLift",
                table: "ExerciseTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Mark Big 3 competition lifts for existing seeded rows
            // CompetitionLiftType: Squat=0, Bench=1, Deadlift=2
            migrationBuilder.Sql("""
                UPDATE "ExerciseTemplates"
                SET "IsCompetitionLift" = true, "CompetitionLiftType" = 0
                WHERE "Name" = 'Squat';

                UPDATE "ExerciseTemplates"
                SET "IsCompetitionLift" = true, "CompetitionLiftType" = 1
                WHERE "Name" = 'Bench Press';

                UPDATE "ExerciseTemplates"
                SET "IsCompetitionLift" = true, "CompetitionLiftType" = 2
                WHERE "Name" = 'Deadlift';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompetitionLiftType",
                table: "ExerciseTemplates");

            migrationBuilder.DropColumn(
                name: "IsCompetitionLift",
                table: "ExerciseTemplates");
        }
    }
}
