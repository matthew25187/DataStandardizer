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
        /// or its second (twice its seconds field) is greater than 59.
        /// </exception>
        public DosDateTime(uint value)
        {
            if (!IsValidPackedValue(value, out var message))
                throw new ArgumentOutOfRangeException(nameof(value), value, message);

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
            : this(year, month, day, HourMinimum, MinuteMinimum, SecondMinimum)
        {
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
        {
            if (!AreValidComponents(year, month, day, hour, minute, second, out var parameterName, out var actualValue, out var message))
                throw new ArgumentOutOfRangeException(parameterName, actualValue, message);

            var date = ((year - YearMinimum) << 9) | (month << 5) | day;
            var time = (hour << 11) | (minute << 5) | (second / 2);
            _value = ((uint)date << 16) | (uint)time;
        }

        // Wraps a packed value that the caller has already validated.
        private DosDateTime(uint validatedValue, bool _)
        {
            _value = validatedValue;
        }

        /// <summary>
        /// Converts a packed 32-bit value to a <see cref="DosDateTime"/>.
        /// </summary>
        /// <param name="value">The packed DOS date and time.</param>
        /// <returns>The <see cref="DosDateTime"/> that <paramref name="value"/> encodes.</returns>
        /// <exception cref="InvalidCastException"><paramref name="value"/> is not a valid DOS date and time.</exception>
        public static explicit operator DosDateTime(uint value)
        {
            if (!IsValidPackedValue(value, out var message))
                throw new InvalidCastException(message);

            return new DosDateTime(value, true);
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

        private static bool IsValidPackedValue(uint value, out string message)
        {
            // The year field is 7 bits, so every value of it is a year from 1980 to 2107.
            var (year, month, day, hour, minute, second) = Unpack(value);

            if (AreValidComponents(year, month, day, hour, minute, second, out var parameterName, out var actualValue, out var componentMessage))
            {
                message = string.Empty;
                return true;
            }

            message = $"The packed value has {parameterName} {actualValue}. {componentMessage}";
            return false;
        }

        // The single source of the DOS date and time range rules. The parameter names are those of the component constructor.
        private static bool AreValidComponents(int year, int month, int day, int hour, int minute, int second, out string parameterName, out int actualValue, out string message)
        {
            if (year < YearMinimum || year > YearMaximum)
                return Invalid(nameof(year), year, $"Year must be in the range {YearMinimum} - {YearMaximum}.", out parameterName, out actualValue, out message);

            if (month < MonthMinimum || month > MonthMaximum)
                return Invalid(nameof(month), month, $"Month must be in the range {MonthMinimum} - {MonthMaximum}.", out parameterName, out actualValue, out message);

            var monthLength = JulianDayNumberHelper.GetGregorianCalendarMonthLength(year, month);
            if (day < DayMinimum || day > monthLength)
                return Invalid(nameof(day), day, $"Day must be in the range {DayMinimum} - {monthLength}.", out parameterName, out actualValue, out message);

            if (hour < HourMinimum || hour > HourMaximum)
                return Invalid(nameof(hour), hour, $"Hour must be in the range {HourMinimum} - {HourMaximum}.", out parameterName, out actualValue, out message);

            if (minute < MinuteMinimum || minute > MinuteMaximum)
                return Invalid(nameof(minute), minute, $"Minute must be in the range {MinuteMinimum} - {MinuteMaximum}.", out parameterName, out actualValue, out message);

            if (second < SecondMinimum || second > SecondMaximum)
                return Invalid(nameof(second), second, $"Second must be in the range {SecondMinimum} - {SecondMaximum}.", out parameterName, out actualValue, out message);

            parameterName = string.Empty;
            actualValue = 0;
            message = string.Empty;
            return true;
        }

        private static bool Invalid(string name, int value, string text, out string parameterName, out int actualValue, out string message)
        {
            parameterName = name;
            actualValue = value;
            message = text;
            return false;
        }
    }
}