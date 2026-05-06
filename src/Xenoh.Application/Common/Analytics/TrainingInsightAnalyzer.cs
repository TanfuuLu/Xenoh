using Xenoh.Application.Features.Plans.Queries.GetPlanAnalytics;

namespace Xenoh.Application.Common.Analytics;

public static class TrainingInsightAnalyzer
{
    private static readonly HashSet<string> MajorMuscleGroups = ["Chest", "Back", "Quads", "Hamstrings"];

    public static TrainingInsightResult Analyze(TrainingInsightInput input)
    {
        var insights = new List<TrainingInsightResponse>();
        var score = 100;

        score -= ConsistencyPenalty(input.ConsistencyPercent);
        score -= Math.Min(20, input.MissedDays * 5);
        score -= Math.Min(16, input.WarningDays * 4);
        score -= FatiguePenalty(input);
        score -= MuscleBalancePenalty(input.MuscleGroupVolume);

        AddConsistencyInsight(input, insights);
        AddVolumeInsights(input, insights, ref score);
        AddFatigueInsight(input, insights);
        AddMuscleBalanceInsight(input, insights);
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

    private static int MuscleBalancePenalty(List<MuscleGroupPoint> muscleGroups)
    {
        if (muscleGroups.Count == 0) return 0;

        var dominant = muscleGroups.Max(m => m.PercentOfTotal);
        var penalty = dominant switch
        {
            > 50m => 18,
            > 35m => 10,
            _ => 0
        };

        var present = muscleGroups.Where(m => m.TotalVolume > 0).Select(m => m.MuscleGroup).ToHashSet();
        if (MajorMuscleGroups.Any(m => !present.Contains(m)))
            penalty += 6;

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
        var nonZeroWeeks = input.WeeklyVolume.Where(w => w.TotalVolume > 0m).ToList();
        if (nonZeroWeeks.Count < 2)
        {
            insights.Add(Insight("VolumeTrend", "Info", "More volume history needed",
                "Log at least two training weeks to compare volume trends.",
                "Weeks logged", nonZeroWeeks.Count.ToString()));
            return;
        }

        var previous = nonZeroWeeks[^2].TotalVolume;
        var current = nonZeroWeeks[^1].TotalVolume;
        if (previous <= 0m) return;

        var changePercent = Math.Round((current - previous) / previous * 100m, 1);

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

    private static void AddMuscleBalanceInsight(TrainingInsightInput input, List<TrainingInsightResponse> insights)
    {
        if (input.MuscleGroupVolume.Count == 0) return;

        var dominant = input.MuscleGroupVolume.OrderByDescending(m => m.PercentOfTotal).First();
        var present = input.MuscleGroupVolume.Where(m => m.TotalVolume > 0).Select(m => m.MuscleGroup).ToHashSet();
        var missing = MajorMuscleGroups.Where(m => !present.Contains(m)).ToList();

        if (dominant.PercentOfTotal > 50m)
        {
            insights.Add(Insight("MuscleBalance", "Critical", "Training is heavily concentrated",
                $"{dominant.MuscleGroup} is taking more than half of weighted volume. Add balance before pushing intensity.",
                dominant.MuscleGroup, $"{dominant.PercentOfTotal}%"));
            return;
        }

        if (dominant.PercentOfTotal > 35m)
        {
            insights.Add(Insight("MuscleBalance", "Warning", "Muscle focus is unbalanced",
                $"{dominant.MuscleGroup} dominates the plan. Balance volume across major muscle groups.",
                dominant.MuscleGroup, $"{dominant.PercentOfTotal}%"));
            return;
        }

        if (missing.Count > 0)
        {
            insights.Add(Insight("MuscleBalance", "Info", "Major muscle gaps detected",
                $"No weighted work found for {string.Join(", ", missing)}.",
                "Missing groups", missing.Count.ToString()));
            return;
        }

        insights.Add(Insight("MuscleBalance", "Positive", "Muscle distribution looks balanced",
            "No single muscle group is dominating weighted volume.",
            "Top group", $"{dominant.PercentOfTotal}%"));
    }

    private static void AddRecommendation(
        TrainingInsightInput input,
        List<TrainingInsightResponse> insights,
        int score)
    {
        if (score < 60 || input.ConsistencyPercent < 50m)
        {
            insights.Add(Insight("Recommendation", "Critical", "Repeat or simplify the week",
                "Do not progress load yet. Reduce friction, repeat key sessions, and rebuild consistency.",
                "Training score", Math.Clamp(score, 0, 100).ToString()));
            return;
        }

        if (input.AverageRpe >= 8.5m || input.HighRpeSetCount >= 5)
        {
            insights.Add(Insight("Recommendation", "Warning", "Prioritize recovery",
                "Keep load stable or deload slightly until RPE and warnings settle.",
                "High-RPE sets", input.HighRpeSetCount.ToString()));
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

    private static List<TrainingInsightResponse> SortInsights(List<TrainingInsightResponse> insights) =>
        insights
            .OrderBy(i => SeverityRank(i.Severity))
            .ThenBy(i => i.Type == "Recommendation" ? 0 : 1)
            .Take(6)
            .ToList();

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
    List<WeekVolumePoint> WeeklyVolume,
    List<MuscleGroupPoint> MuscleGroupVolume
);

public sealed record TrainingInsightResult(
    int TrainingScore,
    List<TrainingInsightResponse> Insights
);
