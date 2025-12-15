using System;
using System.Globalization;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents a subscriber number component of an ITU-T E.164 international telephone number.
    /// </summary>
    /// <remarks>
    /// The <see cref="ItuE164SubscriberNumber"/> struct provides functionality for handling
    /// subscriber numbers as part of the ITU-T E.164 standard for international telephone numbers.
    /// It supports initialization from both numeric and string representations, ensuring that the
    /// provided value adheres to the required format.
    /// </remarks>
    public readonly struct ItuE164SubscriberNumber : IItuE164Field
    {
        private static class ErrorMessage
        {
            internal const string InvalidFormatTemplate = "'{0}' is not in the correct format.";
        }

        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;

        private readonly string _value;

        public ItuE164SubscriberNumber(ulong value)
        {
            _value = value.ToString(CultureInfo.InvariantCulture);
        }

        public ItuE164SubscriberNumber(string value)
        {
            if (!ulong.TryParse(value, NumberStyles, CultureInfo.InvariantCulture, out _))
            {
                var message = string.Format(ErrorMessage.InvalidFormatTemplate, value);
                throw new ArgumentException(message, nameof(value));
            }

            _value = value;
        }

        public static explicit operator ItuE164SubscriberNumber(string value)
        {
            if (!ulong.TryParse(value, NumberStyles, CultureInfo.InvariantCulture, out _))
            {
                var message = string.Format(ErrorMessage.InvalidFormatTemplate, value);
                throw new InvalidCastException(message);
            }

            return new ItuE164SubscriberNumber(value);
        }

        public static implicit operator ItuE164SubscriberNumber(ulong value)
        {
            return new ItuE164SubscriberNumber(value);
        }

        public static implicit operator ulong(ItuE164SubscriberNumber value)
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