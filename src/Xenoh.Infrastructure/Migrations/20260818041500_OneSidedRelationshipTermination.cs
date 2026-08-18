using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xenoh.Infrastructure.Migrations
{
    /// <summary>
    /// Disconnecting a coach-client relationship no longer needs the other party to
    /// approve, so PendingTermination (3) is never written again. Relationships left
    /// mid-negotiation go back to Active (1) rather than being ended, so nobody loses
    /// plans to a request they never agreed to; whoever wanted out can simply end it.
    /// </summary>
    public partial class OneSidedRelationshipTermination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "CoachClientRelationships"
                SET "Status" = 1,
                    "TerminationRequestedBy" = NULL,
                    "UpdatedAt" = NOW()
                WHERE "Status" = 3;
                """);

            migrationBuilder.Sql(
                """
                DELETE FROM "Notifications"
                WHERE "Type" IN ('DisconnectRequested', 'DisconnectRejected', 'DisconnectCancelled');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Pending termination requests cannot be reconstructed once cleared.
        }
    }
}
