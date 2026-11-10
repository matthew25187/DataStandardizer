using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The content of an EMLX file: one message and its metadata.
    /// </summary>
    public sealed class EmlxEntry
    {
        private static readonly ReadOnlyCollection<ImfDiagnostic> NoDiagnostics = new ReadOnlyCollection<ImfDiagnostic>(new ImfDiagnostic[0]);
        private static readonly ReadOnlyCollection<EmlxMissingPart> NoMissingParts = new ReadOnlyCollection<EmlxMissingPart>(new EmlxMissingPart[0]);

        /// <summary>
        /// Creates an entry.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="metadata">The metadata.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> or <paramref name="metadata"/> is <c>null</c>.</exception>
        public EmlxEntry(ImfMessage message, EmlxMetadata metadata)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (metadata is null)
                throw new ArgumentNullException(nameof(metadata));

            Message = message;
            Metadata = metadata;
            Diagnostics = NoDiagnostics;
            MissingParts = NoMissingParts;
        }

        internal EmlxEntry(ImfMessage message, EmlxMetadata metadata, byte[] byteCountLine, List<ImfDiagnostic> diagnostics, List<EmlxMissingPart> missingParts)
        {
            Message = message;
            Metadata = metadata;
            ByteCountLine = byteCountLine;
            Diagnostics = diagnostics.Count == 0 ? NoDiagnostics : new ReadOnlyCollection<ImfDiagnostic>(diagnostics);
            MissingParts = missingParts.Count == 0 ? NoMissingParts : new ReadOnlyCollection<EmlxMissingPart>(missingParts);
        }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public ImfMessage Message { get; }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        public EmlxMetadata Metadata { get; }

        /// <summary>
        /// Gets the problems found in reading this entry that did not stop it being read.  The list is read-only.  A problem with the layout of the file gives a position in the file, and a problem
        /// in the message gives a position in the message.
        /// </summary>
        public IReadOnlyList<ImfDiagnostic> Diagnostics { get; }

        /// <summary>
        /// Gets the parts that the file stores elsewhere, and whose content wasn't supplied.  The list is read-only.
        /// </summary>
        public IReadOnlyList<EmlxMissingPart> MissingParts { get; }

        /// <summary>
        /// Gets the first line of the file that this was read from, with its line feed, or <c>null</c> if it wasn't read.
        /// </summary>
        [MaybeNull]
        internal byte[] ByteCountLine { get; }
    }
}
