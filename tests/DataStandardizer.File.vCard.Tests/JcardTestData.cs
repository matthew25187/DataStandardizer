using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// The examples of RFC 7095, and helpers to read and write jCard for the tests.
    /// </summary>
    internal static class JcardTestData
    {
        /// <summary>
        /// RFC 7095 Appendix B.1.2: the jCard form of the vCard of the author of RFC 6350, as the RFC prints it.
        /// </summary>
        public const string RfcExampleJcard =
            @"[""vcard"",
  [
    [""version"", {}, ""text"", ""4.0""],
    [""fn"", {}, ""text"", ""Simon Perreault""],
    [""n"",
      {},
      ""text"",
      [""Perreault"", ""Simon"", """", """", [""ing. jr"", ""M.Sc.""]]
    ],
    [""bday"", {}, ""date-and-or-time"", ""--02-03""],
    [""anniversary"",
      {},
      ""date-and-or-time"",
      ""2009-08-08T14:30:00-05:00""
    ],
    [""gender"", {}, ""text"", ""M""],
    [""lang"", { ""pref"": ""1"" }, ""language-tag"", ""fr""],
    [""lang"", { ""pref"": ""2"" }, ""language-tag"", ""en""],
    [""org"", { ""type"": ""work"" }, ""text"", ""Viagenie""],
    [""adr"",
       { ""type"": ""work"" },
       ""text"",
       [
        """",
        ""Suite D2-630"",
        ""2875 Laurier"",
        ""Quebec"",
        ""QC"",
        ""G1V 2M2"",
        ""Canada""
       ]
    ],
    [""tel"",
      { ""type"": [""work"", ""voice""], ""pref"": ""1"" },
      ""uri"",
      ""tel:+1-418-656-9254;ext=102""
    ],
    [""tel"",
      { ""type"": [""work"", ""cell"", ""voice"", ""video"", ""text""] },
      ""uri"",
      ""tel:+1-418-262-6501""
    ],
    [""email"",
      { ""type"": ""work"" },
      ""text"",
      ""simon.perreault@viagenie.ca""
    ],
    [""geo"", { ""type"": ""work"" }, ""uri"", ""geo:46.772673,-71.282945""],
    [""key"",
      { ""type"": ""work"" },
      ""uri"",
      ""http://www.viagenie.ca/simon.perreault/simon.asc""
    ],
    [""tz"", {}, ""utc-offset"", ""-05:00""],
    [""url"", { ""type"": ""home"" }, ""uri"", ""http://nomis80.org""]
  ]
]";

        /// <summary>
        /// RFC 7095 Appendix B.1.1: the vCard form of <see cref="RfcExampleJcard"/>, which is the example of RFC 6350 §8, corrected in three places to say what the jCard form says.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description><c>ANNIVERSARY</c> is <c>20090808T143000-0500</c>: the RFC's <c>20090808T1430-0500</c> has no seconds, and the jCard form has
        /// <c>2009-08-08T14:30:00-05:00</c>, which has them.</description></item>
        /// <item><description><c>TZ</c> has <c>VALUE=utc-offset</c>: with none it is TEXT (RFC 6350 §6.5.1), and the jCard form has the type <c>utc-offset</c>.</description></item>
        /// <item><description>The comma in the <c>GEO</c> URI is escaped, as RFC 6350 §3.4 and its errata 3845 and 3846 require.  The jCard form has none, since a JSON string has no
        /// escaping of vCard (RFC 7095 §3.1).</description></item>
        /// </list>
        /// </remarks>
        public const string RfcExampleVcard =
            "BEGIN:VCARD\r\n" +
            "VERSION:4.0\r\n" +
            "FN:Simon Perreault\r\n" +
            "N:Perreault;Simon;;;ing. jr,M.Sc.\r\n" +
            "BDAY:--0203\r\n" +
            "ANNIVERSARY:20090808T143000-0500\r\n" +
            "GENDER:M\r\n" +
            "LANG;PREF=1:fr\r\n" +
            "LANG;PREF=2:en\r\n" +
            "ORG;TYPE=work:Viagenie\r\n" +
            "ADR;TYPE=work:;Suite D2-630;2875 Laurier;\r\n" +
            " Quebec;QC;G1V 2M2;Canada\r\n" +
            "TEL;VALUE=uri;TYPE=\"work,voice\";PREF=1:tel:+1-418-656-9254;ext=102\r\n" +
            "TEL;VALUE=uri;TYPE=\"work,cell,voice,video,text\":tel:+1-418-262-6501\r\n" +
            "EMAIL;TYPE=work:simon.perreault@viagenie.ca\r\n" +
            "GEO;TYPE=work:geo:46.772673\\,-71.282945\r\n" +
            "KEY;TYPE=work;VALUE=uri:\r\n" +
            " http://www.viagenie.ca/simon.perreault/simon.asc\r\n" +
            "TZ;VALUE=utc-offset:-0500\r\n" +
            "URL;TYPE=home:http://nomis80.org\r\n" +
            "END:VCARD\r\n";

        /// <summary>
        /// The properties <see cref="JcardReader"/> gives for <see cref="RfcExampleJcard"/>, worked out by hand from RFC 7095: names in upper case (§4), each value in
        /// the text form of RFC 6350, a <c>VALUE</c> parameter only where the type isn't the default (§3.4.1), after the other parameters, and the values of <c>TYPE</c> as an array
        /// (§3.4.2).
        /// </summary>
        public static readonly List<string> RfcExampleProperties = new List<string>
        {
            "FN:Simon Perreault",
            "N:Perreault;Simon;;;ing. jr,M.Sc.",
            "BDAY:--0203",
            "ANNIVERSARY:20090808T143000-0500",
            "GENDER:M",
            "LANG;PREF=1:fr",
            "LANG;PREF=2:en",
            "ORG;TYPE=work:Viagenie",
            "ADR;TYPE=work:;Suite D2-630;2875 Laurier;Quebec;QC;G1V 2M2;Canada",
            "TEL;TYPE=work|voice;PREF=1;VALUE=uri:tel:+1-418-656-9254;ext=102",
            "TEL;TYPE=work|cell|voice|video|text;VALUE=uri:tel:+1-418-262-6501",
            "EMAIL;TYPE=work:simon.perreault@viagenie.ca",
            "GEO;TYPE=work:geo:46.772673\\,-71.282945",
            "KEY;TYPE=work:http://www.viagenie.ca/simon.perreault/simon.asc",
            "TZ;VALUE=utc-offset:-0500",
            "URL;TYPE=home:http://nomis80.org",
        };

        /// <summary>
        /// Reads every card of a jCard document.
        /// </summary>
        public static List<VcardCard> ReadJcard(string json, VcardOptions options = null)
        {
            using (var reader = new JcardReader(new MemoryStream(Encoding.UTF8.GetBytes(json)), options ?? new VcardOptions { MaxLineLength = null }))
            {
                return reader.ReadCards().ToList();
            }
        }

        /// <summary>
        /// Writes cards as a jCard document.
        /// </summary>
        public static string WriteJcard(IEnumerable<VcardCard> cards, VcardOptions options = null)
        {
            var output = new MemoryStream();
            using (var writer = new JcardWriter(output, options ?? new VcardOptions { MaxLineLength = null }))
            {
                writer.WriteCards(cards);
            }

            return Encoding.UTF8.GetString(output.ToArray());
        }

        /// <summary>
        /// Writes a card as a jCard document.
        /// </summary>
        public static string WriteJcard(VcardCard card, VcardOptions options = null)
        {
            return WriteJcard(new[] { card }, options);
        }

        /// <summary>
        /// Reads every card of some <c>.vcf</c> text.
        /// </summary>
        public static List<VcardCard> ReadVcards(string text, VcardOptions options = null)
        {
            using (var reader = new VcardReader(new StringReader(text), options ?? new VcardOptions { MaxLineLength = null }))
            {
                return reader.ReadCards().ToList();
            }
        }

        /// <summary>
        /// Writes cards as <c>.vcf</c> text.
        /// </summary>
        public static string WriteVcards(IEnumerable<VcardCard> cards, VcardOptions options = null)
        {
            var output = new StringWriter();
            using (var writer = new VcardWriter(output, options ?? new VcardOptions { MaxLineLength = null }))
            {
                writer.WriteCards(cards);
            }

            return output.ToString();
        }

        /// <summary>
        /// Reads the one card of a jCard document and describes its last property, as <see cref="Describe(VcardContentLine)"/> does.
        /// </summary>
        public static string ReadLastProperty(string json, VcardOptions options = null)
        {
            var card = ReadJcard(json, options).Single();
            return Describe(card.Properties[card.Properties.Count - 1]);
        }

        /// <summary>
        /// Reads a card, and leaves a <see cref="VcardFileException"/> for the test to go on from.
        /// </summary>
        public static void ReadPastFailure(JcardReader reader)
        {
            try
            {
                reader.ReadCard();
            }
            catch (VcardFileException)
            {
                return;
            }

            Assert.Fail("The read was expected to fail.");
        }

        /// <summary>
        /// Writes a card, and leaves a <see cref="VcardFileException"/> for the test to go on from.
        /// </summary>
        public static void WritePastFailure(JcardWriter writer, VcardCard card)
        {
            try
            {
                writer.WriteCard(card);
            }
            catch (VcardFileException)
            {
                return;
            }

            Assert.Fail("The write was expected to fail.");
        }

        /// <summary>
        /// Gets the bytes of a jCard document in UTF-8.
        /// </summary>
        public static byte[] Utf8(string json)
        {
            return Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Describes each property of each card as text that a test can compare exactly: its group, its name, each parameter as <c>;NAME=value|value</c>, a colon and its value.
        /// </summary>
        public static List<List<string>> Describe(IEnumerable<VcardCard> cards)
        {
            return cards.Select(Describe).ToList();
        }

        /// <summary>
        /// Describes each property of a card, as <see cref="Describe(IEnumerable{VcardCard})"/> does.
        /// </summary>
        public static List<string> Describe(VcardCard card)
        {
            return card.Properties.Select(Describe).ToList();
        }

        /// <summary>
        /// Describes a property, as <see cref="Describe(IEnumerable{VcardCard})"/> does.
        /// </summary>
        public static string Describe(VcardContentLine line)
        {
            var parameters = line.Parameters.Select(parameter => ";" + parameter.Name + "=" + string.Join("|", parameter.Values));
            var group = line.Group is null ? string.Empty : line.Group + ".";
            return group + line.Name + string.Concat(parameters) + ":" + line.Value;
        }
    }
}
