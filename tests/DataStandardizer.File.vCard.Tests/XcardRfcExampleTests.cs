using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.JcardTestData;
using static DataStandardizer.File.vCard.Tests.XcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests that the examples of RFC 6351, and the xCard examples of RFC 6473 and RFC 6869, read to the same cards as their <c>.vcf</c> forms.
    /// </summary>
    public class XcardRfcExampleTests
    {
        /// <summary>
        /// RFC 6351 §6: the xCard of the example of format conversion, as the RFC prints it, without its indentation of three spaces.
        /// </summary>
        private const string ConversionExample =
            "<?xml version=\"1.0\"?>\n" +
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\">\n" +
            "  <vcard>\n" +
            "    <fn><text>J. Doe</text></fn>\n" +
            "    <n>\n" +
            "      <surname>Doe</surname>\n" +
            "      <given>J.</given>\n" +
            "      <additional/>\n" +
            "      <prefix/>\n" +
            "      <suffix/>\n" +
            "    </n>\n" +
            "    <x-file>\n" +
            "      <parameters>\n" +
            "        <mediatype><text>image/jpeg</text></mediatype>\n" +
            "      </parameters>\n" +
            "      <unknown>alien.jpg</unknown>\n" +
            "    </x-file>\n" +
            "    <a xmlns=\"http://www.w3.org/1999/xhtml\"\n" +
            "       href=\"http://www.example.com\">My web page!</a>\n" +
            "  </vcard>\n" +
            "</vcards>\n";

        /// <summary>
        /// RFC 6351 §6: the vCard form of <see cref="ConversionExample"/>, with the XML value on one line.  The RFC breaks it with <c>\n</c> between the attributes, which is
        /// white space that XML doesn't keep, and folds it.
        /// </summary>
        private const string ConversionExampleVcard =
            "BEGIN:VCARD\r\n" +
            "VERSION:4.0\r\n" +
            "FN:J. Doe\r\n" +
            "N:Doe;J.;;;\r\n" +
            "X-FILE;MEDIATYPE=image/jpeg:alien.jpg\r\n" +
            "XML:<a xmlns=\"http://www.w3.org/1999/xhtml\" href=\"http://www.example.com\">My web page!</a>\r\n" +
            "END:VCARD\r\n";

        /// <summary>
        /// RFC 6473 §3: the example, which the RFC gives as a bare <c>vcard</c> element of XMPP, inside the <c>vcards</c> element that RFC 6351 §5 says MUST be present in a document.
        /// </summary>
        private const string Rfc6473Example =
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\">\n" +
            "<vcard xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\">\n" +
            "  <fn><text>jabber.org IM service</text></fn>\n" +
            "  <url><uri>http://www.jabber.org/</uri></url>\n" +
            "  <lang>\n" +
            "    <parameters><pref><integer>1</integer></pref></parameters>\n" +
            "    <language-tag>en</language-tag>\n" +
            "  </lang>\n" +
            "  <email><text>xmpp@jabber.org</text></email>\n" +
            "  <impp><uri>xmpp:jabber.org</uri></impp>\n" +
            "  <logo><uri>http://www.jabber.org/images/logo.png</uri></logo>\n" +
            "  <geo><uri>geo:42.25,-91.05</uri></geo>\n" +
            "  <tz><text>America/Chicago</text></tz>\n" +
            "  <source><uri>xmpp:jabber.org?vcard</uri></source>\n" +
            "  <rev><timestamp>19990104T122100Z</timestamp></rev>\n" +
            "  <kind><text>application</text></kind>\n" +
            "</vcard>\n" +
            "</vcards>\n";

        /// <summary>
        /// The content lines of <see cref="Rfc6473Example"/>, as <c>VcardExtensionTests</c> gives them, with the comma of the <c>geo</c> URI escaped (RFC 6350 errata 3845 and 3846).
        /// </summary>
        private static readonly string[] Rfc6473ExampleProperties =
        {
            "FN:jabber.org IM service",
            "URL:http://www.jabber.org/",
            "LANG;PREF=1:en",
            "EMAIL:xmpp@jabber.org",
            "IMPP:xmpp:jabber.org",
            "LOGO:http://www.jabber.org/images/logo.png",
            "GEO:geo:42.25\\,-91.05",
            "TZ:America/Chicago",
            "SOURCE:xmpp:jabber.org?vcard",
            "REV:19990104T122100Z",
            "KIND:application"
        };

        /// <summary>
        /// RFC 6869 §3: the example, which the RFC gives as a bare <c>vcard</c> element, inside the <c>vcards</c> element that RFC 6351 §5 says MUST be present in a document.  Its
        /// lines are those of the RFC, without its indentation of three spaces.
        /// </summary>
        private const string Rfc6869Example =
            "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\">\n" +
            "<vcard xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\">\n" +
            "  <kind><text>device</text></kind>\n" +
            "  <fn>\n" +
            "    <parameters>\n" +
            "      <type><text>x-model-name</text></type>\n" +
            "    </parameters>\n" +
            "    <text>RTR1001</text>\n" +
            "  </fn>\n" +
            "  <fn><text>core-rtr-1.example.net</text></fn>\n" +
            "  <url><uri>http://www.example.com/support/index.html</uri></url>\n" +
            "  <email><text>support@example.com</text></email>\n" +
            "  <email>\n" +
            "    <parameters>\n" +
            "      <type><text>x-local-support</text></type>\n" +
            "    </parameters>\n" +
            "    <text>network-support@example.net</text>\n" +
            "  </email>\n" +
            "  <impp><uri>xmpp:core-rtr-1@example.net</uri></impp>\n" +
            "  <related>\n" +
            "    <parameters>\n" +
            "      <type><text>contact</text></type>\n" +
            "    </parameters>\n" +
            "    <uri>urn:uuid:5CEF1870-0326-11E2-A21F-0800200C9A66</uri>\n" +
            "  </related>\n" +
            "  <logo><uri>http://www.example.com/images/logo.png</uri></logo>\n" +
            "  <geo><uri>geo:35.82,-78.64</uri></geo>\n" +
            "  <tz><text>America/New_York</text></tz>\n" +
            "  <rev><timestamp>20120104T213000Z</timestamp></rev>\n" +
            "  <uid><uri>urn:uuid:00CCFB88-155F-40F6-B9D9-B04D134860C0</uri></uid>\n" +
            "  <serial-number xmlns='http://example.org/profiles/serial-number'>\n" +
            "    FTX1234ABCD\n" +
            "  </serial-number>\n" +
            "  <note>\n" +
            "    <parameters>\n" +
            "      <type><text>x-contract-number</text></type>\n" +
            "    </parameters>\n" +
            "    <text>1234567</text>\n" +
            "  </note>\n" +
            "  <mac xmlns='http://example.org/profiles/mac'>\n" +
            "    00-00-5E-00-00-01\n" +
            "  </mac>\n" +
            "  <sw-version xmlns='http://example.org/profiles/sw-version'>\n" +
            "    2.1.5\n" +
            "  </sw-version>\n" +
            "</vcard>\n" +
            "</vcards>\n";

        /// <summary>
        /// The content lines of <see cref="Rfc6869Example"/>, as <c>VcardExtensionTests</c> gives them, except the XML properties.  Those keep the text of their elements as it is,
        /// line breaks and all (RFC 6351 §6: "wrapped as is"), and the reader writes their attributes in double quotes.
        /// </summary>
        private static readonly string[] Rfc6869ExampleProperties =
        {
            "KIND:device",
            "FN;TYPE=x-model-name:RTR1001",
            "FN:core-rtr-1.example.net",
            "URL:http://www.example.com/support/index.html",
            "EMAIL:support@example.com",
            "EMAIL;TYPE=x-local-support:network-support@example.net",
            "IMPP:xmpp:core-rtr-1@example.net",
            "RELATED;TYPE=contact:urn:uuid:5CEF1870-0326-11E2-A21F-0800200C9A66",
            "LOGO:http://www.example.com/images/logo.png",
            "GEO:geo:35.82\\,-78.64",
            "TZ:America/New_York",
            "REV:20120104T213000Z",
            "UID:urn:uuid:00CCFB88-155F-40F6-B9D9-B04D134860C0",
            "XML:<serial-number xmlns=\"http://example.org/profiles/serial-number\">\\n    FTX1234ABCD\\n  </serial-number>",
            "NOTE;TYPE=x-contract-number:1234567",
            "XML:<mac xmlns=\"http://example.org/profiles/mac\">\\n    00-00-5E-00-00-01\\n  </mac>",
            "XML:<sw-version xmlns=\"http://example.org/profiles/sw-version\">\\n    2.1.5\\n  </sw-version>"
        };

        [Fact]
        public void ReadCards_RfcXcardExample_GivesTheRfcProperties()
        {
            // act
            var testResult = Describe(ReadXcard(RfcXcardExample));

            // assert
            testResult.Should().BeEquivalentTo(new[] { RfcXcardExampleProperties }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void ReadCard_RfcXcardExample_IsVersion40()
        {
            // act
            var testResult = ReadXcard(RfcXcardExample)[0].SourceVersion;

            // assert
            testResult.Should().Be(VcardVersion.V4_0);
        }

        [Fact]
        public void WriteCards_RfcXcardExampleRead_WritesTheRfcExample()
        {
            // arrange
            var cards = ReadXcard(RfcXcardExample);

            // act
            var testResult = Canonical(WriteXcard(cards));

            // assert
            testResult.Should().Be(Canonical(RfcXcardExample));
        }

        [Fact]
        public void ReadCards_RfcConversionExample_ReadsTheSameCardAsItsVcardForm()
        {
            // arrange
            var expected = Describe(ReadVcards(ConversionExampleVcard));

            // act
            var testResult = Describe(ReadXcard(ConversionExample));

            // assert
            testResult.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        [Fact]
        public void WriteCards_RfcConversionExampleVcardForm_WritesTheRfcXcard()
        {
            // arrange
            var cards = ReadVcards(ConversionExampleVcard);

            // act
            var testResult = Canonical(WriteXcard(cards));

            // assert
            testResult.Should().Be(Canonical(ConversionExample));
        }

        [Fact]
        public void ReadCards_Rfc6473Example_GivesItsContentLines()
        {
            // act
            var testResult = Describe(ReadXcard(Rfc6473Example));

            // assert
            testResult.Should().BeEquivalentTo(new[] { Rfc6473ExampleProperties }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void ReadCards_Rfc6869Example_GivesItsContentLines()
        {
            // act
            var testResult = Describe(ReadXcard(Rfc6869Example));

            // assert
            testResult.Should().BeEquivalentTo(new[] { Rfc6869ExampleProperties }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void Kind_Rfc6869Example_IsDevice()
        {
            // act
            var testResult = ReadXcard(Rfc6869Example)[0].Kind;

            // assert
            testResult.Should().Be(VcardKind.Device);
        }
    }
}
