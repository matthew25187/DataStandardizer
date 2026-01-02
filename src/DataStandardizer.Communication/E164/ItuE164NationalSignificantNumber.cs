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

        public ItuE164NationalSignificantNumber(ulong value)
        {
            _value = value.ToString(CultureInfo.InvariantCulture);
        }

        public ItuE164NationalSignificantNumber(string value)
        {
            if (!ulong.TryParse(value, NumberStyles, CultureInfo.InvariantCulture, out _))
            {
                var message = string.Format(ErrorMessage.InvalidFormatTemplate, value);
                throw new ArgumentException(message, nameof(value));
            }

            _value = value;
        }

        public static explicit operator ItuE164NationalSignificantNumber(string value)
        {
            if (!ulong.TryParse(value, NumberStyles, CultureInfo.InvariantCulture, out _))
            {
                var message = string.Format(ErrorMessage.InvalidFormatTemplate, value);
                throw new InvalidCastException(message);
            }

            return new ItuE164NationalSignificantNumber(value);
        }

        public static implicit operator ItuE164NationalSignificantNumber(ulong value)
        {
            return new ItuE164NationalSignificantNumber(value);
        }

        public static implicit operator ulong(ItuE164NationalSignificantNumber value)
        {
            return ulong.TryParse(value._value, NumberStyles, CultureInfo.InvariantCulture, out var candidateValue) ? candidateValue : 0;
        }

        public int DigitCount => _value?.Length ?? 0;

        public override string ToString()
        {
            return _value ?? string.Empty;
        }
    }
}