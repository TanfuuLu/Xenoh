using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Plans.Commands.UpdatePlan;

public sealed class UpdatePlanHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<UpdatePlanCommand, PlanResponse>
{
    public async ValueTask<PlanResponse> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");

        var userId = currentUser.UserId;

        var plan = await context.Plans
            .Include(p => p.WeeklyWorkouts)
                .ThenInclude(w => w.DailyWorkouts)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        // Permission check
        bool canEdit = plan.PlanType == PlanType.Coach
            ? plan.CreatedByCoachId == userId
            : plan.OwnerId == userId;

        if (!canEdit)
            throw new InvalidOperationException("You do not have permission to update this plan.");

        bool datesChanged = plan.StartDate != request.StartDate || plan.EndDate != request.EndDate;

        if (datesChanged)
        {
            var hasProgress = plan.WeeklyWorkouts
                .SelectMany(w => w.DailyWorkouts)
                .Any(d => d.IsCompleted);

            if (hasProgress)
                throw new InvalidOperationException("Cannot change dates: plan already has completed days.");

            // Remove all existing weeks (cascade deletes days + exercises)
            context.WeeklyWorkouts.RemoveRange(plan.WeeklyWorkouts);

            plan.StartDate = request.StartDate;
            plan.EndDate = request.EndDate;
            plan.Name = request.Name;

            await context.SaveChangesAsync(cancellationToken);

            // Regenerate weeks
            var newWeeks = PlanWeekGenerator.Generate(plan);
            context.WeeklyWorkouts.AddRange(newWeeks);
        }
        else
        {
            plan.Name = request.Name;
        }

        await context.SaveChangesAsync(cancellationToken);

        // Reload with full data for response
        var saved = await context.Plans
            .AsNoTracking()
            .Include(p => p.Owner)
            .Include(p => p.CreatedByCoach)
            .Include(p => p.WeeklyWorkouts)
                .ThenInclude(w => w.DailyWorkouts)
            .FirstAsync(p => p.Id == plan.Id, cancellationToken);

        var allDays = saved.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).ToList();

        return new PlanResponse(
            saved.Id,
            saved.Name,
            saved.StartDate,
            saved.EndDate,
            saved.PlanType.ToString(),
            saved.OwnerId,
            $"{saved.Owner.FirstName} {saved.Owner.LastName}".Trim(),
            saved.CreatedByCoachId,
            saved.CreatedByCoach is null ? null : $"{saved.CreatedByCoach.FirstName} {saved.CreatedByCoach.LastName}".Trim(),
            saved.WeeklyWorkouts.Count,
            allDays.Count,
            allDays.Count(d => d.IsCompleted),
            saved.CreatedAt
        );
    }
}
