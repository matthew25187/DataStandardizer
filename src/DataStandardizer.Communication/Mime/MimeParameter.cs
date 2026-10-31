using System;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// A media type parameter (RFC 2045 §5.1): a name and its decoded value.
    /// </summary>
    /// <remarks>
    /// Names are compared case-insensitively. Values are compared case-sensitively, except for the value of <c>charset</c> (RFC 2046 §4.1.2).
    /// A value is written as a token when it is one, and otherwise as a quoted-string.
    /// </remarks>
    public readonly struct MimeParameter : IEquatable<MimeParameter>
    {
        private const string CharsetParameterName = "charset";

        private readonly string _name;
        private readonly string _value;

        /// <summary>
        /// Creates a parameter.
        /// </summary>
        /// <param name="name">The name, which must follow the RFC 6838 §4.2 naming rules.</param>
        /// <param name="value">The value, which may be empty. It must contain only printable US-ASCII characters, spaces and horizontal
        /// tabs.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid parameter name, or <paramref name="value"/> contains a
        /// character that can't be written.</exception>
        public MimeParameter(string name, string value)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!MimeGrammar.IsRestrictedName(name.AsSpan())) throw new ArgumentException("The parameter name is not valid.", nameof(name));
            if (!MimeGrammar.IsValue(value)) throw new ArgumentException("The parameter value contains a character that can't be written.", nameof(value));

            _name = name;
            _value = value;
        }

        // Used by the parser, which has already checked the name and value.
        private MimeParameter(string name, string value, bool _)
        {
            _name = name;
            _value = value;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeParameter"/> is not initialised.</exception>
        public string Name => _name ?? throw new InvalidOperationException($"{nameof(MimeParameter)} is uninitialized.");

        /// <summary>
        /// Gets the value, with any quoting removed.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeParameter"/> is not initialised.</exception>
        public string Value => _value ?? throw new InvalidOperationException($"{nameof(MimeParameter)} is uninitialized.");

        /// <summary>
        /// Gets the character set of the value, as given by an RFC 2231 extended parameter.
        /// </summary>
        /// <returns>The character set name; an empty string if none was given.</returns>
        /// <remarks>
        /// RFC 2231 parameter values are not decoded yet, so this is always empty.
        /// </remarks>
        public string Charset => string.Empty;

        /// <summary>
        /// Gets the language of the value, as given by an RFC 2231 extended parameter.
        /// </summary>
        /// <returns>The language tag; an empty string if none was given.</returns>
        /// <remarks>
        /// RFC 2231 parameter values are not decoded yet, so this is always empty.
        /// </remarks>
        public string Language => string.Empty;

        internal bool IsInitialized => _name != null;

        internal static MimeParameter CreateUnchecked(string name, string value)
        {
            return new MimeParameter(name, value, true);
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
            return _name.Length + 1 + MimeGrammar.GetFormattedValueLength(_value);
        }

        /// <summary>
        /// Formats the parameter into a span, as <c>name=value</c>.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <returns><c>true</c> if the parameter was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="MimeParameter"/> is not initialised.</exception>
        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            if (_name is null) throw new InvalidOperationException($"{nameof(MimeParameter)} is uninitialized.");

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
            return MimeGrammar.TryWrite(_name, destination, ref position) &&
                   MimeGrammar.TryWrite('=', destination, ref position) &&
                   MimeGrammar.TryWriteValue(_value, destination, ref position);
        }

        /// <summary>
        /// Formats the parameter as <c>name=value</c>.
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
        /// Indicates whether this parameter equals another.
        /// </summary>
        /// <remarks>
        /// Names are compared case-insensitively. Values are compared case-sensitively, except for the value of <c>charset</c>.
        /// </remarks>
        public bool Equals(MimeParameter other)
        {
            return string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(_value, other._value, GetValueComparison());
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
            // Must agree with Equals, which compares names, and some values, case-insensitively.
            if (_name is null) return 0;

            var valueComparer = GetValueComparison() == StringComparison.OrdinalIgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            unchecked
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(_name) * 31 + valueComparer.GetHashCode(_value);
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
    }
}
