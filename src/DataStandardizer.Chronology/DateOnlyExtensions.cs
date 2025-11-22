using System;

namespace DataStandardizer.Chronology
{
#if NET6_0_OR_GREATER
    public static class DateOnlyExtensions
    {
        /// <summary>
        /// Converts the specified <see cref="DateOnly"/> instance to a <see cref="UnixTime"/> representation.
        /// </summary>
        /// <param name="date">The <see cref="DateOnly"/> instance to convert.</param>
        /// <returns>
        /// A <see cref="UnixTime"/> value representing the number of seconds elapsed since the Unix epoch 
        /// (January 1, 1970, 00:00:00 UTC) for the specified date.
        /// </returns>
        /// <remarks>
        /// This method calculates the Julian Day Number (JDN) for the given date and converts it to Unix time 
        /// by subtracting the JDN of the Unix epoch and multiplying by the number of seconds in a day.
        /// </remarks>
        public static UnixTime ToUnixTime(this DateOnly date)
        {
            var jdn = CalculateJulianDayNumberFromDate(date);
            var unixTime = (long)((jdn - 2440587.5m) * 86400);
            return new UnixTime(unixTime);
        }

        private static decimal CalculateJulianDayNumberFromDate(DateOnly date)
        {
            var a = (14 - date.Month) / 12;
            var y = date.Year + 4800 - a;
            var m = date.Month + 12 * a - 3;
            var jdn = date.Day + (153 * m + 2) / 5 + 365 * y + y / 4 - y / 100 + y / 400 - 32045;
            return jdn - .5m;   // subtract 0.5 to "zero" on midnight to account for JDNs starting the day at noon
        }
    }
#endif
}