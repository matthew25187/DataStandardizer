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
            /// The 0-based index of the card in which the problem was found, counting each <c>BEGIN:VCARD</c> line in the data, as an <see cref="int"/>.
            /// Present only when the line is in a card: from a <c>BEGIN:VCARD</c> line up to and including its <c>END:VCARD</c> line.  A syntax error
            /// on what would have been a <c>BEGIN:VCARD</c> line therefore doesn't count as a card.
            /// </summary>
            public const string CardIndex = "CardIndex";

            /// <summary>
            /// The kind of conformance problem.  The content line reader finds no conformance problems, so it never sets this item.
            /// </summary>
            public const string DiagnosticKind = "DiagnosticKind";

            /// <summary>
            /// The group of the content line (RFC 6350 §3.3), without its '.', as a <see cref="string"/>.  Present only when the line has a group
            /// and it has been read.
            /// </summary>
            public const string Group = "Group";

            /// <summary>
            /// The 1-based number of the logical (unfolded) content line, as a <see cref="long"/>.  Not present when the data could not be decoded,
            /// since decoding works on whole blocks, ahead of the lines that have been returned.  For a value that
            /// <see cref="VcardContentLine.GetTextValue"/> cannot read, it is <see cref="VcardContentLine.LineNumber"/>, which is 0 for a line not
            /// read from data.
            /// </summary>
            public const string LineNumber = "LineNumber";

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
