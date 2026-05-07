namespace Xenoh.Application.Features.CoachClient.Queries.GetCoachDashboard;

public static class CoachAttentionAnalyzer
{
    public static CoachAttentionResult Analyze(
        CoachPlanMonitoringSnapshot? monitoring,
        int? daysSinceLastWorkout)
    {
        var reasons = new List<string>();
        var rank = 0;

        void Raise(int nextRank, string reason)
        {
            rank = Math.Max(rank, nextRank);
            reasons.Add(reason);
        }

        if (monitoring is null)
            Raise(3, "No active plan");

        if (daysSinceLastWorkout is null)
        {
            Raise(3, "No workout history");
        }
        else if (daysSinceLastWorkout > 7)
        {
            Raise(3, "Inactive");
        }
        else if (monitoring is not null && daysSinceLastWorkout >= 4)
        {
            Raise(1, "Inactive");
        }

        if (monitoring is not null)
        {
            if (monitoring.MissedWorkoutDays > 0)
                Raise(2, "Missed days");

            var activeProgress = monitoring.ActivePlanProgressPercent ?? 0;
            if (monitoring.ExpectedProgressPercent - activeProgress >= 25)
                Raise(2, "Behind plan");
        }

        var level = rank switch
        {
            >= 3 => "High",
            2 => "Medium",
            1 => "Low",
            _ => "None"
        };

        return new CoachAttentionResult(level, reasons.Distinct().ToList());
    }
}

public sealed record CoachAttentionResult(string Level, List<string> Reasons);
