using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;

namespace Xenoh.Application.Common.Analytics;

public static class TrainingInsightAnalyzer
{
    public static TrainingInsightResult Analyze(TrainingInsightInput input)
    {
        var insights = new List<TrainingInsightResponse>();
        var score = 100;

        score -= ConsistencyPenalty(input.ConsistencyPercent);
        score -= Math.Min(16, input.WarningDays * 4);
        score -= FatiguePenalty(input);

        AddConsistencyInsight(input, insights);
        AddVolumeInsights(input, insights, ref score);
        AddFatigueInsight(input, insights);
        AddRecommendation(input, insights, score);

        score = Math.Clamp(score, 0, 100);
        return new TrainingInsightResult(score, SortInsights(insights));
    }

    private static int ConsistencyPenalty(decimal consistency) => consistency switch
    {
        < 50m => 25,
        < 70m => 15,
        < 85m => 8,
        _ => 0
    };

    private static int FatiguePenalty(TrainingInsightInput input)
    {
        var penalty = 0;
        if (input.AverageRpe >= 8.5m) penalty += 10;
        if (input.HighRpeSetCount >= 5) penalty += 8;
        return penalty;
    }

    private static void AddConsistencyInsight(TrainingInsightInput input, List<TrainingInsightResponse> insights)
    {
        if (input.NonRestDays == 0)
        {
            insights.Add(Insight("Consistency", "Info", "No training days planned",
                "Add training days to the plan before judging consistency.",
                "Planned days", "0"));
            return;
        }

        if (input.ConsistencyPercent < 50m)
        {
            insights.Add(Insight("Consistency", "Critical", "Consistency needs attention",
                "Completion is below 50%. Repeat the week or reduce planned workload before progressing.",
                "Completion", $"{input.ConsistencyPercent}%"));
            return;
        }

        if (input.ConsistencyPercent < 85m)
        {
            insights.Add(Insight("Consistency", "Warning", "Consistency is uneven",
                "Some planned sessions are being missed. Keep the next week stable until adherence improves.",
                "Completion", $"{input.ConsistencyPercent}%"));
            return;
        }

        insights.Add(Insight("Consistency", "Positive", "Strong consistency",
            "Adherence is high enough to support progressive overload.",
            "Completion", $"{input.ConsistencyPercent}%"));
    }

    private static void AddVolumeInsights(
        TrainingInsightInput input,
        List<TrainingInsightResponse> insights,
        ref int score)
    {
        var comparableWeeks = input.WeeklyVolume
            .Where(w => !w.IsPartial)
            .ToList();
        if (comparableWeeks.Count < 2)
        {
            insights.Add(Insight("VolumeTrend", "Info", "More volume history needed",
                "Log at least two training weeks to compare volume trends.",
                "Weeks logged", comparableWeeks.Count.ToString()));
            return;
        }

        var previousWeek = comparableWeeks[^2];
        var currentWeek = comparableWeeks[^1];
        var previous = previousWeek.TotalVolume;
        var current = currentWeek.TotalVolume;
        if (previous <= 0m)
        {
            insights.Add(Insight("VolumeTrend", "Info", "More volume history needed",
                "The previous completed week has no usable volume baseline.",
                "Weeks logged", comparableWeeks.Count.ToString()));
            return;
        }

        var changePercent = Math.Round((current - previous) / previous * 100m, 1);
        var plannedChangePercent = previousWeek.PlannedVolume <= 0m
            ? 0m
            : Math.Round((currentWeek.PlannedVolume - previousWeek.PlannedVolume) /
                previousWeek.PlannedVolume * 100m, 1);
        var currentPlanCompletion = currentWeek.PlannedVolume <= 0m
            ? 0m
            : current / currentWeek.PlannedVolume * 100m;

        if (changePercent <= -20m &&
            plannedChangePercent <= -20m &&
            currentPlanCompletion >= 80m)
        {
            insights.Add(Insight("VolumeTrend", "Info", "Planned volume reduction",
                "Lower volume matches the programmed week and is not treated as lost training.",
                "Volume change", $"{changePercent}%"));
            return;
        }

        if (changePercent <= -35m)
        {
            score -= 20;
            insights.Add(Insight("VolumeTrend", "Critical", "Volume dropped sharply",
                "Training volume fell more than 35% from the previous logged week. Check recovery and missed sessions.",
                "Volume change", $"{changePercent}%"));
            return;
        }

        if (changePercent <= -20m)
        {
            score -= 12;
            insights.Add(Insight("VolumeTrend", "Warning", "Volume is trending down",
                "Training volume dropped more than 20%. Repeat the week or avoid adding load yet.",
                "Volume change", $"{changePercent}%"));
            return;
        }

        if (changePercent >= 30m)
        {
            score -= 8;
            insights.Add(Insight("Overload", "Warning", "Large overload jump",
                "Volume increased more than 30%. Watch fatigue and consider a smaller progression next week.",
                "Volume change", $"+{changePercent}%"));
            return;
        }

        if (changePercent >= 10m)
        {
            insights.Add(Insight("Overload", "Positive", "Progressive overload is moving",
                "Volume is increasing at a useful pace while staying within a manageable range.",
                "Volume change", $"+{changePercent}%"));
        }
        else
        {
            insights.Add(Insight("VolumeTrend", "Info", "Volume is stable",
                "Volume is close to the previous week. Progress with small load or rep increases when recovery is good.",
                "Volume change", $"{changePercent}%"));
        }
    }

