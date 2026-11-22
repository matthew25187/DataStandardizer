using System;
using System.Text;

// The null arrays and fallback buffers that mean "count only" and "no fallbacks" are checked where they are used, and the netstandard
// builds, which this file also compiles for, can't annotate them.
#if NETCOREAPP3_0_OR_GREATER
#nullable disable
#endif

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// <c>ISO-10646-UCS-2</c>: the characters of the Basic Multilingual Plane, each as one 16-bit code unit in network byte order, with no
    /// byte order mark.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The IANA Character Sets registry gives the character set as "the 2-octet Basic Multilingual Plane", which "needs to specify network
    /// byte order", so it is big-endian. UCS-2 has no surrogate pairs: it can't hold a character outside the Basic Multilingual Plane, and
    /// the code points U+D800 to U+DFFF, which UTF-16 uses for surrogates, are not characters (The Unicode Standard §3.8, D71).
    /// </para>
    /// <para>
    /// So a code unit from 0xD800 to 0xDFFF, and the byte of an incomplete code unit at the end, are invalid bytes, and a character outside
    /// the Basic Multilingual Plane, or an unpaired surrogate, is a character the encoding can't hold. They are read and written as U+FFFD,
    /// once for each surrogate of a character outside the Basic Multilingual Plane, as the runtime's replacement fallback does, unless a
    /// clone is given other fallbacks. The netstandard1.1 build has no fallbacks to change.
    /// </para>
    /// </remarks>
    internal sealed class MimeUcs2Encoding : Encoding
    {
        internal static readonly MimeUcs2Encoding Instance = new MimeUcs2Encoding();

        private const string Name = "ISO-10646-UCS-2";
        private const int UnitLength = 2;
        private const char ReplacementCharacter = (char)0xFFFD;

        private static readonly byte[] NoPreamble = new byte[0];

        private MimeUcs2Encoding()
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            : base(0, new EncoderReplacementFallback(ReplacementCharacter.ToString()),
                new DecoderReplacementFallback(ReplacementCharacter.ToString()))
#endif
        {
        }

        public override string WebName => Name;

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        public override string EncodingName => Name;

        // The names for a message body and header, which System.Net.Mime.ContentType and AlternateView use as the charset. The encoding
        // has no code page, which the base class would look them up by.
        public override string BodyName => Name;

        public override string HeaderName => Name;
#endif

        public override byte[] GetPreamble()
        {
            return NoPreamble;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            CheckRange(chars, nameof(chars), index, count);

            char pendingHighSurrogate;
            return Encode('\0', chars, index, count, true, null, 0, CreateEncoderFallbackBuffer(), out pendingHighSurrogate);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            CheckRange(chars, nameof(chars), charIndex, charCount);
            CheckDestination(bytes, nameof(bytes), byteIndex);

            char pendingHighSurrogate;
            return Encode('\0', chars, charIndex, charCount, true, bytes, byteIndex, CreateEncoderFallbackBuffer(), out pendingHighSurrogate);
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            CheckRange(bytes, nameof(bytes), index, count);

            int pendingByteCount;
            return Decode(bytes, index, count, true, null, 0, CreateDecoderFallbackBuffer(), out pendingByteCount);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            CheckRange(bytes, nameof(bytes), byteIndex, byteCount);
            CheckDestination(chars, nameof(chars), charIndex);

            int pendingByteCount;
            return Decode(bytes, byteIndex, byteCount, true, chars, charIndex, CreateDecoderFallbackBuffer(), out pendingByteCount);
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0) throw new ArgumentOutOfRangeException(nameof(charCount));

            // One more character, for a high surrogate held back by an encoder, and each character may fall back to several.
            return (int)Math.Min(int.MaxValue, (charCount + 1L) * MaxFallbackLength(true) * UnitLength);
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));

            // A byte held back by a decoder may finish another code unit, the byte left at a flush falls back, and each code unit may
            // fall back to several characters.
            return (int)Math.Min(int.MaxValue, ((byteCount + 1L) / UnitLength + 1) * MaxFallbackLength(false));
        }

        public override Encoder GetEncoder()
        {
            return new Ucs2Encoder(this);
        }

        public override Decoder GetDecoder()
        {
            return new Ucs2Decoder(this);
        }

        public override bool Equals(object obj)
        {
            // A clone is equal to the encoding it was cloned from while it has the same fallbacks, as the runtime's encodings are.
            return obj is MimeUcs2Encoding
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
                && ((MimeUcs2Encoding)obj).EncoderFallback.Equals(EncoderFallback)
                && ((MimeUcs2Encoding)obj).DecoderFallback.Equals(DecoderFallback)
#endif
                ;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        private int MaxFallbackLength(bool encoder)
        {
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            return Math.Max(1, encoder ? EncoderFallback.MaxCharCount : DecoderFallback.MaxCharCount);
#else
            return 1;
#endif
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
        private EncoderFallbackBuffer CreateEncoderFallbackBuffer()
        {
            return EncoderFallback.CreateFallbackBuffer();
        }

        private DecoderFallbackBuffer CreateDecoderFallbackBuffer()
        {
            return DecoderFallback.CreateFallbackBuffer();
        }
#else
        private object CreateEncoderFallbackBuffer()
        {
            return null;
        }

        private object CreateDecoderFallbackBuffer()
        {
            return null;
        }
#endif

        /// <summary>
        /// Encode characters, or count the bytes they encode to.
        /// </summary>
        /// <param name="leadingHighSurrogate">A high surrogate held back from the previous block; or <c>'\0'</c> if there is none.</param>
        /// <param name="chars">The characters.</param>
        /// <param name="index">The position of the first character.</param>
        /// <param name="count">The number of characters.</param>
        /// <param name="flush">Whether this is the last block, so that a high surrogate at the end is not held back.</param>
        /// <param name="bytes">The array to write to; or <c>null</c> to only count the bytes.</param>
        /// <param name="byteIndex">The position to write the first byte to.</param>
        /// <param name="fallbackBuffer">The fallback for the characters the encoding can't hold; <c>null</c> in the netstandard1.1 build,
        /// which writes U+FFFD.</param>
        /// <param name="pendingHighSurrogate">A high surrogate at the end, held back for the next block; or <c>'\0'</c> if there is none.</param>
        /// <returns>The number of bytes.</returns>
        private static int Encode(char leadingHighSurrogate, char[] chars, int index, int count, bool flush, byte[] bytes, int byteIndex,
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            EncoderFallbackBuffer fallbackBuffer,
#else
            object fallbackBuffer,
#endif
            out char pendingHighSurrogate)
        {
            // A high surrogate is held back, so that it falls back together with the low surrogate after it, as one character outside the
            // Basic Multilingual Plane, whichever block that low surrogate is in.
            var position = byteIndex;
            var highSurrogate = leadingHighSurrogate;
            var end = index + count;
            for (var charIndex = index; charIndex < end; charIndex++)
            {
                var c = chars[charIndex];
                if (highSurrogate != '\0')
                {
                    if (char.IsLowSurrogate(c))
                    {
                        FallBack(highSurrogate, c, charIndex - index - 1, fallbackBuffer, bytes, ref position);
                        highSurrogate = '\0';
                        continue;
                    }

                    FallBack(highSurrogate, '\0', charIndex - index - 1, fallbackBuffer, bytes, ref position);
                    highSurrogate = '\0';
                }

                if (char.IsHighSurrogate(c))
                {
                    highSurrogate = c;
                }
                else if (char.IsLowSurrogate(c))
                {
                    FallBack(c, '\0', charIndex - index, fallbackBuffer, bytes, ref position);
                }
                else
                {
                    WriteUnit(c, bytes, ref position);
                }
            }

            if (highSurrogate != '\0' && flush)
            {
                FallBack(highSurrogate, '\0', count - 1, fallbackBuffer, bytes, ref position);
                highSurrogate = '\0';
            }

            pendingHighSurrogate = highSurrogate;
            return position - byteIndex;
        }

        /// <summary>
        /// Write what a character that the encoding can't hold falls back to.
        /// </summary>
        /// <param name="c">The character, or the high surrogate of a surrogate pair.</param>
        /// <param name="lowSurrogate">The low surrogate of a surrogate pair; or <c>'\0'</c> if <paramref name="c"/> is a character by itself.</param>
        /// <param name="index">The position of the character in the block.</param>
        /// <param name="fallbackBuffer">The fallback; <c>null</c> in the netstandard1.1 build.</param>
        /// <param name="bytes">The array to write to; or <c>null</c> to only count the bytes.</param>
        /// <param name="position">The position to write the next byte to.</param>
        private static void FallBack(char c, char lowSurrogate, int index,
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            EncoderFallbackBuffer fallbackBuffer,
#else
            object fallbackBuffer,
#endif
            byte[] bytes, ref int position)
        {
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            if (lowSurrogate == '\0') fallbackBuffer.Fallback(c, index);
            else fallbackBuffer.Fallback(c, lowSurrogate, index);

            while (fallbackBuffer.Remaining > 0)
            {
                var replacement = fallbackBuffer.GetNextChar();
                if (char.IsSurrogate(replacement))
                {
                    fallbackBuffer.Reset();
                    throw new ArgumentException("The encoder fallback gave a character that ISO-10646-UCS-2 can't hold.");
                }

                WriteUnit(replacement, bytes, ref position);
            }
#else
            // As the runtime's replacement fallback does, a surrogate pair is replaced once for each of its surrogates.
            WriteUnit(ReplacementCharacter, bytes, ref position);
            if (lowSurrogate != '\0') WriteUnit(ReplacementCharacter, bytes, ref position);
#endif
        }

        private static void WriteUnit(char c, byte[] bytes, ref int position)
        {
            if (bytes != null)
            {
                if (position > bytes.Length - UnitLength) throw new ArgumentException("The byte array is too short.", nameof(bytes));

                bytes[position] = (byte)(c >> 8);
                bytes[position + 1] = (byte)c;
            }

            position += UnitLength;
        }

        /// <summary>
        /// Decode bytes, or count the characters they decode to.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="index">The position of the first byte.</param>
        /// <param name="count">The number of bytes.</param>
        /// <param name="flush">Whether this is the last block, so that an incomplete code unit at the end is not held back.</param>
        /// <param name="chars">The array to write to; or <c>null</c> to only count the characters.</param>
        /// <param name="charIndex">The position to write the first character to.</param>
        /// <param name="fallbackBuffer">The fallback for invalid bytes; <c>null</c> in the netstandard1.1 build, which reads U+FFFD.</param>
        /// <param name="pendingByteCount">The number of bytes at the end held back for the next block.</param>
        /// <returns>The number of characters.</returns>
        private static int Decode(byte[] bytes, int index, int count, bool flush, char[] chars, int charIndex,
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            DecoderFallbackBuffer fallbackBuffer,
#else
            object fallbackBuffer,
#endif
            out int pendingByteCount)
        {
            var position = charIndex;
            var unitCount = count / UnitLength;
            for (var unit = 0; unit < unitCount; unit++)
            {
                var unitIndex = index + unit * UnitLength;
                var c = (char)((bytes[unitIndex] << 8) | bytes[unitIndex + 1]);
                if (char.IsSurrogate(c)) FallBack(bytes, unitIndex, UnitLength, unitIndex - index, fallbackBuffer, chars, ref position);
                else WriteChar(c, chars, ref position);
            }

            pendingByteCount = count % UnitLength;
            if (pendingByteCount > 0 && flush)
            {
                FallBack(bytes, index + count - pendingByteCount, pendingByteCount, count - pendingByteCount, fallbackBuffer, chars, ref position);
                pendingByteCount = 0;
            }

            return position - charIndex;
        }

        /// <summary>
        /// Write what some invalid bytes fall back to.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="index">The position of the first invalid byte.</param>
        /// <param name="count">The number of invalid bytes.</param>
        /// <param name="blockIndex">The position of the first invalid byte in the block.</param>
        /// <param name="fallbackBuffer">The fallback; <c>null</c> in the netstandard1.1 build.</param>
        /// <param name="chars">The array to write to; or <c>null</c> to only count the characters.</param>
        /// <param name="position">The position to write the next character to.</param>
        private static void FallBack(byte[] bytes, int index, int count, int blockIndex,
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            DecoderFallbackBuffer fallbackBuffer,
#else
            object fallbackBuffer,
#endif
            char[] chars, ref int position)
        {
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            var invalidBytes = new byte[count];
            Array.Copy(bytes, index, invalidBytes, 0, count);
            fallbackBuffer.Fallback(invalidBytes, blockIndex);
            while (fallbackBuffer.Remaining > 0)
            {
                WriteChar(fallbackBuffer.GetNextChar(), chars, ref position);
            }
#else
            WriteChar(ReplacementCharacter, chars, ref position);
#endif
        }

        private static void WriteChar(char c, char[] chars, ref int position)
        {
            if (chars != null)
            {
                if (position >= chars.Length) throw new ArgumentException("The character array is too short.", nameof(chars));

                chars[position] = c;
            }

            position++;
        }

        private static void CheckRange<T>(T[] array, string name, int index, int count)
        {
            if (array is null) throw new ArgumentNullException(name);
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > array.Length - index) throw new ArgumentOutOfRangeException(nameof(count));
        }

        private static void CheckDestination<T>(T[] array, string name, int index)
        {
            if (array is null) throw new ArgumentNullException(name);
            if (index < 0 || index > array.Length) throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <summary>
        /// Encodes characters that may arrive in several blocks, holding back a high surrogate at the end of a block.
        /// </summary>
        private sealed class Ucs2Encoder : MimeEncoder
        {
            private readonly MimeUcs2Encoding _encoding;
            private char _pendingHighSurrogate;

            internal Ucs2Encoder(MimeUcs2Encoding encoding)
            {
                _encoding = encoding;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
                Fallback = encoding.EncoderFallback;
#endif
            }

            public override int GetByteCount(char[] chars, int index, int count, bool flush)
            {
                CheckRange(chars, nameof(chars), index, count);

                char pendingHighSurrogate;
                return Encode(_pendingHighSurrogate, chars, index, count, flush, null, 0, CreateFallbackBuffer(), out pendingHighSurrogate);
            }

            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
            {
                CheckRange(chars, nameof(chars), charIndex, charCount);
                CheckDestination(bytes, nameof(bytes), byteIndex);

                // The state changes only once the characters are encoded, so a call that throws can be made again.
                char pendingHighSurrogate;
                var byteCount = Encode(_pendingHighSurrogate, chars, charIndex, charCount, flush, bytes, byteIndex, CreateFallbackBuffer(),
                    out pendingHighSurrogate);
                _pendingHighSurrogate = pendingHighSurrogate;
                return byteCount;
            }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            public override void Reset()
            {
                _pendingHighSurrogate = '\0';
            }
#endif

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            private EncoderFallbackBuffer CreateFallbackBuffer()
            {
                return (Fallback ?? _encoding.EncoderFallback).CreateFallbackBuffer();
            }
#else
            private object CreateFallbackBuffer()
            {
                return null;
            }
#endif
        }

        /// <summary>
        /// Decodes bytes that may arrive in several blocks, holding back the byte of an incomplete code unit at the end of a block.
        /// </summary>
        private sealed class Ucs2Decoder : MimeDecoder
        {
            private readonly MimeUcs2Encoding _encoding;
            private readonly byte[] _pending = new byte[UnitLength];
            private int _pendingCount;

            internal Ucs2Decoder(MimeUcs2Encoding encoding)
            {
                _encoding = encoding;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
                Fallback = encoding.DecoderFallback;
#endif
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                return GetCharCount(bytes, index, count, false);
            }

            public override int GetCharCount(byte[] bytes, int index, int count, bool flush)
            {
                CheckRange(bytes, nameof(bytes), index, count);

                var combined = Combine(bytes, index, count);
                int pendingByteCount;
                return Decode(combined, 0, combined.Length, flush, null, 0, CreateFallbackBuffer(), out pendingByteCount);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                return GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
            {
                CheckRange(bytes, nameof(bytes), byteIndex, byteCount);
                CheckDestination(chars, nameof(chars), charIndex);

                // The state changes only once the bytes are decoded, so a call that throws can be made again.
                var combined = Combine(bytes, byteIndex, byteCount);
                int pendingByteCount;
                var charCount = Decode(combined, 0, combined.Length, flush, chars, charIndex, CreateFallbackBuffer(), out pendingByteCount);
                Array.Copy(combined, combined.Length - pendingByteCount, _pending, 0, pendingByteCount);
                _pendingCount = pendingByteCount;
                return charCount;
            }

            public override void Reset()
            {
                _pendingCount = 0;
            }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            private DecoderFallbackBuffer CreateFallbackBuffer()
            {
                return (Fallback ?? _encoding.DecoderFallback).CreateFallbackBuffer();
            }
#else
            private object CreateFallbackBuffer()
            {
                return null;
            }
#endif

            /// <summary>
            /// Get the byte held back, followed by some new bytes.
            /// </summary>
            private byte[] Combine(byte[] bytes, int index, int count)
            {
                var combined = new byte[_pendingCount + count];
                Array.Copy(_pending, combined, _pendingCount);
                Array.Copy(bytes, index, combined, _pendingCount, count);
                return combined;
            }
        }
    }
}
