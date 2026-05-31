using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Users.Queries.GetMyTrainingActivity;

public sealed class GetMyTrainingActivityHandler(
    ITrainingActivityRepository trainingActivityRepo,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyTrainingActivityQuery, TrainingActivityResponse>
{
    public async ValueTask<TrainingActivityResponse> Handle(
        GetMyTrainingActivityQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Year < 1 || request.Year > 9999)
            throw new InvalidOperationException("Year is invalid.");

        if (request.Month < 1 || request.Month > 12)
            throw new InvalidOperationException("Month is invalid.");

        var userId = currentUser.UserId;
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var accountCreatedDate = DateOnly.FromDateTime(user.CreatedAt);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var snapshot = await trainingActivityRepo.GetActivityAsync(
            userId,
            accountCreatedDate,
            today,
            request.Year,
            request.Month,
            cancellationToken);

        return new TrainingActivityResponse(
            snapshot.TotalDurationSeconds,
            snapshot.TotalWeightTrainedKg,
            user.CreatedAt,
            request.Year,
            request.Month,
            snapshot.TrainedDates);
    }
}
