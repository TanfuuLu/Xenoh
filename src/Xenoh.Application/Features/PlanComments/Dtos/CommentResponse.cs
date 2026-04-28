namespace Xenoh.Application.Features.PlanComments.Dtos;

public sealed record CommentResponse(
    Guid Id,
    string Content,
    Guid AuthorId,
    string AuthorName,
    DateTime CreatedAt);
