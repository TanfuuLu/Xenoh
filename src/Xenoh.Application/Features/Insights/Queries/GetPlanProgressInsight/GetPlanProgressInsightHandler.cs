using System.Text.Json;
using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Cycle.Common;
using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Insights.Queries.GetPlanProgressInsight;

/// <summary>
/// Plan-scoped AI insight. Unlike the account-wide <c>GetUserAnalysis</c> coach notes,
/// this focuses only on a single plan's week-over-week trajectory, reusing the already
/// computed plan analytics (weekly compliance/volume trends, muscle distribution, rule insights).
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

        var planName = await db.Plans
            .AsNoTracking()
            .Where(p => p.Id == request.PlanId && p.OwnerId == userId)
            .Select(p => p.Name)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Plan not found.");

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        // Past 28 days explain recent weekly dips; the next 21 days inform the next-block scheduling.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cycleContext = await CycleContextBuilder.TryBuildAsync(
            db, userId, today.AddDays(-28), today.AddDays(21), cancellationToken);

        var snapshot = BuildTrendSnapshot(planName, user, analytics, cycleContext);
        var snapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);

        var aiResult = await ai.GeneratePlanProgressInsightAsync(
            new PlanProgressInsightAiRequest(language, snapshotJson), cancellationToken);

        var content = JsonSerializer.Deserialize<AiContent>(aiResult.Json, JsonOptions)
            ?? throw new InvalidOperationException("AI returned malformed JSON.");

        return new PlanProgressInsightResponse(
            language,
            planName,
            DateTime.UtcNow,
            content.Headline,
            content.Trajectory,
            content.Summary,
            content.WhatsWorking ?? [],
            content.FocusAreas ?? [],
            content.NextBlock ?? []
        );
    }

    // How many recent trained weeks to feed the trajectory read.
    private const int RecentWeekWindow = 6;

    private static object BuildTrendSnapshot(
        string planName,
        ApplicationUser user,
        PlanAnalyticsResponse a,
        AiCycleContext? cycleContext)
    {
        // The plan can span weeks the user has not trained yet (future weeks with 0 completion
        // and 0 volume). Those would distort the trajectory, so we drop the not-yet-trained tail
        // and keep only the most recent trained weeks.
        var volumeByWeek = a.WeeklyVolume
            .GroupBy(w => w.WeekNumber)
            .ToDictionary(g => g.Key, g => g.Sum(w => w.TotalVolume));

        var lastActiveWeek = 0;
        foreach (var w in a.WeeklyCompliance)
        {
            var vol = volumeByWeek.TryGetValue(w.WeekNumber, out var v) ? v : 0m;
            if (w.CompletedDays > 0 || vol > 0)
                lastActiveWeek = Math.Max(lastActiveWeek, w.WeekNumber);
        }

        var minWeek = Math.Max(1, lastActiveWeek - RecentWeekWindow + 1);

        bool InWindow(int weekNumber) =>
            lastActiveWeek > 0 && weekNumber >= minWeek && weekNumber <= lastActiveWeek;

        var recentCompliance = a.WeeklyCompliance
            .Where(w => InWindow(w.WeekNumber))
            .OrderBy(w => w.WeekNumber)
            .ToList();
        var recentVolume = a.WeeklyVolume
            .Where(w => InWindow(w.WeekNumber))
            .OrderBy(w => w.WeekNumber)
            .ToList();

        var recentCompletedDays = recentCompliance.Sum(w => w.CompletedDays);
        var recentScheduledDays = recentCompliance.Sum(w => w.TotalDays);

        return new
        {
            planName,
            profileContext = new
            {
                developmentDirection = user.DevelopmentDirection?.ToString(),
                trainingDiscipline = user.TrainingDiscipline?.ToString(),
            },
            cycleContext,
            // Aggregates over the recent trained weeks only (excludes not-yet-trained weeks).
            recentWeeks = new
            {
                weeksTrained = recentCompliance.Count,
                firstWeek = recentCompliance.Count > 0 ? recentCompliance[0].WeekNumber : 0,
                lastWeek = lastActiveWeek,
                completedDays = recentCompletedDays,
                scheduledDays = recentScheduledDays,
                completionPercent = recentScheduledDays > 0
                    ? (int)Math.Round((decimal)recentCompletedDays / recentScheduledDays * 100m)
                    : 0,
                totalVolume = recentVolume.Sum(w => w.TotalVolume),
            },
            weeklyCompletion = recentCompliance
                .Select(w => new { w.WeekNumber, w.WeekName, w.CompletedDays, w.TotalDays }),
            weeklyVolume = recentVolume
                .Select(w => new { w.WeekNumber, w.WeekName, w.TotalVolume }),
            // Plan-wide effort/quality signals for context (not cumulative totals).
            overallSignals = new
            {
                a.AvgRpe,
                a.HighRpeSets,
                a.TrainingScore,
            },
            muscleGroupVolume = a.MuscleGroupVolume
                .OrderByDescending(m => m.TotalVolume)
                .Take(8)
                .Select(m => new { m.MuscleGroup, m.CompletedSets, m.TotalVolume, m.PercentOfTotal }),
            ruleInsights = a.Insights
                .Select(i => new { i.Type, i.Severity, i.Title, i.Message }),
        };
    }

    private sealed record AiContent(
        string Headline,
        string Trajectory,
        string Summary,
        IReadOnlyList<string>? WhatsWorking,
        IReadOnlyList<string>? FocusAreas,
        IReadOnlyList<string>? NextBlock
    );
}
