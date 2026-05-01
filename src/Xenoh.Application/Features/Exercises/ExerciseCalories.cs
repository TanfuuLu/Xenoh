namespace Xenoh.Application.Features.Exercises;

public static class ExerciseCalories
{
    public const string Ready = "Ready";
    public const string MissingDuration = "MissingDuration";
    public const string MissingBodyweight = "MissingBodyweight";

    public static (decimal? Calories, string Status) Estimate(
        decimal estimatedMet,
        int? durationSeconds,
        decimal? bodyweightKg)
    {
        if (durationSeconds is null or <= 0)
            return (null, MissingDuration);

        if (bodyweightKg is null)
            return (null, MissingBodyweight);

        var durationHours = (decimal)durationSeconds.Value / 3600m;
        var calories = estimatedMet * bodyweightKg.Value * durationHours;

        return (decimal.Round(calories, 0, MidpointRounding.AwayFromZero), Ready);
    }
}
