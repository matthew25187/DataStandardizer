using System;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    public class VcardReaderTests
    {
        // ref. RFC 6350 §6.6.5: the example of a group and the two cards it links to.
        private const string GroupExampleUid1Line = "UID:urn:uuid:03a0e51f-d1aa-4385-8a53-e29025acd8af";
        private const string GroupExampleUid2 = "urn:uuid:b8767877-b4a1-4c70-9acc-505d3819e519";

        private static readonly string GroupExample = VcardTestData.Crlf(
            "BEGIN:VCARD",
            "VERSION:4.0",
            "KIND:group",
            "FN:The Doe family",
            "MEMBER:urn:uuid:03a0e51f-d1aa-4385-8a53-e29025acd8af",
            "MEMBER:urn:uuid:b8767877-b4a1-4c70-9acc-505d3819e519",
            "END:VCARD",
            "BEGIN:VCARD",
            "VERSION:4.0",
            "FN:John Doe",
            "UID:urn:uuid:03a0e51f-d1aa-4385-8a53-e29025acd8af",
            "END:VCARD",
            "BEGIN:VCARD",
            "VERSION:4.0",
            "FN:Jane Doe",
            "UID:urn:uuid:b8767877-b4a1-4c70-9acc-505d3819e519",
            "END:VCARD");

        [Fact]
        public void ReadCard_Rfc6350Example_GivesItsProperties()
        {
            // arrange
            const int ExamplePropertyCount = 16;
            var options = new VcardOptions();

            // act
            var testResult = VcardCardTestData.Read(Rfc6350Examples.ExampleWithEscapedComma, options);

            // assert
            testResult.Properties.Should().HaveCount(ExamplePropertyCount);
            testResult.SourceVersion.Should().Be(VcardVersion.V4_0);
        }

        [Fact]
        public void ReadCard_Rfc6350Example_GivesTypedValues()
        {
            // arrange
            var text = Rfc6350Examples.ExampleWithEscapedComma;

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.FormattedNames.Select(property => property.Value).Should().Equal("Simon Perreault");
            testResult.Name.Value.ToString(VcardValueFormat.Text).Should().Be("Perreault;Simon;;;ing. jr,M.Sc.");
            testResult.Birthday.Value.DateAndOrTime.ToString(VcardValueFormat.Text).Should().Be("--0203");
            testResult.Anniversary.Value.DateAndOrTime.ToString(VcardValueFormat.Text).Should().Be("20090808T1430-0500");
            testResult.Gender.Value.Sex.Should().Be(VcardSex.Male);
            testResult.Languages.Select(property => property.Value.ToString()).Should().Equal("fr", "en");
            testResult.Organizations.Select(property => property.Value.ToString(VcardValueFormat.Text)).Should().Equal("Viagenie");
            testResult.Addresses[0].Value.ToString(VcardValueFormat.Text).Should().Be(";Suite D2-630;2875 Laurier;Quebec;QC;G1V 2M2;Canada");
            testResult.Telephones.Select(property => property.Value.Uri.OriginalString).Should().Equal("tel:+1-418-656-9254;ext=102", "tel:+1-418-262-6501");
            testResult.Emails.Select(property => property.Value).Should().Equal("simon.perreault@viagenie.ca");
            testResult.GeoLocations.Select(property => property.Value.OriginalString).Should().Equal("geo:46.772673,-71.282945");
            testResult.Keys.Select(property => property.Value.Uri.OriginalString).Should().Equal("http://www.viagenie.ca/simon.perreault/simon.asc");
            testResult.TimeZones.Select(property => property.Value.Text).Should().Equal("-0500");
            testResult.Urls.Select(property => property.Value.OriginalString).Should().Equal("http://nomis80.org");
        }

        [Theory]
        [MemberData(nameof(VcardTestData.BufferSizeData), MemberType = typeof(VcardTestData))]
        public void ReadCard_AnyBufferSize_GivesTheSameLines(int bufferSize)
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(Rfc6350Examples.ExampleWithEscapedComma), new VcardOptions(), bufferSize))
            {
                // act
                var testResult = reader.ReadCard();

                // assert
                VcardCardTestData.NamesAndValuesOf(testResult).Should().Equal(
                    "FN:Simon Perreault",
                    "N:Perreault;Simon;;;ing. jr,M.Sc.",
                    "BDAY:--0203",
                    "ANNIVERSARY:20090808T1430-0500",
                    "GENDER:M",
                    "LANG:fr",
                    "LANG:en",
                    "ORG:Viagenie",
                    "ADR:;Suite D2-630;2875 Laurier;Quebec;QC;G1V 2M2;Canada",
                    "TEL:tel:+1-418-656-9254;ext=102",
                    "TEL:tel:+1-418-262-6501",
                    "EMAIL:simon.perreault@viagenie.ca",
                    "GEO:geo:46.772673\\,-71.282945",
                    "KEY:http://www.viagenie.ca/simon.perreault/simon.asc",
                    "TZ:-0500",
                    "URL:http://nomis80.org");
            }
        }

        [Fact]
        public void ReadCard_QuotedTypeValueList_SplitsItLikeAParameterList()
        {
            // arrange
            var text = Rfc6350Examples.ExampleWithEscapedComma;

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.Telephones[0].Types.Should().Equal("work", "voice");
            testResult.Telephones[0].Pref.Should().Be(1);
        }

        public static TheoryData<string> Rfc6350PropertyExamples()
        {
            // ref. RFC 6350 §5 and §6: the example of each property and parameter, apart from those that need another property and the RFC's own typos (§5.4 and §6.2.5).  A comma in a URI
            // value is escaped: errata 3845 and 3846 correct §6.2.4 and §6.5.2 and say the same is true elsewhere, and §6.1.3 and §6.7.5 are the same case.
            return new TheoryData<string>
            {
                "ROLE;LANGUAGE=tr:hoca",
                "SOURCE:ldap://ldap.example.com/cn=Babs%20Jensen\\,%20o=Babsco\\,%20c=US",
                "SOURCE:http://directory.example.com/addressbooks/jdoe/Jean%20Dupont.vcf",
                "KIND:individual",
                "KIND:org",
                "ORG:ABC\\, Inc.;North American Division;Marketing",
                "XML:<a xmlns=\"http://example.com/ns\">text</a>",
                "N:Public;John;Quinlan;Mr.;Esq.",
                "N:Stevenson;John;Philip,Paul;Dr.;Jr.,M.D.,A.C.P.",
                "N;SORT-AS=\"Harten,Rene\":van der Harten;Rene,J.;Sir;R.D.O.N.;",
                "N;ALTID=1;LANGUAGE=en:Yamada;Taro;;;",
                "NICKNAME:Robbie",
                "NICKNAME:Jim,Jimmie",
                "NICKNAME;TYPE=work:Boss",
                "PHOTO:http://www.example.com/pub/photos/jqpublic.gif",
                "PHOTO:data:image/jpeg;base64\\,MIICajCCAdOgAwIBAgICBEUwDQYJKoZIhv",
                "BDAY:19960415",
                "BDAY:--0415",
                "BDAY;VALUE=text:circa 1800",
                "ANNIVERSARY:19960415",
                "GENDER:M",
                "GENDER:F",
                "GENDER:M;Fellow",
                "GENDER:F;grrrl",
                "GENDER:O;intersex",
                "GENDER:;it's complicated",
                "ADR;GEO=\"geo:12.3457,78.910\";LABEL=\"Mr. John Q. Public, Esq.\\nMail Drop: TNE QB\\n123 Main Street\\nAny Town, CA  91921-1234\\nU.S.A.\":;;123 Main Street;Any Town;CA;91921-1234;U.S.A.",
                "TEL;VALUE=uri;PREF=1;TYPE=\"voice,home\":tel:+1-555-555-5555;ext=5555",
                "TEL;VALUE=uri;TYPE=home:tel:+33-01-23-45-67",
                "EMAIL;TYPE=work:jqpublic@xyz.example.com",
                "EMAIL;PREF=1:jane_doe@example.com",
                "IMPP;PREF=1:xmpp:alice@example.com",
                "LANG;TYPE=work;PREF=1:en",
                "LANG;TYPE=home:fr",
                "TZ:Raleigh/North America",
                "TZ;VALUE=utc-offset:-0500",
                "GEO:geo:37.386013\\,-122.082932",
                "TITLE:Research Scientist",
                "ROLE:Project Leader",
                "LOGO:http://www.example.com/pub/logos/abccorp.jpg",
                "RELATED;TYPE=friend:urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6",
                "RELATED;TYPE=contact:http://example.com/directory/jdoe.vcf",
                "RELATED;TYPE=co-worker;VALUE=text:Please contact my assistant Jane Doe for any inquiries.",
                "CATEGORIES:TRAVEL AGENT",
                "CATEGORIES:INTERNET,IETF,INDUSTRY,INFORMATION TECHNOLOGY",
                "NOTE:This fax number is operational 0800 to 1715 EST\\, Mon-Fri.",
                "PRODID:-//ONLINE DIRECTORY//NONSGML Version 1//EN",
                "REV:19951031T222710Z",
                "SOUND:CID:JOHNQPUBLIC.part8.19960229T080000.xyzMail@example.com",
                "SOUND:data:audio/basic;base64\\,MIICajCCAdOgAwIBAgICBEUwDQYJKoZIh",
                "UID:urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6",
                "CLIENTPIDMAP:1;urn:uuid:3df403f4-5924-4bb7-b077-3c711d9eb34b",
                "URL:http://example.org/restaurant.french/~chezchic.html",
                "KEY:http://www.example.com/keys/jdoe.cer",
                "KEY;MEDIATYPE=application/pgp-keys:ftp://example.com/keys/jdoe",
                "FBURL;PREF=1:http://www.example.com/busy/janedoe",
                "FBURL;MEDIATYPE=text/calendar:ftp://example.com/busy/project-a.ifb",
                "CALADRURI;PREF=1:mailto:janedoe@example.com",
                "CALADRURI:http://example.com/calendar/jdoe",
                "CALURI;PREF=1:http://cal.example.com/calA",
                "CALURI;MEDIATYPE=text/calendar:ftp://ftp.example.com/calA.ics"
            };
        }

        [Theory]
        [MemberData(nameof(Rfc6350PropertyExamples))]
        public void Validate_Rfc6350PropertyExample_FindsNoProblem(string property)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property));

            // act
            var testResult = card.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void ReadCards_SeveralCards_GivesEachInOrder()
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(GroupExample)))
            {
                // act
                var testResult = reader.ReadCards().ToList();

                // assert
                testResult.Select(card => card.FormattedNames[0].Value).Should().Equal("The Doe family", "John Doe", "Jane Doe");
            }
        }

        [Fact]
        public void CardsRead_AfterSeveralCards_CountsThem()
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(GroupExample)))
            {
                reader.ReadCards().ToList();

                // act
                var testResult = reader.CardsRead;

                // assert
                testResult.Should().Be(3);
            }
        }

        [Fact]
        public void ReadCard_AfterLastCard_ReturnsNull()
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(VcardCardTestData.Card())))
            {
                reader.ReadCard();

                // act
                var testResult = reader.ReadCard();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void ResolveMembers_GroupExample_GivesTheLinkedCards()
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(GroupExample)))
            {
                var cards = reader.ReadCards().ToList();

                // act
                var testResult = cards[0].ResolveMembers(cards);

                // assert
                testResult.Should().Equal(cards[1], cards[2]);
            }
        }

        [Fact]
        public void FindByUid_UuidInAnotherCase_FindsTheCard()
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(GroupExample)))
            {
                var cards = reader.ReadCards().ToList();

                // act
                var testResult = VcardCard.FindByUid(cards, "URN:UUID:B8767877-B4A1-4C70-9ACC-505D3819E519");

                // assert
                testResult.Should().BeSameAs(cards[2]);
            }
        }

        [Fact]
        public void FindByUid_NoCardHasIt_ReturnsNull()
        {
            // arrange
            var cards = new[] { VcardCardTestData.Read(VcardCardTestData.Card(GroupExampleUid1Line)) };

            // act
            var testResult = VcardCard.FindByUid(cards, GroupExampleUid2);

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void ReadCard_LowerCaseDelimiters_ReadsTheCard()
        {
            // arrange
            // ref. RFC 6350 §6.1.1: the value is case-insensitive, and ABNF strings are too (RFC 5234 §2.3).
            var text = VcardTestData.Crlf("begin:vcard", "version:4.0", "fn:Jane Doe", "end:vcard");

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.FormattedNames[0].Value.Should().Be(VcardCardTestData.FormattedNameValue);
        }

        public static TheoryData<string, string> UnpairedDelimiterData()
        {
            return new TheoryData<string, string>
            {
                { "no BEGIN", VcardTestData.Crlf("VERSION:4.0", "FN:Jane Doe", "END:VCARD") },
                { "END first", VcardTestData.Crlf("END:VCARD") },
                { "no END", VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "FN:Jane Doe") },
                { "nested BEGIN", VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "FN:Jane Doe", "BEGIN:VCARD") },
                { "another BEGIN", VcardTestData.Crlf("BEGIN:OTHER", "VERSION:4.0", "FN:Jane Doe", "END:OTHER") },
                { "another END", VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "FN:Jane Doe", "END:OTHER") },
                { "no cards", string.Empty }
            };
        }

        [Theory]
        [MemberData(nameof(UnpairedDelimiterData))]
        public void ReadCards_UnpairedDelimiters_Throws(string scenario, string text)
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(text)))
            {
                // act
                Action testAction = () => reader.ReadCards().ToList();

                // assert
                testAction.Should().Throw<VcardFileException>(scenario)
                    .Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidStructure);
            }
        }

        [Fact]
        public void ReadCard_AfterNestedBegin_CannotContinue()
        {
            // arrange
            var text = VcardCardTestData.Stream(VcardCardTestData.BareCard("BEGIN:VCARD"), VcardCardTestData.Card());
            using (var reader = new VcardReader(new StringReader(text)))
            {
                VcardCardTestData.ReadPastFailure(reader);

                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadCard_Version40UriLongerThanUriHoldsOnNetFramework_IsAccepted()
        {
            // arrange
            var text = VcardCardTestData.Card(Vcard30TestData.LargeVersion40PhotoLine);

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.GetProperties(VcardPropertyNames.Photo).Should().ContainSingle();
        }

        [Fact]
        public void ReadCard_Version40PhotoLongerThanUriHoldsOnNetFramework_IsInPhotos()
        {
            // arrange: ref. #619.
            var text = VcardCardTestData.Card(Vcard30TestData.LargeVersion40PhotoLine);

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.Photos.Should().ContainSingle();
        }

        [Fact]
        public void ReadCard_Version40PhotoLongerThanUriHoldsOnNetFramework_HasNoUri()
        {
            // arrange: ref. #619.
            var text = VcardCardTestData.Card(Vcard30TestData.LargeVersion40PhotoLine);

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.Photos.Single().Value.Uri.Should().BeNull();
        }

        [Fact]
        public void ReadCard_Version40UrlLongerThanUriHoldsOnNetFramework_IsLeftOutOfUrls()
        {
            // arrange: ref. #619.
            var text = VcardCardTestData.Card(Vcard30TestData.LargeVersion40UrlLine);

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.Urls.Should().BeEmpty();
            testResult.GetProperties(VcardPropertyNames.Url).Should().ContainSingle();
        }

        public static TheoryData<string> UnsupportedVersionData()
        {
            return new TheoryData<string> { "VERSION:2.0", "VERSION:5.0", "VERSION:4" };
        }

        [Theory]
        [MemberData(nameof(UnsupportedVersionData))]
        public void ReadCard_OtherVersion_ThrowsUnsupportedVersion(string versionLine)
        {
            // arrange
            var text = VcardTestData.Crlf("BEGIN:VCARD", versionLine, "FN:Jane Doe", "END:VCARD");
            using (var reader = new VcardReader(new StringReader(text)))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>()
                    .Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.UnsupportedVersion);
            }
        }

        public static TheoryData<string, string> StructureData()
        {
            return new TheoryData<string, string>
            {
                { "a group on BEGIN", VcardTestData.Crlf("item.BEGIN:VCARD", "VERSION:4.0", "FN:Jane Doe", "END:VCARD") },
                { "a parameter on BEGIN", VcardTestData.Crlf("BEGIN;X-A=1:VCARD", "VERSION:4.0", "FN:Jane Doe", "END:VCARD") },
                { "a group on VERSION", VcardTestData.Crlf("BEGIN:VCARD", "item.VERSION:4.0", "FN:Jane Doe", "END:VCARD") },
                { "a group on END", VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "FN:Jane Doe", "item.END:VCARD") },
                { "a parameter on END", VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "FN:Jane Doe", "END;X-A=1:VCARD") }
            };
        }

        [Theory]
        [MemberData(nameof(StructureData))]
        public void ReadCard_DelimiterLineWithMore_ThrowsInvalidStructure(string scenario, string text)
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(text)))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>(scenario)
                    .Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidStructure);
            }
        }

        public static TheoryData<string, VcardDiagnosticKind> VersionLineData()
        {
            // ref. RFC 6350 §6.7.9: VERSION-param = "VALUE=text" / any-param.  §3.3 has no group.
            return new TheoryData<string, VcardDiagnosticKind>
            {
                { "VERSION;VALUE=uri:4.0", VcardDiagnosticKind.InvalidValue },
                { "VERSION;PREF=1:4.0", VcardDiagnosticKind.InvalidParameter },
                { "VERSION;ENCODING=7BIT:4.0", VcardDiagnosticKind.LegacySyntax },
                { "VERSION;CHARSET=UTF-8:4.0", VcardDiagnosticKind.LegacySyntax }
            };
        }

        [Theory]
        [MemberData(nameof(VersionLineData))]
        public void ReadCard_VersionLineBreaksARule_Throws(string versionLine, VcardDiagnosticKind expected)
        {
            // arrange
            var text = VcardTestData.Crlf("BEGIN:VCARD", versionLine, "FN:Jane Doe", "END:VCARD");
            using (var reader = new VcardReader(new StringReader(text)))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>()
                    .Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(expected);
            }
        }

        [Fact]
        public void ReadCard_GroupOnSecondVersionAndCardinalityOption_ThrowsInvalidStructure()
        {
            // arrange
            var text = VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "item.VERSION:4.0", "FN:Jane Doe", "END:VCARD");
            using (var reader = new VcardReader(new StringReader(text), new VcardOptions { AllowCardinalityViolations = true }))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>()
                    .Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidStructure);
            }
        }

        [Fact]
        public void ReadCard_VersionWithExtensionParameter_ReadsTheCard()
        {
            // arrange
            var text = VcardTestData.Crlf("BEGIN:VCARD", "VERSION;X-A=1:4.0", "FN:Jane Doe", "END:VCARD");

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V4_0);
        }

        [Fact]
        public void ReadCard_EncodedClientPidMapAndAcceptLegacySyntax_CountsItsSource()
        {
            // arrange
            var text = VcardCardTestData.Card("CLIENTPIDMAP;ENCODING=QUOTED-PRINTABLE:1=3Burn=3Aa", "EMAIL;PID=1.1:a@example.com");
            var options = new VcardOptions { AcceptLegacySyntax = true };

            // act
            var testResult = VcardCardTestData.Read(text, options);

            // assert
            testResult.ClientPidMaps[0].Value.SourceId.Should().Be(1);
        }

        [Fact]
        public void ReadCard_EncodedSemicolonAndSortAsAndAcceptLegacySyntax_CountsTheDecodedComponents()
        {
            // arrange
            var text = VcardCardTestData.Card("ORG;SORT-AS=\"Acme,Research\";ENCODING=QUOTED-PRINTABLE:Acme=3BResearch");
            var options = new VcardOptions { AcceptLegacySyntax = true };

            // act
            var testResult = VcardCardTestData.Read(text, options);

            // assert
            testResult.Organizations[0].SortAs.Should().Equal("Acme", "Research");
        }

        [Fact]
        public void ReadCard_InvalidXmlValueAccepted_KeepsItInPropertiesAndLeavesItOutOfTheTypedMember()
        {
            // arrange
            var text = VcardCardTestData.Card("XML:not an element");
            var options = new VcardOptions { AllowInvalidValues = true };

            // act
            var testResult = VcardCardTestData.Read(text, options);

            // assert
            testResult.Xml.Should().BeEmpty();
            testResult.GetProperties(VcardPropertyNames.Xml).Should().ContainSingle();
        }

        [Fact]
        public void ReadCard_CharsetResolverThrows_CannotContinue()
        {
            // arrange
            var text = VcardCardTestData.Stream(VcardCardTestData.Card("NOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=x-test:a"), VcardCardTestData.Card());
            var resolver = new VcardCardTestData.CallbackCharsetResolver { OnResolve = () => throw new ArithmeticException() };
            var options = new VcardOptions { AcceptLegacySyntax = true, CharsetResolver = resolver };
            using (var reader = new VcardReader(new StringReader(text), options))
            {
                VcardCardTestData.ReadPastResolverFailure(reader);

                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadCard_DisposedWhileTheCardIsChecked_ThrowsObjectDisposedException()
        {
            // arrange
            var resolver = new VcardCardTestData.CallbackCharsetResolver();
            var options = new VcardOptions { AcceptLegacySyntax = true, CharsetResolver = resolver };
            var reader = new VcardReader(new StringReader(VcardCardTestData.Card("NOTE;ENCODING=QUOTED-PRINTABLE;CHARSET=x-test:a")), options);
            resolver.OnResolve = reader.Dispose;

            // act
            Action testAction = () => reader.ReadCard();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void ReadCard_VersionWithValueParameter_ReadsTheCard()
        {
            // arrange
            // ref. RFC 6350 §6.7.9: VERSION-param = "VALUE=text" / any-param.
            var text = VcardTestData.Crlf("BEGIN:VCARD", "VERSION;VALUE=text:4.0", "FN:Jane Doe", "END:VCARD");

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.SourceVersion.Should().Be(VcardVersion.V4_0);
        }

        public static TheoryData<string, string, VcardOptions, VcardDiagnosticKind> AccommodationData()
        {
            return new TheoryData<string, string, VcardOptions, VcardDiagnosticKind>
            {
                {
                    "VERSION after FN",
                    VcardTestData.Crlf("BEGIN:VCARD", "FN:Jane Doe", "VERSION:4.0", "END:VCARD"),
                    new VcardOptions { AllowVersionNotFirst = true },
                    VcardDiagnosticKind.InvalidStructure
                },
                {
                    "no VERSION",
                    VcardTestData.Crlf("BEGIN:VCARD", "FN:Jane Doe", "END:VCARD"),
                    new VcardOptions { AllowMissingRequiredProperties = true },
                    VcardDiagnosticKind.MissingRequiredProperty
                },
                {
                    "no FN",
                    VcardCardTestData.BareCard("NOTE:a"),
                    new VcardOptions { AllowMissingRequiredProperties = true },
                    VcardDiagnosticKind.MissingRequiredProperty
                },
                {
                    "two VERSIONs",
                    VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "VERSION:4.0", "FN:Jane Doe", "END:VCARD"),
                    new VcardOptions { AllowCardinalityViolations = true },
                    VcardDiagnosticKind.CardinalityViolation
                },
                {
                    "two BDAYs",
                    VcardCardTestData.Card("BDAY:19960415", "BDAY:19960416"),
                    new VcardOptions { AllowCardinalityViolations = true },
                    VcardDiagnosticKind.CardinalityViolation
                },
                {
                    "MEMBER without KIND:group",
                    VcardCardTestData.Card("MEMBER:mailto:subscriber1@example.com"),
                    new VcardOptions { AllowCardinalityViolations = true },
                    VcardDiagnosticKind.CardinalityViolation
                },
                {
                    "a value that isn't a timestamp",
                    VcardCardTestData.Card("REV:2024-05-01T00:00:00Z"),
                    new VcardOptions { AllowInvalidValues = true },
                    VcardDiagnosticKind.InvalidValue
                },
                {
                    "a VALUE the property doesn't allow",
                    VcardCardTestData.Card("URL;VALUE=text:example"),
                    new VcardOptions { AllowInvalidValues = true },
                    VcardDiagnosticKind.InvalidValue
                },
                {
                    "PREF out of range",
                    VcardCardTestData.Card("TEL;PREF=101:1"),
                    new VcardOptions { AllowInvalidValues = true },
                    VcardDiagnosticKind.InvalidParameter
                },
                {
                    "PID on a property with one instance",
                    VcardCardTestData.Card("UID;PID=1:urn:uuid:03a0e51f-d1aa-4385-8a53-e29025acd8af"),
                    new VcardOptions { AllowInvalidValues = true },
                    VcardDiagnosticKind.InvalidParameter
                },
                {
                    "ENCODING in a 4.0 card",
                    VcardCardTestData.Card("NOTE;ENCODING=QUOTED-PRINTABLE:a"),
                    new VcardOptions { AcceptLegacySyntax = true },
                    VcardDiagnosticKind.LegacySyntax
                },
                {
                    "CHARSET in a 4.0 card",
                    VcardCardTestData.Card("NOTE;CHARSET=UTF-8:a"),
                    new VcardOptions { AcceptLegacySyntax = true },
                    VcardDiagnosticKind.LegacySyntax
                }
            };
        }

        [Theory]
        [MemberData(nameof(AccommodationData))]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters: the options are for the other test of the data.
        public void ReadCard_RuleBrokenAndDefaultOptions_Throws(string scenario, string text, VcardOptions options, VcardDiagnosticKind expected)
#pragma warning restore xUnit1026
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(text)))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>(scenario)
                    .Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(expected);
            }
        }

        [Theory]
        [MemberData(nameof(AccommodationData))]
