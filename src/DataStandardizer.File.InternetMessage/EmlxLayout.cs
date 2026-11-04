using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Finds the three pieces of an EMLX file: a line with the byte count, the message, and the property list.
    /// </summary>
    /// <remarks>
    /// EMLX is proprietary and undocumented.  This follows the layout that open-source readers agree on.  Several of them trim white space from the count before they read it, so spaces after it
    /// are kept and not treated as a problem.
    /// </remarks>
    internal sealed class EmlxLayout
    {
        private const string InvalidByteCountMessage = "The first line isn't a byte count that fits the file.  ImfOptions.AllowInvalidEmlxByteCount accepts it.";
        private const string MissingMetadataMessage = "No property list follows the message.  ImfOptions.AllowInvalidEmlxMetadata accepts it.";
        private const string InvalidMetadataTemplate = "What follows the message isn't a well-formed property list: {0}  ImfOptions.AllowInvalidEmlxMetadata accepts it.";
        private const string MetadataTooLongTemplate = "The property list is longer than the maximum of {0} octets.";

        private const byte LineFeed = 0x0A;
        private const byte Space = 0x20;

        private static readonly byte[] XmlStart = Encoding.UTF8.GetBytes("<?xml");
        private static readonly byte[] DocumentTypeStart = Encoding.UTF8.GetBytes("<!DOCTYPE");
        private static readonly byte[] PlistStart = Encoding.UTF8.GetBytes("<plist");

        private EmlxLayout(byte[] byteCountLine, ArraySegment<byte> message, EmlxMetadata metadata)
        {
            ByteCountLine = byteCountLine;
            Message = message;
            Metadata = metadata;
        }

        /// <summary>
        /// Gets the first line, with its line feed.
        /// </summary>
        internal byte[] ByteCountLine { get; }

        /// <summary>
        /// Gets the bytes of the message.
        /// </summary>
        internal ArraySegment<byte> Message { get; }

        /// <summary>
        /// Gets the metadata.
        /// </summary>
        internal EmlxMetadata Metadata { get; }

        /// <summary>
        /// Finds the pieces of a file.
        /// </summary>
        /// <param name="file">The whole file.</param>
        /// <param name="length">The length of the file.</param>
        /// <param name="options">The options.</param>
        /// <param name="diagnostics">Receives the problems that the options accept.</param>
        /// <exception cref="ImfFileException">The layout breaks a rule that the options don't accept, or the property list passes the limit.</exception>
        internal static EmlxLayout Read(byte[] file, int length, ImfOptions options, List<ImfDiagnostic> diagnostics)
        {
            var bytes = new ReadOnlySpan<byte>(file, 0, length);
            var lineFeed = bytes.IndexOf(LineFeed);
            var lineLength = lineFeed < 0 ? length : lineFeed + 1;
            var rest = bytes.Slice(lineLength);

            int messageLength;
            if (TryParseByteCount(bytes.Slice(0, lineFeed < 0 ? length : lineFeed), lineFeed >= 0, out var count) && Fits(rest, count))
            {
                messageLength = (int)count;
            }
            else
            {
                Report(ImfDiagnosticKind.InvalidEmlxByteCount, InvalidByteCountMessage, 0, 1, options, diagnostics);
                messageLength = FindMetadata(rest);
            }

            var metadataStart = lineLength + messageLength;
            var metadataLength = length - metadataStart;
            if (metadataLength > options.MaxEmlxMetadataLength)
                throw BuildException(string.Format(MetadataTooLongTemplate, options.MaxEmlxMetadataLength), metadataStart, CountLines(bytes.Slice(0, metadataStart)));

            var byteCountLine = bytes.Slice(0, lineLength).ToArray();
            var metadata = ReadMetadata(bytes.Slice(metadataStart).ToArray(), metadataStart, CountLines(bytes.Slice(0, metadataStart)), options, diagnostics);
            return new EmlxLayout(byteCountLine, new ArraySegment<byte>(file, lineLength, messageLength), metadata);
        }

        /// <summary>
        /// Formats the first line of a file, keeping the line it was read with if that is right, and otherwise the width of that line.
        /// </summary>
        /// <param name="count">The length of the message.</param>
        /// <param name="previous">The line that was read, or <c>null</c> if there was none.</param>
        internal static byte[] FormatByteCount(long count, [AllowNull] byte[] previous)
        {
            var digits = count.ToString(CultureInfo.InvariantCulture);
            if (previous is null || previous.Length == 0 || !TryParseByteCount(new ReadOnlySpan<byte>(previous, 0, previous.Length - 1), previous[previous.Length - 1] == LineFeed, out var previousCount))
                return Encoding.UTF8.GetBytes(digits + "\n");

            if (previousCount == count) return previous;

            return Encoding.UTF8.GetBytes(digits.PadRight(previous.Length - 1) + "\n");
        }

        // ref. the first line of the layout: ASCII digits, which may have spaces after them, and a line feed.
        private static bool TryParseByteCount(ReadOnlySpan<byte> line, bool hasLineFeed, out long count)
        {
            count = 0;
            if (!hasLineFeed) return false;

            var end = line.Length;
            while (end > 0 && line[end - 1] == Space) end--;

            if (end == 0) return false;

            for (var index = 0; index < end; index++)
            {
                var digit = line[index] - (byte)'0';
                if (digit < 0 || digit > 9 || count > (long.MaxValue - digit) / 10) return false;

                count = (count * 10) + digit;
            }

            return true;
        }

        // A count that is right ends at the end of the file, or where the property list starts.
        private static bool Fits(ReadOnlySpan<byte> rest, long count)
        {
            if (count > rest.Length) return false;

            var after = rest.Slice((int)count);
            return after.IsEmpty || after.StartsWith(XmlStart) || after.StartsWith(DocumentTypeStart) || after.StartsWith(PlistStart);
        }

        private static int FindMetadata(ReadOnlySpan<byte> rest)
        {
            var start = rest.LastIndexOf(XmlStart);
            if (start < 0) start = rest.LastIndexOf(PlistStart);

            return start < 0 ? rest.Length : start;
        }

        private static EmlxMetadata ReadMetadata(byte[] bytes, long position, long lineNumber, ImfOptions options, List<ImfDiagnostic> diagnostics)
        {
            if (bytes.Length == 0)
            {
                Report(ImfDiagnosticKind.InvalidEmlxMetadata, MissingMetadataMessage, position, lineNumber, options, diagnostics);
                return EmlxMetadata.FromInvalid(bytes);
            }

            if (EmlxPlist.TryRead(bytes, options.MaxNestingDepth, out var entries, out var problem)) return EmlxMetadata.FromRead(entries, bytes);

            Report(ImfDiagnosticKind.InvalidEmlxMetadata, string.Format(InvalidMetadataTemplate, problem), position, lineNumber, options, diagnostics);
            return EmlxMetadata.FromInvalid(bytes);
        }

        private static void Report(ImfDiagnosticKind kind, string message, long position, long lineNumber, ImfOptions options, List<ImfDiagnostic> diagnostics)
        {
            if (!ImfConformance.IsAllowed(kind, options))
            {
                var exception = BuildException(message, position, lineNumber);
                exception.Data[ImfFileException.DataItemName.DiagnosticKind] = kind;
                throw exception;
            }

            diagnostics.Add(new ImfDiagnostic(kind, message, position, string.Empty, string.Empty, lineNumber));
        }

        private static ImfFileException BuildException(string message, long position, long lineNumber)
        {
            var exception = new ImfFileException(message);
            exception.Data[ImfFileException.DataItemName.BytePosition] = position;
            exception.Data[ImfFileException.DataItemName.LineNumber] = lineNumber;
            return exception;
        }

        // Gives the 1-based number of the line after these bytes.
        private static long CountLines(ReadOnlySpan<byte> bytes)
        {
            var lines = 1L;
            for (var rest = bytes; !rest.IsEmpty;)
            {
                var lineFeed = rest.IndexOf(LineFeed);
                if (lineFeed < 0) break;

                lines++;
                rest = rest.Slice(lineFeed + 1);
            }

            return lines;
        }
    }
}
