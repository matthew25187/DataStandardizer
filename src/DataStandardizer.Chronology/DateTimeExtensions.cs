using System;

namespace DataStandardizer.Chronology
{
    public static class DateTimeExtensions
    {
        private const int SecondsPerHour = 60 * 60;
        private const int SecondsPerMinute = 60;

        /// <summary>
        /// Converts the specified <see cref="DateTime"/> to a <see cref="UnixTime"/> representation.
        /// </summary>
        /// <param name="dateTime">
        /// The <see cref="DateTime"/> instance to convert. The time is assumed to be in UTC.
        /// </param>
        /// <returns>
        /// A <see cref="UnixTime"/> instance representing the number of seconds elapsed since 
        /// the Unix epoch (January 1, 1970, 00:00:00 UTC).
        /// </returns>
        /// <remarks>
        /// This method calculates the Julian Day Number for the given <see cref="DateTime"/> 
        /// and converts it to the Unix time format.
        /// </remarks>
        public static UnixTime ToUnixTime(this DateTime dateTime)
        {
            var jdn = CalculateJulianDayNumberFromDateTime(dateTime);
            var unixTime = (long)((jdn - 2440587.5m) * 86400);
            return new UnixTime(unixTime);
        }

        private static decimal CalculateJulianDayNumberFromDateTime(DateTime dateTime)
        {
            // Calculate date portion.
            var a = (14 - dateTime.Month) / 12;
            var y = dateTime.Year + 4800 - a;
            var m = dateTime.Month + 12 * a - 3;
            var jdn = dateTime.Day + (153 * m + 2) / 5 + 365 * y + y / 4 - y / 100 + y / 400 - 32045;

            // Calculate time portion.
            var seconds = dateTime.Hour * SecondsPerHour + dateTime.Minute * SecondsPerMinute + dateTime.Second;
            var fractionOfDay = (decimal)seconds / 86400;

            // Combine result.
            return jdn + fractionOfDay - .5m;
        }
    }
}