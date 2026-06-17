using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xenoh.Infrastructure.Persistence;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260617103000_AddTrainingDaySharePersonalRecord")]
    public partial class AddTrainingDaySharePersonalRecord : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasPersonalRecord",
                table: "TrainingDayShares",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasPersonalRecord",
                table: "TrainingDayShares");
        }
    }
}