    private static void AddFatigueInsight(TrainingInsightInput input, List<TrainingInsightResponse> insights)
    {
        if (input.AverageRpe is null && input.HighRpeSetCount == 0 && input.WarningDays == 0)
            return;

        if (input.AverageRpe >= 8.5m || (input.HighRpeSetCount >= 5 && (input.WarningDays > 0 || input.MissedDays > 0)))
        {
            insights.Add(Insight("FatigueRisk", "Warning", "Fatigue risk is elevated",
                "High RPE work is showing up with warnings or missed training. Consider recovery or a lighter week.",
                "Avg RPE", input.AverageRpe?.ToString("0.0") ?? $"{input.HighRpeSetCount} high-RPE sets"));
            return;
        }

        if (input.WarningDays > 0)
        {
            insights.Add(Insight("FatigueRisk", "Info", "Some sets missed target",
                "A few sessions were below planned reps or weight. Monitor performance before adding load.",
                "Warning days", input.WarningDays.ToString()));
        }
    }

    private static void AddRecommendation(
        TrainingInsightInput input,
        List<TrainingInsightResponse> insights,
        int score)
    {
        if (input.NonRestDays == 0)
            return;

        if (input.AverageRpe >= 8.5m || input.HighRpeSetCount >= 5)
        {
            insights.Add(Insight("Recommendation", "Warning", "Prioritize recovery",
                "Keep load stable or deload slightly until RPE and warnings settle.",
                "High-RPE sets", input.HighRpeSetCount.ToString()));
            return;
        }

        if (input.ConsistencyPercent < 50m)
        {
            insights.Add(Insight("Recommendation", "Critical", "Repeat or simplify the week",
                "Do not progress load yet. Keep remaining sessions on schedule and rebuild consistency without catch-up work.",
                "Training score", Math.Clamp(score, 0, 100).ToString()));
            return;
        }

        if (input.ConsistencyPercent >= 85m && score >= 80)
        {
            insights.Add(Insight("Recommendation", "Positive", "Progress gradually",
                "Add a small load or rep progression to priority lifts next week.",
                "Training score", Math.Clamp(score, 0, 100).ToString()));
            return;
        }

        insights.Add(Insight("Recommendation", "Info", "Hold the plan steady",
            "Maintain current targets and aim for cleaner execution before progressing.",
            "Training score", Math.Clamp(score, 0, 100).ToString()));
    }

    private static List<TrainingInsightResponse> SortInsights(List<TrainingInsightResponse> insights)
    {
        var recommendation = insights.FirstOrDefault(i => i.Type == "Recommendation");
        var supporting = insights.Where(i => i.Type != "Recommendation");

        if (recommendation?.Title is "Repeat or simplify the week" or "Hold the plan steady")
            supporting = supporting.Where(i => i.Type != "Consistency");
        else if (recommendation?.Title == "Prioritize recovery")
            supporting = supporting.Where(i => i.Type != "FatigueRisk");

        var orderedSupporting = supporting
            .OrderBy(i => SeverityRank(i.Severity))
            .ToList();

        return recommendation is null
            ? orderedSupporting.Take(6).ToList()
            : [recommendation, .. orderedSupporting.Take(5)];
    }

    private static int SeverityRank(string severity) => severity switch
    {
        "Critical" => 0,
        "Warning" => 1,
        "Positive" => 2,
        _ => 3
    };

    private static TrainingInsightResponse Insight(
        string type,
        string severity,
        string title,
        string message,
        string metricLabel,
        string metricValue) =>
        new(type, severity, title, message, metricLabel, metricValue);
}

public sealed record TrainingInsightInput(
    decimal ConsistencyPercent,
    int NonRestDays,
    int MissedDays,
    int WarningDays,
    decimal? AverageRpe,
    int HighRpeSetCount,
    List<WeekVolumePoint> WeeklyVolume
);

public sealed record TrainingInsightResult(
    int TrainingScore,
    List<TrainingInsightResponse> Insights
);
