using System;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Exception to normal processing of an Internet message.
    /// </summary>
    /// <remarks>
    /// Details of where the problem was found are held in <see cref="Exception.Data"/>, under the keys in <see cref="DataItemName"/>.
    /// </remarks>
    public sealed class ImfFileException : Exception
    {
        /// <summary>
        /// Keys of the items in <see cref="Exception.Data"/>.
        /// </summary>
        public static class DataItemName
        {
            /// <summary>
            /// The 0-based offset, in bytes from the start of the data, at which the problem was found, as a <see cref="long"/>.  Present only for a problem
            /// found by a reader.
            /// </summary>
            public const string BytePosition = "BytePosition";

            /// <summary>
            /// The kind of problem, as an <see cref="ImfDiagnosticKind"/>.  Present only for a problem that an option can turn into an <see cref="ImfDiagnostic"/>, found by an
            /// <see cref="ImfReader"/>, <see cref="ImfWriter"/>, <see cref="MboxReader"/> or <see cref="MboxWriter"/>.
            /// </summary>
            public const string DiagnosticKind = "DiagnosticKind";

            /// <summary>
            /// The path of the entity in which the problem was found, as a <see cref="string"/>, in the form that
            /// <see cref="ImfEntityEvent.EntityPath"/> describes.  Present only for a problem found by an <see cref="ImfEntityReader"/> or
            /// <see cref="ImfEntityWriter"/>, or by the conformance checks of an <see cref="ImfReader"/> or <see cref="ImfWriter"/>.
            /// </summary>
            public const string EntityPath = "EntityPath";

            /// <summary>
            /// The name of the header field, as a <see cref="string"/>, which for a required field that is missing is the name it should have.  Otherwise present only once
            /// the name has been read.
            /// </summary>
            public const string FieldName = "FieldName";

            /// <summary>
            /// The 1-based number of the physical line on which the problem was found, as a <see cref="long"/>.  Present only for a problem found by a reader.
            /// </summary>
            public const string LineNumber = "LineNumber";

            /// <summary>
            /// The 0-based index of the message in an mbox file, as a <see cref="long"/>.  Present only for a problem found by an <see cref="MboxReader"/> or <see cref="MboxWriter"/>, or by an
            /// <see cref="ImfEntityReader"/> that an <see cref="MboxReader"/> gave.  In a problem with the format of the file, <see cref="BytePosition"/> and <see cref="LineNumber"/> count from
            /// the start of the file.  In a problem in a message, they count from the start of the message as it reads after any quotes have been removed.
            /// </summary>
            public const string MessageIndex = "MessageIndex";
        }

        /// <summary>
        /// Creates an exception with a default message.
        /// </summary>
        public ImfFileException()
        {

        }

        /// <summary>
        /// Creates an exception with a message.
        /// </summary>
        /// <param name="message">The message that describes the problem.</param>
        public ImfFileException(string message) : base(message)
        {

        }

        /// <summary>
        /// Creates an exception with a message and the exception that caused it.
        /// </summary>
        /// <param name="message">The message that describes the problem.</param>
        /// <param name="innerException">The exception that caused this one.</param>
        public ImfFileException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
