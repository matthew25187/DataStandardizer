using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of writing vCard 3.0 with <see cref="VcardOptions.WriteVersion"/>, and of converting between versions.
    /// </summary>
    public class Vcard30WriterTests
    {
        private const string Base64 = "MIICajCC";
        private const string MinimalVersion40Name = "N:Doe;Jane;;;";

        // Each of the data below is read from a card of vCard 4.0, with this so that it gets as far as the conversion.
        private static readonly VcardOptions AcceptEverything = new VcardOptions { AllowInvalidValues = true, AllowCardinalityViolations = true };

        public static TheoryData<string, string> LosslessRoundTripData()
        {
            return new TheoryData<string, string>
            {
                // Written as they were read.
                { "TEL;TYPE=work,voice:+1-213-555-1234", "TEL;TYPE=work,voice:+1-213-555-1234" },
                { "EMAIL;TYPE=internet:jqpublic@xyz.dom1.com", "EMAIL;TYPE=internet:jqpublic@xyz.dom1.com" },
                { "MAILER:PigeonMail 2.1", "MAILER:PigeonMail 2.1" },
                { "TZ:-05:00", "TZ:-05:00" },
                { "GEO:37.386013;-122.082932", "GEO:37.386013;-122.082932" },
                { "TITLE:Director\\, Research and Development", "TITLE:Director\\, Research and Development" },
                { "AGENT;VALUE=uri:CID:JQPUBLIC.part3.960129T083020.xyzMail@host3.com", "AGENT;VALUE=uri:CID:JQPUBLIC.part3.960129T083020.xyzMail@host3.com" },
                { "ORG:ABC\\, Inc.;North American Division;Marketing", "ORG:ABC\\, Inc.;North American Division;Marketing" },
                { "CATEGORIES:INTERNET,IETF", "CATEGORIES:INTERNET,IETF" },
                { "NOTE:a\\nb\\;c", "NOTE:a\\nb\\;c" },
                { "PRODID:-//ONLINE DIRECTORY//NONSGML Version 1//EN", "PRODID:-//ONLINE DIRECTORY//NONSGML Version 1//EN" },
                { "REV:1995-10-31T22:27:10Z", "REV:1995-10-31T22:27:10Z" },
                { "UID:19950401-080045-40000F192713-0052", "UID:19950401-080045-40000F192713-0052" },
                { "UID:urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6", "UID:urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6" },
                { "URL:http://example.com/a,b", "URL:http://example.com/a,b" },
                { "CLASS:PUBLIC", "CLASS:PUBLIC" },
                { "NAME:Bjorn Jensen", "NAME:Bjorn Jensen" },
                { "PROFILE:VCARD", "PROFILE:VCARD" },
                { "SOURCE:http://example.com/a,b", "SOURCE:http://example.com/a,b" },
                { "IMPP;TYPE=personal:im:alice@example.com", "IMPP;TYPE=personal:im:alice@example.com" },
                { "FBURL:http://example.com/fb.ifb", "FBURL:http://example.com/fb.ifb" },
                { "CALADRURI:mailto:a@example.com", "CALADRURI:mailto:a@example.com" },
                { "CAPURI:http://example.com/cap", "CAPURI:http://example.com/cap" },
                { "CALURI:http://example.com/a.ics", "CALURI:http://example.com/a.ics" },
                { "X-ABC;X-A=1:raw\\,text", "X-ABC;X-A=1:raw\\,text" },
                { "item1.EMAIL:a@example.com", "item1.EMAIL:a@example.com" },
                { "PHOTO;ENCODING=b;TYPE=JPEG:" + Base64, "PHOTO;ENCODING=b;TYPE=JPEG:" + Base64 },
                { "LOGO;ENCODING=b;TYPE=HEIC:" + Base64, "LOGO;ENCODING=b;TYPE=HEIC:" + Base64 },
                { "SOUND;ENCODING=b;TYPE=BASIC:" + Base64, "SOUND;ENCODING=b;TYPE=BASIC:" + Base64 },
                { "KEY;ENCODING=b;TYPE=PGP:" + Base64, "KEY;ENCODING=b;TYPE=PGP:" + Base64 },
                { "KEY;ENCODING=b:" + Base64, "KEY;ENCODING=b:" + Base64 },

                // Written in a form of vCard 3.0 that means the same as what was read.
                { "TEL;TYPE=work,voice,pref,msg:+1-213-555-1234", "TEL;TYPE=work,voice,msg,pref:+1-213-555-1234" },
                { "EMAIL;TYPE=internet;TYPE=PREF:a@example.com", "EMAIL;TYPE=internet,pref:a@example.com" },
                { "FBURL;TYPE=PREF:http://example.com/fb.ifb", "FBURL;TYPE=pref:http://example.com/fb.ifb" },
                { "PHOTO;VALUE=uri;TYPE=JPEG:http://example.com/a,b", "PHOTO;VALUE=uri;TYPE=JPEG:http://example.com/a,b" },
                { "PHOTO;VALUE=binary;ENCODING=b;TYPE=image/jpeg:" + Base64, "PHOTO;ENCODING=b;TYPE=JPEG:" + Base64 },
                { "KEY;TYPE=PGP:abc", "KEY;TYPE=PGP;VALUE=text:abc" },
                { "TZ;VALUE=text:-05:00; EST; Raleigh/North America", "TZ;VALUE=text:-05:00\\; EST\\; Raleigh/North America" },
                { "BDAY:1996-04-15", "BDAY:1996-04-15" },
                { "BDAY;VALUE=date:19960415", "BDAY:1996-04-15" },
                { "BDAY:1953-10-15T23:10:00Z", "BDAY;VALUE=date-time:1953-10-15T23:10:00Z" },
                { "REV:1997-11-15", "REV:1997-11-15T00:00:00Z" },
                { "AGENT;VALUE=text:Jim", "AGENT;VALUE=text:Jim" }
            };
        }

        [Theory]
        [MemberData(nameof(LosslessRoundTripData))]
        public void WriteCard_Version30Card_KeepsEveryProperty(string line, string expectedLine)
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card(line));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testResult.Should().Be(Vcard30TestData.Card(expectedLine));
        }

        [Fact]
        public void WriteCard_AddressWithLabel_WritesTheLabelAfterItAgain()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("ADR;TYPE=home,pref:;;1 First St;;;;", "LABEL;TYPE=home:First\\nStreet\\, Town"));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testResult.Should().Be(Vcard30TestData.Card("ADR;TYPE=home,pref:;;1 First St;;;;", "LABEL;TYPE=home,pref:First\\nStreet\\, Town"));
        }

        [Fact]
        public void WriteCard_LabelThatGoesWithNoAddress_WritesItAsItWas()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("LABEL;TYPE=home:Nowhere"));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testResult.Should().Be(Vcard30TestData.Card("LABEL;TYPE=home:Nowhere"));
        }

        [Fact]
        public void WriteCard_SortString_WritesItAfterTheNameAgain()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("SORT-STRING:Doe"));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testResult.Should().Be(Vcard30TestData.Card("SORT-STRING:Doe"));
        }

        [Fact]
        public void WriteCard_Version30CardAsVersion40_MatchesAHandCheckedCard()
        {
            // arrange
            var text = Vcard30TestData.BareCard(
                "FN:Mr. John Q. Public\\, Esq.",
                "N:Public;John;Quinlan;Mr.;Esq.",
                "SORT-STRING:Public",
                "NICKNAME:Jim,Jimmie",
                "PHOTO;ENCODING=b;TYPE=JPEG:" + Base64,
                "BDAY:1996-04-15",
                "ADR;TYPE=dom,home,postal,parcel:;;123 Main Street;Any Town;CA;91921-1234",
                "LABEL;TYPE=dom,home,postal,parcel:123 Main Street\\nAny Town",
                "TEL;TYPE=work,voice,pref,msg:+1-213-555-1234",
                "EMAIL;TYPE=internet,pref:jane_doe@abc.com",
                "MAILER:PigeonMail 2.1",
                "TZ:-05:00",
                "GEO:37.386013;-122.082932",
                "AGENT;VALUE=uri:CID:JQPUBLIC.part3.960129T083020.xyzMail@host3.com",
                "REV:1995-10-31T22:27:10Z",
                "UID:19950401-080045-40000F192713-0052",
                "URL:http://example.com/a,b",
                "CLASS:PUBLIC");
            var card = VcardCardTestData.Read(text);

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V4_0);

            // assert
            testResult.Should().Be(VcardCardTestData.BareCard(
                "FN:Mr. John Q. Public\\, Esq.",
                "N;SORT-AS=Public:Public;John;Quinlan;Mr.;Esq.",
                "NICKNAME:Jim,Jimmie",
                "PHOTO:data:image/jpeg;base64\\," + Base64,
                "BDAY:19960415",
                "ADR;TYPE=dom,home,postal,parcel;LABEL=123 Main Street^nAny Town:;;123 Main Street;Any Town;CA;91921-1234;",
                "TEL;TYPE=work,voice,msg;PREF=1:+1-213-555-1234",
                "EMAIL;TYPE=internet;PREF=1:jane_doe@abc.com",
                "MAILER:PigeonMail 2.1",
                "TZ;VALUE=utc-offset:-0500",
                "GEO:geo:37.386013\\,-122.082932",
                "RELATED;TYPE=agent:CID:JQPUBLIC.part3.960129T083020.xyzMail@host3.com",
                "REV:19951031T222710Z",
                "UID;VALUE=text:19950401-080045-40000F192713-0052",
                "URL:http://example.com/a\\,b",
                "CLASS:PUBLIC"));
        }

        [Fact]
        public void WriteCard_UpConvertedCard_ReadsBackAsAConformingVersion40Card()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("TEL;TYPE=pref:+1", "BDAY:1996-04-15", "GEO:1;2", "TZ:-05:00", "URL:http://a.example/b,c"));
            var text = Vcard30TestData.Write(card, VcardVersion.V4_0);

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V4_0);
            testResult.Validate().Should().BeEmpty();
        }

        public static TheoryData<string, string> LossyLineData()
        {
            return new TheoryData<string, string>
            {
                // ref. #309: the properties 3.0 can't hold.
                { "KIND:group", VcardPropertyNames.Kind },
                { "KIND:application", VcardPropertyNames.Kind },
                { "KIND:device", VcardPropertyNames.Kind },
                { "GENDER:M", VcardPropertyNames.Gender },
                { "ANNIVERSARY:19960415", VcardPropertyNames.Anniversary },
                { "MEMBER:urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6", VcardPropertyNames.Member },
                { "RELATED;TYPE=friend:http://example.com", VcardPropertyNames.Related },
                { "CLIENTPIDMAP:1;urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6", VcardPropertyNames.ClientPidMap },
                { "LANG:fr", VcardPropertyNames.Language },
                { "XML:<a xmlns=\"urn:x\"/>", VcardPropertyNames.Xml },

                // ref. RFC 6474, RFC 6715 and RFC 8605.
                { "BIRTHPLACE:Town", VcardPropertyNames.BirthPlace },
                { "DEATHPLACE:Town", VcardPropertyNames.DeathPlace },
                { "DEATHDATE:19960415", VcardPropertyNames.DeathDate },
                { "EXPERTISE:chemistry", VcardPropertyNames.Expertise },
                { "HOBBY:reading", VcardPropertyNames.Hobby },
                { "INTEREST:rock", VcardPropertyNames.Interest },
                { "ORG-DIRECTORY:http://example.com", VcardPropertyNames.OrganizationDirectory },
                { "CONTACT-URI:mailto:a@example.com", VcardPropertyNames.ContactUri },

                // ref. RFC 9554.
                { "CREATED:20220705T093412Z", VcardPropertyNames.Created },
                { "GRAMGENDER:feminine", VcardPropertyNames.GrammaticalGender },
                { "LANGUAGE:de-AT", VcardPropertyNames.DefaultLanguage },
                { "PRONOUNS:they/them", VcardPropertyNames.Pronouns },
                { "SOCIALPROFILE:https://example.com/ietf", VcardPropertyNames.SocialProfile },

                // ref. RFC 6350: the parameters and values that RFC 2426 has none of.
                { "NOTE;PID=1:a", VcardPropertyNames.Note },
                { "NOTE;ALTID=1:a", VcardPropertyNames.Note },
                { "BDAY;CALSCALE=gregorian:19960415", VcardPropertyNames.Birthday },
                { "ADR;GEO=\"geo:1,2\":;;;;;;", VcardPropertyNames.Address },
                { "ADR;TZ=a:;;;;;;", VcardPropertyNames.Address },
                { "URL;PREF=1:http://example.com", VcardPropertyNames.Url },
                { "NOTE;TYPE=work:a", VcardPropertyNames.Note },
                { "PHOTO;TYPE=work:http://example.com", VcardPropertyNames.Photo },
                { "NOTE;LANGUAGE=es-419:a", VcardPropertyNames.Note },
                { "BDAY:--0415", VcardPropertyNames.Birthday },
                { "BDAY;VALUE=text:circa 1800", VcardPropertyNames.Birthday },
                { "TZ;VALUE=uri:http://example.com", VcardPropertyNames.TimeZone },
                { "GEO:geo:1\\,2;u=35", VcardPropertyNames.Geo },
                { "GEO:http://example.com", VcardPropertyNames.Geo },
                { "TEL;VALUE=uri:sip:a@example.com", VcardPropertyNames.Telephone },
                { "TEL;VALUE=uri:tel:+1-555;ext=5", VcardPropertyNames.Telephone },
                { "KEY:http://example.com/key", VcardPropertyNames.Key },
                { "ADR:a,b;;;;;;", VcardPropertyNames.Address },
                { "NOTE;X-A=\"a^'b\":a", VcardPropertyNames.Note },

                // ref. RFC 6715 §3 and RFC 8605 §3.1.
                { "EXPERTISE;LEVEL=expert:a", VcardPropertyNames.Expertise },
                { "NOTE;LEVEL=expert:a", VcardPropertyNames.Note },
                { "NOTE;INDEX=1:a", VcardPropertyNames.Note },
                { "ADR;CC=US:;;;;;;", VcardPropertyNames.Address },

                // ref. RFC 9554 §4 and §5, and its components of §2.
                { "NOTE;AUTHOR=\"mailto:a@example.com\":a", VcardPropertyNames.Note },
                { "NOTE;AUTHOR-NAME=a:a", VcardPropertyNames.Note },
                { "NOTE;CREATED=20220705T093412Z:a", VcardPropertyNames.Note },
                { "NOTE;DERIVED=true:a", VcardPropertyNames.Note },
                { "NOTE;PROP-ID=a:a", VcardPropertyNames.Note },
                { "NOTE;SCRIPT=Latn:a", VcardPropertyNames.Note },
                { "IMPP;SERVICE-TYPE=a:im:a@example.com", VcardPropertyNames.InstantMessaging },
                { "IMPP;USERNAME=a:im:a@example.com", VcardPropertyNames.InstantMessaging },
                { "ADR;TYPE=billing:;;;;;;", VcardPropertyNames.Address },
                { "ADR;TYPE=delivery:;;;;;;", VcardPropertyNames.Address },
                { "ADR:;;;;;;;Room 1;;;;;;;;;;", VcardPropertyNames.Address },
                { "ADR;ALTID=1;PHONETIC=ipa:;;a;;;;", VcardPropertyNames.Address }
            };
        }

        [Theory]
        [MemberData(nameof(LossyLineData))]
        public void Analyze_DataVersion30CanNotHold_ReportsItAsLossy(string line, string expectedProperty)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(MinimalVersion40Name, line), AcceptEverything);

            // act
            var testResult = VcardVersionConverter.Analyze(card, VcardVersion.V3_0);

            // assert
            testResult.Should().NotBeEmpty();
            testResult.Should().OnlyContain(diagnostic => diagnostic.Kind == VcardDiagnosticKind.Lossy);
            testResult.Should().Contain(diagnostic => diagnostic.PropertyName == expectedProperty);
        }

        [Theory]
        [MemberData(nameof(LossyLineData))]
        public void WriteCard_DataVersion30CanNotHold_ThrowsAndWritesNothing(string line, string expectedProperty)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(MinimalVersion40Name, line), AcceptEverything);
            var output = new StringWriter();
            using (var writer = new VcardWriter(output, new VcardOptions { WriteVersion = VcardVersion.V3_0 }))
            {
                // act
                Action testAction = () => writer.WriteCard(card);

                // assert
                testAction.Should().Throw<VcardFileException>(expectedProperty).Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.Lossy);
                output.ToString().Should().BeEmpty();
            }
        }

        [Theory]
        [MemberData(nameof(LossyLineData))]
        public void WriteCard_DataVersion30CanNotHoldUnderAllowLossyVersionConversion_WritesAConformingCard(string line, string expectedProperty)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(MinimalVersion40Name, line), AcceptEverything);
            var written = Vcard30TestData.Write(card, VcardVersion.V3_0, true);

            // act
            var testResult = VcardCardTestData.Read(written);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V3_0, expectedProperty);
        }

        public static TheoryData<string, string> Version40DownConversionData()
        {
            return new TheoryData<string, string>
            {
                { "TEL;VALUE=uri;PREF=1:tel:+1-555-555-5555", "TEL;TYPE=pref:+1-555-555-5555" },
                { "EMAIL;TYPE=work;PREF=7:a@example.com", "EMAIL;TYPE=work,pref:a@example.com" },
                { "GEO:geo:37.386013\\,-122.082932", "GEO:37.386013;-122.082932" },
                { "TZ;VALUE=utc-offset:-05", "TZ:-05:00" },
                { "TZ:Raleigh/North America", "TZ;VALUE=text:Raleigh/North America" },
                { "BDAY:19850412", "BDAY:1985-04-12" },
                { "REV:19951031T222710Z", "REV:1995-10-31T22:27:10Z" },
                { "URL:http://example.com/a\\,b", "URL:http://example.com/a,b" },
                { "UID;VALUE=text:a;b", "UID:a\\;b" },
                { "NOTE:a;b", "NOTE:a\\;b" },
                { "PHOTO;MEDIATYPE=image/gif:http://example.com/a.gif", "PHOTO;VALUE=uri;TYPE=GIF:http://example.com/a.gif" },
                { "PHOTO:data:image/png;base64\\,AQID", "PHOTO;ENCODING=b;TYPE=PNG:AQID" },
                { "PHOTO:data:image/png\\,%01%02%03", "PHOTO;ENCODING=b;TYPE=PNG:AQID" },
                { "SOUND:data:audio/wav;base64\\,AQID", "SOUND;ENCODING=b;TYPE=WAVE:AQID" },
                { "SOUND:data:video/mp4;base64\\,AQID", "SOUND;ENCODING=b;TYPE=video/mp4:AQID" },
                { "KEY:data:application/pgp-keys;base64\\,AQID", "KEY;ENCODING=b;TYPE=PGP:AQID" },
                { "KEY;VALUE=text:abc", "KEY;VALUE=text:abc" },
                { "RELATED;TYPE=agent:http://example.com/a\\,b", "AGENT;VALUE=uri:http://example.com/a,b" },
                { "RELATED;VALUE=text;TYPE=agent:Jim", "AGENT;VALUE=text:Jim" },
                { "ADR;LABEL=\"a^nb\":;;1 First St;;;;", "ADR:;;1 First St;;;;\r\nLABEL:a\\nb" },
                { "ADR;LABEL=\"a\\nb\":;;1 First St;;;;", "ADR:;;1 First St;;;;\r\nLABEL:a\\nb" },
                { "ORG;SORT-AS=\"Acme\":Acme;Sales", "ORG:Acme;Sales\r\nSORT-STRING:Acme" },
                { "X-UNKNOWN:a,b;c", "X-UNKNOWN:a,b;c" }
            };
        }

        [Theory]
        [MemberData(nameof(Version40DownConversionData))]
        public void WriteCard_Version40LineThatHasAnEquivalent_WritesItAsVersion30(string line, string expected)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.BareCard(Vcard30TestData.FormattedName, MinimalVersion40Name, line));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testResult.Should().Be(Vcard30TestData.Card(expected));
        }

        [Fact]
        public void WriteCard_ListOfPreferences_MakesOnlyTheLowestPreferredAndReportsTheOrder()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(MinimalVersion40Name, "TEL;PREF=2:+1", "TEL;PREF=1:+2"));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0, true);

            // assert
            testResult.Should().Be(Vcard30TestData.Card("TEL:+1", "TEL;TYPE=pref:+2"));
        }

        [Fact]
        public void WriteCard_NameWithComponentsOfRfc9554UnderAllowLossyVersionConversion_FoldsThemIntoOlderComponents()
        {
            // arrange: not folded yet, as a line added to Properties isn't.  ref. RFC 9554 §2.2.
            var card = VcardCardTestData.Read(VcardCardTestData.BareCard(Vcard30TestData.FormattedName, "N:Doe;John;;;;Smith;Jr."));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0, true);

            // assert
            testResult.Should().Be(Vcard30TestData.BareCard(Vcard30TestData.FormattedName, "N:Doe,Smith;John;;;Jr."));
        }

        [Fact]
        public void WriteCard_AddressWithComponentsOfRfc9554UnderAllowLossyVersionConversion_FoldsThemIntoTheStreet()
        {
            // arrange: ref. RFC 9554 §2.1.
            var card = VcardCardTestData.Read(VcardCardTestData.Card(MinimalVersion40Name, "ADR:;;;;;;;;;;123;Main Street;;;;;;"));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0, true);

            // assert
            testResult.Should().Be(Vcard30TestData.Card("ADR:;;123 Main Street;;;;"));
        }

        [Fact]
        public void WriteCard_PhoneticNameUnderAllowLossyVersionConversion_LeavesOutOnlyThePronunciation()
        {
            // arrange: ref. RFC 9554 §4.6.
            var card = VcardCardTestData.Read(VcardCardTestData.BareCard(Vcard30TestData.FormattedName, "N;ALTID=1:Doe;Jane;;;", "N;ALTID=1;PHONETIC=ipa:do;;;;"));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0, true);

            // assert
            testResult.Should().Be(Vcard30TestData.BareCard(Vcard30TestData.FormattedName, Vcard30TestData.Name));
        }

        [Fact]
        public void WriteCard_ServiceTypeAndUsernameOnUnknownProperty_KeepsThemWithNoForcedQuotes()
        {
            // arrange: ref. RFC 9554 §4.9 and §4.10: they are quoted in a line of vCard 4.0 only, and on a property of no version they are unknown parameters.
            var card = VcardCardTestData.Read(VcardCardTestData.Card(MinimalVersion40Name, "X-A;SERVICE-TYPE=b;USERNAME=c:d"));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testResult.Should().Be(Vcard30TestData.Card("X-A;SERVICE-TYPE=b;USERNAME=c:d"));
        }

        [Fact]
        public void WriteCard_Version40CardWithNoName_ThrowsMissingRequiredProperty()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            using (var writer = new VcardWriter(new StringWriter(), new VcardOptions { WriteVersion = VcardVersion.V3_0 }))
            {
                // act
                Action testAction = () => writer.WriteCard(card);

                // assert
                testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.MissingRequiredProperty);
            }
        }

        [Fact]
        public void WriteCard_LongInlineData_FoldsIt()
        {
            // arrange: ref. RFC 2426 §5: "Inline binary content must be "B" encoded and folded."
            var card = VcardCardTestData.Read(Vcard30TestData.Card(Vcard30TestData.LargeVersion30PhotoLine));
            var output = new StringWriter();

            // act
            using (var writer = new VcardWriter(output, new VcardOptions { WriteVersion = VcardVersion.V3_0 }))
            {
                writer.WriteCard(card);
            }

            // assert
            output.ToString().Should().NotContain(Vcard30TestData.LargeVersion30PhotoLine);
        }

        public static TheoryData<VcardVersion> UnwritableVersionData()
        {
            return new TheoryData<VcardVersion> { VcardVersion.V2_1 };
        }

        [Theory]
        [MemberData(nameof(UnwritableVersionData))]
        public void Constructor_VersionThatCanNotBeWrittenYet_ThrowsArgumentException(VcardVersion version)
        {
            // arrange
            var options = new VcardOptions { WriteVersion = version };

            // act
            Action testAction = () => new VcardWriter(new StringWriter(), options);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Validate_Version30_ReportsTheSameAsAWriterWould()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("KIND:group"));

            // act
            var testResult = card.Validate(VcardVersion.V3_0);

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(VcardDiagnosticKind.Lossy, VcardDiagnosticKind.MissingRequiredProperty);
        }

        [Fact]
        public void Analyze_CardOfVersion30ForVersion40_ReportsWhatStaysAsAPropertyOf40ThatIsUndefined()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("MAILER:a", "CLASS:PUBLIC", "NAME:a", "PROFILE:VCARD", "CAPURI:http://example.com"));

            // act
            var testResult = VcardVersionConverter.Analyze(card, VcardVersion.V4_0);

            // assert
            testResult.Should().HaveCount(5);
            testResult.Should().OnlyContain(diagnostic => diagnostic.Kind == VcardDiagnosticKind.VersionConversion);
        }

        [Fact]
        public void Analyze_NullCard_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => VcardVersionConverter.Analyze(null, VcardVersion.V3_0);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Convert_LossyCardByDefault_Throws()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(MinimalVersion40Name, "KIND:group"));

            // act
            Action testAction = () => VcardVersionConverter.Convert(card, VcardVersion.V3_0);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.Lossy);
        }

        [Fact]
        public void Convert_LossyCardUnderAllowLossyVersionConversion_GivesACopyWithOnlyWhatTheVersionHolds()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(MinimalVersion40Name, "KIND:group", "TEL;PREF=1:+1"));
            var options = new VcardOptions { AllowLossyVersionConversion = true };

            // act
            var testResult = VcardVersionConverter.Convert(card, VcardVersion.V3_0, options);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V3_0);
            testResult.GetProperties(VcardPropertyNames.Kind).Should().BeEmpty();
            testResult.Telephones.Single().Pref.Should().Be(1);
            VcardVersionConverter.Analyze(testResult, VcardVersion.V3_0).Should().BeEmpty();
            card.GetProperties(VcardPropertyNames.Kind).Should().ContainSingle();
        }

        [Fact]
        public void Convert_AnyCardToVersion40_KeepsEveryProperty()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("MAILER:a"));

            // act
            var testResult = VcardVersionConverter.Convert(card, VcardVersion.V4_0);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V4_0);
            VcardCardTestData.NamesAndValuesOf(testResult).Should().Equal(VcardCardTestData.NamesAndValuesOf(card));
        }
    }
}
