using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Seeders;

public static class DemoUserSeeder
{
    private const string DemoEmail = "demo@xenoh.app";
    private const string DemoPassword = "Demo@Xenoh123!";
    private const string ActivePlanName = "24-Week Powerlifting Strength Block";

    private sealed record Ex(string Name, int Sets, int Reps, decimal? BaseWeight, decimal WeeklyIncrement);

    private static readonly Dictionary<string, Ex[]> PowerliftingDays = new()
    {
        ["Squat Primary"] =
        [
            new("Squat", 5, 3, 122.5m, 1.25m),
            new("Bench Press", 4, 5, 82.5m, 0.75m),
            new("Romanian Deadlift", 3, 6, 102.5m, 1.00m),
            new("Plank", 3, 45, null, 0m),
        ],
        ["Bench Volume"] =
        [
            new("Bench Press", 5, 5, 77.5m, 0.75m),
            new("Close-Grip Bench Press", 3, 6, 67.5m, 0.75m),
            new("Barbell Row", 4, 8, 72.5m, 0.75m),
            new("Face Pull", 3, 15, 25.0m, 0.25m),
            new("Tricep Pushdown", 3, 12, 35.0m, 0.25m),
        ],
        ["Deadlift Primary"] =
        [
            new("Deadlift", 4, 3, 142.5m, 1.75m),
            new("Squat", 3, 5, 105.0m, 1.00m),
            new("Lat Pulldown", 4, 10, 67.5m, 0.50m),
            new("Romanian Deadlift", 3, 8, 95.0m, 0.75m),
        ],
        ["Upper Secondary"] =
        [
            new("Bench Press", 4, 6, 72.5m, 0.50m),
            new("Overhead Press", 3, 5, 50.0m, 0.50m),
            new("Barbell Row", 4, 6, 77.5m, 0.75m),
            new("Incline Bench Press", 3, 8, 62.5m, 0.50m),
            new("Barbell Curl", 3, 12, 32.5m, 0.25m),
        ],
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();

        var templates = await db.ExerciseTemplates
            .Where(x => x.OwnerId == null)
            .ToDictionaryAsync(x => x.Name);

        if (!PowerliftingDays.Values.SelectMany(x => x).All(x => templates.ContainsKey(x.Name)))
            return;

        var user = await EnsureDemoUserAsync(userManager);
        await EnsureDemoProfileAsync(userManager, user);

        var history = new HashSet<DateOnly>();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentWeekStart = WeekStart(today);
        var startDate = currentWeekStart.AddDays(-19 * 7);
        var cutoff = today.AddDays(-1);

        await EnsureSubscriptionAsync(db, user.Id);
        if (await IsDemoSeedCurrentAsync(db, user.Id, startDate, today))
        {
            await db.SaveChangesAsync();
            return;
        }

        await ResetGeneratedDemoDataAsync(db, user.Id);
        await db.SaveChangesAsync();

        db.Plans.Add(BuildPowerliftingPlan(
            user.Id,
            startDate,
            weeks: 24,
            templates,
            missedSlots: [7, 31, 54],
            history,
            cutoff));

        SeedBodyweightLogs(db, user.Id, startDate, today);
        SeedPrs(db, user.Id, templates);
        SeedNutrition(db, user.Id, today);

        foreach (var date in history)
            db.WorkoutHistories.Add(new WorkoutHistory { UserId = user.Id, Date = date });

        await db.SaveChangesAsync();
    }

    private static async Task<bool> IsDemoSeedCurrentAsync(
        ApplicationDbContext db,
        Guid userId,
        DateOnly expectedStartDate,
        DateOnly today)
    {
        var expectedEndDate = expectedStartDate.AddDays(24 * 7 - 1);
        var hasCurrentPlan = await db.Plans.AnyAsync(p =>
            p.OwnerId == userId &&
            p.IsActive &&
            p.Name == ActivePlanName &&
            p.StartDate == expectedStartDate &&
            p.EndDate == expectedEndDate);
        if (!hasCurrentPlan)
            return false;

        var recentNutritionDays = await db.NutritionDailyLogs.CountAsync(l =>
            l.UserId == userId &&
            l.Date >= today.AddDays(-27) &&
            l.Date <= today);

        return recentNutritionDays >= 28;
    }

    private static async Task<ApplicationUser> EnsureDemoUserAsync(UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(DemoEmail);
        if (user is not null)
            return user;

        user = new ApplicationUser
        {
            Email = DemoEmail,
            UserName = DemoEmail,
            FirstName = "Lutan",
            LastName = "Fuu",
            CreatedAt = new DateTime(2025, 12, 15, 8, 0, 0, DateTimeKind.Utc),
        };

        var result = await userManager.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException("Failed to create demo user.");

        await userManager.AddToRoleAsync(user, UserRole.Individual);
        return user;
    }

