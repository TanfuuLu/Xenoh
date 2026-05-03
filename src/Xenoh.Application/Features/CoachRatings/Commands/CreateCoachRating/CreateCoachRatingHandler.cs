using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachRatings.Commands.CreateCoachRating;

public sealed class CreateCoachRatingHandler(
    ICoachRatingRepository ratingRepo,
    ICurrentUserService currentUser,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<CreateCoachRatingCommand, CoachRatingResponse>
{
    public async ValueTask<CoachRatingResponse> Handle(CreateCoachRatingCommand request, CancellationToken cancellationToken)
    {
        if (request.Rating is < 1 or > 5)
            throw new InvalidOperationException("Rating must be between 1 and 5.");

        var clientId = currentUser.UserId;
        if (clientId == request.CoachId)
            throw new InvalidOperationException("You cannot rate yourself.");

        var coach = await userManager.FindByIdAsync(request.CoachId.ToString())
            ?? throw new InvalidOperationException("Coach not found.");
        if (!await userManager.IsInRoleAsync(coach, UserRole.Coach))
            throw new InvalidOperationException("Coach not found.");

        if (!await ratingRepo.CanClientRateCoachAsync(request.CoachId, clientId, cancellationToken))
            throw new InvalidOperationException("Only current or former clients can rate this coach.");

        if (await ratingRepo.FindAsync(request.CoachId, clientId, cancellationToken) is not null)
            throw new InvalidOperationException("You have already rated this coach.");

        var rating = new CoachRating
        {
            CoachId = request.CoachId,
            ClientId = clientId,
            Rating = request.Rating,
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim()
        };

        await ratingRepo.AddAsync(rating, cancellationToken);
        await ratingRepo.SaveChangesAsync(cancellationToken);

        var client = await userManager.FindByIdAsync(clientId.ToString())
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
