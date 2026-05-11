using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Seeders;

public static class DemoDataSeeder
{
    private const string CoachEmail = "demo.coach@xenoh.app";
    private const string ClientEmail = "demo.client@xenoh.app";
    private const string Password = "Demo@Xenoh123!";
    private const string DemoPlanName = "Demo Strength Rebuild";

    private static readonly DayOfWeek[] TrainingDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Wednesday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday
    ];

    public static async Task SeedAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        CancellationToken cancellationToken = default)
    {
        var coach = await UpsertUserAsync(
            userManager,
            CoachEmail,
            "Demo",
            "Coach",
            [UserRole.Coach, UserRole.Individual],
            cancellationToken);

        coach.Height = 178;
        coach.Gender = Gender.Male;
        coach.DateOfBirth = new DateOnly(1988, 6, 12);
        coach.Bio = "Powerlifting and strength coach focused on simple programming, clear feedback, and sustainable progress.";
        coach.AvatarUrl = "/avatars/demo-coach.png";
        await userManager.UpdateAsync(coach);

        var client = await UpsertUserAsync(
            userManager,
            ClientEmail,
            "Demo",
            "Client",
            [UserRole.Individual],
            cancellationToken);

        client.Height = 174;
        client.Gender = Gender.Male;
        client.DateOfBirth = new DateOnly(1996, 3, 18);
        client.Bio = "Intermediate lifter rebuilding consistency while improving squat, bench, and deadlift technique.";
        client.AvatarUrl = "/avatars/demo-client.png";
        client.TotalXp = 12_450;
        client.Level = 5;
        await userManager.UpdateAsync(client);

        await UpsertSubscriptionAsync(db, coach.Id, PlanTier.ProCoach, cancellationToken);
        await UpsertSubscriptionAsync(db, client.Id, PlanTier.ProIndividual, cancellationToken);
        await UpsertCoachProfileAsync(db, coach.Id, cancellationToken);
        await UpsertRelationshipAsync(db, coach.Id, client.Id, cancellationToken);
        await UpsertNutritionAsync(db, client.Id, cancellationToken);
        await UpsertBodyweightAsync(db, client.Id, cancellationToken);

        var templateMap = await db.ExerciseTemplates
            .AsNoTracking()
            .Where(t => !t.IsArchived)
            .ToDictionaryAsync(t => t.Name, cancellationToken);

        await RecreatePlanAsync(db, coach.Id, client.Id, templateMap, cancellationToken);
        await UpsertPersonalRecordsAsync(db, client.Id, templateMap, cancellationToken);
    }

    private static async Task<ApplicationUser> UpsertUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string firstName,
        string lastName,
        string[] roles,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Email = email,
                UserName = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, Password);
            if (!createResult.Succeeded)
                throw new InvalidOperationException($"Failed to create demo user {email}: {FormatErrors(createResult)}");
        }
        else
        {
            user.FirstName = firstName;
            user.LastName = lastName;
            user.EmailConfirmed = true;
            user.UserName = email;
            user.Email = email;
            await userManager.UpdateAsync(user);
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var missingRoles = roles.Except(currentRoles).ToArray();
        if (missingRoles.Length > 0)
            await userManager.AddToRolesAsync(user, missingRoles);

        cancellationToken.ThrowIfCancellationRequested();
        return user;
    }

    private static async Task UpsertSubscriptionAsync(
        ApplicationDbContext db,
        Guid userId,
        PlanTier tier,
        CancellationToken cancellationToken)
    {
        var subscription = await db.UserSubscriptions.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
        if (subscription is null)
        {
            db.UserSubscriptions.Add(new UserSubscription
            {
                UserId = userId,
                Tier = tier,
                ExpiresAt = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc)
            });
        }
        else
        {
            subscription.Tier = tier;
            subscription.ExpiresAt = new DateTime(9999, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            subscription.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertCoachProfileAsync(
        ApplicationDbContext db,
        Guid coachId,
        CancellationToken cancellationToken)
    {
        var profile = await db.CoachMarketplaceProfiles.FirstOrDefaultAsync(p => p.CoachId == coachId, cancellationToken);
        if (profile is null)
        {
            profile = new CoachMarketplaceProfile { CoachId = coachId };
            db.CoachMarketplaceProfiles.Add(profile);
        }

        profile.Headline = "Strength coach for intermediate lifters";
        profile.ExperienceYears = 7;
        profile.Specialties = ["Powerlifting", "Strength programming", "Technique review", "Habit building"];
        profile.Certifications = ["NASM CPT", "Powerlifting Club Coach"];
        profile.Languages = ["English", "Vietnamese"];
        profile.CoachingMethods = ["Weekly check-ins", "Video feedback", "Plan reviews", "Nutrition accountability"];
        profile.Achievements = ["Coached 40+ recreational lifters", "Helped clients add 10-30 kg to competition lifts"];
        profile.ClientResultsSummary = "Most clients improve consistency first, then add steady strength across 8-12 week blocks.";
        profile.Availability = "Accepting 3 new online clients this month.";
        profile.ResponseTime = "Usually replies within 24 hours.";
        profile.CoachingStyle = "Direct, practical, and data-informed without overcomplicating training.";
        profile.MonthlyPriceAmount = 2_500_000m;
        profile.SessionPriceAmount = 450_000m;
        profile.Currency = "VND";
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertRelationshipAsync(
        ApplicationDbContext db,
        Guid coachId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var relationship = await db.CoachClientRelationships
            .FirstOrDefaultAsync(r => r.ClientId == clientId, cancellationToken);

        if (relationship is null)
        {
            relationship = new CoachClientRelationship { ClientId = clientId };
            db.CoachClientRelationships.Add(relationship);
        }

        relationship.CoachId = coachId;
        relationship.Status = RelationshipStatus.Active;
        relationship.StartDate = Today().AddDays(-30);
        relationship.EndDate = null;
        relationship.TerminationRequestedBy = null;
        relationship.RenewalRequestedBy = null;
        relationship.ProposedEndDate = null;
        relationship.SelectedCoachingType = CoachingType.Monthly;
        relationship.SelectedQuantity = 3;
        relationship.SelectedPriceAmount = 2_500_000m;
        relationship.SelectedCurrency = "VND";
        relationship.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertNutritionAsync(
        ApplicationDbContext db,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var profile = await db.NutritionProfiles.FirstOrDefaultAsync(p => p.UserId == clientId, cancellationToken);
        if (profile is null)
        {
            profile = new NutritionProfile { UserId = clientId };
            db.NutritionProfiles.Add(profile);
        }

        profile.ActivityLevel = ActivityLevel.VeryActive;
        profile.Goal = NutritionGoal.Maintain;
        profile.TargetWeightKg = 78.5m;
        profile.CustomCalorieTarget = 2_650;
        profile.ProteinPerKg = 2.0m;
        profile.FatPerKg = 0.8m;
        profile.UpdatedAt = DateTime.UtcNow;

        var today = Today();
        for (var i = 13; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var log = await db.NutritionDailyLogs
                .FirstOrDefaultAsync(l => l.UserId == clientId && l.Date == date, cancellationToken);

            if (log is null)
            {
                log = new NutritionDailyLog { UserId = clientId, Date = date };
                db.NutritionDailyLogs.Add(log);
            }

            var offset = 13 - i;
            log.Calories = 2_420 + offset % 5 * 70;
            log.ProteinG = 150 + offset % 4 * 8;
            log.CarbsG = 250 + offset % 6 * 14;
            log.FatG = 70 + offset % 3 * 5;
            log.Notes = offset % 4 == 0 ? "Meal prep day. Easy to stay on target." : "Normal tracking day.";
            log.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertBodyweightAsync(
        ApplicationDbContext db,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var today = Today();
        for (var i = 27; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var log = await db.BodyweightLogs
                .FirstOrDefaultAsync(l => l.UserId == clientId && l.Date == date, cancellationToken);

            if (log is null)
            {
                log = new BodyweightLog { UserId = clientId, Date = date };
                db.BodyweightLogs.Add(log);
            }

            var offset = 27 - i;
            log.Weight = Math.Round(80.2m - offset * 0.045m + (offset % 4 - 1) * 0.08m, 2);
            log.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task RecreatePlanAsync(
        ApplicationDbContext db,
        Guid coachId,
        Guid clientId,
        Dictionary<string, ExerciseTemplate> templateMap,
        CancellationToken cancellationToken)
    {
        var existingPlans = await db.Plans
            .Where(p => p.OwnerId == clientId && p.Name == DemoPlanName)
            .ToListAsync(cancellationToken);

        if (existingPlans.Count > 0)
        {
            db.Plans.RemoveRange(existingPlans);
            await db.SaveChangesAsync(cancellationToken);
        }

        var today = Today();
        var startDate = StartOfWeek(today).AddDays(-21);
        var endDate = startDate.AddDays(55);
        var plan = new Plan
        {
            Name = DemoPlanName,
            OwnerId = clientId,
            CreatedByCoachId = coachId,
            PlanType = PlanType.Coach,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true
        };

        for (var weekIndex = 0; weekIndex < 8; weekIndex++)
        {
            var weekStart = startDate.AddDays(weekIndex * 7);
            var week = new WeeklyWorkout
            {
                PlanId = plan.Id,
                WeekNumber = weekIndex + 1,
                Name = $"Week {weekIndex + 1}: {WeekTheme(weekIndex)}",
                StartDate = weekStart,
                EndDate = weekStart.AddDays(6)
            };

            for (var dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var date = weekStart.AddDays(dayOffset);
                var isTrainingDay = IsTrainingDay(date, today);
                var day = new DailyWorkout
                {
                    WeeklyWorkoutId = week.Id,
                    Date = date,
                    DayOfWeek = date.DayOfWeek,
                    Status = isTrainingDay ? DayStatus.Normal : DayStatus.Rest
                };

                if (isTrainingDay)
                    AddExercises(day, templateMap, weekIndex, date, today);

                if (date == today.AddDays(-3) && isTrainingDay)
                {
                    day.Status = DayStatus.Missed;
                    day.IsCompleted = false;
                    day.Exercises.Clear();
                }
                else if (date < today)
                {
                    day.IsCompleted = day.Status is DayStatus.Rest or DayStatus.Missed || day.Exercises.All(e => e.IsCompleted);
                }

                week.DailyWorkouts.Add(day);
            }

            week.IsCompleted = week.DailyWorkouts.All(d =>
                d.IsCompleted || d.Status is DayStatus.Rest or DayStatus.Missed);
            plan.WeeklyWorkouts.Add(week);
        }

        db.Plans.Add(plan);
        await db.SaveChangesAsync(cancellationToken);

        await AddDemoCommentsAsync(db, plan, coachId, clientId, cancellationToken);
        await UpsertWorkoutHistoryAsync(db, clientId, cancellationToken);
    }

    private static void AddExercises(
        DailyWorkout day,
        Dictionary<string, ExerciseTemplate> templateMap,
        int weekIndex,
        DateOnly date,
        DateOnly today)
    {
        var workoutDay = date == today && !TrainingDays.Contains(date.DayOfWeek)
            ? DayOfWeek.Wednesday
            : date.DayOfWeek;

        var exercises = workoutDay switch
        {
            DayOfWeek.Monday => new[]
            {
                Spec("Squat", 4, 5, 110 + weekIndex * 2.5m),
                Spec("Romanian Deadlift", 3, 8, 82.5m + weekIndex * 2.5m),
                Spec("Leg Press", 3, 10, 160 + weekIndex * 5m)
            },
            DayOfWeek.Wednesday => new[]
            {
                Spec("Bench Press", 4, 6, 72.5m + weekIndex * 1.25m),
                Spec("Barbell Row", 4, 8, 70 + weekIndex * 2.5m),
                Spec("Overhead Press", 3, 6, 45 + weekIndex * 1.25m)
            },
            DayOfWeek.Friday => new[]
            {
                Spec("Deadlift", 3, 4, 140 + weekIndex * 2.5m),
                Spec("Front Squat", 3, 6, 85 + weekIndex * 2.5m),
                Spec("Lat Pulldown", 3, 10, 62.5m + weekIndex * 2.5m)
            },
            DayOfWeek.Saturday => new[]
            {
                Spec("Incline Bench Press", 3, 8, 62.5m + weekIndex * 1.25m),
                Spec("Pull-Up", 4, 6, null),
                Spec("Cycling", 1, 30, null)
            },
            _ => []
        };

        var sortOrder = 0;
        foreach (var spec in exercises)
        {
            if (!templateMap.TryGetValue(spec.TemplateName, out var template))
                continue;

            var exercise = new Exercise
            {
                DailyWorkoutId = day.Id,
                ExerciseTemplateId = template.Id,
                Name = template.Name,
                PrimaryMuscleGroup = template.PrimaryMuscleGroup,
                SecondaryMuscleGroups = template.SecondaryMuscleGroups,
                ExerciseKind = template.ExerciseKind,
                EstimatedMet = template.EstimatedMet,
                PlannedSets = spec.Sets,
                PlannedReps = spec.Reps,
                PlannedWeight = spec.Weight,
                SortOrder = sortOrder++,
                Notes = spec.TemplateName == "Cycling" ? "Zone 2 pace, nasal breathing if possible." : null
            };

            var shouldComplete = date < today;
            var partialToday = date == today && sortOrder <= 2;
            if (shouldComplete || partialToday)
            {
                exercise.StartedAtUtc = DateTime.UtcNow.AddDays(-(today.DayNumber - date.DayNumber)).AddMinutes(-45);
                exercise.EndedAtUtc = exercise.StartedAtUtc.Value.AddMinutes(spec.TemplateName == "Cycling" ? 30 : 9 + spec.Sets * 3);
                exercise.DurationSeconds = (int)(exercise.EndedAtUtc.Value - exercise.StartedAtUtc.Value).TotalSeconds;
            }

            for (var setNumber = 1; setNumber <= spec.Sets; setNumber++)
            {
                var set = new ExerciseSet
                {
                    ExerciseId = exercise.Id,
                    SetNumber = setNumber,
                    PlannedReps = spec.Reps,
                    PlannedWeight = spec.Weight
                };

                var setDone = shouldComplete || (partialToday && setNumber <= Math.Max(1, spec.Sets - 1));
                if (setDone)
                {
                    set.IsCompleted = true;
                    set.CompletedAt = DateTime.UtcNow.AddDays(-(today.DayNumber - date.DayNumber)).AddMinutes(-30 + setNumber * 4);
                    set.ActualReps = spec.Reps - (setNumber == spec.Sets && weekIndex % 3 == 0 ? 1 : 0);
                    set.ActualWeight = spec.Weight;
                    set.Rpe = Math.Min(9.0m, 7.0m + (setNumber * 0.25m) + (weekIndex % 3) * 0.25m);
                }

                exercise.Sets.Add(set);
            }

            exercise.IsCompleted = exercise.Sets.Count > 0 && exercise.Sets.All(s => s.IsCompleted);
            exercise.XpAwarded = exercise.IsCompleted && exercise.DurationSeconds is > 60;
            day.Exercises.Add(exercise);
        }
    }

    private static async Task AddDemoCommentsAsync(
        ApplicationDbContext db,
        Plan plan,
        Guid coachId,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        db.PlanComments.AddRange(
            new PlanComment
            {
                PlanId = plan.Id,
                AuthorId = coachId,
                Content = "Demo note: this block is built to test coach-created plans, comments, progress, and workout execution."
            },
            new PlanComment
            {
                PlanId = plan.Id,
                AuthorId = clientId,
                Content = "Feeling good with the current volume. Bench is moving better than squat this week."
            });

        var currentWeek = plan.WeeklyWorkouts.OrderBy(w => w.WeekNumber).FirstOrDefault(w =>
            Today() >= w.StartDate && Today() <= w.EndDate);

        if (currentWeek is not null)
        {
            db.WeeklyWorkoutComments.AddRange(
                new WeeklyWorkoutComment
                {
                    WeeklyWorkoutId = currentWeek.Id,
                    AuthorId = coachId,
                    Content = "Keep RPE around 7-8 this week. Add 2.5 kg only if bar speed stays consistent."
                },
                new WeeklyWorkoutComment
                {
                    WeeklyWorkoutId = currentWeek.Id,
                    AuthorId = clientId,
                    Content = "Sleep was uneven, but warm-ups felt normal. I will keep the first work set conservative."
                });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertWorkoutHistoryAsync(
        ApplicationDbContext db,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var completedDates = await db.DailyWorkouts
            .Where(d => d.WeeklyWorkout.Plan.OwnerId == clientId && d.IsCompleted && d.Exercises.Any())
            .Select(d => d.Date)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var date in completedDates)
        {
            var exists = await db.WorkoutHistories
                .AnyAsync(h => h.UserId == clientId && h.Date == date, cancellationToken);
            if (!exists)
                db.WorkoutHistories.Add(new WorkoutHistory { UserId = clientId, Date = date });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task UpsertPersonalRecordsAsync(
        ApplicationDbContext db,
        Guid clientId,
        Dictionary<string, ExerciseTemplate> templateMap,
        CancellationToken cancellationToken)
    {
        var records = new[]
        {
            ("Squat", 125m, 5),
            ("Bench Press", 85m, 4),
            ("Deadlift", 160m, 3),
            ("Overhead Press", 52.5m, 5)
        };

        foreach (var (name, weight, reps) in records)
        {
            if (!templateMap.TryGetValue(name, out var template))
                continue;

            var pr = await db.UserExercisePRs
                .FirstOrDefaultAsync(p => p.UserId == clientId && p.ExerciseTemplateId == template.Id, cancellationToken);

            if (pr is null)
            {
                pr = new UserExercisePR
                {
                    UserId = clientId,
                    ExerciseTemplateId = template.Id,
                    AchievedAt = DateTime.UtcNow.AddDays(-7)
                };
                db.UserExercisePRs.Add(pr);
            }

            pr.Weight = weight;
            pr.Reps = reps;
            pr.UpdatedAt = DateTime.UtcNow;

            var hasHistory = await db.UserExercisePRHistories
                .AnyAsync(h => h.UserId == clientId && h.ExerciseTemplateId == template.Id, cancellationToken);
            if (!hasHistory)
            {
                db.UserExercisePRHistories.Add(new UserExercisePRHistory
                {
                    UserId = clientId,
                    ExerciseTemplateId = template.Id,
                    Weight = weight - 7.5m,
                    Reps = reps,
                    AchievedAt = DateTime.UtcNow.AddDays(-21)
                });
                db.UserExercisePRHistories.Add(new UserExercisePRHistory
                {
                    UserId = clientId,
                    ExerciseTemplateId = template.Id,
                    Weight = weight,
                    Reps = reps,
                    AchievedAt = DateTime.UtcNow.AddDays(-7)
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static ExerciseSpec Spec(string templateName, int sets, int reps, decimal? weight) =>
        new(templateName, sets, reps, weight);

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-diff);
    }

    private static bool IsTrainingDay(DateOnly date, DateOnly today) =>
        date == today || TrainingDays.Contains(date.DayOfWeek);

    private static string WeekTheme(int weekIndex) => weekIndex switch
    {
        0 => "Technique baseline",
        1 => "Volume build",
        2 => "Load practice",
        3 => "Recovery checkpoint",
        4 => "Strength push",
        5 => "Top set practice",
        6 => "Peak volume",
        _ => "Test and reset"
    };

    private static string FormatErrors(IdentityResult result) =>
        string.Join(", ", result.Errors.Select(e => e.Description));

    private readonly record struct ExerciseSpec(string TemplateName, int Sets, int Reps, decimal? Weight);
}
