using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DefaultUserPreferencesToEnglishLight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PreferredLanguage",
                table: "AspNetUsers",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "en",
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldDefaultValue: "vi");

            migrationBuilder.Sql("""
                UPDATE "AspNetUsers"
                SET "PreferredLanguage" = 'en',
                    "PreferredTheme" = 'light'
                WHERE "PreferredLanguage" = 'vi'
                  AND "PreferredTheme" = 'light';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PreferredLanguage",
                table: "AspNetUsers",
                type: "character varying(2)",
                maxLength: 2,
                nullable: false,
                defaultValue: "vi",
                oldClrType: typeof(string),
                oldType: "character varying(2)",
                oldMaxLength: 2,
                oldDefaultValue: "en");
        }
    }
}
