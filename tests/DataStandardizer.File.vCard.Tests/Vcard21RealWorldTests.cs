using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of vCard 2.1 cards in the style that common software exports them.  These are made-up cards, with made-up details.
    /// </summary>
    public class Vcard21RealWorldTests
    {
        // In the style of Outlook, which writes a LANGUAGE on N, PREF on an address with its label, and X-MS properties.
        private static readonly string OutlookExport = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N;LANGUAGE=en-us:Doe;Jane",
            "FN:Jane Doe",
            "ORG:Contoso;Sales",
            "TITLE:Manager",
            "TEL;WORK;VOICE:(425) 555-0100",
            "TEL;CELL;VOICE:(425) 555-0101",
            "ADR;WORK;PREF:;;1 Main Way;Redmond;WA;98052;United States of America",
            "LABEL;WORK;PREF;ENCODING=QUOTED-PRINTABLE:1 Main Way=0D=0ARedmond, WA 98052=0D=0AUnited States of America",
            "EMAIL;PREF;INTERNET:jane@example.com",
            "X-MS-OL-DEFAULT-POSTAL-ADDRESS:2",
            "REV:20201101T120000Z",
            "END:VCARD");

        // In the style of Android, which writes quoted-printable in UTF-8, a photo in BASE64 indented by a space, and X-ANDROID properties.
        private static readonly string AndroidExport = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:=C3=96zt=C3=BCrk;Ay=C5=9Fe;;;",
            "FN;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:Ay=C5=9Fe =C3=96zt=C3=BCrk",
            "TEL;CELL:+905551234567",
            "TEL;HOME:+902121234567",
            "EMAIL;HOME:ayse@example.com",
            "X-ANDROID-CUSTOM:vnd.android.cursor.item/nickname;Ay;1;;;;;;;;;;;;;",
            "PHOTO;ENCODING=BASE64;JPEG:/9j/4AAQ",
            " SkZJRgAB",
            "",
            "END:VCARD");

        // In the style of a Nokia phone, which writes quoted-printable in UTF-8 with soft line breaks, and X-EPOC properties.
        private static readonly string NokiaExport = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N;ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8:M=C3=BCller;Hans;;;",
            "TEL;PREF;CELL:+491701234567",
            "TEL;WORK:+49301234567",
            "NOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8:Zeile 1=0D=0AZeile 2 mit Stra=",
            "=C3=9Fe",
            "X-EPOCSECONDNAME:Hansi",
            "END:VCARD");

        // The same cards, as a writer of vCard 2.1 writes them back.
        private static readonly string WrittenOutlookExport = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N;LANGUAGE=en-us:Doe;Jane;;;",
            "FN:Jane Doe",
            "ORG:Contoso;Sales",
            "TITLE:Manager",
            "TEL;WORK;VOICE:(425) 555-0100",
            "TEL;CELL;VOICE:(425) 555-0101",
            "ADR;WORK;PREF:;;1 Main Way;Redmond;WA;98052;United States of America",
            "LABEL;WORK;PREF;ENCODING=QUOTED-PRINTABLE:1 Main Way=0D=0ARedmond, WA 9805=",
            "2=0D=0AUnited States of America",
            "EMAIL;INTERNET;PREF:jane@example.com",
            "X-MS-OL-DEFAULT-POSTAL-ADDRESS:2",
            "REV:2020-11-01T12:00:00Z",
            "END:VCARD");

        private static readonly string WrittenNokiaExport = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N;ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8:M=C3=BCller;Hans;;;",
            "TEL;CELL;PREF:+491701234567",
            "TEL;WORK:+49301234567",
            "NOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8:Zeile 1=0D=0AZeile 2 mit Stra=",
            "=C3=9Fe",
            "X-EPOCSECONDNAME:Hansi",
            "END:VCARD");

        public static TheoryData<string> ExportData()
        {
            return new TheoryData<string> { OutlookExport, AndroidExport, NokiaExport };
        }

        [Theory]
        [MemberData(nameof(ExportData))]
        public void ReadCard_Export_IsReadByDefault(string text)
        {
            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V2_1);
        }

        [Fact]
        public void ReadCard_OutlookExport_PutsTheLabelOnItsAddressAndMapsPreference()
        {
            // act
            var testResult = VcardCardTestData.Read(OutlookExport);

            // assert
            testResult.Addresses.Single().Label.Should().Be("1 Main Way\nRedmond, WA 98052\nUnited States of America");
            testResult.Addresses.Single().Pref.Should().Be(1);
            testResult.Emails.Single().Pref.Should().Be(1);
            testResult.GetProperties("X-MS-OL-DEFAULT-POSTAL-ADDRESS").Single().Value.Should().Be("2");
        }

        [Fact]
        public void ReadCard_AndroidExport_DecodesItsTextAndPhoto()
        {
            // act
            var testResult = VcardCardTestData.Read(AndroidExport);

            // assert
            testResult.FormattedNames.Single().Value.Should().Be("Ay\x015Fe \x00D6zt\x00FCrk");
            testResult.Photos.Single().Value.Text.Should().Be("data:image/jpeg;base64,/9j/4AAQSkZJRgAB");
        }

        [Fact]
        public void ReadCard_NokiaExport_JoinsTheSoftLineBreak()
        {
            // act
            var testResult = VcardCardTestData.Read(NokiaExport).Notes.Single().Value;

            // assert
            testResult.Should().Be("Zeile 1\nZeile 2 mit Stra\x00DFe");
        }

        public static TheoryData<string, string> WrittenBackData()
        {
            return new TheoryData<string, string> { { OutlookExport, WrittenOutlookExport }, { NokiaExport, WrittenNokiaExport } };
        }

        [Theory]
        [MemberData(nameof(WrittenBackData))]
        public void WriteCard_ExportAsVersion21_WritesTheSameData(string text, string expected)
        {
            // arrange
            var card = VcardCardTestData.Read(text);

            // act
            var testResult = Vcard21TestData.Write(card);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteCard_AndroidExportAsVersion40_WritesItsCanonicalForm()
        {
            // arrange
            var card = VcardCardTestData.Read(AndroidExport);

            // act
            var testResult = Vcard30TestData.Unfold(VcardCardTestData.Write(card));

            // assert
            testResult.Should().Be(VcardTestData.Crlf(
                "BEGIN:VCARD",
                "VERSION:4.0",
                "N:\x00D6zt\x00FCrk;Ay\x015Fe;;;",
                "FN:Ay\x015Fe \x00D6zt\x00FCrk",
                "TEL;TYPE=cell:+905551234567",
                "TEL;TYPE=home:+902121234567",
                "EMAIL;TYPE=home:ayse@example.com",
                "X-ANDROID-CUSTOM:vnd.android.cursor.item/nickname;Ay;1;;;;;;;;;;;;;",
                "PHOTO:data:image/jpeg;base64\\,/9j/4AAQSkZJRgAB",
                "END:VCARD"));
        }
    }
}
