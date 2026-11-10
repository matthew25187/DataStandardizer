using System;
using System.Buffers;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Encodes a body in quoted-printable or base64, block by block, as the transfer encoding of an <see cref="ImfEntityWriter"/>.
    /// </summary>
    internal abstract class ImfBodyEncoder
    {
        /// <summary>
        /// Encodes the next block of the body.
        /// </summary>
        internal abstract OperationStatus Encode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock);

        /// <summary>
        /// Discards anything held, to start the body again.
        /// </summary>
        internal abstract void Reset();

        internal sealed class Base64 : ImfBodyEncoder
        {
            private readonly MimeBase64Encoder _encoder = new MimeBase64Encoder(MimeBase64Mode.Mime);

            internal override OperationStatus Encode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
            {
                return _encoder.Encode(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);
            }

            internal override void Reset()
            {
                _encoder.Reset();
            }
        }

        internal sealed class QuotedPrintable : ImfBodyEncoder
        {
            private readonly MimeQuotedPrintableEncoder _encoder;

            internal QuotedPrintable(MimeQuotedPrintableEncoderOptions options)
            {
                _encoder = new MimeQuotedPrintableEncoder(MimeQuotedPrintableMode.Body, options);
            }

            internal override OperationStatus Encode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
            {
                return _encoder.Encode(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);
            }

            internal override void Reset()
            {
                _encoder.Reset();
            }
        }
    }
}
