using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.ExerciseTemplates.Commands.CreateCustomExerciseTemplate;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.ExerciseTemplates.Commands.UpdateCustomExerciseTemplate;

public sealed class UpdateCustomExerciseTemplateHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateCustomExerciseTemplateCommand, ExerciseTemplateResponse>
{
    public async ValueTask<ExerciseTemplateResponse> Handle(
        UpdateCustomExerciseTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var template = await db.ExerciseTemplates
            .FirstOrDefaultAsync(t => t.Id == request.Id && t.OwnerId == userId && !t.IsArchived, cancellationToken)
            ?? throw new InvalidOperationException("Custom exercise not found.");

        template.Name = CreateCustomExerciseTemplateHandler.NormalizeRequired(request.Name, "Exercise name is required.");
        template.Description = CreateCustomExerciseTemplateHandler.NormalizeOptional(request.Description);
        template.PrimaryMuscleGroup = request.PrimaryMuscleGroup;
        template.SecondaryMuscleGroups = CreateCustomExerciseTemplateHandler.NormalizeSecondaryMuscleGroups(
            request.SecondaryMuscleGroups,
            request.PrimaryMuscleGroup);
        template.ExerciseKind = request.ExerciseKind;
        template.EstimatedMet = request.ExerciseKind == ExerciseKind.Cardio ? 7.0m : 5.0m;
        template.IsCompetitionLift = false;
        template.CompetitionLiftType = null;
        template.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return CreateCustomExerciseTemplateHandler.ToResponse(template);
    }
}
