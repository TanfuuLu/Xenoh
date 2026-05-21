using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodServingBilingualLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Label",
                table: "FoodServings",
                newName: "LabelVi");

            migrationBuilder.RenameIndex(
                name: "IX_FoodServings_FoodItemId_Label",
                table: "FoodServings",
                newName: "IX_FoodServings_FoodItemId_LabelVi");

            migrationBuilder.RenameColumn(
                name: "ServingLabel",
                table: "FoodLogs",
                newName: "ServingLabelVi");

            migrationBuilder.AddColumn<string>(
                name: "LabelEn",
                table: "FoodServings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServingLabelEn",
                table: "FoodLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LabelEn",
                table: "FoodServings");

            migrationBuilder.DropColumn(
                name: "ServingLabelEn",
                table: "FoodLogs");

            migrationBuilder.RenameColumn(
                name: "LabelVi",
                table: "FoodServings",
                newName: "Label");

            migrationBuilder.RenameIndex(
                name: "IX_FoodServings_FoodItemId_LabelVi",
                table: "FoodServings",
                newName: "IX_FoodServings_FoodItemId_Label");

            migrationBuilder.RenameColumn(
                name: "ServingLabelVi",
                table: "FoodLogs",
                newName: "ServingLabel");
        }
    }
}
