using System;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Exception to normal processing of vCard data.
    /// </summary>
    /// <remarks>
    /// Details of where the problem was found are held in <see cref="Exception.Data"/>, under the keys in <see cref="DataItemName"/>.
    /// </remarks>
    public sealed class VcardFileException : Exception
    {
        /// <summary>
        /// Keys of the items in <see cref="Exception.Data"/>.
        /// </summary>
        public static class DataItemName
        {
            /// <summary>
            /// The 0-based offset, in bytes from the start of the data, of the JSON token at which the problem was found, as a <see cref="long"/>.  Present only for problems that
            /// <c>JcardReader</c> finds in the structure of the data.  A byte order mark at the start of the data is counted.  For data that is not valid JSON, the offset of the end of the
            /// last token that was read.
            /// </summary>
            public const string BytePosition = "BytePosition";

            /// <summary>
            /// The 0-based index of the card in which the problem was found, counting each <c>BEGIN:VCARD</c> line in the data, as an <see cref="int"/>.
            /// Present only when the line is in a card: from a <c>BEGIN:VCARD</c> line up to and including its <c>END:VCARD</c> line.  A syntax error
            /// on what would have been a <c>BEGIN:VCARD</c> line therefore doesn't count as a card.
            /// </summary>
            public const string CardIndex = "CardIndex";

            /// <summary>
            /// The kind of conformance problem, as a <see cref="VcardDiagnosticKind"/>.  <see cref="VcardReader"/> and <see cref="VcardWriter"/> set it.  The content line reader and
            /// writer find no conformance problems, so they never do.
            /// </summary>
            public const string DiagnosticKind = "DiagnosticKind";

            /// <summary>
            /// The group of the content line (RFC 6350 §3.3), without its '.', as a <see cref="string"/>.  Present only when the line has a group
            /// and it has been read.
            /// </summary>
            public const string Group = "Group";

            /// <summary>
            /// The path of the JSON value at which the problem was found, such as <c>$[1][3][2]</c> or <c>$[1][3][1]['type']</c>, as a <see cref="string"/>.  Present only for problems
            /// that <c>JcardReader</c> finds in the structure of the data once the value has been read; a problem in the JSON syntax itself has <see cref="BytePosition"/> instead.
            /// </summary>
            public const string JsonPath = "JsonPath";

            /// <summary>
            /// The 1-based number of the logical (unfolded) content line, as a <see cref="long"/>.  Not present when the data could not be decoded,
            /// since decoding works on whole blocks, ahead of the lines that have been returned.  For a value that
            /// <see cref="VcardContentLine.GetTextValue"/> cannot read, it is <see cref="VcardContentLine.LineNumber"/>, which is 0 for a line not
            /// read from data.  For data that <c>JcardReader</c> finds is not valid JSON, it is the 1-based number of the line of the JSON document, and for a problem that
            /// <see cref="XcardReader"/> finds, the 1-based line of the XML document.
            /// </summary>
            public const string LineNumber = "LineNumber";

            /// <summary>
            /// The 1-based position in bytes on the line of the JSON document at which the problem was found, as a <see cref="long"/>.  Present only for data that <c>JcardReader</c>
            /// finds is not valid JSON, and for a problem that <see cref="XcardReader"/> finds, where it is the 1-based position on the line of the XML document.
            /// </summary>
            public const string LinePosition = "LinePosition";

            /// <summary>
            /// The 1-based number of the physical line on which the problem was found, as a <see cref="long"/>.  Not present when the data could not
            /// be decoded.  For a value that <see cref="VcardContentLine.GetTextValue"/> cannot read, it is 0 for a line not read from data.  For a value that
            /// <see cref="VcardContentLine.GetDecodedValue"/> or <see cref="VcardContentLine.GetDecodedText()"/> cannot decode, it is the line on which the content line starts.
            /// </summary>
            public const string PhysicalLineNumber = "PhysicalLineNumber";

            /// <summary>
            /// The name of the property on the content line, as a <see cref="string"/>.  Present only once the name has been read.
            /// </summary>
            public const string PropertyName = "PropertyName";
        }

        /// <summary>
        /// Creates an exception with a default message.
        /// </summary>
        public VcardFileException()
        {

        }

        /// <summary>
        /// Creates an exception with a message.
        /// </summary>
        /// <param name="message">The message that describes the problem.</param>
        public VcardFileException(string message) : base(message)
        {

        }

        /// <summary>
        /// Creates an exception with a message and the exception that caused it.
        /// </summary>
        /// <param name="message">The message that describes the problem.</param>
        /// <param name="innerException">The exception that caused this one.</param>
        public VcardFileException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
