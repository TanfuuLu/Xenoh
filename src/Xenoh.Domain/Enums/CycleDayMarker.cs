namespace Xenoh.Domain.Enums;

/// <summary>
/// Cycle-aware annotation for a single calendar day, used to overlay menstrual and
/// pre-menstrual markers onto training plans. Only the two phases that materially
/// affect training capacity are surfaced; everything else is <see cref="None"/>.
/// </summary>
public enum CycleDayMarker
{
    None = 0,

    /// <summary>The luteal days immediately before a (predicted or actual) period start.</summary>
    PreMenstrual = 1,

    /// <summary>A day that falls within an actual logged period or a predicted period span.</summary>
    Menstrual = 2
}
