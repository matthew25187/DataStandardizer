using System;
using System.Buffers;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Decodes base64 data as it arrives, buffer by buffer: the <c>base64</c> Content-Transfer-Encoding of RFC 2045 §6.8, or the strict
    /// base64 of RFC 4648 §4.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The decoder works like <c>System.Buffers.Text.Base64.DecodeFromUtf8</c>, but keeps its state between calls. It holds an incomplete
    /// quantum until the next call, so it consumes all of a block that is not final, and the result is the same however the data is split.
    /// </para>
    /// <para>
    /// Padding ends the data: after it, a character of the alphabet or another <c>=</c> is invalid. In <see cref="MimeBase64Mode.Mime"/>
    /// mode, characters outside the alphabet are ignored, before and after the padding, unless
    /// <see cref="MimeBase64DecoderOptions.RejectNonAlphabetCharacters"/> is set. The pad bits of the last quantum are not checked
    /// (RFC 4648 §3.5).
    /// </para>
    /// <para>
    /// After a call with <c>isFinalBlock</c> set to <c>true</c> returns <see cref="OperationStatus.Done"/>, the decoder is ready to
    /// decode new data. After <see cref="OperationStatus.InvalidData"/>, call <see cref="Reset"/> before decoding new data. An instance
    /// is not thread-safe.
    /// </para>
    /// </remarks>
    public sealed class MimeBase64Decoder
    {
        private MimeBase64DecoderState _state;

        /// <summary>
        /// Creates a decoder.
        /// </summary>
        /// <param name="mode">The form of base64 to read.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeBase64Mode"/> member.</exception>
        public MimeBase64Decoder(MimeBase64Mode mode)
            : this(mode, MimeBase64DecoderOptions.None)
        {
        }

        /// <summary>
        /// Creates a decoder.
        /// </summary>
        /// <param name="mode">The form of base64 to read.</param>
        /// <param name="options">Checks to make beyond those of <paramref name="mode"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeBase64Mode"/> member, or
        /// <paramref name="options"/> is not a combination of <see cref="MimeBase64DecoderOptions"/> flags.</exception>
        public MimeBase64Decoder(MimeBase64Mode mode, MimeBase64DecoderOptions options)
        {
            MimeBase64.CheckMode(mode);
            MimeBase64.CheckOptions(options);

            Mode = mode;
            Options = options;
            _state = new MimeBase64DecoderState(mode, options);
        }

        /// <summary>
        /// Gets the form of base64 the decoder reads.
        /// </summary>
        public MimeBase64Mode Mode { get; }

        /// <summary>
        /// Gets the checks the decoder makes beyond those of its <see cref="Mode"/>.
        /// </summary>
        public MimeBase64DecoderOptions Options { get; }

        /// <summary>
        /// Decodes data from UTF-8 (which for base64 is the same as ASCII).
        /// </summary>
        /// <param name="source">The next block of encoded data.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="bytesConsumed">The number of bytes of <paramref name="source"/> consumed, including any that the decoder holds for
        /// the next call; for <see cref="OperationStatus.InvalidData"/>, the position of the invalid byte, or the length of
        /// <paramref name="source"/> if the data ends inside a quantum.</param>
        /// <param name="bytesWritten">The number of bytes written.</param>
        /// <param name="isFinalBlock"><c>true</c> if <paramref name="source"/> ends the data; <c>false</c> if more data may follow.</param>
        /// <returns><see cref="OperationStatus.Done"/> if all of <paramref name="source"/> was consumed;
        /// <see cref="OperationStatus.DestinationTooSmall"/> if <paramref name="destination"/> is full, in which case call again with the
        /// rest of <paramref name="source"/>; <see cref="OperationStatus.InvalidData"/> if the data is not valid.</returns>
        public OperationStatus Decode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            return _state.Decode(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);
        }

        /// <summary>
        /// Decodes data from characters.
        /// </summary>
        /// <param name="source">The next block of encoded data.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsConsumed">The number of characters of <paramref name="source"/> consumed, including any that the decoder
        /// holds for the next call; for <see cref="OperationStatus.InvalidData"/>, the position of the invalid character, or the length of
        /// <paramref name="source"/> if the data ends inside a quantum.</param>
        /// <param name="bytesWritten">The number of bytes written.</param>
        /// <param name="isFinalBlock"><c>true</c> if <paramref name="source"/> ends the data; <c>false</c> if more data may follow.</param>
        /// <returns><see cref="OperationStatus.Done"/> if all of <paramref name="source"/> was consumed;
        /// <see cref="OperationStatus.DestinationTooSmall"/> if <paramref name="destination"/> is full, in which case call again with the
        /// rest of <paramref name="source"/>; <see cref="OperationStatus.InvalidData"/> if the data is not valid.</returns>
        public OperationStatus Decode(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten, bool isFinalBlock)
        {
            return _state.Decode(source, destination, out charsConsumed, out bytesWritten, isFinalBlock);
        }

        /// <summary>
        /// Discards any part of a quantum held, to start decoding new data.
        /// </summary>
        public void Reset()
        {
            _state.Reset();
        }
    }
}
