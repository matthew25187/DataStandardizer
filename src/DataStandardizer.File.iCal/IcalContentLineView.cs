using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A view of the content line an <see cref="IcalContentLineReader"/> has just read, without allocation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Get a view from <see cref="IcalContentLineReader.CurrentLine"/>.  It is valid only until the reader reads another line or is disposed.
    /// After that, every member throws: <see cref="ObjectDisposedException"/> once the reader is disposed, otherwise
    /// <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// A span or memory taken out of the view is valid for the same period, but that cannot be checked: using one later gives undefined
    /// content.  Copy anything that must outlive the line, or use <see cref="ToContentLine"/>.
    /// </para>
    /// <para>
    /// Decoded values (<see cref="GetParameterValue"/> and <see cref="GetTextValue"/>) are decoded when first asked for and kept until the reader
    /// moves on, so asking again costs nothing.  A value with nothing to decode is returned as a slice of the line itself.
    /// </para>
    /// </remarks>
    public readonly struct IcalContentLineView
    {
        private readonly IcalContentLineReader _reader;
        private readonly int _version;

        internal IcalContentLineView(IcalContentLineReader reader, int version)
        {
            _reader = reader;
            _version = version;
        }

        /// <summary>
        /// Gets the 1-based number of the logical (unfolded) line.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public long LineNumber => GetScanner().LineNumber;

        /// <summary>
        /// Gets the 1-based number of the physical line on which the line starts.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public long PhysicalLineNumber => GetScanner().PhysicalLineNumber;

        /// <summary>
        /// Gets the whole logical line, unfolded, without its line break.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public ReadOnlySpan<char> RawLine => GetScanner().RawLine.Span;

        /// <inheritdoc cref="RawLine"/>
        public ReadOnlyMemory<char> RawLineMemory => GetScanner().RawLine;

        /// <summary>
        /// Gets the property name, as written.  Property names are case-insensitive; use <see cref="NameEquals"/> to compare them.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public ReadOnlySpan<char> Name => GetScanner().Name.Span;

        /// <inheritdoc cref="Name"/>
        public ReadOnlyMemory<char> NameMemory => GetScanner().Name;

        /// <summary>
        /// Gets the number of parameters.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public int ParameterCount => GetScanner().ParameterCount;

        /// <summary>
        /// Gets the value, as it appears in the line once unfolded, with any TEXT escaping in place.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public ReadOnlySpan<char> RawValue => GetScanner().RawValue.Span;

        /// <inheritdoc cref="RawValue"/>
        public ReadOnlyMemory<char> RawValueMemory => GetScanner().RawValue;

        /// <summary>
        /// Gets a flag indicating if the property name is a given name, compared case-insensitively.
        /// </summary>
        /// <param name="name">The name to compare with.</param>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public bool NameEquals(ReadOnlySpan<char> name)
        {
            return EqualsIgnoreCase(GetScanner().Name.Span, name);
        }

        /// <summary>
        /// Gets the name of a parameter, as written.
        /// </summary>
        /// <param name="parameterIndex">The 0-based index of the parameter.</param>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="parameterIndex"/> is not a valid index.</exception>
        public ReadOnlySpan<char> GetParameterName(int parameterIndex)
        {
            return GetScanner().GetParameterName(parameterIndex).Span;
        }

        /// <inheritdoc cref="GetParameterName"/>
        public ReadOnlyMemory<char> GetParameterNameMemory(int parameterIndex)
        {
            return GetScanner().GetParameterName(parameterIndex);
        }

        /// <summary>
        /// Gets the index of the first parameter with a name, compared case-insensitively.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <returns>The 0-based index of the parameter, or -1 if there is none with that name.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public int IndexOfParameter(ReadOnlySpan<char> name)
        {
            var scanner = GetScanner();
            for (var parameterIndex = 0; parameterIndex < scanner.ParameterCount; parameterIndex++)
            {
                if (EqualsIgnoreCase(scanner.GetParameterName(parameterIndex).Span, name)) return parameterIndex;
            }

            return -1;
        }

        /// <summary>
        /// Gets the number of values a parameter has.  It is always at least one.
        /// </summary>
        /// <param name="parameterIndex">The 0-based index of the parameter.</param>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="parameterIndex"/> is not a valid index.</exception>
        public int GetParameterValueCount(int parameterIndex)
        {
            return GetScanner().GetParameterValueCount(parameterIndex);
        }

        /// <summary>
        /// Gets a parameter value as written, without any enclosing double quotes and with RFC 6868 caret encoding in place.
        /// </summary>
        /// <param name="parameterIndex">The 0-based index of the parameter.</param>
        /// <param name="valueIndex">The 0-based index of the value.</param>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="parameterIndex"/> or <paramref name="valueIndex"/> is not a valid
        /// index.</exception>
        public ReadOnlySpan<char> GetRawParameterValue(int parameterIndex, int valueIndex)
        {
            return GetScanner().GetRawParameterValue(parameterIndex, valueIndex).Span;
        }

        /// <inheritdoc cref="GetRawParameterValue"/>
        public ReadOnlyMemory<char> GetRawParameterValueMemory(int parameterIndex, int valueIndex)
        {
            return GetScanner().GetRawParameterValue(parameterIndex, valueIndex);
        }

        /// <summary>
        /// Gets a parameter value with RFC 6868 caret encoding decoded: <c>^n</c> becomes a line feed, <c>^'</c> a double quote and <c>^^</c> a
        /// caret.
        /// </summary>
        /// <param name="parameterIndex">The 0-based index of the parameter.</param>
        /// <param name="valueIndex">The 0-based index of the value.</param>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="parameterIndex"/> or <paramref name="valueIndex"/> is not a valid
        /// index.</exception>
        public ReadOnlySpan<char> GetParameterValue(int parameterIndex, int valueIndex)
        {
            return GetScanner().GetParameterValue(parameterIndex, valueIndex).Span;
        }

        /// <inheritdoc cref="GetParameterValue"/>
        public ReadOnlyMemory<char> GetParameterValueMemory(int parameterIndex, int valueIndex)
        {
            return GetScanner().GetParameterValue(parameterIndex, valueIndex);
        }

        /// <summary>
        /// Gets a flag indicating if a parameter value was enclosed in double quotes.
        /// </summary>
        /// <param name="parameterIndex">The 0-based index of the parameter.</param>
        /// <param name="valueIndex">The 0-based index of the value.</param>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="parameterIndex"/> or <paramref name="valueIndex"/> is not a valid
        /// index.</exception>
        public bool IsParameterValueQuoted(int parameterIndex, int valueIndex)
        {
            return GetScanner().IsParameterValueQuoted(parameterIndex, valueIndex);
        }

        /// <summary>
        /// Gets the value with RFC 5545 §3.3.11 TEXT escaping removed: <c>\\</c>, <c>\;</c> and <c>\,</c> become the escaped character, and
        /// <c>\n</c> or <c>\N</c> a line feed.
        /// </summary>
        /// <remarks>
        /// Use this only for properties whose value type is TEXT.  A list of TEXT values must be split on unescaped commas before unescaping, so
        /// for a multi-valued property use <see cref="RawValue"/> instead.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        /// <exception cref="IcalFileException">The value contains a backslash that does not start a valid escape sequence.</exception>
        public ReadOnlySpan<char> GetTextValue()
        {
            return GetScanner().GetTextValue().Span;
        }

        /// <inheritdoc cref="GetTextValue"/>
        public ReadOnlyMemory<char> GetTextValueMemory()
        {
            return GetScanner().GetTextValue();
        }

        /// <summary>
        /// Copies the line into an <see cref="IcalContentLine"/>, which stays valid after the reader moves on.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public IcalContentLine ToContentLine()
        {
            return IcalContentLine.FromScanner(GetScanner());
        }

        private IcalContentLineScanner GetScanner()
        {
            if (_reader is null)
                throw new InvalidOperationException("The view was not obtained from a reader.");

            return _reader.GetScanner(_version);
        }

        private static bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
        {
            if (left.Length != right.Length) return false;

            // Names are ASCII, so only ASCII letters need folding; anything else must match exactly.
            for (var index = 0; index < left.Length; index++)
            {
                var leftCharacter = left[index];
                var rightCharacter = right[index];
                if (leftCharacter == rightCharacter) continue;
                if ((leftCharacter | 0x20) != (rightCharacter | 0x20) || (leftCharacter | 0x20) < 'a' || (leftCharacter | 0x20) > 'z') return false;
            }

            return true;
        }
    }
}
