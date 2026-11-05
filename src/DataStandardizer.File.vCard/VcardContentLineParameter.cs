﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A parameter of a content line (RFC 6350 §5), with its list of values.
    /// </summary>
    public sealed class VcardContentLineParameter
    {
        private readonly string[] _values;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private ReadOnlyCollection<string>? _valuesView;
#else
        private ReadOnlyCollection<string> _valuesView;
#endif

        /// <summary>
        /// Creates a parameter.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="values">The parameter values, without RFC 6868 caret encoding.  There must be at least one.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="values"/> is empty or contains <c>null</c>.</exception>
        public VcardContentLineParameter(string name, params string[] values) : this(name, (IEnumerable<string>)values)
        {
        }

        /// <summary>
        /// Creates a parameter.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="values">The parameter values, without RFC 6868 caret encoding.  There must be at least one.</param>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="values"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="values"/> is empty or contains <c>null</c>.</exception>
        public VcardContentLineParameter(string name, IEnumerable<string> values)
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
            _values = valueArray;
        }

        // The arguments are in the other order so this does not clash with the params constructor.
        private VcardContentLineParameter(string[] values, string name)
        {
            Name = name;
            _values = values;
        }

        /// <summary>
        /// Gets the parameter name, as written.  Parameter names are case-insensitive.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the parameter values, in order, with any enclosing double quotes removed.  On a line read from data, RFC 6868 caret encoding is
        /// decoded where <see cref="VcardContentLineReader"/> says it is, and left as written elsewhere.
        /// </summary>
        /// <remarks>
        /// The list is read-only: it cannot be cast to an array or a writable list and changed.
        /// </remarks>
        public IReadOnlyList<string> Values
        {
            get
            {
                // Created on first use, so that reading a line does not allocate a view for each parameter.
                var view = _valuesView;
                if (view is null)
                {
                    view = new ReadOnlyCollection<string>(_values);
                    view = Interlocked.CompareExchange(ref _valuesView, view, null) ?? view;
                }

                return view;
            }
        }

        /// <summary>
        /// Gets the parameter values, for use within the package without allocating a read-only view.  It must not be changed.
        /// </summary>
        internal string[] ValueArray => _values;

        /// <summary>
        /// Creates a parameter from values read by <see cref="VcardContentLineReader"/>, taking ownership of the array.
        /// </summary>
        internal static VcardContentLineParameter FromReader(string name, string[] values)
        {
            return new VcardContentLineParameter(values, name);
        }
    }
}
