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
        private const int DayMaximum = 31;
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

        public DosDateTime(uint value)
        {
            _value = value;
        }

        public DosDateTime(ushort year, ushort month, ushort day)
        {
            if (year < YearMinimum || year > YearMaximum)
                throw new ArgumentOutOfRangeException(nameof(year), year, $"Year must be in the range {YearMinimum} - {YearMaximum}.");

            if (month < MonthMinimum || month > MonthMaximum)
                throw new ArgumentOutOfRangeException(nameof(month), month, $"Month must be in the range {MonthMinimum} - {MonthMaximum}.");

            if (day < DayMinimum || day > DayMaximum)
                throw new ArgumentOutOfRangeException(nameof(day), day, $"Day must be in the range {DayMinimum} - {DayMaximum}.");

            var date = ((year - YearMinimum) << 9) | (month << 5) | day;
            _value = (uint)(date << 16);
        }

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

        public static implicit operator DosDateTime(uint value)
        {
            return new DosDateTime(value);
        }

        public static implicit operator uint(DosDateTime value)
        {
            return value._value;
        }

        public decimal JulianDayNumber => DoGetJulianDayNumber();

        private decimal DoGetJulianDayNumber()
        {
            ushort date = (ushort)(_value >> 16);
            ushort dateYear = (ushort)(((date & (ushort)DateMask.Year) >> 9) + YearMinimum);
            ushort dateMonth = (ushort)((date & (ushort)DateMask.Month) >> 5);
            ushort dateDay = (ushort)(date & (ushort)DateMask.Day);

            ushort time = (ushort)(_value & 0xffff);
            ushort timeHour = (ushort)((time & (ushort)TimeMask.Hour) >> 11);
            ushort timeMinute = (ushort)((time & (ushort)TimeMask.Minute) >> 5);
            ushort timeSecond = (ushort)((time & (ushort)TimeMask.Second) * 2);

            var jdn = JulianDayNumberHelper.ConvertGregorianCalendarDateToJdn(dateYear, dateMonth, dateDay) +
                      JulianDayNumberHelper.ConvertTimeOfDayToJdn(timeHour, timeMinute, timeSecond);
            return jdn;
        }
    }
}