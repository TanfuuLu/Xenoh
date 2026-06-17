using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xenoh.Infrastructure.Persistence;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260617104500_AddTrainingDayShareExercisePersonalRecord")]
    public partial class AddTrainingDayShareExercisePersonalRecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPersonalRecord",
                table: "TrainingDayShareExercises",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPersonalRecord",
                table: "TrainingDayShareExercises");
        }
    }
}
