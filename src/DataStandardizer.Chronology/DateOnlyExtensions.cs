using System;

namespace DataStandardizer.Chronology
{
#if NET6_0_OR_GREATER
    public static class DateOnlyExtensions
    {
        /// <summary>
        /// Converts the specified <see cref="DateOnly"/> instance to a <see cref="DosDateTime"/> representation.
        /// </summary>
        /// <param name="date">The <see cref="DateOnly"/> instance to convert.</param>
        /// <returns>
        /// A <see cref="DosDateTime"/> value representing the date in the DOS date format.
        /// </returns>
        /// <remarks>
        /// This method creates a <see cref="DosDateTime"/> instance using the year, month, and day components 
        /// of the specified <see cref="DateOnly"/> instance. The resulting value is compatible with the DOS 
        /// file system's date representation.
        /// </remarks>
        public static DosDateTime ToDosDateTime(this DateOnly date)
        {
            return new DosDateTime((ushort)date.Year, (ushort)date.Month, (ushort)date.Day);
        }

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
            var jdn = JulianDayNumberHelper.ConvertGregorianCalendarDateToJdn(date.Year, date.Month, date.Day) +
                      JulianDayNumberHelper.ConvertTimeOfDayToJdn(0, 0, 0);
            var unixTime = (long)((jdn - 2440587.5m) * 86400);
            return new UnixTime(unixTime);
        }
    }
#endif
}