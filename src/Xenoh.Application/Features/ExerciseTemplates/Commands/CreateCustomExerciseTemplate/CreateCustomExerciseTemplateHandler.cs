using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Subscriptions;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.ExerciseTemplates.Commands.CreateCustomExerciseTemplate;

public sealed class CreateCustomExerciseTemplateHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService
) : IRequestHandler<CreateCustomExerciseTemplateCommand, ExerciseTemplateResponse>
{
    public async ValueTask<ExerciseTemplateResponse> Handle(
        CreateCustomExerciseTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User is not authenticated.");

        var tier = await subscriptionService.GetActiveTierAsync(userId, cancellationToken);
        var customExerciseLimit = SubscriptionLimits.MaxCustomExercises(tier);
        if (customExerciseLimit != int.MaxValue)
        {
            var customExerciseCount = await db.ExerciseTemplates
                .AsNoTracking()
                .CountAsync(t => t.OwnerId == userId && !t.IsArchived, cancellationToken);

            if (customExerciseCount >= customExerciseLimit)
                throw new InvalidOperationException($"Your plan allows up to {customExerciseLimit} custom exercises. Delete one or upgrade to create another.");
        }

        var template = new ExerciseTemplate
        {
            Name = NormalizeRequired(request.Name, "Exercise name is required."),
            Description = NormalizeOptional(request.Description),
            OwnerId = userId,
            PrimaryMuscleGroup = request.PrimaryMuscleGroup,
            SecondaryMuscleGroups = NormalizeSecondaryMuscleGroups(request.SecondaryMuscleGroups, request.PrimaryMuscleGroup),
            ExerciseKind = request.ExerciseKind,
            EstimatedMet = request.ExerciseKind == ExerciseKind.Cardio ? 7.0m : 5.0m,
            IsCompetitionLift = false,
            CompetitionLiftType = null
        };

        db.ExerciseTemplates.Add(template);
        await db.SaveChangesAsync(cancellationToken);

        return ToResponse(template);
    }

    internal static ExerciseTemplateResponse ToResponse(ExerciseTemplate template) =>
        new(
            template.Id,
            template.Name,
            template.Description,
            template.PrimaryMuscleGroup.ToString(),
            template.SecondaryMuscleGroups.Select(m => m.ToString()).ToList(),
            template.ExerciseKind.ToString(),
            template.EstimatedMet,
            template.OwnerId != null,
            template.OwnerId,
            template.ImageUrl);

    internal static string NormalizeRequired(string value, string errorMessage)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException(errorMessage);

        return normalized;
    }

    internal static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    internal static List<MuscleGroup> NormalizeSecondaryMuscleGroups(
        IEnumerable<MuscleGroup> groups,
        MuscleGroup primaryMuscleGroup) =>
        groups
            .Where(group => group != primaryMuscleGroup)
            .Distinct()
            .ToList();
}
