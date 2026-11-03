using System;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A view of the event an <see cref="ImfEntityReader"/> has just read, without allocation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A view is valid only until the reader reads again or is disposed.  After that, every member throws: <see cref="ObjectDisposedException"/>
    /// once the reader is disposed, otherwise <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// A span or memory taken out of the view is valid for the same period, but that cannot be checked: using one later gives undefined content.
    /// Copy anything that must outlive the event.
    /// </para>
    /// </remarks>
    public readonly struct ImfEntityEvent
    {
        private readonly ImfEntityReader _reader;
        private readonly int _version;

        internal ImfEntityEvent(ImfEntityReader reader, int version)
        {
            _reader = reader;
            _version = version;
        }

        /// <summary>
        /// Gets the kind of event.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another event, or the view was not obtained from a
        /// reader.</exception>
        public ImfEntityEventKind Kind => GetScanner().EventKind;

        /// <summary>
        /// Gets the path of the entity the event belongs to, as IMAP numbers the parts of a message (RFC 3501 §6.4.5).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The message itself has the empty path.  The parts of a multipart entity are numbered from 1, under the path of that entity, so the second
        /// part of the first part of a message is <c>1.2</c>.
        /// </para>
        /// <para>
        /// IMAP gives a <c>message/rfc822</c> part and the message inside it the one number.  So they have the same path here, and differ in
        /// <see cref="Depth"/>.  Likewise, IMAP numbers the body of a message that is not multipart as part 1 of the message, which matters only when
        /// that body is itself an embedded message: the embedded message of a top-level <c>message/rfc822</c> body has the path <c>1</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another event, or the view was not obtained from a
        /// reader.</exception>
        public string EntityPath => GetScanner().EntityPath;

        /// <summary>
        /// Gets the depth of the entity the event belongs to.  The message itself is at depth 0.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another event, or the view was not obtained from a
        /// reader.</exception>
        public int Depth => GetScanner().Depth;

        /// <summary>
        /// Gets the 0-based offset, in bytes from where the reader started, of the first raw byte that the event covers.
        /// </summary>
        /// <remarks>
        /// <see cref="ImfEntityEventKind.EntityStart"/>, <see cref="ImfEntityEventKind.HeaderEnd"/> and <see cref="ImfEntityEventKind.EntityEnd"/> give
        /// the start of the entity.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another event, or the view was not obtained from a
        /// reader.</exception>
        public long ByteOffset => GetScanner().EventByteOffset;

        /// <summary>
        /// Gets the number of raw bytes that the event covers.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="ImfEntityEventKind.EntityStart"/> covers nothing, as the length of the entity is not yet known.
        /// <see cref="ImfEntityEventKind.HeaderEnd"/> covers the header section, with the empty line that ends it, so the offset plus the length is
        /// where the body starts.  <see cref="ImfEntityEventKind.EntityEnd"/> covers the whole entity, so an entity that has not changed can be copied
        /// byte for byte.
        /// </para>
        /// <para>
        /// A part of a multipart entity does not include the delimiter line after it, or the line break before that line, which belongs to the delimiter
        /// (RFC 2046 §5.1.1).  So if a part has no body, its range stops before the empty line that ended its header section, and is shorter than its
        /// <see cref="ImfEntityEventKind.HeaderEnd"/> event covers.
        /// </para>
        /// <para>
        /// A decoded body chunk covers the raw bytes consumed since the last chunk, which may include bytes the decoder holds for the next chunk, so the
        /// chunks of a body together cover all of its raw bytes.  The last chunk of a body may cover nothing, if it only holds what the decoder was holding,
        /// and may hold no bytes, if it only covers raw bytes that gave no output, such as a line break after the last of the base64.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another event, or the view was not obtained from a
        /// reader.</exception>
        public long ByteLength => GetScanner().EventByteLength;

        /// <summary>
        /// Gets the header field, for a <see cref="ImfEntityEventKind.HeaderField"/> event.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The event is of another kind, or the reader has moved on to another event, or the view was not
        /// obtained from a reader.</exception>
        public ImfHeaderFieldView Field
        {
            get
            {
                GetScanner().CheckIsHeaderField();
                return new ImfHeaderFieldView(_reader, _version);
            }
        }

        /// <summary>
        /// Gets the bytes of a <see cref="ImfEntityEventKind.Preamble"/>, <see cref="ImfEntityEventKind.BodyChunk"/> or
        /// <see cref="ImfEntityEventKind.Epilogue"/> event.  It is empty for an event of any other kind.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another event, or the view was not obtained from a
        /// reader.</exception>
        public ReadOnlySpan<byte> Data => GetScanner().Data.Span;

        /// <inheritdoc cref="Data"/>
        public ReadOnlyMemory<byte> DataMemory => GetScanner().Data;

        /// <summary>
        /// Gets a flag indicating that <see cref="Data"/> has been decoded from the entity's transfer encoding, rather than being the raw bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another event, or the view was not obtained from a
        /// reader.</exception>
        public bool IsDecoded => GetScanner().IsDataDecoded;

        /// <summary>
        /// Gets the media type of the entity, from its Content-Type field or the default (RFC 2045 §5.2, RFC 2046 §5.1.5).
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The event comes before the entity's <see cref="ImfEntityEventKind.HeaderEnd"/> event, or the
        /// reader has moved on to another event, or the view was not obtained from a reader.</exception>
        public MimeMediaType ContentType => GetScanner().ContentType;

        /// <summary>
        /// Gets the transfer encoding of the entity, from its Content-Transfer-Encoding field or the default of <c>7bit</c> (RFC 2045 §6.1).
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The event comes before the entity's <see cref="ImfEntityEventKind.HeaderEnd"/> event, or the
        /// reader has moved on to another event, or the view was not obtained from a reader.</exception>
        public MimeTransferEncoding TransferEncoding => GetScanner().TransferEncoding;

        private ImfEntityScanner GetScanner()
        {
            if (_reader is null)
                throw new InvalidOperationException("The view was not obtained from a reader.");

            return _reader.GetEntityScanner(_version);
        }
    }
}
