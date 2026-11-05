using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of what <see cref="VcardCard.Validate"/> finds, which is what a reader and writer enforce.  The cards are built from their lines, so that nothing else stands in the way.
    /// </summary>
    public class VcardConformanceTests
    {
        public static TheoryData<string, VcardDiagnosticKind> ViolationData()
        {
            return new TheoryData<string, VcardDiagnosticKind>
            {
                // ref. RFC 6350 §5.1, §5.3 and §5.5: the values of parameters.
                { "NOTE;LANGUAGE=not_a_tag:a", VcardDiagnosticKind.InvalidParameter },
                { "TEL;PREF=0:1", VcardDiagnosticKind.InvalidParameter },
                { "TEL;PREF=00:1", VcardDiagnosticKind.InvalidParameter },
                { "TEL;PREF=101:1", VcardDiagnosticKind.InvalidParameter },
                { "TEL;PREF=001:1", VcardDiagnosticKind.InvalidParameter },
                { "TEL;PREF=099:1", VcardDiagnosticKind.InvalidParameter },
                { "TEL;PREF=x:1", VcardDiagnosticKind.InvalidParameter },
                { "TEL;PREF=1;PREF=2:1", VcardDiagnosticKind.InvalidParameter },
                { "TEL;PREF=1,2:1", VcardDiagnosticKind.InvalidParameter },
                { "TEL;PID=0:1", VcardDiagnosticKind.InvalidParameter },
                { "TEL;PID=a:1", VcardDiagnosticKind.InvalidParameter },
                { "X-A;PREF=0:1", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6350 §6.7.7: "Each distinct source identifier present in a vCard MUST have an associated CLIENTPIDMAP."
                { "TEL;PID=1.1:1", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6350 §5.5: PID "MUST NOT appear on properties that may have only one instance per vCard", and §6.7.7 CLIENTPIDMAP.
                { "N;PID=1:a;;;;", VcardDiagnosticKind.InvalidParameter },
                { "BDAY;PID=1:19960415", VcardDiagnosticKind.InvalidParameter },
                { "CLIENTPIDMAP;PID=1:1;urn:a", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6350 §5.3: PREF "MAY be applied to any property that allows multiple instances".
                { "GENDER;PREF=1:M", VcardDiagnosticKind.InvalidParameter },
                { "N;PREF=1:a;;;;", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6350 §5.6: "The TYPE parameter MUST NOT be applied on other properties defined in this document."
                { "N;TYPE=work:a;;;;", VcardDiagnosticKind.InvalidParameter },
                { "PRODID;TYPE=work:a", VcardDiagnosticKind.InvalidParameter },
                { "BDAY;TYPE=work:19960415", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6350 §6.4.1 and §6.6.6: type-param-tel and type-param-related "MUST NOT" be on another property.
                { "EMAIL;TYPE=voice:a@example.com", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;TYPE=friend:a", VcardDiagnosticKind.InvalidParameter },
                { "TEL;TYPE=friend:1", VcardDiagnosticKind.InvalidParameter },
                { "RELATED;TYPE=cell:urn:a", VcardDiagnosticKind.InvalidParameter },
                { "TEL;TYPE=\"a b\":1", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6350 §6: parameters not in a property's ABNF.
                { "KIND;LANGUAGE=en:individual", VcardDiagnosticKind.InvalidParameter },
                { "EMAIL;LANGUAGE=en:a@example.com", VcardDiagnosticKind.InvalidParameter },
                { "ANNIVERSARY;LANGUAGE=en:19960415", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;MEDIATYPE=text/plain:a", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;SORT-AS=a:b", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;GEO=\"geo:1,2\":a", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;TZ=a:b", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;LABEL=a:b", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;CALSCALE=gregorian:a", VcardDiagnosticKind.InvalidParameter },
                { "CLIENTPIDMAP;VALUE=text:1;urn:a", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6350 §6.1.5: XML-param has no any-param.
                { "XML;X-A=1:<a xmlns=\"urn:b\"/>", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6350 §6.2.5, §6.4.1, §6.6.6 and §6.8.1: "Value and parameter MUST match."
                { "BDAY;LANGUAGE=en:19960415", VcardDiagnosticKind.InvalidParameter },
                { "RELATED;LANGUAGE=en:urn:a", VcardDiagnosticKind.InvalidParameter },
                { "TEL;MEDIATYPE=text/plain:1", VcardDiagnosticKind.InvalidParameter },
                { "KEY;VALUE=text;MEDIATYPE=text/plain:a", VcardDiagnosticKind.InvalidParameter },
                { "RELATED;VALUE=text;MEDIATYPE=text/plain:a", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6350 §6.2.5: CALSCALE only with a date or date-time.
                { "BDAY;VALUE=text;CALSCALE=gregorian:a", VcardDiagnosticKind.InvalidParameter },
                { "BDAY;CALSCALE=gregorian:T1200", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6350 §5.7, §5.8 and §5.10: the forms of other parameters.
                { "URL;MEDIATYPE=plain:http://example.com/", VcardDiagnosticKind.InvalidParameter },
                { "BDAY;CALSCALE=\"a b\":19960415", VcardDiagnosticKind.InvalidParameter },
                { "ADR;GEO=\"not a uri\":;;;;;;", VcardDiagnosticKind.InvalidParameter },
                { "ADR;LABEL=a;LABEL=b:;;;;;;", VcardDiagnosticKind.InvalidParameter },
                { "ROLE;ALTID=a,b:c", VcardDiagnosticKind.InvalidParameter },
                { "ROLE;LANGUAGE=en,fr:c", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6350 §5.9: "as many or fewer elements as the corresponding property value has components".
                { "ORG;SORT-AS=\"a,b\":c", VcardDiagnosticKind.InvalidParameter },
                { "N;SORT-AS=\"a,b,c,d,e,f\":a;b;c;d;e", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6350 §5.2 and §6: VALUE only with a type the property allows.
                { "URL;VALUE=text:a", VcardDiagnosticKind.InvalidValue },
                { "FN;VALUE=uri:urn:a", VcardDiagnosticKind.InvalidValue },
                { "TEL;VALUE=date:19960415", VcardDiagnosticKind.InvalidValue },
                { "NOTE;VALUE=x-type:a", VcardDiagnosticKind.InvalidValue },
                { "BDAY;VALUE=date:19960415", VcardDiagnosticKind.InvalidValue },
                { "TZ;VALUE=date-time:19960415T000000", VcardDiagnosticKind.InvalidValue },

                // ref. RFC 6350 §4 and §6: the syntax of each value.
                { "NOTE:a,b", VcardDiagnosticKind.InvalidValue },
                { "NOTE:a\\xb", VcardDiagnosticKind.InvalidValue },
                { "NICKNAME:a\\xb,c", VcardDiagnosticKind.InvalidValue },
                { "N:a;b", VcardDiagnosticKind.InvalidValue },
                { "ADR:a;b", VcardDiagnosticKind.InvalidValue },
                { "ORG:a\\xb", VcardDiagnosticKind.InvalidValue },
                { "GENDER:X", VcardDiagnosticKind.InvalidValue },
                { "CLIENTPIDMAP:0;urn:a", VcardDiagnosticKind.InvalidValue },
                { "CLIENTPIDMAP:a", VcardDiagnosticKind.InvalidValue },
                { "PHOTO:not a uri", VcardDiagnosticKind.InvalidValue },
                { "UID:free text", VcardDiagnosticKind.InvalidValue },
                { "KEY:free text", VcardDiagnosticKind.InvalidValue },
                { "RELATED:free text", VcardDiagnosticKind.InvalidValue },
                { "BDAY:1996-04-15", VcardDiagnosticKind.InvalidValue },
                { "BDAY:19960230", VcardDiagnosticKind.InvalidValue },
                { "REV:19960415", VcardDiagnosticKind.InvalidValue },
                { "LANG:not_a_tag", VcardDiagnosticKind.InvalidValue },
                { "TZ;VALUE=utc-offset:Z", VcardDiagnosticKind.InvalidValue },

                // ref. RFC 6350 §6.1.4: KIND-value is an iana-token or x-name.
                { "KIND:a b", VcardDiagnosticKind.InvalidValue },

                // ref. RFC 6350 §6.1.5: one XML element, in an explicit namespace that isn't vCard 4.0's.
                { "XML:<a/>", VcardDiagnosticKind.InvalidValue },
                { "XML:<a xmlns=\"urn:ietf:params:xml:ns:vcard-4.0\"/>", VcardDiagnosticKind.InvalidValue },
                { "XML:<a xmlns=\"urn:b\"/><c xmlns=\"urn:b\"/>", VcardDiagnosticKind.InvalidValue },
                { "XML:<a xmlns=\"urn:b\">", VcardDiagnosticKind.InvalidValue },
                { "XML:text", VcardDiagnosticKind.InvalidValue },
                { "XML:<!DOCTYPE a><a xmlns=\"urn:b\"/>", VcardDiagnosticKind.InvalidValue },

                // ref. RFC 6350 §6.6.5: MEMBER only with KIND:group.
                { "MEMBER:urn:a", VcardDiagnosticKind.CardinalityViolation },

                // ref. RFC 6350 Appendix A: what vCard 4.0 removed.
                { "NOTE;ENCODING=b:YQ==", VcardDiagnosticKind.LegacySyntax },
                { "NOTE;CHARSET=UTF-8:a", VcardDiagnosticKind.LegacySyntax },
                { "TEL;ENCODING=7BIT:1", VcardDiagnosticKind.LegacySyntax }
            };
        }

        [Theory]
        [MemberData(nameof(ViolationData))]
        public void Validate_RuleBroken_ReportsIt(string property, VcardDiagnosticKind expected)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property), new VcardOptions { AllowInvalidValues = true, AllowCardinalityViolations = true, AcceptLegacySyntax = true });

            // act
            var testResult = card.Validate();

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(expected);
        }

        public static TheoryData<string> PropertyWithOneInstanceData()
        {
            // ref. RFC 6350 §6 and RFC 6474 §2: the properties with cardinality *1.
            return new TheoryData<string>
            {
                "KIND:individual", "N:a;;;;", "BDAY:19960415", "ANNIVERSARY:19960415", "GENDER:M", "PRODID:a", "REV:19951031T222710Z", "UID:urn:a", "BIRTHPLACE:a", "DEATHPLACE:a", "DEATHDATE:19960415"
            };
        }

        [Theory]
        [MemberData(nameof(PropertyWithOneInstanceData))]
        public void Validate_TwoOfAPropertyWithOneInstance_ReportsCardinalityViolation(string property)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property, property), new VcardOptions { AllowCardinalityViolations = true });

            // act
            var testResult = card.Validate();

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(VcardDiagnosticKind.CardinalityViolation);
        }

        public static TheoryData<string[]> ConformingPropertiesData()
        {
            return new TheoryData<string[]>
            {
                // ref. RFC 6350 §5.4: the examples of what is legal; "Property instances having the same ALTID parameter value count as 1 toward cardinality."
                new[] { "N;ALTID=1;LANGUAGE=ja:a;b;;;", "N;ALTID=1;LANGUAGE=en:Yamada;Taro;;;" },
                new[] { "TITLE;ALTID=1;LANGUAGE=fr:Patron", "TITLE;ALTID=1;LANGUAGE=en:Boss" },
                new[] { "TITLE;ALTID=1;LANGUAGE=fr:Patron", "TITLE;ALTID=2;LANGUAGE=en:Boss" },

                // ref. RFC 6350 §5.4: "Values for the ALTID parameter are not globally unique: they MAY be reused for different property names."
                new[] { "TITLE;ALTID=1:a", "NOTE;ALTID=1:b" },
                new[] { "N;ALTID=1:a;;;;", "BDAY;ALTID=1:19960415" },

                // ref. RFC 6350 §6.6.5: MEMBER with KIND:group, in either order and any case.
                new[] { "KIND:group", "MEMBER:urn:a" },
                new[] { "MEMBER:urn:a", "KIND:GROUP" },

                // ref. RFC 6350 §6.1.4: a value of KIND that isn't defined is kept.
                new[] { "KIND:x-robot" },

                // ref. RFC 6350 §6.7.7 and §7.2: PID with and without a source.
                new[] { "EMAIL;PID=1.1:a@example.com", "CLIENTPIDMAP:1;urn:a" },
                new[] { "EMAIL;PID=4.2,5.1:a@example.com", "CLIENTPIDMAP:1;urn:a", "CLIENTPIDMAP:2;urn:b" },
                new[] { "EMAIL;PID=1:a@example.com" },
                new[] { "EMAIL;PID=1.1;PID=2.1:a@example.com", "CLIENTPIDMAP:1;urn:a" },

                // ref. RFC 6350 §6.4.1: TYPE as a parameter list and as a value list.
                new[] { "TEL;TYPE=text;TYPE=voice:1" },
                new[] { "TEL;TYPE=\"text,voice\":1" },
                new[] { "TEL;TYPE=text,voice:1" },
                new[] { "TEL;TYPE=VOICE:1" },

                // ref. RFC 6350 §5.3: the limits of PREF, with a leading zero that 1*2DIGIT allows.
                new[] { "TEL;PREF=1:1" },
                new[] { "TEL;PREF=01:1" },
                new[] { "TEL;PREF=100:1" },

                // ref. RFC 6350 §5: "Applications MUST ignore x-param and iana-param values they don't recognize."
                new[] { "FN;X-A=1:b" },
                new[] { "NOTE;NEW-PARAM=a:b" },
                new[] { "KIND;X-A=1:individual" },

                // ref. RFC 6350 §5.2: a VALUE that names the default type.
                new[] { "FN;VALUE=text:a", "URL;VALUE=uri:http://example.com/", "BDAY;VALUE=date-and-or-time:19960415", "REV;VALUE=timestamp:19951031T222710Z", "LANG;VALUE=language-tag:en" },

                // ref. RFC 6350 §6.2.5, §6.4.1, §6.6.6 and §6.8.1: value types with the parameters that go with them.
                new[] { "BDAY;VALUE=text;LANGUAGE=en:circa 1800" },
                new[] { "BDAY;CALSCALE=gregorian:19960415" },
                new[] { "BDAY;CALSCALE=GREGORIAN:19960415T1200" },

                // ref. RFC 6350 §5.8: a property with a CALSCALE that isn't understood is ignored, so its value can't be judged.
                new[] { "BDAY;CALSCALE=x-mayan:19960230" },

                new[] { "TEL;VALUE=uri;MEDIATYPE=text/plain:tel:+1" },
                new[] { "RELATED;VALUE=text;LANGUAGE=en:a" },
                new[] { "RELATED;MEDIATYPE=text/vcard:urn:a" },
                new[] { "KEY;VALUE=text:a" },
                new[] { "UID;VALUE=text:a" },
                new[] { "TZ;VALUE=uri:urn:a" },
                new[] { "TZ;MEDIATYPE=text/plain:a" },

                // ref. RFC 6350 §5.9: as many elements as there are components, or fewer.
                new[] { "N;SORT-AS=\"a,b,c,d,e\":a;b;c;d;e" },
                new[] { "ORG;SORT-AS=\"a,b\":c;d" },
                new[] { "ORG;SORT-AS=a,b:c;d" },

                // ref. RFC 6350 §6.1.5: a prefixed namespace is explicit too, and comments around the element are nothing.
                new[] { "XML:<p:a xmlns:p=\"urn:b\"><c/></p:a>" },
                new[] { "XML:<!-- x --><a xmlns=\"urn:b\"/>" },

                // ref. RFC 6350 §6: properties it doesn't define can hold anything.
                new[] { "X-A:free\\xtext,with;anything" },
                new[] { "NEW-PROPERTY;PREF=1:free,text" },
                new[] { "item.X-A:a", "ITEM.NOTE:b" }
            };
        }

        [Theory]
        [MemberData(nameof(ConformingPropertiesData))]
        public void Validate_RuleKept_FindsNoProblem(string[] properties)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(properties));

            // act
            var testResult = card.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void Validate_SecondInstanceHasAnotherAltId_ReportsCardinalityViolation()
        {
            // arrange
            // ref. RFC 6350 §5.4: "while this one would not [be legal]", two instances of N.
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N;ALTID=1;LANGUAGE=ja:a;b;;;", "N:Yamada;Taro;;;"), new VcardOptions { AllowCardinalityViolations = true });

            // act
            var testResult = card.Validate();

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(VcardDiagnosticKind.CardinalityViolation);
        }

        [Fact]
        public void Validate_NoFormattedName_ReportsMissingRequiredProperty()
        {
            // arrange
            var card = new VcardCard();

            // act
            var testResult = card.Validate();

            // assert
            testResult.Single().PropertyName.Should().Be(VcardPropertyNames.FormattedName);
            testResult.Single().Kind.Should().Be(VcardDiagnosticKind.MissingRequiredProperty);
        }

        [Fact]
        public void Validate_ProblemsOnSeveralLines_GivesThemInLineOrder()
        {
            // arrange
            const long LineOfFirstProblem = 3;
            const long LineOfSecondProblem = 4;
            var options = new VcardOptions { AllowInvalidValues = true, AllowCardinalityViolations = true };
            var card = VcardCardTestData.Read(VcardCardTestData.BareCard("URL:not a uri", "MEMBER:urn:a", "FN:Jane Doe"), options);

            // act
            var testResult = card.Validate();

            // assert
            testResult.Select(diagnostic => diagnostic.LineNumber).Should().Equal(LineOfFirstProblem, LineOfSecondProblem);
        }

        [Fact]
        public void Validate_GroupOnProperty_ReportsIt()
        {
            // arrange
            const string Group = "item";
            var card = VcardCardTestData.Read(VcardCardTestData.Card("item.URL:not a uri"), new VcardOptions { AllowInvalidValues = true });

            // act
            var testResult = card.Validate();

            // assert
            testResult.Single().Group.Should().Be(Group);
        }
    }
}
