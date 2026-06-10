using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

/// <summary>
/// Per-user cycle-tracking settings. One row per user.
/// Overrides are null when the app should auto-compute the value from logged history.
/// </summary>
public class CycleSettings : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>Manual average cycle length in days (15–60). Null = auto-compute.</summary>
    public int? AverageCycleLengthOverride { get; set; }

    /// <summary>Manual average period length in days (1–10). Null = auto-compute.</summary>
    public int? AveragePeriodLengthOverride { get; set; }

    /// <summary>When true, the user's coach may view a phase/prediction summary (never symptoms/notes).</summary>
    public bool ShareWithCoach { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
