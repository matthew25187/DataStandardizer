namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A problem in a message that does not stop it being read.
    /// </summary>
    public sealed class ImfDiagnostic
    {
        internal ImfDiagnostic(ImfDiagnosticKind kind, string message, long bytePosition, string entityPath)
        {
            Kind = kind;
            Message = message;
            BytePosition = bytePosition;
            EntityPath = entityPath;
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
        /// Gets the 0-based offset, in bytes from the start of the data, at which the problem was found.
        /// </summary>
        public long BytePosition { get; }

        /// <summary>
        /// Gets the path of the entity in which the problem was found, in the form that <see cref="ImfEntityEvent.EntityPath"/> describes.
        /// </summary>
        public string EntityPath { get; }
    }
}
