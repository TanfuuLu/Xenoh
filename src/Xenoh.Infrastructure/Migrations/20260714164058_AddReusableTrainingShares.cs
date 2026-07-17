using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReusableTrainingShares : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlannedReps",
                table: "TrainingDayShareSets",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PlannedWeight",
                table: "TrainingDayShareSets",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReusable",
                table: "TrainingDayShares",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedMet",
                table: "TrainingDayShareExercises",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "ExerciseTemplateId",
                table: "TrainingDayShareExercises",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "PlannedReps",
                table: "TrainingDayShareExercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PlannedSets",
                table: "TrainingDayShareExercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PlannedWeight",
                table: "TrainingDayShareExercises",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryMuscleGroups",
                table: "TrainingDayShareExercises",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDayShareExercises_ExerciseTemplateId",
                table: "TrainingDayShareExercises",
                column: "ExerciseTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TrainingDayShareExercises_ExerciseTemplateId",
                table: "TrainingDayShareExercises");

            migrationBuilder.DropColumn(
                name: "PlannedReps",
                table: "TrainingDayShareSets");

            migrationBuilder.DropColumn(
                name: "PlannedWeight",
                table: "TrainingDayShareSets");

            migrationBuilder.DropColumn(
                name: "IsReusable",
                table: "TrainingDayShares");

            migrationBuilder.DropColumn(
                name: "EstimatedMet",
                table: "TrainingDayShareExercises");

            migrationBuilder.DropColumn(
                name: "ExerciseTemplateId",
                table: "TrainingDayShareExercises");

            migrationBuilder.DropColumn(
                name: "PlannedReps",
                table: "TrainingDayShareExercises");

            migrationBuilder.DropColumn(
                name: "PlannedSets",
                table: "TrainingDayShareExercises");

            migrationBuilder.DropColumn(
                name: "PlannedWeight",
                table: "TrainingDayShareExercises");

            migrationBuilder.DropColumn(
                name: "SecondaryMuscleGroups",
                table: "TrainingDayShareExercises");
        }
    }
}
