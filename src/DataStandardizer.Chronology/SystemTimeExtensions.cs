using System;

namespace DataStandardizer.Chronology
{
    public static class SystemTimeExtensions
    {
        private const int SecondsPerHour = 60 * 60;
        private const int SecondsPerMinute = 60;

        /// <summary>
        /// Converts the specified <see cref="SystemTimeWithGregorianCalendar"/> instance 
        /// to its equivalent <see cref="UnixTime"/> representation, if possible.
        /// </summary>
        /// <param name="systemTime">
        /// The <see cref="SystemTimeWithGregorianCalendar"/> instance to convert.
        /// </param>
        /// <returns>
        /// A nullable <see cref="UnixTime"/> representing the equivalent Unix time, 
        /// or <c>null</c> if the conversion is not possible.
        /// </returns>
        public static UnixTime? AsUnixTime(this SystemTimeWithGregorianCalendar systemTime)
        {
            return systemTime._systemTime as UnixTime?;
        }

        /// <summary>
        /// Converts the specified system time to a <see cref="DateTime"/> representation.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the system time, which must be a value type implementing <see cref="ISystemTime"/>.
        /// </typeparam>
        /// <param name="systemTime">
        /// The system time instance to convert, providing a Julian Day Number.
        /// </param>
        /// <returns>
        /// A <see cref="DateTime"/> instance representing the date and time derived from the Julian Day Number.
        /// </returns>
        /// <remarks>
        /// This method calculates the date and time components based on the Julian Day Number provided by the 
        /// <paramref name="systemTime"/> and constructs a corresponding <see cref="DateTime"/> object.
        /// </remarks>
        public static DateTime ToDateTime<T>(this T systemTime) where T : struct, ISystemTime
        {
            var (year, month, day, hour, minute, second) = CalculateDateTimeFromJulianDayNumber(systemTime.JulianDayNumber);
            return new DateTime(year, month, day, hour, minute, second);
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Converts the specified system time to a <see cref="DateOnly"/> representation.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the system time, which must be a value type implementing <see cref="ISystemTime"/>.
        /// </typeparam>
        /// <param name="systemTime">
        /// The system time instance to convert, providing a Julian Day Number.
        /// </param>
        /// <returns>
        /// A <see cref="DateOnly"/> instance representing the date derived from the Julian Day Number.
        /// </returns>
        /// <remarks>
        /// This method calculates the date components based on the Julian Day Number provided by the 
        /// <paramref name="systemTime"/> and constructs a corresponding <see cref="DateOnly"/> object.
        /// </remarks>
        public static DateOnly ToDateOnly<T>(this T systemTime) where T : struct, ISystemTime
        {
            var (year, month, day, _, _, _) = CalculateDateTimeFromJulianDayNumber(systemTime.JulianDayNumber);
            return new DateOnly(year, month, day);
        }

        /// <summary>
        /// Converts the specified system time to a <see cref="TimeOnly"/> representation.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the system time, which must be a value type implementing <see cref="ISystemTime"/>.
        /// </typeparam>
        /// <param name="systemTime">
        /// The system time instance to convert, providing a Julian Day Number.
        /// </param>
        /// <returns>
        /// A <see cref="TimeOnly"/> instance representing the time derived from the Julian Day Number.
        /// </returns>
        /// <remarks>
        /// This method extracts the time components (hour, minute, and second) based on the Julian Day Number 
        /// provided by the <paramref name="systemTime"/> and constructs a corresponding <see cref="TimeOnly"/> object.
        /// </remarks>
        public static TimeOnly ToTimeOnly<T>(this T systemTime) where T : struct, ISystemTime
        {
            var (_, _, _, hour, minute, second) = CalculateDateTimeFromJulianDayNumber(systemTime.JulianDayNumber);
            return new TimeOnly(hour, minute, second);
        }
#endif

        private static (ushort Year, ushort Month, ushort Day, ushort Hour, ushort Minute, ushort Second) CalculateDateTimeFromJulianDayNumber(decimal jdn)
        {
            // Calculate the date portion.
            var jdnWithoutTime = Math.Truncate(jdn + .5m);
            int a = (int)(jdnWithoutTime + 32044);
            int b = (4 * a + 3) / 146097;
            int c = a - 146097 * b / 4;
            int d = (4 * c + 3) / 1461;
            int e = c - 1461 * d / 4;
            int m = (5 * e + 2) / 153;
            ushort day = (ushort)(e - (153 * m + 2) / 5 + 1);
            ushort month = (ushort)(m + 3 - 12 * (m / 10));
            ushort year = (ushort)(100 * b + d - 4800 + m / 10);

            // Calculate the time portion.
            var secondsRemaining = (jdn - .5m - Math.Truncate(jdn - .5m)) * 86400;
            ushort hour = (ushort)(secondsRemaining / SecondsPerHour);
            secondsRemaining -= hour * SecondsPerHour;
            ushort minute = (ushort)(secondsRemaining / SecondsPerMinute);
            secondsRemaining -= minute * SecondsPerMinute;
            ushort second = (ushort)secondsRemaining;

            // Return date & time.
            return (year, month, day, hour, minute, second);
        }
    }
}