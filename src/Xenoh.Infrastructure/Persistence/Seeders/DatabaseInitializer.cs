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

            await SeedRolesAsync(services, ct);

            if (isDevelopment)
                await SeedDevelopmentAdminAsync(services, db, ct);

            await SeedExerciseTemplatesAsync(db, ct);
            await SeedFoodItemsAsync(db, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during startup initialization.");
        }
    }

    private static async Task SeedRolesAsync(IServiceProvider services, CancellationToken ct)
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

    private static async Task SeedExerciseTemplatesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var seededTemplates = ExerciseTemplateSeeder.GetTemplates();
        var existingTemplateNameSet = (await db.ExerciseTemplates
                .Select(t => t.Name.ToLower())
                .ToListAsync(ct))
            .ToHashSet();
        var missingTemplates = seededTemplates
            .Where(t => !existingTemplateNameSet.Contains(t.Name.ToLower()))
            .ToList();

        if (missingTemplates.Count > 0)
        {
            db.ExerciseTemplates.AddRange(missingTemplates);
            await db.SaveChangesAsync(ct);
        }

        var seededByName = seededTemplates.ToDictionary(t => t.Name.ToLower());
        var templatesToSync = await db.ExerciseTemplates.ToListAsync(ct);
        var syncedAny = false;
        var toDelete = new List<ExerciseTemplate>();
        foreach (var template in templatesToSync)
        {
            if (!seededByName.TryGetValue(template.Name.ToLower(), out var seededTemplate))
            {
                // Remove system templates that are no longer in the seed list
                if (template.OwnerId == null)
                    toDelete.Add(template);
                continue;
            }

            if (template.ExerciseKind == seededTemplate.ExerciseKind &&
                template.EstimatedMet == seededTemplate.EstimatedMet &&
                template.ImageUrl == seededTemplate.ImageUrl)
                continue;

            template.ExerciseKind = seededTemplate.ExerciseKind;
            template.EstimatedMet = seededTemplate.EstimatedMet;
            template.ImageUrl = seededTemplate.ImageUrl;
            syncedAny = true;
        }

        if (toDelete.Count > 0)
        {
            db.ExerciseTemplates.RemoveRange(toDelete);
            syncedAny = true;
        }

        if (syncedAny)
            await db.SaveChangesAsync(ct);
    }

    private static async Task SeedFoodItemsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var seededFoods = FoodItemSeeder.GetItems();
        var existingFoodNameSet = (await db.FoodItems
                .Where(f => f.Source == FoodItemSource.Seed)
                .Select(f => f.NameEn.ToLower())
                .ToListAsync(ct))
            .ToHashSet();

        // Add new foods (with their servings via navigation) in a single round-trip.
        // BaseEntity.Id is client-generated, so EF fixes up the FK without an interim save.
        var newFoods = seededFoods
            .Where(item => !existingFoodNameSet.Contains(item.Food.NameEn.ToLower()))
            .ToList();
        foreach (var (food, servings) in newFoods)
        {
            foreach (var (labelVi, labelEn, grams) in servings)
                food.Servings.Add(new FoodServing { LabelVi = labelVi, LabelEn = labelEn, Grams = grams });
            db.FoodItems.Add(food);
        }

        if (newFoods.Count > 0)
            await db.SaveChangesAsync(ct);

        // Backfill LabelEn for existing seeded food servings that have NULL LabelEn
        var servingsWithNullLabelEn = await db.FoodServings
            .Where(s => s.LabelEn == null && s.FoodItem.Source == FoodItemSource.Seed)
            .Include(s => s.FoodItem)
            .ToListAsync(ct);
        if (servingsWithNullLabelEn.Count > 0)
        {
            var labelEnMap = seededFoods
                .SelectMany(item => item.Servings.Select(sv => (FoodNameEn: item.Food.NameEn.ToLower(), sv.LabelVi, sv.LabelEn)))
                .ToDictionary(x => (x.FoodNameEn, x.LabelVi), x => x.LabelEn);
            foreach (var serving in servingsWithNullLabelEn)
            {
                if (labelEnMap.TryGetValue((serving.FoodItem.NameEn.ToLower(), serving.LabelVi), out var labelEn))
                    serving.LabelEn = labelEn;
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
