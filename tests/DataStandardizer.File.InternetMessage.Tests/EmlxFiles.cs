using System.IO;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Hand-written EMLX files in the layout that Apple Mail produces, and the helpers that read and write them for the tests.
    /// </summary>
    /// <remarks>
    /// EMLX is undocumented, so none of this comes from a specification.  Each byte count is worked out by hand from the message it goes with.
    /// </remarks>
    internal static class EmlxFiles
    {
        public const string Boundary = "b";

        public const string PlistStart = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n<plist version=\"1.0\">\n";
        public const string PlistEnd = "</plist>\n";

        // Flags of 8590133253: read (bit 0), answered (bit 2), two attachments (bits 10 to 15), priority 3 (bits 16 to 22) and bit 33, which no documentation names.
        public const long KnownAndUnknownFlags = 8590133253;

        public const string Metadata = PlistStart + "<dict>\n\t<key>date-last-viewed</key>\n\t<integer>1767225660</integer>\n\t<key>date-received</key>\n\t<integer>1767225600</integer>\n\t<key>flags</key>\n\t<integer>8590133253</integer>\n\t<key>remote-id</key>\n\t<string>42</string>\n\t<key>subject</key>\n\t<string>Hello &amp; welcome</string>\n</dict>\n" + PlistEnd;

        public const string UnknownKeysMetadata = PlistStart + "<dict>\n\t<key>x-color</key>\n\t<real>1.50</real>\n\t<key>x-big</key>\n\t<integer>123456789012345678901234567890</integer>\n\t<key>x-list</key>\n\t<array>\n\t\t<string>a</string>\n\t\t<true/>\n\t\t<false/>\n\t</array>\n\t<key>x-map</key>\n\t<dict>\n\t\t<key>when</key>\n\t\t<date>2026-01-01T00:00:00Z</date>\n\t\t<key>bytes</key>\n\t\t<data>AAEC</data>\n\t</dict>\n\t<!-- kept -->\n\t<key>subject</key>\n\t<string>  spaced  </string>\n</dict>\n" + PlistEnd;

        // 57 octets.
        public const string Message = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\n\none\n";
        public const string Eml = "Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom: a@b.test\r\n\r\none\r\n";
        public const string Plain = "57\n" + Message + Metadata;
        public const string UnknownKeys = "57\n" + Message + UnknownKeysMetadata;

        // 57 octets, with spaces after the count, which several open-source readers trim.
        public const string Padded = "57        \n" + Message + Metadata;

        public const string MessageNoFinalLineBreak = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\n\none";
        public const string NoFinalLineBreak = "56\n" + MessageNoFinalLineBreak + Metadata;

        // 267 octets.
        public const string AttachmentMessage = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\nMIME-Version: 1.0\nContent-Type: multipart/mixed; boundary=b\n\n--b\nContent-Type: text/plain\n\nhi\n--b\nContent-Type: application/octet-stream\nContent-Transfer-Encoding: base64\nContent-Disposition: attachment\n\nAAEC\n--b--\n";
        public const string Attachment = "267\n" + AttachmentMessage + Metadata;

        // 289 octets.  Part 2 is in another file.
        public const string PartialMessage = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\nMIME-Version: 1.0\nContent-Type: multipart/mixed; boundary=b\n\n--b\nContent-Type: text/plain\n\nhi\n--b\nContent-Type: application/octet-stream\nContent-Transfer-Encoding: base64\nX-Apple-Content-Length: 4\nContent-Disposition: attachment\n\n\n--b--\n";
        public const string Partial = "289\n" + PartialMessage + Metadata;

        public const string PartContent = "AAEC";

        // 313 octets, with two parts in other files.
        public const string TwoPartsMessage = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\nMIME-Version: 1.0\nContent-Type: multipart/mixed; boundary=b\n\n--b\nContent-Type: text/plain\nX-Apple-Content-Length: 2\n\n\n--b\nContent-Type: application/octet-stream\nContent-Transfer-Encoding: base64\nX-Apple-Content-Length: 4\nContent-Disposition: attachment\n\n\n--b--\n";
        public const string TwoParts = "313\n" + TwoPartsMessage + Metadata;
        public const string TwoPartsWholeMessage = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\nMIME-Version: 1.0\nContent-Type: multipart/mixed; boundary=b\n\n--b\nContent-Type: text/plain\nX-Apple-Content-Length: 2\n\nhi\n--b\nContent-Type: application/octet-stream\nContent-Transfer-Encoding: base64\nX-Apple-Content-Length: 4\nContent-Disposition: attachment\n\nAAEC\n--b--\n";

        // The message of Partial with the content of its part in place, of 293 octets.
        public const string WholeMessage = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\nMIME-Version: 1.0\nContent-Type: multipart/mixed; boundary=b\n\n--b\nContent-Type: text/plain\n\nhi\n--b\nContent-Type: application/octet-stream\nContent-Transfer-Encoding: base64\nX-Apple-Content-Length: 4\nContent-Disposition: attachment\n\nAAEC\n--b--\n";
        public const string Whole = "293\n" + WholeMessage + Metadata;

        // 288 octets.  As PartialMessage, but the part stops before the empty line, whose line break belongs to the delimiter (RFC 2046 §5.1.1).
        public const string NoEmptyLineMessage = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\nMIME-Version: 1.0\nContent-Type: multipart/mixed; boundary=b\n\n--b\nContent-Type: text/plain\n\nhi\n--b\nContent-Type: application/octet-stream\nContent-Transfer-Encoding: base64\nX-Apple-Content-Length: 4\nContent-Disposition: attachment\n\n--b--\n";
        public const string NoEmptyLine = "288\n" + NoEmptyLineMessage + Metadata;

        // 90 octets.  The body of a message with no parts is in another file.
        public const string SinglePartMessage = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\nX-Apple-Content-Length: not a number\n\n";
        public const string SinglePart = "90\n" + SinglePartMessage + Metadata;

        /// <summary>
        /// Gets options that accept the layout problems.
        /// </summary>
        public static ImfOptions Lenient => new ImfOptions { AllowInvalidEmlxByteCount = true, AllowInvalidEmlxMetadata = true };

        /// <summary>
        /// Reads a whole file.
        /// </summary>
        public static EmlxEntry Read(string file, ImfOptions options = null, EmlxPartResolver partResolver = null)
        {
            using (var reader = new EmlxReader(new MemoryStream(Octets.Of(file)), options ?? new ImfOptions()))
            {
                return reader.ReadEntry(partResolver);
            }
        }

        /// <summary>
        /// Writes a whole file, and gives its bytes with one in each character.
        /// </summary>
        public static string Write(EmlxEntry entry, ImfOptions options = null)
        {
            var output = new MemoryStream();
            using (var writer = new EmlxWriter(output, options ?? new ImfOptions(), () => Boundary))
            {
                writer.WriteEntry(entry);
            }

            return Octets.ToText(output.ToArray());
        }

        /// <summary>
        /// Gets a resolver that supplies the first part of <see cref="TwoParts"/> and then the second, by their numbers.
        /// </summary>
        public static EmlxPartResolver ResolveTwoParts()
        {
            return part => new MemoryStream(Octets.Of(part.Path == "1" ? "hi" : PartContent));
        }

        /// <summary>
        /// Tries to write an entry that the writer should refuse, so that a test can go on to use the writer.
        /// </summary>
        /// <returns><c>true</c> if the writer refused the entry.</returns>
        public static bool TryRefuse(EmlxWriter writer, EmlxEntry entry)
        {
            try
            {
                writer.WriteEntry(entry);
                return false;
            }
            catch (ImfFileException)
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a resolver that supplies the same content for every part.
        /// </summary>
        public static EmlxPartResolver ResolveTo(string content)
        {
            return part => new MemoryStream(Octets.Of(content));
        }
    }
}
