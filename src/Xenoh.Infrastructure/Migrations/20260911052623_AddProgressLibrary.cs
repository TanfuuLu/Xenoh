using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProgressLibrary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgressCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckInDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BodyweightKg = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    IsMilestone = table.Column<bool>(type: "boolean", nullable: false),
                    IsSharedWithCoach = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressCheckIns_AspNetUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgressPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgressCheckInId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Angle = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgressPhotos_ProgressCheckIns_ProgressCheckInId",
                        column: x => x.ProgressCheckInId,
                        principalTable: "ProgressCheckIns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressCheckIns_OwnerId_CheckInDate",
                table: "ProgressCheckIns",
                columns: new[] { "OwnerId", "CheckInDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressCheckIns_OwnerId_IsSharedWithCoach",
                table: "ProgressCheckIns",
                columns: new[] { "OwnerId", "IsSharedWithCoach" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgressPhotos_ProgressCheckInId_SortOrder",
                table: "ProgressPhotos",
                columns: new[] { "ProgressCheckInId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgressPhotos");

            migrationBuilder.DropTable(
                name: "ProgressCheckIns");
        }
    }
}
