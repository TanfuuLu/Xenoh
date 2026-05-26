using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Seeders;

public static class DemoUserSeeder
{
    private const string DemoEmail    = "demo@xenoh.app";
    private const string DemoPassword = "Demo@Xenoh123!";

    // (templateName, sets, reps, baseWeight, weeklyIncrement)
    private record Ex(string N, int Sets, int Reps, decimal W, decimal Inc);

    // Plan 1 — Foundation Strength (PPL 3×/week)
    private static readonly Dictionary<string, Ex[]> P1 = new()
    {
        ["Push"] =
        [
            new("Bench Press",            4, 5,  60.0m, 1.25m),
            new("Overhead Press",         3, 8,  42.5m, 1.0m),
            new("Incline Dumbbell Press", 3, 10, 18.0m, 0.5m),
            new("Tricep Pushdown",        3, 12, 27.5m, 0.5m),
        ],
        ["Pull"] =
        [
            new("Deadlift",               4, 3, 100.0m, 2.5m),
            new("Barbell Row",            3, 8,  50.0m, 1.0m),
            new("Lat Pulldown",           3, 10, 55.0m, 1.0m),
            new("Barbell Curl",           3, 12, 27.5m, 0.75m),
        ],
        ["Legs"] =
        [
            new("Squat",               4, 5,  82.5m, 2.0m),
            new("Romanian Deadlift",   3, 8,  60.0m, 2.0m),
            new("Leg Press",           3, 12, 120.0m, 2.5m),
            new("Leg Extension",       3, 15,  40.0m, 0.5m),
        ],
    };

    // Plan 2 — Hypertrophy Phase (Upper/Lower 4×/week)
    private static readonly Dictionary<string, Ex[]> P2 = new()
    {
        ["Upper"] =
        [
            new("Bench Press",   4, 8,  67.5m, 1.25m),
            new("Barbell Row",   4, 8,  57.5m, 1.25m),
            new("Overhead Press",3, 10, 45.0m, 1.0m),
            new("Lat Pulldown",  3, 12, 62.5m, 1.25m),
        ],
        ["Lower"] =
        [
            new("Squat",               4, 8,  97.5m, 1.5m),
            new("Romanian Deadlift",   4, 10, 72.5m, 1.5m),
            new("Leg Press",           3, 15, 140.0m, 2.5m),
            new("Leg Curl",            3, 15,  42.5m, 0.75m),
        ],
    };

