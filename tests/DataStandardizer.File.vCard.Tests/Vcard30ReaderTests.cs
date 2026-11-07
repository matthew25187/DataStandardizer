using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of reading vCard 3.0 cards (RFC 2426, RFC 2739 and RFC 4770) into the canonical form of the model.
    /// </summary>
    public class Vcard30ReaderTests
    {
        // ref. RFC 2426 §3.1.4: the start of the example, which is cut short there, shortened to whole base64 groups.
        private const string ExampleBase64 = "MIICajCC";

        private static readonly string[] NoParameters = new string[0];

        // ref. RFC 2426 §3.2.2: the example.
        private const string Rfc2426Label = "LABEL;TYPE=dom,home,postal,parcel:Mr.John Q. Public\\, Esq.\\nMail Drop: TNE QB\\n123 Main Street\\nAny Town\\, CA  91921-1234\\nU.S.A.";
        private const string Rfc2426Address = "ADR;TYPE=dom,home,postal,parcel:;;123 Main Street;Any Town;CA;91921-1234";

        // ref. RFC 2426 §7 as erratum 872 corrects it, with the N it adds, and the fold of the RFC.
        private static readonly string Rfc2426Authors = VcardTestData.Crlf(
            "BEGIN:vCard",
            "VERSION:3.0",
            "FN:Frank Dawson",
            "N:Dawson;Frank;;;",
            "ORG:Lotus Development Corporation",
            "ADR;TYPE=WORK,POSTAL,PARCEL:;;6544 Battleford Drive",
            " ;Raleigh;NC;27613-3502;U.S.A.",
            "TEL;TYPE=VOICE,MSG,WORK:+1-919-676-9515",
            "TEL;TYPE=FAX,WORK:+1-919-676-9564",
            "EMAIL;TYPE=INTERNET,PREF:Frank_Dawson@Lotus.com",
            "EMAIL;TYPE=INTERNET:fdawson@earthlink.net",
            "URL:http://home.earthlink.net/~fdawson",
            "END:vCard");

        // ref. RFC 2739 §2.3 as erratum 7914 corrects it, which writes TYPE=PREF.
        private static readonly string Rfc2739Example = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:3.0",
            "N:Dun;Alec",
            "FN:Alec Dun",
            "ORG:Microsoft Corporation",
            "ADR;TYPE=WORK,POSTAL,PARCEL:;;One Microsoft Way;",
            " Redmond;WA;98052-6399;USA",
            "TEL;TYPE=WORK,MSG:+1-206-936-4544",
            "TEL;TYPE=WORK,FAX:+1-206-936-7329",
            "EMAIL;TYPE=INTERNET:user@host1.com",
            "CALADRURI;TYPE=PREF:mailto:user@host1.com",
            "CALURI;TYPE=PREF:http://cal.host1.com/user/cal.ics",
            "FBURL;TYPE=PREF:http://cal.host1.com/user/fb.ifb",
            "CALURI:http://cal.company.com/projectA/pjtA.ics",
            "FBURL:http://cal.company.com/projectA/pjtAfb.ifb",
            "END:VCARD");

        public static TheoryData<string> Rfc2426ExampleData()
        {
            return new TheoryData<string>
            {
                // ref. RFC 2426 §3.1.
                "FN:Mr. John Q. Public\\, Esq.",
                "NICKNAME:Robbie",
                "NICKNAME:Jim,Jimmie",
                "PHOTO;VALUE=uri:http://www.abc.com/pub/photos/jqpublic.gif",
                "PHOTO;ENCODING=b;TYPE=JPEG:" + ExampleBase64,
                "BDAY:1996-04-15",
                "BDAY:1953-10-15T23:10:00Z",
                "BDAY:1987-09-27T08:30:00-06:00",

                // ref. RFC 2426 §3.2 and §3.3.
                Rfc2426Address,
                Rfc2426Label,
                "TEL;TYPE=work,voice,pref,msg:+1-213-555-1234",
                "EMAIL;TYPE=internet:jqpublic@xyz.dom1.com",
                "EMAIL;TYPE=internet,pref:jane_doe@abc.com",
                "MAILER:PigeonMail 2.1",

                // ref. RFC 2426 §3.4 and §3.5.
                "TZ:-05:00",
                "TZ;VALUE=text:-05:00; EST; Raleigh/North America",
                "GEO:37.386013;-122.082932",
                "TITLE:Director\\, Research and Development",
                "ROLE:Programmer",
                "LOGO;VALUE=uri:http://www.abc.com/pub/logos/abccorp.jpg",
                "LOGO;ENCODING=b;TYPE=JPEG:" + ExampleBase64,
                "AGENT;VALUE=uri:CID:JQPUBLIC.part3.960129T083020.xyzMail@host3.com",
                "ORG:ABC\\, Inc.;North American Division;Marketing",

                // ref. RFC 2426 §3.6.
                "CATEGORIES:TRAVEL AGENT",
                "CATEGORIES:INTERNET,IETF,INDUSTRY,INFORMATION TECHNOLOGY",
                "NOTE:This fax number is operational 0800 to 1715 EST\\, Mon-Fri.\\nPlease leave a message.",
                "PRODID:-//ONLINE DIRECTORY//NONSGML Version 1//EN",
                "REV:1995-10-31T22:27:10Z",
                "REV:1997-11-15",
                "SORT-STRING:Harten",
                "SOUND;TYPE=BASIC;VALUE=uri:CID:JOHNQPUBLIC.part8.19960229T080000.xyzMail@host1.com",
                "SOUND;TYPE=BASIC;ENCODING=b:" + ExampleBase64,
                "UID:19950401-080045-40000F192713-0052",
                "URL:http://www.swbyps.restaurant.french/~chezchic.html",

                // ref. RFC 2426 §3.7.
                "CLASS:PUBLIC",
                "CLASS:PRIVATE",
                "CLASS:CONFIDENTIAL",
                "KEY;ENCODING=b:" + ExampleBase64,

                // ref. RFC 2426 §2.1 and §3.8.
                "PROFILE:VCARD",
                "NAME:Bjorn Jensen",
                "X-ABC-VIDEO;X-TYPE=MPEG:http://www.example.com/a.mpeg"
            };
        }

        [Theory]
        [MemberData(nameof(Rfc2426ExampleData))]
        public void ReadCard_Rfc2426Example_IsAccepted(string line)
        {
            // arrange
            var text = Vcard30TestData.Card(line);

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V3_0);
        }

        public static TheoryData<string> WholeCardExampleData()
        {
            return new TheoryData<string> { Rfc2426Authors, Rfc2739Example };
        }

        [Theory]
        [MemberData(nameof(WholeCardExampleData))]
        public void ReadCard_WholeCardExampleWithErrataApplied_IsAccepted(string text)
        {
            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V3_0);
        }

        public static TheoryData<string, string[], string> CanonicalLineData()
        {
            return new TheoryData<string, string[], string>
            {
                // The preference: RFC 2426 §3.3.1 and RFC 6350 §5.3.  Which is not in TYPE is checked by the value of TYPE.
                { "TEL;TYPE=work,voice,pref,msg:+1-213-555-1234", new[] { "TYPE=work,voice,msg", "PREF=1" }, "+1-213-555-1234" },
                { "EMAIL;TYPE=internet;TYPE=PREF:a@example.com", new[] { "TYPE=internet", "PREF=1" }, "a@example.com" },
                { "IMPP;TYPE=personal,pref:im:alice@example.com", new[] { "TYPE=personal", "PREF=1" }, "im:alice@example.com" },
                { "FBURL;TYPE=PREF:http://example.com/fb.ifb", new[] { "PREF=1" }, "http://example.com/fb.ifb" },

                // Binary as a data URI: RFC 6350 §6.2.4, with its comma escaped by §3.4 and erratum 3845.
                { "PHOTO;ENCODING=b;TYPE=JPEG:" + ExampleBase64, NoParameters, "data:image/jpeg;base64\\," + ExampleBase64 },
                { "PHOTO;VALUE=binary;ENCODING=b;TYPE=GIF:" + ExampleBase64, NoParameters, "data:image/gif;base64\\," + ExampleBase64 },
                { "LOGO;ENCODING=b;TYPE=HEIC:" + ExampleBase64, NoParameters, "data:image/heic;base64\\," + ExampleBase64 },
                { "SOUND;TYPE=BASIC;ENCODING=b:" + ExampleBase64, NoParameters, "data:audio/basic;base64\\," + ExampleBase64 },
                { "KEY;TYPE=PGP;ENCODING=b:" + ExampleBase64, NoParameters, "data:application/pgp-keys;base64\\," + ExampleBase64 },
                { "KEY;ENCODING=b:" + ExampleBase64, NoParameters, "data:application/octet-stream;base64\\," + ExampleBase64 },

                // A URI with a type: RFC 6350 §5.7.
                { "PHOTO;VALUE=uri;TYPE=JPEG:http://example.com/a.jpg", new[] { "MEDIATYPE=image/jpeg" }, "http://example.com/a.jpg" },
                { "PHOTO;VALUE=uri:http://example.com/a.jpg", NoParameters, "http://example.com/a.jpg" },

                // A key as text: RFC 6350 §6.8.1.
                { "KEY;TYPE=PGP:abc", new[] { "TYPE=PGP", "VALUE=text" }, "abc" },

                // Commas in URIs: RFC 6350 §3.4, and errata 3845 and 3846, which aren't of vCard 3.0.
                { "URL:http://example.com/a,b", NoParameters, "http://example.com/a\\,b" },
                { "PHOTO;VALUE=uri:http://example.com/a,b", NoParameters, "http://example.com/a\\,b" },

                // GEO: RFC 6350 §6.5.2.
                { "GEO:37.386013;-122.082932", NoParameters, "geo:37.386013\\,-122.082932" },

                // TZ: RFC 6350 §6.5.1.
                { "TZ:-05:00", new[] { "VALUE=utc-offset" }, "-0500" },
                { "TZ;VALUE=text:-05:00; EST; Raleigh/North America", NoParameters, "-05:00; EST; Raleigh/North America" },

                // Dates: RFC 6350 §4.3.
                { "BDAY:1996-04-15", NoParameters, "19960415" },
                { "BDAY;VALUE=date:1996-04-15", NoParameters, "19960415" },
                { "BDAY:1953-10-15T23:10:00Z", NoParameters, "19531015T231000Z" },
                { "BDAY;VALUE=date-time:1987-09-27T08:30:00-06:00", NoParameters, "19870927T083000-0600" },
                { "REV:1995-10-31T22:27:10Z", NoParameters, "19951031T222710Z" },
                { "REV:1997-11-15", NoParameters, "19971115T000000Z" },

                // UID: RFC 6350 §6.7.6, whose default is a URI.
                { "UID:19950401-080045-40000F192713-0052", new[] { "VALUE=text" }, "19950401-080045-40000F192713-0052" },
                { "UID:urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6", NoParameters, "urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6" },

                // Kept as they are.
                { "CALURI:http://example.com/a.ics", NoParameters, "http://example.com/a.ics" },
                { "CAPURI:http://example.com/cap", NoParameters, "http://example.com/cap" },
                { "NICKNAME:Jim,Jimmie", NoParameters, "Jim,Jimmie" },
                { "CATEGORIES:A,B", NoParameters, "A,B" },
                { "PRODID:-//ONLINE DIRECTORY//NONSGML Version 1//EN", NoParameters, "-//ONLINE DIRECTORY//NONSGML Version 1//EN" },
                { "SOURCE;CONTEXT=word:http://example.com", new[] { "CONTEXT=word" }, "http://example.com" },
                { "MAILER:PigeonMail 2.1", NoParameters, "PigeonMail 2.1" },
                { "CLASS:PUBLIC", NoParameters, "PUBLIC" },
                { "PROFILE:VCARD", NoParameters, "VCARD" },
                { "NAME:Bjorn Jensen", NoParameters, "Bjorn Jensen" }
            };
        }

        [Theory]
        [MemberData(nameof(CanonicalLineData))]
        public void ReadCard_Version30Line_GivesItInCanonicalForm(string line, string[] expectedParameters, string expectedValue)
        {
            // arrange
            var text = Vcard30TestData.Card(line);

            // act
            var testResult = VcardCardTestData.Read(text).Properties.Last();

            // assert
            Vcard30TestData.ParametersOf(testResult).Should().Equal(expectedParameters);
            testResult.Value.Should().Be(expectedValue);
        }

        public static TheoryData<string, string> ShortStructuredValueData()
        {
            return new TheoryData<string, string>
            {
                // ref. RFC 2426 §3.6.5: the example, with fewer components than RFC 6350 §6.2.2 has.
                { "N:Koura;Osamu", "Koura;Osamu;;;" },
                { "N:Pau;Shou Chang;Robert", "Pau;Shou Chang;Robert;;" }
            };
        }

        [Theory]
        [MemberData(nameof(ShortStructuredValueData))]
        public void ReadCard_NameWithFewerComponents_GivesEachOneOfVersion40(string line, string expected)
        {
            // arrange
            var text = Vcard30TestData.BareCard(Vcard30TestData.FormattedName, line);

            // act
            var testResult = VcardCardTestData.Read(text).GetProperties(VcardPropertyNames.Name).Single().Value;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ReadCard_AddressWithFewerComponents_GivesEachOneOfVersion40()
        {
            // arrange
            var text = Vcard30TestData.Card(Rfc2426Address);

            // act
            var testResult = VcardCardTestData.Read(text).GetProperties(VcardPropertyNames.Address).Single().Value;

            // assert
            testResult.Should().Be(";;123 Main Street;Any Town;CA;91921-1234;");
        }

        [Fact]
        public void ReadCard_LabelWithTheTypesOfAnAddress_GoesOnThatAddress()
        {
            // arrange
            var text = Vcard30TestData.Card(Rfc2426Address, Rfc2426Label);

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.GetProperties(VcardPropertyNames.PostalLabel).Should().BeEmpty();
            testResult.Addresses.Single().Label.Should().Be("Mr.John Q. Public, Esq.\nMail Drop: TNE QB\n123 Main Street\nAny Town, CA  91921-1234\nU.S.A.");
        }

        [Fact]
        public void ReadCard_LabelWithTheGroupOfAnAddress_GoesOnThatAddress()
        {
            // arrange
            var text = Vcard30TestData.Card("a.ADR:;;1 First St;;;;", "b.ADR;TYPE=home:;;2 Second St;;;;", "B.LABEL:Second");

            // act
            var testResult = VcardCardTestData.Read(text).Addresses;

            // assert
            testResult[0].Label.Should().BeNull();
            testResult[1].Label.Should().Be("Second");
        }

        [Fact]
        public void ReadCard_LabelAndAddressWithNoTypes_GoTogetherByTheirSameDefault()
        {
            // arrange
            var text = Vcard30TestData.Card("ADR:;;1 First St;;;;", "LABEL:First");

            // act
            var testResult = VcardCardTestData.Read(text).Addresses.Single().Label;

            // assert
            testResult.Should().Be("First");
        }

        public static TheoryData<string[]> UnmatchedLabelData()
        {
            return new TheoryData<string[]>
            {
                new[] { "LABEL:Nowhere" },
                new[] { "ADR;TYPE=home:;;1 First St;;;;", "LABEL;TYPE=work:Other" },
                new[] { "ADR;TYPE=home:;;1 First St;;;;", "LABEL;TYPE=home;LANGUAGE=en:Other" },
                new[] { "ADR;TYPE=home:;;1 First St;;;;", "LABEL;TYPE=home;X-A=1:Other" },
                new[] { "ADR;TYPE=home:;;1 First St;;;;", "LABEL;TYPE=home,pref:Other" },
                new[] { "a.ADR;TYPE=home:;;1 First St;;;;", "a.LABEL;TYPE=work:Other" }
            };
        }

        [Theory]
        [MemberData(nameof(UnmatchedLabelData))]
        public void ReadCard_LabelThatGoesWithNoAddress_StaysAPropertyAndIsReported(string[] lines)
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card(lines));

            // act
            var testResult = VcardVersionConverter.Analyze(card, VcardVersion.V4_0);

            // assert
            card.GetProperties(VcardPropertyNames.PostalLabel).Should().HaveCount(1);
            testResult.Should().ContainSingle().Which.Kind.Should().Be(VcardDiagnosticKind.VersionConversion);
        }

        [Fact]
        public void ReadCard_SortString_GoesOnTheName()
        {
            // arrange
            var text = Vcard30TestData.BareCard("FN:Rene van der Harten", "N:van der Harten;Rene;J.;Sir;R.D.O.N.", "SORT-STRING:Harten");

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.GetProperties(VcardPropertyNames.SortString).Should().BeEmpty();
            testResult.Name.SortAs.Should().Equal("Harten");
        }

        [Fact]
        public void ReadCard_SortStringWithNoName_GoesOnTheOrganization()
        {
            // arrange
            var options = new VcardOptions { AllowMissingRequiredProperties = true };
            var text = Vcard30TestData.BareCard(Vcard30TestData.FormattedName, "ORG:Acme", "SORT-STRING:Acme");

            // act
            var testResult = VcardCardTestData.Read(text, options);

            // assert
            testResult.Organizations.Single().SortAs.Should().Equal("Acme");
        }

        public static TheoryData<string> UnattachedSortStringData()
        {
            return new TheoryData<string>
            {
                // ref. RFC 6350 §5.9: a comma separates its elements.
                "SORT-STRING:a\\,b",
                "SORT-STRING;LANGUAGE=en:a"
            };
        }

        [Theory]
        [MemberData(nameof(UnattachedSortStringData))]
        public void ReadCard_SortStringThatAParameterCanNotHold_StaysAProperty(string line)
        {
            // arrange
            var text = Vcard30TestData.Card(line);

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.GetProperties(VcardPropertyNames.SortString).Should().ContainSingle();
        }

        [Fact]
        public void ReadCard_LegacyMembers_HaveTypedAccessors()
        {
            // arrange
            var text = Vcard30TestData.Card("MAILER:PigeonMail 2.1", "CLASS:PUBLIC", "NAME:Bjorn Jensen", "PROFILE:VCARD", "CAPURI:http://example.com/cap");

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.Mailer.Value.Should().Be("PigeonMail 2.1");
            testResult.Classification.Value.Should().Be("PUBLIC");
            testResult.SourceName.Value.Should().Be("Bjorn Jensen");
            testResult.Profile.Value.Should().Be("VCARD");
            testResult.CalendarAccessUris.Single().Value.Should().Be(new Uri("http://example.com/cap"));
        }

        [Fact]
        public void ReadCard_Version30Values_TypedMembersReadThemInCanonicalForm()
        {
            // arrange
            var text = Vcard30TestData.Card("TEL;TYPE=pref:+1", "TEL:+2", "GEO:37.386013;-122.082932", "TZ:-05:00", "BDAY:1996-04-15", "PHOTO;ENCODING=b;TYPE=JPEG:" + ExampleBase64);

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.Telephones.GetPreferred().Value.Text.Should().Be("+1");
            testResult.GeoLocations.Single().Value.OriginalString.Should().Be("geo:37.386013,-122.082932");
            testResult.TimeZones.Single().Value.ValueType.Should().Be(VcardValueType.UtcOffset);
            testResult.Birthday.Value.ValueType.Should().Be(VcardValueType.DateAndOrTime);
            testResult.Photos.Single().Value.IsData.Should().BeTrue();
        }

        public static TheoryData<string, string[], VcardDiagnosticKind> StrictDefaultData()
        {
            return new TheoryData<string, string[], VcardDiagnosticKind>
            {
                // ref. RFC 2426 §5: "The VERSION, N and FN types MUST be specified in a vCard."
                { "no N", new[] { Vcard30TestData.FormattedName }, VcardDiagnosticKind.MissingRequiredProperty },
                { "no FN", new[] { Vcard30TestData.Name }, VcardDiagnosticKind.MissingRequiredProperty },

                // ref. RFC 2426 §4: a comma and a semicolon have to be escaped in text.
                { "a bare comma in text", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "NOTE:a,b" }, VcardDiagnosticKind.InvalidValue },
                { "a bare semicolon in text", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "NOTE:a;b" }, VcardDiagnosticKind.InvalidValue },

                // ref. RFC 2426 §4: the values of each type.
                { "a date of vCard 4.0", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "BDAY:--0415" }, VcardDiagnosticKind.InvalidValue },
                { "a date that isn't one", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "BDAY;VALUE=date:1953-10-15T23:10:00Z" }, VcardDiagnosticKind.InvalidValue },
                { "a value type BDAY doesn't have", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "BDAY;VALUE=text:soon" }, VcardDiagnosticKind.InvalidValue },
                { "a GEO of vCard 4.0", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "GEO:geo:37.386013,-122.082932" }, VcardDiagnosticKind.InvalidValue },
                { "a TZ of vCard 4.0", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "TZ:-0500" }, VcardDiagnosticKind.InvalidValue },
                { "a PROFILE other than VCARD", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "PROFILE:other" }, VcardDiagnosticKind.InvalidValue },
                { "a CLASS that isn't a token", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "CLASS:a b" }, VcardDiagnosticKind.InvalidValue },
                { "a PHOTO with no type of value", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "PHOTO:http://example.com/a.jpg" }, VcardDiagnosticKind.InvalidValue },
                { "a b value that isn't base64", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "PHOTO;ENCODING=b:!" }, VcardDiagnosticKind.InvalidValue },
                { "an escaped comma in a URI", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "URL:http://example.com/a\\,b" }, VcardDiagnosticKind.InvalidValue },
                { "an AGENT that isn't a card", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "AGENT:Jim" }, VcardDiagnosticKind.InvalidValue },
                { "a URI that a Uri can't hold", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "URL:http://[bad]" }, VcardDiagnosticKind.InvalidValue },

                // ref. RFC 2426 §4: the parameters of each type.
                { "a parameter of vCard 4.0", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "TEL;PREF=1:+1" }, VcardDiagnosticKind.InvalidParameter },
                { "a parameter on URL", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "URL;TYPE=work:http://example.com" }, VcardDiagnosticKind.InvalidParameter },
                { "a parameter on VERSION", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "VERSION;X-A=1:3.0" }, VcardDiagnosticKind.InvalidParameter },
                { "a repeated LANGUAGE", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "NOTE;LANGUAGE=en;LANGUAGE=fr:a" }, VcardDiagnosticKind.InvalidParameter },
                { "a language that isn't one of RFC 1766", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "NOTE;LANGUAGE=es-419:a" }, VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 2426 §5: "The QUOTED-PRINTABLE inline encoding has been eliminated", and "The [VCARD] CHARSET type parameter has been eliminated."
                { "quoted-printable", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "NOTE;ENCODING=QUOTED-PRINTABLE:a" }, VcardDiagnosticKind.LegacySyntax },
                { "CHARSET", new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "NOTE;CHARSET=UTF-8:a" }, VcardDiagnosticKind.LegacySyntax }
            };
        }

        [Theory]
        [MemberData(nameof(StrictDefaultData))]
        public void ReadCard_WhatRfc2426DoesNotAllow_ThrowsItsKind(string scenario, string[] lines, VcardDiagnosticKind expected)
        {
            // arrange
            var text = Vcard30TestData.BareCard(lines);
            using (var reader = new VcardReader(new StringReader(text)))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>(scenario).Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(expected);
            }
        }

        public static TheoryData<string[], VcardOptions> OptInData()
        {
            return new TheoryData<string[], VcardOptions>
            {
                { new[] { Vcard30TestData.FormattedName }, new VcardOptions { AllowMissingRequiredProperties = true } },
                { new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "NOTE:a,b" }, new VcardOptions { AllowInvalidValues = true } },
                { new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "URL;TYPE=work:http://example.com" }, new VcardOptions { AllowInvalidValues = true } },
                { new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "NOTE;LANGUAGE=es-419:a" }, new VcardOptions { AllowInvalidValues = true } },
                { new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "NOTE;ENCODING=QUOTED-PRINTABLE:a" }, new VcardOptions { AcceptLegacySyntax = true } },
                { new[] { Vcard30TestData.FormattedName, Vcard30TestData.Name, "NOTE;CHARSET=UTF-8:a" }, new VcardOptions { AcceptLegacySyntax = true } }
            };
        }

        [Theory]
        [MemberData(nameof(OptInData))]
        public void ReadCard_WhatRfc2426DoesNotAllowUnderItsOption_IsRead(string[] lines, VcardOptions options)
        {
            // arrange
            var text = Vcard30TestData.BareCard(lines);

            // act
            var testResult = VcardCardTestData.Read(text, options);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V3_0);
        }

        [Fact]
        public void ReadCard_VersionAfterOtherProperties_IsAccepted()
        {
            // arrange
            var text = VcardTestData.Crlf(Vcard30TestData.Begin, Vcard30TestData.FormattedName, Vcard30TestData.Version30, Vcard30TestData.Name, Vcard30TestData.End);

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V3_0);
        }

        [Fact]
        public void ReadCard_GroupOnBeginVersionAndEnd_IsAccepted()
        {
            // arrange
            var text = VcardTestData.Crlf("a.BEGIN:VCARD", "a.VERSION:3.0", Vcard30TestData.FormattedName, Vcard30TestData.Name, "a.END:VCARD");

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V3_0);
        }

        [Fact]
        public void ReadCard_QuotedPrintableUnderAcceptLegacySyntax_IsDecodedIntoCanonicalForm()
        {
            // arrange
            var options = new VcardOptions { AcceptLegacySyntax = true };
            var text = Vcard30TestData.Card("NOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=UTF-8:caf=C3=A9");

            // act
            var testResult = VcardCardTestData.Read(text, options).Notes.Single();

            // assert
            testResult.Value.Should().Be("caf\xE9");
            testResult.Line.Parameters.Should().BeEmpty();
        }

        public static TheoryData<string, string> Rfc1766TagData()
        {
            return new TheoryData<string, string>
            {
                { "NOTE;LANGUAGE=en:a", "en" },
                { "NOTE;LANGUAGE=en-US:a", "en-US" },
                { "NOTE;LANGUAGE=i-klingon:a", "i-klingon" },
                { "NOTE;LANGUAGE=en-cockney:a", "en-cockney" }
            };
        }

        [Theory]
        [MemberData(nameof(Rfc1766TagData))]
        public void ReadCard_LanguageOfRfc1766_IsKept(string line, string tag)
        {
            // arrange
            var text = Vcard30TestData.Card(line);

            // act
            var testResult = VcardCardTestData.Read(text).Notes.Single().Line.GetParameter(VcardParameterNames.Language).Values.Single();

            // assert
            testResult.Should().Be(tag);
        }

        [Fact]
        public void ReadCard_GrandfatheredLanguageTag_GivesItAsAStronglyTypedTag()
        {
            // arrange
            var text = Vcard30TestData.Card("NOTE;LANGUAGE=i-klingon:a");

            // act
            var testResult = VcardCardTestData.Read(text).Notes.Single().Language;

            // assert
            testResult.Should().NotBeNull();
            testResult.Value.ToString().Should().Be("i-klingon");
        }

        [Fact]
        public void ReadCard_CardsOfDifferentVersionsInOneStream_ReadsEachByItsOwnRules()
        {
            // arrange
            var text = VcardCardTestData.Stream(Vcard30TestData.Card("GEO:1;2"), VcardCardTestData.Card("GEO:geo:1\\,2"));
            using (var reader = new VcardReader(new StringReader(text)))
            {
                // act
                var testResult = reader.ReadCards().ToList();

                // assert
                testResult[0].SourceVersion.Should().Be(VcardVersion.V3_0);
                testResult[1].SourceVersion.Should().Be(VcardVersion.V4_0);
                testResult[0].GetProperties(VcardPropertyNames.Geo).Single().Value.Should().Be("geo:1\\,2");
                testResult[1].GetProperties(VcardPropertyNames.Geo).Single().Value.Should().Be("geo:1\\,2");
            }
        }

        [Fact]
        public void ReadCard_InvalidCardThenValidCard_CanGoOnToTheNext()
        {
            // arrange
            var text = VcardCardTestData.Stream(Vcard30TestData.BareCard(Vcard30TestData.FormattedName), Vcard30TestData.Card());
            using (var reader = new VcardReader(new StringReader(text)))
            {
                VcardCardTestData.ReadPastFailure(reader);

                // act
                var testResult = reader.ReadCard();

                // assert
                testResult.SourceVersion.Should().Be(VcardVersion.V3_0);
            }
        }

        [Fact]
        public void ReadCard_LargeInlinePhoto_IsKept()
        {
            // arrange
            var text = Vcard30TestData.Card(Vcard30TestData.LargeVersion30PhotoLine);

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.GetProperties(VcardPropertyNames.Photo).Should().ContainSingle();
        }

        [Fact]
        public void WriteCard_LargeInlinePhotoAsVersion40_WritesIt()
        {
            // arrange: a limit of Uri mustn't stop a card that is valid being written.
            var card = VcardCardTestData.Read(Vcard30TestData.Card(Vcard30TestData.LargeVersion30PhotoLine));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V4_0);

            // assert
            testResult.Should().Contain(Vcard30TestData.LargeVersion40PhotoLine);
        }

        [Fact]
        public void WriteCard_LargeInlinePhotoAsVersion30_RoundTrips()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card(Vcard30TestData.LargeVersion30PhotoLine));

            // act
            var testResult = Vcard30TestData.Write(card, VcardVersion.V3_0);

            // assert
            testResult.Should().Be(Vcard30TestData.Card(Vcard30TestData.LargeVersion30PhotoLine));
        }
    }
}
