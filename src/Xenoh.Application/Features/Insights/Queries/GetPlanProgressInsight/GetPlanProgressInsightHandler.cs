using System.Text.Json;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Cycle.Common;
using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Insights.Queries.GetPlanProgressInsight;

/// <summary>
/// Plan-scoped AI insight. Unlike the account-wide <c>GetUserAnalysis</c> coach notes,
/// this evaluates a single plan from week one through today. Future weeks and future days
/// in the current week are excluded so they cannot reduce progress or adherence metrics.
/// </summary>
public sealed class GetPlanProgressInsightHandler(
    IMediator mediator,
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser,
    IUserAnalysisAi ai
) : IRequestHandler<GetPlanProgressInsightQuery, PlanProgressInsightResponse>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public async ValueTask<PlanProgressInsightResponse> Handle(
        GetPlanProgressInsightQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var language = string.Equals(request.Language, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";

        // Reuse the existing plan analytics computation. This also enforces plan ownership
        // and the Pro-subscription requirement, so no separate authorization is needed here.
        var analytics = await mediator.Send(new GetPlanAnalyticsQuery(request.PlanId), cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var plan = await db.Plans
            .AsNoTracking()
            .Where(p => p.Id == request.PlanId && p.OwnerId == userId)
            .Select(p => new
            {
                p.Name,
                p.StartDate,
                p.EndDate,
                Weeks = p.WeeklyWorkouts
                    .OrderBy(w => w.WeekNumber)
                    .Select(w => new PlanWeekToDate(
                        w.WeekNumber,
                        w.Name,
                        w.StartDate,
                        w.EndDate,
                        w.DailyWorkouts.Count(d => d.Date <= today && d.Status != DayStatus.Rest),
                        w.DailyWorkouts.Count(d => d.Date <= today && d.IsCompleted),
                        w.DailyWorkouts.Count(d => d.Date <= today && d.Status == DayStatus.Missed)))
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        // Cover the complete plan-to-date and the next 21 days so cycle-aware coaching can
        // explain historical patterns and place the next hard sessions appropriately.
        var cycleRangeStart = plan.StartDate <= today ? plan.StartDate : today;
        var cycleContext = await CycleContextBuilder.TryBuildAsync(
            db, userId, cycleRangeStart, today.AddDays(21), cancellationToken);

        var snapshot = BuildPlanToDateSnapshot(
            plan.Name,
            plan.StartDate,
            plan.EndDate,
            plan.Weeks,
            today,
            user,
            analytics,
            cycleContext);
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);

        var aiResult = await ai.GeneratePlanProgressInsightAsync(
            new PlanProgressInsightAiRequest(language, snapshotJson), cancellationToken);

        var content = JsonSerializer.Deserialize<AiContent>(aiResult.Json, JsonOptions)
            ?? throw new InvalidOperationException("AI returned malformed JSON.");

        return new PlanProgressInsightResponse(
            language,
            plan.Name,
            DateTime.UtcNow,
            content.Headline,
            content.Trajectory,
            content.Summary,
            content.WhatsWorking ?? [],
            content.FocusAreas ?? [],
            content.NextBlock ?? []
        );
    }

    private static object BuildPlanToDateSnapshot(
        string planName,
        DateOnly planStartDate,
        DateOnly planEndDate,
        IReadOnlyList<PlanWeekToDate> weeks,
        DateOnly today,
        ApplicationUser user,
        PlanAnalyticsResponse a,
        AiCycleContext? cycleContext)
    {
        var volumeByWeek = a.WeeklyVolume
            .GroupBy(w => w.WeekNumber)
            .ToDictionary(g => g.Key, g => g.Sum(w => w.TotalVolume));

        var reachedWeeks = weeks
            .Where(w => w.StartDate <= today)
            .OrderBy(w => w.WeekNumber)
            .Select(w => new
            {
                w.WeekNumber,
                WeekName = w.Name,
                w.StartDate,
                w.EndDate,
                IsCurrentWeek = w.StartDate <= today && w.EndDate >= today,
                w.ScheduledDaysToDate,
                w.CompletedDaysToDate,
                w.MissedDaysToDate,
                CompletionPercent = w.ScheduledDaysToDate > 0
                    ? (int)Math.Round((decimal)w.CompletedDaysToDate / w.ScheduledDaysToDate * 100m)
                    : 0,
                TotalVolume = volumeByWeek.GetValueOrDefault(w.WeekNumber, 0m)
            })
            .ToList();

        var completedDays = reachedWeeks.Sum(w => w.CompletedDaysToDate);
        var scheduledDays = reachedWeeks.Sum(w => w.ScheduledDaysToDate);
        var totalVolume = reachedWeeks.Sum(w => w.TotalVolume);
        var weeksWithTraining = reachedWeeks.Count(w => w.CompletedDaysToDate > 0 || w.TotalVolume > 0m);
        var currentWeek = reachedWeeks.FirstOrDefault(w => w.IsCurrentWeek)?.WeekNumber;
        var totalPlanDays = Math.Max(1, planEndDate.DayNumber - planStartDate.DayNumber + 1);
        var elapsedPlanDays = Math.Clamp(today.DayNumber - planStartDate.DayNumber + 1, 0, totalPlanDays);
        var status = today < planStartDate ? "NotStarted" : today > planEndDate ? "Completed" : "InProgress";

        return new
        {
            plan = new
            {
                name = planName,
                startDate = planStartDate,
                endDate = planEndDate,
                status,
                totalWeeks = weeks.Count,
                reachedWeeks = reachedWeeks.Count,
                currentWeek,
                elapsedPercent = (int)Math.Round((decimal)elapsedPlanDays / totalPlanDays * 100m)
            },
            profileContext = new
            {
                developmentDirection = user.DevelopmentDirection?.ToString(),
                trainingDiscipline = user.TrainingDiscipline?.ToString(),
            },
            cycleContext,
            planToDate = new
            {
                weeksReached = reachedWeeks.Count,
                weeksWithTraining,
                completedDays,
                scheduledDays,
                completionPercent = scheduledDays > 0
                    ? (int)Math.Round((decimal)completedDays / scheduledDays * 100m)
                    : 0,
                totalVolume
            },
            weeklyProgress = reachedWeeks,
            overallSignals = new
            {
                a.CompletedSets,
                a.AvgRpe,
                a.HighRpeSets,
                a.WarningDays
            },
            muscleGroupVolume = a.MuscleGroupVolume
                .OrderByDescending(m => m.TotalVolume)
                .Take(8)
                .Select(m => new { m.MuscleGroup, m.CompletedSets, m.TotalVolume, m.PercentOfTotal }),
            powerlifting = a.Powerlifting
        };
    }

    private sealed record PlanWeekToDate(
        int WeekNumber,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        int ScheduledDaysToDate,
        int CompletedDaysToDate,
        int MissedDaysToDate);

    private sealed record AiContent(
        string Headline,
        string Trajectory,
        string Summary,
        IReadOnlyList<string>? WhatsWorking,
        IReadOnlyList<string>? FocusAreas,
        IReadOnlyList<string>? NextBlock
    );
}
