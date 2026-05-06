using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Plans.Commands.CreatePlanForUser;

public sealed class CreatePlanForUserHandler(
    IPlanRepository planRepo,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser,
    ISubscriptionService subscriptionService,
    INotificationService notificationService
) : IRequestHandler<CreatePlanForUserCommand, PlanResponse>
{
    public async ValueTask<PlanResponse> Handle(CreatePlanForUserCommand request, CancellationToken cancellationToken)
    {
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");

        var coachId = currentUser.UserId;

        var relationship = await coachClientRepo.FindActiveByCoachAndClientAsync(
            coachId, request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("You are not the coach of this user.");

        var maxPlans = await subscriptionService.GetMaxPlansAsync(request.UserId, cancellationToken);
        var planCount = await planRepo.CountByOwnerAsync(request.UserId, cancellationToken);
        if (maxPlans != int.MaxValue && planCount >= maxPlans)
            throw new InvalidOperationException($"This user has reached the maximum of {maxPlans} plans.");

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

        await planRepo.AddAsync(plan, cancellationToken);
        await planRepo.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            request.UserId,
            "PlanAssigned",
            $"Coach đã tạo kế hoạch '{plan.Name}' cho bạn.",
            plan.Id,
            "Plan",
            cancellationToken);

        return await planRepo.GetByIdForUserAsync(plan.Id, coachId, cancellationToken)
            ?? throw new InvalidOperationException("Failed to reload created plan.");
    }
}
