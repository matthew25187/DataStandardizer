using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A content line (RFC 5545 §3.1): a property name, its parameters and its value.
    /// </summary>
    /// <remarks>
    /// This is the allocating form of a content line, returned by <see cref="IcalContentLineReader.ReadLine"/>.  For access without allocation,
    /// use <see cref="IcalContentLineReader.ReadContentLine"/> and <see cref="IcalContentLineReader.CurrentLine"/>.
    /// </remarks>
    public sealed class IcalContentLine
    {
        private static readonly IcalContentLineParameter[] NoParameters = new IcalContentLineParameter[0];
        private static readonly ReadOnlyCollection<IcalContentLineParameter> NoParametersView = new ReadOnlyCollection<IcalContentLineParameter>(NoParameters);

        private readonly IcalContentLineParameter[] _parameters;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly int[]? _foldOffsets;
        private ReadOnlyCollection<IcalContentLineParameter>? _parametersView;
#else
        private readonly int[] _foldOffsets;
        private ReadOnlyCollection<IcalContentLineParameter> _parametersView;
#endif
        private readonly int _valueOffset;

        /// <summary>
        /// Creates a content line with no parameters.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="value">The value, as it appears in the content line, with any TEXT escaping in place.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        public IcalContentLine(string name, string value) : this(name, NoParameters, value)
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
        public IcalContentLine(string name, IEnumerable<IcalContentLineParameter> parameters, string value)
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

            Name = name;
            _parameters = parameterArray.Length == 0 ? NoParameters : parameterArray;
            Value = value;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalContentLine(string name, IcalContentLineParameter[] parameters, string? rawParameters, string value, long lineNumber,
            long physicalLineNumber, int[]? foldOffsets, int valueOffset)
#else
        private IcalContentLine(string name, IcalContentLineParameter[] parameters, string rawParameters, string value, long lineNumber,
            long physicalLineNumber, int[] foldOffsets, int valueOffset)
#endif
        {
            Name = name;
            _parameters = parameters;
            RawParameters = rawParameters;
            Value = value;
            LineNumber = lineNumber;
            PhysicalLineNumber = physicalLineNumber;
            _foldOffsets = foldOffsets;
            _valueOffset = valueOffset;
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
        public IReadOnlyList<IcalContentLineParameter> Parameters
        {
            get
            {
                if (_parameters.Length == 0) return NoParametersView;

                // Created on first use, so that reading a line does not allocate a view that the caller may never ask for.
                var view = _parametersView;
                if (view is null)
                {
                    view = new ReadOnlyCollection<IcalContentLineParameter>(_parameters);
                    view = Interlocked.CompareExchange(ref _parametersView, view, null) ?? view;
                }

                return view;
            }
        }

        /// <summary>
        /// Gets the parameters, for use within the package without allocating a read-only view.  It must not be changed.
        /// </summary>
        internal IcalContentLineParameter[] ParameterArray => _parameters;

        /// <summary>
        /// Gets the 1-based number of the physical line on which this line starts in its source; or 0 if it was not read from a source.
        /// </summary>
        public long PhysicalLineNumber { get; }

        /// <summary>
        /// Gets the parameters exactly as they appeared in the source, from the first ';' up to the ':' before the value; or <c>null</c> if the
        /// line has no parameters or was not read from a source.  <see cref="IcalContentLineWriter"/> writes these, so that a line read and
        /// written back keeps its quoting and caret encoding.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal string? RawParameters { get; }
#else
        internal string RawParameters { get; }
#endif

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
        public IcalContentLineParameter? GetParameter(string name)
#else
        public IcalContentLineParameter GetParameter(string name)
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
        /// Gets the value with RFC 5545 §3.3.11 TEXT escaping removed: <c>\\</c>, <c>\;</c> and <c>\,</c> become the escaped character, and
        /// <c>\n</c> or <c>\N</c> a line feed.
        /// </summary>
        /// <remarks>
        /// Use this only for properties whose value type is TEXT.  A list of TEXT values must be split on unescaped commas before unescaping, so
        /// for a multi-valued property use <see cref="Value"/> instead.
        /// </remarks>
        /// <exception cref="IcalFileException">The value contains a backslash that does not start a valid escape sequence.</exception>
        public string GetTextValue()
        {
            var value = Value;
            if (value.IndexOf('\\') < 0) return value;

            var unescaped = new char[value.Length];
            int unescapedLength, errorIndex;
            if (!IcalEscaping.TryUnescapeText(value.AsSpan(), unescaped, out unescapedLength, out errorIndex))
            {
                var physicalLineNumber = PhysicalLineNumber == 0
                    ? 0
                    : IcalContentLineScanner.GetPhysicalLineNumber(PhysicalLineNumber, _foldOffsets, _foldOffsets?.Length ?? 0, _valueOffset + errorIndex);
                throw IcalContentLineScanner.BuildInvalidTextEscapeException(LineNumber, physicalLineNumber, Name);
            }

            return new string(unescaped, 0, unescapedLength);
        }

        /// <summary>
        /// Creates a content line converted from another serialization, such as xCal, taking ownership of the parameter array.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="parameters">The parameters, in order.</param>
        /// <param name="value">The value, in the form of a content line.</param>
        /// <param name="lineNumber">The 1-based number of the line in the source on which the property starts; or 0 if not known.</param>
        internal static IcalContentLine FromConversion(string name, IcalContentLineParameter[] parameters, string value, long lineNumber)
        {
            return new IcalContentLine(name, parameters.Length == 0 ? NoParameters : parameters, null, value, lineNumber, 0, null, 0);
        }

        /// <summary>
        /// Creates a content line from the reader's current line.
        /// </summary>
        internal static IcalContentLine FromScanner(IcalContentLineScanner scanner)
        {
            var parameterCount = scanner.ParameterCount;
            var parameters = parameterCount == 0 ? NoParameters : new IcalContentLineParameter[parameterCount];
            for (var parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            {
                var values = new string[scanner.GetParameterValueCount(parameterIndex)];
                for (var valueIndex = 0; valueIndex < values.Length; valueIndex++)
                {
                    values[valueIndex] = scanner.GetParameterValue(parameterIndex, valueIndex).Span.ToString();
                }

                parameters[parameterIndex] = IcalContentLineParameter.FromReader(scanner.GetParameterName(parameterIndex).Span.ToString(), values);
            }

            var name = scanner.Name.Span;
            var valueOffset = scanner.ValueOffsetInLine;

            // The parameters run from the end of the name to the ':' before the value.
            var rawParameters = parameterCount == 0 ? null : scanner.RawLine.Span.Slice(name.Length, valueOffset - 1 - name.Length).ToString();

            return new IcalContentLine(name.ToString(), parameters, rawParameters, scanner.RawValue.Span.ToString(), scanner.LineNumber,
                scanner.PhysicalLineNumber, scanner.CopyFoldOffsets(), valueOffset);
        }
    }
}
