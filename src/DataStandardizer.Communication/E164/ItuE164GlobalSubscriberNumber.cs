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

        public ItuE164GlobalSubscriberNumber(ulong value)
        {
            _value = value.ToString(CultureInfo.InvariantCulture);
        }

        public ItuE164GlobalSubscriberNumber(string value)
        {
            if (!ulong.TryParse(value, NumberStyles, CultureInfo.InvariantCulture, out _))
            {
                var message = string.Format(ErrorMessage.InvalidFormatTemplate, value);
                throw new ArgumentException(message, nameof(value));
            }

            _value = value;
        }

        public static explicit operator ItuE164GlobalSubscriberNumber(string value)
        {
            if (!ulong.TryParse(value, NumberStyles, CultureInfo.InvariantCulture, out _))
            {
                var message = string.Format(ErrorMessage.InvalidFormatTemplate, value);
                throw new InvalidCastException(message);
            }

            return new ItuE164GlobalSubscriberNumber(value);
        }

        public static implicit operator ItuE164GlobalSubscriberNumber(ulong value)
        {
            return new ItuE164GlobalSubscriberNumber(value);
        }

        public static implicit operator ulong(ItuE164GlobalSubscriberNumber value)
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