using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.Supplements.Queries;

public sealed class GetSupplementRegimensHandler(
    ISupplementRepository supplementRepository,
    ICoachClientRepository coachClientRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<GetSupplementRegimensQuery, IReadOnlyList<SupplementRegimenResponse>>
{
    public async ValueTask<IReadOnlyList<SupplementRegimenResponse>> Handle(
        GetSupplementRegimensQuery query,
        CancellationToken cancellationToken)
    {
        var userId = await SupplementAccess.ResolveTargetUserAsync(
            currentUser.UserId,
            query.UserId,
            coachClientRepository,
            cancellationToken);
        var regimens = await supplementRepository.GetRegimensAsync(
            userId,
            query.IncludeArchived,
            cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return regimens.Select(x => SupplementMapper.ToResponse(x, today)).ToList();
    }
}

public sealed class GetSupplementDailyHandler(
    ISupplementRepository supplementRepository,
    ICoachClientRepository coachClientRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<GetSupplementDailyQuery, SupplementDailyResponse>
{
    public async ValueTask<SupplementDailyResponse> Handle(
        GetSupplementDailyQuery query,
        CancellationToken cancellationToken)
    {
        var userId = await SupplementAccess.ResolveTargetUserAsync(
            currentUser.UserId,
            query.UserId,
            coachClientRepository,
            cancellationToken);
        var versions = await supplementRepository.GetScheduleVersionsAsync(
            userId,
            query.Date,
            query.Date,
            cancellationToken);
        var intakes = await supplementRepository.GetIntakesAsync(
            userId,
            query.Date,
            query.Date,
            cancellationToken);
        var intakeBySlot = intakes.ToDictionary(x => x.DoseSlotId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var doses = versions
            .Where(x => x.IsEffectiveOn(query.Date))
            .SelectMany(x => x.DoseSlots)
            .Where(x => x.OccursOn(query.Date))
            .Select(slot => SupplementOccurrenceFactory.ToDailyDose(
                slot,
                intakeBySlot.GetValueOrDefault(slot.Id),
                query.Date,
                today))
            .OrderBy(x => x.Time)
            .ThenBy(x => x.RegimenName)
            .ToList();

        return new SupplementDailyResponse(
            userId,
            query.Date,
            doses,
            SupplementRules.CalculateTotals(doses.Select(x => x.Status)));
    }
}

public sealed class GetSupplementHistoryHandler(
    ISupplementRepository supplementRepository,
    ICoachClientRepository coachClientRepository,
    ICurrentUserService currentUser)
    : IRequestHandler<GetSupplementHistoryQuery, SupplementHistoryResponse>
{
    public async ValueTask<SupplementHistoryResponse> Handle(
        GetSupplementHistoryQuery query,
        CancellationToken cancellationToken)
    {
        SupplementRules.ValidateHistoryRange(query.From, query.To);
        var userId = await SupplementAccess.ResolveTargetUserAsync(
            currentUser.UserId,
            query.UserId,
            coachClientRepository,
            cancellationToken);
        var versions = await supplementRepository.GetScheduleVersionsAsync(
            userId,
            query.From,
            query.To,
            cancellationToken);
        var intakes = await supplementRepository.GetIntakesAsync(
            userId,
            query.From,
            query.To,
            cancellationToken);
        var intakesByOccurrence = intakes.ToDictionary(
            x => (x.DoseSlotId, x.ScheduledDate));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var days = new List<SupplementHistoryDayResponse>();
        var allStatuses = new List<SupplementDoseStatus>();

        for (var date = query.From; date <= query.To; date = date.AddDays(1))
        {
            var statuses = versions
                .Where(x => x.IsEffectiveOn(date))
                .SelectMany(x => x.DoseSlots)
                .Where(x => x.OccursOn(date))
                .Select(slot => SupplementOccurrenceFactory.ResolveStatus(
                    intakesByOccurrence.GetValueOrDefault((slot.Id, date)),
                    date,
                    today))
                .ToList();
            allStatuses.AddRange(statuses);
            days.Add(new SupplementHistoryDayResponse(
                date,
                SupplementRules.CalculateTotals(statuses)));
        }

        return new SupplementHistoryResponse(
            userId,
            query.From,
            query.To,
            SupplementRules.CalculateTotals(allStatuses),
            days);
    }
}
