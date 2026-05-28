namespace Xenoh.Application.Features.Users.Preferences;

public static class UserPreferenceValidator
{
    public const string DefaultLanguage = "en";
    public const string DefaultTheme = "light";
    public const string DefaultWeightUnit = "kg";

    private static readonly HashSet<string> AllowedLanguages = ["en", "vi"];
    private static readonly HashSet<string> AllowedThemes = ["light", "dark"];
    private static readonly HashSet<string> AllowedWeightUnits = ["kg", "lb"];

    public static string NormalizeLanguage(string? value)
        => Normalize(value, AllowedLanguages, DefaultLanguage, "Invalid language preference.");

    public static string NormalizeTheme(string? value)
        => Normalize(value, AllowedThemes, DefaultTheme, "Invalid theme preference.");

    public static string NormalizeWeightUnit(string? value)
        => Normalize(value, AllowedWeightUnits, DefaultWeightUnit, "Invalid weight unit preference.");

    private static string Normalize(string? value, HashSet<string> allowedValues, string defaultValue, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        var normalized = value.Trim().ToLowerInvariant();
        if (!allowedValues.Contains(normalized))
            throw new InvalidOperationException(errorMessage);

        return normalized;
    }
}
