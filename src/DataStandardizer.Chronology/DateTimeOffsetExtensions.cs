using System;

namespace DataStandardizer.Chronology
{
    public static class DateTimeOffsetExtensions
    {
        /// <summary>
        /// Converts an instant to the time shown on the local clock of a TZ Database zone at that instant.
        /// </summary>
        /// <param name="instant">The instant to convert.</param>
        /// <param name="timezone">The timezone whose local clock shows the result.</param>
        /// <returns>
        /// The same instant, with the offset from universal time in effect in the timezone at that instant.
        /// </returns>
        /// <remarks>
        /// This is the same as <see cref="DateTimeOffset.ToOffset(TimeSpan)"/>, with the offset taken from the timezone as
        /// <see cref="TzDataExtensions.GetUtcOffset(TzDataTimezone, DateTimeOffset)"/> gives it.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The offset in effect is not a whole number of minutes of at most 14 hours, as for some offsets of local mean time,
        /// and so cannot be held by a <see cref="DateTimeOffset"/>; or the result falls outside the range of <see cref="DateTime"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
        public static DateTimeOffset ToOffset(this DateTimeOffset instant, TzDataTimezone timezone)
        {
            var utc = instant.UtcDateTime;
            return TzDataExtensions.ToDateTimeOffset(utc, timezone.GetUtcOffset(utc), nameof(instant));
        }
    }
}
