using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Features.Supplements;
using Xenoh.Application.Features.Supplements.Commands;
using Xenoh.Application.Features.Supplements.Queries;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Xenoh.Application.Tests.Features.Supplements;

public sealed class SupplementHandlerTests : HandlerTestBase
{
    private readonly Guid CoachId = Guid.NewGuid();
    private readonly Guid StrangerId = Guid.NewGuid();
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Create_ThenGetDaily_ReturnsScheduledDose()
    {
        await SeedUsersAsync();
        await using var context = CreateContext();

        var regimen = await CreateHandler(context, UserId).Handle(
            new CreateSupplementRegimenCommand(CreateRequest()),
            CancellationToken.None);
        var daily = await DailyHandler(context, UserId).Handle(
            new GetSupplementDailyQuery(Today),
            CancellationToken.None);

        regimen.Name.Should().Be("Creatine");
        regimen.DoseSlots.Should().ContainSingle();
        daily.Doses.Should().ContainSingle();
        daily.Doses[0].Status.Should().Be(SupplementDoseStatus.Pending);
        daily.Totals.Planned.Should().Be(1);
    }

    [Fact]
    public async Task Create_WithOverlappingDoseSlots_Throws()
    {
        await SeedUsersAsync();
        await using var context = CreateContext();
        var request = CreateRequest() with
        {
            DoseSlots =
            [
                Dose(5, "g", new TimeOnly(8, 0)),
                Dose(3, "g", new TimeOnly(8, 0))
            ]
        };

        var act = () => CreateHandler(context, UserId)
            .Handle(new CreateSupplementRegimenCommand(request), CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*overlapping weekdays*");
    }

    [Fact]
    public async Task Update_ReplacesFutureRevision_AndPreservesCurrentSchedule()
    {
        await SeedUsersAsync();
        Guid regimenId;
        await using (var createContext = CreateContext())
        {
            regimenId = (await CreateHandler(createContext, UserId).Handle(
                new CreateSupplementRegimenCommand(CreateRequest()),
                CancellationToken.None)).Id;
        }

        await using (var firstUpdateContext = CreateContext())
        {
            await UpdateHandler(firstUpdateContext, UserId).Handle(
                new UpdateSupplementRegimenCommand(
                    regimenId,
                    UpdateRequest(Today.AddDays(1), 3m)),
                CancellationToken.None);
        }

        await using (var secondUpdateContext = CreateContext())
        {
            await UpdateHandler(secondUpdateContext, UserId).Handle(
                new UpdateSupplementRegimenCommand(
                    regimenId,
                    UpdateRequest(Today.AddDays(2), 4m)),
                CancellationToken.None);
        }

        await using var verify = CreateContext();
        var versions = await verify.SupplementScheduleVersions
            .Include(x => x.DoseSlots)
            .Where(x => x.RegimenId == regimenId)
            .OrderBy(x => x.EffectiveFrom)
            .ToListAsync();

        versions.Should().HaveCount(2);
        versions[0].EffectiveTo.Should().Be(Today.AddDays(1));
        versions[0].DoseSlots.Single().Amount.Should().Be(5m);
        versions[1].EffectiveFrom.Should().Be(Today.AddDays(2));
        versions[1].DoseSlots.Single().Amount.Should().Be(4m);
    }

    [Fact]
    public async Task Coach_WithActiveRelationship_CanManageClientRegimen()
    {
        await SeedUsersAsync(withRelationship: true);
        await using var context = CreateContext();

        var result = await CreateHandler(context, CoachId).Handle(
            new CreateSupplementRegimenCommand(CreateRequest(), UserId),
            CancellationToken.None);

        result.UserId.Should().Be(UserId);
        (await context.SupplementRegimens.SingleAsync()).CreatedByUserId.Should().Be(CoachId);
    }

    [Fact]
    public async Task UnrelatedUser_CannotReadClientRegimens()
    {
        await SeedUsersAsync();
        await using var context = CreateContext();

        var act = () => RegimensHandler(context, StrangerId)
            .Handle(new GetSupplementRegimensQuery(UserId: UserId), CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Client_CannotUseCoachRouteDirection_ToManageCoachData()
    {
        await SeedUsersAsync(withRelationship: true);
        await using var context = CreateContext();

        var act = () => CreateHandler(context, UserId)
            .Handle(
                new CreateSupplementRegimenCommand(CreateRequest(), CoachId),
                CancellationToken.None)
            .AsTask();

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RecordDose_IsIdempotent_AndResetRemovesStatus()
    {
        await SeedUsersAsync();
        Guid slotId;
        await using (var createContext = CreateContext())
        {
            slotId = (await CreateHandler(createContext, UserId).Handle(
                new CreateSupplementRegimenCommand(CreateRequest()),
                CancellationToken.None)).DoseSlots.Single().Id;
        }

        await using (var recordContext = CreateContext())
        {
            var handler = RecordHandler(recordContext, UserId);
            await handler.Handle(
                new RecordSupplementDoseCommand(
                    slotId,
                    Today,
                    new RecordSupplementDoseRequest(SupplementIntakeStatus.Taken, null, "Morning")),
                CancellationToken.None);
            await handler.Handle(
                new RecordSupplementDoseCommand(
                    slotId,
                    Today,
                    new RecordSupplementDoseRequest(SupplementIntakeStatus.Skipped, null, null)),
                CancellationToken.None);

            (await recordContext.SupplementIntakeLogs.CountAsync()).Should().Be(1);
            (await recordContext.SupplementIntakeLogs.SingleAsync()).Status
                .Should().Be(SupplementIntakeStatus.Skipped);
        }

        await using (var resetContext = CreateContext())
        {
            await ResetHandler(resetContext, UserId).Handle(
                new ResetSupplementDoseCommand(slotId, Today),
                CancellationToken.None);
            (await resetContext.SupplementIntakeLogs.CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task History_CalculatesTakenAndMissedAdherence()
    {
        await SeedUsersAsync();
        var yesterday = Today.AddDays(-1);
        Guid slotId;
        await using (var setup = CreateContext())
        {
            var request = CreateRequest() with { StartDate = yesterday };
            // Backdated creation is intentionally rejected, so seed a stable historical regimen.
            var regimen = new SupplementRegimen { UserId = UserId, Name = "Creatine" };
            var version = new SupplementScheduleVersion
            {
                EffectiveFrom = yesterday,
                DoseSlots = [new SupplementDoseSlot
                {
                    Amount = 5,
                    Unit = "g",
                    Time = new TimeOnly(8, 0),
                    Weekdays = SupplementWeekdays.EveryDay
                }]
            };
            regimen.ScheduleVersions.Add(version);
            setup.SupplementRegimens.Add(regimen);
            await setup.SaveChangesAsync();
            slotId = version.DoseSlots.Single().Id;
        }

        await using (var record = CreateContext())
        {
            await RecordHandler(record, UserId).Handle(
                new RecordSupplementDoseCommand(
                    slotId,
                    Today,
                    new RecordSupplementDoseRequest(SupplementIntakeStatus.Taken, null, null)),
                CancellationToken.None);
        }

        await using var query = CreateContext();
        var history = await HistoryHandler(query, UserId).Handle(
            new GetSupplementHistoryQuery(yesterday, Today),
            CancellationToken.None);

        history.Totals.Planned.Should().Be(2);
        history.Totals.Taken.Should().Be(1);
        history.Totals.Missed.Should().Be(1);
        history.Totals.AdherencePercentage.Should().Be(50m);
    }

    [Fact]
    public async Task Archive_EndsScheduleToday_AndKeepsHistory()
    {
        await SeedUsersAsync();
        Guid regimenId;
        await using (var create = CreateContext())
        {
            regimenId = (await CreateHandler(create, UserId).Handle(
                new CreateSupplementRegimenCommand(CreateRequest()),
                CancellationToken.None)).Id;
        }

        await using (var archive = CreateContext())
        {
            await ArchiveHandler(archive, UserId).Handle(
                new ArchiveSupplementRegimenCommand(regimenId),
                CancellationToken.None);
        }

        await using var verify = CreateContext();
        var regimen = await verify.SupplementRegimens
            .Include(x => x.ScheduleVersions)
            .SingleAsync(x => x.Id == regimenId);
        regimen.IsArchived.Should().BeTrue();
        regimen.ScheduleVersions.Single().EffectiveTo.Should().Be(Today);
    }

    [Fact]
    public async Task Delete_RemovesRegimenAndItsIntakeHistory()
    {
        await SeedUsersAsync();
        Guid regimenId;
        await using (var create = CreateContext())
        {
            var regimen = await CreateHandler(create, UserId).Handle(
                new CreateSupplementRegimenCommand(CreateRequest()),
                CancellationToken.None);
            regimenId = regimen.Id;

            await RecordHandler(create, UserId).Handle(
                new RecordSupplementDoseCommand(
                    regimen.DoseSlots.Single().Id,
                    Today,
                    new RecordSupplementDoseRequest(SupplementIntakeStatus.Taken, null, null)),
                CancellationToken.None);
        }

        await using (var delete = CreateContext())
        {
            await DeleteHandler(delete, UserId).Handle(
                new DeleteSupplementRegimenCommand(regimenId),
                CancellationToken.None);
        }

        await using var verify = CreateContext();
        (await verify.SupplementRegimens.AnyAsync()).Should().BeFalse();
        (await verify.SupplementScheduleVersions.AnyAsync()).Should().BeFalse();
        (await verify.SupplementDoseSlots.AnyAsync()).Should().BeFalse();
        (await verify.SupplementIntakeLogs.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_SucceedsOnAnArchivedRegimen()
    {
        await SeedUsersAsync();
        Guid regimenId;
        await using (var create = CreateContext())
        {
            regimenId = (await CreateHandler(create, UserId).Handle(
                new CreateSupplementRegimenCommand(CreateRequest()),
                CancellationToken.None)).Id;
        }

        await using (var archive = CreateContext())
        {
            await ArchiveHandler(archive, UserId).Handle(
                new ArchiveSupplementRegimenCommand(regimenId),
                CancellationToken.None);
        }

        await using (var delete = CreateContext())
        {
            await DeleteHandler(delete, UserId).Handle(
                new DeleteSupplementRegimenCommand(regimenId),
                CancellationToken.None);
        }

        await using var verify = CreateContext();
        (await verify.SupplementRegimens.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ByAnUnrelatedUser_LeavesTheRegimenInPlace()
    {
        await SeedUsersAsync();
        Guid regimenId;
        await using (var create = CreateContext())
        {
            regimenId = (await CreateHandler(create, UserId).Handle(
                new CreateSupplementRegimenCommand(CreateRequest()),
                CancellationToken.None)).Id;
        }

        await using (var delete = CreateContext())
        {
            var act = () => DeleteHandler(delete, StrangerId)
                .Handle(new DeleteSupplementRegimenCommand(regimenId), CancellationToken.None)
                .AsTask();

            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        await using var verify = CreateContext();
        (await verify.SupplementRegimens.AnyAsync(x => x.Id == regimenId)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteCoachRegimens_RemovesOnlyTheCoachAuthoredOnes()
    {
        await SeedUsersAsync(withRelationship: true);
        await using (var seed = CreateContext())
        {
            await CreateHandler(seed, CoachId).Handle(
                new CreateSupplementRegimenCommand(CreateRequest(), UserId),
                CancellationToken.None);
        }

        await using (var seed = CreateContext())
        {
            await CreateHandler(seed, UserId).Handle(
                new CreateSupplementRegimenCommand(CreateRequest() with { Name = "Own vitamin" }),
                CancellationToken.None);
        }

        await using (var cleanup = CreateContext())
        {
            await new SupplementRepository(cleanup)
                .DeleteCoachRegimensForClientAsync(UserId, CoachId, CancellationToken.None);
            await cleanup.SaveChangesAsync();
        }

        await using var verify = CreateContext();
        var remaining = await verify.SupplementRegimens.ToListAsync();
        remaining.Should().ContainSingle();
        remaining[0].Name.Should().Be("Own vitamin");
        remaining[0].CreatedByUserId.Should().Be(UserId);
    }

    [Fact]
    public void PersistenceModel_EnforcesUniqueDoseOccurrenceAndUserDateIndex()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(SupplementIntakeLog));
        entity.Should().NotBeNull();

        var indexes = entity!.GetIndexes().ToList();
        indexes.Should().Contain(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(SupplementIntakeLog.DoseSlotId), nameof(SupplementIntakeLog.ScheduledDate) }));
        indexes.Should().Contain(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual(new[] { nameof(SupplementIntakeLog.UserId), nameof(SupplementIntakeLog.ScheduledDate) }));
    }

    private CreateSupplementRegimenHandler CreateHandler(ApplicationDbContext context, Guid callerId) =>
        new(new SupplementRepository(context), new CoachClientRepository(context), new FakeCurrentUserService(callerId));

    private UpdateSupplementRegimenHandler UpdateHandler(ApplicationDbContext context, Guid callerId) =>
        new(new SupplementRepository(context), new CoachClientRepository(context), new FakeCurrentUserService(callerId));

    private ArchiveSupplementRegimenHandler ArchiveHandler(ApplicationDbContext context, Guid callerId) =>
        new(new SupplementRepository(context), new CoachClientRepository(context), new FakeCurrentUserService(callerId));

    private DeleteSupplementRegimenHandler DeleteHandler(ApplicationDbContext context, Guid callerId) =>
        new(new SupplementRepository(context), new CoachClientRepository(context), new FakeCurrentUserService(callerId));

    private GetSupplementRegimensHandler RegimensHandler(ApplicationDbContext context, Guid callerId) =>
        new(new SupplementRepository(context), new CoachClientRepository(context), new FakeCurrentUserService(callerId));

    private GetSupplementDailyHandler DailyHandler(ApplicationDbContext context, Guid callerId) =>
        new(new SupplementRepository(context), new CoachClientRepository(context), new FakeCurrentUserService(callerId));

    private GetSupplementHistoryHandler HistoryHandler(ApplicationDbContext context, Guid callerId) =>
        new(new SupplementRepository(context), new CoachClientRepository(context), new FakeCurrentUserService(callerId));

    private RecordSupplementDoseHandler RecordHandler(ApplicationDbContext context, Guid callerId) =>
        new(new SupplementRepository(context), new FakeCurrentUserService(callerId));

    private ResetSupplementDoseHandler ResetHandler(ApplicationDbContext context, Guid callerId) =>
        new(new SupplementRepository(context), new FakeCurrentUserService(callerId));

    private static CreateSupplementRegimenRequest CreateRequest() =>
        new(
            "Creatine",
            "Xenoh Labs",
            "Powder",
            "Take with water",
            null,
            Today,
            [Dose(5, "g", new TimeOnly(8, 0))]);

    private static UpdateSupplementRegimenRequest UpdateRequest(DateOnly effectiveFrom, decimal amount) =>
        new(
            "Creatine",
            "Xenoh Labs",
            "Powder",
            "Take with water",
            null,
            effectiveFrom,
            [Dose(amount, "g", new TimeOnly(9, 0))]);

    private static SupplementDoseSlotRequest Dose(decimal amount, string unit, TimeOnly time) =>
        new(amount, unit, time, Enum.GetValues<DayOfWeek>());

    private async Task SeedUsersAsync(bool withRelationship = false)
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            CreateUser(UserId, "Client"),
            CreateUser(CoachId, "Coach"),
            CreateUser(StrangerId, "Stranger"));
        if (withRelationship)
        {
            context.CoachClientRelationships.Add(new CoachClientRelationship
            {
                CoachId = CoachId,
                ClientId = UserId,
                Status = RelationshipStatus.Active,
                StartDate = Today.AddDays(-30)
            });
        }

        await context.SaveChangesAsync();
    }

    private static ApplicationUser CreateUser(Guid id, string firstName) =>
        new()
        {
            Id = id,
            FirstName = firstName,
            LastName = "User",
            Email = $"{firstName.ToLowerInvariant()}@test.local",
            UserName = $"{firstName.ToLowerInvariant()}@test.local"
        };
}
