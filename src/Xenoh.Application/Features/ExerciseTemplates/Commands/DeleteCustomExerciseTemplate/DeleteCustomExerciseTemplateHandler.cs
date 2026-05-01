using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.ExerciseTemplates.Commands.DeleteCustomExerciseTemplate;

public sealed class DeleteCustomExerciseTemplateHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteCustomExerciseTemplateCommand>
{
    public async ValueTask<Unit> Handle(
        DeleteCustomExerciseTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var template = await db.ExerciseTemplates
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.OwnerId == userId && !t.IsArchived, cancellationToken)
            ?? throw new InvalidOperationException("Custom exercise not found.");

        var isUsed = await db.Exercises
            .AnyAsync(e => e.ExerciseTemplateId == template.Id, cancellationToken);

        if (isUsed)
        {
            template.IsArchived = true;
            template.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            db.ExerciseTemplates.Remove(template);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