    // Plan 3 — Strength Focus (Push/Pull/Legs/Upper 4×/week)
    private static readonly Dictionary<string, Ex[]> P3 = new()
    {
        ["Push"] =
        [
            new("Bench Press",         4, 5,  80.0m, 2.5m),
            new("Overhead Press",      3, 5,  55.0m, 1.25m),
            new("Incline Bench Press", 3, 8,  62.5m, 1.25m),
            new("Tricep Pushdown",     3, 12, 40.0m, 0.5m),
        ],
        ["Pull"] =
        [
            new("Deadlift",          4, 3, 130.0m, 2.5m),
            new("Barbell Row",       3, 6,  70.0m, 1.25m),
            new("Lat Pulldown",      3, 10, 72.5m, 1.0m),
            new("Barbell Curl",      3, 12, 35.0m, 0.5m),
        ],
        ["Legs"] =
        [
            new("Squat",             4, 5, 110.0m, 2.5m),
            new("Romanian Deadlift", 3, 8,  90.0m, 2.5m),
            new("Leg Press",         3, 10, 160.0m, 2.5m),
            new("Leg Extension",     3, 15,  45.0m, 0.5m),
        ],
        ["Upper"] =
        [
            new("Bench Press",   4, 8,  75.0m, 2.5m),
            new("Barbell Row",   4, 8,  65.0m, 1.25m),
            new("Overhead Press",3, 10, 47.5m, 1.0m),
            new("Lat Pulldown",  3, 12, 67.5m, 1.0m),
        ],
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db          = services.GetRequiredService<ApplicationDbContext>();

        var templates = await db.ExerciseTemplates
            .Where(x => x.OwnerId == null)
            .ToDictionaryAsync(x => x.Name);

        if (!templates.ContainsKey("Bench Press")) return; // templates not seeded yet

        // ── User ──────────────────────────────────────────────────────────────
        var existingUser = await userManager.FindByEmailAsync(DemoEmail);

        ApplicationUser user;
        if (existingUser is not null)
        {
            // User exists — check if data was already seeded
            var hasPlans = await db.Plans.AnyAsync(p => p.OwnerId == existingUser.Id);
            if (hasPlans) return;

            // User exists but data is missing — patch profile fields and continue
            existingUser.FirstName   = "Lutan";
            existingUser.LastName    = "Fuu";
            existingUser.Height      = 175;
            existingUser.Gender      = Gender.Male;
            existingUser.DateOfBirth = new DateOnly(2000, 3, 14);
            existingUser.CreatedAt   = new DateTime(2024, 11, 1, 8, 0, 0, DateTimeKind.Utc);
            existingUser.TotalXp     = 8_500;
            existingUser.Level       = 9;
            await userManager.UpdateAsync(existingUser);
            user = existingUser;
        }
        else
        {
            user = new ApplicationUser
            {
                Email       = DemoEmail,
                UserName    = DemoEmail,
                FirstName   = "Lutan",
                LastName    = "Fuu",
                Height      = 175,
                Gender      = Gender.Male,
                DateOfBirth = new DateOnly(2000, 3, 14),
                CreatedAt   = new DateTime(2024, 11, 1, 8, 0, 0, DateTimeKind.Utc),
                TotalXp     = 8_500,
                Level       = 9,
            };

            if (!(await userManager.CreateAsync(user, DemoPassword)).Succeeded) return;
            await userManager.AddToRoleAsync(user, UserRole.Individual);
        }

        // ── Subscription ─────────────────────────────────────────────────────
        var hasSub = await db.UserSubscriptions.AnyAsync(s => s.UserId == user.Id);
        if (!hasSub)
            db.UserSubscriptions.Add(new UserSubscription
            {
                UserId    = user.Id,
                Tier      = PlanTier.ProIndividual,
                ExpiresAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            });

        // ── Bodyweight logs (weekly, Nov 4 2024 – May 19 2025) ───────────────
        decimal[] bwSeries =
        [
            78.0m, 77.8m, 77.6m, 77.7m, 77.4m, 77.5m, 77.2m, 77.3m,
            77.0m, 76.8m, 76.9m, 76.6m, 76.7m, 76.4m, 76.5m, 76.2m,
            76.3m, 76.0m, 76.1m, 75.8m, 75.9m, 75.7m, 75.8m, 75.5m,
            75.6m, 75.4m, 75.5m, 75.3m,
        ];
        var bwDate = new DateOnly(2024, 11, 4);
        foreach (var w in bwSeries)
        {
            db.BodyweightLogs.Add(new BodyweightLog { UserId = user.Id, Weight = w, Date = bwDate });
            bwDate = bwDate.AddDays(7);
        }

        // ── PRs ───────────────────────────────────────────────────────────────
        SeedPrs(db, user.Id, templates);

        // ── Plans ─────────────────────────────────────────────────────────────
        var history = new HashSet<DateOnly>();

        db.Plans.Add(BuildPlan(
            userId:      user.Id,
            name:        "Foundation Strength",
            startDate:   new DateOnly(2024, 11, 4),
            weeks:       8,
            isActive:    false,
            schedule:    new()
            {
                [DayOfWeek.Monday]    = "Push",
                [DayOfWeek.Wednesday] = "Pull",
                [DayOfWeek.Friday]    = "Legs",
            },
            exerciseMap: P1,
            templates:   templates,
            missedSlots: [3, 9, 16, 22],
            history:     history,
            cutoff:      null));

        db.Plans.Add(BuildPlan(
            userId:      user.Id,
            name:        "Hypertrophy Phase",
            startDate:   new DateOnly(2025, 1, 6),
            weeks:       12,
            isActive:    false,
            schedule:    new()
            {
                [DayOfWeek.Monday]   = "Upper",
                [DayOfWeek.Tuesday]  = "Lower",
                [DayOfWeek.Thursday] = "Upper",
                [DayOfWeek.Friday]   = "Lower",
            },
            exerciseMap: P2,
            templates:   templates,
            missedSlots: [4, 11, 19, 26, 34, 41],
            history:     history,
            cutoff:      null));

        db.Plans.Add(BuildPlan(
            userId:      user.Id,
            name:        "Strength Focus",
            startDate:   new DateOnly(2025, 4, 7),
            weeks:       12,
            isActive:    true,
            schedule:    new()
            {
                [DayOfWeek.Monday]   = "Push",
                [DayOfWeek.Tuesday]  = "Pull",
                [DayOfWeek.Thursday] = "Legs",
                [DayOfWeek.Friday]   = "Upper",
            },
            exerciseMap: P3,
            templates:   templates,
            missedSlots: [8, 22],
            history:     history,
            cutoff:      new DateOnly(2025, 5, 21)));

        // ── WorkoutHistory (for streak calculation) ──────────────────────────
        foreach (var date in history)
            db.WorkoutHistories.Add(new WorkoutHistory { UserId = user.Id, Date = date });

        await db.SaveChangesAsync();
    }

