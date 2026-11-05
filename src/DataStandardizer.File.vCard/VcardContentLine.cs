﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using DataStandardizer.Communication.Mime;
using DataStandardizer.File.ContentLine;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A content line (RFC 6350 §3.3): an optional group, a property name, its parameters and its value.
    /// </summary>
    /// <remarks>
    /// This is the allocating form of a content line, returned by <see cref="VcardContentLineReader.ReadLine"/>.  For access without allocation,
    /// use <see cref="VcardContentLineReader.ReadContentLine"/> and <see cref="VcardContentLineReader.CurrentLine"/>.
    /// </remarks>
    public sealed class VcardContentLine
    {
        private static readonly VcardContentLineParameter[] NoParameters = new VcardContentLineParameter[0];
        private static readonly ReadOnlyCollection<VcardContentLineParameter> NoParametersView = new ReadOnlyCollection<VcardContentLineParameter>(NoParameters);

        private readonly VcardContentLineParameter[] _parameters;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly int[]? _foldOffsets;
        private ReadOnlyCollection<VcardContentLineParameter>? _parametersView;
#else
        private readonly int[] _foldOffsets;
        private ReadOnlyCollection<VcardContentLineParameter> _parametersView;
#endif
        private readonly int _cardIndex = -1;
        private readonly VcardValueDecoder _decoder = VcardValueDecoder.Default;
        private readonly bool _hasValuelessParameter;
        private readonly int _valueOffset;
        private readonly VcardLineVersion _version;

        /// <summary>
        /// Creates a content line with no parameters.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The value, as it appears in the content line, with any TEXT escaping in place.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        public VcardContentLine(string name, string value) : this(null, name, NoParameters, value)
        {
        }

        /// <summary>
        /// Creates a content line.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="parameters">The parameters, in order.</param>
        /// <param name="value">The value, as it appears in the content line, with any TEXT escaping in place.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="parameters"/> or <paramref name="value"/> is
        /// <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="parameters"/> contains <c>null</c>.</exception>
        public VcardContentLine(string name, IEnumerable<VcardContentLineParameter> parameters, string value) : this(null, name, parameters, value)
        {
        }

        /// <summary>
        /// Creates a content line with a group.
        /// </summary>
        /// <param name="group">The group, without its '.'; or <c>null</c> for none.</param>
        /// <param name="name">The property name.</param>
        /// <param name="parameters">The parameters, in order.</param>
        /// <param name="value">The value, as it appears in the content line, with any TEXT escaping in place.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/>, <paramref name="parameters"/> or <paramref name="value"/> is
        /// <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="parameters"/> contains <c>null</c>.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public VcardContentLine(string? group, string name, IEnumerable<VcardContentLineParameter> parameters, string value)
#else
        public VcardContentLine(string group, string name, IEnumerable<VcardContentLineParameter> parameters, string value)
#endif
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (parameters is null)
                throw new ArgumentNullException(nameof(parameters));
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            var parameterArray = parameters.ToArray();
            foreach (var parameter in parameterArray)
            {
                if (parameter is null)
                    throw new ArgumentException("Parameters cannot be null.", nameof(parameters));
            }

            Group = group;
            Name = name;
            _parameters = parameterArray.Length == 0 ? NoParameters : parameterArray;
            Value = value;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardContentLine(string? group, string name, VcardContentLineParameter[] parameters, string value, long lineNumber, long physicalLineNumber,
            int[]? foldOffsets, int valueOffset, int cardIndex, bool hasValuelessParameter, VcardValueDecoder decoder, VcardLineVersion version)
#else
        private VcardContentLine(string group, string name, VcardContentLineParameter[] parameters, string value, long lineNumber, long physicalLineNumber,
            int[] foldOffsets, int valueOffset, int cardIndex, bool hasValuelessParameter, VcardValueDecoder decoder, VcardLineVersion version)
#endif
        {
            Group = group;
            Name = name;
            _parameters = parameters;
            Value = value;
            LineNumber = lineNumber;
            PhysicalLineNumber = physicalLineNumber;
            _foldOffsets = foldOffsets;
            _valueOffset = valueOffset;
            _cardIndex = cardIndex;
            _hasValuelessParameter = hasValuelessParameter;
            _decoder = decoder;
            _version = version;
        }

        /// <summary>
        /// Gets the 0-based index of the card the line is in, or -1 if it is outside a card or was not read from a source.
        /// </summary>
        internal int CardIndex => _cardIndex;

        /// <summary>
        /// Gets the card an <c>AGENT</c> of vCard 2.1 holds, which follows its line in the data (vCard 2.1 §2.5.4); or <c>null</c>.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal VcardCard? HeldCard { get; private set; }
#else
        internal VcardCard HeldCard { get; private set; }
#endif

        /// <summary>
        /// Gets the group (RFC 6350 §3.3), as written and without its '.'; or <c>null</c> if the line has none.  Groups are case-insensitive.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Group { get; }
#else
        public string Group { get; }
#endif

        /// <summary>
        /// Gets the lexical features of vCard 2.1 that the line uses.
        /// </summary>
        public VcardLegacySyntax LegacySyntax
        {
            get
            {
                var syntax = _hasValuelessParameter ? VcardLegacySyntax.ValuelessParameter : VcardLegacySyntax.None;
                foreach (var parameter in _parameters)
                {
                    syntax |= VcardLegacyTokens.GetParameterSyntax(parameter.Name.AsSpan(), parameter.ValueArray[0].AsSpan());
                }

                return syntax;
            }
        }

        /// <summary>
        /// Gets the 1-based number of the logical (unfolded) line this was read from; or 0 if it was not read from a source.
        /// </summary>
        public long LineNumber { get; }

        /// <summary>
        /// Gets the property name, as written.  Property names are case-insensitive.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the parameters, in the order they appear.
        /// </summary>
        /// <remarks>
        /// The list is read-only: it cannot be cast to an array or a writable list and changed.
        /// </remarks>
        public IReadOnlyList<VcardContentLineParameter> Parameters
        {
            get
            {
                if (_parameters.Length == 0) return NoParametersView;

                // Created on first use, so that reading a line does not allocate a view that the caller may never ask for.
                var view = _parametersView;
                if (view is null)
                {
                    view = new ReadOnlyCollection<VcardContentLineParameter>(_parameters);
                    view = Interlocked.CompareExchange(ref _parametersView, view, null) ?? view;
                }

                return view;
            }
        }

        /// <summary>
        /// Gets the parameters, for use within the package without allocating a read-only view.  It must not be changed.
        /// </summary>
        internal VcardContentLineParameter[] ParameterArray => _parameters;

        /// <summary>
        /// Gets the 1-based number of the physical line on which this line starts in its source; or 0 if it was not read from a source.
        /// </summary>
        public long PhysicalLineNumber { get; }

        /// <summary>
        /// Gets the value, as it appears in the content line once unfolded.  TEXT escaping is left in place, because whether it applies depends
        /// on the value type; see <see cref="GetTextValue"/>.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets the octets of the value, decoded by its <c>ENCODING</c> parameter.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <c>QUOTED-PRINTABLE</c> and <c>BASE64</c> (vCard 2.1 §2.1.5) and <c>b</c> (RFC 2426 §4) are decoded.  With no encoding, or with <c>7BIT</c> or <c>8BIT</c>, the
        /// octets are the value in the encoding of the source.
        /// </para>
        /// <para>
        /// A parameter applies only where the line's version defines it: on a line read from a vCard 2.1 card, or read under
        /// <see cref="VcardOptions.AcceptLegacySyntax"/>, or not read from a source at all.  <c>b</c> also applies to any line but one of a vCard 4.0 card.
        /// </para>
        /// </remarks>
        /// <exception cref="VcardFileException">The encoding isn't one of those, or the value isn't valid in it.</exception>
        public byte[] GetDecodedValue()
        {
            return _decoder.DecodeBytes(this, _version);
        }

        /// <summary>
        /// Gets the text of the value, decoded by its <c>ENCODING</c> and <c>CHARSET</c> parameters.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The value is decoded as <see cref="GetDecodedValue"/> says, and the octets by the <c>CHARSET</c>.  With no <c>CHARSET</c>, a value is US-ASCII in a vCard 2.1 card
        /// (vCard 2.1 §2.1.6) and UTF-8 elsewhere (RFC 6350 §3.1), and a value with no encoding to decode is returned as it is in a card of a later version.  TEXT escaping
        /// is left in place; see <see cref="GetTextValue"/>.
        /// </para>
        /// <para>
        /// A character set is resolved as <see cref="VcardOptions.CharsetResolver"/> says, with the options of the reader the line came from.  Which code pages exist depends
        /// on the runtime.
        /// </para>
        /// </remarks>
        /// <exception cref="VcardFileException">The encoding or character set can't be resolved, or the value isn't valid in them.</exception>
        public string GetDecodedText()
        {
            bool usedCharsetFallback;
            return _decoder.DecodeText(this, _version, out usedCharsetFallback);
        }

        /// <summary>
        /// Gets the text of the value, decoded by its <c>ENCODING</c> and <c>CHARSET</c> parameters, and whether a character set that can't be resolved was decoded as
        /// ISO-8859-1 under <see cref="VcardOptions.AllowUnknownCharsetAsLatin1"/>.
        /// </summary>
        /// <param name="usedCharsetFallback"><c>true</c> if the text may not have the characters that were meant.</param>
        /// <remarks>
        /// See <see cref="GetDecodedText()"/>.
        /// </remarks>
        /// <exception cref="VcardFileException">The encoding or character set can't be resolved, or the value isn't valid in them.</exception>
        public string GetDecodedText(out bool usedCharsetFallback)
        {
            return _decoder.DecodeText(this, _version, out usedCharsetFallback);
        }

        /// <summary>
        /// Gets the transfer encoding of the value (RFC 2045 §6.1), from its <c>ENCODING</c> parameter.  <c>b</c> (RFC 2426 §4) gives <c>base64</c>.
        /// </summary>
        /// <param name="encoding">The encoding, if there is one.</param>
        /// <returns><c>true</c> if the line has an encoding with a valid name that applies to it; see <see cref="GetDecodedValue"/>.</returns>
        public bool TryGetTransferEncoding(out MimeTransferEncoding encoding)
        {
            bool isVersion30Base64;
            return _decoder.TryGetTransferEncoding(this, _version, out encoding, out isVersion30Base64) && encoding != default(MimeTransferEncoding);
        }

        /// <summary>
        /// Gets the character set of the value, from its <c>CHARSET</c> parameter.
        /// </summary>
        /// <param name="charset">The character set, if there is one.</param>
        /// <returns><c>true</c> if the line has a character set with a valid name that applies to it; see <see cref="GetDecodedValue"/>.</returns>
        public bool TryGetCharset(out MimeCharset charset)
        {
            return _decoder.TryGetCharset(this, _version, out charset);
        }

        /// <summary>
        /// Gets the first parameter with a name, compared case-insensitively.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <returns>The parameter, or <c>null</c> if there is none with that name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public VcardContentLineParameter? GetParameter(string name)
#else
        public VcardContentLineParameter GetParameter(string name)
#endif
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            foreach (var parameter in _parameters)
            {
                if (string.Equals(parameter.Name, name, StringComparison.OrdinalIgnoreCase)) return parameter;
            }

            return null;
        }

        /// <summary>
        /// Gets the value with RFC 6350 §3.4 TEXT escaping removed: <c>\\</c>, <c>\;</c> and <c>\,</c> become the escaped character, and
        /// <c>\n</c> or <c>\N</c> a line feed.  Any other backslash is an error.
        /// </summary>
        /// <remarks>
        /// Use this only for properties whose value type is TEXT.  A list of TEXT values must be split on unescaped commas, and a compound value on
        /// unescaped semicolons, before unescaping, so for those use <see cref="Value"/> instead.
        /// <para>
        /// On a line read from a vCard 2.1 card, the value is returned as it is, since a backslash escapes only a semicolon in a compound value there (vCard 2.1 §2.1.3).
        /// <see cref="VcardOptions.AcceptLegacySyntax"/> accepts any other backslash.
        /// </para>
        /// </remarks>
        /// <exception cref="VcardFileException">The value contains a backslash that does not start a valid escape sequence.</exception>
        public string GetTextValue()
        {
            var value = Value;
            if (_version == VcardLineVersion.Version21 || value.IndexOf('\\') < 0) return value;

            var unescaped = new char[value.Length];
            int unescapedLength, errorIndex;
            if (!ContentLineEscaping.TryUnescapeText(value.AsSpan(), unescaped, out unescapedLength, out errorIndex, _decoder.AcceptsOtherTextEscapes))
            {
                var physicalLineNumber = PhysicalLineNumber == 0
                    ? 0
                    : ContentLineScanner.GetPhysicalLineNumber(PhysicalLineNumber, _foldOffsets, _foldOffsets?.Length ?? 0, _valueOffset + errorIndex);
                throw VcardContentLineErrorFactory.BuildInvalidTextEscapeException(LineNumber, physicalLineNumber, Name, Group, _cardIndex);
            }

            return new string(unescaped, 0, unescapedLength);
        }

        /// <summary>
        /// Gets a copy of the line, read from the same place, with another name and value.
        /// </summary>
        internal VcardContentLine WithNameAndValue(string name, string value)
        {
            return new VcardContentLine(Group, name, _parameters, value, LineNumber, PhysicalLineNumber, _foldOffsets, _valueOffset, _cardIndex, _hasValuelessParameter, _decoder, _version);
        }

        /// <summary>
        /// Gets a line that a version mapping made from this one.  It is reported at the same place, and its parameters mean what they say, as on a line built in code.
        /// </summary>
        /// <summary>
        /// Gets a copy of the line that holds a card, as an <c>AGENT</c> of vCard 2.1 does.
        /// </summary>
        internal VcardContentLine WithHeldCard(VcardCard card)
        {
            return new VcardContentLine(Group, Name, _parameters, Value, LineNumber, PhysicalLineNumber, _foldOffsets, _valueOffset, _cardIndex, _hasValuelessParameter, _decoder, _version)
            {
                HeldCard = card
            };
        }

        internal VcardContentLine With(string name, VcardContentLineParameter[] parameters, string value)
        {
            return With(Group, name, parameters, value);
        }

        /// <summary>
        /// Gets a line with another group, name, parameters and value, at the place of this one in the data.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal VcardContentLine With(string? group, string name, VcardContentLineParameter[] parameters, string value)
