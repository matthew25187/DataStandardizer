using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// The examples of RFC 6351, and helpers to read and write xCard for the tests.
    /// </summary>
    internal static class XcardTestData
    {
        /// <summary>
        /// The xCard namespace (RFC 6351 §8.1).
        /// </summary>
        public const string Namespace = "urn:ietf:params:xml:ns:vcard-4.0";

        /// <summary>
        /// The start of a document of one card that has an <c>fn</c>, to which a test adds properties and then <see cref="CardEnd"/>.
        /// </summary>
        public const string CardStart = "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard><fn><text>A</text></fn>";

        /// <summary>
        /// The end of a document that <see cref="CardStart"/> starts.
        /// </summary>
        public const string CardEnd = "</vcard></vcards>";

        /// <summary>
        /// RFC 6351 §4: the xCard of the author of RFC 6351, as the RFC prints it, without the RFC's indentation of three spaces.
        /// </summary>
        public const string RfcXcardExample =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\">\n" +
            "  <vcard>\n" +
            "    <fn><text>Simon Perreault</text></fn>\n" +
            "    <n>\n" +
            "      <surname>Perreault</surname>\n" +
            "      <given>Simon</given>\n" +
            "      <additional/>\n" +
            "      <prefix/>\n" +
            "      <suffix>ing. jr</suffix>\n" +
            "      <suffix>M.Sc.</suffix>\n" +
            "    </n>\n" +
            "    <bday><date>--0203</date></bday>\n" +
            "    <anniversary>\n" +
            "      <date-time>20090808T1430-0500</date-time>\n" +
            "    </anniversary>\n" +
            "    <gender><sex>M</sex></gender>\n" +
            "    <lang>\n" +
            "      <parameters><pref><integer>1</integer></pref></parameters>\n" +
            "      <language-tag>fr</language-tag>\n" +
            "    </lang>\n" +
            "    <lang>\n" +
            "      <parameters><pref><integer>2</integer></pref></parameters>\n" +
            "      <language-tag>en</language-tag>\n" +
            "    </lang>\n" +
            "    <org>\n" +
            "      <parameters><type><text>work</text></type></parameters>\n" +
            "      <text>Viagenie</text>\n" +
            "    </org>\n" +
            "    <adr>\n" +
            "      <parameters>\n" +
            "        <type><text>work</text></type>\n" +
            "        <label><text>Simon Perreault\n" +
            "2875 boul. Laurier, suite D2-630\n" +
            "Quebec, QC, Canada\n" +
            "G1V 2M2</text></label>\n" +
            "      </parameters>\n" +
            "      <pobox/>\n" +
            "      <ext/>\n" +
            "      <street>2875 boul. Laurier, suite D2-630</street>\n" +
            "      <locality>Quebec</locality>\n" +
            "      <region>QC</region>\n" +
            "      <code>G1V 2M2</code>\n" +
            "      <country>Canada</country>\n" +
            "    </adr>\n" +
            "    <tel>\n" +
            "      <parameters>\n" +
            "        <type>\n" +
            "          <text>work</text>\n" +
            "          <text>voice</text>\n" +
            "        </type>\n" +
            "      </parameters>\n" +
            "      <uri>tel:+1-418-656-9254;ext=102</uri>\n" +
            "    </tel>\n" +
            "    <tel>\n" +
            "      <parameters>\n" +
            "        <type>\n" +
            "          <text>work</text>\n" +
            "          <text>text</text>\n" +
            "          <text>voice</text>\n" +
            "          <text>cell</text>\n" +
            "          <text>video</text>\n" +
            "        </type>\n" +
            "      </parameters>\n" +
            "      <uri>tel:+1-418-262-6501</uri>\n" +
            "    </tel>\n" +
            "    <email>\n" +
            "      <parameters><type><text>work</text></type></parameters>\n" +
            "      <text>simon.perreault@viagenie.ca</text>\n" +
            "    </email>\n" +
            "    <geo>\n" +
            "      <parameters><type><text>work</text></type></parameters>\n" +
            "      <uri>geo:46.766336,-71.28955</uri>\n" +
            "    </geo>\n" +
            "    <key>\n" +
            "      <parameters><type><text>work</text></type></parameters>\n" +
            "      <uri>http://www.viagenie.ca/simon.perreault/simon.asc</uri>\n" +
            "    </key>\n" +
            "    <tz><text>America/Montreal</text></tz>\n" +
            "    <url>\n" +
            "      <parameters><type><text>home</text></type></parameters>\n" +
            "      <uri>http://nomis80.org</uri>\n" +
            "    </url>\n" +
            "  </vcard>\n" +
            "</vcards>\n";

        /// <summary>
        /// The properties <see cref="XcardReader"/> gives for <see cref="RfcXcardExample"/>, worked out by hand from RFC 6351 §6: names in upper case, each value with the escaping of
        /// RFC 6350, a <c>VALUE</c> parameter only where the value element isn't the property's default type, after the other parameters, and the <c>VERSION</c> the namespace
        /// gives (§5.1) first.
        /// </summary>
        public static readonly List<string> RfcXcardExampleProperties = new List<string>
        {
            "FN:Simon Perreault",
            "N:Perreault;Simon;;;ing. jr,M.Sc.",
            "BDAY:--0203",
            "ANNIVERSARY:20090808T1430-0500",
            "GENDER:M",
            "LANG;PREF=1:fr",
            "LANG;PREF=2:en",
            "ORG;TYPE=work:Viagenie",
            "ADR;TYPE=work;LABEL=Simon Perreault\n2875 boul. Laurier, suite D2-630\nQuebec, QC, Canada\nG1V 2M2:;;2875 boul. Laurier\\, suite D2-630;Quebec;QC;G1V 2M2;Canada",
            "TEL;TYPE=work|voice;VALUE=uri:tel:+1-418-656-9254;ext=102",
            "TEL;TYPE=work|text|voice|cell|video;VALUE=uri:tel:+1-418-262-6501",
            "EMAIL;TYPE=work:simon.perreault@viagenie.ca",
            "GEO;TYPE=work:geo:46.766336\\,-71.28955",
            "KEY;TYPE=work:http://www.viagenie.ca/simon.perreault/simon.asc",
            "TZ:America/Montreal",
            "URL;TYPE=home:http://nomis80.org",
        };

        /// <summary>
        /// Reads every card of an xCard document.
        /// </summary>
        public static List<VcardCard> ReadXcard(string xml, VcardOptions options = null)
        {
            using (var reader = new XcardReader(new StringReader(xml), options ?? new VcardOptions()))
            {
                return reader.ReadCards().ToList();
            }
        }

        /// <summary>
        /// Writes cards as an xCard document.
        /// </summary>
        public static string WriteXcard(IEnumerable<VcardCard> cards, VcardOptions options = null)
        {
            var output = new MemoryStream();
            using (var writer = new XcardWriter(output, options ?? new VcardOptions()))
            {
                writer.WriteCards(cards);
            }

            return Encoding.UTF8.GetString(output.ToArray());
        }

        /// <summary>
        /// Writes a card as an xCard document.
        /// </summary>
        public static string WriteXcard(VcardCard card, VcardOptions options = null)
        {
            return WriteXcard(new[] { card }, options);
        }

        /// <summary>
        /// Gets an XML document with no white space between its elements and no declaration, so that two documents that say the same thing compare equal as text.  White space in
        /// an element that holds text is kept, as are the empty elements, such as <c>&lt;a/&gt;</c>, which become <c>&lt;a /&gt;</c>.
        /// </summary>
        public static string Canonical(string xml)
        {
            return XDocument.Parse(xml).ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Reads the one card of an xCard document and describes its last property, as <see cref="JcardTestData.Describe(VcardContentLine)"/> does.
        /// </summary>
        public static string ReadLastXcardProperty(string xml, VcardOptions options = null)
        {
            var card = ReadXcard(xml, options).Single();
            return JcardTestData.Describe(card.Properties[card.Properties.Count - 1]);
        }
    }
}