    // ── Plan builder ─────────────────────────────────────────────────────────

    private static Plan BuildPlan(
        Guid userId,
        string name,
        DateOnly startDate,
        int weeks,
        bool isActive,
        Dictionary<DayOfWeek, string> schedule,
        Dictionary<string, Ex[]> exerciseMap,
        Dictionary<string, ExerciseTemplate> templates,
        HashSet<int> missedSlots,
        HashSet<DateOnly> history,
        DateOnly? cutoff)
    {
        var plan = new Plan
        {
            Name      = name,
            StartDate = startDate,
            EndDate   = startDate.AddDays(weeks * 7 - 1),
            PlanType  = PlanType.Self,
            OwnerId   = userId,
            IsActive  = isActive,
        };

        int workoutIdx = 0;

        for (int week = 1; week <= weeks; week++)
        {
            var weekStart = startDate.AddDays((week - 1) * 7);
            var weekEnd   = weekStart.AddDays(6);

            var ww = new WeeklyWorkout
            {
                WeekNumber = week,
                Name       = $"Week {week}",
                StartDate  = weekStart,
                EndDate    = weekEnd,
            };

            for (int d = 0; d < 7; d++)
            {
                var date = weekStart.AddDays(d);
                var dow  = date.DayOfWeek;

                if (!schedule.TryGetValue(dow, out var dayType))
                {
                    ww.DailyWorkouts.Add(new DailyWorkout
                    {
                        Date       = date,
                        DayOfWeek  = dow,
                        IsCompleted = false,
                        Status     = DayStatus.Rest,
                    });
                    continue;
                }

                // Future day: create planned-but-not-done
                if (cutoff.HasValue && date > cutoff.Value)
                {
                    var futureDay = new DailyWorkout
                    {
                        Date       = date,
                        DayOfWeek  = dow,
                        IsCompleted = false,
                        Status     = DayStatus.Normal,
                    };
                    if (exerciseMap.TryGetValue(dayType, out var futurePx))
                        AddPlannedExercises(futureDay, futurePx, templates, week);
                    ww.DailyWorkouts.Add(futureDay);
                    continue;
                }

                // Past day
                bool isMissed   = missedSlots.Contains(workoutIdx);
                bool isCompleted = !isMissed;

                var daily = new DailyWorkout
                {
                    Date        = date,
                    DayOfWeek   = dow,
                    IsCompleted = isCompleted,
                    Status      = isMissed ? DayStatus.Missed : DayStatus.Normal,
                };

                if (exerciseMap.TryGetValue(dayType, out var px))
                {
                    var workoutUtc = new DateTime(date.Year, date.Month, date.Day, 18, 30, 0, DateTimeKind.Utc);
                    if (isCompleted)
                    {
                        AddCompletedExercises(daily, px, templates, week, workoutUtc);
                        history.Add(date);
                    }
                    else
                    {
                        AddPlannedExercises(daily, px, templates, week);
                    }
                }

                ww.DailyWorkouts.Add(daily);
                workoutIdx++;
            }

            // Week is complete once all effective days have a terminal state.
            bool weekPassed     = !cutoff.HasValue || weekEnd <= cutoff.Value;
            bool anyIncompletePast = ww.DailyWorkouts.Any(d =>
                d.Status == DayStatus.Normal &&
                !d.IsCompleted &&
                (!cutoff.HasValue || d.Date <= cutoff.Value));
            ww.IsCompleted = weekPassed && !anyIncompletePast;

            plan.WeeklyWorkouts.Add(ww);
        }

        return plan;
    }

