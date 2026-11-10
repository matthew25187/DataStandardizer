﻿using System;
using DataStandardizer.File.ContentLine;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A view of the content line an <see cref="VcardContentLineReader"/> has just read, without allocation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Get a view from <see cref="VcardContentLineReader.CurrentLine"/>.  It is valid only until the reader reads another line or is disposed.
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
    public readonly struct VcardContentLineView
    {
        private readonly VcardContentLineReader _reader;
        private readonly int _version;

        internal VcardContentLineView(VcardContentLineReader reader, int version)
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
        /// Gets the lexical features of vCard 2.1 that the line uses.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public VcardLegacySyntax LegacySyntax
        {
            get
            {
                var scanner = GetScanner();
                var syntax = VcardLegacySyntax.None;
                for (var parameterIndex = 0; parameterIndex < scanner.ParameterCount; parameterIndex++)
                {
                    if (scanner.IsParameterValueless(parameterIndex))
                    {
                        syntax |= VcardLegacySyntax.ValuelessParameter;
                    }

                    syntax |= VcardLegacyTokens.GetParameterSyntax(GetParameterName(parameterIndex), scanner.GetRawParameterValue(parameterIndex, 0).Span);
                }

                return syntax;
            }
        }

        /// <summary>
        /// Gets the group (RFC 6350 §3.3), as written and without its '.'; empty if the line has none.  Groups are case-insensitive; use
        /// <see cref="GroupEquals"/> to compare them.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public ReadOnlySpan<char> Group => GetScanner().Group.Span;

        /// <inheritdoc cref="Group"/>
        public ReadOnlyMemory<char> GroupMemory => GetScanner().Group;

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
        /// Gets a flag indicating if the group is a given group, compared case-insensitively.
        /// </summary>
        /// <param name="group">The group to compare with.</param>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public bool GroupEquals(ReadOnlySpan<char> group)
        {
            return EqualsIgnoreCase(GetScanner().Group.Span, group);
        }

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
        /// Gets the name of a parameter, as written; or, for a parameter with no name, as in <c>TEL;HOME:</c>, the name it belongs to: <c>ENCODING</c>, <c>VALUE</c>,
        /// <c>CHARSET</c> or <c>TYPE</c>.
        /// </summary>
        /// <param name="parameterIndex">The 0-based index of the parameter.</param>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="parameterIndex"/> is not a valid index.</exception>
        public ReadOnlySpan<char> GetParameterName(int parameterIndex)
        {
            return GetParameterNameMemory(parameterIndex).Span;
        }

        /// <inheritdoc cref="GetParameterName"/>
        public ReadOnlyMemory<char> GetParameterNameMemory(int parameterIndex)
        {
            var scanner = GetScanner();
            var name = scanner.GetParameterName(parameterIndex);
            return scanner.IsParameterValueless(parameterIndex) ? VcardLegacyTokens.GetValuelessParameterName(name.Span).AsMemory() : name;
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
                if (EqualsIgnoreCase(GetParameterName(parameterIndex), name)) return parameterIndex;
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
        /// Gets a parameter value, without any enclosing double quotes.  On a line where RFC 6868 caret encoding is decoded, <c>^n</c> becomes a
        /// line feed, <c>^'</c> a double quote and <c>^^</c> a caret; see <see cref="VcardContentLineReader"/> for which lines those are.
        /// </summary>
        /// <param name="parameterIndex">The 0-based index of the parameter.</param>
        /// <param name="valueIndex">The 0-based index of the value.</param>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="parameterIndex"/> or <paramref name="valueIndex"/> is not a valid
        /// index.</exception>
        public ReadOnlySpan<char> GetParameterValue(int parameterIndex, int valueIndex)
        {
            return GetParameterValueMemory(parameterIndex, valueIndex).Span;
        }

        /// <inheritdoc cref="GetParameterValue"/>
        public ReadOnlyMemory<char> GetParameterValueMemory(int parameterIndex, int valueIndex)
        {
            var scanner = GetScanner();
            return _reader.DecodesCaretEncoding ? scanner.GetParameterValue(parameterIndex, valueIndex) : scanner.GetRawParameterValue(parameterIndex, valueIndex);
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
        /// Gets the value with RFC 6350 §3.4 TEXT escaping removed: <c>\\</c>, <c>\;</c> and <c>\,</c> become the escaped character, and
        /// <c>\n</c> or <c>\N</c> a line feed.  Any other backslash is an error.
        /// </summary>
        /// <remarks>
        /// Use this only for properties whose value type is TEXT.  A list of TEXT values must be split on unescaped commas, and a compound value on
        /// unescaped semicolons, before unescaping, so for those use <see cref="RawValue"/> instead.
        /// <para>
        /// On a line of a vCard 2.1 card, the value is returned as it is, since a backslash escapes only a semicolon in a compound value there (vCard 2.1 §2.1.3).
        /// <see cref="VcardOptions.AcceptLegacySyntax"/> accepts any other backslash.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        /// <exception cref="VcardFileException">The value contains a backslash that does not start a valid escape sequence.</exception>
        public ReadOnlySpan<char> GetTextValue()
        {
            return GetTextValueMemory().Span;
        }

        /// <inheritdoc cref="GetTextValue"/>
        public ReadOnlyMemory<char> GetTextValueMemory()
        {
            var scanner = GetScanner();
            return _reader.IsVersion21Line ? scanner.RawValue : scanner.GetTextValue(_reader.ValueDecoder.AcceptsOtherTextEscapes);
        }

        /// <inheritdoc cref="VcardContentLine.GetDecodedValue"/>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public byte[] GetDecodedValue()
        {
            return ToContentLine().GetDecodedValue();
        }

        /// <inheritdoc cref="VcardContentLine.GetDecodedText()"/>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public string GetDecodedText()
        {
            return ToContentLine().GetDecodedText();
        }

        /// <inheritdoc cref="VcardContentLine.GetDecodedText(out bool)"/>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public string GetDecodedText(out bool usedCharsetFallback)
        {
            return ToContentLine().GetDecodedText(out usedCharsetFallback);
        }

        /// <summary>
        /// Copies the line into an <see cref="VcardContentLine"/>, which stays valid after the reader moves on.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another line.</exception>
        public VcardContentLine ToContentLine()
        {
            var scanner = GetScanner();
            return VcardContentLine.FromScanner(scanner, _reader.DecodesCaretEncoding, _reader.CardIndexOfLine, _reader.ValueDecoder, _reader.LineVersion);
        }

        private ContentLineScanner GetScanner()
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
