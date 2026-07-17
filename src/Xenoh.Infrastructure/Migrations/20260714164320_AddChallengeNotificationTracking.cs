using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChallengeNotificationTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletionNotifiedAt",
                table: "FitnessChallenges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastBehindReminderWeekStart",
                table: "FitnessChallengeMembers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastCompletionNotificationWeekStart",
                table: "FitnessChallengeMembers",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionNotifiedAt",
                table: "FitnessChallenges");

            migrationBuilder.DropColumn(
                name: "LastBehindReminderWeekStart",
                table: "FitnessChallengeMembers");

            migrationBuilder.DropColumn(
                name: "LastCompletionNotificationWeekStart",
                table: "FitnessChallengeMembers");
        }
    }
}
