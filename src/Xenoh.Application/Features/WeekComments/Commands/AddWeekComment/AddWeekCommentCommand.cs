using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.PlanComments.Dtos;

namespace Xenoh.Application.Features.WeekComments.Commands.AddWeekComment;

public sealed record AddWeekCommentCommand : IRequest<CommentResponse>
{
    public Guid WeeklyWorkoutId { get; init; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string Content { get; init; }
}
