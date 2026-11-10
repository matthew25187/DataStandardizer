using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of vCard 3.0 cards in the style that common software exports them.  These are made-up cards, with made-up details.
    /// </summary>
    public class Vcard30RealWorldTests
    {
        // In the style of Apple Contacts, which has groups, repeated TYPE parameters and X-AB properties.
        private static readonly string AppleExport = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:3.0",
            "PRODID:-//Apple Inc.//Mac OS X 10.15.7//EN",
            "N:Doe;Jane;;;",
            "FN:Jane Doe",
            "ORG:Example Inc.;",
            "EMAIL;type=INTERNET;type=HOME;type=pref:jane@example.com",
            "TEL;type=CELL;type=VOICE;type=pref:+1 555 555 0100",
            "item1.ADR;type=HOME;type=pref:;;1 Main St;Town;CA;90210;USA",
            "item1.X-ABADR:us",
            "item2.X-ABLabel:_$!<Other>!$_",
            "BDAY:1980-05-01",
            "PHOTO;ENCODING=b;TYPE=JPEG:AQID",
            "X-ABShowAs:COMPANY",
            "REV:2020-01-02T03:04:05Z",
            "END:VCARD");

        // In the style of Google Contacts, which has no parameters beyond TYPE.
        private static readonly string GoogleExport = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:3.0",
            "FN:John Smith",
            "N:Smith;John;;;",
            "EMAIL;TYPE=INTERNET;TYPE=WORK:john@example.com",
            "TEL;TYPE=CELL:+1 555-0101",
            "ADR;TYPE=HOME:;;123 Main St;Springfield;IL;62701;US",
            "ORG:Acme",
            "TITLE:Engineer",
            "CATEGORIES:myContacts",
            "NOTE:Line one\\nLine two",
            "END:VCARD");

        // In the style of Outlook and Android, which write quoted-printable in a vCard 3.0 card.
        private static readonly string OutlookExport = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:3.0",
            "N;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:M=C3=BCller;Anna;;;",
            "FN;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:Anna M=C3=BCller",
            "TEL;WORK;VOICE:+49 30 555 0102",
            "NOTE;ENCODING=QUOTED-PRINTABLE:first=",
            "second",
            "END:VCARD");

        public static TheoryData<string> StrictExportData()
        {
            return new TheoryData<string> { AppleExport, GoogleExport };
        }

        [Theory]
        [MemberData(nameof(StrictExportData))]
        public void ReadCard_ExportThatFollowsTheRfc_IsReadByDefault(string text)
        {
            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V3_0);
        }

        [Fact]
        public void ReadCard_AppleExport_MapsPreferenceAndKeepsGroupsAndExtensions()
        {
            // act
            var testResult = VcardCardTestData.Read(AppleExport);

            // assert
            testResult.Emails.Single().Pref.Should().Be(1);
            testResult.Emails.Single().Types.Should().Equal("INTERNET", "HOME");
            testResult.Addresses.Single().Group.Should().Be("item1");
            testResult.Photos.Single().Value.Text.Should().Be("data:image/jpeg;base64,AQID");
            testResult.ExtensionProperties.Should().HaveCount(3);
        }

        [Fact]
        public void WriteCard_AppleExport_RoundTrips()
        {
            // arrange
            var card = VcardCardTestData.Read(AppleExport);

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testResult.Should().Be(VcardTestData.Crlf(
                "BEGIN:VCARD",
                "VERSION:3.0",
                "PRODID:-//Apple Inc.//Mac OS X 10.15.7//EN",
                "N:Doe;Jane;;;",
                "FN:Jane Doe",
                "ORG:Example Inc.;",
                "EMAIL;TYPE=INTERNET,HOME,pref:jane@example.com",
                "TEL;TYPE=CELL,VOICE,pref:+1 555 555 0100",
                "item1.ADR;TYPE=HOME,pref:;;1 Main St;Town;CA;90210;USA",
                "item1.X-ABADR:us",
                "item2.X-ABLabel:_$!<Other>!$_",
                "BDAY:1980-05-01",
                "PHOTO;ENCODING=b;TYPE=JPEG:AQID",
                "X-ABShowAs:COMPANY",
                "REV:2020-01-02T03:04:05Z",
                "END:VCARD"));
        }

        [Fact]
        public void WriteCard_GoogleExportAsVersion40_ReadsBackConforming()
        {
            // arrange
            var card = VcardCardTestData.Read(GoogleExport);
            var written = Vcard30TestData.Write(card, VcardVersion.V4_0);

            // act
            var testResult = VcardCardTestData.Read(written);

            // assert
            testResult.Validate().Should().BeEmpty();
            testResult.Notes.Single().Value.Should().Be("Line one\nLine two");
        }

        [Fact]
        public void ReadCard_OutlookExportByDefault_IsRejected()
        {
            // act
            System.Action testAction = () => VcardCardTestData.Read(OutlookExport);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Fact]
        public void ReadCard_OutlookExportUnderAcceptLegacySyntax_DecodesIntoCanonicalForm()
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = null, AcceptLegacySyntax = true };

            // act
            var testResult = VcardCardTestData.Read(OutlookExport, options);

            // assert
            testResult.FormattedNames.Single().Value.Should().Be("Anna M\xFCller");
            testResult.Telephones.Single().Types.Should().Equal("WORK", "VOICE");
            testResult.Notes.Single().Value.Should().Be("firstsecond");
        }

        [Fact]
        public void WriteCard_OutlookExportUnderAcceptLegacySyntax_WritesNoLegacyForms()
        {
            // arrange
            var card = VcardCardTestData.Read(OutlookExport, new VcardOptions { MaxLineLength = null, AcceptLegacySyntax = true });

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testResult.Should().Contain("FN:Anna M\xFCller").And.Contain("TEL;TYPE=WORK;TYPE=VOICE:+49 30 555 0102").And.NotContain("QUOTED-PRINTABLE");
        }
    }
}
