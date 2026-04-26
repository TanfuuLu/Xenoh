using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Users.Commands.LogBodyweight;

public sealed class LogBodyweightHandler(
    IBodyweightRepository bodyweightRepo,
    ICurrentUserService currentUser
) : IRequestHandler<LogBodyweightCommand, BodyweightLogResponse>
{
    public async ValueTask<BodyweightLogResponse> Handle(LogBodyweightCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var existing = await bodyweightRepo.FindTodayAsync(userId, today, cancellationToken);

        if (existing is null)
        {
            existing = new BodyweightLog
            {
                UserId = userId,
                Weight = request.Weight,
                Date = today
            };
            await bodyweightRepo.AddAsync(existing, cancellationToken);
        }
        else
        {
            existing.Weight = request.Weight;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await bodyweightRepo.SaveChangesAsync(cancellationToken);

        return new BodyweightLogResponse(existing.Id, existing.Weight, existing.Date);
    }
}
