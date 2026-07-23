namespace Xenoh.Domain.Enums;

[Flags]
public enum SupplementWeekdays
{
    None = 0,
    Sunday = 1 << 0,
    Monday = 1 << 1,
    Tuesday = 1 << 2,
    Wednesday = 1 << 3,
    Thursday = 1 << 4,
    Friday = 1 << 5,
    Saturday = 1 << 6,
    EveryDay = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
}

public enum SupplementIntakeStatus
{
    Taken = 1,
    Skipped = 2
}
