using System.IO;
using System.Text;

namespace DataStandardizer.Communication.Tests.Mime
{
    /// <summary>
    /// Decodes and encodes through an encoding's decoder and encoder, giving them their input in small blocks, through a
    /// <see cref="StreamReader"/> and a <see cref="StreamWriter"/>.
    /// </summary>
    internal static class EncodingBlocks
    {
        /// <summary>
        /// Decodes bytes, giving the decoder <paramref name="blockSize"/> of them at a time.
        /// </summary>
        /// <remarks>
        /// .NET Framework's <see cref="StreamReader"/> doesn't flush the decoder at the end of the stream, so the bytes must end with a
        /// whole character.
        /// </remarks>
        public static string Decode(Encoding encoding, byte[] bytes, int blockSize)
        {
            using (var reader = new StreamReader(new BlockStream(bytes, blockSize), encoding, false))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Encodes text, giving the encoder one character at a time, and flushing it at the end.
        /// </summary>
        public static byte[] EncodeOneCharacterAtATime(Encoding encoding, string text)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new CharacterAtATimeWriter(new StreamWriter(stream, encoding) { AutoFlush = true }))
                {
                    writer.Write(text);
                }

                return stream.ToArray();
            }
        }
    }
}
