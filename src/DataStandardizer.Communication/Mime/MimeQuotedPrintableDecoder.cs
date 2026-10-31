using System;
using System.Buffers;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Decodes quoted-printable data as it arrives, buffer by buffer: the <c>quoted-printable</c> Content-Transfer-Encoding of
    /// RFC 2045 §6.7, or the RFC 2047 §4.2 "Q" encoding of encoded words.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The decoder works like <c>System.Buffers.Text.Base64.DecodeFromUtf8</c>, but keeps its state between calls. It holds an
    /// incomplete escape, soft line break or line break, and a run of spaces and tabs that may turn out to be trailing whitespace, until
    /// the next call. So it consumes all of a block that is not final, and the result is the same however the data is split.
    /// </para>
    /// <para>
    /// In <see cref="MimeQuotedPrintableMode.Body"/> mode, the decoder removes soft line breaks, including whitespace a transport added
    /// after the <c>=</c>, and removes spaces and tabs at the end of a line or of the data (RFC 2045 §6.7 rule 3). A CRLF line break is
    /// decoded as CRLF. As a line is at most 76 characters, a run of more than 76 spaces and tabs is not valid. The decoder doesn't
    /// otherwise check the line length.
    /// </para>
    /// <para>
    /// In the "Q" modes, <c>_</c> is decoded as a space, and only the characters that the mode's context allows can appear as
    /// themselves. Spaces, tabs and line breaks are not valid.
    /// </para>
    /// <para>
    /// In every mode, an <c>=</c> must be followed by two upper-case hexadecimal digits (or in a body by a line break), and control
    /// characters and characters outside ASCII are not valid. <see cref="MimeQuotedPrintableDecoderOptions"/> names the forms that
    /// can be accepted as well.
    /// </para>
    /// <para>
    /// The decoder writes up to 2 bytes at a time, so it needs a destination of at least 2 bytes to make progress. After a call with
    /// <c>isFinalBlock</c> set to <c>true</c> returns <see cref="OperationStatus.Done"/>, the decoder is ready to decode new data. After <see cref="OperationStatus.InvalidData"/>, call <see cref="Reset"/> before decoding new data. An instance
    /// is not thread-safe.
    /// </para>
    /// </remarks>
    public sealed class MimeQuotedPrintableDecoder
    {
        private MimeQuotedPrintableDecoderState _state;

        /// <summary>
        /// Creates a decoder.
        /// </summary>
        /// <param name="mode">The form of quoted-printable to read.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeQuotedPrintableMode"/>
        /// member.</exception>
        public MimeQuotedPrintableDecoder(MimeQuotedPrintableMode mode)
            : this(mode, MimeQuotedPrintableDecoderOptions.None)
        {
        }

        /// <summary>
        /// Creates a decoder.
        /// </summary>
        /// <param name="mode">The form of quoted-printable to read.</param>
        /// <param name="options">The forms to accept beyond those of <paramref name="mode"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeQuotedPrintableMode"/> member,
        /// or <paramref name="options"/> is not a combination of <see cref="MimeQuotedPrintableDecoderOptions"/> flags.</exception>
        public MimeQuotedPrintableDecoder(MimeQuotedPrintableMode mode, MimeQuotedPrintableDecoderOptions options)
        {
            MimeQuotedPrintable.CheckMode(mode);
            MimeQuotedPrintable.CheckOptions(options);

            Mode = mode;
            Options = options;
            _state = new MimeQuotedPrintableDecoderState(mode, options);
        }

        /// <summary>
        /// Gets the form of quoted-printable the decoder reads.
        /// </summary>
        public MimeQuotedPrintableMode Mode { get; }

        /// <summary>
        /// Gets the forms the decoder accepts beyond those of its <see cref="Mode"/>.
        /// </summary>
        public MimeQuotedPrintableDecoderOptions Options { get; }

        /// <summary>
        /// Decodes data from UTF-8 (which for quoted-printable is the same as ASCII).
        /// </summary>
        /// <param name="source">The next block of encoded data.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="bytesConsumed">The number of bytes of <paramref name="source"/> consumed, including any that the decoder holds for
        /// the next call; for <see cref="OperationStatus.InvalidData"/>, the position of the invalid byte, or the length of
        /// <paramref name="source"/> if the data is truncated.</param>
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
        /// <paramref name="source"/> if the data is truncated.</param>
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
        /// Discards anything held from earlier calls, to start decoding new data.
        /// </summary>
        public void Reset()
        {
            _state.Reset();
        }
    }
}
