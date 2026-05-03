using Mediator;

namespace Xenoh.Application.Features.CoachRatings.Commands.DeleteCoachRating;

public sealed record DeleteCoachRatingCommand(Guid CoachId) : IRequest;
