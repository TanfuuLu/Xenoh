using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Services;

public static class PowerliftingScoreCalculator
{
    public const string FormulaVersion = "2020";

    public static decimal Calculate(PowerliftingScoringFormula formula, decimal totalKg, decimal bodyweightKg, string? sex)
    {
        if (totalKg < 0 || bodyweightKg <= 0) throw new ArgumentOutOfRangeException(nameof(totalKg));
        if (formula == PowerliftingScoringFormula.Total) return decimal.Round(totalKg, 4);
        var female = string.Equals(sex, "Female", StringComparison.OrdinalIgnoreCase) || string.Equals(sex, "Women", StringComparison.OrdinalIgnoreCase);
        var bw = (double)bodyweightKg;
        var total = (double)totalKg;
        var value = formula switch
        {
            PowerliftingScoringFormula.Dots => Dots(total, bw, female),
            PowerliftingScoringFormula.Wilks => Wilks(total, bw, female),
            PowerliftingScoringFormula.IpfGlPoints => IpfGl(total, bw, female),
            _ => total
        };
        return decimal.Round((decimal)Math.Max(0, value), 4);
    }

    private static double Dots(double total, double bw, bool female)
    {
        var c = female
            ? new[] { -0.0000010706, 0.0005158568, -0.1126655495, 13.6175032, -57.96288 }
            : new[] { -0.0000010930, 0.0007391293, -0.1918759221, 24.0900756, -307.75076 };
        var denominator = c[0] * Math.Pow(bw, 4) + c[1] * Math.Pow(bw, 3) + c[2] * Math.Pow(bw, 2) + c[3] * bw + c[4];
        return denominator <= 0 ? 0 : total * 500d / denominator;
    }

    private static double Wilks(double total, double bw, bool female)
    {
        var c = female
            ? new[] { 594.31747775582, -27.23842536447, 0.82112226871, -0.00930733913, 0.00004731582, -0.00000009054 }
            : new[] { -216.0475144, 16.2606339, -0.002388645, -0.00113732, 0.00000701863, -0.00000001291 };
        var denominator = c.Select((value, power) => value * Math.Pow(bw, power)).Sum();
        return denominator <= 0 ? 0 : total * 500d / denominator;
    }

    private static double IpfGl(double total, double bw, bool female)
    {
        var (a, b, c) = female
            ? (610.32796d, 1045.59282d, 0.03048d)
            : (1199.72839d, 1025.18162d, 0.00921d);
        var denominator = a - b * Math.Exp(-c * bw);
        return denominator <= 0 ? 0 : total * 100d / denominator;
    }
}
