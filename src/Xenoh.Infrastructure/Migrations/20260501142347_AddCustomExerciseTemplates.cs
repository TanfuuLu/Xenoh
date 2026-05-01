using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomExerciseTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "ExerciseTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "ExerciseTemplates",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseTemplates_OwnerId_IsArchived",
                table: "ExerciseTemplates",
                columns: new[] { "OwnerId", "IsArchived" });

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseTemplates_AspNetUsers_OwnerId",
                table: "ExerciseTemplates",
                column: "OwnerId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseTemplates_AspNetUsers_OwnerId",
                table: "ExerciseTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseTemplates_OwnerId_IsArchived",
                table: "ExerciseTemplates");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "ExerciseTemplates");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "ExerciseTemplates");
        }
    }
}
