using System.Collections.Generic;
using System.IO;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Builds vCard 2.1 cards, and writes cards in that version, for the tests of it.
    /// </summary>
    internal static class Vcard21TestData
    {
        public const string Begin = "BEGIN:VCARD";
        public const string End = "END:VCARD";
        public const string Version21 = "VERSION:2.1";
        public const string Name = "N:Doe;Jane";

        /// <summary>
        /// A vCard 2.1 card with the given properties and nothing else.
        /// </summary>
        public static string BareCard(params string[] properties)
        {
            var lines = new List<string> { Begin, Version21 };
            lines.AddRange(properties);
            lines.Add(End);
            return VcardTestData.Crlf(lines.ToArray());
        }

        /// <summary>
        /// A minimal vCard 2.1 card, with an <c>N</c>, and the given properties after it.
        /// </summary>
        public static string Card(params string[] properties)
        {
            var lines = new List<string> { Name };
            lines.AddRange(properties);
            return BareCard(lines.ToArray());
        }

        /// <summary>
        /// Writes a card as vCard 2.1.
        /// </summary>
        public static string Write(VcardCard card, bool allowLossy = false, bool allowMissingRequired = false, bool writeNamedTypes = false)
        {
            var options = new VcardOptions
            {
                WriteVersion = VcardVersion.V2_1,
                AllowLossyVersionConversion = allowLossy,
                AllowMissingRequiredProperties = allowMissingRequired,
                WriteNamedTypesInVersion21 = writeNamedTypes
            };

            var output = new StringWriter();
            using (var writer = new VcardWriter(output, options))
            {
                writer.WriteCard(card);
            }

            return output.ToString();
        }
    }
}
