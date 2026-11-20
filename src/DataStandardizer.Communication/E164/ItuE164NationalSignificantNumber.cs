using System;
using System.Globalization;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents the national significant number (NSN) component of an ITU-T E.164 international telephone number.
    /// </summary>
    /// <remarks>
    /// The national significant number is a part of the E.164 numbering plan and is used to uniquely identify
    /// a subscriber within a specific country or geographic area. This struct provides functionality to handle
    /// and validate the NSN according to the E.164 standard.
    /// </remarks>
    public readonly struct ItuE164NationalSignificantNumber : IItuE164Field
    {
        private static class ErrorMessage
        {
            internal const string InvalidFormatTemplate = "'{0}' is not in the correct format.";
        }

        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;

        private readonly string _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItuE164NationalSignificantNumber"/> struct from a numeric value.
        /// </summary>
        /// <param name="value">The national significant number. Its digits are the decimal digits of this value, with no leading zeros.</param>
        public ItuE164NationalSignificantNumber(ulong value)
        {
            _value = value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItuE164NationalSignificantNumber"/> struct from a string of digits.
        /// </summary>
        /// <param name="value">
        /// The national significant number: one or more decimal digits (<c>0</c> to <c>9</c>) whose value fits in a <see cref="ulong"/>,
        /// with no sign, white space or other characters. Leading zeros are kept, and count towards <see cref="DigitCount"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is <c>null</c>, empty, contains a character that is not a decimal digit, or is greater than
        /// <see cref="ulong.MaxValue"/>.
        /// </exception>
        public ItuE164NationalSignificantNumber(string value)
        {
            if (!ulong.TryParse(value, NumberStyles, CultureInfo.InvariantCulture, out _))
            {
                var message = string.Format(ErrorMessage.InvalidFormatTemplate, value);
                throw new ArgumentException(message, nameof(value));
            }

            _value = value;
        }

        /// <summary>
        /// Converts a string of digits to an <see cref="ItuE164NationalSignificantNumber"/>.
        /// </summary>
        /// <param name="value">
        /// The national significant number: one or more decimal digits (<c>0</c> to <c>9</c>) whose value fits in a <see cref="ulong"/>,
        /// with no sign, white space or other characters. Leading zeros are kept.
        /// </param>
        /// <returns>The national significant number that <paramref name="value"/> holds.</returns>
        /// <exception cref="InvalidCastException">
        /// <paramref name="value"/> is <c>null</c>, empty, contains a character that is not a decimal digit, or is greater than
        /// <see cref="ulong.MaxValue"/>.
        /// </exception>
        public static explicit operator ItuE164NationalSignificantNumber(string value)
        {
            if (!ulong.TryParse(value, NumberStyles, CultureInfo.InvariantCulture, out _))
            {
                var message = string.Format(ErrorMessage.InvalidFormatTemplate, value);
                throw new InvalidCastException(message);
            }

            return new ItuE164NationalSignificantNumber(value);
        }

        /// <summary>
        /// Converts a numeric value to an <see cref="ItuE164NationalSignificantNumber"/>.
        /// </summary>
        /// <param name="value">The national significant number. Its digits are the decimal digits of this value, with no leading zeros.</param>
        /// <returns>The national significant number that <paramref name="value"/> holds.</returns>
        public static implicit operator ItuE164NationalSignificantNumber(ulong value)
        {
            return new ItuE164NationalSignificantNumber(value);
        }

        /// <summary>
        /// Converts an <see cref="ItuE164NationalSignificantNumber"/> to its numeric value.
        /// </summary>
        /// <param name="value">The national significant number to convert.</param>
        /// <returns>
        /// The numeric value of <paramref name="value"/>, without any leading zeros its digits have, or 0 for the default instance.
        /// </returns>
        public static implicit operator ulong(ItuE164NationalSignificantNumber value)
        {
            return ulong.TryParse(value._value, NumberStyles, CultureInfo.InvariantCulture, out var candidateValue) ? candidateValue : 0;
        }

        /// <inheritdoc/>
        /// <remarks>Leading zeros count as digits. The default instance has no digits, so its count is 0.</remarks>
        public int DigitCount => _value?.Length ?? 0;

        /// <summary>
        /// Returns the digits of the national significant number.
        /// </summary>
        /// <returns>
        /// The digits of the number, including any leading zeros it was created with, or an empty string for the default instance.
        /// </returns>
        public override string ToString()
        {
            return _value ?? string.Empty;
        }
    }
}