    // ── Exercise helpers ─────────────────────────────────────────────────────

    private static void AddCompletedExercises(
        DailyWorkout day,
        Ex[] prescriptions,
        Dictionary<string, ExerciseTemplate> templates,
        int weekNum,
        DateTime workoutUtc)
    {
        for (int i = 0; i < prescriptions.Length; i++)
        {
            var p = prescriptions[i];
            if (!templates.TryGetValue(p.N, out var tmpl)) continue;

            var weight = p.W + p.Inc * (weekNum - 1);
            var ex = new Exercise
            {
                Name                   = tmpl.Name,
                PrimaryMuscleGroup     = tmpl.PrimaryMuscleGroup,
                SecondaryMuscleGroups  = [.. tmpl.SecondaryMuscleGroups],
                ExerciseKind           = tmpl.ExerciseKind,
                EstimatedMet           = tmpl.EstimatedMet,
                ExerciseTemplateId     = tmpl.Id,
                PlannedSets            = p.Sets,
                PlannedReps            = p.Reps,
                PlannedWeight          = weight,
                SortOrder              = i,
                IsCompleted            = true,
                XpAwarded              = true,
                StartedAtUtc           = workoutUtc.AddMinutes(i * 18),
                EndedAtUtc             = workoutUtc.AddMinutes(i * 18 + 15),
            };

            for (int s = 1; s <= p.Sets; s++)
            {
                var rpe = 7.0m + (s == p.Sets ? 1.5m : 0.5m) + (weekNum % 4 == 0 ? 0.5m : 0m);
                ex.Sets.Add(new ExerciseSet
                {
                    SetNumber     = s,
                    PlannedReps   = p.Reps,
                    PlannedWeight = weight,
                    ActualReps    = p.Reps,
                    ActualWeight  = weight,
                    Rpe           = Math.Min(10m, rpe),
                    IsCompleted   = true,
                    CompletedAt   = workoutUtc.AddMinutes(i * 18 + s * 3),
                });
            }

            day.Exercises.Add(ex);
        }
    }

