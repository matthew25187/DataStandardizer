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
            /// The 0-based offset, in bytes from the start of the data, of the JSON token at which the problem was found, as a
            /// <see cref="long"/>.  Present only for problems found by <c>JcalReader</c>.  A byte order mark at the start of the data is
            /// counted.  For data that is not valid JSON, the offset of the end of the last token that was read.
            /// </summary>
            public const string BytePosition = "BytePosition";

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
            /// The path of the JSON value at which the problem was found, such as <c>$[2][0][1][3]</c> or <c>$[1][3][1]['tzid']</c>, as a
            /// <see cref="string"/>.  Present only for problems found by <c>JcalReader</c> once the value has been read; a problem in the JSON
            /// syntax itself has only <see cref="BytePosition"/>.
            /// </summary>
            public const string JsonPath = "JsonPath";

            /// <summary>
            /// The 1-based number of the logical (unfolded) content line, as a <see cref="long"/>.  For a problem found by
            /// <see cref="XcalReader"/>, the 1-based number of the line of the XML document; and for data that <c>JcalReader</c> finds is not
            /// valid JSON, the 1-based number of the line of the JSON document.
            /// </summary>
            /// <remarks>
            /// For a content line that <see cref="IcalContentLineWriter"/> or <see cref="IcalWriter"/> cannot write, it is the number the line
            /// would have had in the output.  For a conformance problem that a writer of the component object model finds, it is the line
            /// number of the property, from the data it was read from, and the item is left out when that is not known.  For a value that a
            /// typed accessor or <see cref="IcalContentLine.GetTextValue"/> cannot read, it is <see cref="IcalContentLine.LineNumber"/>, which
            /// is 0 for a line not read from data.
            /// </remarks>
            public const string LineNumber = "LineNumber";

            /// <summary>
            /// The 1-based position on the line of the XML document at which the problem was found, as a <see cref="long"/>.  Present only for
            /// problems found by <see cref="XcalReader"/>, and only where the position is known; and for data that <c>JcalReader</c> finds is
            /// not valid JSON, where it is the 1-based position in bytes on the line of the JSON document.
            /// </summary>
            public const string LinePosition = "LinePosition";

            /// <summary>
            /// The 1-based number of the physical line on which the problem was found, as a <see cref="long"/>.
            /// </summary>
            /// <remarks>
            /// Present for problems found while reading <c>.ics</c> data.  It is not present for problems found by <see cref="XcalReader"/> or
            /// <c>JcalReader</c>.  For a value that a typed accessor or <see cref="IcalContentLine.GetTextValue"/> cannot read, it is present
            /// whatever the data was read from, and is 0 for a line that was not read from <c>.ics</c> data, such as one read from xCal or
            /// jCal, or created in code.
            /// </remarks>
            public const string PhysicalLineNumber = "PhysicalLineNumber";

            /// <summary>
            /// The name of the property on the content line, as a <see cref="string"/>.  Present only once the name has been read.
            /// </summary>
            public const string PropertyName = "PropertyName";
        }

        /// <summary>
        /// Creates an exception with a default message.
        /// </summary>
        public IcalFileException()
        {

        }

        /// <summary>
        /// Creates an exception with a message.
        /// </summary>
        /// <param name="message">The message that describes the problem.</param>
        public IcalFileException(string message) : base(message)
        {

        }

        /// <summary>
        /// Creates an exception with a message and the exception that caused it.
        /// </summary>
        /// <param name="message">The message that describes the problem.</param>
        /// <param name="innerException">The exception that caused this one.</param>
        public IcalFileException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
