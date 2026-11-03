namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The kinds of event that an <see cref="ImfEntityReader"/> gives.
    /// </summary>
    public enum ImfEntityEventKind
    {
        /// <summary>
        /// An entity starts.  Its header fields come next.
        /// </summary>
        EntityStart = 0,

        /// <summary>
        /// A field of the entity's header section.
        /// </summary>
        HeaderField,

        /// <summary>
        /// The entity's header section has ended, so its media type and transfer encoding are known.
        /// </summary>
        HeaderEnd,

        /// <summary>
        /// Raw bytes of the preamble of a multipart entity (RFC 2046 §5.1.1).  A preamble may take several events.
        /// </summary>
        Preamble,

        /// <summary>
        /// Bytes of the body of an entity that is neither a multipart entity nor an embedded message.  A body may take several events.
        /// </summary>
        BodyChunk,

        /// <summary>
        /// Raw bytes of the epilogue of a multipart entity (RFC 2046 §5.1.1).  An epilogue may take several events.
        /// </summary>
        Epilogue,

        /// <summary>
        /// An entity ends.  The event gives the whole range of raw bytes of the entity.
        /// </summary>
        EntityEnd,
    }
}