    private static void AddPlannedExercises(
        DailyWorkout day,
        Ex[] prescriptions,
        Dictionary<string, ExerciseTemplate> templates,
        int weekNum)
    {
        for (int i = 0; i < prescriptions.Length; i++)
        {
            var p = prescriptions[i];
            if (!templates.TryGetValue(p.N, out var tmpl)) continue;

            var weight = p.W + p.Inc * (weekNum - 1);
            var ex = new Exercise
            {
                Name                  = tmpl.Name,
                PrimaryMuscleGroup    = tmpl.PrimaryMuscleGroup,
                SecondaryMuscleGroups = [.. tmpl.SecondaryMuscleGroups],
                ExerciseKind          = tmpl.ExerciseKind,
                EstimatedMet          = tmpl.EstimatedMet,
                ExerciseTemplateId    = tmpl.Id,
                PlannedSets           = p.Sets,
                PlannedReps           = p.Reps,
                PlannedWeight         = weight,
                SortOrder             = i,
                IsCompleted           = false,
                XpAwarded             = false,
            };

            for (int s = 1; s <= p.Sets; s++)
                ex.Sets.Add(new ExerciseSet
                {
                    SetNumber     = s,
                    PlannedReps   = p.Reps,
                    PlannedWeight = weight,
                    IsCompleted   = false,
                });

            day.Exercises.Add(ex);
        }
    }

    // ── PRs ───────────────────────────────────────────────────────────────────
    // UserExercisePR has a unique index on (UserId, ExerciseTemplateId) — one
    // current-best row per exercise.  Older milestones go to UserExercisePRHistories.

    private static void SeedPrs(
        ApplicationDbContext db,
        Guid userId,
        Dictionary<string, ExerciseTemplate> t)
    {
        // All milestones ordered chronologically; last entry per exercise = current PR
        (string Name, decimal Weight, int Reps, DateTime At)[] allMilestones =
        [
            ("Bench Press",    65.0m, 5, Utc(2024, 11, 18)),
            ("Bench Press",    70.0m, 5, Utc(2024, 12,  9)),
            ("Bench Press",    75.0m, 5, Utc(2025,  1, 20)),
            ("Bench Press",    80.0m, 3, Utc(2025,  3,  3)),
            ("Bench Press",    85.0m, 3, Utc(2025,  4, 28)),  // current best
            ("Squat",          87.5m, 5, Utc(2024, 11, 22)),
            ("Squat",          97.5m, 5, Utc(2024, 12, 13)),
            ("Squat",         107.5m, 5, Utc(2025,  2,  7)),
            ("Squat",         112.5m, 3, Utc(2025,  4, 14)),  // current best
            ("Deadlift",      105.0m, 3, Utc(2024, 11, 27)),
            ("Deadlift",      117.5m, 3, Utc(2024, 12, 18)),
            ("Deadlift",      127.5m, 3, Utc(2025,  2, 19)),
            ("Deadlift",      135.0m, 2, Utc(2025,  4, 30)),  // current best
            ("Overhead Press",  47.5m, 5, Utc(2024, 12,  4)),
            ("Overhead Press",  55.0m, 5, Utc(2025,  3, 19)),
            ("Overhead Press",  57.5m, 3, Utc(2025,  5, 12)),  // current best
        ];

        // Group by exercise; last in list = current best (highest weight / latest date)
        var grouped = allMilestones
            .GroupBy(x => x.Name)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (name, milestones) in grouped)
        {
            if (!t.TryGetValue(name, out var tmpl)) continue;

            // All milestones → history table (no unique constraint)
            foreach (var (_, weight, reps, at) in milestones)
                db.UserExercisePRHistories.Add(new UserExercisePRHistory
                {
                    UserId             = userId,
                    ExerciseTemplateId = tmpl.Id,
                    Weight             = weight,
                    Reps               = reps,
                    AchievedAt         = at,
                });

            // Latest milestone only → current-best table (unique per user+exercise)
            var best = milestones[^1];
            db.UserExercisePRs.Add(new UserExercisePR
            {
                UserId             = userId,
                ExerciseTemplateId = tmpl.Id,
                Weight             = best.Weight,
                Reps               = best.Reps,
                AchievedAt         = best.At,
            });
        }
    }

    private static DateTime Utc(int y, int m, int d) =>
        new(y, m, d, 19, 0, 0, DateTimeKind.Utc);
}
