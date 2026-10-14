namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The kinds of observance of a time zone (RFC 5545 §3.6.5).
    /// </summary>
    public enum IcalTimeZoneObservanceKind
    {
        /// <summary>
        /// Standard time: the STANDARD component.
        /// </summary>
        Standard = 1,

        /// <summary>
        /// Daylight saving time: the DAYLIGHT component.
        /// </summary>
        Daylight = 2
    }
}
