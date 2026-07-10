using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Nutrition;
using Xenoh.Application.Common.XP;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Dashboard.Queries.GetPersonalDashboard;

public sealed class GetPersonalDashboardHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IWorkoutHistoryRepository workoutHistoryRepo,
    IBodyweightRepository bodyweightRepo,
    IUserPrRepository userPrRepo,
    ISubscriptionService subscriptionService,
    UserManager<ApplicationUser> userManager,
    IApplicationCache? cache = null
) : IRequestHandler<GetPersonalDashboardQuery, PersonalDashboardResponse>
{
    private static readonly TimeSpan VietnamUtcOffset = TimeSpan.FromHours(7);

    public ValueTask<PersonalDashboardResponse> Handle(
        GetPersonalDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        return new(cache is null
            ? BuildAsync(userId, cancellationToken)
            : cache.GetOrCreateAsync(CacheTags.User(userId), "dashboard", TimeSpan.FromSeconds(15),
                ct => BuildAsync(userId, ct), cancellationToken));
    }

    private async Task<PersonalDashboardResponse> BuildAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var today = GetVietnamToday();

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var latestBodyweight = await bodyweightRepo.GetLatestWeightAsync(userId, cancellationToken);
        var workoutDates = await workoutHistoryRepo.GetSortedDatesDescAsync(userId, cancellationToken);
        var streak = GetMyProfileHandler.CalculateCurrentStreak(workoutDates, today);
        var big3Prs = await userPrRepo.GetBig3Async(userId, cancellationToken);
        var gender = user.Gender.HasValue && Enum.IsDefined(user.Gender.Value) ? user.Gender : null;
        var dots = GetMyProfileHandler.CalculateDots(gender, latestBodyweight, big3Prs);
        var (bmi, bmiCategory) = GetMyProfileHandler.CalculateBmi(user.Height, latestBodyweight);
        var profile = new PersonalDashboardProfileResponse(
            user.FirstName,
            user.AvatarUrl,
            streak,
            Math.Max(1, user.Level),
            user.TotalXp,
            XpCalculator.XpToNextLevel(Math.Max(1, user.Level)),
            XpCalculator.GetTitle(Math.Max(1, user.Level)),
            latestBodyweight,
            bmi,
            bmiCategory,
            dots);

        var activePlanSnapshot = await db.Plans
            .AsNoTracking()
            .Where(p => p.OwnerId == userId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new DashboardPlanSnapshot(
                p.Id,
                p.Name,
                p.StartDate,
                p.EndDate,
                p.WeeklyWorkouts
                    .Select(w => new DashboardWeekSnapshot(
                        w.Id,
                        w.Name,
                        w.StartDate,
                        w.EndDate,
                        w.DailyWorkouts
                            .Select(d => new DashboardDaySnapshot(
                                d.Id,
                                d.Date,
                                d.DayOfWeek,
                                d.Status,
                                d.Exercises
                                    .Select(e => new DashboardExerciseSnapshot(
                                        e.PrimaryMuscleGroup.ToString(),
                                        e.PlannedSets,
                                        e.PlannedReps,
                                        e.PlannedWeight,
                                        e.IsCompleted,
                                        e.SortOrder,
                                        e.Sets
                                            .Select(s => new DashboardSetSnapshot(s.IsCompleted))
                                            .ToList()))
                                    .ToList()))
                            .ToList()))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        var activePlan = activePlanSnapshot is null ? null : BuildPlan(activePlanSnapshot, today);
        var todayWorkout = activePlanSnapshot is null ? null : BuildTodayWorkout(activePlanSnapshot, today);

        var nutrition = await BuildNutritionAsync(user, userId, latestBodyweight, today, cancellationToken);
        var canUseAdvanced = await subscriptionService.CanUseAdvancedAnalyticsAsync(userId, cancellationToken);
        var nextActions = BuildNextActions(activePlan, todayWorkout, nutrition, latestBodyweight, user);
        var proInsights = BuildProInsights(canUseAdvanced, activePlan, todayWorkout, nutrition, streak);

        return new PersonalDashboardResponse(profile, activePlan, todayWorkout, nutrition, nextActions, proInsights);
    }

    private static PersonalDashboardPlanResponse BuildPlan(DashboardPlanSnapshot plan, DateOnly today)
    {
        var days = plan.WeeklyWorkouts.SelectMany(w => w.DailyWorkouts).ToList();
        var trainingDays = days.Where(d => d.Status != DayStatus.Rest).ToList();
        var completedDays = trainingDays.Count(IsCompletedDay);
        var totalDays = trainingDays.Count;
        var currentWeek = plan.WeeklyWorkouts
            .OrderBy(w => w.StartDate)
            .FirstOrDefault(w => w.StartDate <= today && today <= w.EndDate);

        return new PersonalDashboardPlanResponse(
            plan.Id,
            plan.Name,
            plan.StartDate,
            plan.EndDate,
            totalDays,
            completedDays,
            Percent(completedDays, totalDays),
            currentWeek is null ? null : BuildWeek(currentWeek));
    }

    private static PersonalDashboardWeekResponse BuildWeek(DashboardWeekSnapshot week)
    {
        var days = week.DailyWorkouts.Where(d => d.Status != DayStatus.Rest).ToList();
        var completedDays = days.Count(IsCompletedDay);
        return new PersonalDashboardWeekResponse(
            week.Id,
            week.Name,
            week.StartDate,
            week.EndDate,
            days.Count,
            completedDays,
            Percent(completedDays, days.Count));
    }

    private static PersonalDashboardTodayWorkoutResponse? BuildTodayWorkout(DashboardPlanSnapshot plan, DateOnly today)
    {
        var day = plan.WeeklyWorkouts
            .SelectMany(w => w.DailyWorkouts.Select(d => new { Week = w, Day = d }))
            .FirstOrDefault(x => x.Day.Date == today);

        if (day is null)
            return null;

        var exercises = day.Day.Exercises.OrderBy(e => e.SortOrder).ToList();
        var sets = exercises.SelectMany(e => e.Sets).ToList();
        var plannedVolume = exercises.Sum(e => e.PlannedSets * e.PlannedReps * (e.PlannedWeight ?? 0m));
        var muscleGroups = exercises
            .Select(e => e.PrimaryMuscleGroup.ToString())
            .Distinct()
            .Take(5)
            .ToList();

        return new PersonalDashboardTodayWorkoutResponse(
            day.Day.Id,
            day.Week.Id,
            day.Day.DayOfWeek.ToString(),
            day.Day.Date,
            day.Day.Status.ToString(),
            IsCompletedDay(day.Day),
            exercises.Count,
            exercises.Count(e => e.IsCompleted),
            sets.Count,
            sets.Count(s => s.IsCompleted),
            plannedVolume,
            muscleGroups,
            $"/days/{day.Day.Id}");
    }

    private async Task<PersonalDashboardNutritionResponse> BuildNutritionAsync(
        ApplicationUser user,
        Guid userId,
        decimal? latestBodyweight,
        DateOnly today,
        CancellationToken ct)
    {
        var profile = await db.NutritionProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? new NutritionProfile { UserId = userId };

        var log = await db.NutritionDailyLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.UserId == userId && l.Date == today, ct);

        var mealPlan = await db.MealPlanDays
            .AsNoTracking()
            .Where(d => d.UserId == userId && d.Date == today)
            .Select(d => new PersonalDashboardMealPlanResponse(
                d.Meals.SelectMany(m => m.Items).Sum(i => (int?)i.PlannedCalories) ?? 0,
                d.Meals.SelectMany(m => m.Items).Sum(i => (decimal?)i.PlannedProteinG) ?? 0m,
                d.Meals.SelectMany(m => m.Items).Sum(i => (decimal?)i.PlannedCarbsG) ?? 0m,
                d.Meals.SelectMany(m => m.Items).Sum(i => (decimal?)i.PlannedFatG) ?? 0m,
                d.Meals.SelectMany(m => m.Items).Where(i => i.IsChecked).Sum(i => (int?)i.PlannedCalories) ?? 0,
                d.Meals.SelectMany(m => m.Items).Where(i => i.IsChecked).Sum(i => (decimal?)i.PlannedProteinG) ?? 0m,
                d.Meals.SelectMany(m => m.Items).Where(i => i.IsChecked).Sum(i => (decimal?)i.PlannedCarbsG) ?? 0m,
                d.Meals.SelectMany(m => m.Items).Where(i => i.IsChecked).Sum(i => (decimal?)i.PlannedFatG) ?? 0m,
                d.Meals.SelectMany(m => m.Items).Count(),
                d.Meals.SelectMany(m => m.Items).Count(i => i.IsChecked),
                "/nutrition"))
            .FirstOrDefaultAsync(ct);

        var calc = NutritionCalculator.Calculate(user, latestBodyweight, profile, today);

        return new PersonalDashboardNutritionResponse(
            calc.CalorieTarget,
            calc.ProteinG,
            calc.CarbsG,
            calc.FatG,
            log?.Calories ?? 0,
            log?.ProteinG ?? 0m,
            log?.CarbsG ?? 0m,
            log?.FatG ?? 0m,
            calc.CalorieTarget is null ? null : calc.CalorieTarget.Value - (log?.Calories ?? 0),
            calc.ProteinG is null ? null : calc.ProteinG.Value - (log?.ProteinG ?? 0m),
            calc.CarbsG is null ? null : calc.CarbsG.Value - (log?.CarbsG ?? 0m),
            calc.FatG is null ? null : calc.FatG.Value - (log?.FatG ?? 0m),
            calc.MissingFields.ToList(),
            mealPlan);
    }

    private static List<PersonalDashboardActionResponse> BuildNextActions(
        PersonalDashboardPlanResponse? activePlan,
        PersonalDashboardTodayWorkoutResponse? todayWorkout,
        PersonalDashboardNutritionResponse nutrition,
        decimal? latestBodyweight,
        ApplicationUser user)
    {
        var actions = new List<PersonalDashboardActionResponse>();

        if (activePlan is null)
        {
            actions.Add(Action("CreatePlan", "Create your first plan", "Set up training so the hub can guide your day.", "/plans", 10));
        }
        else if (todayWorkout is null)
        {
            actions.Add(Action("ViewPlan", "Review active plan", "No workout is scheduled today. Check the plan ahead.", $"/plans/{activePlan.Id}", 20));
        }
        else if (todayWorkout.Status == DayStatus.Rest.ToString() || todayWorkout.TotalExercises == 0)
        {
            actions.Add(Action("RestDay", "Use the rest day well", "Log nutrition or review the next training day.", "/nutrition", 20));
        }
        else if (!todayWorkout.IsCompleted)
        {
            var label = todayWorkout.CompletedSets > 0 ? "Continue today's workout" : "Start today's workout";
            actions.Add(Action("StartWorkout", label, $"{todayWorkout.CompletedSets}/{todayWorkout.TotalSets} sets completed.", todayWorkout.Route, 10));
        }

        if (nutrition.MissingProfileFields.Count > 0)
        {
            actions.Add(Action("CompleteNutritionProfile", "Complete nutrition setup", "Add missing profile data to unlock calorie and macro targets.", "/nutrition", 30));
        }
        else if (nutrition.LoggedCalories <= 0)
        {
            actions.Add(Action("LogNutrition", "Log today's nutrition", "Track calories and macros against your daily target.", "/nutrition", 30));
        }

        if (latestBodyweight is null or <= 0)
            actions.Add(Action("LogBodyweight", "Log bodyweight", "Bodyweight improves BMI, DOTS, and nutrition calculations.", "/profile", 40));

        if (user.Height is null or <= 0 || user.DateOfBirth is null || user.Gender is null)
            actions.Add(Action("CompleteProfile", "Complete profile", "Add body details for better personalization.", "/profile", 50));

        actions.Add(Action("ViewInsights", "Review insights", "See training recommendations and trends.", "/insights", 80));

        return actions.OrderBy(a => a.Priority).Take(5).ToList();
    }

    private static PersonalDashboardProInsightsResponse BuildProInsights(
        bool canUseAdvanced,
        PersonalDashboardPlanResponse? activePlan,
        PersonalDashboardTodayWorkoutResponse? todayWorkout,
        PersonalDashboardNutritionResponse nutrition,
        int streak)
    {
        if (!canUseAdvanced)
        {
            return new PersonalDashboardProInsightsResponse(
                false,
                "Unlock Pro insights",
                "/subscription",
                []);
        }

        var items = new List<PersonalDashboardInsightResponse>();
        if (activePlan is not null)
        {
            var severity = activePlan.ProgressPercent >= 75 ? "Positive" : activePlan.ProgressPercent >= 40 ? "Info" : "Warning";
            items.Add(new("Plan", severity, "Active plan progress", $"{activePlan.CompletedDays}/{activePlan.TotalDays} training days completed."));
        }

        if (todayWorkout is not null && todayWorkout.TotalSets > 0)
        {
            var pct = Percent(todayWorkout.CompletedSets, todayWorkout.TotalSets);
            items.Add(new("Workout", pct >= 100 ? "Positive" : "Info", "Today's training", $"{pct}% of today's sets are done."));
        }

        if (nutrition.CalorieTarget is not null)
        {
            var remaining = nutrition.RemainingCalories ?? 0;
            var severity = remaining < -300 ? "Warning" : "Info";
            items.Add(new("Nutrition", severity, "Nutrition target", $"{nutrition.LoggedCalories}/{nutrition.CalorieTarget} kcal logged today."));
        }

        if (streak > 0)
            items.Add(new("Consistency", "Positive", "Current streak", $"{streak} training-day streak."));

        return new PersonalDashboardProInsightsResponse(true, null, null, items.Take(3).ToList());
    }

    private static bool IsCompletedDay(DashboardDaySnapshot day) =>
        day.Exercises.Any() && day.Exercises.All(e => e.IsCompleted);

    private static int Percent(int completed, int total) =>
        total <= 0 ? 0 : (int)Math.Round(completed * 100.0 / total);

    private static DateOnly GetVietnamToday() =>
        DateOnly.FromDateTime(DateTime.UtcNow.Add(VietnamUtcOffset));

    private static PersonalDashboardActionResponse Action(
        string type,
        string label,
        string description,
        string route,
        int priority) =>
        new(type, label, description, route, priority);

    private sealed record DashboardPlanSnapshot(
        Guid Id,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        List<DashboardWeekSnapshot> WeeklyWorkouts);

    private sealed record DashboardWeekSnapshot(
        Guid Id,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        List<DashboardDaySnapshot> DailyWorkouts);

    private sealed record DashboardDaySnapshot(
        Guid Id,
        DateOnly Date,
        DayOfWeek DayOfWeek,
        DayStatus Status,
        List<DashboardExerciseSnapshot> Exercises);

    private sealed record DashboardExerciseSnapshot(
        string PrimaryMuscleGroup,
        int PlannedSets,
        int PlannedReps,
        decimal? PlannedWeight,
        bool IsCompleted,
        int SortOrder,
        List<DashboardSetSnapshot> Sets);

    private sealed record DashboardSetSnapshot(bool IsCompleted);
}
