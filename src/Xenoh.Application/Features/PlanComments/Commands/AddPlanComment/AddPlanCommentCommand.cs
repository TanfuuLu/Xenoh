using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.PlanComments.Dtos;

namespace Xenoh.Application.Features.PlanComments.Commands.AddPlanComment;

public sealed record AddPlanCommentCommand : IRequest<CommentResponse>
{
    public Guid PlanId { get; init; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string Content { get; init; }
}
