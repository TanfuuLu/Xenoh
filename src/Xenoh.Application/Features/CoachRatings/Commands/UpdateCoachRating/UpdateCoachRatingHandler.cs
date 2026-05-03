using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.CoachRatings.Commands.UpdateCoachRating;

public sealed class UpdateCoachRatingHandler(
    ICoachRatingRepository ratingRepo,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<UpdateCoachRatingCommand, CoachRatingResponse>
{
    public async ValueTask<CoachRatingResponse> Handle(UpdateCoachRatingCommand request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5.");

        var rating = await ratingRepo.FindAsync(request.CoachId, currentUser.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Rating not found.");

        rating.Rating = request.Rating;
        rating.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        rating.UpdatedAt = DateTime.UtcNow;

        await ratingRepo.SaveChangesAsync(cancellationToken);

        var client = await userManager.FindByIdAsync(currentUser.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        return new CoachRatingResponse(
            rating.Id,
            rating.CoachId,
            rating.ClientId,
            $"{client.FirstName} {client.LastName}".Trim(),
            rating.Rating,
            rating.Comment,
            rating.CreatedAt,
            rating.UpdatedAt);
    }
}
