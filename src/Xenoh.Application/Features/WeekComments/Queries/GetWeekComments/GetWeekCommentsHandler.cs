using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.PlanComments.Dtos;

namespace Xenoh.Application.Features.WeekComments.Queries.GetWeekComments;

public sealed class GetWeekCommentsHandler(IApplicationDbContext db)
    : IRequestHandler<GetWeekCommentsQuery, IReadOnlyList<CommentResponse>>
{
    public async ValueTask<IReadOnlyList<CommentResponse>> Handle(
        GetWeekCommentsQuery request, CancellationToken cancellationToken)
    {
        return await db.WeeklyWorkoutComments
            .Where(c => c.WeeklyWorkoutId == request.WeeklyWorkoutId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponse(
                c.Id,
                c.Content,
                c.AuthorId,
                c.Author.FirstName + " " + c.Author.LastName,
                c.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
