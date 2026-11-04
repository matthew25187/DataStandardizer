using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A value in an XML property list.  It can't be changed once made.
    /// </summary>
    /// <remarks>
    /// A value that was read keeps its text, so that a <c>real</c> of <c>1.50</c>, or an <c>integer</c> too large for a <see cref="long"/>, is written back as it was.
    /// </remarks>
    public sealed class EmlxPlistValue
    {
        private const string DateFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";
        private const int DecimalBase = 10;
        private const int HexBase = 16;
        private const string HexPrefix = "0x";
        private const string NaNText = "nan";
        private const string NegativeInfinityText = "-infinity";
        private const string PositiveInfinityText = "+infinity";

        private static readonly ReadOnlyCollection<EmlxPlistValue> NoItems = new ReadOnlyCollection<EmlxPlistValue>(new EmlxPlistValue[0]);
        private static readonly ReadOnlyCollection<KeyValuePair<string, EmlxPlistValue>> NoEntries = new ReadOnlyCollection<KeyValuePair<string, EmlxPlistValue>>(new KeyValuePair<string, EmlxPlistValue>[0]);

        private EmlxPlistValue(EmlxPlistValueKind kind, string text, ReadOnlyCollection<EmlxPlistValue> items, ReadOnlyCollection<KeyValuePair<string, EmlxPlistValue>> entries)
        {
            Kind = kind;
            Text = text;
            Items = items;
            Entries = entries;
        }

        /// <summary>
        /// Gets the kind of value.
        /// </summary>
        public EmlxPlistValueKind Kind { get; }

        /// <summary>
        /// Gets the text of a <c>string</c>, <c>integer</c>, <c>real</c>, <c>date</c> or <c>data</c>, as it is held.  For any other kind, it is empty.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the values of an <c>array</c>.  The list is read-only, and for any other kind, it is empty.
        /// </summary>
        public IReadOnlyList<EmlxPlistValue> Items { get; }

        /// <summary>
        /// Gets the keys and values of a <c>dict</c>, in order.  The list is read-only, and for any other kind, it is empty.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, EmlxPlistValue>> Entries { get; }

        /// <summary>
        /// Creates a <c>string</c>.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character that XML doesn't allow.</exception>
        public static EmlxPlistValue FromString(string value)
        {
            CheckText(value, nameof(value));
            return CreateScalar(EmlxPlistValueKind.String, value);
        }

        /// <summary>
        /// Creates an <c>integer</c>.
        /// </summary>
        /// <param name="value">The number.</param>
        /// <returns>The value.</returns>
        public static EmlxPlistValue FromInteger(long value)
        {
            return CreateScalar(EmlxPlistValueKind.Integer, value.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Creates a <c>real</c>.
        /// </summary>
        /// <param name="value">The number.</param>
        /// <returns>The value.</returns>
        public static EmlxPlistValue FromReal(double value)
        {
            string text;
            if (double.IsNaN(value)) text = NaNText;
            else if (double.IsPositiveInfinity(value)) text = PositiveInfinityText;
            else if (double.IsNegativeInfinity(value)) text = NegativeInfinityText;
            else text = value.ToString("R", CultureInfo.InvariantCulture);

            return CreateScalar(EmlxPlistValueKind.Real, text);
        }

        /// <summary>
        /// Creates a <c>date</c>, which holds whole seconds in UTC.
        /// </summary>
        /// <param name="value">The date and time.</param>
        /// <returns>The value.</returns>
        public static EmlxPlistValue FromDate(DateTimeOffset value)
        {
            return CreateScalar(EmlxPlistValueKind.Date, value.ToUniversalTime().ToString(DateFormat, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Creates a <c>data</c>.
        /// </summary>
        /// <param name="value">The bytes.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        public static EmlxPlistValue FromData(byte[] value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return CreateScalar(EmlxPlistValueKind.Data, Convert.ToBase64String(value));
        }

        /// <summary>
        /// Creates a <c>true</c> or <c>false</c>.
        /// </summary>
        /// <param name="value">The flag.</param>
        /// <returns>The value.</returns>
        public static EmlxPlistValue FromBoolean(bool value)
        {
            return CreateScalar(value ? EmlxPlistValueKind.True : EmlxPlistValueKind.False, string.Empty);
        }

        /// <summary>
        /// Creates an <c>array</c>.
        /// </summary>
        /// <param name="items">The values.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="items"/> holds a <c>null</c>.</exception>
        public static EmlxPlistValue FromArray(IEnumerable<EmlxPlistValue> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            var list = new List<EmlxPlistValue>();
            foreach (var item in items)
            {
                if (item is null)
                    throw new ArgumentException("A value is null.", nameof(items));

                list.Add(item);
            }

            return CreateArray(list);
        }

        /// <summary>
        /// Creates a <c>dict</c>.
        /// </summary>
        /// <param name="entries">The keys and values, in order.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="entries"/> holds a <c>null</c> key or value, or a key with a character that XML doesn't allow.</exception>
        public static EmlxPlistValue FromDictionary(IEnumerable<KeyValuePair<string, EmlxPlistValue>> entries)
        {
            if (entries is null)
                throw new ArgumentNullException(nameof(entries));

            var list = new List<KeyValuePair<string, EmlxPlistValue>>();
            foreach (var entry in entries)
            {
                if (entry.Key is null || entry.Value is null)
                    throw new ArgumentException("A key or value is null.", nameof(entries));

                CheckText(entry.Key, nameof(entries));
                list.Add(entry);
            }

            return CreateDictionary(list);
        }

        /// <summary>
        /// Gets the number of an <c>integer</c>.
        /// </summary>
        /// <param name="value">Receives the number.</param>
        /// <returns><c>true</c> if this is an <c>integer</c> that a <see cref="long"/> can hold.</returns>
        public bool TryGetInt64(out long value)
        {
            value = 0;
            if (Kind != EmlxPlistValueKind.Integer) return false;

            if (Text.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase))
                return long.TryParse(Text.Substring(HexPrefix.Length), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value) && value >= 0;

            return long.TryParse(Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// Gets the number of a <c>real</c> or an <c>integer</c>.
        /// </summary>
        /// <param name="value">Receives the number, rounded if a <see cref="double"/> can't hold it exactly, or 0.</param>
        /// <returns><c>true</c> if this is a number within the range of a <see cref="double"/>, or a <c>real</c> that holds a word for a value with no digits.</returns>
        public bool TryGetDouble(out double value)
        {
            value = 0;
            if (Kind == EmlxPlistValueKind.Integer)
            {
                if (TryGetInt64(out var integer))
                {
                    value = integer;
                    return true;
                }

                return TryGetLargeInteger(out value);
            }

            if (Kind != EmlxPlistValueKind.Real) return false;

            if (TryGetSpecialReal(Text, out value)) return true;

            // A number past the range is an infinity on some runtimes, and a failure on others.
            return double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && KeepIfFinite(ref value);
        }

        /// <summary>
        /// Gets the date and time of a <c>date</c>.
        /// </summary>
        /// <param name="value">Receives the date and time, in UTC.</param>
        /// <returns><c>true</c> if this is a <c>date</c>.</returns>
        public bool TryGetDate(out DateTimeOffset value)
        {
            value = default(DateTimeOffset);
            return Kind == EmlxPlistValueKind.Date && TryParseDate(Text, out value);
        }

        /// <summary>
        /// Gets the bytes of a <c>data</c>.
        /// </summary>
        /// <param name="value">Receives a copy of the bytes, or an empty array if this is not a <c>data</c>.</param>
        /// <returns><c>true</c> if this is a <c>data</c>.</returns>
        public bool TryGetData(out byte[] value)
        {
            if (Kind != EmlxPlistValueKind.Data)
            {
                value = new byte[0];
                return false;
            }

            value = Convert.FromBase64String(Text);
            return true;
        }

        /// <summary>
        /// Creates a value from a list that it takes over.
        /// </summary>
        internal static EmlxPlistValue CreateArray(List<EmlxPlistValue> items)
        {
            return new EmlxPlistValue(EmlxPlistValueKind.Array, string.Empty, new ReadOnlyCollection<EmlxPlistValue>(items), NoEntries);
        }

        /// <summary>
        /// Creates a value from a list that it takes over.
        /// </summary>
        internal static EmlxPlistValue CreateDictionary(List<KeyValuePair<string, EmlxPlistValue>> entries)
        {
            return new EmlxPlistValue(EmlxPlistValueKind.Dictionary, string.Empty, NoItems, new ReadOnlyCollection<KeyValuePair<string, EmlxPlistValue>>(entries));
        }

        /// <summary>
        /// Creates a value of a kind that holds no other values, whose text has been checked.
        /// </summary>
        internal static EmlxPlistValue CreateScalar(EmlxPlistValueKind kind, string text)
        {
            return new EmlxPlistValue(kind, text, NoItems, NoEntries);
        }

        /// <summary>
        /// Determines if text is one that an element of a kind may hold.
        /// </summary>
        internal static bool IsValidText(EmlxPlistValueKind kind, string text)
        {
            switch (kind)
            {
                case EmlxPlistValueKind.Integer:
                    return IsValidInteger(text);
                case EmlxPlistValueKind.Real:
                    return IsValidReal(text);
                case EmlxPlistValueKind.Date:
                    return TryParseDate(text, out _);
                case EmlxPlistValueKind.Data:
                    return IsValidData(text);
                case EmlxPlistValueKind.True:
                case EmlxPlistValueKind.False:
                    return text.Length == 0;
                default:
                    return true;
            }
        }

        // ref. XML 1.0 §2.2: the characters that a document may hold.
        internal static void CheckText(string text, string parameterName)
        {
            if (text is null)
                throw new ArgumentNullException(parameterName);

            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                bool isValid;
                if (char.IsSurrogate(character))
                {
                    isValid = char.IsHighSurrogate(character) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]);
                    index++;
                }
                else
                {
                    isValid = character == '\t' || character == '\n' || character == '\r' || (character >= ' ' && character <= '�');
                }

                if (!isValid)
                    throw new ArgumentException("The text holds a character that XML doesn't allow.", parameterName);
            }
        }

        private bool TryGetLargeInteger(out double value)
        {
            if (!Text.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase))
                return double.TryParse(Text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value) && KeepIfFinite(ref value);

            value = 0;
            for (var index = HexPrefix.Length; index < Text.Length; index++)
            {
                var character = Text[index];
                value = (value * HexBase) + (character <= '9' ? character - '0' : (char.ToLowerInvariant(character) - 'a') + DecimalBase);
            }

            return KeepIfFinite(ref value);
        }

        private static bool KeepIfFinite(ref double value)
        {
            if (!double.IsInfinity(value)) return true;

            value = 0;
            return false;
        }

        private static bool IsValidInteger(string text)
        {
            if (text.StartsWith(HexPrefix, StringComparison.OrdinalIgnoreCase)) return AreAllDigits(text, HexPrefix.Length, true);

            return AreAllDigits(text, text.Length > 0 && (text[0] == '-' || text[0] == '+') ? 1 : 0, false);
        }

        // ref. the form that CFPropertyList reads: a sign, digits, a fraction and an exponent, or a word for a value that has no digits.
        private static bool IsValidReal(string text)
        {
            if (TryGetSpecialReal(text, out _)) return true;

            var position = text.Length > 0 && (text[0] == '-' || text[0] == '+') ? 1 : 0;
            var digitsStart = position;
            position = SkipDigits(text, position);
            var digitCount = position - digitsStart;
            if (position < text.Length && text[position] == '.')
            {
                var fractionStart = ++position;
                position = SkipDigits(text, position);
                digitCount += position - fractionStart;
            }

            if (digitCount == 0) return false;

            if (position < text.Length && (text[position] == 'e' || text[position] == 'E'))
            {
                position++;
                if (position < text.Length && (text[position] == '-' || text[position] == '+')) position++;

                var exponentStart = position;
                position = SkipDigits(text, position);
                if (position == exponentStart) return false;
            }

            return position == text.Length;
        }

        private static bool TryGetSpecialReal(string text, out double value)
        {
            value = 0;
            if (string.Equals(text, NaNText, StringComparison.OrdinalIgnoreCase))
            {
                value = double.NaN;
                return true;
            }

            if (string.Equals(text, PositiveInfinityText, StringComparison.OrdinalIgnoreCase))
            {
                value = double.PositiveInfinity;
                return true;
            }

            if (string.Equals(text, NegativeInfinityText, StringComparison.OrdinalIgnoreCase))
            {
                value = double.NegativeInfinity;
                return true;
            }

            return false;
        }

        private static bool IsValidData(string text)
        {
            try
            {
                Convert.FromBase64String(text);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool TryParseDate(string text, out DateTimeOffset value)
        {
            return DateTimeOffset.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value);
        }

        private static int SkipDigits(string text, int position)
        {
            while (position < text.Length && text[position] >= '0' && text[position] <= '9') position++;

            return position;
        }

        private static bool AreAllDigits(string text, int start, bool allowHex)
        {
            if (start >= text.Length) return false;

            for (var index = start; index < text.Length; index++)
            {
                var character = text[index];
                var isDigit = character >= '0' && character <= '9';
                var isHex = (character >= 'a' && character <= 'f') || (character >= 'A' && character <= 'F');
                if (!isDigit && !(allowHex && isHex)) return false;
            }

            return true;
        }
    }
}
