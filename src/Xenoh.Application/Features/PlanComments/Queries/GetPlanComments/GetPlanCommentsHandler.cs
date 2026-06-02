using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.PlanComments.Dtos;

namespace Xenoh.Application.Features.PlanComments.Queries.GetPlanComments;

public sealed class GetPlanCommentsHandler(IApplicationDbContext db)
    : IRequestHandler<GetPlanCommentsQuery, IReadOnlyList<CommentResponse>>
{
    public async ValueTask<IReadOnlyList<CommentResponse>> Handle(
        GetPlanCommentsQuery request, CancellationToken cancellationToken)
    {
        return await db.PlanComments
            .AsNoTracking()
            .Where(c => c.PlanId == request.PlanId)
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
