using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachRatings.Commands.CreateCoachRating;

public sealed record CreateCoachRatingCommand : IRequest<CoachRatingResponse>
{
    public Guid CoachId { get; init; }

    [Range(1, 5)]
    public int Rating { get; init; }

    [MaxLength(1000)]
    public string? Comment { get; init; }
}
