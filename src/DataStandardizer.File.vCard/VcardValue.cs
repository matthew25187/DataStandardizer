using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using DataStandardizer.LanguageTag;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Parses and formats the value types of RFC 6350 §4 that map to .NET types and other packages: TEXT, BOOLEAN, INTEGER, FLOAT, URI and LANGUAGE-TAG.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other value types have types of their own, such as <see cref="VcardDate"/> and <see cref="VcardName"/>.
    /// </para>
    /// <para>
    /// A value in the <see cref="VcardValueFormat.Json"/> form is the content of a JSON string of jCard (RFC 7095 §3.5), with its JSON escaping removed, or the text of a JSON number or
    /// literal.  TEXT has no escaping of vCard in that form, and the items of a list are separate JSON strings, so a value is one item.  BOOLEAN is <c>true</c> or <c>false</c>.  INTEGER
    /// and FLOAT are JSON numbers.  A URI has no escaping of its commas.  A LANGUAGE-TAG is as it is in the text form.  jCard represents vCard 4.0 alone, so that form has no other version.
    /// </para>
    /// </remarks>
    public static class VcardValue
    {
        private const string TrueText = "TRUE";
        private const string FalseText = "FALSE";

        // ref. RFC 8259 §3: true = %x74.72.75.65; false = %x66.61.6c.73.65
        private const string JsonTrueText = "true";
        private const string JsonFalseText = "false";
        private const string GeoScheme = "GEO";

        // The longest text that Uri holds on .NET Framework.
        private const int MaxUriLengthOnEveryRuntime = 65519;

        #region TEXT

        /// <summary>
        /// Parses a vCard 4.0 TEXT value (RFC 6350 §4.1) into a span, removing its escaping.
        /// </summary>
        /// <param name="value">The value as it appears in the content line.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="destination">Receives the text.  It must be at least as long as <paramref name="value"/>, which is always enough.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if the value is not valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is shorter than <paramref name="value"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryParseText(ReadOnlySpan<char> value, VcardValueFormat format, Span<char> destination, out int charsWritten)
        {
            return TryParseText(value, format, VcardVersion.V4_0, destination, out charsWritten);
        }

        /// <summary>
        /// Parses a TEXT value into a span in the form a version uses, removing its escaping.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For a property that holds a list of values, split the list with <see cref="EnumerateList"/> first, then parse each item.  So a comma that isn't escaped makes a value
        /// invalid here, as RFC 6350 §4 and RFC 2426 §4 say.  In vCard 4.0 a semicolon may be escaped or not (RFC 6350 §3.4), and in vCard 3.0 it must be (RFC 2426 §4).
        /// </para>
        /// <para>
        /// vCard 2.1 has no escaping outside a compound value, so its text is copied as it is.
        /// </para>
        /// </remarks>
        /// <param name="value">The value as it appears in the content line.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <param name="destination">Receives the text.  It must be at least as long as <paramref name="value"/>, which is always enough.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if the value is not valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="destination"/> is shorter than <paramref name="value"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/>, which represents vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        public static bool TryParseText(ReadOnlySpan<char> value, VcardValueFormat format, VcardVersion version, Span<char> destination, out int charsWritten)
        {
            VcardValueFormatting.CheckFormat(format, version);
            if (destination.Length < value.Length)
                throw new ArgumentException("The destination must be at least as long as the value.", nameof(destination));

            charsWritten = 0;

            // ref. RFC 7095 §3.1: in jCard "any vCard escaping MUST be unescaped", so the text is copied as it is.
            var isUnescaped = format == VcardValueFormat.Json || version == VcardVersion.V2_1;
            if (!(isUnescaped ? VcardText.IsValidUnescapedText(value) : VcardText.IsValidText(value, version))) return false;

            if (isUnescaped)
            {
                value.CopyTo(destination);
                charsWritten = value.Length;
                return true;
            }

            charsWritten = VcardText.Unescape(value, destination, version);
            return true;
        }

        /// <summary>
        /// Parses a vCard 4.0 TEXT value (RFC 6350 §4.1) into a string, removing its escaping.
        /// </summary>
        /// <param name="value">The value as it appears in the content line.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The text, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryParseText(ReadOnlySpan<char> value, VcardValueFormat format, [NotNullWhen(true)] out string? result)
#else
        public static bool TryParseText(ReadOnlySpan<char> value, VcardValueFormat format, [MaybeNullWhen(false)] out string result)
#endif
        {
            var buffer = new char[value.Length];
            int charsWritten;
            if (!TryParseText(value, format, VcardVersion.V4_0, buffer, out charsWritten))
            {
                result = null;
                return false;
            }

            result = new string(buffer, 0, charsWritten);
            return true;
        }

        /// <summary>
        /// Formats text as a vCard 4.0 TEXT value (RFC 6350 §4.1) into a span.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a control character other than a tab or a line break, or a lone surrogate.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryFormatText(ReadOnlySpan<char> value, Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            return TryFormatText(value, destination, out charsWritten, format, VcardVersion.V4_0);
        }

        /// <summary>
        /// Formats text as a TEXT value into a span in the form a version uses.
        /// </summary>
        /// <remarks>
        /// In vCard 4.0 a backslash and a comma are escaped and each line break (CRLF, LF or a lone CR) is written <c>\n</c>.  vCard 3.0 escapes a semicolon as well.  vCard 2.1 has
        /// no escaping outside a compound value, so its text is written as it is.  <paramref name="destination"/> must not overlap <paramref name="value"/>.
        /// </remarks>
        /// <param name="value">The text.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a control character other than a tab or a line break, or a lone surrogate.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/>, which represents vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        public static bool TryFormatText(ReadOnlySpan<char> value, Span<char> destination, out int charsWritten, VcardValueFormat format, VcardVersion version)
        {
            VcardValueFormatting.CheckFormat(format, version);
            VcardText.CheckCharacters(value, nameof(value));

            var writer = new VcardSpanWriter(destination);
            if (format == VcardValueFormat.Json || version == VcardVersion.V2_1)
            {
                writer.Append(value);
            }
            else
            {
                VcardText.AppendEscaped(ref writer, value, version, version == VcardVersion.V3_0);
            }

            return writer.TryComplete(out charsWritten);
        }

        /// <summary>
        /// Formats text as a TEXT value in the form a version uses.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <param name="version">The version whose grammar applies.</param>
        /// <returns>The value.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a control character other than a tab or a line break, or a lone surrogate.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> or <paramref name="version"/> is not a member of its type.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        /// <exception cref="ArgumentException"><paramref name="format"/> is <see cref="VcardValueFormat.Json"/>, which represents vCard 4.0 alone, and <paramref name="version"/> isn't vCard 4.0.</exception>
        public static string FormatText(string value, VcardValueFormat format, VcardVersion version)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var buffer = new char[VcardText.GetMaxEscapedLength(value.Length)];
            int charsWritten;
            return VcardValueFormatting.ToString(buffer, TryFormatText(value.AsSpan(), buffer, out charsWritten, format, version), charsWritten);
        }

        /// <summary>
        /// Splits a list of values (RFC 6350 §4, RFC 2425 §5.8.4), such as <c>text-list</c> or <c>date-list</c>, on the commas that are not escaped.
        /// </summary>
        /// <param name="value">The value as it appears in the content line.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <returns>An enumerator of the items, with their escaping in place.  It yields one empty item for an empty value, which RFC 6350 §4 allows in a <c>text-list</c>.  In the
        /// <see cref="VcardValueFormat.Json"/> form, where each item of a list is a JSON string of its own (RFC 7095 §3.3), it yields the value as one item.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static VcardListEnumerator EnumerateList(ReadOnlySpan<char> value, VcardValueFormat format)
        {
            VcardValueFormatting.CheckFormat(format);

            return new VcardListEnumerator(value, format == VcardValueFormat.Json, false);
        }

        #endregion

        #region BOOLEAN

        /// <summary>
        /// Parses a BOOLEAN value (RFC 6350 §4.4): <c>TRUE</c> or <c>FALSE</c>, in any case.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The Boolean, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryParseBoolean(ReadOnlySpan<char> value, VcardValueFormat format, out bool result)
        {
            VcardValueFormatting.CheckFormat(format);

            if (format == VcardValueFormat.Json)
            {
                // ref. RFC 7095 §3.5.8: "The value element is a JSON boolean value."  ref. RFC 8259 §3: the literal names are lower case.
                result = value.SequenceEqual(JsonTrueText.AsSpan());
                return result || value.SequenceEqual(JsonFalseText.AsSpan());
            }

            result = VcardValueFormatting.EqualsKeyword(value, TrueText);
            return result || VcardValueFormatting.EqualsKeyword(value, FalseText);
        }

        /// <summary>
        /// Formats a BOOLEAN value (RFC 6350 §4.4) as <c>TRUE</c> or <c>FALSE</c>.
        /// </summary>
        /// <param name="value">The Boolean.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryFormatBoolean(bool value, Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            VcardValueFormatting.CheckFormat(format);

            var writer = new VcardSpanWriter(destination);
            if (format == VcardValueFormat.Json)
            {
                writer.Append((value ? JsonTrueText : JsonFalseText).AsSpan());
            }
            else
            {
                writer.Append((value ? TrueText : FalseText).AsSpan());
            }

            return writer.TryComplete(out charsWritten);
        }

        #endregion

        #region INTEGER

        /// <summary>
        /// Parses an INTEGER value (RFC 6350 §4.5), from -9223372036854775808 to 9223372036854775807, with an optional sign.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The integer, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryParseInteger(ReadOnlySpan<char> value, VcardValueFormat format, out long result)
        {
            VcardValueFormatting.CheckFormat(format);

            if (format == VcardValueFormat.Json) return TryParseJsonInteger(value, out result);

            // ref. RFC 6350 §4: integer = [sign] 1*DIGIT
            result = 0;
            var index = 0;
            var isNegative = false;
            if (value.Length > 0 && (value[0] == '+' || value[0] == '-'))
            {
                isNegative = value[0] == '-';
                index = 1;
            }

            if (index == value.Length) return false;

            // Accumulated as a negative number, since the smallest value has no positive counterpart.
            long total = 0;
            for (; index < value.Length; index++)
            {
                var character = value[index];
                if (!VcardValueFormatting.IsDigit(character)) return false;

                var digit = character - '0';
                if (total < (long.MinValue + digit) / 10) return false;

                total = total * 10 - digit;
            }

            if (!isNegative && total == long.MinValue) return false;

            result = isNegative ? total : -total;
            return true;
        }

        /// <summary>
        /// Formats an INTEGER value (RFC 6350 §4.5).
        /// </summary>
        /// <param name="value">The integer.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryFormatInteger(long value, Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            VcardValueFormatting.CheckFormat(format);

            var writer = new VcardSpanWriter(destination);
            writer.Append(value.ToString(CultureInfo.InvariantCulture).AsSpan());
            return writer.TryComplete(out charsWritten);
        }

        #endregion

        #region FLOAT

        /// <summary>
        /// Parses a FLOAT value (RFC 6350 §4.6): an optional sign, digits and an optional fraction, with no exponent.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The number, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryParseFloat(ReadOnlySpan<char> value, VcardValueFormat format, out double result)
        {
            VcardValueFormatting.CheckFormat(format);

            if (format == VcardValueFormat.Json) return TryParseJsonFloat(value, out result);

            return TryParseFloatCore(value, true, out result);
        }

        /// <summary>
        /// Formats a FLOAT value (RFC 6350 §4.6), with no exponent.
        /// </summary>
        /// <param name="value">The number.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member, or <paramref name="value"/> is NaN or infinite.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryFormatFloat(double value, Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            VcardValueFormatting.CheckFormat(format);

            var writer = new VcardSpanWriter(destination);
            writer.Append(FormatFloatText(value).AsSpan());
            return writer.TryComplete(out charsWritten);
        }

        #endregion

        #region URI

        /// <summary>
        /// Parses a URI value (RFC 6350 §4.2) as it appears in a content line of vCard 4.0: an absolute URI, which starts with a scheme, with its commas escaped.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This allocates the <see cref="Uri"/>.  Characters RFC 3986 doesn't allow are rejected, including <c>[</c> and <c>]</c> outside the host (§3.2.2), except in a <c>geo</c> URI,
        /// whose parameter values RFC 5870 §3.3 lets hold them.
        /// </para>
        /// <para>
        /// RFC 6350 §3.4 says a comma in a value "MUST be escaped", and its errata 3845 and 3846 apply that to URI values, as in <c>geo:37.386013\,-122.082932</c>.  So a comma with no
        /// backslash is rejected; see <see cref="VcardOptions.AcceptUnescapedCommasInUris"/>.  A semicolon "MAY be escaped".  The <see cref="Uri"/> has the text with its escaping
        /// removed.
        /// </para>
        /// </remarks>
        /// <param name="value">The value, such as <c>http://www.example.com/my/picture.jpg</c>.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The URI, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public static bool TryParseUri(ReadOnlySpan<char> value, VcardValueFormat format, [NotNullWhen(true)] out Uri? result)
