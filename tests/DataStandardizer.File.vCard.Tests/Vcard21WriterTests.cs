using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of writing cards as vCard 2.1, from the canonical form of the model.
    /// </summary>
    public class Vcard21WriterTests
    {
        // ref. vCard 2.1 §3.1.1: the card of the example.
        private static readonly string Version21SmithCard = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N:Smith;John;M.;Mr.;Esq.",
            "TEL;WORK;VOICE;MSG:+1 (919) 555-1234",
            "TEL;WORK;FAX:+1 (919) 555-9876",
            "ADR;WORK;PARCEL;POSTAL;DOM:Suite 101;1 Central St.;Any Town;NC;27654",
            "END:VCARD");

        // What a writer writes for it: the components a compound value has, and no fold.
        private static readonly string WrittenSmithCard = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N:Smith;John;M.;Mr.;Esq.",
            "TEL;WORK;VOICE;MSG:+1 (919) 555-1234",
            "TEL;WORK;FAX:+1 (919) 555-9876",
            "ADR;WORK;PARCEL;POSTAL;DOM:Suite 101;1 Central St.;Any Town;NC;27654;;",
            "END:VCARD");

        [Fact]
        public void WriteCard_Version21Card_WritesItBack()
        {
            // arrange
            var card = VcardCardTestData.Read(Version21SmithCard);

            // act
            var testResult = Vcard21TestData.Write(card);

            // assert
            testResult.Should().Be(WrittenSmithCard);
        }

        [Fact]
        public void WriteCard_NamedTypesOption_WritesTypeBeforeEachKnownType()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard21TestData.Card("TEL;WORK;VOICE:1"));

            // act
            var testResult = Vcard21TestData.Write(card, writeNamedTypes: true);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:2.1\r\nN:Doe;Jane;;;\r\nTEL;TYPE=WORK;TYPE=VOICE:1\r\nEND:VCARD\r\n");
        }

        public static TheoryData<string, string> WrittenLineData()
        {
            return new TheoryData<string, string>
            {
                // ref. vCard 2.1 §2.1.3 and §2.1.6: text outside US-ASCII in quoted-printable, with its character set.
                { "NOTE:Caf\x00E9", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nNOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8:Caf=C3=A9\r\nEND:VCARD\r\n" },

                // A line break as =0D=0A, as in the example of §2.1.3, and a comma or a backslash as it is.
                { "NOTE:a\\nb", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nNOTE;ENCODING=QUOTED-PRINTABLE:a=0D=0Ab\r\nEND:VCARD\r\n" },
                { "NOTE:a\\,b\\\\c", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nNOTE:a,b\\c\r\nEND:VCARD\r\n" },

                // A value of 76 characters, whose line is too long to write plainly, broken with soft line breaks, which are read the same way by any reader, and never folded.
                { "NOTE:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwx", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nNOTE;ENCODING=QUOTED-PRINTABLE:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopq=\r\nrstuvwxyzabcdefghijklmnopqrstuvwx\r\nEND:VCARD\r\n" },

                // A preference as the knowntype PREF (§2.4.1), and every TYPE value as a parameter of its own (§2.9).
                { "TEL;TYPE=work,voice;PREF=1:+1-555", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nTEL;WORK;VOICE;PREF:+1-555\r\nEND:VCARD\r\n" },
                { "EMAIL;TYPE=internet,x-custom:a@example.com", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nEMAIL;INTERNET;TYPE=x-custom:a@example.com\r\nEND:VCARD\r\n" },
                { "TEL;VALUE=uri:tel:+1-555-555-5555", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nTEL:+1-555-555-5555\r\nEND:VCARD\r\n" },

                // Values in the forms of vCard 2.1.
                { "BDAY:19950415", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nBDAY:1995-04-15\r\nEND:VCARD\r\n" },
                { "REV:19951031T222710Z", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nREV:1995-10-31T22:27:10Z\r\nEND:VCARD\r\n" },
                { "TZ;VALUE=utc-offset:-0500", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nTZ:-05:00\r\nEND:VCARD\r\n" },
                { "GEO:geo:37.24\\,-17.87", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nGEO:37.24,-17.87\r\nEND:VCARD\r\n" },
                { "URL:http://example.com/a\\,b", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nURL:http://example.com/a,b\r\nEND:VCARD\r\n" },
                { "UID;VALUE=text:a\\,b", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nUID:a,b\r\nEND:VCARD\r\n" },
                { "ORG:ABC\\, Inc.;Marketing", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nORG:ABC, Inc.;Marketing\r\nEND:VCARD\r\n" },
                { "MAILER:ccMail 2.2", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nMAILER:ccMail 2.2\r\nEND:VCARD\r\n" },

                // Media: binary in BASE64 with the empty line that ends it, a URL, and a Content-ID (RFC 2392 §2).
                { "PHOTO:data:image/gif;base64\\,AQID", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nPHOTO;GIF;ENCODING=BASE64:\r\n    AQID\r\n\r\nEND:VCARD\r\n" },
                { "PHOTO;MEDIATYPE=image/jpeg:http://example.com/a.jpg", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nPHOTO;VALUE=URL;JPEG:http://example.com/a.jpg\r\nEND:VCARD\r\n" },
                { "SOUND:cid:a%20b@host1.com", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nSOUND;VALUE=CONTENT-ID:<a b@host1.com>\r\nEND:VCARD\r\n" },

                { "KEY;VALUE=text;TYPE=pgp:abc", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nKEY;PGP:abc\r\nEND:VCARD\r\n" },

                // A group, and an extension as it is.
                { "a.NOTE:b", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\na.NOTE:b\r\nEND:VCARD\r\n" },
                { "X-A;X-B=c:a\\,b", "BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nX-A;X-B=c:a\\,b\r\nEND:VCARD\r\n" }
            };
        }

        [Theory]
        [MemberData(nameof(WrittenLineData))]
        public void WriteCard_CanonicalLine_WritesItInTheFormOfVersion21(string line, string expected)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N:Doe;Jane;;;", line));

            // act
            var testResult = Vcard21TestData.Write(card);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteCard_Version21SoundAsText_WritesItBack()
        {
            // arrange
            // ref. vCard 2.1 §2.6.3: the example of "a string representation", which vCard 4.0 can't hold.
            var card = VcardCardTestData.Read(Vcard21TestData.Card("SOUND:JON Q PUBLIK"));

            // act
            var testResult = Vcard21TestData.Write(card);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:2.1\r\nN:Doe;Jane;;;\r\nSOUND:JON Q PUBLIK\r\nEND:VCARD\r\n");
        }

        [Fact]
        public void WriteCard_AddressWithALabel_WritesTheLabelAfterIt()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N:Doe;Jane;;;", "ADR;TYPE=home;LABEL=\"1 Main St^nTown\":;;1 Main St;Town;;;"));

            // act
            var testResult = Vcard21TestData.Write(card);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nADR;HOME:;;1 Main St;Town;;;\r\nLABEL;HOME;ENCODING=QUOTED-PRINTABLE:1 Main St=0D=0ATown\r\nEND:VCARD\r\n");
        }

        public static TheoryData<string> LossyLineData()
        {
            return new TheoryData<string>
            {
                // Properties vCard 2.1 doesn't define, and a name that isn't an extension's (§2.9).
                "NICKNAME:Jim",
                "KIND:individual",
                "CATEGORIES:a",
                "RELATED:http://example.com/friend",
                "FOO:bar",

                // Parameters and TYPE values it doesn't have.
                "TEL;PID=1:+1-555",
                "NOTE;LANGUAGE=es-419:a",
                "TEL;TYPE=text:+1-555",
                "TEL;PREF=1:+1-555\r\nTEL;PREF=2:+1-556",
                "N;SORT-AS=Doe:Doe;Jane;;;",

                // Values it can't hold.
                "PHOTO:data:image/png;base64\\,AQID",
                "BDAY:--0415",
                "BDAY;VALUE=text:circa 1800",
                "TZ:America/New_York",
                "ADR:;;a,b;;;;",
                "N:a\\\\;Jane;;;"
            };
        }

        [Theory]
        [MemberData(nameof(LossyLineData))]
        public void WriteCard_WhatVersion21CanNotHold_ThrowsLossy(string line)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N:Doe;Jane;;;", line), new VcardOptions { AllowCardinalityViolations = true });

            // act
            Action testAction = () => Vcard21TestData.Write(card);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.Lossy);
        }

        [Theory]
        [MemberData(nameof(LossyLineData))]
        public void Analyze_WhatVersion21CanNotHold_ListsItAsLossy(string line)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N:Doe;Jane;;;", line), new VcardOptions { AllowCardinalityViolations = true });

            // act
            var testResult = VcardVersionConverter.Analyze(card, VcardVersion.V2_1);

            // assert
            testResult.Should().NotBeEmpty().And.OnlyContain(diagnostic => diagnostic.Kind == VcardDiagnosticKind.Lossy);
        }

        [Fact]
        public void WriteCard_AllowLossyVersionConversion_LeavesOutWhatVersion21CanNotHold()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N:Doe;Jane;;;", "NICKNAME:Jim", "PHOTO:data:image/png;base64\\,AQID"));

            // act
            var testResult = Vcard21TestData.Write(card, allowLossy: true);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nN:Doe;Jane;;;\r\nPHOTO;ENCODING=BASE64:\r\n    AQID\r\n\r\nEND:VCARD\r\n");
        }

        [Fact]
        public void WriteCard_CardWithNoName_ThrowsMissingRequiredProperty()
        {
            // arrange
            // ref. vCard 2.1 §2.2.2: support for N "is mandatory".
            var card = VcardCardTestData.Read(VcardCardTestData.Card());

            // act
            Action testAction = () => Vcard21TestData.Write(card);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.MissingRequiredProperty);
        }

        [Fact]
        public void WriteCard_CardWithNoNameWithAllowMissingRequiredProperties_WritesIt()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card());

            // act
            var testResult = Vcard21TestData.Write(card, allowMissingRequired: true);

            // assert
            testResult.Should().Be("BEGIN:VCARD\r\nVERSION:2.1\r\nFN:Jane Doe\r\nEND:VCARD\r\n");
        }

        public static TheoryData<string> RoundTripData()
        {
            return new TheoryData<string>
            {
                "NOTE:Caf\x00E9 \\, \\\\ ; a\\nb",
                "ORG:ABC\\, Inc.;Caf\x00E9",
                "ADR;TYPE=home,pref:;;1 Main St\\nFlat 2;Town;;;",
                "ADR:;;1 Main St\\; Flat 2;Town;;;",
                "PHOTO:data:image/jpeg;base64\\,AQID",
                "TEL;TYPE=cell:+1-555",
                "X-A:Caf\x00E9"
            };
        }

        [Theory]
        [MemberData(nameof(RoundTripData))]
        public void WriteCard_ThenReadCard_GivesTheSameLines(string line)
        {
            // arrange
            var text = VcardCardTestData.Card("N:Doe;Jane;;;", line);
            var card = VcardCardTestData.Read(text);

            // act
            var testResult = VcardCardTestData.Read(Vcard21TestData.Write(card));

            // assert
            VcardCardTestData.NamesAndValuesOf(testResult).Should().Equal(VcardCardTestData.NamesAndValuesOf(card));
        }

        [Fact]
        public void Convert_Version21_GivesACardOfThatVersion()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N:Doe;Jane;;;", "TEL;TYPE=cell;PREF=1:+1-555"));

            // act
            var testResult = VcardVersionConverter.Convert(card, VcardVersion.V2_1);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V2_1);
            VcardCardTestData.NamesAndValuesOf(testResult).Should().Equal(VcardCardTestData.NamesAndValuesOf(card));
        }

        [Fact]
        public void Validate_Version21_ReportsTheSameAsAWriterWould()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("KIND:group"));

            // act
            var testResult = card.Validate(VcardVersion.V2_1);

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(VcardDiagnosticKind.Lossy, VcardDiagnosticKind.MissingRequiredProperty);
        }
    }
}
