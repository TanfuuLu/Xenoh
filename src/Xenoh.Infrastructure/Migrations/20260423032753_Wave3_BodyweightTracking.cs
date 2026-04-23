using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Wave3_BodyweightTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use conditional DDL — columns/tables may already exist from a previous worktree branch
            migrationBuilder.Sql(@"
                ALTER TABLE ""AspNetUsers"" ADD COLUMN IF NOT EXISTS ""DateOfBirth"" date;
                ALTER TABLE ""AspNetUsers"" ADD COLUMN IF NOT EXISTS ""Gender"" integer;
                ALTER TABLE ""AspNetUsers"" ADD COLUMN IF NOT EXISTS ""Height"" numeric;
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""WorkoutHistories"" (
                    ""Id"" uuid NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""Date"" date NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""UpdatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_WorkoutHistories"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_WorkoutHistories_AspNetUsers_UserId""
                        FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE
                );
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_WorkoutHistories_UserId_Date""
                ON ""WorkoutHistories""(""UserId"", ""Date"");
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""BodyweightLogs"" (
                    ""Id"" uuid NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""Weight"" numeric(6,2) NOT NULL,
                    ""Date"" date NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""UpdatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_BodyweightLogs"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_BodyweightLogs_AspNetUsers_UserId""
                        FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers""(""Id"") ON DELETE CASCADE
                );
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ""IX_BodyweightLogs_UserId_Date""
                ON ""BodyweightLogs""(""UserId"", ""Date"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BodyweightLogs");

            migrationBuilder.DropTable(
                name: "WorkoutHistories");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "AspNetUsers");
        }
    }
}