#else
        public static bool TryParseUri(ReadOnlySpan<char> value, VcardValueFormat format, [MaybeNullWhen(false)] out Uri result)
#endif
        {
            VcardValueFormatting.CheckFormat(format);

            result = null;

            // ref. RFC 7095 §3.5.2: in jCard a URI is a JSON string, which has no escaping of vCard.
            if (format == VcardValueFormat.Json || (value.IndexOf('\\') < 0 && value.IndexOf(',') < 0)) return TryParseUriText(value, out result);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? text;
#else
            string text;
#endif
            return TryUnescapeUri(value, out text) && TryParseUriText(text.AsSpan(), out result);
        }

        /// <summary>
        /// Formats a URI value (RFC 6350 §4.2) as it appears in a content line of vCard 4.0: the URI as it was given, with its commas escaped, as RFC 6350 §3.4 and its errata 3845 and 3846
        /// say.
        /// </summary>
        /// <param name="value">The URI, which must be absolute.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not absolute, or was given with characters a URI value can't hold, such as a space, which
        /// <see cref="Uri"/> accepts and <see cref="TryParseUri"/> doesn't.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryFormatUri(Uri value, Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            CheckUri(value, nameof(value));
            VcardValueFormatting.CheckFormat(format);

            var writer = new VcardSpanWriter(destination);
            writer.Append((format == VcardValueFormat.Json ? value.OriginalString : EscapeUri(value.OriginalString)).AsSpan());
            return writer.TryComplete(out charsWritten);
        }

        #endregion

        #region LANGUAGE-TAG

        /// <summary>
        /// Parses a LANGUAGE-TAG value (RFC 6350 §4.8), which RFC 6350 §4 defines as a well-formed tag of RFC 5646 §2.1.
        /// </summary>
        /// <remarks>
        /// <see cref="Bcp47LanguageTag"/> decides which tags are well-formed.  The check itself allocates nothing, but a tag that passes it is copied to the one string it holds.
        /// </remarks>
        /// <param name="value">The value, such as <c>fr-CA</c>.</param>
        /// <param name="format">The lexical form of the value.</param>
        /// <param name="result">The tag, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryParseLanguageTag(ReadOnlySpan<char> value, VcardValueFormat format, out Bcp47LanguageTag result)
        {
            VcardValueFormatting.CheckFormat(format);

            return Bcp47LanguageTag.TryCreate(value, out result);
        }

        /// <summary>
        /// Formats a LANGUAGE-TAG value (RFC 6350 §4.8).
        /// </summary>
        /// <param name="value">The tag.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="format">The lexical form to write.</param>
        /// <returns><c>true</c> if the value was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="format"/> is not a <see cref="VcardValueFormat"/> member.</exception>
        /// <exception cref="NotSupportedException"><paramref name="format"/> is <see cref="VcardValueFormat.Xml"/>, which isn't implemented yet.</exception>
        public static bool TryFormatLanguageTag(Bcp47LanguageTag value, Span<char> destination, out int charsWritten, VcardValueFormat format)
        {
            VcardValueFormatting.CheckFormat(format);

            var text = value.AsSpan();
            if (text.Length == 0)
                throw new ArgumentException("The language tag is not initialised.", nameof(value));

            var writer = new VcardSpanWriter(destination);
            writer.Append(text);
            return writer.TryComplete(out charsWritten);
        }

        #endregion

        /// <summary>
        /// Parses a float of RFC 6350 §4, or the number of RFC 5870 §3.3 if <paramref name="allowsPlusSign"/> is <c>false</c>.
        /// </summary>
        internal static bool TryParseFloatCore(ReadOnlySpan<char> value, bool allowsPlusSign, out double result)
        {
            // ref. RFC 6350 §4: float = [sign] 1*DIGIT ["." 1*DIGIT].  RFC 5870 §3.3: num = [ "-" ] pnum; pnum = 1*DIGIT [ "." 1*DIGIT ]
            result = 0;
            var index = 0;
            if (value.Length > 0 && (value[0] == '-' || (allowsPlusSign && value[0] == '+')))
            {
                index = 1;
            }

            var integerStart = index;
            while (index < value.Length && VcardValueFormatting.IsDigit(value[index]))
            {
                index++;
            }

            if (index == integerStart) return false;

            if (index < value.Length)
            {
                if (value[index] != '.') return false;

                var fractionStart = ++index;
                while (index < value.Length && VcardValueFormatting.IsDigit(value[index]))
                {
                    index++;
                }

                if (index == fractionStart || index != value.Length) return false;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            if (!double.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result)) return false;
#else
            if (!double.TryParse(value.ToString(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result)) return false;
#endif

            return !double.IsInfinity(result);
        }

        /// <summary>
        /// Parses the text of a JSON number as an INTEGER of jCard (RFC 7095 §3.5.9), which "does not prohibit" a fraction or an exponent.  They "MUST be eliminated" when the value
        /// becomes one of vCard, so the number is taken without its fraction, rounded toward zero.
        /// </summary>
        internal static bool TryParseJsonInteger(ReadOnlySpan<char> value, out long result)
        {
            // The digits are taken from the text, since a decimal or a double would round a long fraction up across a whole number.
            result = 0;
            bool isNegative;
            string integerDigits, fractionDigits;
            if (!VcardValueFormatting.TryExpandJsonNumber(value, out isNegative, out integerDigits, out fractionDigits)) return false;
            if (integerDigits.Length == 0) return true;

            return TryParseInteger((isNegative ? "-" + integerDigits : integerDigits).AsSpan(), VcardValueFormat.Text, out result);
        }

        /// <summary>
        /// Gets the text of a FLOAT of RFC 6350 §4.6 for a JSON number of jCard, whose exponent "MUST be eliminated" (RFC 7095 §3.5.10), keeping every digit.
        /// </summary>
        /// <returns><c>false</c> if the text isn't a JSON number, or its exponent is too large to expand.</returns>
        internal static bool TryExpandJsonFloat(ReadOnlySpan<char> value, out string text)
        {
            bool isNegative;
            string integerDigits, fractionDigits;
            text = string.Empty;
            if (!VcardValueFormatting.TryExpandJsonNumber(value, out isNegative, out integerDigits, out fractionDigits)) return false;

            var builder = new StringBuilder();
            if (isNegative)
            {
                builder.Append('-');
            }

            builder.Append(integerDigits.Length == 0 ? "0" : integerDigits);
            if (fractionDigits.Length != 0)
            {
                builder.Append('.').Append(fractionDigits);
            }

            text = builder.ToString();
            return true;
        }

        /// <summary>
        /// Parses the text of a JSON number as a FLOAT of jCard (RFC 7095 §3.5.10), which may have an exponent.
        /// </summary>
        internal static bool TryParseJsonFloat(ReadOnlySpan<char> value, out double result)
        {
            result = 0;
            if (!VcardValueFormatting.IsJsonNumber(value)) return false;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result)) return false;
