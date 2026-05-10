using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceCoachPackagesWithContractPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoachPackages");

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

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "CoachMarketplaceProfiles",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "VND");

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPriceAmount",
                table: "CoachMarketplaceProfiles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SessionPriceAmount",
                table: "CoachMarketplaceProfiles",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "CoachMarketplaceProfiles");

            migrationBuilder.DropColumn(
                name: "MonthlyPriceAmount",
                table: "CoachMarketplaceProfiles");

            migrationBuilder.DropColumn(
                name: "SessionPriceAmount",
                table: "CoachMarketplaceProfiles");

            migrationBuilder.CreateTable(
                name: "CoachPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachMarketplaceProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    DurationLabel = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PriceAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
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
                name: "IX_CoachPackages_CoachMarketplaceProfileId_DisplayOrder",
                table: "CoachPackages",
                columns: new[] { "CoachMarketplaceProfileId", "DisplayOrder" });

        }
    }
}
