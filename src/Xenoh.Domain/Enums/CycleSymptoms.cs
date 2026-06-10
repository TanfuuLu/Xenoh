namespace Xenoh.Domain.Enums;

/// <summary>
/// Curated set of cycle-related symptoms logged for a day. Stored as a single int
/// (bit flags); serialized to/from a string array at the DTO layer. Labels are
/// localized in the frontend by enum name.
/// </summary>
[Flags]
public enum CycleSymptoms
{
    None = 0,
    Cramps = 1,
    Headache = 2,
    Bloating = 4,
    BreastTenderness = 8,
    Fatigue = 16,
    BackPain = 32,
    Nausea = 64,
    Acne = 128,
    Cravings = 256,
    Insomnia = 512,
    MoodSwings = 1024
}
