﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
        private readonly int _valueOffset;

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
            int[]? foldOffsets, int valueOffset, int cardIndex)
#else
        private VcardContentLine(string group, string name, VcardContentLineParameter[] parameters, string value, long lineNumber, long physicalLineNumber,
            int[] foldOffsets, int valueOffset, int cardIndex)
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
        }

        /// <summary>
        /// Gets the group (RFC 6350 §3.3), as written and without its '.'; or <c>null</c> if the line has none.  Groups are case-insensitive.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? Group { get; }
#else
        public string Group { get; }
#endif

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
        /// </remarks>
        /// <exception cref="VcardFileException">The value contains a backslash that does not start a valid escape sequence.</exception>
        public string GetTextValue()
        {
            var value = Value;
            if (value.IndexOf('\\') < 0) return value;

            var unescaped = new char[value.Length];
            int unescapedLength, errorIndex;
            if (!ContentLineEscaping.TryUnescapeText(value.AsSpan(), unescaped, out unescapedLength, out errorIndex))
            {
                var physicalLineNumber = PhysicalLineNumber == 0
                    ? 0
                    : ContentLineScanner.GetPhysicalLineNumber(PhysicalLineNumber, _foldOffsets, _foldOffsets?.Length ?? 0, _valueOffset + errorIndex);
                throw VcardContentLineErrorFactory.BuildInvalidTextEscapeException(LineNumber, physicalLineNumber, Name, Group, _cardIndex);
            }

            return new string(unescaped, 0, unescapedLength);
        }

        /// <summary>
        /// Creates a content line from the reader's current line.
        /// </summary>
        /// <param name="scanner">The scanner holding the line.</param>
        /// <param name="decodeCaretEncoding"><c>true</c> to decode RFC 6868 caret encoding in the parameter values.</param>
        /// <param name="cardIndex">The 0-based index of the card the line is in, or -1 if it is outside a card.</param>
        internal static VcardContentLine FromScanner(ContentLineScanner scanner, bool decodeCaretEncoding, int cardIndex)
        {
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

                parameters[parameterIndex] = VcardContentLineParameter.FromReader(scanner.GetParameterName(parameterIndex).Span.ToString(), values);
            }

            var group = scanner.Group.Span;
            return new VcardContentLine(group.Length == 0 ? null : group.ToString(), scanner.Name.Span.ToString(), parameters, scanner.RawValue.Span.ToString(),
                scanner.LineNumber, scanner.PhysicalLineNumber, scanner.CopyFoldOffsets(), scanner.ValueOffsetInLine, cardIndex);
        }
    }
}
