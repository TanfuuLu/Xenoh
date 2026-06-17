using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xenoh.Infrastructure.Persistence;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260617110000_AddTrainingDayShareCaptionAndLoves")]
    public partial class AddTrainingDayShareCaptionAndLoves : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Caption",
                table: "TrainingDayShares",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TrainingDayShareLoves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingDayShareId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingDayShareLoves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingDayShareLoves_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingDayShareLoves_TrainingDayShares_TrainingDayShareId",
                        column: x => x.TrainingDayShareId,
                        principalTable: "TrainingDayShares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDayShareLoves_TrainingDayShareId_UserId",
                table: "TrainingDayShareLoves",
                columns: new[] { "TrainingDayShareId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrainingDayShareLoves_UserId",
                table: "TrainingDayShareLoves",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingDayShareLoves");

            migrationBuilder.DropColumn(
                name: "Caption",
                table: "TrainingDayShares");
        }
    }
}