#pragma warning disable xUnit1026 // Theory methods should use all of their parameters: the kind is for the other test of the data.
        public void ReadCard_RuleBrokenAndOptionSet_ReadsTheCard(string scenario, string text, VcardOptions options, VcardDiagnosticKind expected)
#pragma warning restore xUnit1026
        {
            // arrange
            using (var reader = new VcardReader(new StringReader(text), options))
            {
                // act
                var testResult = reader.ReadCard();

                // assert
                testResult.Should().NotBeNull(scenario);
            }
        }

        [Fact]
        public void ReadCard_ValuelessParameterAndAcceptLegacySyntax_GivesItAsAType()
        {
            // arrange
            var text = VcardCardTestData.Card("TEL;HOME;VOICE:1");
            var options = new VcardOptions { AcceptLegacySyntax = true };

            // act
            var testResult = VcardCardTestData.Read(text, options);

            // assert
            testResult.Telephones[0].Types.Should().Equal("HOME", "VOICE");
        }

        [Fact]
        public void ReadCard_QuotedPrintableAndAcceptLegacySyntax_DecodesTheValue()
        {
            // arrange
            var text = VcardCardTestData.Card("NOTE;ENCODING=QUOTED-PRINTABLE:caf=C3=A9");
            var options = new VcardOptions { AcceptLegacySyntax = true };

            // act
            var testResult = VcardCardTestData.Read(text, options);

            // assert
            testResult.Notes[0].Value.Should().Be("caf\x00E9");
        }

        [Fact]
        public void ReadCard_ValuelessParameterAndDefaultOptions_Throws()
        {
            // arrange
            var text = VcardCardTestData.Card("TEL;HOME:1");
            using (var reader = new VcardReader(new StringReader(text)))
            {
                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<VcardFileException>();
            }
        }

        [Fact]
        public void ReadCard_NoVersionAndOptionSet_LeavesSourceVersionNull()
        {
            // arrange
            var text = VcardTestData.Crlf("BEGIN:VCARD", "FN:Jane Doe", "END:VCARD");
            var options = new VcardOptions { AllowMissingRequiredProperties = true };

            // act
            var testResult = VcardCardTestData.Read(text, options);

            // assert
            testResult.SourceVersion.Should().BeNull();
        }

        [Fact]
        public void ReadCard_InvalidValueAccepted_KeepsItInPropertiesAndLeavesItOutOfTheTypedMember()
        {
            // arrange
            var text = VcardCardTestData.Card("REV:2024-05-01T00:00:00Z");
            var options = new VcardOptions { AllowInvalidValues = true };

            // act
            var testResult = VcardCardTestData.Read(text, options);

            // assert
            testResult.Revision.Should().BeNull();
            testResult.GetProperties(VcardPropertyNames.Revision).Single().Value.Should().Be("2024-05-01T00:00:00Z");
        }

        [Fact]
        public void ReadCard_NonconformingCardFollowedByAnother_CanContinue()
        {
            // arrange
            var text = VcardCardTestData.NonconformingCardThenCard;
            using (var reader = new VcardReader(new StringReader(text)))
            {
                VcardCardTestData.ReadPastFailure(reader);

                // act
                var testResult = reader.ReadCard();

                // assert
                testResult.FormattedNames[0].Value.Should().Be(VcardCardTestData.FormattedNameValue);
            }
        }

        [Fact]
        public void ReadCard_NonconformingSecondCard_ExceptionHasItsPlace()
        {
            // arrange
            const int SecondCardIndex = 1;
            const long LineOfSecondBirthday = 9;
            var text = VcardCardTestData.Stream(VcardCardTestData.Card(), VcardCardTestData.Card("item.BDAY:19960415", "item.BDAY:19960416"));
            using (var reader = new VcardReader(new StringReader(text)))
            {
                reader.ReadCard();

                // act
                Action testAction = () => reader.ReadCard();

                // assert
                var data = testAction.Should().Throw<VcardFileException>().Which.Data;
                data[VcardFileException.DataItemName.CardIndex].Should().Be(SecondCardIndex);
                data[VcardFileException.DataItemName.PropertyName].Should().Be("BDAY");
                data[VcardFileException.DataItemName.Group].Should().Be("item");
                data[VcardFileException.DataItemName.LineNumber].Should().Be(LineOfSecondBirthday);
            }
        }

        [Fact]
        public void ReadCard_MalformedLine_ThrowsAndCannotContinue()
        {
            // arrange
            var text = VcardCardTestData.Stream(VcardCardTestData.BareCard("no colon"), VcardCardTestData.Card());
            using (var reader = new VcardReader(new StringReader(text)))
            {
                VcardCardTestData.ReadPastFailure(reader);

                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadCard_AppleStyleCard_KeepsGroupsAndExtensions()
        {
            // arrange
            var text = VcardCardTestData.Card(
                "PRODID:-//Apple Inc.//iPhone OS 17.0//EN",
                "N:Doe;Jane;;;",
                "item1.EMAIL;TYPE=INTERNET;TYPE=pref:jane@example.com",
                "item1.X-ABLabel:_$!<Other>!$_",
                "TEL;TYPE=CELL;TYPE=VOICE:+1 555 555 5555",
                "X-ABShowAs:COMPANY");

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.Emails[0].Group.Should().Be("item1");
            testResult.Emails[0].Types.Should().Equal("INTERNET", "pref");
            testResult.Telephones[0].Value.Text.Should().Be("+1 555 555 5555");
            testResult.ExtensionProperties.Select(property => property.Value).Should().Equal("_$!<Other>!$_", "COMPANY");
        }

        [Fact]
        public void ReadCard_GoogleStyleCardWithExtendedTimestampAndOptionSet_ReadsTheRest()
        {
            // arrange
            var text = VcardCardTestData.Card(
                "N:Doe;Jane;;;",
                "EMAIL;TYPE=INTERNET;TYPE=HOME:jane@example.com",
                "TEL;TYPE=CELL:(555) 123-4567",
                "ADR;TYPE=HOME:;;123 Main St;Anytown;CA;91921;US",
                "ORG:Example\\, Inc.;",
                "CATEGORIES:myContacts",
                "REV:2024-05-01T00:00:00Z");
            var options = new VcardOptions { AllowInvalidValues = true };

            // act
            var testResult = VcardCardTestData.Read(text, options);

            // assert
            testResult.Emails[0].Value.Should().Be("jane@example.com");
            testResult.Categories[0].Value.Should().Equal("myContacts");
            testResult.Validate().Select(diagnostic => diagnostic.PropertyName).Should().Equal("REV");
        }

        [Fact]
        public void ReadCard_CaretEncodedLabel_DecodesIt()
        {
            // arrange
            // ref. RFC 6868 §3: ^n is a line break, and ^' a double quote.
            var text = VcardCardTestData.Card("ADR;LABEL=\"a^nb^'c\":;;1 Main St;Town;;;");

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.Addresses[0].Label.Should().Be("a\nb\"c");
        }

        [Fact]
        public void ReadCard_FromStream_ReadsTheCard()
        {
            // arrange
            var data = Encoding.UTF8.GetBytes(VcardCardTestData.Card());
            using (var reader = new VcardReader(new MemoryStream(data)))
            {
                // act
                var testResult = reader.ReadCard();

                // assert
                testResult.FormattedNames[0].Value.Should().Be(VcardCardTestData.FormattedNameValue);
            }
        }

        [Fact]
        public void LinesRead_AfterACard_CountsItsLines()
        {
            // arrange
            const long MinimalCardLineCount = 4;
            using (var reader = new VcardReader(new StringReader(VcardCardTestData.Card())))
            {
                reader.ReadCard();

                // act
                var testResult = reader.LinesRead;

                // assert
                testResult.Should().Be(MinimalCardLineCount);
            }
        }

        [Fact]
        public void PhysicalLinesRead_AfterAFoldedLine_CountsEachLine()
        {
            // arrange
            const long PhysicalLineCount = 5;
            var text = VcardTestData.Crlf("BEGIN:VCARD", "VERSION:4.0", "FN:Jane", "  Doe", "END:VCARD");
            using (var reader = new VcardReader(new StringReader(text)))
            {
                reader.ReadCard();

                // act
                var testResult = reader.PhysicalLinesRead;

                // assert
                testResult.Should().Be(PhysicalLineCount);
            }
        }

        public static TheoryData<Func<VcardReader, object>> DisposedMemberData()
        {
            return new TheoryData<Func<VcardReader, object>>
            {
                reader => reader.CardsRead,
                reader => reader.LinesRead,
                reader => reader.PhysicalLinesRead,
                reader => reader.ReadCard(),
                reader => reader.ReadCards()
            };
        }

        [Theory]
        [MemberData(nameof(DisposedMemberData))]
        public void Member_ReaderDisposed_ThrowsObjectDisposedException(Func<VcardReader, object> member)
        {
            // arrange
            var reader = new VcardReader(new StringReader(VcardCardTestData.Card()));
            reader.Dispose();

            // act
            Action testAction = () => member(reader);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Constructor_NullStream_ThrowsArgumentNullException()
        {
            // arrange
            Stream stream = null;

            // act
            Action testAction = () => new VcardReader(stream);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullOptions_ThrowsArgumentNullException()
        {
            // arrange
            var text = new StringReader(string.Empty);

            // act
            Action testAction = () => new VcardReader(text, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }
    }
}
