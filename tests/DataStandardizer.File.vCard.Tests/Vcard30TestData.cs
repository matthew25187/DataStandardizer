using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Builds vCard 3.0 cards, and writes cards in that version, for the tests of it.
    /// </summary>
    internal static class Vcard30TestData
    {
        public const string Begin = "BEGIN:VCARD";
        public const string End = "END:VCARD";
        public const string Version30 = "VERSION:3.0";
        public const string FormattedName = "FN:Jane Doe";
        public const string Name = "N:Doe;Jane;;;";

        // A nested card of vCard 3.0 that follows the rules of a card, with its line breaks and the semicolons of its N as they are before it is escaped as a value.
        private const string NestedCardStart = "BEGIN:VCARD\nVERSION:3.0\nFN:a\nN:a;;;;\n";
        private const string NestedCardEnd = "END:VCARD\n";

        // A longer limit than any test's line, so that a test's text has no folds.
        private const int NoFolding = 100000;

        // Longer than the 65519 characters that Uri holds on .NET Framework.
        private static readonly string LargeBase64 = new string('A', 80000);

        /// <summary>
        /// A <c>PHOTO</c> line of vCard 3.0 with more data than <see cref="System.Uri"/> holds on .NET Framework.
        /// </summary>
        public static readonly string LargeVersion30PhotoLine = "PHOTO;ENCODING=b;TYPE=JPEG:" + LargeBase64;

        /// <summary>
        /// The same <c>PHOTO</c> line in vCard 4.0.
        /// </summary>
        public static readonly string LargeVersion40PhotoLine = "PHOTO:data:image/jpeg;base64\\," + LargeBase64;

        /// <summary>
        /// A <c>URL</c> line of vCard 4.0 with a URI longer than <see cref="System.Uri"/> holds on .NET Framework.
        /// </summary>
        public static readonly string LargeVersion40UrlLine = "URL:http://example.com/" + LargeBase64;

        /// <summary>
        /// The text of a URI longer than <see cref="System.Uri"/> holds on .NET Framework.
        /// </summary>
        public static readonly string LargeUri = "http://example.com/" + LargeBase64;

        /// <summary>
        /// Data whose base64 is <see cref="LargeBase64"/>: 60000 zero bytes are 80000 base64 characters, all <c>A</c>.
        /// </summary>
        public static byte[] LargeData()
        {
            return new byte[60000];
        }

        /// <summary>
        /// The value of the <c>PHOTO</c> line of vCard 4.0 that holds <see cref="LargeData"/> with no format.
        /// </summary>
        public static readonly string LargeDataVersion40PhotoValue = "data:application/octet-stream;base64\\," + LargeBase64;

        /// <summary>
        /// A vCard 3.0 card with the given properties and nothing else.
        /// </summary>
        public static string BareCard(params string[] properties)
        {
            var lines = new List<string> { Begin, Version30 };
            lines.AddRange(properties);
            lines.Add(End);
            return VcardTestData.Crlf(lines.ToArray());
        }

        /// <summary>
        /// A minimal vCard 3.0 card, with an <c>FN</c> and an <c>N</c>, and the given properties after them.
        /// </summary>
        public static string Card(params string[] properties)
        {
            var lines = new List<string> { FormattedName, Name };
            lines.AddRange(properties);
            return BareCard(lines.ToArray());
        }

        /// <summary>
        /// Gets an <c>AGENT</c> line that holds a card that holds another, to the given number of <c>AGENT</c> properties, with each level escaped as a TEXT value (RFC 2426 §4).
        /// </summary>
        public static string NestedAgentLine(int levels)
        {
            var text = NestedCardStart + NestedCardEnd;
            for (var level = 1; level < levels; level++)
            {
                text = NestedCardStart + "AGENT:" + EscapeText(text) + "\n" + NestedCardEnd;
            }

            return "AGENT:" + EscapeText(text);
        }

        /// <summary>
        /// Removes the folds of a writer that folds with a space.
        /// </summary>
        public static string Unfold(string text)
        {
            return text.Replace("\r\n ", string.Empty);
        }

        /// <summary>
        /// Gets the parameters of a line in the form <c>NAME=a,b</c>, in order.
        /// </summary>
        public static List<string> ParametersOf(VcardContentLine line)
        {
            return line.Parameters.Select(parameter => parameter.Name + "=" + string.Join(",", parameter.Values)).ToList();
        }

        /// <summary>
        /// Writes a card in a version, with no folds.
        /// </summary>
        public static string Write(VcardCard card, VcardVersion version, bool allowLossy = false, bool allowMissingRequired = false)
        {
            var options = new VcardOptions { MaxLineLength = null, WriteVersion = version, FoldLength = NoFolding, AllowLossyVersionConversion = allowLossy, AllowMissingRequiredProperties = allowMissingRequired };
            var output = new StringWriter();
            using (var writer = new VcardWriter(output, options))
            {
                writer.WriteCard(card);
            }

            return output.ToString();
        }

        private static string EscapeText(string text)
        {
            return text.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");
        }
    }
}
