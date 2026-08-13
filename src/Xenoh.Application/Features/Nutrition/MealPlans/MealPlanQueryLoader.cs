using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Nutrition.MealPlans;

internal static class MealPlanQueryLoader
{
    public static Task<MealPlanDay?> LoadTrackedByUserDateAsync(
        IApplicationDbContext db,
        Guid userId,
        DateOnly date,
        CancellationToken ct) =>
        db.MealPlanDays
            .Include(d => d.Meals)
                .ThenInclude(m => m.Items)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == date, ct);

    public static Task<MealPlanDay?> LoadAsNoTrackingByUserDateAsync(
        IApplicationDbContext db,
        Guid userId,
        DateOnly date,
        CancellationToken ct) =>
        BaseMealPlanQuery(db)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == date, ct);

    public static Task<MealPlanDay?> LoadAsNoTrackingByDayIdAsync(
        IApplicationDbContext db,
        Guid dayId,
        CancellationToken ct) =>
        BaseMealPlanQuery(db)
            .FirstOrDefaultAsync(d => d.Id == dayId, ct);

    public static Task<List<MealPlanDay>> LoadTrackedByUserDateRangeAsync(
        IApplicationDbContext db,
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct) =>
        db.MealPlanDays
            .Include(d => d.Meals)
                .ThenInclude(m => m.Items)
            .Where(d => d.UserId == userId && d.Date >= startDate && d.Date <= endDate)
            .ToListAsync(ct);

    public static Task<List<MealPlanDay>> LoadAsNoTrackingByUserDateRangeAsync(
        IApplicationDbContext db,
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct) =>
        BaseMealPlanQuery(db)
            .Where(d => d.UserId == userId && d.Date >= startDate && d.Date <= endDate)
            .ToListAsync(ct);

    private static IQueryable<MealPlanDay> BaseMealPlanQuery(IApplicationDbContext db) =>
        db.MealPlanDays
            .AsNoTracking()
            .Include(d => d.Meals)
                .ThenInclude(m => m.Items)
                    .ThenInclude(i => i.FoodItem);
}
