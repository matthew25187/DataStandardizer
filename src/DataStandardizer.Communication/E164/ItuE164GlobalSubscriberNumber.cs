using System;
using System.Globalization;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents a global subscriber number as defined by the ITU-T E.164 standard.
    /// </summary>
    /// <remarks>
    /// The <see cref="ItuE164GlobalSubscriberNumber"/> struct encapsulates the global subscriber number,
    /// which uniquely identifies a subscriber within the context of international telecommunication.
    /// It provides functionality for parsing, validating, and converting global subscriber numbers
    /// to and from their string and numeric representations.
    /// </remarks>
    public readonly struct ItuE164GlobalSubscriberNumber : IItuE164Field
    {
        private static class ErrorMessage
        {
            internal const string InvalidFormatTemplate = "'{0}' is not in the correct format.";
        }

        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;

        private readonly string _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItuE164GlobalSubscriberNumber"/> struct from a numeric value.
        /// </summary>
        /// <param name="value">The global subscriber number. Its digits are the decimal digits of this value, with no leading zeros.</param>
        public ItuE164GlobalSubscriberNumber(ulong value)
        {
            _value = value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItuE164GlobalSubscriberNumber"/> struct from a string of digits.
        /// </summary>
        /// <param name="value">
        /// The global subscriber number: one or more decimal digits (<c>0</c> to <c>9</c>) whose value fits in a <see cref="ulong"/>,
        /// with no sign, white space or other characters. Leading zeros are kept, and count towards <see cref="DigitCount"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is <c>null</c>, empty, contains a character that is not a decimal digit, or is greater than
        /// <see cref="ulong.MaxValue"/>.
        /// </exception>
        public ItuE164GlobalSubscriberNumber(string value)
        {
            if (!ulong.TryParse(value, NumberStyles, CultureInfo.InvariantCulture, out _))
            {
                var message = string.Format(ErrorMessage.InvalidFormatTemplate, value);
                throw new ArgumentException(message, nameof(value));
            }

            _value = value;
        }

        /// <summary>
        /// Converts a string of digits to an <see cref="ItuE164GlobalSubscriberNumber"/>.
        /// </summary>
        /// <param name="value">
        /// The global subscriber number: one or more decimal digits (<c>0</c> to <c>9</c>) whose value fits in a <see cref="ulong"/>,
        /// with no sign, white space or other characters. Leading zeros are kept.
        /// </param>
        /// <returns>The global subscriber number that <paramref name="value"/> holds.</returns>
        /// <exception cref="InvalidCastException">
        /// <paramref name="value"/> is <c>null</c>, empty, contains a character that is not a decimal digit, or is greater than
        /// <see cref="ulong.MaxValue"/>.
        /// </exception>
        public static explicit operator ItuE164GlobalSubscriberNumber(string value)
        {
            if (!ulong.TryParse(value, NumberStyles, CultureInfo.InvariantCulture, out _))
            {
                var message = string.Format(ErrorMessage.InvalidFormatTemplate, value);
                throw new InvalidCastException(message);
            }

            return new ItuE164GlobalSubscriberNumber(value);
        }

        /// <summary>
        /// Converts a numeric value to an <see cref="ItuE164GlobalSubscriberNumber"/>.
        /// </summary>
        /// <param name="value">The global subscriber number. Its digits are the decimal digits of this value, with no leading zeros.</param>
        /// <returns>The global subscriber number that <paramref name="value"/> holds.</returns>
        public static implicit operator ItuE164GlobalSubscriberNumber(ulong value)
        {
            return new ItuE164GlobalSubscriberNumber(value);
        }

        /// <summary>
        /// Converts an <see cref="ItuE164GlobalSubscriberNumber"/> to its numeric value.
        /// </summary>
        /// <param name="value">The global subscriber number to convert.</param>
        /// <returns>
        /// The numeric value of <paramref name="value"/>, without any leading zeros its digits have, or 0 for the default instance.
        /// </returns>
        public static implicit operator ulong(ItuE164GlobalSubscriberNumber value)
        {
            return ulong.TryParse(value._value, NumberStyles, CultureInfo.InvariantCulture, out var candidateValue) ? candidateValue : 0;
        }

        /// <inheritdoc/>
        /// <remarks>Leading zeros count as digits. The default instance has no digits, so its count is 0.</remarks>
        public int DigitCount => _value?.Length ?? 0;

        /// <summary>
        /// Returns the digits of the global subscriber number.
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