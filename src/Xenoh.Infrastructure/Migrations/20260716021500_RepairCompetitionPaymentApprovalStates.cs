using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xenoh.Infrastructure.Persistence;

#nullable disable

namespace Xenoh.Infrastructure.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260716021500_RepairCompetitionPaymentApprovalStates")]
public sealed class RepairCompetitionPaymentApprovalStates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "CompetitionRegistrations"
            SET "Status" = 'Submitted',
                "ReviewedAt" = NULL,
                "ReviewedById" = NULL,
                "DecisionReason" = NULL,
                "UpdatedAt" = NOW()
            WHERE "Status" = 'Approved'
              AND "ExpectedFee" > 0
              AND "PaymentStatus" <> 'Paid';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Approval cannot be restored safely without a verified payment record.
    }
}
