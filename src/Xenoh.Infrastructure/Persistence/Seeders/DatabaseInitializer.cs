using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Seeders;

/// <summary>
/// Applies migrations and seeds baseline data (roles, dev admin, exercise templates,
/// food items) at startup. Extracted from Program.cs to keep composition-root wiring
/// readable and the seeding logic testable/maintainable in one place.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        bool isDevelopment,
        CancellationToken ct = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseInitializer).FullName!);

        try
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            await db.Database.MigrateAsync(ct);

            await SeedRolesAsync(services);

            // Reference data only. The demo/admin/coach accounts and their sample data
            // are seeded out-of-band via docs/seeds/clean-seed.sql, not from app startup.
            await SeedExerciseTemplatesAsync(db, ct);
            await SeedFoodItemsAsync(db, ct);

            // Food reference-data reads are cached in shared Redis. Invalidate after
            // synchronization so a previous version cannot keep serving stale foods.
            // Exercise templates intentionally bypass Redis and need no invalidation.
            var cacheInvalidator = services.GetService<ICacheInvalidator>();
            if (cacheInvalidator is not null)
                await cacheInvalidator.InvalidateAsync(CacheTags.Foods, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during startup initialization.");
        }
    }

    private static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        string[] roles = [UserRole.Individual, UserRole.Coach, UserRole.Admin];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    private static async Task SeedExerciseTemplatesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var seededTemplates = ExerciseTemplateSeeder.GetTemplates();
        var existingSystemTemplateRows = await db.ExerciseTemplates
            .Where(t => t.OwnerId == null)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

        var changed = false;
        foreach (var retiredTemplate in existingSystemTemplateRows.Where(t =>
                     t.ImageUrl != null &&
                     t.ImageUrl.StartsWith("/exercise-library-images/", StringComparison.OrdinalIgnoreCase)))
        {
            if (retiredTemplate.IsArchived)
                continue;

            retiredTemplate.IsArchived = true;
            retiredTemplate.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }

        var existingSystemTemplates = existingSystemTemplateRows
            .Where(t => t.ImageUrl == null ||
                        !t.ImageUrl.StartsWith("/exercise-library-images/", StringComparison.OrdinalIgnoreCase))
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var seededTemplate in seededTemplates)
        {
            if (!existingSystemTemplates.TryGetValue(seededTemplate.Name, out var existingTemplate))
            {
                db.ExerciseTemplates.Add(seededTemplate);
                changed = true;
                continue;
            }

            if (existingTemplate.Description == seededTemplate.Description &&
                existingTemplate.PrimaryMuscleGroup == seededTemplate.PrimaryMuscleGroup &&
                existingTemplate.SecondaryMuscleGroups.SequenceEqual(seededTemplate.SecondaryMuscleGroups) &&
                existingTemplate.ExerciseKind == seededTemplate.ExerciseKind &&
                existingTemplate.EstimatedMet == seededTemplate.EstimatedMet &&
                existingTemplate.IsCompetitionLift == seededTemplate.IsCompetitionLift &&
                existingTemplate.CompetitionLiftType == seededTemplate.CompetitionLiftType &&
                existingTemplate.ImageUrl == seededTemplate.ImageUrl)
                continue;

            existingTemplate.Description = seededTemplate.Description;
            existingTemplate.PrimaryMuscleGroup = seededTemplate.PrimaryMuscleGroup;
            existingTemplate.SecondaryMuscleGroups = seededTemplate.SecondaryMuscleGroups;
            existingTemplate.ExerciseKind = seededTemplate.ExerciseKind;
            existingTemplate.EstimatedMet = seededTemplate.EstimatedMet;
            existingTemplate.IsCompetitionLift = seededTemplate.IsCompetitionLift;
            existingTemplate.CompetitionLiftType = seededTemplate.CompetitionLiftType;
            existingTemplate.ImageUrl = seededTemplate.ImageUrl;
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync(ct);
    }

    private static async Task SeedFoodItemsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var seededFoods = FoodItemSeeder.GetItems();
        var existingSeedFoodRows = await db.FoodItems
            .Where(f => f.Source == FoodItemSource.Seed)
            .Include(f => f.Servings)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync(ct);
        var existingSeedFoods = existingSeedFoodRows
            .GroupBy(f => f.NameEn, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var changed = false;
        foreach (var (food, servings) in seededFoods)
        {
            if (!existingSeedFoods.TryGetValue(food.NameEn, out var existingFood))
            {
                foreach (var (labelVi, labelEn, grams) in servings)
                    food.Servings.Add(new FoodServing { LabelVi = labelVi, LabelEn = labelEn, Grams = grams });

                db.FoodItems.Add(food);
                changed = true;
                continue;
            }

            if (existingFood.NameVi != food.NameVi ||
                existingFood.CaloriesPer100g != food.CaloriesPer100g ||
                existingFood.ProteinPer100g != food.ProteinPer100g ||
                existingFood.CarbsPer100g != food.CarbsPer100g ||
                existingFood.FatPer100g != food.FatPer100g ||
                existingFood.IsVerified != food.IsVerified)
            {
                existingFood.NameVi = food.NameVi;
                existingFood.CaloriesPer100g = food.CaloriesPer100g;
                existingFood.ProteinPer100g = food.ProteinPer100g;
                existingFood.CarbsPer100g = food.CarbsPer100g;
                existingFood.FatPer100g = food.FatPer100g;
                existingFood.IsVerified = food.IsVerified;
                changed = true;
            }

            var existingServings = existingFood.Servings
                .OrderBy(s => s.CreatedAt)
                .GroupBy(s => s.LabelVi, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            foreach (var (labelVi, labelEn, grams) in servings)
            {
                if (!existingServings.TryGetValue(labelVi, out var existingServing))
                {
                    existingFood.Servings.Add(new FoodServing
                    {
                        LabelVi = labelVi,
                        LabelEn = labelEn,
                        Grams = grams
                    });
                    changed = true;
                    continue;
                }

                if (existingServing.LabelEn == labelEn && existingServing.Grams == grams)
                    continue;

                existingServing.LabelEn = labelEn;
                existingServing.Grams = grams;
                changed = true;
            }
        }

        if (changed)
            await db.SaveChangesAsync(ct);
    }
}
