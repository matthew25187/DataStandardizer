using System;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A view of the header field an <see cref="ImfHeaderReader"/> or <see cref="ImfEntityReader"/> has just read, without allocation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A view is valid only until the reader reads again or is disposed.  After that, every member throws: <see cref="ObjectDisposedException"/>
    /// once the reader is disposed, otherwise <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// A span or memory taken out of the view is valid for the same period, but that cannot be checked: using one later gives undefined content.
    /// Copy anything that must outlive the field.
    /// </para>
    /// <para>
    /// The value of a folded field is unfolded when first asked for and kept until the reader moves on, so asking again costs nothing.  The value
    /// of a field with no folds is a slice of the input itself.
    /// </para>
    /// </remarks>
    public readonly struct ImfHeaderFieldView
    {
        private readonly IImfHeaderFieldSource _source;
        private readonly int _version;

        internal ImfHeaderFieldView(IImfHeaderFieldSource source, int version)
        {
            _source = source;
            _version = version;
        }

        /// <summary>
        /// Gets the 0-based offset, in bytes from the start of the data, of the first byte of the field.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another field, or the view was not obtained from a
        /// reader.</exception>
        public long BytePosition => GetScanner().FieldBytePosition;

        /// <summary>
        /// Gets the 1-based number of the physical line on which the field starts.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another field, or the view was not obtained from a
        /// reader.</exception>
        public long LineNumber => GetScanner().FieldLineNumber;

        /// <summary>
        /// Gets a flag indicating that the field body is not valid UTF-8, so <see cref="GetValue"/> decodes it with
        /// <see cref="ImfOptions.HeaderFallbackCharset"/>.  It can be <c>true</c> only with <see cref="ImfOptions.AllowEightBitHeaders"/>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another field, or the view was not obtained from a
        /// reader.</exception>
        public bool NeedsHeaderFallbackCharset => GetScanner().NeedsFallbackCharset;

        /// <summary>
        /// Gets the whole field exactly as it appears in the data: its name, any white space before the colon, its folds and its final line break.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another field, or the view was not obtained from a
        /// reader.</exception>
        public ReadOnlySpan<byte> RawBytes => GetScanner().RawBytes.Span;

        /// <inheritdoc cref="RawBytes"/>
        public ReadOnlyMemory<byte> RawBytesMemory => GetScanner().RawBytes;

        /// <summary>
        /// Gets the field name, as written, without any white space before the colon.  Field names are case-insensitive.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another field, or the view was not obtained from a
        /// reader.</exception>
        public ReadOnlySpan<byte> Name => GetScanner().Name.Span;

        /// <inheritdoc cref="Name"/>
        public ReadOnlyMemory<byte> NameMemory => GetScanner().Name;

        /// <summary>
        /// Gets the field body, unfolded (RFC 5322 §2.2.3): every byte after the colon, without the line breaks of folds and the final line break.
        /// </summary>
        /// <remarks>
        /// White space is kept, including any after the colon, so the value of <c>Subject: Hi</c> starts with a space.  The syntax of the body, and
        /// the obsolete forms of RFC 5322 §4, are not checked here.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another field, or the view was not obtained from a
        /// reader.</exception>
        public ReadOnlySpan<byte> Value => GetScanner().Value.Span;

        /// <inheritdoc cref="Value"/>
        public ReadOnlyMemory<byte> ValueMemory => GetScanner().Value;

        /// <summary>
        /// Decodes <see cref="Name"/> into a string.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another field, or the view was not obtained from a
        /// reader.</exception>
        public string GetName()
        {
            return GetScanner().GetName();
        }

        /// <summary>
        /// Decodes <see cref="Value"/> into a string: as UTF-8 (RFC 6532), or with <see cref="ImfOptions.HeaderFallbackCharset"/> if
        /// <see cref="NeedsHeaderFallbackCharset"/> is <c>true</c>.  RFC 2047 encoded words are left as they are.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another field, or the view was not obtained from a
        /// reader.</exception>
        public string GetValue()
        {
            return GetScanner().GetValue();
        }

        /// <summary>
        /// Decodes <see cref="Value"/> as the body of an unstructured field, such as Subject or Comments, or a field of unknown meaning: as <see cref="GetValue"/> does,
        /// and then without the white space at either end, and with each RFC 2047 encoded word decoded and the white space between adjacent encoded words removed
        /// (RFC 2047 §6.2).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is what <see cref="ImfHeaderText.DecodeUnstructured(string, ImfOptions)"/> does, with the options of the reader.  A word that can't be decoded stays as
        /// ordinary text, as <see cref="ImfOptions.AllowMalformedEncodedWords"/> describes.
        /// </para>
        /// <para>
        /// The result can hold anything that the words encode, including control characters and line breaks.  A structured field has encoded words only in some places
        /// (RFC 2047 §5), so parse one with its type, such as <see cref="ImfAddressList"/>, and get its comments with <see cref="ImfHeaderText.GetComments(string, ImfOptions)"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on to another field, or the view was not obtained from a
        /// reader.
        /// -or-
        /// The options of the reader have no <see cref="ImfOptions.CharsetResolver"/>.</exception>
        public string GetDecodedValue()
        {
            return GetScanner().GetDecodedValue();
        }

        private ImfHeaderScanner GetScanner()
        {
            if (_source is null)
                throw new InvalidOperationException("The view was not obtained from a reader.");

            return _source.GetScanner(_version);
        }
    }
}
