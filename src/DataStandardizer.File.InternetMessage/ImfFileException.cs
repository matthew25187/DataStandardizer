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
            /// The path of the entity in which the problem was found, as a <see cref="string"/>, in the form that
            /// <see cref="ImfEntityEvent.EntityPath"/> describes.  Present only for a problem found by an <see cref="ImfEntityReader"/> or
            /// <see cref="ImfEntityWriter"/>.
            /// </summary>
            public const string EntityPath = "EntityPath";

            /// <summary>
            /// The name of the header field, as a <see cref="string"/>.  Present only once the name has been read.
            /// </summary>
            public const string FieldName = "FieldName";

            /// <summary>
            /// The 1-based number of the physical line on which the problem was found, as a <see cref="long"/>.  Present only for a problem found by a reader.
            /// </summary>
            public const string LineNumber = "LineNumber";
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
