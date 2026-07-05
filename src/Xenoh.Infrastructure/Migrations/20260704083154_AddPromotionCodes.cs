using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "PaymentOrders",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "PromotionCodeId",
                table: "PaymentOrders",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PromotionCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DiscountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AppliesToTier = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    MaxRedemptions = table.Column<int>(type: "integer", nullable: true),
                    MaxRedemptionsPerUser = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOrders_PromotionCodeId",
                table: "PaymentOrders",
                column: "PromotionCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCodes_Code",
                table: "PromotionCodes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentOrders_PromotionCodes_PromotionCodeId",
                table: "PaymentOrders",
                column: "PromotionCodeId",
                principalTable: "PromotionCodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentOrders_PromotionCodes_PromotionCodeId",
                table: "PaymentOrders");

            migrationBuilder.DropTable(
                name: "PromotionCodes");

            migrationBuilder.DropIndex(
                name: "IX_PaymentOrders_PromotionCodeId",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "PaymentOrders");

            migrationBuilder.DropColumn(
                name: "PromotionCodeId",
                table: "PaymentOrders");
        }
    }
}
