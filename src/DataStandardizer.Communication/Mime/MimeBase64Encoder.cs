using System;
using System.Buffers;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Encodes data in base64 as it arrives, buffer by buffer: the <c>base64</c> Content-Transfer-Encoding of RFC 2045 §6.8, or the strict
    /// base64 of RFC 4648 §4.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The encoder works like <c>System.Buffers.Text.Base64.EncodeToUtf8</c>, but keeps its state between calls. It holds the last 1 or 2
    /// bytes of a block that is not final until the next call, and in <see cref="MimeBase64Mode.Mime"/> mode it keeps track of the line
    /// length, so the output is the same however the data is split.
    /// </para>
    /// <para>
    /// In <see cref="MimeBase64Mode.Mime"/> mode, the encoder breaks lines after 76 characters with CRLF. It writes no line break at the
    /// end of the data.
    /// </para>
    /// <para>
    /// After a call with <c>isFinalBlock</c> set to <c>true</c> returns <see cref="OperationStatus.Done"/>, the encoder is ready to
    /// encode new data. An instance is not thread-safe.
    /// </para>
    /// </remarks>
    public sealed class MimeBase64Encoder
    {
        private MimeBase64EncoderState _state;

        /// <summary>
        /// Creates an encoder.
        /// </summary>
        /// <param name="mode">The form of base64 to write.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeBase64Mode"/> member.</exception>
        public MimeBase64Encoder(MimeBase64Mode mode)
        {
            MimeBase64.CheckMode(mode);

            Mode = mode;
            _state = new MimeBase64EncoderState(mode);
        }

        /// <summary>
        /// Gets the form of base64 the encoder writes.
        /// </summary>
        public MimeBase64Mode Mode { get; }

        /// <summary>
        /// Encodes data into UTF-8 (which for base64 is the same as ASCII).
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
