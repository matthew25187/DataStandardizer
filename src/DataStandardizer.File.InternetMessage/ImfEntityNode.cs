namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// An entity that <see cref="ImfMessage.EnumerateEntities"/> found, with where it is in the message.
    /// </summary>
    public sealed class ImfEntityNode
    {
        internal ImfEntityNode(ImfEntity entity, string path, int depth, bool isMessageBody)
        {
            Entity = entity;
            Path = path;
            Depth = depth;
            IsMessageBody = isMessageBody;
        }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        public ImfEntity Entity { get; }

        /// <summary>
        /// Gets the path of the entity, as <see cref="ImfEntityEvent.EntityPath"/> describes.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the depth of the entity.  The body of the message is at depth 0.
        /// </summary>
        public int Depth { get; }

        /// <summary>
        /// Gets a flag indicating that the entity is the body of a message, rather than a part of a multipart entity.
        /// </summary>
        internal bool IsMessageBody { get; }
    }
}
