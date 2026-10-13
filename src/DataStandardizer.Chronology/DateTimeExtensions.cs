using System;

namespace DataStandardizer.Chronology
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Converts the specified <see cref="DateTime"/> to a <see cref="DosDateTime"/> representation.
        /// </summary>
        /// <param name="dateTime">
        /// The <see cref="DateTime"/> instance to convert. The time is assumed to be in local time.
        /// </param>
        /// <returns>
        /// A <see cref="DosDateTime"/> instance representing the date and time in the DOS-compatible format.
        /// </returns>
        /// <remarks>
        /// This method extracts the year, month, day, hour, minute, and second components from the 
        /// provided <see cref="DateTime"/> and constructs a <see cref="DosDateTime"/> instance.
        /// </remarks>
        public static DosDateTime ToDosDateTime(this DateTime dateTime)
        {
            return new DosDateTime((ushort)dateTime.Year, (ushort)dateTime.Month, (ushort)dateTime.Day, (ushort)dateTime.Hour, (ushort)dateTime.Minute, (ushort)dateTime.Second);
        }

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

        /// <summary>
        /// Places a time on the local clock of a TZ Database zone, giving the instant at which the clock shows it.
        /// </summary>
        /// <param name="local">
        /// The time on the local clock of <paramref name="timezone"/>. It must be of kind <see cref="DateTimeKind.Unspecified"/>;
        /// a time of kind <see cref="DateTimeKind.Utc"/> or <see cref="DateTimeKind.Local"/> is rejected.
        /// </param>
        /// <param name="timezone">The timezone whose local clock shows the time.</param>
        /// <param name="resolution">How a time that falls in a gap or an overlap is placed.</param>
        /// <returns>
        /// The time, with the offset from universal time in effect in the timezone at the instant. Its
        /// <see cref="DateTimeOffset.UtcDateTime"/> gives the instant.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Unlike <see cref="DateTime.ToUniversalTime"/>, which uses the time zone of the machine, this method uses the rules of
        /// <paramref name="timezone"/>, and requires a time of kind <see cref="DateTimeKind.Unspecified"/>.
        /// </para>
        /// <para>
        /// A time normally has one instant, and the result shows it unchanged. A time in a gap, as when clocks go forward, has none.
        /// <see cref="TzDataLocalTimeResolution.Rfc5545"/> takes the offset in effect before the gap, so that the result shows the
        /// time moved forward by the length of the gap. A time in an overlap, as when clocks go back, has two.
        /// <see cref="TzDataLocalTimeResolution.Rfc5545"/> takes the earlier.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="local"/> is of kind <see cref="DateTimeKind.Utc"/> or <see cref="DateTimeKind.Local"/>; or
        /// <paramref name="resolution"/> is <see cref="TzDataLocalTimeResolution.Strict"/> and <paramref name="local"/> falls in a gap or an overlap.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="resolution"/> is not a defined value; or the offset in effect is not a whole number of minutes of at most
        /// 14 hours, as for some offsets of local mean time, and so cannot be held by a <see cref="DateTimeOffset"/>; or the result
        /// falls outside the range of <see cref="DateTime"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">The timezone is the default value, or its identifier is not that of a known timezone.</exception>
        public static DateTimeOffset ToDateTimeOffset(this DateTime local, TzDataTimezone timezone, TzDataLocalTimeResolution resolution)
        {
            var calculator = timezone.GetCalculator();
            var localTicks = TzDataExtensions.ToLocalTicks(local, nameof(local));

            if (resolution != TzDataLocalTimeResolution.Strict && resolution != TzDataLocalTimeResolution.Rfc5545)
                throw new ArgumentOutOfRangeException(nameof(resolution), resolution, "The resolution is not a defined value.");

            var offsetInfos = calculator.GetOffsetInfosForLocal(localTicks, out var offsetBeforeGap);

            if (offsetInfos.Count == 1 || (offsetInfos.Count > 1 && resolution == TzDataLocalTimeResolution.Rfc5545))
            {
                var utcOffset = offsetInfos[0].UtcOffset;
                return TzDataExtensions.ToDateTimeOffset(new DateTime(localTicks - utcOffset.Ticks, DateTimeKind.Utc), utcOffset, nameof(local));
            }

            if (offsetInfos.Count > 1)
                throw new ArgumentException("The time falls in an overlap, where the local clock of the timezone shows it more than once.", nameof(local));

            if (resolution == TzDataLocalTimeResolution.Strict || !offsetBeforeGap.HasValue)
                throw new ArgumentException("The time falls in a gap, which the local clock of the timezone skips.", nameof(local));

            var gapTicks = localTicks - offsetBeforeGap.Value.UtcOffset.Ticks;
            if (gapTicks < DateTime.MinValue.Ticks || gapTicks > DateTime.MaxValue.Ticks)
                throw new ArgumentOutOfRangeException(nameof(local), "The instant falls outside the range of DateTime.");

            var utc = new DateTime(gapTicks, DateTimeKind.Utc);
            return TzDataExtensions.ToDateTimeOffset(utc, calculator.GetOffsetInfo(utc).UtcOffset, nameof(local));
        }
    }
}