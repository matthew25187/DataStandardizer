using System;
using System.Text;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// A decoder of the package's own encodings, whose <see cref="Convert(byte[], int, int, char[], int, int, bool, out int, out int, out bool)"/>
    /// works as the runtime's decoders do.
    /// </summary>
    /// <remarks>
    /// <see cref="Decoder"/>'s own <c>Convert</c> tries half as many bytes each time they don't fit, and never tries none, so it throws for
    /// no bytes, and for bytes that give no characters, where the runtime's decoders convert them.
    /// </remarks>
    internal abstract class MimeDecoder : Decoder
    {
        public override void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush,
            out int bytesUsed, out int charsUsed, out bool completed)
        {
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
            if (chars is null) throw new ArgumentNullException(nameof(chars));
            if (byteIndex < 0) throw new ArgumentOutOfRangeException(nameof(byteIndex));
            if (byteCount < 0 || byteCount > bytes.Length - byteIndex) throw new ArgumentOutOfRangeException(nameof(byteCount));
            if (charIndex < 0) throw new ArgumentOutOfRangeException(nameof(charIndex));
            if (charCount < 0 || charCount > chars.Length - charIndex) throw new ArgumentOutOfRangeException(nameof(charCount));

            // Bytes that don't all fit are converted up to a point that does, without a flush, as the rest is still to come.
            bytesUsed = byteCount;
            while (GetCharCount(bytes, byteIndex, bytesUsed, flush) > charCount)
            {
                if (bytesUsed == 0) throw new ArgumentException("The character array is too short to convert any bytes.", nameof(chars));

                flush = false;
                bytesUsed /= 2;
            }

            charsUsed = GetChars(bytes, byteIndex, bytesUsed, chars, charIndex, flush);
            completed = bytesUsed == byteCount;
        }

#if NETCOREAPP
        public override void Convert(ReadOnlySpan<byte> bytes, Span<char> chars, bool flush, out int bytesUsed, out int charsUsed,
            out bool completed)
        {
            var byteArray = bytes.ToArray();
            var charArray = new char[chars.Length];
            Convert(byteArray, 0, byteArray.Length, charArray, 0, charArray.Length, flush, out bytesUsed, out charsUsed, out completed);
            charArray.AsSpan(0, charsUsed).CopyTo(chars);
        }
#endif
    }
}
