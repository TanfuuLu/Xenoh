using FluentAssertions;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Competitions;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Services;
using Xunit;

namespace Xenoh.Application.Tests;

public sealed class CompetitionEventTests : HandlerTestBase
{
    [Theory]
    [InlineData(PowerliftingScoringFormula.Total)]
    [InlineData(PowerliftingScoringFormula.Dots)]
    [InlineData(PowerliftingScoringFormula.IpfGlPoints)]
    [InlineData(PowerliftingScoringFormula.Wilks)]
    public void ScoringFormulas_ReturnStablePositiveScores(PowerliftingScoringFormula formula)
    {
        var score = PowerliftingScoreCalculator.Calculate(formula, 600m, 82.5m, "Men");
        score.Should().BeGreaterThan(0);
        PowerliftingScoreCalculator.Calculate(formula, 600m, 82.5m, "Men").Should().Be(score);
    }

    [Fact]
    public async Task ApprovedOrganizer_CreatesDraftWithoutTemplateCategories()
    {
        await using var db = CreateContext();
        db.ApplicationUsers.Add(User(UserId));
        db.OrganizerProfiles.Add(new OrganizerProfile { UserId = UserId, OrganizationName = "Test Federation", ContactEmail = "org@test.local", ContactPhone = "1", Status = OrganizerProfileStatus.Approved });
        await db.SaveChangesAsync();
        var handler = new CreateCompetitionEventHandler(db, CurrentUser());

        var result = await handler.Handle(new CreateCompetitionEventCommand(Input(CompetitionDiscipline.Powerlifting)), CancellationToken.None);

        result.Status.Should().Be(CompetitionEventStatus.Draft);
        result.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task UnverifiedUser_CannotCreatePublicCompetition()
    {
        await using var db = CreateContext(); db.ApplicationUsers.Add(User(UserId)); await db.SaveChangesAsync();
        var handler = new CreateCompetitionEventHandler(db, CurrentUser());
        var action = () => handler.Handle(new CreateCompetitionEventCommand(Input(CompetitionDiscipline.Bodybuilding)), CancellationToken.None).AsTask();
        await action.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Registration_SnapshotsFeeAndRequiresBothApprovalAndPayment()
    {
        await using var db = CreateContext(); var user = User(UserId); db.ApplicationUsers.Add(user);
        var category = new CompetitionCategory { Code = "OPEN", Name = "Open", Capacity = 5 };
        var e = Event(category, fee: 250000m); db.CompetitionEvents.Add(e); await db.SaveChangesAsync();
        var handler = new RegisterForCompetitionHandler(db, CurrentUser(), new FakeLock(), new FakeNotificationService());

        var result = await handler.Handle(new RegisterForCompetitionCommand(e.Id, category.Id, null, null, null, null, null), CancellationToken.None);

        result.ExpectedFee.Should().Be(250000m);
        result.PaymentStatus.Should().Be(CompetitionPaymentStatus.AwaitingReceipt);
        result.IsConfirmed.Should().BeFalse();
        var entity = await db.CompetitionRegistrations.FindAsync(result.Id); entity!.Status = CompetitionRegistrationStatus.Approved;
        entity.IsConfirmed.Should().BeFalse(); entity.PaymentStatus = CompetitionPaymentStatus.Paid; entity.IsConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task CapacityReached_PlacesNextAthleteOnWaitlist()
    {
        await using var db = CreateContext(); var first = Guid.NewGuid(); db.ApplicationUsers.AddRange(User(UserId), User(first));
        var category = new CompetitionCategory { Code = "OPEN", Name = "Open", Capacity = 1 };
        var e = Event(category); e.Capacity = 1;
        e.Registrations.Add(new CompetitionRegistration { Category = category, UserId = first, AthleteName = "First Athlete", ContactEmail = "first@test.local", Status = CompetitionRegistrationStatus.Submitted, PaymentStatus = CompetitionPaymentStatus.NotRequired });
        db.CompetitionEvents.Add(e); await db.SaveChangesAsync();
        var handler = new RegisterForCompetitionHandler(db, CurrentUser(), new FakeLock(), new FakeNotificationService());

        var result = await handler.Handle(new RegisterForCompetitionCommand(e.Id, category.Id, null, null, null, null, null), CancellationToken.None);

        result.Status.Should().Be(CompetitionRegistrationStatus.Waitlisted);
    }

    [Fact]
    public async Task PaidRegistration_CannotBeApprovedBeforeReceiptIsAccepted()
    {
        await using var db = CreateContext();
        db.ApplicationUsers.Add(User(UserId));
        var category = new CompetitionCategory { Code = "OPEN", Name = "Open", Capacity = 5 };
        var e = Event(category, fee: 250000m); e.OwnerId = UserId;
        var registration = new CompetitionRegistration
        {
            Event = e, Category = category, AthleteName = "Waiting Athlete", ContactEmail = "waiting@test.local",
            Status = CompetitionRegistrationStatus.Submitted, PaymentStatus = CompetitionPaymentStatus.AwaitingReceipt,
            ExpectedFee = 250000m, Currency = "VND"
        };
        e.Registrations.Add(registration); db.CompetitionEvents.Add(e); await db.SaveChangesAsync();
        var handler = new DecideCompetitionRegistrationHandler(db, CurrentUser(), new FakeNotificationService(), new FakeLock());

        var action = () => handler.Handle(new DecideCompetitionRegistrationCommand(e.Id, registration.Id, true, null), CancellationToken.None).AsTask();

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("*accepted payment receipt*");
        registration.Status.Should().Be(CompetitionRegistrationStatus.Submitted);
    }

    [Fact]
    public async Task RejectedReceipt_RevertsLegacyApprovedRegistrationToSubmitted()
    {
        await using var db = CreateContext();
        db.ApplicationUsers.Add(User(UserId));
        var category = new CompetitionCategory { Code = "OPEN", Name = "Open", Capacity = 5 };
        var e = Event(category, fee: 250000m); e.OwnerId = UserId;
        var registration = new CompetitionRegistration
        {
            Event = e, Category = category, AthleteName = "Approved Athlete", ContactEmail = "approved@test.local",
            Status = CompetitionRegistrationStatus.Approved, PaymentStatus = CompetitionPaymentStatus.UnderReview,
            ExpectedFee = 250000m, Currency = "VND", ReviewedAt = DateTime.UtcNow, ReviewedById = UserId
        };
        var receipt = new CompetitionPaymentReceipt
        {
            Registration = registration, UploadedById = UserId, FileName = "receipt.jpg", ContentType = "image/jpeg",
            SizeBytes = 512, StorageKey = "private/receipt.jpg", Status = CompetitionReceiptStatus.UnderReview
        };
        registration.Receipts.Add(receipt); e.Registrations.Add(registration); db.CompetitionEvents.Add(e); await db.SaveChangesAsync();
        var handler = new ReviewCompetitionReceiptHandler(db, CurrentUser(), new FakeNotificationService());

        var result = await handler.Handle(new ReviewCompetitionReceiptCommand(e.Id, receipt.Id, false, "Transfer could not be verified."), CancellationToken.None);

        result.Status.Should().Be(CompetitionRegistrationStatus.Submitted);
        result.PaymentStatus.Should().Be(CompetitionPaymentStatus.ReceiptRejected);
        result.IsConfirmed.Should().BeFalse();
        registration.ReviewedAt.Should().BeNull();
        registration.ReviewedById.Should().BeNull();
    }

    [Fact]
    public async Task EventDetail_ReturnsOnlyFullyConfirmedRegistrationCount()
    {
        await using var db = CreateContext();
        db.ApplicationUsers.Add(User(UserId));
        var category = new CompetitionCategory { Code = "OPEN", Name = "Open", Capacity = 5 };
        var e = Event(category, fee: 250000m);
        e.Registrations.Add(new CompetitionRegistration
        {
            Category = category, AthleteName = "Confirmed Athlete", ContactEmail = "confirmed@test.local",
            Status = CompetitionRegistrationStatus.Approved, PaymentStatus = CompetitionPaymentStatus.Paid,
            ExpectedFee = 250000m, Currency = "VND"
        });
        e.Registrations.Add(new CompetitionRegistration
        {
            Category = category, AthleteName = "Unpaid Athlete", ContactEmail = "unpaid@test.local",
            Status = CompetitionRegistrationStatus.Approved, PaymentStatus = CompetitionPaymentStatus.ReceiptRejected,
            ExpectedFee = 250000m, Currency = "VND"
        });
        db.CompetitionEvents.Add(e); await db.SaveChangesAsync();
        var handler = new GetCompetitionEventBySlugHandler(db, CurrentUser());

        var result = await handler.Handle(new GetCompetitionEventBySlugQuery(e.Slug), CancellationToken.None);

        result.ConfirmedCount.Should().Be(1);
    }

    private static CompetitionEventInput Input(CompetitionDiscipline discipline)
    {
        var starts = DateTime.UtcNow.AddDays(40);
        return new CompetitionEventInput("National Open", "Description", null, discipline, "Arena", "District 1", "UTC",
            starts, starts.AddHours(8), DateTime.UtcNow.AddDays(1), starts.AddDays(-7), 100, 0, "VND", "organizer@test.local",
            null, null, null, null, PowerliftingScoringFormula.Dots);
    }

    private static CompetitionEvent Event(CompetitionCategory category, decimal fee = 0)
    {
        var now = DateTime.UtcNow;
        return new CompetitionEvent { OwnerId = Guid.NewGuid(), Slug = Guid.NewGuid().ToString("N"), Title = "Open", Description = "Test",
            Discipline = CompetitionDiscipline.Powerlifting, Status = CompetitionEventStatus.Published, VenueName = "Arena", Address = "Address",
            StartsAtUtc = now.AddDays(10), EndsAtUtc = now.AddDays(10).AddHours(8), RegistrationOpensAtUtc = now.AddDays(-1),
            RegistrationClosesAtUtc = now.AddDays(5), Capacity = 5, RegistrationFee = fee, Currency = "VND", OrganizerContact = "test",
            Categories = [category] };
    }

    private static ApplicationUser User(Guid id) => new() { Id = id, FirstName = "Test", LastName = "Athlete", UserName = $"{id}@test.local", Email = $"{id}@test.local" };
    private sealed class FakeLock : IDistributedLock
    {
        public Task<IAsyncDisposable?> TryAcquireAsync(string name, TimeSpan leaseTime, CancellationToken cancellationToken = default) => Task.FromResult<IAsyncDisposable?>(new Lease());
        private sealed class Lease : IAsyncDisposable { public ValueTask DisposeAsync() => ValueTask.CompletedTask; }
    }
}
