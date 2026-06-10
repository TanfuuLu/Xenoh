using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Users.Queries.GetMyVolumeHistory;

public sealed class GetMyVolumeHistoryHandler(
    ITrainingActivityRepository trainingActivityRepo,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyVolumeHistoryQuery, IReadOnlyList<VolumeHistoryPoint>>
{
    private const int MaxMonths = 24;

    public async ValueTask<IReadOnlyList<VolumeHistoryPoint>> Handle(
        GetMyVolumeHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var months = Math.Clamp(request.Months, 1, MaxMonths);

        var userId = currentUser.UserId;
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-(months - 1));

        var buckets = await trainingActivityRepo.GetMonthlyVolumeAsync(
            userId,
            startMonth,
            today,
            cancellationToken);

        var byMonth = buckets.ToDictionary(b => (b.Year, b.Month), b => b.VolumeKg);

        var result = new List<VolumeHistoryPoint>(months);
        for (var i = 0; i < months; i++)
        {
            var month = startMonth.AddMonths(i);
            result.Add(new VolumeHistoryPoint(
                month.Year,
                month.Month,
                byMonth.GetValueOrDefault((month.Year, month.Month), 0m)));
        }

        return result;
    }
}
