using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Plans.Commands.CreatePlan;

public sealed class CreatePlanHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<CreatePlanCommand, PlanResponse>
{
    private const int MaxPlansPerUser = 3;

    public async ValueTask<PlanResponse> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");

        var userId = currentUser.UserId;

        var planCount = await context.Plans
            .CountAsync(p => p.OwnerId == userId, cancellationToken);

        if (planCount >= MaxPlansPerUser)
            throw new InvalidOperationException($"You have reached the maximum of {MaxPlansPerUser} plans. Please delete a plan before creating a new one.");

        var plan = new Plan
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PlanType = PlanType.Self,
            OwnerId = userId
        };

        plan.WeeklyWorkouts = PlanWeekGenerator.Generate(plan);

        context.Plans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);

        var saved = await context.Plans
            .AsNoTracking()
            .Include(p => p.Owner)
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
            null,
            saved.WeeklyWorkouts.Count,
            allDays.Count,
            0,
            saved.CreatedAt
        );
    }
}
