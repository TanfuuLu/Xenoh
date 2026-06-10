namespace Xenoh.Domain.Enums;

/// <summary>
/// Menstrual flow intensity for a logged day. A day with no period flow stores null.
/// </summary>
public enum FlowIntensity
{
    Spotting = 1,
    Light = 2,
    Medium = 3,
    Heavy = 4
}
