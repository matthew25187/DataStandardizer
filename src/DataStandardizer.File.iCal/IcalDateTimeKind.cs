namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The forms of a DATE-TIME value (RFC 5545 §3.3.5).
    /// </summary>
    public enum IcalDateTimeKind
    {
        /// <summary>
        /// A local time not bound to any time zone, such as <c>19980118T230000</c>.  It means the same wall-clock time wherever it is read.
        /// </summary>
        Floating = 0,

        /// <summary>
        /// A time in UTC, such as <c>19980119T070000Z</c>.
        /// </summary>
        Utc = 1,

        /// <summary>
        /// A local time in the time zone named by a <c>TZID</c> parameter, such as <c>TZID=America/New_York:19980119T020000</c>.
        /// </summary>
        Zoned = 2
    }
}
