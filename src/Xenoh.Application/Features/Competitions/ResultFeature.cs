using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Domain.Services;

namespace Xenoh.Application.Features.Competitions;

public sealed record UpsertPowerliftingResultCommand(Guid EventId, Guid RegistrationId, decimal BodyweightKg,
    decimal BestSquatKg, decimal BestBenchKg, decimal BestDeadliftKg, CompetitionResultState State, string? Notes)
    : IRequest<IReadOnlyList<CompetitionResultDto>>;
public sealed record UpsertBodybuildingResultCommand(Guid EventId, Guid RegistrationId, int? Place,
    CompetitionResultState State, string? Notes) : IRequest<CompetitionResultDto>;
public sealed record GetCompetitionResultsQuery(string Slug, bool Preview = false) : IRequest<IReadOnlyList<CompetitionResultDto>>;
public sealed record PublishCompetitionResultsCommand(Guid EventId) : IRequest<IReadOnlyList<CompetitionResultDto>>;

internal static class CompetitionResultMapping
{
    public static CompetitionResultDto Power(PowerliftingCompetitionResult x) => new(x.RegistrationId, x.Registration.AthleteName,
        x.Registration.Category.Name, x.State, x.Place, x.BodyweightKg, x.BestSquatKg, x.BestBenchKg, x.BestDeadliftKg,
        x.TotalKg, x.Score, x.Formula, x.FormulaVersion, x.Notes);
    public static CompetitionResultDto Body(BodybuildingCompetitionResult x) => new(x.RegistrationId, x.Registration.AthleteName,
        x.Registration.Category.Name, x.State, x.Place, null, null, null, null, null, null, null, null, x.Notes);
}

public sealed class UpsertPowerliftingResultHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpsertPowerliftingResultCommand, IReadOnlyList<CompetitionResultDto>>
{
    public async ValueTask<IReadOnlyList<CompetitionResultDto>> Handle(UpsertPowerliftingResultCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageResults, ct);
        var registration = await db.CompetitionRegistrations.Include(x => x.Event).Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == request.RegistrationId && x.EventId == request.EventId, ct) ?? throw new KeyNotFoundException("Registration not found.");
        if (registration.Event.Discipline != CompetitionDiscipline.Powerlifting) throw new InvalidOperationException("This is not a powerlifting event.");
        if (!registration.IsConfirmed) throw new InvalidOperationException("Results may only be recorded for confirmed competitors.");
        if (request.BodyweightKg <= 0 || request.BestSquatKg < 0 || request.BestBenchKg < 0 || request.BestDeadliftKg < 0)
            throw new InvalidOperationException("Bodyweight must be positive and best lifts cannot be negative.");
        var result = await db.PowerliftingCompetitionResults.FirstOrDefaultAsync(x => x.RegistrationId == registration.Id, ct);
        var correction = result is not null;
        result ??= new PowerliftingCompetitionResult { RegistrationId = registration.Id };
        if (!correction) db.PowerliftingCompetitionResults.Add(result);
        result.BodyweightKg = request.BodyweightKg; result.BestSquatKg = request.BestSquatKg; result.BestBenchKg = request.BestBenchKg;
        result.BestDeadliftKg = request.BestDeadliftKg; result.TotalKg = request.State == CompetitionResultState.Finished
            ? request.BestSquatKg + request.BestBenchKg + request.BestDeadliftKg : 0;
        result.Formula = registration.Event.PowerliftingScoringFormula; result.FormulaVersion = registration.Event.PowerliftingFormulaVersion;
        result.Score = request.State == CompetitionResultState.Finished
            ? PowerliftingScoreCalculator.Calculate(result.Formula, result.TotalKg, result.BodyweightKg, registration.Sex) : 0;
        result.State = request.State; result.Notes = request.Notes?.Trim(); result.UpdatedAt = DateTime.UtcNow;
        CompetitionAccess.Audit(db, request.EventId, currentUser.UserId, correction ? "PowerliftingResultCorrected" : "PowerliftingResultRecorded", "PowerliftingCompetitionResult", result.Id);
        await db.SaveChangesAsync(ct);
        return await RecalculateCategoryAsync(db, registration.CategoryId, ct);
    }

    internal static async Task<IReadOnlyList<CompetitionResultDto>> RecalculateCategoryAsync(IApplicationDbContext db, Guid categoryId, CancellationToken ct)
    {
        var results = await db.PowerliftingCompetitionResults.Include(x => x.Registration).ThenInclude(x => x.Category)
            .Where(x => x.Registration.CategoryId == categoryId).OrderByDescending(x => x.Score).ThenBy(x => x.BodyweightKg)
            .ThenBy(x => x.Registration.AthleteName).ToListAsync(ct);
        decimal? previousScore = null; decimal? previousBodyweight = null; var previousPlace = 0;
        for (var i = 0; i < results.Count; i++)
        {
            var x = results[i];
            if (x.State != CompetitionResultState.Finished) { x.Place = null; continue; }
            var tied = previousScore == x.Score && previousBodyweight == x.BodyweightKg;
            x.Place = tied ? previousPlace : i + 1; previousPlace = x.Place.Value; previousScore = x.Score; previousBodyweight = x.BodyweightKg;
        }
        await db.SaveChangesAsync(ct); return results.Select(CompetitionResultMapping.Power).ToList();
    }
}

public sealed class UpsertBodybuildingResultHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpsertBodybuildingResultCommand, CompetitionResultDto>
{
    public async ValueTask<CompetitionResultDto> Handle(UpsertBodybuildingResultCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageResults, ct);
        var registration = await db.CompetitionRegistrations.Include(x => x.Event).Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == request.RegistrationId && x.EventId == request.EventId, ct) ?? throw new KeyNotFoundException("Registration not found.");
        if (registration.Event.Discipline != CompetitionDiscipline.Bodybuilding) throw new InvalidOperationException("This is not a bodybuilding event.");
        if (!registration.IsConfirmed) throw new InvalidOperationException("Results may only be recorded for confirmed competitors.");
        if (request.State == CompetitionResultState.Finished && (!request.Place.HasValue || request.Place <= 0)) throw new InvalidOperationException("A positive place is required for a finished competitor.");
        if (request.State == CompetitionResultState.Finished && await db.BodybuildingCompetitionResults.AsNoTracking().AnyAsync(x =>
                x.Registration.CategoryId == registration.CategoryId && x.RegistrationId != registration.Id && x.State == CompetitionResultState.Finished && x.Place == request.Place, ct))
            throw new InvalidOperationException("Place must be unique within the category.");
        var result = await db.BodybuildingCompetitionResults.FirstOrDefaultAsync(x => x.RegistrationId == registration.Id, ct);
        var correction = result is not null; result ??= new BodybuildingCompetitionResult { RegistrationId = registration.Id };
        if (!correction) db.BodybuildingCompetitionResults.Add(result);
        result.State = request.State; result.Place = request.State == CompetitionResultState.Finished ? request.Place : null;
        result.Notes = request.Notes?.Trim(); result.UpdatedAt = DateTime.UtcNow;
        CompetitionAccess.Audit(db, request.EventId, currentUser.UserId, correction ? "BodybuildingResultCorrected" : "BodybuildingResultRecorded", "BodybuildingCompetitionResult", result.Id);
        await db.SaveChangesAsync(ct);
        result.Registration = registration; return CompetitionResultMapping.Body(result);
    }
}

public sealed class GetCompetitionResultsHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetCompetitionResultsQuery, IReadOnlyList<CompetitionResultDto>>
{
    public async ValueTask<IReadOnlyList<CompetitionResultDto>> Handle(GetCompetitionResultsQuery request, CancellationToken ct)
    {
        var e = await db.CompetitionEvents.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == request.Slug, ct) ?? throw new KeyNotFoundException("Competition event not found.");
        if (!e.ResultsPublishedAt.HasValue)
        {
            if (!request.Preview || !currentUser.IsAuthenticated || (await CompetitionAccess.GetPermissionsAsync(db, e.Id, currentUser.UserId, ct) & CompetitionStaffPermission.ManageResults) == 0)
                throw new KeyNotFoundException("Competition results have not been published.");
        }
        if (e.Discipline == CompetitionDiscipline.Powerlifting)
        {
            var rows = await db.PowerliftingCompetitionResults.AsNoTracking().Include(x => x.Registration).ThenInclude(x => x.Category)
                .Where(x => x.Registration.EventId == e.Id).OrderBy(x => x.Registration.Category.DisplayOrder).ThenBy(x => x.Place ?? int.MaxValue).ToListAsync(ct);
            return rows.Select(CompetitionResultMapping.Power).ToList();
        }
        var body = await db.BodybuildingCompetitionResults.AsNoTracking().Include(x => x.Registration).ThenInclude(x => x.Category)
            .Where(x => x.Registration.EventId == e.Id).OrderBy(x => x.Registration.Category.DisplayOrder).ThenBy(x => x.Place ?? int.MaxValue).ToListAsync(ct);
        return body.Select(CompetitionResultMapping.Body).ToList();
    }
}

public sealed class PublishCompetitionResultsHandler(IApplicationDbContext db, ICurrentUserService currentUser, INotificationService notifications)
    : IRequestHandler<PublishCompetitionResultsCommand, IReadOnlyList<CompetitionResultDto>>
{
    public async ValueTask<IReadOnlyList<CompetitionResultDto>> Handle(PublishCompetitionResultsCommand request, CancellationToken ct)
    {
        await CompetitionAccess.RequireAsync(db, request.EventId, currentUser.UserId, CompetitionStaffPermission.ManageResults, ct);
        var e = await db.CompetitionEvents.Include(x => x.Registrations).FirstAsync(x => x.Id == request.EventId, ct);
        if (e.Status == CompetitionEventStatus.Cancelled) throw new InvalidOperationException("Cancelled events cannot publish results.");
        var count = e.Discipline == CompetitionDiscipline.Powerlifting
            ? await db.PowerliftingCompetitionResults.AsNoTracking().CountAsync(x => x.Registration.EventId == e.Id, ct)
            : await db.BodybuildingCompetitionResults.AsNoTracking().CountAsync(x => x.Registration.EventId == e.Id, ct);
        if (count == 0) throw new InvalidOperationException("Record at least one result before publishing.");
        e.ResultsPublishedAt = DateTime.UtcNow; e.Status = CompetitionEventStatus.Completed; e.UpdatedAt = DateTime.UtcNow;
        CompetitionAccess.Audit(db, e.Id, currentUser.UserId, "ResultsPublished", "CompetitionEvent", e.Id);
        await db.SaveChangesAsync(ct);
        foreach (var userId in e.Registrations.Where(x => x.UserId.HasValue && x.Status == CompetitionRegistrationStatus.Approved).Select(x => x.UserId!.Value).Distinct())
            await notifications.NotifyAsync(userId, "CompetitionResultsPublished", $"Results for {e.Title} are now available.", e.Id, "CompetitionEvent", ct);
        var query = new GetCompetitionResultsHandler(db, currentUser);
        return await query.Handle(new GetCompetitionResultsQuery(e.Slug), ct);
    }
}
