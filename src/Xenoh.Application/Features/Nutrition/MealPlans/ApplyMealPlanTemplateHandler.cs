using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Nutrition.MealPlans;

public sealed class ApplyMealPlanTemplateHandler(
    IApplicationDbContext db,
    IFoodLogService foodLogService,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser
) : IRequestHandler<ApplyMealPlanTemplateCommand, ApplyMealPlanTemplateResponse>
{
    public async ValueTask<ApplyMealPlanTemplateResponse> Handle(
        ApplyMealPlanTemplateCommand request,
        CancellationToken cancellationToken)
    {
        var dayCount = MealPlanTemplateRules.Validate(request);
        var callerId = currentUser.UserId;
        var userId = request.UserId ?? callerId;
        await EnsureAccessAsync(callerId, userId, cancellationToken);

        var existingDays = await MealPlanQueryLoader.LoadTrackedByUserDateRangeAsync(
            db, userId, request.StartDate, request.EndDate, cancellationToken);

        if (existingDays.SelectMany(day => day.Meals).SelectMany(meal => meal.Items).Any(item => item.IsChecked))
            throw new InvalidOperationException("Uncheck meal plan items in the selected range before replacing its plans.");

        var foodItemIds = request.Meals
            .SelectMany(meal => meal.Items)
            .Select(item => item.FoodItemId)
            .Distinct()
            .ToList();
        await db.FoodItems
            .Include(food => food.Servings)
            .Where(food => foodItemIds.Contains(food.Id))
            .LoadAsync(cancellationToken);

        var preparedDays = new List<PreparedMealPlanDay>(dayCount);
        for (var offset = 0; offset < dayCount; offset++)
        {
            var date = request.StartDate.AddDays(offset);
            var meals = await MealPlanDayBuilder.BuildMealsAsync(
                foodLogService,
                userId,
                date,
                request.Meals,
                cancellationToken);
            preparedDays.Add(new PreparedMealPlanDay(date, meals));
        }

        var existingByDate = existingDays.ToDictionary(day => day.Date);
        foreach (var prepared in preparedDays)
        {
            if (!existingByDate.TryGetValue(prepared.Date, out var day))
            {
                day = new MealPlanDay { UserId = userId, Date = prepared.Date };
                await db.MealPlanDays.AddAsync(day, cancellationToken);
            }
            else
            {
                db.MealPlanMeals.RemoveRange(day.Meals);
                day.Meals.Clear();
            }

            day.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
            day.UpdatedAt = DateTime.UtcNow;

            foreach (var meal in prepared.Meals)
            {
                meal.MealPlanDayId = day.Id;
                day.Meals.Add(meal);
            }

            await db.MealPlanMeals.AddRangeAsync(prepared.Meals, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new ApplyMealPlanTemplateResponse(request.StartDate, request.EndDate, dayCount);
    }

    private async Task EnsureAccessAsync(Guid callerId, Guid userId, CancellationToken ct)
    {
        if (callerId == userId) return;

        if (!await coachClientRepo.HasActiveRelationshipAsync(callerId, userId, ct))
            throw new UnauthorizedAccessException("You do not have access to edit this user's meal plan.");
    }

    private sealed record PreparedMealPlanDay(DateOnly Date, List<MealPlanMeal> Meals);
}
