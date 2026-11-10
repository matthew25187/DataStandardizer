namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A problem in a message that does not stop it being read.
    /// </summary>
    public sealed class ImfDiagnostic
    {
        internal ImfDiagnostic(ImfDiagnosticKind kind, string message, long bytePosition, string entityPath)
            : this(kind, message, bytePosition, entityPath, string.Empty, 0)
        {
        }

        internal ImfDiagnostic(ImfDiagnosticKind kind, string message, long bytePosition, string entityPath, string fieldName, long lineNumber)
        {
            Kind = kind;
            Message = message;
            BytePosition = bytePosition;
            EntityPath = entityPath;
            FieldName = fieldName;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Gets the kind of problem.
        /// </summary>
        public ImfDiagnosticKind Kind { get; }

        /// <summary>
        /// Gets a description of the problem.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets the 0-based offset, in bytes from the start of the data, at which the problem was found.  For a required field that is missing, it is the end of the header
        /// section.  It is -1 for a message or field that was not read.
        /// </summary>
        public long BytePosition { get; }

        /// <summary>
        /// Gets the path of the entity in which the problem was found, in the form that <see cref="ImfEntityEvent.EntityPath"/> describes.
        /// </summary>
        public string EntityPath { get; }

        /// <summary>
        /// Gets the name of the header field that the problem is with, which for a required field that is missing is the name it should have.  It is empty if the problem is not
        /// with one field.
        /// </summary>
        public string FieldName { get; }

        /// <summary>
        /// Gets the 1-based number of the physical line at <see cref="BytePosition"/>, or 0 if that is not known.
        /// </summary>
        internal long LineNumber { get; }
    }
}
