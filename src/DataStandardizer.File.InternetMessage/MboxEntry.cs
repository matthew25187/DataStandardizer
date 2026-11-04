using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// One message of an mbox file, with its separator line.
    /// </summary>
    public sealed class MboxEntry
    {
        private static readonly ReadOnlyCollection<ImfDiagnostic> NoDiagnostics = new ReadOnlyCollection<ImfDiagnostic>(new ImfDiagnostic[0]);

        /// <summary>
        /// Creates an entry.
        /// </summary>
        /// <param name="envelope">The separator line.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="ArgumentNullException"><paramref name="envelope"/> or <paramref name="message"/> is <c>null</c>.</exception>
        public MboxEntry(MboxEnvelope envelope, ImfMessage message)
        {
            if (envelope is null)
                throw new ArgumentNullException(nameof(envelope));
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            Envelope = envelope;
            Message = message;
            Diagnostics = NoDiagnostics;
        }

        internal MboxEntry(MboxEnvelope envelope, ImfMessage message, List<ImfDiagnostic> diagnostics)
        {
            Envelope = envelope;
            Message = message;
            Diagnostics = diagnostics.Count == 0 ? NoDiagnostics : new ReadOnlyCollection<ImfDiagnostic>(diagnostics);
        }

        /// <summary>
        /// Gets the separator line.
        /// </summary>
        public MboxEnvelope Envelope { get; }

        /// <summary>
        /// Gets the message.
        /// </summary>
        public ImfMessage Message { get; }

        /// <summary>
        /// Gets the problems found in reading this entry that did not stop it being read, as the options allow.  The list is read-only.  A problem with the format of the file gives a
        /// position in the file, and a problem in the message gives a position in the message as it reads after any quotes have been removed.
        /// </summary>
        public IReadOnlyList<ImfDiagnostic> Diagnostics { get; }
    }
}
