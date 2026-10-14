using System;
using System.Globalization;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A conformance problem found by <see cref="IcalComponent.Validate"/>.
    /// </summary>
    public sealed class IcalDiagnostic
    {
        /// <summary>
        /// Creates a diagnostic.
        /// </summary>
        /// <param name="kind">The kind of problem.</param>
        /// <param name="message">A description of the problem.</param>
        /// <param name="componentPath">The path of the component the problem was found in.</param>
        /// <param name="propertyName">The name of the property at fault; or <c>null</c> if the problem is not with one property.</param>
        /// <param name="lineNumber">The 1-based number of the logical line the problem was found on; or 0 if the model was not read from a
        /// source.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> or <paramref name="componentPath"/> is <c>null</c>.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public IcalDiagnostic(IcalDiagnosticKind kind, string message, string componentPath, string? propertyName, long lineNumber)
#else
        public IcalDiagnostic(IcalDiagnosticKind kind, string message, string componentPath, string propertyName, long lineNumber)
#endif
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (componentPath is null)
                throw new ArgumentNullException(nameof(componentPath));

            Kind = kind;
            Message = message;
            ComponentPath = componentPath;
            PropertyName = propertyName;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Gets the path of the component the problem was found in, such as <c>VCALENDAR/VEVENT[3]/VALARM[1]</c>.
        /// </summary>
        /// <remarks>
        /// The path starts with the component that was validated, which has no index.  Each component under it has a 1-based index among the
        /// components of the same name in its parent.
        /// </remarks>
        public string ComponentPath { get; }

        /// <summary>
        /// Gets the kind of problem.
        /// </summary>
        public IcalDiagnosticKind Kind { get; }

        /// <summary>
        /// Gets the 1-based number of the logical line the problem was found on: the line of the property at fault, or the <c>BEGIN</c> line of
        /// the component.  0 if the model was not read from a source.
        /// </summary>
        public long LineNumber { get; }

        /// <summary>
        /// Gets a description of the problem.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the name of the property at fault; or <c>null</c> if the problem is not with one property.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public string? PropertyName { get; }
#else
        public string PropertyName { get; }
#endif

        /// <summary>
        /// Gets the problem, with where it was found.
        /// </summary>
        public override string ToString()
        {
            return LineNumber == 0
                ? string.Format(CultureInfo.InvariantCulture, "{0}: {1}", ComponentPath, Message)
                : string.Format(CultureInfo.InvariantCulture, "{0} (line {1}): {2}", ComponentPath, LineNumber, Message);
        }
    }
}
