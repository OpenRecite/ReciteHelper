namespace ReciteHelper.Core.Scheduling;

/// <summary>
/// Converts wall-clock timestamps into the whole-day elapsed times consumed by
/// <see cref="FsrsAlgorithm"/>.  A "day" starts at <see cref="RolloverHour"/>
/// local time (04:00, as in Anki), so a session that runs past midnight still
/// counts as the same study day.
/// </summary>
public static class ReviewCalendar
{
    public const int RolloverHour = 4;

    /// <summary>Ordinal study-day index of a timestamp.</summary>
    public static int DayIndex(DateTime moment)
    {
        var local = moment.Kind == DateTimeKind.Utc ? moment.ToLocalTime() : moment;
        return (int)Math.Floor((local - new DateTime(2000, 1, 1)).TotalDays - RolloverHour / 24.0);
    }

    /// <summary>
    /// Whole study days elapsed between two reviews; never negative (clock adjustments
    /// are treated as a same-day review).
    /// </summary>
    public static int ElapsedDays(DateTime previous, DateTime current)
        => Math.Max(0, DayIndex(current) - DayIndex(previous));
}
