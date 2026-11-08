using System;

namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Represents a DOS date and time structure, encapsulating date and time information 
    /// in a compact format compatible with the DOS file system.
    /// </summary>
    /// <remarks>
    /// The <see cref="DosDateTime"/> struct provides functionality to store and manipulate 
    /// date and time values within the constraints of the DOS format. It supports conversion 
    /// to and from a 32-bit unsigned integer representation and provides access to the Julian 
    /// Day Number for compatibility with astronomical and calendar calculations.
    /// </remarks>
    public readonly struct DosDateTime : ISystemTime
    {
        private const int DayMinimum = 1;
        private const int MonthMinimum = 1;
        private const int MonthMaximum = 12;
        private const int YearMinimum = 1980;
        private const int YearMaximum = 2107;
        private const int HourMinimum = 0;
        private const int HourMaximum = 23;
        private const int MinuteMinimum = 0;
        private const int MinuteMaximum = 59;
        private const int SecondMinimum = 0;
        private const int SecondMaximum = 59;

        private enum DateMask : ushort
        {
            Day = 0x1f,
            Month = 0x1e0,
            Year = 0xfe00
        }

        private enum TimeMask : ushort
        {
            Second = 0x1f,
            Minute = 0x7e0,
            Hour = 0xf800
        }

        private readonly uint _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="DosDateTime"/> struct from a packed 32-bit value,
        /// with the DOS date in the high 16 bits and the DOS time in the low 16 bits.
        /// </summary>
        /// <param name="value">The packed DOS date and time.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="value"/> is not a valid DOS date and time: its month is not 1 to 12, its day is
        /// not within the length of that month, its hour is greater than 23, its minute is greater than 59,
        /// or its seconds field (seconds divided by 2) is greater than 29.
        /// </exception>
        public DosDateTime(uint value)
        {
            var errorMessage = GetInvalidValueMessage(value);
            if (errorMessage != null)
                throw new ArgumentOutOfRangeException(nameof(value), value, errorMessage);

            _value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DosDateTime"/> struct from a date, at midnight.
        /// </summary>
        /// <param name="year">The year, from 1980 to 2107.</param>
        /// <param name="month">The month, from 1 to 12.</param>
        /// <param name="day">The day, from 1 to the number of days in <paramref name="month"/> of <paramref name="year"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">A component is out of range.</exception>
        public DosDateTime(ushort year, ushort month, ushort day)
        {
            if (year < YearMinimum || year > YearMaximum)
                throw new ArgumentOutOfRangeException(nameof(year), year, $"Year must be in the range {YearMinimum} - {YearMaximum}.");

            if (month < MonthMinimum || month > MonthMaximum)
                throw new ArgumentOutOfRangeException(nameof(month), month, $"Month must be in the range {MonthMinimum} - {MonthMaximum}.");

            var monthLength = JulianDayNumberHelper.GetGregorianCalendarMonthLength(year, month);
            if (day < DayMinimum || day > monthLength)
                throw new ArgumentOutOfRangeException(nameof(day), day, $"Day must be in the range {DayMinimum} - {monthLength}.");

            var date = ((year - YearMinimum) << 9) | (month << 5) | day;
            _value = (uint)(date << 16);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DosDateTime"/> struct from a date and a time of day.
        /// </summary>
        /// <param name="year">The year, from 1980 to 2107.</param>
        /// <param name="month">The month, from 1 to 12.</param>
        /// <param name="day">The day, from 1 to the number of days in <paramref name="month"/> of <paramref name="year"/>.</param>
        /// <param name="hour">The hour, from 0 to 23.</param>
        /// <param name="minute">The minute, from 0 to 59.</param>
        /// <param name="second">The second, from 0 to 59. It is stored at two-second resolution, so an odd value is rounded down.</param>
        /// <exception cref="ArgumentOutOfRangeException">A component is out of range.</exception>
        public DosDateTime(ushort year, ushort month, ushort day, ushort hour, ushort minute, ushort second)
            : this(year, month, day)
        {
            if (hour < HourMinimum || hour > HourMaximum)
                throw new ArgumentOutOfRangeException(nameof(hour), hour, $"Hour must be in the range {HourMinimum} - {HourMaximum}.");

            if (minute < MinuteMinimum || minute > MinuteMaximum)
                throw new ArgumentOutOfRangeException(nameof(minute), minute, $"Minute must be in the range {MinuteMinimum} - {MinuteMaximum}.");

            if (second < SecondMinimum || second > SecondMaximum)
                throw new ArgumentOutOfRangeException(nameof(second), second, $"Second must be in the range {SecondMinimum} - {SecondMaximum}.");

            var time = (hour << 11) | (minute << 5) | (second / 2);
            _value |= (uint)time;
        }

        /// <summary>
        /// Converts a packed 32-bit value to a <see cref="DosDateTime"/>.
        /// </summary>
        /// <param name="value">The packed DOS date and time.</param>
        /// <returns>The <see cref="DosDateTime"/> that <paramref name="value"/> encodes.</returns>
        /// <exception cref="InvalidCastException"><paramref name="value"/> is not a valid DOS date and time.</exception>
        public static explicit operator DosDateTime(uint value)
        {
            var errorMessage = GetInvalidValueMessage(value);
            if (errorMessage != null)
                throw new InvalidCastException(errorMessage);

            return new DosDateTime(value);
        }

        /// <summary>
        /// Converts a <see cref="DosDateTime"/> to its packed 32-bit value.
        /// </summary>
        /// <param name="value">The DOS date and time.</param>
        /// <returns>The packed value, or 0 for a default instance.</returns>
        public static implicit operator uint(DosDateTime value)
        {
            return value._value;
        }

        /// <inheritdoc/>
        /// <exception cref="InvalidOperationException">The instance is a default instance, which holds no date.</exception>
        public decimal JulianDayNumber => DoGetJulianDayNumber();

        private decimal DoGetJulianDayNumber()
        {
            // A default instance holds 0, which has month 0 and day 0, so no constructor can produce it.
            if (_value == 0)
                throw new InvalidOperationException($"The {nameof(DosDateTime)} instance is not initialised.");

            var (year, month, day, hour, minute, second) = Unpack(_value);

            var jdn = JulianDayNumberHelper.ConvertGregorianCalendarDateToJdn(year, month, day) +
                      JulianDayNumberHelper.ConvertTimeOfDayToJdn(hour, minute, second);
            return jdn;
        }

        private static (int Year, int Month, int Day, int Hour, int Minute, int Second) Unpack(uint value)
        {
            ushort date = (ushort)(value >> 16);
            int year = ((date & (ushort)DateMask.Year) >> 9) + YearMinimum;
            int month = (date & (ushort)DateMask.Month) >> 5;
            int day = date & (ushort)DateMask.Day;

            ushort time = (ushort)(value & 0xffff);
            int hour = (time & (ushort)TimeMask.Hour) >> 11;
            int minute = (time & (ushort)TimeMask.Minute) >> 5;
            int second = (time & (ushort)TimeMask.Second) * 2;

            return (year, month, day, hour, minute, second);
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string? GetInvalidValueMessage(uint value)
#else
        private static string GetInvalidValueMessage(uint value)
#endif
        {
            // The year field is 7 bits, so every value of it is a year from 1980 to 2107.
            var (year, month, day, hour, minute, second) = Unpack(value);

            if (month < MonthMinimum || month > MonthMaximum)
                return $"The month must be in the range {MonthMinimum} - {MonthMaximum}. Actual value was {month}.";

            var monthLength = JulianDayNumberHelper.GetGregorianCalendarMonthLength(year, month);
            if (day < DayMinimum || day > monthLength)
                return $"The day must be in the range {DayMinimum} - {monthLength}. Actual value was {day}.";

            if (hour > HourMaximum)
                return $"The hour must be in the range {HourMinimum} - {HourMaximum}. Actual value was {hour}.";

            if (minute > MinuteMaximum)
                return $"The minute must be in the range {MinuteMinimum} - {MinuteMaximum}. Actual value was {minute}.";

            if (second > SecondMaximum)
                return $"The second must be in the range {SecondMinimum} - {SecondMaximum}. Actual value was {second}.";

            return null;
        }
    }
}