using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualReps",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ActualSets",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ActualWeight",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "CompletedSets",
                table: "Exercises");

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
                name: "IX_ExerciseSets_ExerciseId",
                table: "ExerciseSets",
                column: "ExerciseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseSets");

            migrationBuilder.AddColumn<int>(
                name: "ActualReps",
                table: "Exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActualSets",
                table: "Exercises",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ActualWeight",
                table: "Exercises",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedSets",
                table: "Exercises",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
