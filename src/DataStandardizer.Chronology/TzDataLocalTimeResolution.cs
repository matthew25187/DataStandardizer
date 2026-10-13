namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Identifies how a time on the local clock of a TZ Database zone is placed where it falls in a gap or an overlap.
    /// </summary>
    /// <remarks>
    /// A gap is a period that the local clock skips, as when clocks go forward. An overlap is a period that the local clock
    /// shows twice, as when clocks go back.
    /// </remarks>
    public enum TzDataLocalTimeResolution
    {
        /// <summary>
        /// A time in a gap or an overlap is rejected.
        /// </summary>
        Strict,

        /// <summary>
        /// A time is placed as RFC 5545, section 3.3.5, requires. A time in a gap takes the offset in effect before the gap,
        /// which moves it forward by the length of the gap. A time in an overlap takes its first occurrence, being the earlier instant.
        /// </summary>
        Rfc5545
    }
}
