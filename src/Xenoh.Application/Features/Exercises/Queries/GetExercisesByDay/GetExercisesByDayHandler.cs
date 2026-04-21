using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Queries.GetExercisesByDay;

public sealed class GetExercisesByDayHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<GetExercisesByDayQuery, List<ExerciseResponse>>
{
    public async ValueTask<List<ExerciseResponse>> Handle(GetExercisesByDayQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var dayExists = await context.DailyWorkouts
            .AsNoTracking()
            .Include(d => d.WeeklyWorkout)
                .ThenInclude(w => w.Plan)
            .AnyAsync(d => d.Id == request.DailyWorkoutId &&
                (d.WeeklyWorkout.Plan.OwnerId == userId || d.WeeklyWorkout.Plan.CreatedByCoachId == userId), cancellationToken);

        if (!dayExists)
            throw new InvalidOperationException("Daily workout not found.");

        var exercises = await context.Exercises
            .AsNoTracking()
            .Include(e => e.Sets)
            .Where(e => e.DailyWorkoutId == request.DailyWorkoutId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return exercises.Select(CreateExerciseHandler.ToResponse).ToList();
    }
}
