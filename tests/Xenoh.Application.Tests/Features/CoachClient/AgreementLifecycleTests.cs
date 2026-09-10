using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Exceptions;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.CoachClient;
using Xenoh.Application.Features.CoachClient.Agreements;
using Xenoh.Application.Features.CoachClient.Commands.AutoExpireContracts;
using Xenoh.Application.Features.CoachClient.Commands.ConnectByInviteCode;
using Xenoh.Application.Features.CoachClient.Commands.EndRelationship;
using Xenoh.Application.Features.CoachClient.Commands.GenerateInviteCode;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Rules;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Xenoh.Application.Tests.Features.CoachClient;

public sealed class AgreementLifecycleTests : IdentityHandlerTestBase
{
    private readonly Guid coachId = Guid.NewGuid();
    private static DateOnly Today => CoachingPolicy.LocalDate(DateTime.UtcNow);

    [Fact]
    public async Task Preview_is_read_only_and_acceptance_requires_the_exact_reviewed_version()
    {
        var invite = await PublishAsync();
        await using var db = CreateContext();
        var preview = await new PreviewAgreementHandler(db, CurrentUser()).Handle(new(invite.Code), default);
        (await db.CoachInviteCodes.SingleAsync()).IsUsed.Should().BeFalse();
        (await db.CoachClientRelationships.CountAsync()).Should().Be(0);
        var connect = Connect(db);
        var stale = async () => await connect.Handle(new() { Code = invite.Code, AgreementId = Guid.NewGuid(), Acknowledged = true }, default);
        await stale.Should().ThrowAsync<AgreementConflictException>();
        var connected = await connect.Handle(new() { Code = invite.Code, AgreementId = preview.Agreement.Id, Acknowledged = true }, default);
        var accepted = await db.CoachingAgreements.SingleAsync();
        accepted.AcceptedBy.Should().Be(UserId);
        accepted.AcceptedAtUtc.Should().NotBeNull();
        accepted.RelationshipId.Should().Be(connected.Id);
        (await db.Notifications.CountAsync()).Should().Be(2);
        var retried = await connect.Handle(new() { Code = invite.Code, AgreementId = preview.Agreement.Id, Acknowledged = true }, default);
        retried.Id.Should().Be(connected.Id);
        (await db.CoachClientRelationships.CountAsync()).Should().Be(1);
        (await db.Notifications.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Notice_uses_preview_deadline_and_safety_exit_preserves_sets_but_revokes_access()
    {
        var r = await ConnectAsync();
        var planId = await SeedPlanAsync(r.Id);
        await using (var db = CreateContext())
        {
            var preview = await new PreviewEndingHandler(db, CurrentUser()).Handle(new(r.Id), default);
            await End(db).Handle(new() { RelationshipId = r.Id, ExpectedRevision = preview.Revision, RequestedAtUtc = preview.RequestedAtUtc, EffectiveAtUtc = preview.EffectiveAtUtc }, default);
            var ending = await db.CoachClientRelationships.SingleAsync();
            ending.Status.Should().Be(RelationshipStatus.PendingTermination);
            ending.NoticeEndsAtUtc.Should().Be(preview.EffectiveAtUtc);
            (await new CoachClientRepository(db).HasActiveRelationshipAsync(coachId, UserId, default)).Should().BeTrue();
            await End(db).Handle(new() { RelationshipId = r.Id, ExpectedRevision = ending.Revision, Immediate = true }, default);
            await End(db).Handle(new() { RelationshipId = r.Id, ExpectedRevision = ending.Revision, Immediate = true }, default);
        }
        await using var verify = CreateContext();
        var plan = await verify.Plans.SingleAsync(p => p.Id == planId);
        plan.IsCoachingArchived.Should().BeTrue();
        plan.CreatedByCoachId.Should().Be(coachId);
        plan.IsActive.Should().BeFalse();
        var set = await verify.ExerciseSets.SingleAsync();
        set.ActualReps.Should().Be(5);
        set.ActualWeight.Should().Be(100);
        (await new CoachClientRepository(verify).HasActiveRelationshipAsync(coachId, UserId, default)).Should().BeFalse();
        (await new PlanRepository(verify).GetByIdForUserAsync(planId, coachId, default)).Should().BeNull();
        (await new PlanRepository(verify).GetByIdForUserAsync(planId, UserId, default)).Should().NotBeNull();
        (await new ExerciseSetRepository(verify).FindForCompleteAsync(set.Id, default)).Should().BeNull();
        (await verify.CoachingAgreementEvents.CountAsync(e => e.Kind == "Ended")).Should().Be(1);
        (await new GetAgreementHandler(verify, new FakeCurrentUserService(coachId)).Handle(new(r.Id), default)).Status.Should().Be("Ended");
    }

    [Fact]
    public async Task Expired_access_is_denied_without_waiting_for_finalization_even_with_pending_renewal()
    {
        var r = await ConnectAsync();
        var planId = await SeedPlanAsync(r.Id);
        await using var db = CreateContext();
        var entity = await db.CoachClientRelationships.SingleAsync();
        entity.Status = RelationshipStatus.PendingRenewal;
        entity.EndDate = Today.AddDays(-1);
        await db.SaveChangesAsync();
        (await new PlanRepository(db).GetByIdForUserAsync(planId, coachId, default)).Should().BeNull();
        (await new CoachClientRepository(db).FindActiveByCoachAndClientAsync(coachId, UserId, default)).Should().BeNull();
        var worker = new AutoExpireContractsHandler(db, new FakeNotificationService(), new PlanRepository(db), new SupplementRepository(db));
        (await worker.Handle(new(), default)).Should().Be(1);
        (await worker.Handle(new(), default)).Should().Be(0);
        (await db.ExerciseSets.SingleAsync()).ActualWeight.Should().Be(100);
    }

    [Fact]
    public async Task Future_agreement_is_reserved_but_has_no_access_and_can_be_cancelled_immediately()
    {
        var r = await ConnectAsync(Today.AddDays(3));
        await using var db = CreateContext();
        (await new GetAgreementHandler(db, CurrentUser()).Handle(new(r.Id), default)).Status.Should().Be("Scheduled");
        (await new CoachClientRepository(db).HasActiveRelationshipAsync(coachId, UserId, default)).Should().BeFalse();
        var preview = await new PreviewEndingHandler(db, CurrentUser()).Handle(new(r.Id), default);
        preview.EffectiveAtUtc.Should().Be(preview.RequestedAtUtc);
    }

    [Fact]
    public async Task Unrelated_user_cannot_read_or_end_an_agreement()
    {
        var r = await ConnectAsync();
        await using var db = CreateContext();
        var stranger = new FakeCurrentUserService(Guid.NewGuid());
        var read = async () => await new GetAgreementHandler(db, stranger).Handle(new(r.Id), default);
        await read.Should().ThrowAsync<KeyNotFoundException>();
        var preview = async () => await new PreviewEndingHandler(db, stranger).Handle(new(r.Id), default);
        await preview.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Concurrent_redemption_has_one_winner()
    {
        var invite = await PublishAsync();
        await using var first = CreateContext();
        await using var second = CreateContext();
        var firstInvite = await first.CoachInviteCodes.SingleAsync();
        var secondInvite = await second.CoachInviteCodes.SingleAsync();
        firstInvite.IsUsed = true;
        await first.SaveChangesAsync();
        secondInvite.IsUsed = true;
        var save = () => second.SaveChangesAsync();
        await save.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public void Access_queries_translate_to_PostgreSql_without_client_evaluation()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_only;Username=test;Password=test").Options);
        var coach = coachId;
        var sql = db.Plans.Where(p => p.OwnerId == UserId || (p.CreatedByCoachId == coach
            && db.Plans.WritableCoachingPlans(db).Any(access => access.Id == p.Id))).ToQueryString();
        sql.Should().Contain("NoticeEndsAtUtc").And.Contain("StartDate");
    }

    [Fact]
    public async Task Renewal_keeps_old_snapshot_and_requires_the_other_participants_acceptance()
    {
        var connected = await ConnectAsync();
        await using var db = CreateContext();
        var r = await db.CoachClientRelationships.SingleAsync();
        var original = await db.CoachingAgreements.SingleAsync();
        var oldEnd = original.EndDate;
        var proposal = await new ProposeAgreementHandler(db, CurrentUser(), new FakeNotificationService()).Handle(new()
        {
            RelationshipId = r.Id, ExpectedRevision = r.Revision, EndDate = oldEnd.AddDays(30), Acknowledged = true
        }, default);
        r.EndDate.Should().Be(oldEnd);
        proposal.NoticeDays.Should().Be(original.NoticeDays);
        var selfAccept = async () => await new RespondToAgreementHandler(db, CurrentUser(), new UnlimitedSubscription(), new FakeNotificationService())
            .Handle(new(r.Id, proposal.Id, r.Revision, true, true), default);
        await selfAccept.Should().ThrowAsync<InvalidOperationException>();
        await new RespondToAgreementHandler(db, new FakeCurrentUserService(coachId), new UnlimitedSubscription(), new FakeNotificationService())
            .Handle(new(r.Id, proposal.Id, r.Revision, true, true), default);
        r.EndDate.Should().Be(oldEnd.AddDays(30));
        original.EndDate.Should().Be(oldEnd);
        (await db.CoachingAgreements.CountAsync(a => a.AcceptedAtUtc != null)).Should().Be(2);
        (await new GetAgreementHandler(db, CurrentUser()).Handle(new(connected.Id), default)).Agreements.Should().HaveCount(2);
    }

    [Fact]
    public async Task Stale_ending_preview_does_not_change_the_agreement()
    {
        var connected = await ConnectAsync();
        await using var db = CreateContext();
        var preview = await new PreviewEndingHandler(db, CurrentUser()).Handle(new(connected.Id), default);
        var end = async () => await End(db).Handle(new()
        {
            RelationshipId = connected.Id, ExpectedRevision = Guid.NewGuid(), RequestedAtUtc = preview.RequestedAtUtc, EffectiveAtUtc = preview.EffectiveAtUtc
        }, default);
        await end.Should().ThrowAsync<AgreementConflictException>();
        (await db.CoachClientRelationships.SingleAsync()).NoticeEndsAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Legacy_invitation_cannot_be_redeemed_without_published_terms()
    {
        await SeedUserAsync(UserId, "client@legacy.test", "password", "Client");
        await SeedUserAsync(coachId, "coach@legacy.test", "password", "Coach");
        await using var db = CreateContext();
        db.CoachInviteCodes.Add(new() { Code = "LEGACYAB", CoachId = coachId, CoachingStartDate = Today, CoachingEndDate = Today.AddDays(30) });
        await db.SaveChangesAsync();
        var preview = async () => await new PreviewAgreementHandler(db, CurrentUser()).Handle(new("LEGACYAB"), default);
        await preview.Should().ThrowAsync<InvalidOperationException>();
        var accept = async () => await Connect(db).Handle(new() { Code = "LEGACYAB", Acknowledged = true }, default);
        await accept.Should().ThrowAsync<AgreementConflictException>();
        (await db.CoachClientRelationships.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Ending_rejects_pending_renewal_and_it_cannot_restore_access()
    {
        var connected = await ConnectAsync();
        await using var db = CreateContext();
        var r = await db.CoachClientRelationships.SingleAsync();
        var proposal = await new ProposeAgreementHandler(db, CurrentUser(), new FakeNotificationService()).Handle(new()
        {
            RelationshipId = r.Id, ExpectedRevision = r.Revision, EndDate = r.EndDate!.Value.AddDays(30), Acknowledged = true
        }, default);
        await End(db).Handle(new() { RelationshipId = connected.Id, ExpectedRevision = r.Revision, Immediate = true }, default);
        var accept = async () => await new RespondToAgreementHandler(db, new FakeCurrentUserService(coachId), new UnlimitedSubscription(), new FakeNotificationService())
            .Handle(new(r.Id, proposal.Id, r.Revision, true, true), default);
        await accept.Should().ThrowAsync<AgreementConflictException>();
        CoachingPolicy.HasAccess(r, DateTime.UtcNow).Should().BeFalse();
    }

    private async Task<CoachInviteCodeDto> PublishAsync(DateOnly? start = null)
    {
        await SeedUserAsync(UserId, "client@agreement.test", "password", "Client");
        await SeedUserAsync(coachId, "coach@agreement.test", "password", "Coach");
        await using var db = CreateContext();
        return await new GenerateInviteCodeHandler(db, new FakeCurrentUserService(coachId)).Handle(new()
        {
            CoachingStartDate = start ?? Today, CoachingEndDate = Today.AddDays(30), Acknowledged = true,
            Terms = new() { Title = "Strength coaching", Goals = "Build strength", Services = "Weekly plans", CheckInFrequency = "Weekly",
                Availability = "Weekdays, within 48 hours", CoachResponsibilities = "Review progress", ClientResponsibilities = "Log training", NoticeDays = 7 }
        }, default);
    }

    private async Task<CoachRelationshipResponse> ConnectAsync(DateOnly? start = null)
    {
        var invite = await PublishAsync(start);
        await using var db = CreateContext();
        var agreementId = (await db.CoachInviteCodes.SingleAsync()).AgreementId!.Value;
        return await Connect(db).Handle(new() { Code = invite.Code, AgreementId = agreementId, Acknowledged = true }, default);
    }

    private ConnectByInviteCodeHandler Connect(ApplicationDbContext db) => new(db, new PlanRepository(db), new SupplementRepository(db), CurrentUser(), CreateUserManager(), new UnlimitedSubscription(), new FakeNotificationService());
    private EndRelationshipHandler End(ApplicationDbContext db) => new(new CoachClientRepository(db), new PlanRepository(db), new SupplementRepository(db), db, CurrentUser(), new FakeNotificationService(), CreateUserManager());

    private async Task<Guid> SeedPlanAsync(Guid relationshipId)
    {
        await using var db = CreateContext();
        var template = new ExerciseTemplate { Name = "Squat" };
        db.ExerciseTemplates.Add(template);
        var plan = new Plan { Name = "Coached plan", OwnerId = UserId, CreatedByCoachId = coachId, CoachingRelationshipId = relationshipId,
            PlanType = PlanType.Coach, StartDate = Today, EndDate = Today.AddDays(30), IsActive = true };
        var week = new WeeklyWorkout { Plan = plan };
        var day = new DailyWorkout { WeeklyWorkout = week, Date = Today };
        var exercise = new Exercise { DailyWorkout = day, ExerciseTemplate = template, Name = "Squat" };
        db.ExerciseSets.Add(new ExerciseSet { Exercise = exercise, IsCompleted = true, ActualReps = 5, ActualWeight = 100 });
        await db.SaveChangesAsync();
        return plan.Id;
    }

    private sealed class UnlimitedSubscription : ISubscriptionService
    {
        public Task<PlanTier> GetActiveTierAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(PlanTier.ProCoach);
        public Task<int> GetMaxPlansAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(int.MaxValue);
        public Task<int> GetMaxClientsAsync(Guid coachId, CancellationToken ct = default) => Task.FromResult(int.MaxValue);
        public Task<bool> CanUseAdvancedAnalyticsAsync(Guid userId, CancellationToken ct = default) => Task.FromResult(true);
    }
}