    private static async Task EnsureDemoProfileAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        user.FirstName = "Lutan";
        user.LastName = "Fuu";
        user.Height = 175;
        user.Gender = Gender.Male;
        user.DateOfBirth = new DateOnly(2000, 3, 14);
        user.DevelopmentDirection = DevelopmentDirection.Strength;
        user.TrainingDiscipline = TrainingDiscipline.Powerlifting;
        user.PreferredLanguage = "en";
        user.PreferredTheme = "dark";
        user.PreferredWeightUnit = "kg";
        user.TotalXp = 18_400;
        user.Level = 18;

        await userManager.UpdateAsync(user);
    }

    private static async Task ResetGeneratedDemoDataAsync(ApplicationDbContext db, Guid userId)
    {
        var plans = await db.Plans
            .Where(p => p.OwnerId == userId)
            .ToListAsync();
        db.Plans.RemoveRange(plans);

        db.BodyweightLogs.RemoveRange(await db.BodyweightLogs.Where(x => x.UserId == userId).ToListAsync());
        db.WorkoutHistories.RemoveRange(await db.WorkoutHistories.Where(x => x.UserId == userId).ToListAsync());
        db.UserExercisePRs.RemoveRange(await db.UserExercisePRs.Where(x => x.UserId == userId).ToListAsync());
        db.UserExercisePRHistories.RemoveRange(await db.UserExercisePRHistories.Where(x => x.UserId == userId).ToListAsync());
        db.NutritionDailyLogs.RemoveRange(await db.NutritionDailyLogs.Where(x => x.UserId == userId).ToListAsync());
        db.FoodLogs.RemoveRange(await db.FoodLogs.Where(x => x.UserId == userId).ToListAsync());
        db.AiFeatureCaches.RemoveRange(await db.AiFeatureCaches.Where(x => x.UserId == userId).ToListAsync());

        var nutritionProfile = await db.NutritionProfiles.FirstOrDefaultAsync(x => x.UserId == userId);
        if (nutritionProfile is not null)
            db.NutritionProfiles.Remove(nutritionProfile);
    }

    private static async Task EnsureSubscriptionAsync(ApplicationDbContext db, Guid userId)
    {
        var existing = await db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        var expiry = DateTime.UtcNow.AddYears(1);

        if (existing is null)
        {
            db.UserSubscriptions.Add(new UserSubscription
            {
                UserId = userId,
                Tier = PlanTier.ProIndividual,
                ExpiresAt = expiry,
            });
            return;
        }

        existing.Tier = PlanTier.ProIndividual;
        existing.ExpiresAt = expiry;
    }

    private static Plan BuildPowerliftingPlan(
        Guid userId,
        DateOnly startDate,
        int weeks,
        Dictionary<string, ExerciseTemplate> templates,
        HashSet<int> missedSlots,
        HashSet<DateOnly> history,
        DateOnly cutoff)
    {
        var schedule = new Dictionary<DayOfWeek, string>
        {
            [DayOfWeek.Monday] = "Squat Primary",
            [DayOfWeek.Tuesday] = "Bench Volume",
            [DayOfWeek.Thursday] = "Deadlift Primary",
            [DayOfWeek.Friday] = "Upper Secondary",
        };

        var plan = new Plan
        {
            Name = ActivePlanName,
            StartDate = startDate,
            EndDate = startDate.AddDays(weeks * 7 - 1),
            PlanType = PlanType.Self,
            OwnerId = userId,
            IsActive = true,
        };

        var workoutIndex = 0;
        for (var week = 1; week <= weeks; week++)
        {
            var weekStart = startDate.AddDays((week - 1) * 7);
            var weekly = new WeeklyWorkout
            {
                WeekNumber = week,
                Name = $"Week {week}",
                StartDate = weekStart,
                EndDate = weekStart.AddDays(6),
            };

            for (var dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var date = weekStart.AddDays(dayOffset);
                var day = new DailyWorkout
                {
                    Date = date,
                    DayOfWeek = date.DayOfWeek,
                    Status = DayStatus.Rest,
                };

                if (!schedule.TryGetValue(date.DayOfWeek, out var focus))
                {
                    weekly.DailyWorkouts.Add(day);
                    continue;
                }

                day.Status = DayStatus.Normal;
                var prescriptions = PowerliftingDays[focus];

                if (date > cutoff)
                {
                    AddPlannedExercises(day, prescriptions, templates, week);
                }
                else if (missedSlots.Contains(workoutIndex))
                {
                    day.Status = DayStatus.Missed;
                    AddPlannedExercises(day, prescriptions, templates, week);
                }
                else
                {
                    day.IsCompleted = true;
                    AddCompletedExercises(day, prescriptions, templates, week, focus, date);
                    history.Add(date);
                }

                weekly.DailyWorkouts.Add(day);
                workoutIndex++;
            }

            weekly.IsCompleted = weekly.DailyWorkouts
                .Where(d => d.Exercises.Count > 0)
                .All(d => d.IsCompleted || d.Status == DayStatus.Missed);
            plan.WeeklyWorkouts.Add(weekly);
        }

        return plan;
    }

    private static void AddCompletedExercises(
        DailyWorkout day,
        Ex[] prescriptions,
        Dictionary<string, ExerciseTemplate> templates,
        int week,
        string focus,
        DateOnly date)
    {
        var workoutUtc = new DateTime(date.Year, date.Month, date.Day, 18, 15, 0, DateTimeKind.Utc);

        for (var i = 0; i < prescriptions.Length; i++)
        {
            var p = prescriptions[i];
            if (!templates.TryGetValue(p.Name, out var template))
                continue;

            var plannedWeight = CalculateWeight(p, week);
            var started = workoutUtc.AddMinutes(i * 17);
            var ended = started.AddMinutes(p.Name is "Squat" or "Bench Press" or "Deadlift" ? 16 : 12);
            var exercise = new Exercise
            {
                Name = template.Name,
                PrimaryMuscleGroup = template.PrimaryMuscleGroup,
                SecondaryMuscleGroups = [.. template.SecondaryMuscleGroups],
                ExerciseKind = template.ExerciseKind,
                EstimatedMet = template.EstimatedMet,
                ExerciseTemplateId = template.Id,
                PlannedSets = p.Sets,
                PlannedReps = p.Reps,
                PlannedWeight = plannedWeight,
                SortOrder = i,
                IsCompleted = true,
                XpAwarded = true,
                StartedAtUtc = started,
                EndedAtUtc = ended,
                DurationSeconds = (int)(ended - started).TotalSeconds,
                Notes = i == 0 ? $"{focus} main work" : null,
            };

            for (var set = 1; set <= p.Sets; set++)
            {
                var rpe = CalculateRpe(week, set, p.Sets, p.Name);
                var actualReps = p.Reps;
                var actualWeight = plannedWeight;

                if (ShouldMissFinalRep(date, p.Name, set, p.Sets))
                {
                    actualReps = Math.Max(1, p.Reps - 1);
                    rpe = 9.0m;
                }

                exercise.Sets.Add(new ExerciseSet
                {
                    SetNumber = set,
                    PlannedReps = p.Reps,
                    PlannedWeight = plannedWeight,
                    ActualReps = actualReps,
                    ActualWeight = actualWeight,
                    Rpe = rpe,
                    IsCompleted = true,
                    CompletedAt = started.AddMinutes(set * 3),
                });
            }

            day.Exercises.Add(exercise);
        }
    }

    private static void AddPlannedExercises(
        DailyWorkout day,
        Ex[] prescriptions,
        Dictionary<string, ExerciseTemplate> templates,
        int week)
    {
        for (var i = 0; i < prescriptions.Length; i++)
        {
            var p = prescriptions[i];
            if (!templates.TryGetValue(p.Name, out var template))
                continue;

            var plannedWeight = CalculateWeight(p, week);
            var exercise = new Exercise
            {
                Name = template.Name,
                PrimaryMuscleGroup = template.PrimaryMuscleGroup,
                SecondaryMuscleGroups = [.. template.SecondaryMuscleGroups],
                ExerciseKind = template.ExerciseKind,
                EstimatedMet = template.EstimatedMet,
                ExerciseTemplateId = template.Id,
                PlannedSets = p.Sets,
                PlannedReps = p.Reps,
                PlannedWeight = plannedWeight,
                SortOrder = i,
            };

            for (var set = 1; set <= p.Sets; set++)
            {
                exercise.Sets.Add(new ExerciseSet
                {
                    SetNumber = set,
                    PlannedReps = p.Reps,
                    PlannedWeight = plannedWeight,
                });
            }

            day.Exercises.Add(exercise);
        }
    }

    private static decimal? CalculateWeight(Ex prescription, int week)
    {
        if (prescription.BaseWeight is null)
            return null;

        var load = prescription.BaseWeight.Value + prescription.WeeklyIncrement * (week - 1);
        if (week % 4 == 0)
            load *= 0.92m;

        return Math.Round(load * 2m, MidpointRounding.AwayFromZero) / 2m;
    }

    private static decimal CalculateRpe(int week, int set, int totalSets, string exerciseName)
    {
        var deload = week % 4 == 0;
        var baseRpe = deload ? 6.0m : 6.6m;
        var setRamp = totalSets <= 1 ? 1.0m : (decimal)(set - 1) / (totalSets - 1) * 1.4m;
        var mainLiftBias = exerciseName is "Squat" or "Bench Press" or "Deadlift" ? 0.2m : 0m;
        var waveBias = week % 4 == 3 ? 0.3m : 0m;
        return Math.Min(9.0m, Math.Round(baseRpe + setRamp + mainLiftBias + waveBias, 1));
    }

    private static bool ShouldMissFinalRep(DateOnly date, string exerciseName, int set, int totalSets) =>
        set == totalSets &&
        exerciseName == "Deadlift" &&
        date >= DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-14) &&
        date.DayOfWeek == DayOfWeek.Thursday;

    private static void SeedBodyweightLogs(ApplicationDbContext db, Guid userId, DateOnly startDate, DateOnly today)
    {
        var date = startDate;
        var index = 0;
        while (date <= today)
        {
            var weight = 77.2m + index * 0.045m + (index % 3 == 0 ? 0.15m : 0m);
            db.BodyweightLogs.Add(new BodyweightLog
            {
                UserId = userId,
                Date = date,
                Weight = Math.Round(weight, 1),
            });

            date = date.AddDays(7);
            index++;
        }
    }

    private static void SeedPrs(
        ApplicationDbContext db,
        Guid userId,
        Dictionary<string, ExerciseTemplate> templates)
    {
        var milestones = new (string Name, decimal Weight, int Reps, DateTime At)[]
        {
            ("Squat", 140.0m, 3, Utc(2026, 2, 16)),
            ("Squat", 150.0m, 3, Utc(2026, 4, 13)),
            ("Squat", 157.5m, 2, Utc(2026, 5, 25)),
            ("Bench Press", 95.0m, 5, Utc(2026, 2, 10)),
            ("Bench Press", 102.5m, 3, Utc(2026, 4, 21)),
            ("Bench Press", 107.5m, 2, Utc(2026, 5, 26)),
            ("Deadlift", 165.0m, 3, Utc(2026, 2, 26)),
            ("Deadlift", 175.0m, 2, Utc(2026, 4, 30)),
            ("Deadlift", 182.5m, 1, Utc(2026, 5, 28)),
            ("Overhead Press", 60.0m, 3, Utc(2026, 5, 15)),
        };

        foreach (var group in milestones.GroupBy(x => x.Name))
        {
            if (!templates.TryGetValue(group.Key, out var template))
                continue;

            foreach (var item in group)
            {
                db.UserExercisePRHistories.Add(new UserExercisePRHistory
                {
                    UserId = userId,
                    ExerciseTemplateId = template.Id,
                    Weight = item.Weight,
                    Reps = item.Reps,
                    AchievedAt = item.At,
                });
            }

            var latest = group.OrderBy(x => x.At).Last();
            db.UserExercisePRs.Add(new UserExercisePR
            {
                UserId = userId,
                ExerciseTemplateId = template.Id,
                Weight = latest.Weight,
                Reps = latest.Reps,
                AchievedAt = latest.At,
            });
        }
    }

    private static void SeedNutrition(ApplicationDbContext db, Guid userId, DateOnly today)
    {
        db.NutritionProfiles.Add(new NutritionProfile
        {
            UserId = userId,
            ActivityLevel = ActivityLevel.Athlete,
            Goal = NutritionGoal.Bulk,
            TargetWeightKg = 80.0m,
            CustomCalorieTarget = 3050,
            ProteinPerKg = 2.0m,
            FatPerKg = 0.9m,
        });

        var start = today.AddDays(-83);
        for (var i = 0; i < 84; i++)
        {
            var date = start.AddDays(i);
            var trainingDay = date.DayOfWeek is DayOfWeek.Monday or DayOfWeek.Tuesday or DayOfWeek.Thursday or DayOfWeek.Friday;
            var calories = trainingDay ? 3100 + (i % 5 - 2) * 45 : 2850 + (i % 4 - 1) * 35;
            var protein = trainingDay ? 165m + (i % 4) * 4m : 152m + (i % 3) * 3m;
            var carbs = trainingDay ? 395m + (i % 6) * 8m : 320m + (i % 5) * 7m;
            var fat = Math.Round((calories - protein * 4m - carbs * 4m) / 9m, 1);

            db.NutritionDailyLogs.Add(new NutritionDailyLog
            {
                UserId = userId,
                Date = date,
                Calories = calories,
                ProteinG = protein,
                CarbsG = carbs,
                FatG = fat,
                Notes = trainingDay ? "Training day nutrition" : "Rest day nutrition",
            });
        }
    }

    private static DateOnly WeekStart(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 19, 0, 0, DateTimeKind.Utc);
}
