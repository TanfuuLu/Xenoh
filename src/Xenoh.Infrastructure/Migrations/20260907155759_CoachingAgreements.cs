using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CoachingAgreements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CoachingRelationshipId",
                table: "Plans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCoachingArchived",
                table: "Plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAtUtc",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceEventId",
                table: "Notifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AgreementId",
                table: "CoachInviteCodes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AgreementId",
                table: "CoachClientRelationships",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndedAtUtc",
                table: "CoachClientRelationships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NoticeDays",
                table: "CoachClientRelationships",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NoticeEndsAtUtc",
                table: "CoachClientRelationships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Revision",
                table: "CoachClientRelationships",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "CoachingAgreementEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipId = table.Column<Guid>(type: "uuid", nullable: false),
                    AgreementId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Kind = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachingAgreementEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachingAgreementEvents_CoachClientRelationships_Relationsh~",
                        column: x => x.RelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CoachingAgreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoachId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelationshipId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Goals = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Services = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CheckInFrequency = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Availability = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CoachResponsibilities = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ClientResponsibilities = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PolicyVersion = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TimeZone = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NoticeDays = table.Column<int>(type: "integer", nullable: false),
                    PublishedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoachingAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoachingAgreements_CoachClientRelationships_RelationshipId",
                        column: x => x.RelationshipId,
                        principalTable: "CoachClientRelationships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DeliveredAtUtc",
                table: "Notifications",
                column: "DeliveredAtUtc",
                filter: "\"SourceEventId\" IS NOT NULL AND \"DeliveredAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SourceEventId_RecipientId",
                table: "Notifications",
                columns: new[] { "SourceEventId", "RecipientId" },
                unique: true,
                filter: "\"SourceEventId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CoachInviteCodes_AgreementId",
                table: "CoachInviteCodes",
                column: "AgreementId");

            migrationBuilder.CreateIndex(
                name: "IX_CoachingAgreementEvents_RelationshipId_OccurredAtUtc",
                table: "CoachingAgreementEvents",
                columns: new[] { "RelationshipId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CoachingAgreements_RelationshipId_Version",
                table: "CoachingAgreements",
                columns: new[] { "RelationshipId", "Version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CoachInviteCodes_CoachingAgreements_AgreementId",
                table: "CoachInviteCodes",
                column: "AgreementId",
                principalTable: "CoachingAgreements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Bind retained plans to their original relationship without inventing legacy acceptance.
            migrationBuilder.Sql("""
                UPDATE "Plans" p SET "CoachingRelationshipId" = (
                    SELECT r."Id" FROM "CoachClientRelationships" r
                    WHERE r."ClientId" = p."OwnerId" AND r."CoachId" = p."CreatedByCoachId"
                    ORDER BY (r."CreatedAt" <= p."CreatedAt") DESC, r."CreatedAt" DESC LIMIT 1
                ) WHERE p."PlanType" = 1;

                UPDATE "Plans" p SET "IsCoachingArchived" = TRUE, "IsActive" = FALSE
                WHERE p."PlanType" = 1 AND NOT EXISTS (
                    SELECT 1 FROM "CoachClientRelationships" r
                    WHERE r."Id" = p."CoachingRelationshipId" AND r."Status" IN (1, 3, 5)
                    AND (r."EndDate" IS NULL OR r."EndDate" >= (CURRENT_TIMESTAMP AT TIME ZONE 'Asia/Bangkok')::date)
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoachInviteCodes_CoachingAgreements_AgreementId",
                table: "CoachInviteCodes");

            migrationBuilder.DropTable(
                name: "CoachingAgreementEvents");

            migrationBuilder.DropTable(
                name: "CoachingAgreements");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_DeliveredAtUtc",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_SourceEventId_RecipientId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_CoachInviteCodes_AgreementId",
                table: "CoachInviteCodes");

            migrationBuilder.DropColumn(
                name: "CoachingRelationshipId",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "IsCoachingArchived",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "DeliveredAtUtc",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "SourceEventId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "AgreementId",
                table: "CoachInviteCodes");

            migrationBuilder.DropColumn(
                name: "AgreementId",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "EndedAtUtc",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "NoticeDays",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "NoticeEndsAtUtc",
                table: "CoachClientRelationships");

            migrationBuilder.DropColumn(
                name: "Revision",
                table: "CoachClientRelationships");
        }
    }
}
