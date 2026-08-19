using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Nutrition.MealPlans;

public sealed class UpsertMealPlanForDateHandler(
    IApplicationDbContext db,
    IFoodLogService foodLogService,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<UpsertMealPlanForDateCommand, MealPlanDayResponse>
{
    public async ValueTask<MealPlanDayResponse> Handle(UpsertMealPlanForDateCommand request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;
        var userId = request.UserId ?? callerId;
        await EnsureAccessAsync(callerId, userId, cancellationToken);
        MealPlanDayBuilder.Validate(request.Notes, request.Meals);

        var day = await MealPlanQueryLoader.LoadTrackedByUserDateAsync(db, userId, request.Date, cancellationToken);
        if (day is null)
        {
            day = new MealPlanDay { UserId = userId, Date = request.Date };
            await db.MealPlanDays.AddAsync(day, cancellationToken);
        }
        else if (day.Meals.SelectMany(m => m.Items).Any(i => i.IsChecked))
        {
            throw new InvalidOperationException("Uncheck meal plan items before editing this date.");
        }

        day.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        // Whoever writes the day owns it: a client editing their coach's plan takes it over,
        // so a later disconnect leaves their version alone.
        day.CreatedByUserId = callerId;
        day.UpdatedAt = DateTime.UtcNow;
        day.Meals.Clear();

        var meals = await MealPlanDayBuilder.BuildMealsAsync(
            foodLogService,
            userId,
            request.Date,
            request.Meals,
            cancellationToken);

        foreach (var meal in meals)
        {
            meal.MealPlanDayId = day.Id;
            day.Meals.Add(meal);
            await db.MealPlanMeals.AddAsync(meal, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        var saved = await MealPlanQueryLoader.LoadAsNoTrackingByUserDateAsync(db, userId, request.Date, cancellationToken)
            ?? throw new InvalidOperationException("Meal plan could not be loaded after saving.");

        return MealPlanResponseMapper.ToResponse(saved);
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to edit this user's meal plan.");
    }
}
