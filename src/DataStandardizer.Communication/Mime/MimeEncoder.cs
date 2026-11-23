using System;
using System.Text;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// An encoder of the package's hand-coded encodings, whose
    /// <see cref="Convert(char[], int, int, byte[], int, int, bool, out int, out int, out bool)"/> works as the runtime's encoders do.
    /// </summary>
    /// <remarks>
    /// <see cref="Encoder"/>'s own <c>Convert</c> tries half as many characters each time they don't fit, and never tries none, so it throws
    /// for no characters, where the runtime's encoders convert them.
    /// </remarks>
    internal abstract class MimeEncoder : Encoder
    {
        public override void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount, bool flush,
            out int charsUsed, out int bytesUsed, out bool completed)
        {
            if (chars is null) throw new ArgumentNullException(nameof(chars));
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
            if (charIndex < 0) throw new ArgumentOutOfRangeException(nameof(charIndex));
            if (charCount < 0 || charCount > chars.Length - charIndex) throw new ArgumentOutOfRangeException(nameof(charCount));
            if (byteIndex < 0) throw new ArgumentOutOfRangeException(nameof(byteIndex));
            if (byteCount < 0 || byteCount > bytes.Length - byteIndex) throw new ArgumentOutOfRangeException(nameof(byteCount));

            // Characters that don't all fit are converted up to a point that does, without a flush, as the rest is still to come.
            charsUsed = charCount;
            while (GetByteCount(chars, charIndex, charsUsed, flush) > byteCount)
            {
                if (charsUsed == 0) throw new ArgumentException("The byte array is too short to convert any characters.", nameof(bytes));

                flush = false;
                charsUsed /= 2;
            }

            bytesUsed = GetBytes(chars, charIndex, charsUsed, bytes, byteIndex, flush);
            completed = charsUsed == charCount;
        }
    }
}
