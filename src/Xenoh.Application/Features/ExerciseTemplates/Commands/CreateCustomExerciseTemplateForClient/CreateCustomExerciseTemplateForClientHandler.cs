using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.ExerciseTemplates.Commands.CreateCustomExerciseTemplate;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.ExerciseTemplates.Commands.CreateCustomExerciseTemplateForClient;

public sealed class CreateCustomExerciseTemplateForClientHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService,
    ICoachClientRepository coachClientRepo
) : IRequestHandler<CreateCustomExerciseTemplateForClientCommand, ExerciseTemplateResponse>
{
    public async ValueTask<ExerciseTemplateResponse> Handle(
        CreateCustomExerciseTemplateForClientCommand request,
        CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;
        if (coachId == Guid.Empty)
            throw new InvalidOperationException("User is not authenticated.");

        if (!await subscriptionService.CanUseAdvancedAnalyticsAsync(coachId, cancellationToken))
            throw new InvalidOperationException("Creating custom exercises requires an active Pro subscription.");

        var relationship = await coachClientRepo.FindActiveByCoachAndClientAsync(coachId, request.ClientId, cancellationToken);
        if (relationship is null)
            throw new InvalidOperationException("Client not found or no active coaching relationship.");

        var template = new ExerciseTemplate
        {
            Name = CreateCustomExerciseTemplateHandler.NormalizeRequired(request.Name, "Exercise name is required."),
            Description = CreateCustomExerciseTemplateHandler.NormalizeOptional(request.Description),
            OwnerId = request.ClientId,
            PrimaryMuscleGroup = request.PrimaryMuscleGroup,
            SecondaryMuscleGroups = CreateCustomExerciseTemplateHandler.NormalizeSecondaryMuscleGroups(
                request.SecondaryMuscleGroups, request.PrimaryMuscleGroup),
            ExerciseKind = request.ExerciseKind,
            EstimatedMet = request.ExerciseKind == ExerciseKind.Cardio ? 7.0m : 5.0m,
            IsCompetitionLift = false,
            CompetitionLiftType = null
        };

        db.ExerciseTemplates.Add(template);
        await db.SaveChangesAsync(cancellationToken);

        return CreateCustomExerciseTemplateHandler.ToResponse(template);
    }
}