#else
        internal VcardContentLine With(string group, string name, VcardContentLineParameter[] parameters, string value)
#endif
        {
            return new VcardContentLine(group, name, parameters, value, LineNumber, PhysicalLineNumber, null, 0, _cardIndex, false, VcardValueDecoder.Default, VcardLineVersion.NotRead);
        }

        /// <summary>
        /// Creates a line converted from another form of a vCard 4.0 card, such as a property of jCard (RFC 7095).  It has no place in <c>.vcf</c> data, so its line numbers are 0,
        /// and it is in a card of vCard 4.0, whose parameters mean what RFC 6350 says.
        /// </summary>
        /// <param name="group">The group, without its '.'; or <c>null</c> for none.</param>
        /// <param name="name">The property name.</param>
        /// <param name="parameters">The parameters, which the line takes ownership of.</param>
        /// <param name="value">The value, as it appears in a content line.</param>
        /// <param name="cardIndex">The 0-based index of the card the line is in.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static VcardContentLine FromConversion(string? group, string name, VcardContentLineParameter[] parameters, string value, int cardIndex)
#else
        internal static VcardContentLine FromConversion(string group, string name, VcardContentLineParameter[] parameters, string value, int cardIndex)
#endif
        {
            return new VcardContentLine(group, name, parameters.Length == 0 ? NoParameters : parameters, value, 0, 0, null, 0, cardIndex, false, VcardValueDecoder.Default,
                VcardLineVersion.Version40);
        }

        /// <summary>
        /// Creates a content line from the reader's current line.
        /// </summary>
        /// <param name="scanner">The scanner holding the line.</param>
        /// <param name="decodeCaretEncoding"><c>true</c> to decode RFC 6868 caret encoding in the parameter values.</param>
        /// <param name="cardIndex">The 0-based index of the card the line is in, or -1 if it is outside a card.</param>
        /// <param name="decoder">Decodes the line's value.</param>
        /// <param name="version">The version of the line's card.</param>
        internal static VcardContentLine FromScanner(ContentLineScanner scanner, bool decodeCaretEncoding, int cardIndex, VcardValueDecoder decoder, VcardLineVersion version)
        {
            var hasValuelessParameter = false;
            var parameterCount = scanner.ParameterCount;
            var parameters = parameterCount == 0 ? NoParameters : new VcardContentLineParameter[parameterCount];
            for (var parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            {
                var values = new string[scanner.GetParameterValueCount(parameterIndex)];
                for (var valueIndex = 0; valueIndex < values.Length; valueIndex++)
                {
                    var value = decodeCaretEncoding ? scanner.GetParameterValue(parameterIndex, valueIndex) : scanner.GetRawParameterValue(parameterIndex, valueIndex);
                    values[valueIndex] = value.Span.ToString();
                }

                var isValueless = scanner.IsParameterValueless(parameterIndex);
                hasValuelessParameter |= isValueless;
                var name = isValueless
                    ? VcardLegacyTokens.GetValuelessParameterName(values[0].AsSpan())
                    : scanner.GetParameterName(parameterIndex).Span.ToString();
                parameters[parameterIndex] = VcardContentLineParameter.FromReader(name, values);
            }

            var group = scanner.Group.Span;
            return new VcardContentLine(group.Length == 0 ? null : group.ToString(), scanner.Name.Span.ToString(), parameters, scanner.RawValue.Span.ToString(),
                scanner.LineNumber, scanner.PhysicalLineNumber, scanner.CopyFoldOffsets(), scanner.ValueOffsetInLine, cardIndex, hasValuelessParameter, decoder, version);
        }
    }
}
