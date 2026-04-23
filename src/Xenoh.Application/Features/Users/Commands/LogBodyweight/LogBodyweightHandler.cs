using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Users.Commands.LogBodyweight;

public sealed class LogBodyweightHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<LogBodyweightCommand, BodyweightLogResponse>
{
    public async ValueTask<BodyweightLogResponse> Handle(LogBodyweightCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var existing = await context.BodyweightLogs
            .FirstOrDefaultAsync(b => b.UserId == userId && b.Date == today, cancellationToken);

        if (existing is null)
        {
            existing = new BodyweightLog
            {
                UserId = userId,
                Weight = request.Weight,
                Date = today
            };
            context.BodyweightLogs.Add(existing);
        }
        else
        {
            existing.Weight = request.Weight;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        return new BodyweightLogResponse(existing.Id, existing.Weight, existing.Date);
    }
}
