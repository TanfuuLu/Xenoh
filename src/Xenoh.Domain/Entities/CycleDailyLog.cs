using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

/// <summary>
/// One menstrual-cycle log entry per user per day. Unique on (UserId, Date).
/// </summary>
public class CycleDailyLog : BaseEntity
{
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }

    /// <summary>Period flow intensity. Null means no flow logged for that day.</summary>
    public FlowIntensity? Flow { get; set; }

    /// <summary>Bit flags of logged symptoms.</summary>
    public CycleSymptoms Symptoms { get; set; } = CycleSymptoms.None;

    public CycleMood? Mood { get; set; }

    /// <summary>Self-reported energy level, 1 (very low) – 5 (very high).</summary>
    public int? EnergyLevel { get; set; }

    public string? Notes { get; set; }

    public ApplicationUser User { get; set; } = null!;
}
