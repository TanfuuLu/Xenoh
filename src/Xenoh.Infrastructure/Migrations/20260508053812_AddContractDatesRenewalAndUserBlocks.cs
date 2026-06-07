using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContractDatesRenewalAndUserBlocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "CoachClientRelationships",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ProposedEndDate",
                table: "CoachClientRelationships",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "RenewalRequestedBy",
                table: "CoachClientRelationships",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "CoachClientRelationships",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // Backfill StartDate from CreatedAt for rows that existed before this migration.
            migrationBuilder.Sql(
                "UPDATE \"CoachClientRelationships\" SET \"StartDate\" = (\"CreatedAt\" AT TIME ZONE 'UTC')::date WHERE \"StartDate\" = DATE '0001-01-01';");

            migrationBuilder.CreateTable(
                name: "UserBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BlockedId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBlocks_AspNetUsers_BlockedId",
                        column: x => x.BlockedId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserBlocks_AspNetUsers_BlockerId",
                        column: x => x.BlockerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_Status_EndDate",
                table: "CoachClientRelationships",
                columns: new[] { "Status", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockedId",
                table: "UserBlocks",
                column: "BlockedId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBlocks_BlockerId_BlockedId",
                table: "UserBlocks",
                columns: new[] { "BlockerId", "BlockedId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBlocks");

            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_Status_EndDate",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "ProposedEndDate",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "RenewalRequestedBy",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "CoachClientRelationships");
        }
    }
}