#else
            if (!double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out result)) return false;
#endif

            return !double.IsInfinity(result);
        }

        /// <summary>
        /// Gets the text of a number with no exponent, which RFC 6350 §4.6 disallows.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is NaN or infinite.</exception>
        internal static string FormatFloatText(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "A FLOAT value cannot be NaN or infinite.");

            var text = value.ToString("R", CultureInfo.InvariantCulture);
            var exponentIndex = text.IndexOf('E');
            if (exponentIndex < 0) return text;

            var isNegative = text[0] == '-';
            var mantissa = text.Substring(isNegative ? 1 : 0, exponentIndex - (isNegative ? 1 : 0));
            var exponent = int.Parse(text.Substring(exponentIndex + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            var pointIndex = mantissa.IndexOf('.');
            var digits = pointIndex < 0 ? mantissa : mantissa.Remove(pointIndex, 1);
            var newPointIndex = (pointIndex < 0 ? mantissa.Length : pointIndex) + exponent;

            string expanded;
            if (newPointIndex <= 0)
            {
                expanded = "0." + new string('0', -newPointIndex) + digits;
            }
            else if (newPointIndex >= digits.Length)
            {
                expanded = digits + new string('0', newPointIndex - digits.Length);
            }
            else
            {
                expanded = digits.Substring(0, newPointIndex) + "." + digits.Substring(newPointIndex);
            }

            return isNegative ? "-" + expanded : expanded;
        }

        /// <summary>
        /// Checks that a URI is one a URI value can hold.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="uri"/> is not absolute, or its text is not that of a URI value.</exception>
        internal static void CheckUri(Uri uri, string parameterName)
        {
            if (!uri.IsAbsoluteUri)
                throw new ArgumentException("The URI must be absolute.", parameterName);
            if (!IsUriText(uri.OriginalString.AsSpan()))
                throw new ArgumentException("The URI was given with characters that a URI value can't hold.", parameterName);
        }

        /// <summary>
        /// Parses the text of a URI that has no escaping of a content line, such as the value of a <c>GEO</c> parameter, or the text a <see cref="Uri"/> was given.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static bool TryParseUriText(ReadOnlySpan<char> text, [NotNullWhen(true)] out Uri? result)
#else
        internal static bool TryParseUriText(ReadOnlySpan<char> text, [MaybeNullWhen(false)] out Uri result)
#endif
        {
            result = null;
            return IsUriText(text) && Uri.TryCreate(text.ToString(), UriKind.Absolute, out result);
        }

        /// <summary>
        /// Gets the text of a URI as it appears in a content line of vCard 4.0.  ref. RFC 6350 §3.4 and its errata 3845 and 3846: a comma "MUST be escaped".  A semicolon "MAY be escaped",
        /// and isn't, since the RFC writes one as it is in <c>tel:+1-555-555-5555;ext=5555</c> (§6.4.1).
        /// </summary>
        internal static string EscapeUri(string text)
        {
            return text.IndexOf(',') < 0 ? text : text.Replace(",", "\\,");
        }

        /// <summary>
        /// Gets the text of a URI value with the commas that have no backslash escaped, for <see cref="VcardOptions.AcceptUnescapedCommasInUris"/>.  Anything else, including an escape
        /// that isn't valid, is left to be judged as usual.
        /// </summary>
        /// <returns>The text; or <paramref name="value"/> itself if it has no comma.</returns>
        internal static string EscapeBareCommas(string value)
        {
            if (value.IndexOf(',') < 0) return value;

            var builder = new StringBuilder(value.Length + 1);
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character == '\\' && index + 1 < value.Length)
                {
                    builder.Append(character).Append(value[++index]);
                }
                else if (character == ',')
                {
                    builder.Append('\\').Append(character);
                }
                else
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets whether a URI value, as it appears in a content line of vCard 4.0, is valid, without allocating unless it has an escape or a comma.
        /// </summary>
        internal static bool IsUriValue(ReadOnlySpan<char> value)
        {
            if (value.IndexOf('\\') < 0 && value.IndexOf(',') < 0) return IsUriText(value);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? text;
#else
            string text;
#endif
            return TryUnescapeUri(value, out text) && IsUriText(text.AsSpan());
        }

        /// <summary>
        /// Gets whether a URI value, as it appears in a content line of vCard 4.0, is valid, with the same answer on every runtime.  <see cref="Uri"/> on .NET Framework can't hold a long one, such
        /// as the <c>data:</c> URI of a photo, and neither RFC 6350 nor RFC 3986 sets a limit, so a long one is checked without it.
        /// </summary>
        internal static bool IsValidUriValue(ReadOnlySpan<char> value)
        {
            if (value.Length > MaxUriLengthOnEveryRuntime) return IsUriValue(value);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Uri? uri;
#else
            Uri uri;
#endif
            return TryParseUri(value, VcardValueFormat.Text, out uri);
        }

        /// <summary>
        /// Gets whether text with no escaping of a content line is a URI, with the same answer on every runtime, as <see cref="IsValidUriValue"/> does for a value of vCard 4.0.
        /// </summary>
        internal static bool IsValidUriText(ReadOnlySpan<char> text)
        {
            if (text.Length > MaxUriLengthOnEveryRuntime) return IsUriText(text);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Uri? uri;
#else
            Uri uri;
#endif
            return TryParseUriText(text, out uri);
        }

        /// <summary>
        /// Gets the text of a URI value as it appears in a content line of vCard 4.0, with its escaping removed, without making a <see cref="Uri"/>, which can't hold a long one on every runtime.
        /// </summary>
        /// <returns><c>false</c> if the value isn't valid.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static bool TryGetUriText(string value, [NotNullWhen(true)] out string? text)
#else
        internal static bool TryGetUriText(string value, [MaybeNullWhen(false)] out string text)
#endif
        {
            text = null;
            if (!IsValidUriValue(value.AsSpan())) return false;

            if (value.IndexOf('\\') < 0)
            {
                text = value;
                return true;
            }

            return TryUnescapeUri(value.AsSpan(), out text);
        }

        /// <summary>
        /// Gets whether text has the characters and the scheme of a URI (RFC 3986 §2, §3).  It stops a path being taken as a file URI.
        /// </summary>
        internal static bool IsUriText(ReadOnlySpan<char> value)
        {
            if (value.Length == 0 || !IsAsciiLetter(value[0])) return false;

            // ref. RFC 3986§2: a URI is made of unreserved and reserved characters and percent-encodings, all of them printable ASCII.
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character <= ' ' || character >= '\x7F') return false;

                switch (character)
                {
                    case '"':
                    case '<':
                    case '>':
                    case '\\':
                    case '^':
                    case '`':
                    case '{':
                    case '|':
                    case '}':
                        return false;
                    case '%':
                        // ref. RFC 3986§2.1: pct-encoded = "%" HEXDIG HEXDIG
                        if (index + 2 >= value.Length || !IsHexDigit(value[index + 1]) || !IsHexDigit(value[index + 2])) return false;

                        break;
                }
            }

            // ref. RFC 3986§3.1: scheme = ALPHA *( ALPHA / DIGIT / "+" / "-" / "." )
            for (var index = 1; index < value.Length; index++)
            {
                var character = value[index];
                if (character == ':') return AreBracketsAllowed(value, index);
                if (!IsAsciiLetter(character) && !VcardValueFormatting.IsDigit(character) && character != '+' && character != '-' && character != '.')
                {
                    return false;
                }
            }

            return false;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static bool TryUnescapeUri(ReadOnlySpan<char> value, [NotNullWhen(true)] out string? text)
