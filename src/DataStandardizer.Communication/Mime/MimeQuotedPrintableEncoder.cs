using System;
using System.Buffers;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Encodes data in quoted-printable as it arrives, buffer by buffer: the <c>quoted-printable</c> Content-Transfer-Encoding of
    /// RFC 2045 §6.7, or the RFC 2047 §4.2 "Q" encoding of encoded words.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The encoder works like <c>System.Buffers.Text.Base64.EncodeToUtf8</c>, but keeps its state between calls. In
    /// <see cref="MimeQuotedPrintableMode.Body"/> mode, how a byte is written depends on whether a line break follows it, so the encoder
    /// holds the last 1 or 2 bytes of a block that is not final until the next call. It also keeps track of the line length, so the
    /// output is the same however the data is split.
    /// </para>
    /// <para>
    /// In <see cref="MimeQuotedPrintableMode.Body"/> mode, the encoder writes lines of at most 76 characters, with soft line breaks
    /// (<c>=</c> and CRLF) where a line of the data is longer. A space or tab at the end of a line is encoded. Unless
    /// <see cref="MimeQuotedPrintableEncoderOptions.Binary"/> is set, each CRLF or bare LF in the data is written as a CRLF line break,
    /// and a bare CR is encoded. The encoder writes no line break at the end of the data.
    /// </para>
    /// <para>
    /// In the "Q" modes, a space is written as <c>_</c>, and the characters that can't be written as themselves in the mode's context
    /// are encoded. The encoder doesn't limit the length of the output: splitting it into encoded words of at most 75 characters
    /// (RFC 2047 §2) is up to the caller.
    /// </para>
    /// <para>
    /// The encoder writes up to 3 characters at a time, so it needs a destination of at least 3 bytes or characters to make progress.
    /// After a call with <c>isFinalBlock</c> set to <c>true</c> returns <see cref="OperationStatus.Done"/>, the encoder is ready to
    /// encode new data. An instance is not thread-safe.
    /// </para>
    /// </remarks>
    public sealed class MimeQuotedPrintableEncoder
    {
        private MimeQuotedPrintableEncoderState _state;

        /// <summary>
        /// Creates an encoder that encodes data as text.
        /// </summary>
        /// <param name="mode">The form of quoted-printable to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeQuotedPrintableMode"/>
        /// member.</exception>
        public MimeQuotedPrintableEncoder(MimeQuotedPrintableMode mode)
            : this(mode, MimeQuotedPrintableEncoderOptions.None)
        {
        }

        /// <summary>
        /// Creates an encoder.
        /// </summary>
        /// <param name="mode">The form of quoted-printable to write.</param>
        /// <param name="options">How to treat the data.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeQuotedPrintableMode"/> member,
        /// or <paramref name="options"/> is not a combination of <see cref="MimeQuotedPrintableEncoderOptions"/> flags.</exception>
        public MimeQuotedPrintableEncoder(MimeQuotedPrintableMode mode, MimeQuotedPrintableEncoderOptions options)
        {
            MimeQuotedPrintable.CheckMode(mode);
            MimeQuotedPrintable.CheckOptions(options);

            Mode = mode;
            Options = options;
            _state = new MimeQuotedPrintableEncoderState(mode, options);
        }

        /// <summary>
        /// Gets the form of quoted-printable the encoder writes.
        /// </summary>
        public MimeQuotedPrintableMode Mode { get; }

        /// <summary>
        /// Gets how the encoder treats the data.
        /// </summary>
        public MimeQuotedPrintableEncoderOptions Options { get; }

        /// <summary>
        /// Encodes data into UTF-8 (which for quoted-printable is the same as ASCII).
        /// </summary>
        /// <param name="source">The next block of data.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="bytesConsumed">The number of bytes of <paramref name="source"/> consumed, including any that the encoder holds for
        /// the next call.</param>
        /// <param name="bytesWritten">The number of bytes written.</param>
        /// <param name="isFinalBlock"><c>true</c> if <paramref name="source"/> ends the data; <c>false</c> if more data may follow.</param>
        /// <returns><see cref="OperationStatus.Done"/> if all of <paramref name="source"/> was consumed;
        /// <see cref="OperationStatus.DestinationTooSmall"/> if <paramref name="destination"/> is full, in which case call again with the
        /// rest of <paramref name="source"/>.</returns>
        public OperationStatus Encode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            return _state.Encode(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);
        }

        /// <summary>
        /// Encodes data into characters.
        /// </summary>
        /// <param name="source">The next block of data.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="bytesConsumed">The number of bytes of <paramref name="source"/> consumed, including any that the encoder holds for
        /// the next call.</param>
        /// <param name="charsWritten">The number of characters written.</param>
        /// <param name="isFinalBlock"><c>true</c> if <paramref name="source"/> ends the data; <c>false</c> if more data may follow.</param>
        /// <returns><see cref="OperationStatus.Done"/> if all of <paramref name="source"/> was consumed;
        /// <see cref="OperationStatus.DestinationTooSmall"/> if <paramref name="destination"/> is full, in which case call again with the
        /// rest of <paramref name="source"/>.</returns>
        public OperationStatus Encode(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten, bool isFinalBlock)
        {
            return _state.Encode(source, destination, out bytesConsumed, out charsWritten, isFinalBlock);
        }

        /// <summary>
        /// Discards any bytes held and the line length, to start encoding new data.
        /// </summary>
        public void Reset()
        {
            _state.Reset();
        }
    }
}
