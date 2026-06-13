using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            if (isDevelopment)
            {
                await SeedDevelopmentAdminAsync(services, db, ct);
                await SeedDevelopmentLinhAsync(services);
            }

            await SeedExerciseTemplatesAsync(db, ct);
            await SeedFoodItemsAsync(db, ct);

            if (isDevelopment && !await db.Plans.AnyAsync(ct))
                await DemoUserSeeder.SeedAsync(services);
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

    private static async Task SeedDevelopmentAdminAsync(
        IServiceProvider services,
        ApplicationDbContext db,
        CancellationToken ct)
    {
        const string adminEmail = "admin@xenoh.app";
        const string adminPassword = "Admin@Xenoh123!";

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                Email = adminEmail,
                UserName = adminEmail,
                FirstName = "Admin",
                LastName = "Xenoh",
            };
            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createResult.Succeeded)
                await userManager.AddToRolesAsync(adminUser, [UserRole.Admin, UserRole.Individual, UserRole.Coach]);
        }

        var existingSub = await db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == adminUser.Id, ct);
        var maxExpiry = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        if (existingSub is null)
        {
            db.UserSubscriptions.Add(new UserSubscription
            {
                UserId = adminUser.Id,
                Tier = PlanTier.ProCoach,
                ExpiresAt = maxExpiry,
            });
            await db.SaveChangesAsync(ct);
        }
        else if (existingSub.Tier != PlanTier.ProCoach)
        {
            existingSub.Tier = PlanTier.ProCoach;
            existingSub.ExpiresAt = maxExpiry;
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task SeedDevelopmentLinhAsync(IServiceProvider services)
    {
        const string email = "linhnguyen@xenoh.app";
        const string password = "LinhNguyen123!";

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                FirstName = "Linh",
                LastName = "Nguyen",
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create Linh Nguyen seed user: {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(user, UserRole.Individual))
            await userManager.AddToRoleAsync(user, UserRole.Individual);
    }

    private static async Task SeedExerciseTemplatesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var seededTemplates = ExerciseTemplateSeeder.GetTemplates();
        var existingSystemTemplateRows = await db.ExerciseTemplates
            .Where(t => t.OwnerId == null)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);
        var existingSystemTemplates = existingSystemTemplateRows
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var changed = false;
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
