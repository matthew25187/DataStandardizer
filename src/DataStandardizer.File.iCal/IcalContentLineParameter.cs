using System;
using System.Collections.Generic;
using System.Linq;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A parameter of a content line (RFC 5545 §3.2), with its list of values.
    /// </summary>
    public sealed class IcalContentLineParameter
    {
        /// <summary>
        /// Creates a parameter.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="values">The parameter values, without RFC 6868 caret encoding.  There must be at least one.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="values"/> is empty or contains <c>null</c>.</exception>
        public IcalContentLineParameter(string name, params string[] values) : this(name, (IEnumerable<string>)values)
        {
        }

        /// <summary>
        /// Creates a parameter.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="values">The parameter values, without RFC 6868 caret encoding.  There must be at least one.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="values"/> is empty or contains <c>null</c>.</exception>
        public IcalContentLineParameter(string name, IEnumerable<string> values)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (values is null)
                throw new ArgumentNullException(nameof(values));

            var valueArray = values.ToArray();
            if (valueArray.Length == 0)
                throw new ArgumentException("Expected one or more parameter values.", nameof(values));
            foreach (var value in valueArray)
            {
                if (value is null)
                    throw new ArgumentException("Parameter values cannot be null.", nameof(values));
            }

            Name = name;
            Values = valueArray;
        }

        // The arguments are in the other order so this does not clash with the params constructor.
        private IcalContentLineParameter(string[] values, string name)
        {
            Name = name;
            Values = values;
        }

        /// <summary>
        /// Gets the parameter name, as written.  Parameter names are case-insensitive.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the parameter values, in order, with RFC 6868 caret encoding decoded and any enclosing double quotes removed.
        /// </summary>
        public IReadOnlyList<string> Values { get; }

        /// <summary>
        /// Creates a parameter from values read by <see cref="IcalContentLineReader"/>, taking ownership of the array.
        /// </summary>
        internal static IcalContentLineParameter FromReader(string name, string[] values)
        {
            return new IcalContentLineParameter(values, name);
        }
    }
}
