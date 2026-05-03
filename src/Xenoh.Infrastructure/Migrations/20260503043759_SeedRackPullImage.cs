using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRackPullImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ExerciseTemplates"
                SET "ImageUrl" = '/ExerciseImages/back/rackpull.png'
                WHERE "OwnerId" IS NULL AND "Name" = 'Rack Pull';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "ExerciseTemplates"
                SET "ImageUrl" = NULL
                WHERE "OwnerId" IS NULL AND "Name" = 'Rack Pull';
                """);
        }
    }
}
