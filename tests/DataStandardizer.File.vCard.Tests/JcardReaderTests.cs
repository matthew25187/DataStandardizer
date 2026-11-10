using System;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.JcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of <see cref="JcardReader"/>.
    /// </summary>
    public class JcardReaderTests
    {
        private const string MinimalCard = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"]]]";

        // ref. RFC 7095 §3.3.1.2: the example of a group.
        private const string GroupExample = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[ \"fn\", { \"group\": \"CONTACT\" }, \"text\", \"Mr. John Q. Public, Esq.\" ]]]";
        private const string GroupExampleVcard = "CONTACT.FN:Mr. John Q. Public\\, Esq.";

        public static TheoryData<string, string> Properties() => new TheoryData<string, string>
        {
            // ref. RFC 7095 §3.3: the example of a multi-valued property.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"categories\", {}, \"text\", \"computers\", \"cameras\"]]]", "CATEGORIES:computers,cameras" },

            // ref. RFC 7095 §3.3.1.3: the examples of structured values.
            {
                "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"adr\", {}, \"text\", [\"\", \"\", \"123 Main Street\", \"Any Town\", \"CA\", \"91921-1234\", \"U.S.A.\"]]]]",
                "ADR:;;123 Main Street;Any Town;CA;91921-1234;U.S.A."
            },
            {
                "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"adr\", {}, \"text\", [\"\", \"\", [\"My Street\", \"Left Side\", \"Second Shack\"], \"Hometown\", \"PA\", \"18252\", \"U.S.A.\"]]]]",
                "ADR:;;My Street,Left Side,Second Shack;Hometown;PA;18252;U.S.A."
            },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"gender\", {}, \"text\", [\"F\", \"grrrl\"] ]]]", "GENDER:F;grrrl" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"gender\", {}, \"text\", \"M\" ]]]", "GENDER:M" },
            {
                "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"adr\",{\"label\":\"123 Maple Ave\\nSuite 901\\nVancouver BC\\nA1B 2C9\\nCanada\"},\"text\",[\"\", \"\", \"\", \"\", \"\", \"\", \"\"]]]]",
                "ADR;LABEL=123 Maple Ave\nSuite 901\nVancouver BC\nA1B 2C9\nCanada:;;;;;;"
            },

            // ref. RFC 7095 §3.4 and §3.4.2: the examples of parameters.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"role\", { \"language\": \"tr\" }, \"text\", \"roca\"]]]", "ROLE;LANGUAGE=tr:roca" },
            {
                "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"n\",{ \"sort-as\": [\"Harten\", \"Rene\"] },\"text\",[\"van der Harten\", \"Rene\", \"J.\", \"Sir\", \"R.D.O.N.\"]]]]",
                "N;SORT-AS=Harten|Rene:van der Harten;Rene;J.;Sir;R.D.O.N."
            },

            // ref. RFC 7095 §3.5.1, §3.5.2 and §3.5.12: TEXT, URI and LANGUAGE-TAG.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"kind\", {}, \"text\", \"group\"]]]", "KIND:group" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"source\", {}, \"uri\", \"ldap://ldap.example.com/cn=babs%20jensen\"]]]", "SOURCE:ldap://ldap.example.com/cn=babs%20jensen" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"lang\", {}, \"language-tag\", \"de\"]]]", "LANG:de" },

            // ref. RFC 7095 §3.5.3 to §3.5.7: dates and times, in the text form of RFC 6350.  The examples of §3.5.3 and §3.5.5 put a date on BDAY and a date-time on ANNIVERSARY,
            // which RFC 6350 §6.2.5 and §6.2.6 don't allow, so extension properties take their place.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-date\", {}, \"date\", \"1985-04-12\"]]]", "X-DATE;VALUE=date:19850412" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-time-offset\", {}, \"time\", \"12:30:00-08:00\"]]]", "X-TIME-OFFSET;VALUE=time:123000-0800" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-date-time\", {}, \"date-time\", \"---15T09:45:00+01:00\"]]]", "X-DATE-TIME;VALUE=date-time:---15T094500+0100" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"bday\", {}, \"date-and-or-time\", \"T12:30\"]]]", "BDAY:T1230" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"rev\", {}, \"timestamp\", \"2013-02-14T12:30:00-05\"]]]", "REV:20130214T123000-05" },

            // ref. RFC 7095 §3.5.8 to §3.5.11: BOOLEAN, INTEGER, FLOAT and UTC-OFFSET.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-non-smoking\", {}, \"boolean\", true]]]", "X-NON-SMOKING;VALUE=boolean:TRUE" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-karma-points\", {}, \"integer\", 42]]]", "X-KARMA-POINTS;VALUE=integer:42" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-karma-points\", {}, \"integer\", 4.2e1]]]", "X-KARMA-POINTS;VALUE=integer:42" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-grade\", {}, \"float\", 1.3]]]", "X-GRADE;VALUE=float:1.3" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-grade\", {}, \"float\", 1.3e2]]]", "X-GRADE;VALUE=float:130" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"tz\", {}, \"utc-offset\", \"-05:00\"]]]", "TZ;VALUE=utc-offset:-0500" },

            // ref. RFC 7095 §5.3: the examples of an unknown value and an unknown parameter.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-complaint-uri\", {}, \"unknown\", \"mailto:abuse@example.org\"]]]", "X-COMPLAINT-URI:mailto:abuse@example.org" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-coffee-data\", {}, \"unknown\", \"Stenophylla;Guinea\\\\,Africa\"]]]", "X-COFFEE-DATA:Stenophylla;Guinea\\,Africa" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"gender\", { \"x-probability\": \"0.8\" }, \"text\", \"M\"]]]", "GENDER;X-PROBABILITY=0.8:M" },

            // ref. RFC 7095 §3.3.1.3: "extensions may define structured" values, so an array is taken for a property whose type isn't known.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-a\", {}, \"text\", [\"a\", \"b;c\"]]]]", "X-A;VALUE=text:a;b\\;c" },

            // ref. RFC 7095 §3.5.10: the exponent of a FLOAT is eliminated in the text, so no digit is lost, as it would be through a double.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-grade\", {}, \"float\", 9007199254740993e0]]]", "X-GRADE;VALUE=float:9007199254740993" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-grade\", {}, \"float\", 1.5e-3]]]", "X-GRADE;VALUE=float:0.0015" },

            // ref. RFC 7095 §3.5.9: the decimals of an INTEGER are eliminated in the text, so a long fraction isn't rounded up.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-karma-points\", {}, \"integer\", 1.99999999999999999999999999999999999999]]]", "X-KARMA-POINTS;VALUE=integer:1" },

            // A type identifier that RFC 6350 doesn't define is an extension value type, whose value is kept as it is.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-a\", {}, \"x-mytype\", \"a\\\\b\"]]]", "X-A;VALUE=x-mytype:a\\b" },

            // A TEXT value has its escaping of vCard added, and a semicolon is escaped in a component of a structured value.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"note\", {}, \"text\", \"a,b;c\\\\d\\ne\"]]]", "NOTE:a\\,b;c\\\\d\\ne" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"org\", {}, \"text\", \"a,b;c\"]]]", "ORG:a\\,b\\;c" },

            // A URI has its commas escaped, as RFC 6350 §3.4 and its errata 3845 and 3846 require.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"geo\", {}, \"uri\", \"geo:46.772673,-71.282945\"]]]", "GEO:geo:46.772673\\,-71.282945" },

            // The case of a parameter value is kept (RFC 7095 §3.4), and a value of the default type has no VALUE parameter (§3.4.1).
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"tel\", {\"type\":\"Work\"}, \"text\", \"123\"]]]", "TEL;TYPE=Work:123" },
        };

        [Theory]
        [MemberData(nameof(Properties))]
        public void ReadCard_Property_GivesTheContentLineOfRfc6350(string json, string expected)
        {
            // act
            var testResult = ReadLastProperty(json);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ReadCard_GroupParameter_IsTheGroupOfTheProperty()
        {
            // act
            var testResult = ReadLastProperty(GroupExample);

            // assert
            testResult.Should().Be(GroupExampleVcard);
        }

        [Fact]
        public void ReadCard_GroupInLowerCase_IsInUpperCase()
        {
            // act
            var testResult = ReadLastProperty("[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{\"group\":\"item1\"},\"text\",\"A\"]]]");

            // assert
            testResult.Should().Be("ITEM1.FN:A");
        }

        [Fact]
        public void ReadCards_ArrayOfJcardObjects_GivesEachCard()
        {
            // arrange
            const string json = "[[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"]]],[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"B\"]]]]";

            // act
            var testResult = Describe(ReadJcard(json));

            // assert
            testResult.Should().BeEquivalentTo(new[] { new[] { "FN:A" }, new[] { "FN:B" } }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void ReadCards_ArrayOfJcardObjects_CountsTheCardsAndProperties()
        {
            // arrange
            const string json = "[[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"]]],[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"B\"],[\"note\",{},\"text\",\"C\"]]]]";
            using (var reader = new JcardReader(new MemoryStream(Utf8(json))))
            {
                // act
                var testResult = reader.ReadCards().ToList();

                // assert
                testResult.Should().HaveCount(2);
                reader.CardsRead.Should().Be(2);
                reader.PropertiesRead.Should().Be(3);
            }
        }

        [Fact]
        public void ReadCard_ByteOrderMark_IsSkipped()
        {
            // arrange
            var data = new UTF8Encoding(true).GetPreamble().Concat(Utf8(MinimalCard)).ToArray();
            using (var reader = new JcardReader(new MemoryStream(data)))
            {
                // act
                var testResult = Describe(reader.ReadCard());

                // assert
                testResult.Should().Equal("FN:A");
            }
        }

        [Fact]
        public void ReadCard_EndOfData_IsNull()
        {
            // arrange
            using (var reader = new JcardReader(new MemoryStream(Utf8(MinimalCard))))
            {
                reader.ReadCard();

                // act
                var testResult = reader.ReadCard();

                // assert
                testResult.Should().BeNull();
            }
        }

        public static TheoryData<string, string> Nonconforming() => new TheoryData<string, string>
        {
            // ref. RFC 7095 §3.2: a jCard object, or an array of them.
            { "{\"vcard\":[]}", "$" },
            { "[\"VCARD\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"]]]", "$[0]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"]],[]]", "$[2]" },
            { "[[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"]]],\"vcard\"]", "$[1]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],\"fn\"]]", "$[1][1]" },

            // ref. RFC 7095 §3.2: BEGIN and END "MUST NOT appear as properties of the jCard".
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"begin\",{},\"text\",\"vcard\"]]]", "$[1][1][0]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"end\",{},\"text\",\"vcard\"]]]", "$[1][2][0]" },

            // ref. RFC 7095 §3.3: a property is a name, a parameters object, a type identifier and one or more values.  Its name "MUST be in lowercase".
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"FN\",{},\"text\",\"A\"]]]", "$[1][1][0]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\"]]]", "$[1][1]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",[],\"text\",\"A\"]]]", "$[1][1][1]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"TEXT\",\"A\"]]]", "$[1][1][2]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\",\"B\"]]]", "$[1][1][4]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-a\",{},\"unknown\",\"A\",\"B\"]]]", "$[1][2][4]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",null]]]", "$[1][1][3]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",1]]]", "$[1][1][3]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",{}]]]", "$[1][1][3]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-a\",{},\"unknown\",[\"A\"]]]]", "$[1][2][3]" },

            // ref. RFC 7095 §3.3.1.3: a structured value is an array, and RFC 6350 structures N, ADR, ORG, GENDER and CLIENTPIDMAP alone.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",[\"A\"]]]]", "$[1][1][3]" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"note\",{},\"text\",[\"A\",\"B\"]]]]", "$[1][2][3]" },

            // ref. RFC 7095 §3.4: "The name of the parameter MUST be in lowercase", and its value is a string or an array of strings.
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{\"Language\":\"en\"},\"text\",\"A\"]]]", "$[1][1][1]['Language']" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{\"language\":\"en\",\"language\":\"fr\"},\"text\",\"A\"]]]", "$[1][1][1]['language']" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{\"type\":[]},\"text\",\"A\"]]]", "$[1][1][1]['type']" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{\"pref\":1},\"text\",\"A\"]]]", "$[1][1][1]['pref']" },

            // ref. RFC 7095 §3.4.1: the VALUE parameter "MUST NOT be added to the parameters object".
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{\"value\":\"text\"},\"text\",\"A\"]]]", "$[1][1][1]['value']" },

            // ref. RFC 7095 §7.1: the group "is restricted to characters allowed in property names" and "MUST NOT be empty".
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{\"group\":\"a.b\"},\"text\",\"A\"]]]", "$[1][1][1]['group']" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{\"group\":\"\"},\"text\",\"A\"]]]", "$[1][1][1]['group']" },
            { "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{\"group\":[\"a\"]},\"text\",\"A\"]]]", "$[1][1][1]['group']" },
        };

        [Theory]
        [MemberData(nameof(Nonconforming))]
        public void ReadCard_NotTheStructureOfJcard_ThrowsAtThePath(string json, string expectedPath)
        {
            // act
            Action testAction = () => ReadJcard(json);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.JsonPath].Should().Be(expectedPath);
        }

        [Theory]
        [MemberData(nameof(Nonconforming))]
        public void ReadCard_NotTheStructureOfJcard_IsAnInvalidStructure(string json, string expectedPath)
        {
            // act
            Action testAction = () => ReadJcard(json);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.InvalidStructure,
                "the problem is at {0}", expectedPath);
        }

        [Fact]
        public void ReadCard_UpperCaseName_GivesTheBytePositionOfTheName()
        {
            // arrange: the name "FN" starts at byte 39.
            const long expected = 39;

            // act
            Action testAction = () => ReadJcard("[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"FN\",{},\"text\",\"A\"]]]");

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.BytePosition].Should().Be(expected);
        }

        [Fact]
        public void ReadCard_Version30_ThrowsUnsupportedVersion()
        {
            // act
            Action testAction = () => ReadJcard("[\"vcard\",[[\"version\",{},\"text\",\"3.0\"],[\"fn\",{},\"text\",\"A\"]]]");

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(VcardDiagnosticKind.UnsupportedVersion);
        }

        [Fact]
        public void ReadCard_Version30_GivesThePathOfTheVersion()
        {
            // act
            Action testAction = () => ReadJcard("[\"vcard\",[[\"version\",{},\"text\",\"3.0\"],[\"fn\",{},\"text\",\"A\"]]]");

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.JsonPath].Should().Be("$[1][0]");
        }

        [Fact]
        public void ReadCard_NotValidJson_GivesTheLineAndPosition()
        {
            // arrange: the x is the fourth byte of the second line.
            const long expectedLine = 2;
            const long expectedPosition = 4;

            // act
            Action testAction = () => ReadJcard("[\"vcard\",\n  [x]]");

            // assert
            var data = testAction.Should().Throw<VcardFileException>().Which.Data;
            data[VcardFileException.DataItemName.LineNumber].Should().Be(expectedLine);
            data[VcardFileException.DataItemName.LinePosition].Should().Be(expectedPosition);
        }

        [Fact]
        public void ReadCard_StringWithALoneSurrogate_Throws()
        {
            // act
            Action testAction = () => ReadJcard("[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"\\ud800\"]]]");

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Fact]
        public void ReadCards_EmptyArray_ThrowsSinceThereIsNoCard()
        {
            // act
            Action testAction = () => ReadJcard("[]");

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Fact]
        public void ReadCards_EmptyArray_GivesTheBytePositionOfTheEndOfTheData()
        {
            // arrange: the data is the two bytes "[]".
            const long expected = 2;

            // act
            Action testAction = () => ReadJcard("[]");

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.BytePosition].Should().Be(expected);
        }

        [Fact]
        public void ReadCard_BirthdayInBasicFormatAllowed_KeepsTheValueAsItIs()
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = null, AllowInvalidValues = true };

            // act
            var testResult = ReadLastProperty(BirthdayInBasicFormat, options);

            // assert
            testResult.Should().Be("BDAY:19850412");
        }

        // ref. RFC 6350 §6.2.1: FN is required.
        private const string NoFormattedName = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"]]]";

        // ref. RFC 6350 §6.7.9: VERSION is required, and comes first; RFC 7095 §3.3.1.1 says the same of the version property.
        private const string NoVersion = "[\"vcard\",[[\"fn\",{},\"text\",\"A\"]]]";
        private const string VersionNotFirst = "[\"vcard\",[[\"fn\",{},\"text\",\"A\"],[\"version\",{},\"text\",\"4.0\"]]]";

        // ref. RFC 6350 §4.3.4: a value that isn't a DATE-AND-OR-TIME.
        private const string InvalidBirthday = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"bday\",{},\"date-and-or-time\",\"circa 1800\"]]]";

        // ref. RFC 6350 §6.2.5: BDAY is a DATE-AND-OR-TIME or TEXT, so the example of RFC 7095 §3.5.3 that gives it the type date isn't valid.
        private const string BirthdayAsDate = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"bday\", {}, \"date\", \"1985-04-12\"]]]";

        // ref. RFC 7095 §3.5.3: a date is in the extended format, so the basic format, though a valid value of vCard, isn't one of jCard.
        private const string BirthdayInBasicFormat = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"bday\",{},\"date-and-or-time\",\"19850412\"]]]";

        // ref. RFC 7095 §3.5.9: an INTEGER is a JSON number, so a string isn't one.
        private const string IntegerAsString = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"x-karma-points\",{},\"integer\",\"42\"]]]";

        // ref. RFC 6350 §6.1.4: KIND has at most one instance.
        private const string TwoKinds = "[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"kind\",{},\"text\",\"individual\"],[\"kind\",{},\"text\",\"org\"]]]";

        public static TheoryData<string, VcardDiagnosticKind> BreaksRfc6350() => new TheoryData<string, VcardDiagnosticKind>
        {
            { NoFormattedName, VcardDiagnosticKind.MissingRequiredProperty },
            { NoVersion, VcardDiagnosticKind.MissingRequiredProperty },
            { VersionNotFirst, VcardDiagnosticKind.InvalidStructure },
            { InvalidBirthday, VcardDiagnosticKind.InvalidValue },
            { BirthdayAsDate, VcardDiagnosticKind.InvalidValue },
            { BirthdayInBasicFormat, VcardDiagnosticKind.InvalidValue },
            { IntegerAsString, VcardDiagnosticKind.InvalidValue },
            { TwoKinds, VcardDiagnosticKind.CardinalityViolation },
        };

        [Theory]
        [MemberData(nameof(BreaksRfc6350))]
        public void ReadCard_CardBreakingRfc6350WithDefaultOptions_Throws(string json, VcardDiagnosticKind expected)
        {
            // act
            Action testAction = () => ReadJcard(json);

            // assert
            testAction.Should().Throw<VcardFileException>().Which.Data[VcardFileException.DataItemName.DiagnosticKind].Should().Be(expected);
        }

        public static TheoryData<string, VcardOptions> BreaksRfc6350WithTheOptionThatAcceptsIt() => new TheoryData<string, VcardOptions>
        {
            { NoFormattedName, new VcardOptions { MaxLineLength = null, AllowMissingRequiredProperties = true } },
            { NoVersion, new VcardOptions { MaxLineLength = null, AllowMissingRequiredProperties = true } },
            { VersionNotFirst, new VcardOptions { MaxLineLength = null, AllowVersionNotFirst = true } },
            { InvalidBirthday, new VcardOptions { MaxLineLength = null, AllowInvalidValues = true } },
            { BirthdayAsDate, new VcardOptions { MaxLineLength = null, AllowInvalidValues = true } },
            { BirthdayInBasicFormat, new VcardOptions { MaxLineLength = null, AllowInvalidValues = true } },
            { IntegerAsString, new VcardOptions { MaxLineLength = null, AllowInvalidValues = true } },
            { TwoKinds, new VcardOptions { MaxLineLength = null, AllowCardinalityViolations = true } },
        };

        [Theory]
        [MemberData(nameof(BreaksRfc6350WithTheOptionThatAcceptsIt))]
        public void ReadCard_CardBreakingRfc6350WithTheOptionThatAcceptsIt_IsRead(string json, VcardOptions options)
        {
            // act
            var testResult = ReadJcard(json, options);

            // assert
            testResult.Should().ContainSingle();
        }

        [Fact]
        public void ReadCard_InvalidValueAllowed_KeepsTheValueAsItIs()
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = null, AllowInvalidValues = true };

            // act
            var testResult = ReadLastProperty(InvalidBirthday, options);

            // assert
            testResult.Should().Be("BDAY:circa 1800");
        }

        [Fact]
        public void ReadCard_CardBreakingRfc6350_GivesTheCardIndexAndProperty()
        {
            // act
            Action testAction = () => ReadJcard("[[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"]]],[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"A\"],[\"bday\",{},\"date-and-or-time\",\"circa 1800\"]]]]");

            // assert
            var data = testAction.Should().Throw<VcardFileException>().Which.Data;
            data[VcardFileException.DataItemName.CardIndex].Should().Be(1);
            data[VcardFileException.DataItemName.PropertyName].Should().Be("BDAY");
        }

        [Fact]
        public void ReadCard_AfterACardBreakingRfc6350_ReadsTheNextCard()
        {
            // arrange
            const string json = "[[\"vcard\",[[\"version\",{},\"text\",\"4.0\"]]],[\"vcard\",[[\"version\",{},\"text\",\"4.0\"],[\"fn\",{},\"text\",\"B\"]]]]";
            using (var reader = new JcardReader(new MemoryStream(Utf8(json))))
            {
                ReadPastFailure(reader);

                // act
                var testResult = Describe(reader.ReadCard());

                // assert
                testResult.Should().Equal("FN:B");
            }
        }

        [Fact]
        public void ReadCard_AfterDataThatIsNotJcard_ThrowsInvalidOperation()
        {
            // arrange
            using (var reader = new JcardReader(new MemoryStream(Utf8("[\"vcard\",[[\"FN\",{},\"text\",\"A\"]]]"))))
            {
                ReadPastFailure(reader);

                // act
                Action testAction = () => reader.ReadCard();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadCard_TokensSplitAcrossReads_ReadsTheCard()
        {
            // arrange
            var stream = new OneByteReadStream(Utf8(RfcExampleJcard));
            var expected = Describe(ReadJcard(RfcExampleJcard));
            using (var reader = new JcardReader(stream))
            {
                // act
                var testResult = Describe(reader.ReadCards());

                // assert
                testResult.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
            }
        }

        [Fact]
        public void Constructor_EncodingThatIsNotUtf8_Throws()
        {
            // arrange
            var options = new VcardOptions { MaxLineLength = null, Encoding = Encoding.Unicode };

            // act
            Action testAction = () => new JcardReader(new MemoryStream(), options);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // act
            Action testAction = () => new JcardReader((Stream)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("jcardStream");
        }

        [Fact]
        public void Constructor_StreamThatCannotBeRead_Throws()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream();

            // act
            Action testAction = () => new JcardReader(stream);

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("jcardStream");
        }

        [Fact]
        public void Constructor_NullPath_Throws()
        {
            // act
            Action testAction = () => new JcardReader((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("jcardFilePath");
        }

        [Fact]
        public void ReadCard_Disposed_Throws()
        {
            // arrange
            var reader = new JcardReader(new MemoryStream(Utf8(MinimalCard)));
            reader.Dispose();

            // act
            Action testAction = () => reader.ReadCard();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void CardsRead_Disposed_Throws()
        {
            // arrange
            var reader = new JcardReader(new MemoryStream(Utf8(MinimalCard)));
            reader.Dispose();

            // act
            Action testAction = () => _ = reader.CardsRead;

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_CallerSuppliedStream_IsLeftOpen()
        {
            // arrange
            var stream = new MemoryStream(Utf8(MinimalCard));
            var reader = new JcardReader(stream);

            // act
            reader.Dispose();

            // assert
            stream.CanRead.Should().BeTrue();
        }

        /// <summary>
        /// A stream that gives one byte at each read, so that every JSON token is split across blocks.
        /// </summary>
        private sealed class OneByteReadStream : MemoryStream
        {
            public OneByteReadStream(byte[] data) : base(data)
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return base.Read(buffer, offset, Math.Min(count, 1));
            }
        }
    }
}
