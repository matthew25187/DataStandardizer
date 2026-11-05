using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.JcardTestData;
using static DataStandardizer.File.vCard.Tests.XcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests that cards converted between <c>.vcf</c>, xCard and jCard keep their properties and values.  Only the spelling may change, as <see cref="XcardWriter"/> says: here, a
    /// <c>VALUE</c> parameter after the others or gone where it names the default, the values of a list parameter split on their commas, and a <c>VALUE</c> parameter that names an
    /// extension value type, which xCard has no element for.
    /// </summary>
    public class XcardRoundTripTests
    {
        /// <summary>
        /// The xCard that <see cref="XcardWriter"/> writes for each card of <see cref="JcardFixtures"/>, checked by hand against RFC 6351, and compared once made
        /// <see cref="XcardTestData.Canonical"/>.
        /// </summary>
        private static readonly Dictionary<string, string> ExpectedXcard = new Dictionary<string, string>
        {
            [JcardFixtures.Rfc6350Example] =
                "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard>" +
                "<fn><text>Simon Perreault</text></fn>" +
                "<n><surname>Perreault</surname><given>Simon</given><additional/><prefix/><suffix>ing. jr</suffix><suffix>M.Sc.</suffix></n>" +
                "<bday><date>--0203</date></bday>" +
                "<anniversary><date-time>20090808T1430-0500</date-time></anniversary>" +
                "<gender><sex>M</sex></gender>" +
                "<lang><parameters><pref><integer>1</integer></pref></parameters><language-tag>fr</language-tag></lang>" +
                "<lang><parameters><pref><integer>2</integer></pref></parameters><language-tag>en</language-tag></lang>" +
                "<org><parameters><type><text>work</text></type></parameters><text>Viagenie</text></org>" +
                "<adr><parameters><type><text>work</text></type></parameters><pobox/><ext>Suite D2-630</ext><street>2875 Laurier</street><locality>Quebec</locality>" +
                "<region>QC</region><code>G1V 2M2</code><country>Canada</country></adr>" +
                "<tel><parameters><type><text>work</text><text>voice</text></type><pref><integer>1</integer></pref></parameters><uri>tel:+1-418-656-9254;ext=102</uri></tel>" +
                "<tel><parameters><type><text>work</text><text>cell</text><text>voice</text><text>video</text><text>text</text></type></parameters>" +
                "<uri>tel:+1-418-262-6501</uri></tel>" +
                "<email><parameters><type><text>work</text></type></parameters><text>simon.perreault@viagenie.ca</text></email>" +
                "<geo><parameters><type><text>work</text></type></parameters><uri>geo:46.772673,-71.282945</uri></geo>" +
                "<key><parameters><type><text>work</text></type></parameters><uri>http://www.viagenie.ca/simon.perreault/simon.asc</uri></key>" +
                "<tz><text>-0500</text></tz>" +
                "<url><parameters><type><text>home</text></type></parameters><uri>http://nomis80.org</uri></url>" +
                "</vcard></vcards>",
            [JcardFixtures.Rfc7095Example] =
                "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard>" +
                "<fn><text>Simon Perreault</text></fn>" +
                "<n><surname>Perreault</surname><given>Simon</given><additional/><prefix/><suffix>ing. jr</suffix><suffix>M.Sc.</suffix></n>" +
                "<bday><date>--0203</date></bday>" +
                "<anniversary><date-time>20090808T143000-0500</date-time></anniversary>" +
                "<gender><sex>M</sex></gender>" +
                "<lang><parameters><pref><integer>1</integer></pref></parameters><language-tag>fr</language-tag></lang>" +
                "<lang><parameters><pref><integer>2</integer></pref></parameters><language-tag>en</language-tag></lang>" +
                "<org><parameters><type><text>work</text></type></parameters><text>Viagenie</text></org>" +
                "<adr><parameters><type><text>work</text></type></parameters><pobox/><ext>Suite D2-630</ext><street>2875 Laurier</street><locality>Quebec</locality>" +
                "<region>QC</region><code>G1V 2M2</code><country>Canada</country></adr>" +
                "<tel><parameters><type><text>work</text><text>voice</text></type><pref><integer>1</integer></pref></parameters><uri>tel:+1-418-656-9254;ext=102</uri></tel>" +
                "<tel><parameters><type><text>work</text><text>cell</text><text>voice</text><text>video</text><text>text</text></type></parameters>" +
                "<uri>tel:+1-418-262-6501</uri></tel>" +
                "<email><parameters><type><text>work</text></type></parameters><text>simon.perreault@viagenie.ca</text></email>" +
                "<geo><parameters><type><text>work</text></type></parameters><uri>geo:46.772673,-71.282945</uri></geo>" +
                "<key><parameters><type><text>work</text></type></parameters><uri>http://www.viagenie.ca/simon.perreault/simon.asc</uri></key>" +
                "<tz><utc-offset>-0500</utc-offset></tz>" +
                "<url><parameters><type><text>home</text></type></parameters><uri>http://nomis80.org</uri></url>" +
                "</vcard></vcards>",
            [JcardFixtures.GroupsAndExtensions] =
                "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard>" +
                "<fn><text>Jane Doe</text></fn>" +
                "<group name=\"item1\"><tel><parameters><type><text>work</text></type></parameters><text>+1-555-0100</text></tel>" +
                "<x-ablabel><unknown>Work</unknown></x-ablabel></group>" +
                "<x-coffee-data><unknown>Stenophylla;Guinea\\,Africa</unknown></x-coffee-data>" +
                "<x-karma-points><integer>95</integer></x-karma-points>" +
                "<x-non-smoking><boolean>false</boolean></x-non-smoking>" +
                "<x-grade><float>1.30</float></x-grade>" +
                "<x-time-local><time>123000</time></x-time-local>" +
                "<x-a><unknown>a\\b</unknown></x-a>" +
                "<gender><parameters><x-probability><unknown>0.8</unknown></x-probability></parameters><sex>M</sex></gender>" +
                "</vcard></vcards>",
            [JcardFixtures.Values] =
                "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard>" +
                "<fn><text>Jane Doe</text></fn>" +
                "<n><parameters><sort-as><text>Doe</text><text>Jane</text></sort-as></parameters><surname>Doe</surname><given>Jane</given><additional>Q.</additional>" +
                "<additional>R.</additional><prefix>Dr.</prefix><suffix>PhD</suffix><suffix>MD</suffix></n>" +
                "<nickname><text>Janie</text><text>JD</text></nickname>" +
                "<bday><date>19850412</date></bday>" +
                "<anniversary><date-time>--0412T1430Z</date-time></anniversary>" +
                "<deathdate><text>circa 2080</text></deathdate>" +
                "<rev><timestamp>20130214T123000-05</timestamp></rev>" +
                "<tz><utc-offset>-0500</utc-offset></tz>" +
                "<categories><text>computers</text><text>cameras</text></categories>" +
                "<org><text>ABC, Inc.</text><text>North American Division</text><text>Marketing</text></org>" +
                "<note><text>Line one\nLine two, with a comma\\ and a backslash</text></note>" +
                "<lang><parameters><pref><integer>1</integer></pref></parameters><language-tag>fr-CA</language-tag></lang>" +
                "<clientpidmap><sourceid>1</sourceid><uri>urn:uuid:3df403f4-5924-4bb7-b077-3c711d9eb34b</uri></clientpidmap>" +
                "<tel><parameters><pid><text>1.1</text></pid><type><text>work</text><text>voice</text></type></parameters><uri>tel:+1-555-555-5555;ext=5555</uri></tel>" +
                "</vcard></vcards>",
            [JcardFixtures.JsContactExtensions] =
                "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard>" +
                "<fn><text>Jane Doe III</text></fn>" +
                "<n><surname>Doe</surname><given>Jane</given><additional/><prefix/><suffix>III</suffix><surname2/><generation>III</generation></n>" +
                "<created><timestamp>20220705T093412Z</timestamp></created>" +
                "<pronouns><parameters><pref><integer>1</integer></pref></parameters><text>she/her</text></pronouns>" +
                "<socialprofile><parameters><service-type><text>Mastodon</text></service-type><username><text>JaneDoe</text></username></parameters>" +
                "<uri>https://example.com/@JaneDoe</uri></socialprofile>" +
                "</vcard></vcards>",
            [JcardFixtures.Version30] =
                "<vcards xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"><vcard>" +
                "<fn><text>Jane Doe</text></fn>" +
                "<n><surname>Doe</surname><given>Jane</given><additional/><prefix/><suffix/></n>" +
                "<tel><parameters><type><text>WORK</text><text>VOICE</text></type></parameters><text>+1-555-0100</text></tel>" +
                "</vcard></vcards>",
        };

        /// <summary>
        /// The properties <see cref="XcardReader"/> reads from <see cref="ExpectedXcard"/>, checked by hand against RFC 6351.
        /// </summary>
        private static readonly Dictionary<string, string[]> ExpectedModel = new Dictionary<string, string[]>
        {
            [JcardFixtures.Rfc6350Example] = new[]
            {
                "FN:Simon Perreault",
                "N:Perreault;Simon;;;ing. jr,M.Sc.",
                "BDAY:--0203",
                "ANNIVERSARY:20090808T1430-0500",
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
                "TZ:-0500",
                "URL;TYPE=home:http://nomis80.org",
            },
            [JcardFixtures.Rfc7095Example] = new[]
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
            },
            [JcardFixtures.GroupsAndExtensions] = new[]
            {
                "FN:Jane Doe",
                "item1.TEL;TYPE=work:+1-555-0100",
                "item1.X-ABLABEL:Work",
                "X-COFFEE-DATA:Stenophylla;Guinea\\,Africa",
                "X-KARMA-POINTS;VALUE=integer:95",
                "X-NON-SMOKING;VALUE=boolean:FALSE",
                "X-GRADE;VALUE=float:1.30",
                "X-TIME-LOCAL;VALUE=time:123000",
                "X-A:a\\b",
                "GENDER;X-PROBABILITY=0.8:M",
            },
            [JcardFixtures.Values] = new[]
            {
                "FN:Jane Doe",
                "N;SORT-AS=Doe|Jane:Doe;Jane;Q.,R.;Dr.;PhD,MD",
                "NICKNAME:Janie,JD",
                "BDAY:19850412",
                "ANNIVERSARY:--0412T1430Z",
                "DEATHDATE;VALUE=text:circa 2080",
                "REV:20130214T123000-05",
                "TZ;VALUE=utc-offset:-0500",
                "CATEGORIES:computers,cameras",
                "ORG:ABC\\, Inc.;North American Division;Marketing",
                "NOTE:Line one\\nLine two\\, with a comma\\\\ and a backslash",
                "LANG;PREF=1:fr-CA",
                "CLIENTPIDMAP:1;urn:uuid:3df403f4-5924-4bb7-b077-3c711d9eb34b",
                "TEL;PID=1.1;TYPE=work|voice;VALUE=uri:tel:+1-555-555-5555;ext=5555",
            },
            [JcardFixtures.JsContactExtensions] = new[]
            {
                "FN:Jane Doe III",
                "N:Doe;Jane;;;III;;III",
                "CREATED:20220705T093412Z",
                "PRONOUNS;PREF=1:she/her",
                "SOCIALPROFILE;SERVICE-TYPE=Mastodon;USERNAME=JaneDoe:https://example.com/@JaneDoe",
            },
            [JcardFixtures.Version30] = new[]
            {
                "FN:Jane Doe",
                "N:Doe;Jane;;;",
                "TEL;TYPE=WORK|VOICE:+1-555-0100",
            },
        };

        public static TheoryData<string> FixtureNames()
        {
            var data = new TheoryData<string>();
            foreach (var name in JcardFixtures.Names)
            {
                data.Add(name);
            }

            return data;
        }

        /// <summary>
        /// The fixtures that go through jCard unchanged.  <see cref="JcardFixtures.GroupsAndExtensions"/> doesn't, since jCard reads a group in upper case (RFC 7095 §3.3.1.2.1).
        /// </summary>
        public static TheoryData<string> JcardFixtureNames() => new TheoryData<string>
        {
            JcardFixtures.Rfc6350Example,
            JcardFixtures.Rfc7095Example,
            JcardFixtures.Values,
            JcardFixtures.JsContactExtensions,
            JcardFixtures.Version30,
        };

        [Theory]
        [MemberData(nameof(FixtureNames))]
        public void WriteCards_VcfCard_WritesTheExpectedXcard(string fixtureName)
        {
            // arrange
            var (text, options) = JcardFixtures.Get(fixtureName);
            var cards = ReadVcards(text, options);

            // act
            var testResult = Canonical(WriteXcard(cards, options));

            // assert
            testResult.Should().Be(Canonical(ExpectedXcard[fixtureName]));
        }

        [Theory]
        [MemberData(nameof(FixtureNames))]
        public void ReadCards_VcfCardWrittenAsXcard_GivesTheSameProperties(string fixtureName)
        {
            // arrange
            var (text, options) = JcardFixtures.Get(fixtureName);
            var xcard = WriteXcard(ReadVcards(text, options), options);

            // act
            var testResult = Describe(ReadXcard(xcard, options));

            // assert
            testResult.Should().BeEquivalentTo(new[] { ExpectedModel[fixtureName] }, assertionOptions => assertionOptions.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(FixtureNames))]
        public void ReadCards_XcardWrittenAsVcf_GivesTheSameProperties(string fixtureName)
        {
            // arrange
            var (_, options) = JcardFixtures.Get(fixtureName);
            var vcf = WriteVcards(ReadXcard(ExpectedXcard[fixtureName], options), options);

            // act
            var testResult = Describe(ReadVcards(vcf, options));

            // assert
            testResult.Should().BeEquivalentTo(new[] { ExpectedModel[fixtureName] }, assertionOptions => assertionOptions.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(FixtureNames))]
        public void WriteCards_XcardThroughVcf_WritesTheSameXcard(string fixtureName)
        {
            // arrange
            var (_, options) = JcardFixtures.Get(fixtureName);
            var cards = ReadVcards(WriteVcards(ReadXcard(ExpectedXcard[fixtureName], options), options), options);

            // act
            var testResult = Canonical(WriteXcard(cards, options));

            // assert
            testResult.Should().Be(Canonical(ExpectedXcard[fixtureName]));
        }

        [Theory]
        [MemberData(nameof(JcardFixtureNames))]
        public void ReadCards_XcardWrittenAsJcard_GivesTheSameProperties(string fixtureName)
        {
            // arrange
            var (_, options) = JcardFixtures.Get(fixtureName);
            var jcard = WriteJcard(ReadXcard(ExpectedXcard[fixtureName], options), options);

            // act
            var testResult = Describe(ReadJcard(jcard, options));

            // assert
            testResult.Should().BeEquivalentTo(new[] { ExpectedModel[fixtureName] }, assertionOptions => assertionOptions.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(JcardFixtureNames))]
        public void WriteCards_XcardThroughJcard_WritesTheSameXcard(string fixtureName)
        {
            // arrange
            var (_, options) = JcardFixtures.Get(fixtureName);
            var cards = ReadJcard(WriteJcard(ReadXcard(ExpectedXcard[fixtureName], options), options), options);

            // act
            var testResult = Canonical(WriteXcard(cards, options));

            // assert
            testResult.Should().Be(Canonical(ExpectedXcard[fixtureName]));
        }
    }
}
