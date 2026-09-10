using FluentAssertions;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Rules;
using Xunit;

namespace Xenoh.Application.Tests.Features.CoachClient;

public sealed class CoachingPolicyTests
{
    private static readonly DateTime Now = new(2026, 9, 7, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Inclusive_end_date_ends_at_next_midnight_in_Bangkok() =>
        CoachingPolicy.TermBoundary(new DateOnly(2026, 9, 7)).Should()
            .Be(new DateTime(2026, 9, 7, 17, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Notice_cannot_extend_the_term()
    {
        var relationship = Active();
        relationship.EndDate = new DateOnly(2026, 9, 8);
        CoachingPolicy.EndingAt(relationship, Now, false).Should()
            .Be(CoachingPolicy.TermBoundary(relationship.EndDate.Value));
    }

    [Fact]
    public void Safety_exit_and_scheduled_cancellation_are_immediate()
    {
        var relationship = Active();
        CoachingPolicy.EndingAt(relationship, Now, true).Should().Be(Now);
        relationship.StartDate = new DateOnly(2026, 9, 10);
        CoachingPolicy.EndingAt(relationship, Now, false).Should().Be(Now);
    }

    [Fact]
    public void Legacy_relationships_do_not_inherit_a_notice_period()
    {
        var relationship = Active();
        relationship.AgreementId = null;
        CoachingPolicy.EndingAt(relationship, Now, false).Should().Be(Now);
    }

    [Fact]
    public void Access_ends_at_deadline_even_before_worker_runs()
    {
        var relationship = Active();
        relationship.Status = RelationshipStatus.PendingRenewal;
        relationship.NoticeEndsAtUtc = Now;
        CoachingPolicy.HasAccess(relationship, Now).Should().BeFalse();
        CoachingPolicy.HasAccess(relationship, Now.AddTicks(-1)).Should().BeTrue();
    }

    [Fact]
    public void Future_start_never_grants_early_access()
    {
        var relationship = Active();
        relationship.StartDate = new DateOnly(2026, 9, 8);
        CoachingPolicy.HasAccess(relationship, Now).Should().BeFalse();
    }

    private static CoachClientRelationship Active() => new()
    {
        Status = RelationshipStatus.Active,
        StartDate = new DateOnly(2026, 9, 1),
        EndDate = new DateOnly(2026, 10, 1),
        AgreementId = Guid.NewGuid(),
        NoticeDays = 7
    };
}
