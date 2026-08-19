using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMealPlanDayAuthor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "MealPlanDays",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MealPlanDays_CreatedByUserId",
                table: "MealPlanDays",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_MealPlanDays_AspNetUsers_CreatedByUserId",
                table: "MealPlanDays",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MealPlanDays_AspNetUsers_CreatedByUserId",
                table: "MealPlanDays");

            migrationBuilder.DropIndex(
                name: "IX_MealPlanDays_CreatedByUserId",
                table: "MealPlanDays");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MealPlanDays");
        }
    }
}
