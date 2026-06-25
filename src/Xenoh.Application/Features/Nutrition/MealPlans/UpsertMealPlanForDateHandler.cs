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
        Validate(request);

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
        else
        {
            db.MealPlanMeals.RemoveRange(day.Meals);
        }

        day.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        day.UpdatedAt = DateTime.UtcNow;
        day.Meals = [];

        foreach (var mealRequest in request.Meals.OrderBy(m => m.SortOrder))
        {
            var meal = new MealPlanMeal
            {
                MealPlanDayId = day.Id,
                Name = mealRequest.Name.Trim(),
                SortOrder = mealRequest.SortOrder
            };

            foreach (var itemRequest in mealRequest.Items.OrderBy(i => i.SortOrder))
            {
                var snapshot = await foodLogService.BuildFoodLogAsync(
                    userId,
                    request.Date,
                    itemRequest.FoodItemId,
                    itemRequest.Grams,
                    itemRequest.ServingLabel,
                    itemRequest.ServingCount,
                    cancellationToken);

                meal.Items.Add(new MealPlanItem
                {
                    FoodItemId = snapshot.FoodItemId,
                    SortOrder = itemRequest.SortOrder,
                    Grams = snapshot.Grams,
                    ServingLabelVi = snapshot.ServingLabelVi,
                    ServingLabelEn = snapshot.ServingLabelEn,
                    ServingCount = snapshot.ServingCount,
                    PlannedCalories = snapshot.ComputedCalories,
                    PlannedProteinG = snapshot.ComputedProteinG,
                    PlannedCarbsG = snapshot.ComputedCarbsG,
                    PlannedFatG = snapshot.ComputedFatG
                });
            }

            day.Meals.Add(meal);
        }

        await db.SaveChangesAsync(cancellationToken);

        var saved = await MealPlanQueryLoader.LoadAsNoTrackingByUserDateAsync(db, userId, request.Date, cancellationToken)
            ?? throw new InvalidOperationException("Meal plan could not be loaded after saving.");

        return MealPlanResponseMapper.ToResponse(saved);
    }

    private static void Validate(UpsertMealPlanForDateCommand request)
    {
        if (request.Meals.Count > 12)
            throw new InvalidOperationException("Meal plan can contain at most 12 meals per day.");

        foreach (var meal in request.Meals)
        {
            if (string.IsNullOrWhiteSpace(meal.Name))
                throw new InvalidOperationException("Meal name is required.");

            if (meal.Name.Length > 100)
                throw new InvalidOperationException("Meal name must be 100 characters or less.");

            if (meal.Items.Count > 30)
                throw new InvalidOperationException("A meal can contain at most 30 items.");
        }
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct);
        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to edit this user's meal plan.");
    }
}
