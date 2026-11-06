using System.Text;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// An encoding that decodes and encodes as another one does, with the same fallbacks, but has no preamble.
    /// </summary>
    /// <remarks>
    /// A <see cref="System.IO.StreamReader"/> removes its encoding's preamble from the start of the data.  Reading through this one instead leaves a byte order
    /// mark in the data, as U+FEFF, for <see cref="IcalContentLineReader"/> to accept or reject by <see cref="IcalOptions.AcceptByteOrderMark"/> alone.
    /// </remarks>
    internal sealed class PreamblelessEncoding : Encoding
    {
        private static readonly byte[] EmptyPreamble = new byte[0];

        private readonly Encoding _encoding;

        /// <param name="encoding">The encoding to decode and encode as.</param>
        internal PreamblelessEncoding(Encoding encoding)
        {
            _encoding = encoding;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return _encoding.GetByteCount(chars, index, count);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return _encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return _encoding.GetCharCount(bytes, index, count);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return _encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
        }

        public override Decoder GetDecoder()
        {
            return _encoding.GetDecoder();
        }

        public override Encoder GetEncoder()
        {
            return _encoding.GetEncoder();
        }

        public override int GetMaxByteCount(int charCount)
        {
            return _encoding.GetMaxByteCount(charCount);
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return _encoding.GetMaxCharCount(byteCount);
        }

        public override byte[] GetPreamble()
        {
            return EmptyPreamble;
        }
    }
}
