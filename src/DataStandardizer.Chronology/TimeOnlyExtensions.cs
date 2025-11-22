using System;

namespace DataStandardizer.Chronology
{
#if NET6_0_OR_GREATER
    public static class TimeOnlyExtensions
    {
        private const int SecondsPerHour = 60 * 60;
        private const int SecondsPerMinute = 60;

        /// <summary>
        /// Converts the specified <see cref="TimeOnly"/> instance to a <see cref="UnixTime"/> representation.
        /// </summary>
        /// <param name="time">The <see cref="TimeOnly"/> instance to convert.</param>
        /// <returns>
        /// A <see cref="UnixTime"/> value representing the number of seconds elapsed since midnight (00:00:00) 
        /// of the same day as the provided <see cref="TimeOnly"/> instance.
        /// </returns>
        /// <remarks>
        /// This method calculates the Unix time by summing the total seconds contributed by the hours, 
        /// minutes, and seconds of the <paramref name="time"/> parameter.
        /// </remarks>
        public static UnixTime ToUnixTime(this TimeOnly time)
        {
            var unixTime = time.Hour * SecondsPerHour + time.Minute * SecondsPerMinute + time.Second;
            return new UnixTime(unixTime);
        }
    } 
#endif
}