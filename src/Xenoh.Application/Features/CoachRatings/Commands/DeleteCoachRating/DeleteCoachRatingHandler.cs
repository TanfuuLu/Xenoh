using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.CoachRatings.Commands.DeleteCoachRating;

public sealed class DeleteCoachRatingHandler(
    ICoachRatingRepository ratingRepo,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteCoachRatingCommand>
{
    public async ValueTask<Unit> Handle(DeleteCoachRatingCommand request, CancellationToken cancellationToken)
    {
        var rating = await ratingRepo.FindAsync(request.CoachId, currentUser.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Rating not found.");

        ratingRepo.Remove(rating);
        await ratingRepo.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
