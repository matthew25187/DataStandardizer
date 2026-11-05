using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.JcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests that cards converted between <c>.vcf</c> and jCard keep their properties and values.  Only the spelling may change, as <see cref="JcardWriter"/> says: here, the
    /// group in upper case, a <c>VALUE</c> parameter after the others or gone where it names the default, and the values of a list parameter split on their commas.
    /// </summary>
    public class JcardRoundTripTests
    {
        /// <summary>
        /// The jCard that <see cref="JcardWriter"/> writes for each card of <see cref="JcardFixtures"/>, checked by hand against RFC 7095.
        /// </summary>
        private static readonly Dictionary<string, string> ExpectedJcard = new Dictionary<string, string>
        {
            [JcardFixtures.Rfc6350Example] = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"Simon Perreault\"],[\"n\",{},\"text\",[\"Perreault\",\"Simon\",\"\",\"\",[\"ing. jr\",\"M.Sc.\"]]],[\"bday\",{},\"date-and-or-time\",\"--02-03\"],[\"anniversary\",{},\"date-and-or-time\",\"2009-08-08T14:30-05:00\"],[\"gender\",{},\"text\",\"M\"],[\"lang\",{\"pref\":\"1\"},\"language-tag\",\"fr\"],[\"lang\",{\"pref\":\"2\"},\"language-tag\",\"en\"],[\"org\",{\"type\":\"work\"},\"text\",\"Viagenie\"],[\"adr\",{\"type\":\"work\"},\"text\",[\"\",\"Suite D2-630\",\"2875 Laurier\",\"Quebec\",\"QC\",\"G1V 2M2\",\"Canada\"]],[\"tel\",{\"type\":[\"work\",\"voice\"],\"pref\":\"1\"},\"uri\",\"tel:+1-418-656-9254;ext=102\"],[\"tel\",{\"type\":[\"work\",\"cell\",\"voice\",\"video\",\"text\"]},\"uri\",\"tel:+1-418-262-6501\"],[\"email\",{\"type\":\"work\"},\"text\",\"simon.perreault@viagenie.ca\"],[\"geo\",{\"type\":\"work\"},\"uri\",\"geo:46.772673,-71.282945\"],[\"key\",{\"type\":\"work\"},\"uri\",\"http://www.viagenie.ca/simon.perreault/simon.asc\"],[\"tz\",{},\"text\",\"-0500\"],[\"url\",{\"type\":\"home\"},\"uri\",\"http://nomis80.org\"]]]",
            [JcardFixtures.Rfc7095Example] = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"Simon Perreault\"],[\"n\",{},\"text\",[\"Perreault\",\"Simon\",\"\",\"\",[\"ing. jr\",\"M.Sc.\"]]],[\"bday\",{},\"date-and-or-time\",\"--02-03\"],[\"anniversary\",{},\"date-and-or-time\",\"2009-08-08T14:30:00-05:00\"],[\"gender\",{},\"text\",\"M\"],[\"lang\",{\"pref\":\"1\"},\"language-tag\",\"fr\"],[\"lang\",{\"pref\":\"2\"},\"language-tag\",\"en\"],[\"org\",{\"type\":\"work\"},\"text\",\"Viagenie\"],[\"adr\",{\"type\":\"work\"},\"text\",[\"\",\"Suite D2-630\",\"2875 Laurier\",\"Quebec\",\"QC\",\"G1V 2M2\",\"Canada\"]],[\"tel\",{\"type\":[\"work\",\"voice\"],\"pref\":\"1\"},\"uri\",\"tel:+1-418-656-9254;ext=102\"],[\"tel\",{\"type\":[\"work\",\"cell\",\"voice\",\"video\",\"text\"]},\"uri\",\"tel:+1-418-262-6501\"],[\"email\",{\"type\":\"work\"},\"text\",\"simon.perreault@viagenie.ca\"],[\"geo\",{\"type\":\"work\"},\"uri\",\"geo:46.772673,-71.282945\"],[\"key\",{\"type\":\"work\"},\"uri\",\"http://www.viagenie.ca/simon.perreault/simon.asc\"],[\"tz\",{},\"utc-offset\",\"-05:00\"],[\"url\",{\"type\":\"home\"},\"uri\",\"http://nomis80.org\"]]]",
            [JcardFixtures.GroupsAndExtensions] = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"Jane Doe\"],[\"tel\",{\"group\":\"item1\",\"type\":\"work\"},\"text\",\"+1-555-0100\"],[\"x-ablabel\",{\"group\":\"item1\"},\"unknown\",\"Work\"],[\"x-coffee-data\",{},\"unknown\",\"Stenophylla;Guinea\\\\,Africa\"],[\"x-karma-points\",{},\"integer\",95],[\"x-non-smoking\",{},\"boolean\",false],[\"x-grade\",{},\"float\",1.30],[\"x-time-local\",{},\"time\",\"12:30:00\"],[\"x-a\",{},\"x-mytype\",\"a\\\\b\"],[\"gender\",{\"x-probability\":\"0.8\"},\"text\",\"M\"]]]",
            [JcardFixtures.Values] = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"Jane Doe\"],[\"n\",{\"sort-as\":[\"Doe\",\"Jane\"]},\"text\",[\"Doe\",\"Jane\",[\"Q.\",\"R.\"],\"Dr.\",[\"PhD\",\"MD\"]]],[\"nickname\",{},\"text\",\"Janie\",\"JD\"],[\"bday\",{},\"date-and-or-time\",\"1985-04-12\"],[\"anniversary\",{},\"date-and-or-time\",\"--04-12T14:30Z\"],[\"deathdate\",{},\"text\",\"circa 2080\"],[\"rev\",{},\"timestamp\",\"2013-02-14T12:30:00-05\"],[\"tz\",{},\"utc-offset\",\"-05:00\"],[\"categories\",{},\"text\",\"computers\",\"cameras\"],[\"org\",{},\"text\",[\"ABC, Inc.\",\"North American Division\",\"Marketing\"]],[\"note\",{},\"text\",\"Line one\\nLine two, with a comma\\\\ and a backslash\"],[\"lang\",{\"pref\":\"1\"},\"language-tag\",\"fr-CA\"],[\"clientpidmap\",{},\"text\",[\"1\",\"urn:uuid:3df403f4-5924-4bb7-b077-3c711d9eb34b\"]],[\"tel\",{\"pid\":\"1.1\",\"type\":[\"work\",\"voice\"]},\"uri\",\"tel:+1-555-555-5555;ext=5555\"]]]",
            [JcardFixtures.JsContactExtensions] = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"Jane Doe III\"],[\"n\",{},\"text\",[\"Doe\",\"Jane\",\"\",\"\",\"III\",\"\",\"III\"]],[\"created\",{},\"timestamp\",\"2022-07-05T09:34:12Z\"],[\"pronouns\",{\"pref\":\"1\"},\"text\",\"she/her\"],[\"socialprofile\",{\"service-type\":\"Mastodon\",\"username\":\"JaneDoe\"},\"uri\",\"https://example.com/@JaneDoe\"]]]",
            [JcardFixtures.Version30] = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"Jane Doe\"],[\"n\",{},\"text\",[\"Doe\",\"Jane\",\"\",\"\",\"\"]],[\"tel\",{\"type\":[\"WORK\",\"VOICE\"]},\"text\",\"+1-555-0100\"]]]",
        };

        /// <summary>
        /// The properties <see cref="JcardReader"/> reads from <see cref="ExpectedJcard"/>, checked by hand against RFC 7095.
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
                "ITEM1.TEL;TYPE=work:+1-555-0100",
                "ITEM1.X-ABLABEL:Work",
                "X-COFFEE-DATA:Stenophylla;Guinea\\,Africa",
                "X-KARMA-POINTS;VALUE=integer:95",
                "X-NON-SMOKING;VALUE=boolean:FALSE",
                "X-GRADE;VALUE=float:1.30",
                "X-TIME-LOCAL;VALUE=time:123000",
                "X-A;VALUE=x-mytype:a\\b",
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

        [Theory]
        [MemberData(nameof(FixtureNames))]
        public void WriteCards_VcfCard_WritesTheExpectedJcard(string fixtureName)
        {
            // arrange
            var (text, options) = JcardFixtures.Get(fixtureName);
            var cards = ReadVcards(text, options);

            // act
            var testResult = WriteJcard(cards, options);

            // assert
            testResult.Should().Be(ExpectedJcard[fixtureName]);
        }

        [Theory]
        [MemberData(nameof(FixtureNames))]
        public void ReadCards_VcfCardWrittenAsJcard_GivesTheSameProperties(string fixtureName)
        {
            // arrange
            var (text, options) = JcardFixtures.Get(fixtureName);
            var jcard = WriteJcard(ReadVcards(text, options), options);

            // act
            var testResult = Describe(ReadJcard(jcard, options));

            // assert
            testResult.Should().BeEquivalentTo(new[] { ExpectedModel[fixtureName] }, assertionOptions => assertionOptions.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(FixtureNames))]
        public void ReadCards_JcardWrittenAsVcf_GivesTheSameProperties(string fixtureName)
        {
            // arrange
            var (_, options) = JcardFixtures.Get(fixtureName);
            var vcf = WriteVcards(ReadJcard(ExpectedJcard[fixtureName], options), options);

            // act
            var testResult = Describe(ReadVcards(vcf, options));

            // assert
            testResult.Should().BeEquivalentTo(new[] { ExpectedModel[fixtureName] }, assertionOptions => assertionOptions.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(FixtureNames))]
        public void WriteCards_JcardThroughVcf_WritesTheSameJcard(string fixtureName)
        {
            // arrange
            var (_, options) = JcardFixtures.Get(fixtureName);
            var cards = ReadVcards(WriteVcards(ReadJcard(ExpectedJcard[fixtureName], options), options), options);

            // act
            var testResult = WriteJcard(cards, options);

            // assert
            testResult.Should().Be(ExpectedJcard[fixtureName]);
        }
    }
}
