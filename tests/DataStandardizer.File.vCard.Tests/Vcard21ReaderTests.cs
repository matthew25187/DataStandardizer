using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of reading vCard 2.1 cards (the versit specification of 1996) into the canonical form of the model.
    /// </summary>
    public class Vcard21ReaderTests
    {
        private static readonly string[] NoParameters = new string[0];

        // ref. vCard 2.1 §2.5.5: the example.
        private const string Version21Organization = "ORG:ABC, Inc.;North American Division;Marketing";

        // ref. vCard 2.1 §2.6.5: the example.
        private const string Version21Uid = "UID:19950401-080045-40000F192713-0052";

        // ref. vCard 2.1 §3.1.1: the card of the example, which has no FN.
        private static readonly string Version21SmithCard = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N:Smith;John;M.;Mr.;Esq.",
            "TEL;WORK;VOICE;MSG:+1 (919) 555-1234",
            "TEL;WORK;FAX:+1 (919) 555-9876",
            "ADR;WORK;PARCEL;POSTAL;DOM:Suite 101;1 Central St.;Any Town;NC;27654",
            "END:VCARD");

        // ref. vCard 2.1 §2.6.1: the example, whose soft line breaks join its lines, and whose second line starts with a space that is data.
        private static readonly string Version21Note = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N:Public;John",
            "NOTE;ENCODING=QUOTED-PRINTABLE:This facsimile machine if operational=",
            " 0830 to 1715 hours=0D=0A=",
            "Monday through Friday. Call +1-213-555-1234 if you have problems=0D=0A=",
            "with access to the machine.",
            "END:VCARD");

        private const string Version21NoteText = "This facsimile machine if operational 0830 to 1715 hours\nMonday through Friday. Call +1-213-555-1234 if you have problems\nwith access to the machine.";

        // ref. vCard 2.1 §2.3.2: the example of a domestic label, which goes with no address here.
        private static readonly string Version21Label = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N:Public;John",
            "LABEL;DOM;POSTAL;ENCODING=QUOTED-PRINTABLE:P. O. Box 456=0D=0A=",
            "123 Main Street=0D=0A=",
            "Any Town, CA 91921-1234",
            "END:VCARD");

        // ref. vCard 2.1 §2.2.3: the start of the example of an inline GIF, cut short there, shortened to whole base64 groups.
        private static readonly string Version21Photo = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:2.1",
            "N:Public;John",
            "PHOTO;ENCODING=BASE64;TYPE=GIF:",
            "    R0lGODdhfgA4AOYAAAAAAK+vr62trVIxa6WlpZ+fnzEpCEpzlAha/0Kc74+PjyGM",
            "    SuecKRhrtX9/fzExORBSjCEYCGtra2NjYyF7nDGE50JrhAg51qWtOTl7vee1MWu1",
            "",
            "END:VCARD");

        public static TheoryData<string> Version21ExampleData()
        {
            return new TheoryData<string>
            {
                // ref. vCard 2.1 §2.1.
                "A.TEL;HOME:+1-213-555-1234",
                "A.NOTE:This is my vacation home.",

                "ADR;LANGUAGE=fr-CA:...",
                "PHOTO;VALUE=URL;TYPE=GIF:http://www.abc.com/dir_photos/my_photo.gif",
                "SOUND;VALUE=CONTENT-ID:<jsmith.part3.960817T083000.xyzMail@host1.com>",

                // ref. vCard 2.1 §2.2 and §2.3.
                "FN:Mr. John Q. Public, Esq.",
                "N:Veni, Vidi, Vici;The Restaurant.",
                "PHOTO;VALUE=URL:file:///jqpublic.gif",
                "BDAY:19950415",
                "BDAY:1995-04-15",
                "ADR;DOM;HOME:P.O. Box 101;Suite 101;123 Main Street;Any Town;CA;91921-1234;",
                "ADR;DOM;WORK;HOME;POSTAL:P.O. Box 101;;;Any Town;CA;91921-1234;",

                // ref. vCard 2.1 §2.4.
                "TEL;PREF;WORK;MSG;FAX:+1-800-555-1234",
                "TEL;WORK;HOME;VOICE;FAX:+1-800-555-1234",
                "EMAIL;INTERNET:john.public@abc.com",
                "MAILER:ccMail 2.2",
                "GEO:37.24,-17.87",

                // ref. vCard 2.1 §2.5 and §2.6.
                Version21Organization,
                "REV:19951031T222710",
                "REV:1995-10-31T22:27:10Z",
                "SOUND:JON Q PUBLIK",
                "URL:http://abc.com/pub/directory/northam/jpublic.ecd",
                Version21Uid,

                // ref. vCard 2.1 §2.8.1.
                "X-ABC-VIDEO;MPEG2:http://lonestar.bubbas.org/billibob.mpg"
            };
        }

        [Theory]
        [MemberData(nameof(Version21ExampleData))]
        public void ReadCard_Version21Example_IsAccepted(string line)
        {
            // arrange
            var text = Vcard21TestData.Card(line);

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V2_1);
        }

        public static TheoryData<string> WholeCardExampleData()
        {
            return new TheoryData<string> { Version21SmithCard, Version21Note, Version21Label, Version21Photo };
        }

        [Theory]
        [MemberData(nameof(WholeCardExampleData))]
        public void ReadCard_WholeCardExample_IsAccepted(string text)
        {
            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V2_1);
        }

        [Fact]
        public void ReadCard_QuotedPrintableAcrossSoftLineBreaks_GivesTheTextAsOne()
        {
            // arrange
            var text = Version21Note;

            // act
            var testResult = VcardCardTestData.Read(text).Notes.Single().Value;

            // assert
            testResult.Should().Be(Version21NoteText);
        }

        [Fact]
        public void ReadCard_InlineBinary_GivesADataUri()
        {
            // arrange
            var text = Version21Photo;

            // act
            var testResult = VcardCardTestData.Read(text).Properties.Last().Value;

            // assert
            testResult.Should().Be("data:image/gif;base64\\,R0lGODdhfgA4AOYAAAAAAK+vr62trVIxa6WlpZ+fnzEpCEpzlAha/0Kc74+PjyGMSuecKRhrtX9/fzExORBSjCEYCGtra2NjYyF7nDGE50JrhAg51qWtOTl7vee1MWu1");
        }

        public static TheoryData<string, string[], string> CanonicalLineData()
        {
            return new TheoryData<string, string[], string>
            {
                // A knowntype in lower case, and PREF as RFC 6350 §5.3 writes it.
                { "TEL;WORK;VOICE;MSG:+1 (919) 555-1234", new[] { "TYPE=work,voice,msg" }, "+1 (919) 555-1234" },
                { "TEL;PREF;CELL:+1-555", new[] { "TYPE=cell", "PREF=1" }, "+1-555" },
                { "TEL;TYPE=WORK;TYPE=PREF:+1-555", new[] { "TYPE=work", "PREF=1" }, "+1-555" },
                { "EMAIL;PREF;INTERNET:a@example.com", new[] { "TYPE=internet", "PREF=1" }, "a@example.com" },

                // Text escaped as RFC 6350 §3.4 says, where vCard 2.1 escapes nothing outside a compound value.
                { "NOTE:a,b\\c", NoParameters, "a\\,b\\\\c" },
                { "FN:Mr. John Q. Public, Esq.", NoParameters, "Mr. John Q. Public\\, Esq." },

                // Compound values: a comma is a character of a component, and \; a semicolon (§2.9).
                { "N:Veni, Vidi, Vici;The Restaurant.", NoParameters, "Veni\\, Vidi\\, Vici;The Restaurant.;;;" },
                { "N:a\\;b;c", NoParameters, "a\\;b;c;;;" },
                { Version21Organization, NoParameters, "ABC\\, Inc.;North American Division;Marketing" },

                // Encodings decoded, with their parameters gone.
                { "NOTE;CHARSET=ISO-8859-1;ENCODING=QUOTED-PRINTABLE:Caf=E9", NoParameters, "Caf\x00E9" },
                { "NOTE;CHARSET=UTF-8;ENCODING=QUOTED-PRINTABLE:Caf=C3=A9", NoParameters, "Caf\x00E9" },
                { "NOTE;QUOTED-PRINTABLE:a=0D=0Ab", NoParameters, "a\\nb" },

                // Dates and times of RFC 6350 §4.3, and its GEO and TZ.
                { "BDAY:1995-04-15", NoParameters, "19950415" },
                { "REV:1995-10-31T22:27:10Z", NoParameters, "19951031T222710Z" },
                { "TZ:-0500", new[] { "VALUE=utc-offset" }, "-0500" },
                { "GEO:37.24,-17.87", NoParameters, "geo:37.24\\,-17.87" },

                // URLs, and a Content-ID as a cid URI (RFC 2392 §2).
                { "URL:http://example.com/a,b", NoParameters, "http://example.com/a\\,b" },
                { "PHOTO;VALUE=URL;TYPE=GIF:http://example.com/a.gif", new[] { "MEDIATYPE=image/gif" }, "http://example.com/a.gif" },
                { "SOUND;VALUE=CONTENT-ID:<jsmith.part3@host1.com>", NoParameters, "cid:jsmith.part3@host1.com" },
                { "SOUND;VALUE=CID:<a b@host1.com>", NoParameters, "cid:a%20b@host1.com" },
                { "SOUND;VALUE=CID:<a#b?c@host1.com>", NoParameters, "cid:a%23b%3Fc@host1.com" },

                // Binary, ending at an empty line (§2.9), and text where vCard 2.1 allows it (§2.6.3, §2.7.1).
                { "SOUND;WAVE;BASE64:AQID\r\n", NoParameters, "data:audio/vnd.wave;base64\\,AQID" },
                { "KEY;ENCODING=BASE64:AQID\r\n", NoParameters, "data:application/octet-stream;base64\\,AQID" },
                { "PHOTO;ENCODING=BASE64;TYPE=PICT:AQID\r\n", new[] { "TYPE=pict" }, "data:application/octet-stream;base64\\,AQID" },

                // Quoted-printable is binary where a TYPE names a format (§2.1.9), whether or not it has a media type, and otherwise text.
                { "PHOTO;ENCODING=QUOTED-PRINTABLE;TYPE=PICT:=01=02=03", new[] { "TYPE=pict" }, "data:application/octet-stream;base64\\,AQID" },
                { "SOUND;WORK;ENCODING=QUOTED-PRINTABLE:JON", new[] { "VALUE=text", "TYPE=work" }, "JON" },
                { "SOUND:JON Q PUBLIK", new[] { "VALUE=text" }, "JON Q PUBLIK" },
                { "KEY;PGP:abc", new[] { "VALUE=text", "TYPE=pgp" }, "abc" },

                // UID: RFC 6350 §6.7.6, whose default is a URI.
                { Version21Uid, new[] { "VALUE=text" }, "19950401-080045-40000F192713-0052" },
                { "UID:urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6", NoParameters, "urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6" },

                // An X- location (§2.9, pvalueval) says nothing vCard 4.0 can, so the line stays as it is.
                { "UID;VALUE=X-REF:a1", new[] { "VALUE=X-REF" }, "a1" },

                // An extension is kept as it was.
                { "X-ABC-VIDEO;MPEG2:http://lonestar.bubbas.org/billibob.mpg", new[] { "TYPE=mpeg2" }, "http://lonestar.bubbas.org/billibob.mpg" },
                { "X-A;X-B=c:a\\,b", new[] { "X-B=c" }, "a\\,b" },
                { "MAILER:ccMail 2.2", NoParameters, "ccMail 2.2" }
            };
        }

        [Theory]
        [MemberData(nameof(CanonicalLineData))]
        public void ReadCard_Version21Line_GivesItInCanonicalForm(string line, string[] expectedParameters, string expectedValue)
        {
            // arrange
            var text = Vcard21TestData.Card(line);

            // act
            var testResult = VcardCardTestData.Read(text).Properties.Last();

            // assert
            Vcard30TestData.ParametersOf(testResult).Should().Equal(expectedParameters);
            testResult.Value.Should().Be(expectedValue);
        }

        [Fact]
        public void ReadCard_LabelWithTheTypesOfAnAddress_GoesOnThatAddress()
        {
            // arrange
            var text = Vcard21TestData.Card("ADR;DOM;HOME:;;123 Main Street;Any Town;CA;91921-1234;", "LABEL;HOME;DOM;ENCODING=QUOTED-PRINTABLE:123 Main Street=0D=0AAny Town");

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.GetProperties(VcardPropertyNames.PostalLabel).Should().BeEmpty();
            testResult.Addresses.Single().Label.Should().Be("123 Main Street\nAny Town");
        }

        [Fact]
        public void ReadCard_TypedMembers_GiveTheValuesOfTheCard()
        {
            // arrange
            var text = Version21SmithCard;

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.Name.Value.HonorificSuffixes.Should().Equal("Esq.");
            testResult.Telephones.Select(telephone => telephone.Value.Text).Should().Equal("+1 (919) 555-1234", "+1 (919) 555-9876");
            // The example has five of the seven fields of §2.3.1, which are positional, so its town is in the street's field and its state in the locality's.
            testResult.Addresses.Single().Value.Locality.Should().Equal("NC");
        }

        public static TheoryData<string, string> LanguageTagData()
        {
            return new TheoryData<string, string>
            {
                // ref. vCard 2.1 §2.1.7: its example, and an RFC 1766 tag that RFC 5646 keeps only as grandfathered (#240).
                { "NOTE;LANGUAGE=fr-CA:a", "fr-CA" },
                { "NOTE;LANGUAGE=i-klingon:a", "i-klingon" }
            };
        }

        [Theory]
        [MemberData(nameof(LanguageTagData))]
        public void ReadCard_LanguageTagOfRfc1766_IsKept(string line, string tag)
        {
            // arrange
            var text = Vcard21TestData.Card(line);

            // act
            var testResult = VcardCardTestData.Read(text).Notes.Single().Language;

            // assert
            testResult.ToString().Should().Be(tag);
        }

        [Fact]
        public void ReadCard_LanguageTagWithDigits_ThrowsInvalidParameter()
        {
            // arrange
            // ref. RFC 1766 §2: subtags are letters, and digits came with RFC 4646.
            var text = Vcard21TestData.Card("NOTE;LANGUAGE=es-419:a");

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidParameter);
        }

        public static TheoryData<string, VcardDiagnosticKind> NonconformingLineData()
        {
            return new TheoryData<string, VcardDiagnosticKind>
            {
                // ref. vCard 2.1 §2.9: name has a closed list, and an extension starts with X- (§2.8.1).
                { "NICKNAME:Jim", VcardDiagnosticKind.InvalidValue },
                { "CATEGORIES:a", VcardDiagnosticKind.InvalidValue },

                // ref. vCard 2.1 §2.9: ptypeval = knowntype / "X-" word, and a param has one value.
                { "TEL;TYPE=IPHONE:1", VcardDiagnosticKind.InvalidParameter },
                { "TEL;TYPE=WORK,VOICE:1", VcardDiagnosticKind.InvalidParameter },
                { "TEL;PID=1:1", VcardDiagnosticKind.InvalidParameter },
                { "PHOTO;VALUE=uri:http://example.com/a.gif", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;ENCODING=b:YQ==", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;ENCODING=QUOTED-PRINTABLE;ENCODING=8BIT:a", VcardDiagnosticKind.InvalidParameter },

                // Values of the types of vCard 2.1.
                { "BDAY:--0415", VcardDiagnosticKind.InvalidValue },
                { "REV:1995-10-31", VcardDiagnosticKind.InvalidValue },
                { "GEO:37.24;-17.87", VcardDiagnosticKind.InvalidValue },
                { "URL:not a url", VcardDiagnosticKind.InvalidValue },
                { "PHOTO;GIF:R0lGOD", VcardDiagnosticKind.InvalidValue },
                { "NOTE:Caf\x00E9", VcardDiagnosticKind.InvalidValue },

                // ref. vCard 2.1 §2.5.4: the value of an AGENT is a card.
                { "AGENT:Fred", VcardDiagnosticKind.InvalidValue }
            };
        }

        [Theory]
        [MemberData(nameof(NonconformingLineData))]
        public void ReadCard_LineVersion21DoesNotAllow_Throws(string line, VcardDiagnosticKind expected)
        {
            // arrange
            var text = Vcard21TestData.Card(line);

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(expected);
        }

        [Fact]
        public void ReadCard_PropertyNameOfALaterVersionWithAllowInvalidValues_KeepsIt()
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = null, AllowInvalidValues = true };
            var text = Vcard21TestData.Card("NICKNAME:Jim");

            // act
            var testResult = VcardCardTestData.Read(text, options).Nicknames.Single().Value;

            // assert
            testResult.Should().Equal("Jim");
        }

        [Fact]
        public void ReadCard_CardWithNoName_ThrowsMissingRequiredProperty()
        {
            // arrange
            // ref. vCard 2.1 §2.2.2: support for N "is mandatory", where FN is optional (§2.2.1).
            var text = Vcard21TestData.BareCard("FN:Jane Doe");

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.MissingRequiredProperty);
        }

        [Fact]
        public void ReadCard_CardWithNoNameWithAllowMissingRequiredProperties_IsRead()
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = null, AllowMissingRequiredProperties = true };
            var text = Vcard21TestData.BareCard("FN:Jane Doe");

            // act
            var testResult = VcardCardTestData.Read(text, options).FormattedNames.Single().Value;

            // assert
            testResult.Should().Be("Jane Doe");
        }

        [Fact]
        public void ReadCard_CardWithOnlyAName_NeedsNoFormattedName()
        {
            // arrange
            var text = Vcard21TestData.Card();

            // act
            var testResult = VcardCardTestData.Read(text).Name.Value.FamilyNames;

            // assert
            testResult.Should().Equal("Doe");
        }

        [Fact]
        public void ReadCard_EightBitWithAcceptEightBitWithoutEncoding_DecodesIt()
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = null, AcceptEightBitWithoutEncoding = true };
            var text = Vcard21TestData.Card("NOTE;CHARSET=UTF-8:Caf\x00E9");

            // act
            var testResult = VcardCardTestData.Read(text, options).Notes.Single().Value;

            // assert
            testResult.Should().Be("Caf\x00E9");
        }

        public static TheoryData<string> StructureData()
        {
            return new TheoryData<string>
            {
                // ref. vCard 2.1 §2.9: vcard = "BEGIN" [ws] ":" [ws] "VCARD" ..., with no group.
                VcardTestData.Crlf("a.BEGIN:VCARD", "VERSION:2.1", "N:a", "END:VCARD"),
                VcardTestData.Crlf("BEGIN:VCARD", "VERSION:2.1", "N:a", "a.END:VCARD"),

                // A card inside a card that no AGENT holds, as the distribution list of §2.8.1 has, which the grammar of §2.9 doesn't allow.
                VcardTestData.Crlf("BEGIN:VCARD", "VERSION:2.1", "N:a", "BEGIN:VCARD", "N:b", "END:VCARD", "END:VCARD")
            };
        }

        [Theory]
        [MemberData(nameof(StructureData))]
        public void ReadCard_StructureVersion21DoesNotAllow_ThrowsInvalidStructure(string text)
        {
            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidStructure);
        }

        [Fact]
        public void ReadCard_VersionNotFirst_IsAccepted()
        {
            // arrange
            // ref. vCard 2.1 §2.9: VERSION is one of the items, in any place.
            var text = VcardTestData.Crlf("BEGIN:VCARD", "N:a", "VERSION:2.1", "END:VCARD");

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V2_1);
        }
    }
}
