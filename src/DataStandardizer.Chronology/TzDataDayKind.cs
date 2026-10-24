namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Identifies how the day of a TZ Database rule transition or zone line UNTIL is specified.
    /// </summary>
    public enum TzDataDayKind
    {
        /// <summary>
        /// A fixed day of the month, for example <c>5</c>.
        /// </summary>
        DayOfMonth,

        /// <summary>
        /// The last given weekday of the month, for example <c>lastSun</c>.
        /// </summary>
        LastWeekday,

        /// <summary>
        /// The first given weekday on or after a day of the month, for example <c>Sun&gt;=8</c>.
        /// </summary>
        WeekdayOnOrAfter,

        /// <summary>
        /// The last given weekday on or before a day of the month, for example <c>Sun&lt;=25</c>.
        /// </summary>
        WeekdayOnOrBefore
    }
}
