using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFeatureCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiFeatureCaches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Feature = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiFeatureCaches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiFeatureCaches_AspNetUsers_SubjectUserId",
                        column: x => x.SubjectUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiFeatureCaches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiFeatureCaches_Feature_Language_UserId_SubjectUserId_Resou~",
                table: "AiFeatureCaches",
                columns: new[] { "Feature", "Language", "UserId", "SubjectUserId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiFeatureCaches_SubjectUserId",
                table: "AiFeatureCaches",
                column: "SubjectUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiFeatureCaches_UserId",
                table: "AiFeatureCaches",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiFeatureCaches");
        }
    }
}
