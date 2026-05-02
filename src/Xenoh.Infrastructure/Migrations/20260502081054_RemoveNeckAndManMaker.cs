using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNeckAndManMaker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM "ExerciseTemplates"
                WHERE "OwnerId" IS NULL
                  AND "Name" IN (
                    'Neck Flexion',
                    'Neck Extension',
                    'Lateral Neck Flexion',
                    'Neck Harness Extension',
                    'Man Maker'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
