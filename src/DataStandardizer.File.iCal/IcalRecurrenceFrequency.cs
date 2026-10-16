namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The FREQ rule part of a recurrence rule (RFC 5545 §3.3.10): the interval at which a rule repeats.
    /// </summary>
    public enum IcalRecurrenceFrequency
    {
        /// <summary>
        /// Repeats every second or more (<c>SECONDLY</c>).
        /// </summary>
        Secondly = 1,

        /// <summary>
        /// Repeats every minute or more (<c>MINUTELY</c>).
        /// </summary>
        Minutely = 2,

        /// <summary>
        /// Repeats every hour or more (<c>HOURLY</c>).
        /// </summary>
        Hourly = 3,

        /// <summary>
        /// Repeats every day or more (<c>DAILY</c>).
        /// </summary>
        Daily = 4,

        /// <summary>
        /// Repeats every week or more (<c>WEEKLY</c>).
        /// </summary>
        Weekly = 5,

        /// <summary>
        /// Repeats every month or more (<c>MONTHLY</c>).
        /// </summary>
        Monthly = 6,

        /// <summary>
        /// Repeats every year or more (<c>YEARLY</c>).
        /// </summary>
        Yearly = 7
    }
}
