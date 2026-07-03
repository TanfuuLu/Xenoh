using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionExpiryReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryReminderSentAt",
                table: "UserSubscriptions",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiryReminderSentAt",
                table: "UserSubscriptions");
        }
    }
}
