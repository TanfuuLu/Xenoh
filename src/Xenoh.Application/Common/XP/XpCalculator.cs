namespace Xenoh.Application.Common.XP;

public static class XpCalculator
{
    public static int ComputeSetXp(decimal? actualWeight, decimal? plannedWeight,
        int? actualReps, int plannedReps, bool isCompetitionLift)
    {
        var weight = actualWeight ?? plannedWeight ?? 0m;
        var reps = actualReps ?? plannedReps;
        int baseXp = Math.Max(1, (int)Math.Floor(weight / 2.5m)) * reps;
        return isCompetitionLift ? baseXp * 2 : baseXp;
    }

    public static long TotalXpForLevel(int level) =>
        (long)level * (level - 1) / 2 * 1000L;

    public static long XpToNextLevel(int level) => (long)Math.Max(1, level) * 1000L;

    public static int ComputeLevel(long totalXp)
    {
        if (totalXp <= 0) return 1;
        return Math.Max(1, (int)Math.Floor((1.0 + Math.Sqrt(1.0 + 8.0 * totalXp / 1000.0)) / 2.0));
    }

    public static string GetTitle(int level) => level switch
    {
        < 10 => "Beginner",
        < 20 => "Novice",
        < 30 => "Intermediate",
        < 40 => "Dedicated",
        < 50 => "Advanced",
        < 60 => "Expert",
        < 70 => "Elite",
        < 80 => "Master",
        < 90 => "Grandmaster",
        _ => "Legend",
    };
}
