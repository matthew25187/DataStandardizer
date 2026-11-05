using System;
using System.Linq;
using DataStandardizer.Geography;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the extensions of vCard 4.0: RFC 6473, RFC 6474, RFC 6715, RFC 6869 and RFC 8605.
    /// </summary>
    public class VcardExtensionTests
    {
        /// <summary>
        /// ref. RFC 6473 §3: the example, which the RFC gives in xCard, as content lines.
        /// </summary>
        private static readonly string[] Rfc6473Example =
        {
            "FN:jabber.org IM service",
            "URL:http://www.jabber.org/",
            "LANG;PREF=1:en",
            "EMAIL:xmpp@jabber.org",
            "IMPP:xmpp:jabber.org",
            "LOGO:http://www.jabber.org/images/logo.png",
            "GEO:geo:42.25\\,-91.05",
            "TZ:America/Chicago",
            "SOURCE:xmpp:jabber.org?vcard",
            "REV:19990104T122100Z",
            "KIND:application"
        };

        /// <summary>
        /// ref. RFC 6869 §3: the example, which the RFC gives in xCard, as content lines.
        /// </summary>
        private static readonly string[] Rfc6869Example =
        {
            "KIND:device",
            "FN;TYPE=x-model-name:RTR1001",
            "FN:core-rtr-1.example.net",
            "URL:http://www.example.com/support/index.html",
            "EMAIL:support@example.com",
            "EMAIL;TYPE=x-local-support:network-support@example.net",
            "IMPP:xmpp:core-rtr-1@example.net",
            "RELATED;TYPE=contact:urn:uuid:5CEF1870-0326-11E2-A21F-0800200C9A66",
            "LOGO:http://www.example.com/images/logo.png",
            "GEO:geo:35.82\\,-78.64",
            "TZ:America/New_York",
            "REV:20120104T213000Z",
            "UID:urn:uuid:00CCFB88-155F-40F6-B9D9-B04D134860C0",
            "XML:<serial-number xmlns='http://example.org/profiles/serial-number'>FTX1234ABCD</serial-number>",
            "NOTE;TYPE=x-contract-number:1234567",
            "XML:<mac xmlns='http://example.org/profiles/mac'>00-00-5E-00-00-01</mac>",
            "XML:<sw-version xmlns='http://example.org/profiles/sw-version'>2.1.5</sw-version>"
        };

        public static TheoryData<string> RfcExampleData()
        {
            return new TheoryData<string>
            {
                // ref. RFC 6474 §2.1, with the comma escaped as its erratum 4557 says.
                "BIRTHPLACE:Babies'R'Us Hospital",
                "BIRTHPLACE;VALUE=uri:http://example.com/hospitals/babiesrus.vcf",
                "BIRTHPLACE;VALUE=uri:geo:46.769307\\,-71.283079",

                // ref. RFC 6474 §2.2, with the comma escaped as its erratum 4559 says.
                "DEATHPLACE:Aboard the Titanic\\, near Newfoundland",
                "DEATHPLACE;VALUE=uri:http://example.com/ships/titanic.vcf",
                "DEATHPLACE;VALUE=uri:geo:41.731944\\,-49.945833",

                // ref. RFC 6474 §2.3, with the colon its erratum 4556 corrects.
                "DEATHDATE:19960415",
                "DEATHDATE:--0415",
                "DEATHDATE:19531015T231000Z",
                "DEATHDATE;VALUE=text:circa 1800",

                // ref. RFC 6715 §2.1.
                "EXPERTISE;LEVEL=beginner;INDEX=2:chinese literature",
                "EXPERTISE;INDEX=1;LEVEL=expert:chemistry",

                // ref. RFC 6715 §2.2.
                "HOBBY;INDEX=1;LEVEL=high:reading",
                "HOBBY;INDEX=2;LEVEL=high:sewing",

                // ref. RFC 6715 §2.3.
                "INTEREST;INDEX=1;LEVEL=medium:r&b music",
                "INTEREST;INDEX=2;LEVEL=high:rock 'n' roll music",

                // ref. RFC 6715 §2.4, with the comma escaped as RFC 6350 §3.4 says.
                "ORG-DIRECTORY;INDEX=1:http://directory.mycompany.example.com",
                "ORG-DIRECTORY;PREF=1:ldap://ldap.tech.example/o=Example%20Tech\\,ou=Engineering",

                // ref. RFC 6715 §3.1, with the name its erratum 3340 corrects.
                "ORG-DIRECTORY;INDEX=1:http://mycompany.example1.com",
                "ORG-DIRECTORY;PREF=1;INDEX=2:http://mycompany.example2.com",

                // ref. RFC 6715 §3.2.
                "EXPERTISE;LEVEL=beginner:chinese literature",
                "HOBBY;LEVEL=high:reading",
                "INTEREST;LEVEL=medium:r&b music",

                // ref. RFC 8605 §2.1.
                "CONTACT-URI:https://contact.example.com",
                "CONTACT-URI;PREF=1:mailto:contact@example.com",

                // ref. RFC 8605 §3.1.
                "ADR;TYPE=work;CC=US:;;54321 Oak St;Reston;VA;20190;USA",
                "ADR;TYPE=home;CC=US:;;12345 Elm St;Reston;VA;20190;USA"
            };
        }

        [Theory]
        [MemberData(nameof(RfcExampleData))]
        public void Validate_RfcExample_FindsNoProblem(string property)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property));

            // act
            var testResult = card.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        public static TheoryData<string[], VcardKind> KindExampleData()
        {
            return new TheoryData<string[], VcardKind>
            {
                { Rfc6473Example, VcardKind.Application },
                { Rfc6869Example, VcardKind.Device }
            };
        }

        [Theory]
        [MemberData(nameof(KindExampleData))]
        public void Kind_RfcExample_IsTheKindItShows(string[] properties, VcardKind expected)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.BareCard(properties));

            // act
            var testResult = card.Kind;

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<string[]> KindExamplePropertiesData()
        {
            return new TheoryData<string[]> { Rfc6473Example, Rfc6869Example };
        }

        [Theory]
        [MemberData(nameof(KindExamplePropertiesData))]
        public void Validate_RfcExampleOfAKind_FindsNoProblem(string[] properties)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.BareCard(properties));

            // act
            var testResult = card.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void ReadCard_Rfc6474DeathDateAsPrinted_Throws()
        {
            // arrange
            // ref. RFC 6474 §2.3: printed with a semicolon, which its erratum 4556 corrects to a colon, so it is not a content line.
            var text = VcardCardTestData.Card("DEATHDATE;19531015T231000Z");

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        public static TheoryData<string, VcardDiagnosticKind> BrokenRuleData()
        {
            return new TheoryData<string, VcardDiagnosticKind>
            {
                // ref. RFC 6715 §3.2: "'beginner', 'average', 'expert' when used with EXPERTISE", and "'high', 'medium', 'low' when used with HOBBY or INTEREST".
                { "EXPERTISE;LEVEL=high:a", VcardDiagnosticKind.InvalidParameter },
                { "EXPERTISE;LEVEL=medium:a", VcardDiagnosticKind.InvalidParameter },
                { "EXPERTISE;LEVEL=low:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;LEVEL=beginner:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;LEVEL=average:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;LEVEL=expert:a", VcardDiagnosticKind.InvalidParameter },
                { "INTEREST;LEVEL=beginner:a", VcardDiagnosticKind.InvalidParameter },
                { "INTEREST;LEVEL=expert:a", VcardDiagnosticKind.InvalidParameter },
                { "EXPERTISE;LEVEL=x:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;LEVEL=x:a", VcardDiagnosticKind.InvalidParameter },
                { "X-A;LEVEL=x:a", VcardDiagnosticKind.InvalidParameter },
                { "EXPERTISE;LEVEL=expert,average:a", VcardDiagnosticKind.InvalidParameter },
                { "EXPERTISE;LEVEL=expert;LEVEL=average:a", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6715 §3.1: "INDEX values must be strictly positive.  Zero is not allowed."
                { "HOBBY;INDEX=0:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;INDEX=00:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;INDEX=+0:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;INDEX=-1:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;INDEX=a:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;INDEX=1.5:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;INDEX=9223372036854775808:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;INDEX=1,2:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;INDEX=1;INDEX=2:a", VcardDiagnosticKind.InvalidParameter },
                { "X-A;INDEX=0:a", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 8605 §3.1: CC-param = "CC=" 2ALPHA, an ISO 3166 code.
                { "ADR;CC=ZZ:;;;;;;", VcardDiagnosticKind.InvalidParameter },
                { "ADR;CC=USA:;;;;;;", VcardDiagnosticKind.InvalidParameter },
                { "ADR;CC=U:;;;;;;", VcardDiagnosticKind.InvalidParameter },
                { "ADR;CC=20:;;;;;;", VcardDiagnosticKind.InvalidParameter },
                { "ADR;CC=US,CA:;;;;;;", VcardDiagnosticKind.InvalidParameter },
                { "ADR;CC=US;CC=CA:;;;;;;", VcardDiagnosticKind.InvalidParameter },
                { "X-A;CC=ZZ:a", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 8605 §3.1: "This parameter is used to extend the ADR property".
                { "NOTE;CC=US:a", VcardDiagnosticKind.InvalidParameter },
                { "TEL;CC=US:1", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6715 §2 and §3: LEVEL and INDEX are in the ABNF of its own properties.
                { "NOTE;LEVEL=high:a", VcardDiagnosticKind.InvalidParameter },
                { "ORG-DIRECTORY;LEVEL=high:urn:a", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;INDEX=1:a", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6715 §2: the parameters not in the ABNF of each property, which has no VALUE.
                { "EXPERTISE;PID=1:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;PID=1:a", VcardDiagnosticKind.InvalidParameter },
                { "INTEREST;PID=1:a", VcardDiagnosticKind.InvalidParameter },
                { "EXPERTISE;VALUE=text:a", VcardDiagnosticKind.InvalidParameter },
                { "HOBBY;VALUE=text:a", VcardDiagnosticKind.InvalidParameter },
                { "INTEREST;VALUE=text:a", VcardDiagnosticKind.InvalidParameter },
                { "ORG-DIRECTORY;VALUE=uri:urn:a", VcardDiagnosticKind.InvalidParameter },
                { "ORG-DIRECTORY;MEDIATYPE=text/plain:urn:a", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 8605 §2.1: CONTACT-URI-param = "VALUE=uri" / pref-param.
                { "CONTACT-URI;TYPE=work:http://example.com/", VcardDiagnosticKind.InvalidParameter },
                { "CONTACT-URI;INDEX=1:http://example.com/", VcardDiagnosticKind.InvalidParameter },
                { "CONTACT-URI;X-A=1:http://example.com/", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6474 §2: BIRTHPLACE-param and DEATHPLACE-param.
                { "BIRTHPLACE;PREF=1:a", VcardDiagnosticKind.InvalidParameter },
                { "BIRTHPLACE;TYPE=home:a", VcardDiagnosticKind.InvalidParameter },
                { "DEATHPLACE;PID=1:a", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6474 §2.3: "CALSCALE can only be present when the value is a date-and-or-time value and actually contains a date or date-time.  LANGUAGE can only be present
                // when the value is text."
                { "DEATHDATE;PREF=1:19960415", VcardDiagnosticKind.InvalidParameter },
                { "DEATHDATE;LANGUAGE=en:19960415", VcardDiagnosticKind.InvalidParameter },
                { "DEATHDATE;VALUE=text;CALSCALE=gregorian:a", VcardDiagnosticKind.InvalidParameter },
                { "DEATHDATE;CALSCALE=gregorian:T1200", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 6474 §2: the value types of each property.
                { "BIRTHPLACE;VALUE=date:19960415", VcardDiagnosticKind.InvalidValue },
                { "BIRTHPLACE;VALUE=uri:not a uri", VcardDiagnosticKind.InvalidValue },
                { "BIRTHPLACE:a,b", VcardDiagnosticKind.InvalidValue },
                { "DEATHPLACE;VALUE=uri:not a uri", VcardDiagnosticKind.InvalidValue },
                { "DEATHDATE:1996-04-15", VcardDiagnosticKind.InvalidValue },
                { "DEATHDATE;VALUE=uri:urn:a", VcardDiagnosticKind.InvalidValue },

                // ref. RFC 6715 §2: the value types of each property.
                { "EXPERTISE:a,b", VcardDiagnosticKind.InvalidValue },
                { "HOBBY:a\\xb", VcardDiagnosticKind.InvalidValue },
                { "INTEREST:a\\xb", VcardDiagnosticKind.InvalidValue },
                { "ORG-DIRECTORY:not a uri", VcardDiagnosticKind.InvalidValue }
            };
        }

        [Theory]
        [MemberData(nameof(BrokenRuleData))]
        public void Validate_RuleBroken_ReportsIt(string property, VcardDiagnosticKind expected)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property), new VcardOptions { AllowInvalidValues = true });

            // act
            var testResult = card.Validate();

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(expected);
        }

        public static TheoryData<string> BrokenRulePropertyData()
        {
            var data = new TheoryData<string>();
            foreach (var row in BrokenRuleData())
            {
                data.Add((string)row[0]);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(BrokenRulePropertyData))]
        public void ReadCard_RuleBrokenWithDefaultOptions_Throws(string property)
        {
            // arrange
            var text = VcardCardTestData.Card(property);

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        public static TheoryData<string[]> ConformingPropertiesData()
        {
            return new TheoryData<string[]>
            {
                // ref. RFC 6474 §2: the parameters of each property, in which LANGUAGE on a place isn't limited to text.
                new[] { "BIRTHPLACE;ALTID=1;LANGUAGE=fr:a", "BIRTHPLACE;ALTID=1;LANGUAGE=en:b" },
                new[] { "DEATHPLACE;VALUE=text;LANGUAGE=fr:a" },
                new[] { "DEATHPLACE;VALUE=uri;LANGUAGE=fr:urn:a" },
                new[] { "DEATHDATE;ALTID=1;CALSCALE=gregorian:19960415", "DEATHDATE;ALTID=1;VALUE=text;LANGUAGE=en:circa 1996" },
                new[] { "DEATHDATE;VALUE=date-and-or-time:19960415" },
                new[] { "BIRTHPLACE;X-A=1:a" },

                // ref. RFC 6715 §2: several instances, and the parameters of each property.
                new[] { "EXPERTISE:a", "EXPERTISE:b" },
                new[] { "HOBBY:a", "HOBBY:b" },
                new[] { "INTEREST:a", "INTEREST:b" },
                new[] { "ORG-DIRECTORY:urn:a", "ORG-DIRECTORY:urn:b" },
                new[] { "EXPERTISE;LEVEL=average;INDEX=1;LANGUAGE=en;PREF=1;ALTID=a;TYPE=work;X-A=1:b" },
                new[] { "HOBBY;LEVEL=medium;INDEX=1;LANGUAGE=en;PREF=1;ALTID=a;TYPE=work;X-A=1:b" },
                new[] { "INTEREST;LEVEL=low;INDEX=1;LANGUAGE=en;PREF=1;ALTID=a;TYPE=work;X-A=1:b" },
                new[] { "ORG-DIRECTORY;PREF=1;INDEX=1;LANGUAGE=en;PID=1;ALTID=a;TYPE=work;X-A=1:urn:b" },

                // ref. RFC 6350 §5: "Property parameter values that are not in quoted-strings are case-insensitive."
                new[] { "HOBBY;LEVEL=HIGH:a" },
                new[] { "ADR;CC=us:;;;;;;" },

                // ref. RFC 6350 §4: integer = [sign] 1*DIGIT, so these are strictly positive integers.
                new[] { "HOBBY;INDEX=+1:a" },
                new[] { "HOBBY;INDEX=01:a" },
                new[] { "HOBBY;INDEX=9223372036854775807:a" },

                // ref. RFC 6715 §3.2: the RFC limits LEVEL on its own properties, and any-param lets an unknown property have it.
                new[] { "X-A;LEVEL=high;INDEX=1;CC=US:a" },

                // ref. RFC 8605 §2.1: one of the schemes, in any case, and others beside it, with a PREF as there are several.
                new[] { "CONTACT-URI:mailto:a@example.com" },
                new[] { "CONTACT-URI:http://example.com/" },
                new[] { "CONTACT-URI:https://example.com/" },
                new[] { "CONTACT-URI:HTTPS://example.com/" },
                new[] { "CONTACT-URI;VALUE=uri:https://example.com/" },
                new[] { "CONTACT-URI;PREF=1:ftp://example.com/", "CONTACT-URI:mailto:a@example.com" },
                new[] { "CONTACT-URI:mailto:a@example.com", "CONTACT-URI;PREF=2:https://example.com/" }
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
        public void Validate_ContactUriWithoutARequiredScheme_ReportsMissingRequiredProperty()
        {
            // arrange
            // ref. RFC 8605 §2.1: 'At least one "mailto", "http", or "https" URI value MUST be provided.'
            var options = new VcardOptions { AllowMissingRequiredProperties = true };
            var card = VcardCardTestData.Read(VcardCardTestData.Card("CONTACT-URI:ftp://example.com/"), options);

            // act
            var testResult = card.Validate();

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(VcardDiagnosticKind.MissingRequiredProperty);
            testResult.Single().PropertyName.Should().Be(VcardPropertyNames.ContactUri);
        }

        [Fact]
        public void ReadCard_ContactUriWithoutARequiredSchemeByDefault_Throws()
        {
            // arrange
            var text = VcardCardTestData.Card("CONTACT-URI:ftp://example.com/");

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Fact]
        public void Validate_SeveralContactUrisWithoutPref_ReportsInvalidParameter()
        {
            // arrange
            // ref. RFC 8605 §2.1: "If multiple CONTACT-URI properties are used, the vCard PREF parameter MUST be used to describe the most preferred property".
            var options = new VcardOptions { AllowInvalidValues = true };
            var card = VcardCardTestData.Read(VcardCardTestData.Card("CONTACT-URI:mailto:a@example.com", "CONTACT-URI:https://example.com/"), options);

            // act
            var testResult = card.Validate();

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(VcardDiagnosticKind.InvalidParameter);
        }

        [Fact]
        public void ReadCard_SeveralContactUrisWithoutPrefByDefault_Throws()
        {
            // arrange
            var text = VcardCardTestData.Card("CONTACT-URI:mailto:a@example.com", "CONTACT-URI:https://example.com/");

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        public static TheoryData<string, Func<VcardCard, object>, object> MemberData()
        {
            return new TheoryData<string, Func<VcardCard, object>, object>
            {
                { "BIRTHPLACE:a", card => card.BirthPlace.Value.Text, "a" },
                { "BIRTHPLACE;VALUE=uri:geo:1\\,2", card => card.BirthPlace.Value.Uri.OriginalString, "geo:1,2" },
                { "DEATHPLACE:a", card => card.DeathPlace.Value.Text, "a" },
                { "DEATHPLACE;VALUE=uri:urn:a", card => card.DeathPlace.Value.Uri.OriginalString, "urn:a" },
                { "DEATHDATE:19960415", card => card.DeathDate.Value.DateAndOrTime.ToString(VcardValueFormat.Text), "19960415" },
                { "DEATHDATE;VALUE=text:circa 1800", card => card.DeathDate.Value.Text, "circa 1800" },
                { "EXPERTISE:a\\, b", card => card.Expertise[0].Value, "a, b" },
                { "HOBBY:a", card => card.Hobbies[0].Value, "a" },
                { "INTEREST:a", card => card.Interests[0].Value, "a" },
                { "ORG-DIRECTORY:urn:a", card => card.OrganizationDirectories[0].Value.OriginalString, "urn:a" },
                { "CONTACT-URI:mailto:a@example.com", card => card.ContactUris[0].Value.OriginalString, "mailto:a@example.com" },
                { "EXPERTISE;LEVEL=beginner:a", card => card.Expertise[0].Level, VcardLevel.Beginner },
                { "EXPERTISE;LEVEL=average:a", card => card.Expertise[0].Level, VcardLevel.Average },
                { "EXPERTISE;LEVEL=expert:a", card => card.Expertise[0].Level, VcardLevel.Expert },
                { "HOBBY;LEVEL=high:a", card => card.Hobbies[0].Level, VcardLevel.High },
                { "HOBBY;LEVEL=MEDIUM:a", card => card.Hobbies[0].Level, VcardLevel.Medium },
                { "INTEREST;LEVEL=low:a", card => card.Interests[0].Level, VcardLevel.Low },
                { "HOBBY;INDEX=2:a", card => card.Hobbies[0].Index, 2L },
                { "HOBBY;INDEX=+3:a", card => card.Hobbies[0].Index, 3L },
                { "HOBBY;INDEX=9223372036854775807:a", card => card.Hobbies[0].Index, long.MaxValue },
                { "ADR;CC=US:;;;;;;", card => card.Addresses[0].CountryCode, Iso3166Part1Alpha2Country.US },
                { "ADR;CC=nz:;;;;;;", card => card.Addresses[0].CountryCode, Iso3166Part1Alpha2Country.NZ }
            };
        }

        [Theory]
        [MemberData(nameof(MemberData))]
        public void Member_CardHasAValue_GivesIt(string property, Func<VcardCard, object> member, object expected)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property));

            // act
            var testResult = member(card);

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<string, Func<VcardCard, object>> InvalidParameterMemberData()
        {
            return new TheoryData<string, Func<VcardCard, object>>
            {
                { "HOBBY;LEVEL=expert:a", card => card.Hobbies[0].Level },
                { "HOBBY;LEVEL=x:a", card => card.Hobbies[0].Level },
                { "HOBBY;INDEX=0:a", card => card.Hobbies[0].Index },
                { "ADR;CC=ZZ:;;;;;;", card => card.Addresses[0].CountryCode },
                { "HOBBY:a", card => card.Hobbies[0].Level },
                { "HOBBY:a", card => card.Hobbies[0].Index },
                { "ADR:;;;;;;", card => card.Addresses[0].CountryCode }
            };
        }

        [Theory]
        [MemberData(nameof(InvalidParameterMemberData))]
        public void Member_NoValidParameter_IsNull(string property, Func<VcardCard, object> member)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property), new VcardOptions { AllowInvalidValues = true });

            // act
            var testResult = member(card);

            // assert
            testResult.Should().BeNull();
        }

        public static TheoryData<Func<VcardCard, object>> AbsentPropertyData()
        {
            return new TheoryData<Func<VcardCard, object>>
            {
                card => card.BirthPlace,
                card => card.DeathPlace,
                card => card.DeathDate
            };
        }

        [Theory]
        [MemberData(nameof(AbsentPropertyData))]
        public void Member_CardHasNoProperty_IsNull(Func<VcardCard, object> member)
        {
            // arrange
            var card = new VcardCard();

            // act
            var testResult = member(card);

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void ExtensionProperties_PropertiesOfAnExtension_LeavesThemOut()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("HOBBY:a", "CONTACT-URI:mailto:a@example.com", "BIRTHPLACE:b", "X-A:c"));

            // act
            var testResult = card.ExtensionProperties;

            // assert
            testResult.Select(property => property.Name).Should().Equal("X-A");
        }

        [Fact]
        public void ExtensionParameters_ParametersOfAnExtension_LeavesThemOut()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("HOBBY;LEVEL=high;INDEX=1;X-A=1:a"));

            // act
            var testResult = card.Hobbies[0].ExtensionParameters;

            // assert
            testResult.Select(parameter => parameter.Name).Should().Equal("X-A");
        }

        [Fact]
        public void AddExtensionProperty_PropertyOfAnExtension_Throws()
        {
            // arrange
            var card = new VcardCard();

            // act
            Action testAction = () => card.AddExtensionProperty(VcardPropertyNames.Hobby, "a");

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        public static TheoryData<Action<VcardCard>, string> WrittenData()
        {
            return new TheoryData<Action<VcardCard>, string>
            {
                { card => card.SetBirthPlace(new VcardTypedValue("a, b")), "BIRTHPLACE:a\\, b" },
                { card => card.SetBirthPlace(new VcardTypedValue(new Uri("geo:1,2"))), "BIRTHPLACE;VALUE=uri:geo:1\\,2" },
                { card => card.SetDeathPlace(new VcardTypedValue("a"), new VcardPropertyParameters { AltId = "1" }), "DEATHPLACE;ALTID=1:a" },
                { card => card.SetDeathDate(new VcardTypedValue(VcardDateAndOrTime.Parse("19960415".AsSpan(), VcardValueFormat.Text))), "DEATHDATE:19960415" },
                { card => card.SetDeathDate(new VcardTypedValue("circa 1800")), "DEATHDATE;VALUE=text:circa 1800" },
                { card => card.Expertise.Add("a", new VcardPropertyParameters { Level = VcardLevel.Expert, Index = 1 }), "EXPERTISE;LEVEL=expert;INDEX=1:a" },
                { card => card.Hobbies.Add("a", new VcardPropertyParameters { Level = VcardLevel.High }), "HOBBY;LEVEL=high:a" },
                { card => card.Interests.Add("a", new VcardPropertyParameters { Level = VcardLevel.Low, Index = 2 }), "INTEREST;LEVEL=low;INDEX=2:a" },
                { card => card.OrganizationDirectories.Add(new Uri("ldap://a.example/o=b,ou=c")), "ORG-DIRECTORY:ldap://a.example/o=b\\,ou=c" },
                { card => card.ContactUris.Add(new Uri("mailto:a@example.com"), new VcardPropertyParameters { Pref = 1 }), "CONTACT-URI;PREF=1:mailto:a@example.com" },
                { card => card.Addresses.Add(VcardAddress.Parse(";;;;;;".AsSpan(), VcardValueFormat.Text), new VcardPropertyParameters { CountryCode = Iso3166Part1Alpha2Country.US }), "ADR;CC=US:;;;;;;" }
            };
        }

        [Theory]
        [MemberData(nameof(WrittenData))]
        public void WriteCard_ValueAndParameters_WritesTheLine(Action<VcardCard> change, string expected)
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            change(card);

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(VcardTestData.Crlf(VcardCardTestData.Begin, VcardCardTestData.Version40, VcardCardTestData.FormattedNameLine, expected, VcardCardTestData.End));
        }

        [Fact]
        public void WriteCard_UnknownCountryCode_Throws()
        {
            // arrange
            const Iso3166Part1Alpha2Country NotACountry = (Iso3166Part1Alpha2Country)1;
            var card = VcardCardTestData.NewCard();
            card.Addresses.Add(VcardAddress.Parse(";;;;;;".AsSpan(), VcardValueFormat.Text), new VcardPropertyParameters { CountryCode = NotACountry });

            // act
            Action testAction = () => VcardCardTestData.Write(card);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Fact]
        public void WriteCard_LevelOfAnotherProperty_Throws()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            card.Hobbies.Add("a", new VcardPropertyParameters { Level = VcardLevel.Expert });

            // act
            Action testAction = () => VcardCardTestData.Write(card);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Fact]
        public void WriteCard_CountryCodeOnAnotherProperty_Throws()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            card.Notes.Add("a", new VcardPropertyParameters { CountryCode = Iso3166Part1Alpha2Country.US });

            // act
            Action testAction = () => VcardCardTestData.Write(card);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Theory]
        [InlineData(0L)]
        [InlineData(-1L)]
        public void Index_NotPositive_ThrowsArgumentOutOfRangeException(long value)
        {
            // arrange
            var parameters = new VcardPropertyParameters();

            // act
            Action testAction = () => parameters.Index = value;

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Level_NotAMember_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const VcardLevel UnknownLevel = (VcardLevel)99;
            var parameters = new VcardPropertyParameters();

            // act
            Action testAction = () => parameters.Level = UnknownLevel;

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void GetOrderedByIndex_SomeHaveAnIndex_PutsTheOthersAfterThem()
        {
            // arrange
            // ref. RFC 6715 erratum 3342: the instances in its example, as they appear.
            var card = VcardCardTestData.Read(VcardCardTestData.Card("INTEREST:art", "INTEREST;INDEX=2:baseball", "INTEREST:music", "INTEREST;INDEX=1:hockey"));

            // act
            var testResult = card.Interests.GetOrderedByIndex();

            // assert
            testResult.Select(property => property.Value).Should().Equal("hockey", "baseball", "art", "music");
        }

        [Fact]
        public void GetOrderedByIndex_ThroughTheInterface_GivesTheSameOrder()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("INTEREST;INDEX=4:music", "INTEREST;INDEX=3:art", "INTEREST;INDEX=1:hockey"));
            var version40 = card.AsVersion40();

            // act
            var testResult = version40.Interests.GetOrderedByIndex();

            // assert
            testResult.Select(property => property.Value).Should().Equal("hockey", "art", "music");
        }

        [Fact]
        public void GetOrderedByIndex_IndexNotValid_CountsAsAbsent()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("INTEREST;INDEX=0:a", "INTEREST;INDEX=1:b"), new VcardOptions { AllowInvalidValues = true });

            // act
            var testResult = card.Interests.GetOrderedByIndex();

            // assert
            testResult.Select(property => property.Value).Should().Equal("b", "a");
        }

        [Fact]
        public void ReadCard_OrgUriByDefault_IsAnUnknownProperty()
        {
            // arrange
            // ref. RFC 6715 §3.1 as printed, which its erratum 3340 corrects.
            var text = VcardCardTestData.Card("ORG-URI;PREF=1;INDEX=2:http://mycompany.example2.com");

            // act
            var testResult = VcardCardTestData.Read(text);

            // assert
            testResult.ExtensionProperties.Select(property => property.Name).Should().Equal("ORG-URI");
            testResult.OrganizationDirectories.Should().BeEmpty();
        }

        [Theory]
        [InlineData("ORG-URI;INDEX=1:http://mycompany.example1.com")]
        [InlineData("org-uri;INDEX=1:http://mycompany.example1.com")]
        public void ReadCard_OrgUriWithOption_IsAnOrganizationDirectory(string property)
        {
            // arrange
            var options = new VcardOptions { AcceptOrgUriProperty = true };

            // act
            var testResult = VcardCardTestData.Read(VcardCardTestData.Card(property), options);

            // assert
            testResult.OrganizationDirectories.Select(directory => directory.Value.OriginalString).Should().Equal("http://mycompany.example1.com");
            testResult.OrganizationDirectories[0].Index.Should().Be(1);
            testResult.ExtensionProperties.Should().BeEmpty();
        }

        [Fact]
        public void WriteCard_OrgUriReadWithOption_WritesTheRegisteredName()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("item.ORG-URI;INDEX=1:urn:a"), new VcardOptions { AcceptOrgUriProperty = true });

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(VcardCardTestData.Card("item.ORG-DIRECTORY;INDEX=1:urn:a"));
        }

        [Fact]
        public void ReadCard_OrgUriWithOption_IsJudgedAsAnOrganizationDirectory()
        {
            // arrange
            var text = VcardCardTestData.Card("ORG-URI:not a uri");
            var options = new VcardOptions { AcceptOrgUriProperty = true };

            // act
            Action testAction = () => VcardCardTestData.Read(text, options);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Fact]
        public void ReadCard_OrgUriWithOption_KeepsWhereItWasRead()
        {
            // arrange
            const long LineOfProperty = 4;
            var options = new VcardOptions { AcceptOrgUriProperty = true };

            // act
            var testResult = VcardCardTestData.Read(VcardCardTestData.Card("ORG-URI:urn:a"), options);

            // assert
            testResult.GetProperties(VcardPropertyNames.OrganizationDirectory).Single().LineNumber.Should().Be(LineOfProperty);
        }

        public static TheoryData<string, VcardValueType> ValueTypeData()
        {
            // ref. RFC 6474 §2, RFC 6715 §2 and RFC 8605 §2.1.
            return new TheoryData<string, VcardValueType>
            {
                { "BIRTHPLACE", VcardValueType.Text },
                { "DEATHPLACE", VcardValueType.Text },
                { "DEATHDATE", VcardValueType.DateAndOrTime },
                { "EXPERTISE", VcardValueType.Text },
                { "HOBBY", VcardValueType.Text },
                { "INTEREST", VcardValueType.Text },
                { "ORG-DIRECTORY", VcardValueType.Uri },
                { "CONTACT-URI", VcardValueType.Uri }
            };
        }

        [Theory]
        [MemberData(nameof(ValueTypeData))]
        public void GetDefaultValueType_PropertyOfAnExtension_IsWhatTheRfcSays(string name, VcardValueType expected)
        {
            // act
            var testResult = VcardValueTypeRegistry.GetDefaultValueType(name.AsSpan());

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("BIRTHPLACE", VcardValueType.Uri, true)]
        [InlineData("DEATHPLACE", VcardValueType.Uri, true)]
        [InlineData("DEATHDATE", VcardValueType.Text, true)]
        [InlineData("BIRTHPLACE", VcardValueType.DateAndOrTime, false)]
        [InlineData("DEATHDATE", VcardValueType.Uri, false)]
        [InlineData("HOBBY", VcardValueType.Uri, false)]
        [InlineData("ORG-DIRECTORY", VcardValueType.Text, false)]
        [InlineData("CONTACT-URI", VcardValueType.Text, false)]
        public void IsAllowedValueType_PropertyOfAnExtension_IsWhatTheRfcSays(string name, VcardValueType valueType, bool expected)
        {
            // act
            var testResult = VcardValueTypeRegistry.IsAllowedValueType(name.AsSpan(), valueType);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void IsKnownProperty_OrgUri_IsFalse()
        {
            // arrange
            const string OrgUri = "ORG-URI";

            // act
            var testResult = VcardValueTypeRegistry.IsKnownProperty(OrgUri.AsSpan());

            // assert
            testResult.Should().BeFalse();
        }
    }
}
