namespace Xenoh.Application.Common.Analytics;

public enum OneRmFormula
{
    Epley = 0,
    Brzycki = 1
}

/// <summary>Pure 1RM estimators. Both formulas degenerate to <paramref name="weight"/> when reps == 1.</summary>
public static class OneRepMaxCalculator
{
    private const decimal DefaultTrainingMaxPercent = 0.9m;

    public static decimal? Estimate(decimal weight, int reps, OneRmFormula formula = OneRmFormula.Epley)
    {
        if (weight <= 0m || reps <= 0) return null;
        if (reps == 1) return Round(weight);

        return formula switch
        {
            OneRmFormula.Brzycki => Brzycki(weight, reps),
            _ => Epley(weight, reps)
        };
    }

    public static decimal Epley(decimal weight, int reps)
    {
        if (reps <= 1) return Round(weight);
        return Round(weight * (1m + reps / 30m));
    }

    public static decimal? Brzycki(decimal weight, int reps)
    {
        if (reps <= 1) return Round(weight);
        // Formula breaks down as reps → 37; clamp to a safe upper bound.
        if (reps >= 12) return null;
        return Round(weight * 36m / (37m - reps));
    }

    public static decimal TrainingMax(decimal e1Rm, decimal pct = DefaultTrainingMaxPercent) =>
        Round(e1Rm * pct);

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
