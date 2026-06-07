using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiUsageQuotasAndFeatureUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiFeatureUsages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    Feature = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UsedRequests = table.Column<int>(type: "integer", nullable: false),
                    LastConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiFeatureUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiFeatureUsages_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AiUsageQuotas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    UsedRequests = table.Column<int>(type: "integer", nullable: false),
                    LastFeature = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    LastConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsageQuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiUsageQuotas_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiFeatureUsages_PeriodStart_Feature",
                table: "AiFeatureUsages",
                columns: new[] { "PeriodStart", "Feature" });

            migrationBuilder.CreateIndex(
                name: "IX_AiFeatureUsages_UserId_PeriodStart_Feature",
                table: "AiFeatureUsages",
                columns: new[] { "UserId", "PeriodStart", "Feature" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageQuotas_UserId_PeriodStart",
                table: "AiUsageQuotas",
                columns: new[] { "UserId", "PeriodStart" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiFeatureUsages");

            migrationBuilder.DropTable(
                name: "AiUsageQuotas");
        }
    }
}
