using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The value of a property that may be either a DATE or a DATE-TIME (RFC 5545 §3.3.4 and §3.3.5), such as <c>DTSTART</c>, <c>DTEND</c> or
    /// <c>RECURRENCE-ID</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Which value type a property holds is set by its <c>VALUE</c> parameter, and a zoned date-time takes its time zone from the <c>TZID</c>
    /// parameter.  The typed accessors of the components read and write both parameters.
    /// </para>
    /// <para>
    /// The default value is the DATE 0001-01-01.
    /// </para>
    /// </remarks>
    public readonly struct IcalDateOrDateTime : IEquatable<IcalDateOrDateTime>
    {
        private readonly IcalDateTime _dateTime;
        private readonly bool _isDateTime;

        /// <summary>
        /// Creates a DATE value.
        /// </summary>
        /// <param name="date">The date.</param>
        public IcalDateOrDateTime(IcalDate date)
        {
            _dateTime = new IcalDateTime(date, default(IcalTime));
            _isDateTime = false;
        }

        /// <summary>
        /// Creates a DATE-TIME value.
        /// </summary>
        /// <param name="dateTime">The date-time.</param>
        public IcalDateOrDateTime(IcalDateTime dateTime)
        {
            _dateTime = dateTime;
            _isDateTime = true;
        }

        /// <summary>
        /// Gets the date: the value itself if it is a DATE, or the date part of a DATE-TIME.
        /// </summary>
        public IcalDate Date => _dateTime.Date;

        /// <summary>
        /// Gets the date-time.
        /// </summary>
        /// <exception cref="InvalidOperationException">The value is a DATE.</exception>
        public IcalDateTime DateTime
        {
            get
            {
                if (!_isDateTime)
                    throw new InvalidOperationException("The value is a DATE, not a DATE-TIME.");

                return _dateTime;
            }
        }

        /// <summary>
        /// Gets a flag indicating if the value is a DATE, with no time of day.
        /// </summary>
        public bool IsDate => !_isDateTime;

        /// <summary>
        /// Gets the value type: <see cref="IcalValueType.Date"/> or <see cref="IcalValueType.DateTime"/>.
        /// </summary>
        public IcalValueType ValueType => _isDateTime ? IcalValueType.DateTime : IcalValueType.Date;

        /// <summary>
        /// Converts a date to a DATE value.
        /// </summary>
        /// <param name="date">The date.</param>
        public static implicit operator IcalDateOrDateTime(IcalDate date)
        {
            return new IcalDateOrDateTime(date);
        }

        /// <summary>
        /// Converts a date-time to a DATE-TIME value.
        /// </summary>
        /// <param name="dateTime">The date-time.</param>
        public static implicit operator IcalDateOrDateTime(IcalDateTime dateTime)
        {
            return new IcalDateOrDateTime(dateTime);
        }

        /// <summary>
        /// Gets the value in <see cref="IcalValueFormat.Text"/> form, without the time zone of a zoned date-time.
        /// </summary>
        /// <returns>The value, such as <c>19970714</c> or <c>19970714T133000Z</c>.</returns>
        public override string ToString()
        {
            return _isDateTime ? _dateTime.ToString(IcalValueFormat.Text) : _dateTime.Date.ToString(IcalValueFormat.Text);
        }

        /// <summary>
        /// Indicates whether this value equals another: both are DATE values for the same date, or both are equal DATE-TIME values.
        /// </summary>
        /// <param name="other">The other value.</param>
        /// <returns><c>true</c> if the values are equal; otherwise <c>false</c>.</returns>
        public bool Equals(IcalDateOrDateTime other)
        {
            return _isDateTime == other._isDateTime && _dateTime.Equals(other._dateTime);
        }

        /// <inheritdoc />
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is IcalDateOrDateTime other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _dateTime.GetHashCode() * 2 + (_isDateTime ? 1 : 0);
        }

        /// <summary>
        /// Indicates whether two values are equal.
        /// </summary>
        public static bool operator ==(IcalDateOrDateTime left, IcalDateOrDateTime right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two values differ.
        /// </summary>
        public static bool operator !=(IcalDateOrDateTime left, IcalDateOrDateTime right)
        {
            return !left.Equals(right);
        }
    }
}
