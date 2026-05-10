using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachMarketplaceProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoachMarketplaceProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachId = table.Column<Guid>(type: "uuid", nullable: false),
                    Headline = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ExperienceYears = table.Column<int>(type: "integer", nullable: true),
                    Specialties = table.Column<string[]>(type: "text[]", nullable: false),
                    Certifications = table.Column<string[]>(type: "text[]", nullable: false),
                    Languages = table.Column<string[]>(type: "text[]", nullable: false),
                    CoachingMethods = table.Column<string[]>(type: "text[]", nullable: false),
                    Achievements = table.Column<string[]>(type: "text[]", nullable: false),
                    ClientResultsSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Availability = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ResponseTime = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CoachingStyle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachMarketplaceProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachMarketplaceProfiles_AspNetUsers_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoachPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachMarketplaceProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    DurationLabel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachPackages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachPackages_CoachMarketplaceProfiles_CoachMarketplaceProf~",
                        column: x => x.CoachMarketplaceProfileId,
                        principalTable: "CoachMarketplaceProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachMarketplaceProfiles_CoachId",
                table: "CoachMarketplaceProfiles",
                column: "CoachId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachPackages_CoachMarketplaceProfileId_DisplayOrder",
                table: "CoachPackages",
                columns: new[] { "CoachMarketplaceProfileId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachPackages");

            migrationBuilder.DropTable(
                name: "CoachMarketplaceProfiles");
        }
    }
}
