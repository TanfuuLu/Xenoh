using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachInviteCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create CoachInviteCodes table
            migrationBuilder.CreateTable(
                name: "CoachInviteCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    CoachingStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CoachingEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    UsedByClientId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachInviteCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachInviteCodes_AspNetUsers_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachInviteCodes_Code",
                table: "CoachInviteCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachInviteCodes_CoachId",
                table: "CoachInviteCodes",
                column: "CoachId");

            // Add CoachInviteCodeId FK column to CoachClientRelationships
            migrationBuilder.AddColumn<Guid>(
                name: "CoachInviteCodeId",
                table: "CoachClientRelationships",
                type: "uuid",
                nullable: true);

            // Drop old unique index (filter: Status <> 2)
            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_ClientId",
                table: "CoachClientRelationships");

            // Create new unique index (filter: Status <> 2 AND Status <> 4)
            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_ClientId",
                table: "CoachClientRelationships",
                column: "ClientId",
                unique: true,
                filter: "\"Status\" <> 2 AND \"Status\" <> 4");

            // Add FK for CoachInviteCodeId
            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_CoachInviteCodeId",
                table: "CoachClientRelationships",
                column: "CoachInviteCodeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CoachClientRelationships_CoachInviteCodes_CoachInviteCodeId",
                table: "CoachClientRelationships",
                column: "CoachInviteCodeId",
                principalTable: "CoachInviteCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoachClientRelationships_CoachInviteCodes_CoachInviteCodeId",
                table: "CoachClientRelationships");

            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_CoachInviteCodeId",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "CoachInviteCodeId",
                table: "CoachClientRelationships");

            migrationBuilder.DropIndex(
                name: "IX_CoachClientRelationships_ClientId",
                table: "CoachClientRelationships");

            migrationBuilder.CreateIndex(
                name: "IX_CoachClientRelationships_ClientId",
                table: "CoachClientRelationships",
                column: "ClientId",
                unique: true,
                filter: "\"Status\" <> 2");

            migrationBuilder.DropTable(name: "CoachInviteCodes");
        }
    }
}
