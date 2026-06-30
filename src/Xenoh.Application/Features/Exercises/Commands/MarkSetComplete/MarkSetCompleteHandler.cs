using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.XP;
using Xenoh.Application.Features.Chat.Dtos;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Exercises.Commands.MarkSetComplete;

public sealed class MarkSetCompleteHandler(
    IExerciseSetRepository exerciseSetRepo,
    IWorkoutHistoryRepository workoutHistoryRepo,
    IUserPrRepository userPrRepo,
    IBodyweightRepository bodyweightRepo,
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    IChatRealtimeService chatRealtimeService,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<MarkSetCompleteCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(MarkSetCompleteCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var set = await exerciseSetRepo.FindForCompleteAsync(request.SetId, cancellationToken)
            ?? throw new InvalidOperationException("Set not found.");

        var exercise = set.Exercise;
        var plan = exercise.DailyWorkout.WeeklyWorkout.Plan;

        if (plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");

        if (exercise.IsSkipped)
            throw new InvalidOperationException("Cannot complete a skipped exercise.");

        if (set.IsCompleted)
        {
            var personalRecordWeight = await GetPersonalRecordWeight(userId, exercise.ExerciseTemplateId, cancellationToken);
            var existingBodyweight = await bodyweightRepo.GetLatestWeightOnOrBeforeAsync(
                plan.OwnerId,
                exercise.DailyWorkout.Date,
                cancellationToken);

            return CreateExerciseHandler.ToResponse(exercise, personalRecordWeight, existingBodyweight);
        }

        var now = DateTime.UtcNow;
        set.IsCompleted = true;
        set.CompletedAt = now;
        set.UpdatedAt = now;

        if (request.ActualReps is not null) set.ActualReps = request.ActualReps;
        if (request.ActualWeight is not null) set.ActualWeight = request.ActualWeight;
        if (request.Rpe is not null) set.Rpe = request.Rpe;

        bool allSetsDone = exercise.Sets.All(s => s.IsCompleted || s.Id == set.Id);
        exercise.IsCompleted = allSetsDone;
        if (allSetsDone && exercise.StartedAtUtc is not null && exercise.EndedAtUtc is null)
        {
            exercise.EndedAtUtc = now;
            exercise.DurationSeconds = Math.Max(
                0,
                (int)Math.Round((now - exercise.StartedAtUtc.Value).TotalSeconds));
        }

        exercise.UpdatedAt = now;

        var dailyWorkout = exercise.DailyWorkout;
        var dayWasCompleted = dailyWorkout.IsCompleted;
        var hasIncompleteOtherExercise = await db.Exercises
            .AsNoTracking()
            .AnyAsync(e =>
                e.DailyWorkoutId == dailyWorkout.Id &&
                e.Id != exercise.Id &&
                !e.IsCompleted &&
                !e.IsSkipped,
                cancellationToken);
        bool allExercisesDone = allSetsDone && !hasIncompleteOtherExercise;
        var dayJustCompleted = !dayWasCompleted && allExercisesDone;

        dailyWorkout.IsCompleted = allExercisesDone;
        dailyWorkout.UpdatedAt = now;

        // Auto-complete the week when all effective days are done / rest / missed
        var week = dailyWorkout.WeeklyWorkout;
        week.IsCompleted = await IsWeekCompleteAsync(week, dailyWorkout.Id, allExercisesDone, cancellationToken);
        week.UpdatedAt = now;

        // Log workout history once per day (for streak tracking)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool alreadyLogged = await workoutHistoryRepo.ExistsForDateAsync(userId, today, cancellationToken);
        if (!alreadyLogged)
            await workoutHistoryRepo.AddAsync(new WorkoutHistory { UserId = userId, Date = today }, cancellationToken);

        await ExerciseXpAwarder.AwardIfEligibleAsync(userManager, userId, exercise);
        await exerciseSetRepo.SaveChangesAsync(cancellationToken);

        // PR upsert: use actual weight if provided, else fall back to planned weight
        decimal? prWeight = null;
        var effectiveWeight = set.ActualWeight ?? set.PlannedWeight;
        if (effectiveWeight is > 0)
        {
            var pr = await userPrRepo.FindAsync(userId, exercise.ExerciseTemplateId, cancellationToken);
            var achievedAt = DateTime.UtcNow;
            var reps = set.ActualReps ?? set.PlannedReps;

            if (pr is null)
            {
                pr = new UserExercisePR
                {
                    UserId = userId,
                    ExerciseTemplateId = exercise.ExerciseTemplateId,
                    Weight = effectiveWeight.Value,
                    Reps = reps,
                    AchievedAt = achievedAt
                };
                await userPrRepo.AddAsync(pr, cancellationToken);
                await userPrRepo.AddHistoryAsync(CreateHistory(pr), cancellationToken);
                await userPrRepo.SaveChangesAsync(cancellationToken);
            }
            else if (effectiveWeight.Value > pr.Weight)
            {
                if (!await userPrRepo.HasHistoryAsync(userId, exercise.ExerciseTemplateId, cancellationToken))
                    await userPrRepo.AddHistoryAsync(CreateHistory(pr), cancellationToken);

                pr.Weight = effectiveWeight.Value;
                pr.Reps = reps;
                pr.AchievedAt = achievedAt;
                pr.UpdatedAt = achievedAt;
                await userPrRepo.AddHistoryAsync(CreateHistory(pr), cancellationToken);
                await userPrRepo.SaveChangesAsync(cancellationToken);
            }

            prWeight = pr.Weight;
        }

        // Notify coach of exercise warning (RPE >= 9 or < 70% of planned reps)
        if (plan.CreatedByCoachId.HasValue)
        {
            bool hasWarning =
                (set.Rpe.HasValue && set.Rpe.Value >= 9) ||
                (set.ActualReps.HasValue && set.ActualReps.Value < set.PlannedReps * 0.7m);

            if (hasWarning)
            {
                await notificationService.NotifyAsync(
                    plan.CreatedByCoachId.Value,
                    "ExerciseWarning",
                    $"Client có cảnh báo khi tập '{exercise.Name}' (ngày {exercise.DailyWorkout.Date:dd/MM/yyyy}).",
                    exercise.DailyWorkout.Id,
                    "Day",
                    cancellationToken);
            }
        }

        if (dayJustCompleted)
            await CreateWorkoutCompletionChatNoteAsync(plan, dailyWorkout, userId, cancellationToken);

        var bodyweight = await bodyweightRepo.GetLatestWeightOnOrBeforeAsync(
            plan.OwnerId,
            dailyWorkout.Date,
            cancellationToken);

        return CreateExerciseHandler.ToResponse(exercise, prWeight, bodyweight);
    }

    private async Task CreateWorkoutCompletionChatNoteAsync(
        Plan plan,
        DailyWorkout dailyWorkout,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        if (!plan.CreatedByCoachId.HasValue || plan.PlanType != PlanType.Coach)
            return;

        var coachId = plan.CreatedByCoachId.Value;
        var relationship = await db.CoachClientRelationships
            .AsNoTracking()
            .Where(r =>
                r.CoachId == coachId &&
                r.ClientId == clientId &&
                r.Status == RelationshipStatus.Active)
            .Select(r => new { r.Id, r.CoachId, r.ClientId })
            .FirstOrDefaultAsync(cancellationToken);

        if (relationship is null)
            return;

        var content = $"Workout completed: {plan.Name} - {dailyWorkout.Date:yyyy-MM-dd}";
        var alreadyExists = await db.Messages
            .AsNoTracking()
            .AnyAsync(m =>
                m.RelationshipId == relationship.Id &&
                m.Kind == MessageKind.System &&
                m.Content == content,
                cancellationToken);

        if (alreadyExists)
            return;

        var message = new Message
        {
            RelationshipId = relationship.Id,
            SenderId = clientId,
            Content = content,
            Kind = MessageKind.System,
            IsRead = false,
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        var response = new MessageResponse(
            message.Id,
            message.RelationshipId,
            message.SenderId,
            "System",
            message.Content,
            message.Kind.ToString(),
            message.IsRead,
            [],
            message.CreatedAt);

        await chatRealtimeService.MessageSentAsync(
            relationship.Id,
            response,
            [relationship.ClientId, relationship.CoachId],
            cancellationToken);
    }

    private async Task<bool> IsWeekCompleteAsync(
        WeeklyWorkout week,
        Guid updatedDailyWorkoutId,
        bool updatedDailyWorkoutIsCompleted,
        CancellationToken cancellationToken)
    {
        var plan = week.Plan;
        var effective = await db.DailyWorkouts
            .AsNoTracking()
            .Where(d =>
                d.WeeklyWorkoutId == week.Id &&
                d.Date >= plan.StartDate &&
                d.Date <= plan.EndDate)
            .Select(d => new
            {
                d.Id,
                d.IsCompleted,
                d.Status
            })
            .ToListAsync(cancellationToken);

        return effective.Count > 0 && effective.All(d =>
            {
                var isCompleted = d.Id == updatedDailyWorkoutId
                    ? updatedDailyWorkoutIsCompleted
                    : d.IsCompleted;

                return isCompleted || d.Status == DayStatus.Rest || d.Status == DayStatus.Missed;
            });
    }

    private async Task<decimal?> GetPersonalRecordWeight(Guid userId, Guid exerciseTemplateId, CancellationToken cancellationToken)
    {
        var pr = await userPrRepo.FindAsync(userId, exerciseTemplateId, cancellationToken);
        return pr?.Weight;
    }

    private static UserExercisePRHistory CreateHistory(UserExercisePR pr) =>
        new()
        {
            UserId = pr.UserId,
            ExerciseTemplateId = pr.ExerciseTemplateId,
            Weight = pr.Weight,
            Reps = pr.Reps,
            AchievedAt = pr.AchievedAt
        };
}
