using System;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Exception to normal processing of iCalendar data.
    /// </summary>
    /// <remarks>
    /// Details of where the problem was found are held in <see cref="Exception.Data"/>, under the keys in <see cref="DataItemName"/>.
    /// </remarks>
    public sealed class IcalFileException : Exception
    {
        /// <summary>
        /// Keys of the items in <see cref="Exception.Data"/>.
        /// </summary>
        public static class DataItemName
        {
            /// <summary>
            /// The path of the component in which the problem was found, such as <c>VCALENDAR/VEVENT[3]</c>, as a <see cref="string"/>.  Present
            /// only for problems found by a reader or writer of the component object model, such as <see cref="IcalReader"/> or
            /// <see cref="XcalWriter"/>.  See <see cref="IcalDiagnostic.ComponentPath"/>.
            /// </summary>
            public const string ComponentPath = "ComponentPath";

            /// <summary>
            /// The kind of conformance problem, as an <see cref="IcalDiagnosticKind"/>.  Present only for a problem that
            /// <see cref="IcalComponent.Validate"/> also reports.
            /// </summary>
            public const string DiagnosticKind = "DiagnosticKind";

            /// <summary>
            /// The 1-based number of the logical (unfolded) content line, as a <see cref="long"/>.  For a problem found by
            /// <see cref="XcalReader"/>, the 1-based number of the line of the XML document.
            /// </summary>
            public const string LineNumber = "LineNumber";

            /// <summary>
            /// The 1-based position on the line of the XML document at which the problem was found, as a <see cref="long"/>.  Present only for
            /// problems found by <see cref="XcalReader"/>, and only where the position is known.
            /// </summary>
            public const string LinePosition = "LinePosition";

            /// <summary>
            /// The 1-based number of the physical line on which the problem was found, as a <see cref="long"/>.  Not present for xCal.
            /// </summary>
            public const string PhysicalLineNumber = "PhysicalLineNumber";

            /// <summary>
            /// The name of the property on the content line, as a <see cref="string"/>.  Present only once the name has been read.
            /// </summary>
            public const string PropertyName = "PropertyName";
        }

        public IcalFileException()
        {

        }

        public IcalFileException(string message) : base(message)
        {

        }

        public IcalFileException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
