using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseTemplateImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ExerciseTemplates",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ExerciseTemplates");
        }
    }
}
