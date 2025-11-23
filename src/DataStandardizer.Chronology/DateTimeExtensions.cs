using System;

namespace DataStandardizer.Chronology
{
    public static class DateTimeExtensions
    {
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
            var jdn = JulianDayNumberHelper.ConvertGregorianCalendarDateToJdn(dateTime.Year, dateTime.Month, dateTime.Day) + 
                      JulianDayNumberHelper.ConvertTimeOfDayToJdn(dateTime.Hour, dateTime.Minute, dateTime.Second);
            var unixTime = (long)((jdn - 2440587.5m) * 86400);
            return new UnixTime(unixTime);
        }
    }
}