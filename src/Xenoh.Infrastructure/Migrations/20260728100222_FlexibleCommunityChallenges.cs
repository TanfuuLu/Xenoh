using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FlexibleCommunityChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FitnessChallenges_StartsOn_EndsOn",
                table: "FitnessChallenges");

            migrationBuilder.AddColumn<string>(
                name: "AccessType",
                table: "FitnessChallenges",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "InviteOnly");

            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "FitnessChallenges",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<string>(
                name: "CheckInPrompt",
                table: "FitnessChallenges",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FitnessChallenges",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndsAtUtc",
                table: "FitnessChallenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetricType",
                table: "FitnessChallenges",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "TrainingSessions");

            migrationBuilder.AddColumn<string>(
                name: "SelectedLifts",
                table: "FitnessChallenges",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartNotifiedAt",
                table: "FitnessChallenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartsAtUtc",
                table: "FitnessChallenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "FitnessChallenges",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "Asia/Ho_Chi_Minh");

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "FitnessChallenges",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.Sql(
                """
                UPDATE "FitnessChallenges" AS c
                SET "StartsAtUtc" = c."StartsOn"::timestamp AT TIME ZONE 'Asia/Ho_Chi_Minh',
                    "EndsAtUtc" = ((c."EndsOn" + 1)::timestamp AT TIME ZONE 'Asia/Ho_Chi_Minh') - interval '1 microsecond',
                    "Capacity" = GREATEST(
                        10,
                        (SELECT COUNT(*)::integer
                         FROM "FitnessChallengeMembers" AS m
                         WHERE m."ChallengeId" = c."Id"
                           AND m."Status" IN ('Invited', 'Accepted')))
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartsAtUtc",
                table: "FitnessChallenges",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndsAtUtc",
                table: "FitnessChallenges",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "EndsOn",
                table: "FitnessChallenges");

            migrationBuilder.DropColumn(
                name: "StartsOn",
                table: "FitnessChallenges");

            migrationBuilder.CreateTable(
                name: "FitnessChallengeCheckIns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChallengeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FitnessChallengeCheckIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FitnessChallengeCheckIns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FitnessChallengeCheckIns_FitnessChallenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "FitnessChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FitnessChallenges_AccessType_StartsAtUtc_EndsAtUtc",
                table: "FitnessChallenges",
                columns: new[] { "AccessType", "StartsAtUtc", "EndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FitnessChallengeCheckIns_ChallengeId_UserId_LocalDate",
                table: "FitnessChallengeCheckIns",
                columns: new[] { "ChallengeId", "UserId", "LocalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FitnessChallengeCheckIns_UserId_LocalDate",
                table: "FitnessChallengeCheckIns",
                columns: new[] { "UserId", "LocalDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FitnessChallengeCheckIns");

            migrationBuilder.DropIndex(
                name: "IX_FitnessChallenges_AccessType_StartsAtUtc_EndsAtUtc",
                table: "FitnessChallenges");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndsOn",
                table: "FitnessChallenges",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartsOn",
                table: "FitnessChallenges",
                type: "date",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "FitnessChallenges"
                SET "StartsOn" = ("StartsAtUtc" AT TIME ZONE "TimeZoneId")::date,
                    "EndsOn" = ("EndsAtUtc" AT TIME ZONE "TimeZoneId")::date
                """);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "StartsOn",
                table: "FitnessChallenges",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndsOn",
                table: "FitnessChallenges",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "AccessType",
                table: "FitnessChallenges");

            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "FitnessChallenges");

            migrationBuilder.DropColumn(
                name: "CheckInPrompt",
                table: "FitnessChallenges");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "FitnessChallenges");

            migrationBuilder.DropColumn(
                name: "EndsAtUtc",
                table: "FitnessChallenges");

            migrationBuilder.DropColumn(
                name: "MetricType",
                table: "FitnessChallenges");

            migrationBuilder.DropColumn(
                name: "SelectedLifts",
                table: "FitnessChallenges");

            migrationBuilder.DropColumn(
                name: "StartNotifiedAt",
                table: "FitnessChallenges");

            migrationBuilder.DropColumn(
                name: "StartsAtUtc",
                table: "FitnessChallenges");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "FitnessChallenges");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "FitnessChallenges");

            migrationBuilder.CreateIndex(
                name: "IX_FitnessChallenges_StartsOn_EndsOn",
                table: "FitnessChallenges",
                columns: new[] { "StartsOn", "EndsOn" });
        }
    }
}
