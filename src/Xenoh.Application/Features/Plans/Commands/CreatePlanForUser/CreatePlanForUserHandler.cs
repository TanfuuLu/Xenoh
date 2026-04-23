using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Plans.Commands.CreatePlanForUser;

public sealed class CreatePlanForUserHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<CreatePlanForUserCommand, PlanResponse>
{
    private const int MaxPlansPerUser = 3;

    public async ValueTask<PlanResponse> Handle(CreatePlanForUserCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");

        var coachId = currentUser.UserId;

        var relationship = await context.CoachClientRelationships
            .FirstOrDefaultAsync(r =>
                r.CoachId == coachId &&
                r.ClientId == request.UserId &&
                r.Status == RelationshipStatus.Accepted, cancellationToken)
            ?? throw new InvalidOperationException("You are not the coach of this user.");

        var planCount = await context.Plans
            .CountAsync(p => p.OwnerId == request.UserId, cancellationToken);

        if (planCount >= MaxPlansPerUser)
            throw new InvalidOperationException($"This user has reached the maximum of {MaxPlansPerUser} plans.");

        var plan = new Plan
        {
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PlanType = PlanType.Coach,
            OwnerId = request.UserId,
            CreatedByCoachId = coachId
        };

        plan.WeeklyWorkouts = PlanWeekGenerator.Generate(plan);

        context.Plans.Add(plan);
        await context.SaveChangesAsync(cancellationToken);

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
            0,
            saved.IsActive,
            saved.CreatedAt
        );
    }
}