#else
        private static bool TryUnescapeUri(ReadOnlySpan<char> value, [MaybeNullWhen(false)] out string text)
#endif
        {
            // ref. RFC 6350 §3.4: a comma "MUST be escaped", and a semicolon "MAY be escaped".  A backslash can't be in a URI (RFC 3986 §2), so no other escape can be either.
            text = null;
            var characters = new char[value.Length];
            var length = 0;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character == ',') return false;

                if (character == '\\')
                {
                    if (++index == value.Length) return false;

                    character = value[index];
                    if (character != ',' && character != ';') return false;
                }

                characters[length++] = character;
            }

            text = new string(characters, 0, length);
            return true;
        }

        private static bool AreBracketsAllowed(ReadOnlySpan<char> value, int colonIndex)
        {
            // ref. RFC 5870 §3.3: p-unreserved = "[" / "]" / ":" / "&" / "+" / "$"
            if (VcardValueFormatting.EqualsKeyword(value.Slice(0, colonIndex), GeoScheme)) return true;

            // ref. RFC 3986 §3.2.2: IP-literal = "[" ( IPv6address / IPvFuture ) "]".  No other part of a URI has a bracket.
            var rest = value.Slice(colonIndex + 1);
            if (rest.Length >= 2 && rest[0] == '/' && rest[1] == '/')
            {
                rest = rest.Slice(2);
                var authorityEnd = rest.IndexOfAny('/', '?', '#');
                rest = authorityEnd < 0 ? default(ReadOnlySpan<char>) : rest.Slice(authorityEnd);
            }

            return rest.IndexOfAny('[', ']') < 0;
        }

        private static bool IsAsciiLetter(char character)
        {
            return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
        }

        private static bool IsHexDigit(char character)
        {
            return VcardValueFormatting.IsDigit(character) || (character >= 'A' && character <= 'F') || (character >= 'a' && character <= 'f');
        }
    }

    /// <summary>
    /// Enumerates the items of a list of values, split on the commas that are not escaped.  Get one from <see cref="VcardValue.EnumerateList"/> or
    /// <see cref="VcardComponentView.GetEnumerator"/>.
    /// </summary>
    public ref struct VcardListEnumerator
    {
        private ReadOnlySpan<char> _rest;
        private ReadOnlySpan<char> _current;
        private bool _isDone;
        private readonly bool _isSingleItem;

        internal VcardListEnumerator(ReadOnlySpan<char> value, bool isSingleItem, bool isEmpty)
        {
            _rest = value;
            _current = default(ReadOnlySpan<char>);
            _isDone = isEmpty;
            _isSingleItem = isSingleItem;
        }

        /// <summary>
        /// Gets the current item, with its escaping in place.
        /// </summary>
        public ReadOnlySpan<char> Current => _current;

        /// <summary>
        /// Gets the enumerator, so that it can be used with <c>foreach</c>.
        /// </summary>
        public VcardListEnumerator GetEnumerator()
        {
            return this;
        }

        /// <summary>
        /// Moves to the next item.
        /// </summary>
        /// <returns><c>true</c> if there is another item; otherwise <c>false</c>.</returns>
        public bool MoveNext()
        {
            if (_isDone) return false;

            var index = _isSingleItem ? _rest.Length : VcardText.IndexOfDelimiter(_rest, ',', VcardVersion.V4_0);
            if (index == _rest.Length)
            {
                _current = _rest;
                _rest = default(ReadOnlySpan<char>);
                _isDone = true;
                return true;
            }

            _current = _rest.Slice(0, index);
            _rest = _rest.Slice(index + 1);
            return true;
        }
    }
}
