using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCoachMarketplaceRatingsAndPricing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachMarketplaceProfiles");

            migrationBuilder.DropTable(
                name: "CoachRatings");

            migrationBuilder.DropColumn(
                name: "SelectedCoachingType",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "SelectedCurrency",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "SelectedPriceAmount",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "SelectedQuantity",
                table: "CoachClientRelationships");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SelectedCoachingType",
                table: "CoachClientRelationships",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedCurrency",
                table: "CoachClientRelationships",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SelectedPriceAmount",
                table: "CoachClientRelationships",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SelectedQuantity",
                table: "CoachClientRelationships",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CoachMarketplaceProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachId = table.Column<Guid>(type: "uuid", nullable: false),
                    Achievements = table.Column<string[]>(type: "text[]", nullable: false),
                    Availability = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Certifications = table.Column<string[]>(type: "text[]", nullable: false),
                    ClientResultsSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CoachingMethods = table.Column<string[]>(type: "text[]", nullable: false),
                    CoachingStyle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    ExperienceYears = table.Column<int>(type: "integer", nullable: true),
                    Headline = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Languages = table.Column<string[]>(type: "text[]", nullable: false),
                    MonthlyPriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ResponseTime = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SessionPriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Specialties = table.Column<string[]>(type: "text[]", nullable: false),
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
                name: "CoachRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachRatings_AspNetUsers_ClientId",
                        column: x => x.ClientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CoachRatings_AspNetUsers_CoachId",
                        column: x => x.CoachId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoachMarketplaceProfiles_CoachId",
                table: "CoachMarketplaceProfiles",
                column: "CoachId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoachRatings_ClientId",
                table: "CoachRatings",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachRatings_CoachId_ClientId",
                table: "CoachRatings",
                columns: new[] { "CoachId", "ClientId" },
                unique: true);
        }
    }
}
