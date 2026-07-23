using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public sealed class SupplementDoseSlot : BaseEntity
{
    public Guid ScheduleVersionId { get; set; }
    public decimal Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public TimeOnly Time { get; set; }
    public SupplementWeekdays Weekdays { get; set; }

    public SupplementScheduleVersion ScheduleVersion { get; set; } = null!;
    public ICollection<SupplementIntakeLog> IntakeLogs { get; set; } = [];

    public bool OccursOn(DateOnly date)
    {
        var day = date.DayOfWeek switch
        {
            DayOfWeek.Sunday => SupplementWeekdays.Sunday,
            DayOfWeek.Monday => SupplementWeekdays.Monday,
            DayOfWeek.Tuesday => SupplementWeekdays.Tuesday,
            DayOfWeek.Wednesday => SupplementWeekdays.Wednesday,
            DayOfWeek.Thursday => SupplementWeekdays.Thursday,
            DayOfWeek.Friday => SupplementWeekdays.Friday,
            DayOfWeek.Saturday => SupplementWeekdays.Saturday,
            _ => SupplementWeekdays.None
        };

        return (Weekdays & day) != 0;
    }
}
