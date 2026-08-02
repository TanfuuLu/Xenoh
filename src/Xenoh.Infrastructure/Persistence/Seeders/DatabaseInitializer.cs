using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Data;
using System.Reflection;
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
    private const string DemoSeedResourceName =
        "Xenoh.Infrastructure.Persistence.Seeders.clean-seed.sql";

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
            await InitializeCoreAsync(services, db, logger, ct);
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

    /// <summary>
    /// Permanently drops the configured development database, rebuilds it from all
    /// migrations, and loads the comprehensive demo dataset. This is intentionally
    /// reachable only through the explicit API command-line maintenance switch.
    /// </summary>
    public static async Task RebuildDemoDatabaseAsync(
        IServiceProvider services,
        bool isDevelopment,
        CancellationToken ct = default)
    {
        if (!isDevelopment)
            throw new InvalidOperationException("Demo database rebuild is allowed only in Development.");

        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseInitializer).FullName!);
        var db = services.GetRequiredService<ApplicationDbContext>();
        var total = Stopwatch.StartNew();

        logger.LogWarning("Dropping and rebuilding development database {Database}.",
            db.Database.GetDbConnection().Database);

        var phase = Stopwatch.StartNew();
        await db.Database.EnsureDeletedAsync(ct);
        logger.LogInformation("Database drop completed in {ElapsedMs} ms.", phase.ElapsedMilliseconds);

        phase.Restart();
        await InitializeCoreAsync(services, db, logger, ct);
        logger.LogInformation("Migrations and reference seed completed in {ElapsedMs} ms.", phase.ElapsedMilliseconds);

        phase.Restart();
        await SeedDemoDataAsync(db, ct);
        logger.LogInformation(
            "Demo seed completed in {ElapsedMs} ms; full rebuild completed in {TotalElapsedMs} ms.",
            phase.ElapsedMilliseconds,
            total.ElapsedMilliseconds);
    }

    public static async Task SeedDemoDatabaseAsync(
        IServiceProvider services,
        bool isDevelopment,
        CancellationToken ct = default)
    {
        if (!isDevelopment)
            throw new InvalidOperationException("Demo database seeding is allowed only in Development.");

        var logger = services.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(DatabaseInitializer).FullName!);
        var db = services.GetRequiredService<ApplicationDbContext>();
        var timer = Stopwatch.StartNew();
        await SeedDemoDataAsync(db, ct);
        logger.LogInformation("Demo seed completed in {ElapsedMs} ms.", timer.ElapsedMilliseconds);
    }

    private static async Task SeedDemoDataAsync(ApplicationDbContext db, CancellationToken ct)
    {
        await using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(DemoSeedResourceName)
            ?? throw new InvalidOperationException($"Embedded demo seed '{DemoSeedResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        var sql = await reader.ReadToEndAsync(ct);
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
            await connection.OpenAsync(ct);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 30;
            await command.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }

    private static async Task InitializeCoreAsync(
        IServiceProvider services,
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken ct)
    {
        var phase = Stopwatch.StartNew();
        await db.Database.MigrateAsync(ct);
        logger.LogInformation("Database migrations checked/applied in {ElapsedMs} ms.", phase.ElapsedMilliseconds);

        phase.Restart();
        await SeedRolesAsync(services);
        await SeedExerciseTemplatesAsync(db, ct);
        await SeedFoodItemsAsync(db, ct);
        logger.LogInformation("Reference data synchronized in {ElapsedMs} ms.", phase.ElapsedMilliseconds);

        var cacheInvalidator = services.GetService<ICacheInvalidator>();
        if (cacheInvalidator is not null)
            await cacheInvalidator.InvalidateAsync(CacheTags.Foods, ct);
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
