using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseCaloriesAndTimers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedMet",
                table: "ExerciseTemplates",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 5.0m);

            migrationBuilder.AddColumn<int>(
                name: "ExerciseKind",
                table: "ExerciseTemplates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "Exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAtUtc",
                table: "Exercises",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedMet",
                table: "Exercises",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 5.0m);

            migrationBuilder.AddColumn<int>(
                name: "ExerciseKind",
                table: "Exercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAtUtc",
                table: "Exercises",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "ExerciseTemplates"
                SET "ExerciseKind" = 1,
                    "EstimatedMet" = CASE "Name"
                        WHEN 'Running' THEN 9.8
                        WHEN 'Cycling' THEN 7.5
                        WHEN 'Rowing Machine' THEN 7.0
                        WHEN 'Jump Rope' THEN 10.0
                        WHEN 'Assault Bike' THEN 9.0
                        WHEN 'Elliptical' THEN 5.5
                        WHEN 'Stair Climber' THEN 8.8
                        WHEN 'Mountain Climber' THEN 8.0
                        ELSE "EstimatedMet"
                    END
                WHERE "Name" IN (
                    'Running',
                    'Cycling',
                    'Rowing Machine',
                    'Jump Rope',
                    'Assault Bike',
                    'Elliptical',
                    'Stair Climber',
                    'Mountain Climber'
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedMet",
                table: "ExerciseTemplates");

            migrationBuilder.DropColumn(
                name: "ExerciseKind",
                table: "ExerciseTemplates");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "EndedAtUtc",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "EstimatedMet",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ExerciseKind",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "StartedAtUtc",
                table: "Exercises");
        }
    }
}
