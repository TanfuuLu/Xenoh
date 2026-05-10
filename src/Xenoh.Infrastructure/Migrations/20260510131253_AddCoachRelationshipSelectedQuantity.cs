using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCoachRelationshipSelectedQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "CoachClientRelationships"
                ADD COLUMN IF NOT EXISTS "SelectedQuantity" integer;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "CoachMarketplaceProfiles"
                DROP COLUMN IF EXISTS "PlanPriceAmount";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "CoachClientRelationships"
                DROP COLUMN IF EXISTS "SelectedQuantity";
                """);

            migrationBuilder.Sql("""
                ALTER TABLE "CoachMarketplaceProfiles"
                ADD COLUMN IF NOT EXISTS "PlanPriceAmount" numeric(18,2);
                """);
        }
    }
}
