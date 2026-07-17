using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xenoh.Infrastructure.Persistence;

#nullable disable

namespace Xenoh.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260716014500_RemoveGeneratedCompetitionCategories")]
public sealed class RemoveGeneratedCompetitionCategories : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM "CompetitionCategories" AS category
            USING "CompetitionEvents" AS event
            WHERE category."EventId" = event."Id"
              AND event."Status" = 'Draft'
              AND NOT EXISTS (
                  SELECT 1
                  FROM "CompetitionRegistrations" AS registration
                  WHERE registration."CategoryId" = category."Id"
              )
              AND (
                  (
                      event."Discipline" = 'Powerlifting'
                      AND category."EquipmentDivision" = 'Classic/Raw'
                      AND category."Code" IN (
                          'PL-M-59', 'PL-M-66', 'PL-M-74', 'PL-M-83', 'PL-M-93', 'PL-M-105', 'PL-M-120', 'PL-M-120P',
                          'PL-W-47', 'PL-W-52', 'PL-W-57', 'PL-W-63', 'PL-W-69', 'PL-W-76', 'PL-W-84', 'PL-W-84P'
                      )
                  )
                  OR (
                      event."Discipline" = 'Bodybuilding'
                      AND category."Code" IN ('BB-01', 'BB-02', 'BB-03', 'BB-04', 'BB-05', 'BB-06', 'BB-07', 'BB-08')
                      AND category."BodybuildingDivision" = category."Name"
                  )
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Generated categories intentionally cannot be reconstructed because staff-owned category data is authoritative.
    }
}
