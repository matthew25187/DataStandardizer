using System;
using System.Globalization;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A part that a partial EMLX file stores in another file: an entity with an <c>X-Apple-Content-Length</c> field and an empty body.
    /// </summary>
    public sealed class EmlxMissingPart
    {
        internal const string ContentLengthFieldName = "X-Apple-Content-Length";

        private const string MessageExtension = ".emlx";
        private const string PartExtension = ".emlxpart";
        private const string PartialMessageExtension = ".partial.emlx";
        private const string SinglePartPath = "1";

        internal EmlxMissingPart(ImfEntityNode node)
        {
            Entity = node.Entity;
            EntityPath = node.Path;
            Path = node.Path.Length == 0 ? SinglePartPath : node.Path;

            var field = node.Entity.Fields.Find(ContentLengthFieldName);
            if (!(field is null) && long.TryParse(field.Value.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var length)) ContentLength = length;
        }

        /// <summary>
        /// Gets the entity, whose body is empty.
        /// </summary>
        public ImfEntity Entity { get; }

        /// <summary>
        /// Gets the number of the part, as IMAP numbers it, and as the name of its file does: <c>2</c>, or <c>1.2</c> for the second part of the first part.  The body of a message with no parts is
        /// <c>1</c>.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the value of the <c>X-Apple-Content-Length</c> field, or <c>null</c> if it isn't a number.  Community documentation doesn't say whether this counts the bytes before or after the
        /// transfer encoding, so it is for information only.
        /// </summary>
        public long? ContentLength { get; }

        /// <summary>
        /// Gets the path of the entity, as <see cref="ImfEntityEvent.EntityPath"/> describes.
        /// </summary>
        internal string EntityPath { get; }

        /// <summary>
        /// Gets the name of the file that holds this part, by the convention that open-source readers follow: <c>123.partial.emlx</c> keeps part 2 in <c>123.2.emlxpart</c>.
        /// </summary>
        /// <remarks>
        /// This only works out the name, and any folders in <paramref name="messageFileName"/> stay.  Opening the file is left to the <see cref="EmlxPartResolver"/>.
        /// </remarks>
        /// <param name="messageFileName">The name of the EMLX file.</param>
        /// <returns>The name of the file of the part.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="messageFileName"/> is <c>null</c>.</exception>
        public string GetPartFileName(string messageFileName)
        {
            if (messageFileName is null)
                throw new ArgumentNullException(nameof(messageFileName));

            return RemoveExtension(messageFileName) + "." + Path + PartExtension;
        }

        private static string RemoveExtension(string fileName)
        {
            if (fileName.EndsWith(PartialMessageExtension, StringComparison.OrdinalIgnoreCase)) return fileName.Substring(0, fileName.Length - PartialMessageExtension.Length);
            if (fileName.EndsWith(MessageExtension, StringComparison.OrdinalIgnoreCase)) return fileName.Substring(0, fileName.Length - MessageExtension.Length);

            return fileName;
        }
    }
}
