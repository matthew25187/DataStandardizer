using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Turns the bytes of a whole message into what an mbox file holds for it: its line breaks, its quotes, its Content-Length field and the empty line that ends it.
    /// </summary>
    internal static class MboxFormatter
    {
        private const byte CarriageReturn = 0x0D;
        private const byte LineFeed = 0x0A;
        private const byte Quote = 0x3E;
        private const byte Space = 0x20;
        private const byte Tab = 0x09;

        private const string ContentLengthFieldName = "Content-Length: ";
        private const int FromLength = 5;

        private static readonly byte[] From = { (byte)'F', (byte)'r', (byte)'o', (byte)'m', Space };
        private static readonly byte[] CrLf = { CarriageReturn, LineFeed };
        private static readonly byte[] Lf = { LineFeed };

        /// <summary>
        /// Builds what a file holds for a message.
        /// </summary>
        /// <param name="envelope">The separator line.</param>
        /// <param name="message">The bytes of the message, in the line breaks of the file.</param>
        /// <param name="variant">The variant of the format.</param>
        /// <param name="isCrLf"><c>true</c> if the line breaks of the file are CRLF.</param>
        /// <param name="fieldChange">How many fields the file holds beyond those of the message, from setting its Content-Length field.</param>
        /// <returns>The separator line, the message and the empty line that ends it.</returns>
        internal static byte[] Format(MboxEnvelope envelope, ReadOnlySpan<byte> message, MboxVariant variant, bool isCrLf, out int fieldChange)
        {
            fieldChange = 0;
            var lineBreak = isCrLf ? CrLf : Lf;
            var quoted = variant == MboxVariant.Mboxcl2 ? message.ToArray() : AddQuotes(message, variant, isCrLf);
            var output = new MemoryStream(quoted.Length + envelope.RawLine.Length + 2 * lineBreak.Length + ContentLengthFieldName.Length + lineBreak.Length);
            Write(output, envelope.RawLine.Span);
            Write(output, lineBreak);

            if (variant == MboxVariant.Mboxcl || variant == MboxVariant.Mboxcl2)
            {
                fieldChange = WriteWithContentLength(output, quoted, lineBreak, isCrLf);
            }
            else
            {
                Write(output, quoted);
            }

            if (quoted.Length > 0 && !EndsWithLineBreak(quoted, isCrLf)) Write(output, lineBreak);

            Write(output, lineBreak);
            return output.ToArray();
        }

        // ref. mboxrd and mboxo.  This covers the header as well as the body: the obsolete syntax of RFC 5322 §4.5 lets a field start "From : ", which a reader that looks for separator lines
        // has to be able to tell from one.
        private static byte[] AddQuotes(ReadOnlySpan<byte> message, MboxVariant variant, bool isCrLf)
        {
            var output = new MemoryStream(message.Length);
            var rest = message;
            while (!rest.IsEmpty)
            {
                var lineLength = GetLineLength(rest, isCrLf);
                var line = rest.Slice(0, lineLength);
                if (NeedsQuote(line, variant)) output.WriteByte(Quote);

                Write(output, line);
                rest = rest.Slice(lineLength);
            }

            return output.ToArray();
        }

        private static bool NeedsQuote(ReadOnlySpan<byte> line, MboxVariant variant)
        {
            var start = 0;
            if (variant == MboxVariant.Mboxrd)
            {
                while (start < line.Length && line[start] == Quote) start++;
            }

            return line.Length - start >= FromLength && line.Slice(start, FromLength).SequenceEqual(From);
        }

        // Gets the length of the first line, with its line break.  In a file of CRLF line breaks, a line feed with no carriage return doesn't end a line.
        private static int GetLineLength(ReadOnlySpan<byte> bytes, bool isCrLf)
        {
            var searched = 0;
            while (true)
            {
                var lineFeed = bytes.Slice(searched).IndexOf(LineFeed);
                if (lineFeed < 0) return bytes.Length;

                searched += lineFeed + 1;
                if (!isCrLf || (searched >= 2 && bytes[searched - 2] == CarriageReturn)) return searched;
            }
        }

        // ref. RFC 5322 §2.1: the header ends at the first empty line.  A field given the right length already stays as it is, so that a message that hasn't changed stays the same.
        private static int WriteWithContentLength(MemoryStream output, ReadOnlySpan<byte> quoted, byte[] lineBreak, bool isCrLf)
        {
            var fieldChange = 0;
            var headerLength = FindHeaderLength(quoted, isCrLf, out var bodyStart);
            var bodyLength = bodyStart < 0 ? 0L : quoted.Length - bodyStart;
            var header = quoted.Slice(0, headerLength);
            var body = bodyStart < 0 ? ReadOnlySpan<byte>.Empty : quoted.Slice(bodyStart);

            if (HasOnlyLength(header, bodyLength, isCrLf))
            {
                Write(output, header);
            }
            else
            {
                fieldChange = 1 - WriteWithoutContentLength(output, header, isCrLf);
                Write(output, Encoding.UTF8.GetBytes(ContentLengthFieldName + bodyLength.ToString(CultureInfo.InvariantCulture)));
                Write(output, lineBreak);
            }

            Write(output, lineBreak);
            Write(output, body);
            return fieldChange;
        }

        private static int FindHeaderLength(ReadOnlySpan<byte> quoted, bool isCrLf, out int bodyStart)
        {
            var position = 0;
            while (position < quoted.Length)
            {
                var lineLength = GetLineLength(quoted.Slice(position), isCrLf);
                var line = quoted.Slice(position, lineLength);
                if (line.SequenceEqual(isCrLf ? CrLf : Lf))
                {
                    bodyStart = position + lineLength;
                    return position;
                }

                position += lineLength;
            }

            bodyStart = -1;
            return quoted.Length;
        }

        private static bool HasOnlyLength(ReadOnlySpan<byte> header, long bodyLength, bool isCrLf)
        {
            var found = 0;
            var isRight = false;
            for (var rest = header; !rest.IsEmpty;)
            {
                var lineLength = GetLineLength(rest, isCrLf);
                var line = rest.Slice(0, lineLength);
                if (MboxScanner.StartsWithContentLengthName(line, out var valueStart))
                {
                    found++;
                    isRight = MboxScanner.TryParseLength(TrimLineBreak(line).Slice(valueStart), out var value) && value == bodyLength;
                }

                rest = rest.Slice(lineLength);
            }

            return found == 1 && isRight;
        }

        // Gives the number of fields left out.
        private static int WriteWithoutContentLength(MemoryStream output, ReadOnlySpan<byte> header, bool isCrLf)
        {
            var removed = 0;
            var isSkipping = false;
            for (var rest = header; !rest.IsEmpty;)
            {
                var lineLength = GetLineLength(rest, isCrLf);
                var line = rest.Slice(0, lineLength);
                // ref. RFC 5322 §2.2.3: a line that starts with white space continues the field before it.
                var isContinuation = line[0] == Space || line[0] == Tab;
                if (!isContinuation)
                {
                    isSkipping = MboxScanner.StartsWithContentLengthName(line, out _);
                    if (isSkipping) removed++;
                }

                if (!isSkipping) Write(output, line);

                rest = rest.Slice(lineLength);
            }

            return removed;
        }

        private static ReadOnlySpan<byte> TrimLineBreak(ReadOnlySpan<byte> line)
        {
            var end = line.Length;
            if (end > 0 && line[end - 1] == LineFeed) end--;

            return line.Slice(0, end);
        }

        private static bool EndsWithLineBreak(ReadOnlySpan<byte> bytes, bool isCrLf)
        {
            if (bytes.IsEmpty || bytes[bytes.Length - 1] != LineFeed) return false;

            return !isCrLf || (bytes.Length >= 2 && bytes[bytes.Length - 2] == CarriageReturn);
        }

        private static void Write(MemoryStream output, ReadOnlySpan<byte> bytes)
        {
            ImfLineBreaks.Write(output, bytes);
        }
    }
}
