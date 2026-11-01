using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// A media type parameter (RFC 2045 §5.1): a name, its decoded value, and the language of the value, if any (RFC 2231).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Names are compared case-insensitively. Values are compared case-sensitively, except for the value of <c>charset</c> (RFC 2046 §4.1.2).
    /// Languages are compared case-insensitively. The character set an extended value was written in is not compared, as it doesn't change
    /// the value.
    /// </para>
    /// <para>
    /// A value of printable US-ASCII characters, spaces and horizontal tabs, with no language, is written as a token when it is one, and
    /// otherwise as a quoted-string. Any other value, or a value with a language, is written as an RFC 2231 extended value in UTF-8, such as
    /// <c>title*=UTF-8'en'%C2%A3%20rates</c>. Use <see cref="FormatSections(int)"/> to split a long parameter into sections.
    /// </para>
    /// </remarks>
    public readonly struct MimeParameter : IEquatable<MimeParameter>
    {
        private const string CharsetParameterName = "charset";
        private const string UninitializedMessage = nameof(MimeParameter) + " is uninitialized.";

        private readonly string _name;
        private readonly string _value;
#if NETCOREAPP3_0_OR_GREATER
        private readonly string? _charset;
        private readonly string? _language;
#else
        private readonly string _charset;
        private readonly string _language;
#endif

        /// <summary>
        /// Creates a parameter.
        /// </summary>
        /// <param name="name">The name, which must follow the RFC 6838 §4.2 naming rules.</param>
        /// <param name="value">The value, which may be empty. It must not contain an unpaired surrogate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid parameter name, or <paramref name="value"/> contains an
        /// unpaired surrogate.</exception>
        public MimeParameter(string name, string value)
        {
            CheckNameAndValue(name, value);

            _name = name;
            _value = value;
            _charset = null;
            _language = null;
        }

        /// <summary>
        /// Creates a parameter with the language of its value, which is written as an RFC 2231 extended value.
        /// </summary>
        /// <param name="name">The name, which must follow the RFC 6838 §4.2 naming rules.</param>
        /// <param name="value">The value, which may be empty. It must not contain an unpaired surrogate.</param>
        /// <param name="language">The language of the value, which must be an RFC 1766 §2 language tag, such as <c>en-US</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="value"/> or <paramref name="language"/> is
        /// <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid parameter name, <paramref name="value"/> contains an
        /// unpaired surrogate, or <paramref name="language"/> is not an RFC 1766 language tag.</exception>
        /// <remarks>
        /// RFC 2231 §4 calls for RFC 1766 language tags, whose subtags are 1 to 8 letters. Later BCP 47 tags with other subtags, such as
        /// <c>es-419</c>, are not accepted.
        /// </remarks>
        public MimeParameter(string name, string value, string language)
        {
            CheckNameAndValue(name, value);
            if (language is null) throw new ArgumentNullException(nameof(language));
            if (!MimeExtendedParameter.IsLanguage(language.AsSpan())) throw new ArgumentException("The language is not an RFC 1766 language tag.", nameof(language));

            _name = name;
            _value = value;
            _charset = null;
            _language = language;
        }

        // Used by the parser, which has already checked the name, value and language.
#if NETCOREAPP3_0_OR_GREATER
        private MimeParameter(string name, string value, string? charset, string? language, bool _)
#else
        private MimeParameter(string name, string value, string charset, string language, bool _)
#endif
        {
            _name = name;
            _value = value;
            _charset = charset;
            _language = language;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeParameter"/> is not initialised.</exception>
        public string Name => _name ?? throw new InvalidOperationException(UninitializedMessage);

        /// <summary>
        /// Gets the value, with any quoting removed, its sections joined and any RFC 2231 encoding decoded.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeParameter"/> is not initialised.</exception>
        public string Value => _value ?? throw new InvalidOperationException(UninitializedMessage);

        /// <summary>
        /// Gets the name of the character set an RFC 2231 extended value was written in, when the parameter was parsed.
        /// </summary>
        /// <returns>The character set name, as written; an empty string if the parameter was not parsed from an extended value, or the
        /// extended value gave no character set.</returns>
        /// <remarks>
        /// The value is already decoded from the character set. A parameter is always written in UTF-8, whatever its character set was.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="MimeParameter"/> is not initialised.</exception>
        public string Charset => _name is null ? throw new InvalidOperationException(UninitializedMessage) : _charset ?? string.Empty;

        // TODO: Revisit the type of Language, and use Bcp47LanguageTag, if the MIME implementation is revised to a standard that calls for
        // BCP 47 language tags.
        /// <summary>
        /// Gets the language of the value, as given by an RFC 2231 extended value.
        /// </summary>
        /// <returns>The RFC 1766 §2 language tag, as written; an empty string if none was given.</returns>
        /// <remarks>
        /// RFC 2231 §4 calls for RFC 1766 language tags, not BCP 47 language tags, so the language is kept as a string.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The <see cref="MimeParameter"/> is not initialised.</exception>
        public string Language => _name is null ? throw new InvalidOperationException(UninitializedMessage) : _language ?? string.Empty;

        internal bool IsInitialized => _name != null;

#if NETCOREAPP3_0_OR_GREATER
        internal static MimeParameter CreateUnchecked(string name, string value, string? charset, string? language)
#else
        internal static MimeParameter CreateUnchecked(string name, string value, string charset, string language)
#endif
        {
            return new MimeParameter(name, value, charset, language, true);
        }

        /// <summary>
        /// Determine if this parameter has a name, compared case-insensitively.
        /// </summary>
        internal bool HasName(string name)
        {
            return string.Equals(_name, name, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the number of characters the parameter takes when written.
        /// </summary>
        internal int GetFormattedLength()
        {
            if (!IsExtended) return _name.Length + 1 + MimeGrammar.GetFormattedValueLength(_value);

            return _name.Length + 2 + GetCharsetAndLanguageLength() + MimeExtendedParameter.GetEncodedLength(_value);
        }

        // ref. RFC 2231 §4: a value that is not US-ASCII, or that has a language, can only be written as an extended value.
        private bool IsExtended => _language != null || !MimeGrammar.IsValue(_value);

        /// <summary>
        /// Formats the parameter into a span, as <c>name=value</c>, or as <c>name*=UTF-8'language'value</c> for an RFC 2231 extended
        /// value.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <returns><c>true</c> if the parameter was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="MimeParameter"/> is not initialised.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            if (_name is null) throw new InvalidOperationException(UninitializedMessage);

            var position = 0;
            if (!TryWrite(destination, ref position))
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = position;
            return true;
        }

        internal bool TryWrite(Span<char> destination, ref int position)
        {
            if (!IsExtended)
            {
                return MimeGrammar.TryWrite(_name, destination, ref position) &&
                       MimeGrammar.TryWrite('=', destination, ref position) &&
                       MimeGrammar.TryWriteValue(_value, destination, ref position);
            }

            if (GetFormattedLength() > destination.Length - position) return false;

            MimeGrammar.TryWrite(_name, destination, ref position);
            MimeGrammar.TryWrite("*=", destination, ref position);
            WriteCharsetAndLanguage(destination, ref position);
            for (var index = 0; index < _value.Length;)
            {
                index += MimeExtendedParameter.WriteEncodedUnit(_value, index, destination, ref position);
            }

            return true;
        }

        /// <summary>
        /// Formats the parameter as <c>name=value</c>, or as <c>name*=UTF-8'language'value</c> for an RFC 2231 extended value.
        /// </summary>
        /// <returns>The parameter; an empty string if the <see cref="MimeParameter"/> is not initialised.</returns>
        public override string ToString()
        {
            if (_name is null) return string.Empty;

            var buffer = new char[GetFormattedLength()];
            var position = 0;
            TryWrite(buffer, ref position);
            return new string(buffer, 0, position);
        }

        /// <summary>
        /// Formats the parameter as RFC 2231 sections of at most a given length, such as to keep the lines of a header field within 78
        /// characters.
        /// </summary>
        /// <param name="maxSectionLength">The most characters each section may take.</param>
        /// <returns>The parameter as <see cref="ToString"/> writes it, if it fits in <paramref name="maxSectionLength"/>. Otherwise, its
        /// sections, such as <c>title*0=...</c> and <c>title*1=...</c>, or <c>title*0*=UTF-8'en'...</c> and <c>title*1*=...</c> for an
        /// extended value, in order.</returns>
        /// <remarks>
        /// <para>
        /// Write the sections as separate parameters, each after a <c>;</c>, and fold the header field between them. The length of a section
        /// doesn't count the <c>;</c> or the whitespace before it.
        /// </para>
        /// <para>
        /// A character, and the octets of its encoding, are never split between sections. A section is longer than
        /// <paramref name="maxSectionLength"/> only when its name and a single character of the value don't fit in it. An empty value
        /// that doesn't fit is written as one section, <c>name*0=""</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSectionLength"/> is less than 1.</exception>
        /// <exception cref="InvalidOperationException">The <see cref="MimeParameter"/> is not initialised.</exception>
        public IReadOnlyList<string> FormatSections(int maxSectionLength)
        {
            if (_name is null) throw new InvalidOperationException(UninitializedMessage);
            if (maxSectionLength < 1) throw new ArgumentOutOfRangeException(nameof(maxSectionLength), maxSectionLength, "The length must be at least 1.");

            var sections = new List<string>();
            if (GetFormattedLength() <= maxSectionLength)
            {
                sections.Add(ToString());
            }
            else if (IsExtended)
            {
                AddExtendedSections(sections, maxSectionLength);
            }
            else
            {
                AddSections(sections, maxSectionLength);
            }

            return new ReadOnlyCollection<string>(sections);
        }

        /// <summary>
        /// Indicates whether this parameter equals another.
        /// </summary>
        /// <remarks>
        /// Names are compared case-insensitively. Values are compared case-sensitively, except for the value of <c>charset</c>. Languages are
        /// compared case-insensitively. The character set is not compared.
        /// </remarks>
        public bool Equals(MimeParameter other)
        {
            return string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(_value, other._value, GetValueComparison()) &&
                   string.Equals(_language, other._language, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is MimeParameter other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // Must agree with Equals, which compares names, languages and some values case-insensitively.
            if (_name is null) return 0;

            var valueComparer = GetValueComparison() == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            unchecked
            {
                var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(_name) * 31 + valueComparer.GetHashCode(_value);
                return _language is null ? hash : hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(_language);
            }
        }

        /// <summary>
        /// Indicates whether two parameters are equal.
        /// </summary>
        public static bool operator ==(MimeParameter left, MimeParameter right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two parameters differ.
        /// </summary>
        public static bool operator !=(MimeParameter left, MimeParameter right)
        {
            return !left.Equals(right);
        }

        private StringComparison GetValueComparison()
        {
            // ref. RFC 2046 §4.1.2: the values of the charset parameter are not case sensitive.
            return HasName(CharsetParameterName) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        }

        private int GetCharsetAndLanguageLength()
        {
            return MimeCharset.Utf8.Name.Length + 2 + (_language?.Length ?? 0);
        }

        private void WriteCharsetAndLanguage(Span<char> destination, ref int position)
        {
            // ref. RFC 2231 §4: charset'language'
            MimeGrammar.TryWrite(MimeCharset.Utf8.Name, destination, ref position);
            MimeGrammar.TryWrite('\'', destination, ref position);
            if (_language != null) MimeGrammar.TryWrite(_language, destination, ref position);
            MimeGrammar.TryWrite('\'', destination, ref position);
        }

        private void AddExtendedSections(List<string> sections, int maxSectionLength)
        {
            // ref. RFC 2231 §4.1: name*0*=charset'language'value, name*1*=value, ...
            // A section holds its name, and then units while they fit, or one unit if none does, so the buffer needs room for the longest
            // name and one more unit than fits.
            var buffer = new char[GetSectionName(int.MaxValue, true).Length + GetCharsetAndLanguageLength() + maxSectionLength + 12];
            var index = 0;
            for (var section = 0; section == 0 || index < _value.Length; section++)
            {
                var position = 0;
                MimeGrammar.TryWrite(GetSectionName(section, true), buffer, ref position);
                if (section == 0) WriteCharsetAndLanguage(buffer, ref position);

                var isFirstUnit = true;
                while (index < _value.Length)
                {
                    int charCount;
                    var unitLength = MimeExtendedParameter.GetEncodedUnitLength(_value, index, out charCount);
                    if (!isFirstUnit && position + unitLength > maxSectionLength) break;

                    index += MimeExtendedParameter.WriteEncodedUnit(_value, index, buffer, ref position);
                    isFirstUnit = false;
                }

                sections.Add(new string(buffer, 0, position));
            }
        }

        private void AddSections(List<string> sections, int maxSectionLength)
        {
            // ref. RFC 2231 §3: name*0=value, name*1=value, ..., each value a token or a quoted-string.
            var index = 0;
            for (var section = 0; section == 0 || index < _value.Length; section++)
            {
                var sectionName = GetSectionName(section, false);

                // Take characters while the value, as a token or as a quoted-string, still fits.
                var start = index;
                var isToken = true;
                var quotedLength = 2;
                while (index < _value.Length)
                {
                    var c = _value[index];
                    var nextIsToken = isToken && MimeGrammar.IsTokenChar(c);
                    var nextQuotedLength = quotedLength + (MimeGrammar.IsQuotedPairChar(c) ? 2 : 1);
                    var nextLength = sectionName.Length + (nextIsToken ? index - start + 1 : nextQuotedLength);
                    if (index > start && nextLength > maxSectionLength) break;

                    isToken = nextIsToken;
                    quotedLength = nextQuotedLength;
                    index++;
                }

                var value = _value.Substring(start, index - start);
                var buffer = new char[sectionName.Length + MimeGrammar.GetFormattedValueLength(value)];
                var position = 0;
                MimeGrammar.TryWrite(sectionName, buffer, ref position);
                MimeGrammar.TryWriteValue(value, buffer, ref position);
                sections.Add(new string(buffer));
            }
        }

        private string GetSectionName(int section, bool extended)
        {
            return _name + "*" + section.ToString(CultureInfo.InvariantCulture) + (extended ? "*=" : "=");
        }

        private static void CheckNameAndValue(string name, string value)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!MimeGrammar.IsRestrictedName(name.AsSpan())) throw new ArgumentException("The parameter name is not valid.", nameof(name));
            if (!IsWellFormed(value)) throw new ArgumentException("The parameter value contains an unpaired surrogate.", nameof(value));
        }

        private static bool IsWellFormed(string value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var c = value[index];
                if (char.IsHighSurrogate(c))
                {
                    if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1])) return false;

                    index++;
                }
                else if (char.IsLowSurrogate(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
