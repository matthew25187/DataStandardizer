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

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
        // Decoder's own Convert(ReadOnlySpan<byte>, Span<char>, ...) calls this one, which, like the array one, throws for no bytes.
        public override unsafe void Convert(byte* bytes, int byteCount, char* chars, int charCount, bool flush, out int bytesUsed,
            out int charsUsed, out bool completed)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (chars == null) throw new ArgumentNullException(nameof(chars));
            if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));
            if (charCount < 0) throw new ArgumentOutOfRangeException(nameof(charCount));

            var byteArray = new ReadOnlySpan<byte>(bytes, byteCount).ToArray();

            // No more characters than the bytes give are needed, however long the destination is.
            var charArray = new char[Math.Min(charCount, GetCharCount(byteArray, 0, byteCount, flush))];
            Convert(byteArray, 0, byteCount, charArray, 0, charArray.Length, flush, out bytesUsed, out charsUsed, out completed);
            charArray.AsSpan(0, charsUsed).CopyTo(new Span<char>(chars, charCount));
        }
#endif
    }
}
