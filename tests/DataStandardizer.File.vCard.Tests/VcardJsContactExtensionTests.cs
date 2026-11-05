using System;
using System.Linq;
using DataStandardizer.LanguageTag;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the extensions of vCard 4.0 in RFC 9554.
    /// </summary>
    public class VcardJsContactExtensionTests
    {
        private const string MainStreetAddress = ";;123 Main Street;Any Town;CA;91921-1234;U.S.A";

        // ref. RFC 9554 §2.1: the example, with the fold of the RFC, whose layout has more white space than a fold has.
        private const string Rfc9554AddressExample = "ADR;GEO=\"geo:12.3457,78.910\":\r\n ;;123 Main Street;Any Town;CA;91921-1234;U.S.A\r\n ;;;;123;Main Street;;;;;;";

        // ref. RFC 9554 §2.2: the second example.
        private const string Rfc9554NameExample = "N:Stevenson;John;Philip,Paul;Dr.;Jr.,M.D.,A.C.P.;;Jr.";

        // ref. RFC 9554 §4.6: the example, in which U+5B6B, U+4E2D, U+5C71, U+6587, U+9038 and U+4ED9 are the characters of the name.
        private const string Rfc9554PhoneticRelated = "N;ALTID=1;LANGUAGE=zh-Hant:\x5B6B;\x4E2D\x5C71;\x6587,\x9038\x4ED9;;;;";
        private const string Rfc9554PhoneticProperty = "N;ALTID=1;PHONETIC=jyut;\r\n SCRIPT=Latn;LANGUAGE=yue:syun1;zung1saan1;man4,jat6sin1;;;;";

        private const string ExampleTimestamp = "20220705T093412Z";
        private const string ExampleProfile = "https://example.com/@foo";

        public static TheoryData<string[]> RfcExampleData()
        {
            return new TheoryData<string[]>
            {
                // ref. RFC 9554 §2.1.
                new[] { Rfc9554AddressExample },

                // ref. RFC 9554 §2.2.
                new[] { "N:Public;John;Quinlan;Mr.;Esq." },
                new[] { Rfc9554NameExample },

                // ref. RFC 9554 §3.1.
                new[] { "CREATED:20220705T093412Z" },
                new[] { "CREATED;VALUE=TIMESTAMP:20211022T140000-05" },

                // ref. RFC 9554 §3.2.
                new[] { "GRAMGENDER;LANGUAGE=de:feminine" },

                // ref. RFC 9554 §3.3.
                new[] { "LANGUAGE:de-AT" },

                // ref. RFC 9554 §3.4.
                new[] { "PRONOUNS;LANGUAGE=en;PREF=1:xe/xir", "PRONOUNS;LANGUAGE=en;PREF=2:they/them" },

                // ref. RFC 9554 §3.5, and §4.9 before and after its erratum 8652.
                new[] { "SOCIALPROFILE;SERVICE-TYPE=Mastodon:https://example.com/@foo" },
                new[] { "SOCIALPROFILE;SERVICE-TYPE=\"Mastodon\":https://example.com/@foo" },
                new[] { "SOCIALPROFILE:https://example.com/ietf" },
                new[] { "SOCIALPROFILE;SERVICE-TYPE=SomeSite;VALUE=text:peter94" },

                // ref. RFC 9554 §4.1, §4.2 and §4.3.
                new[] { "NOTE;AUTHOR=\"mailto:john@example.com\":This is some note." },
                new[] { "NOTE;AUTHOR-NAME=John Doe:This is some note." },
                new[] { "NOTE;AUTHOR-NAME=\"_:l33tHckr:_\":A note by an unusual author name." },
                new[] { "NOTE;CREATED=20221122T151823Z:This is some note." },

                // ref. RFC 9554 §4.4.
                new[] { "N:;John;Quinlan;Mr.;" },

                // ref. RFC 9554 §4.5.
                new[] { "ADR;LABEL=\"Mr. John Q. Public, Esq.\\nMail Drop: TNE QB\\n123\r\n  Main Street\\nAny Town, CA  91921-1234\\nU.S.A.\":\r\n ;;123 Main Street;Any Town;CA;91921-1234;U.S.A." },

                // ref. RFC 9554 §4.6.
                new[] { Rfc9554PhoneticRelated, Rfc9554PhoneticProperty },

                // ref. RFC 9554 §4.7, with the comma escaped as RFC 6350 §3.4 and its errata 3845 and 3846 say, and without the rest of the data.
                new[] { "PHOTO;PROP-ID=p827:data:image/jpeg;base64\\,MIICajCCAdOgAwIBAg" },

                // ref. RFC 9554 §4.8, which gives only the parameter.
                new[] { "NOTE;SCRIPT=Latn:a" },

                // ref. RFC 9554 §4.10.
                new[] { "SOCIALPROFILE;USERNAME=\"The Foo\":https://example.com/@foo" },

                // ref. RFC 9554 §5.1 and §5.2.
                new[] { "ADR;TYPE=billing:;;123 Main Street;Any Town;CA;91921-1234;U.S.A." },
                new[] { "ADR;TYPE=delivery:;;123 Main Street;Any Town;CA;91921-1234;U.S.A." }
            };
        }

        [Theory]
        [MemberData(nameof(RfcExampleData))]
        public void Validate_RfcExample_FindsNoProblem(string[] properties)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(properties));

            // act
            var testResult = card.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void Validate_Rfc9554DerivedExample_FindsNoProblem()
        {
            // arrange
            // ref. RFC 9554 §4.4: the example has its own FN.
            var card = VcardCardTestData.Read(VcardCardTestData.BareCard("N:;John;Quinlan;Mr.;", "FN;DERIVED=TRUE:Mr. John Quinlan"));

            // act
            var testResult = card.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        public static TheoryData<string, VcardDiagnosticKind> BrokenRuleData()
        {
            return new TheoryData<string, VcardDiagnosticKind>
            {
                // ref. RFC 9554 §3.1: a single timestamp value.
                { "CREATED:not-a-timestamp", VcardDiagnosticKind.InvalidValue },
                { "CREATED:20220705", VcardDiagnosticKind.InvalidValue },
                { "CREATED;VALUE=text:a", VcardDiagnosticKind.InvalidValue },

                // ref. RFC 9554 §3.2: gramgender-value is a token, and gramgender-param has language-param and any-param.
                { "GRAMGENDER:a b", VcardDiagnosticKind.InvalidValue },
                { "GRAMGENDER;PREF=1:common", VcardDiagnosticKind.InvalidParameter },
                { "GRAMGENDER;VALUE=text:common", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §3.3: "The LANGUAGE parameter MUST NOT be assigned to this property", and the ABNF has no VALUE.
                { "LANGUAGE:not a tag", VcardDiagnosticKind.InvalidValue },
                { "LANGUAGE;LANGUAGE=en:de-AT", VcardDiagnosticKind.InvalidParameter },
                { "LANGUAGE;VALUE=language-tag:de-AT", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §3.4: pronouns-param, and a text value.
                { "PRONOUNS;PID=1:a", VcardDiagnosticKind.InvalidParameter },
                { "PRONOUNS:a,b", VcardDiagnosticKind.InvalidValue },

                // ref. RFC 9554 §3.5: "The SERVICE-TYPE parameter MUST be assigned to this property if the value type is TEXT", and "it MUST NOT be assigned more than once".
                { "SOCIALPROFILE;VALUE=text:a", VcardDiagnosticKind.InvalidParameter },
                { "SOCIALPROFILE;VALUE=text;SERVICE-TYPE=\"\":a", VcardDiagnosticKind.InvalidParameter },
                { "SOCIALPROFILE;SERVICE-TYPE=a;SERVICE-TYPE=b:urn:c", VcardDiagnosticKind.InvalidParameter },
                { "SOCIALPROFILE:not a uri", VcardDiagnosticKind.InvalidValue },
                { "SOCIALPROFILE;VALUE=date:19960415", VcardDiagnosticKind.InvalidValue },
                { "SOCIALPROFILE;PREF=1:urn:a", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §4.1: "It identifies the author as a URI".
                { "NOTE;AUTHOR=a:b", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;AUTHOR=\"not a uri\":b", VcardDiagnosticKind.InvalidParameter },
                { "X-A;AUTHOR=a:b", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §4.2: "The parameter value MUST NOT be empty."
                { "NOTE;AUTHOR-NAME=\"\":a", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;AUTHOR-NAME=a;AUTHOR-NAME=b:c", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §4.3: "The value MUST be a valid TIMESTAMP value".
                { "NOTE;CREATED=20221122:a", VcardDiagnosticKind.InvalidParameter },
                { "X-A;CREATED=a:b", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §4.4: derived-param = "DERIVED" "=" ("true" / "false").
                { "NOTE;DERIVED=maybe:a", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;DERIVED=true,false:a", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §4.5: "for use with the ADR property".
                { "NOTE;LABEL=a:b", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §4.6: "This specification defines the PHONETIC parameter for use with the ADR and N properties", and the value "MUST be one of the values enumerated" in the registry.
                { "NOTE;PHONETIC=ipa:a", VcardDiagnosticKind.InvalidParameter },
                { "X-A;PHONETIC=klingon:a", VcardDiagnosticKind.InvalidParameter },
                { "X-A;PHONETIC=x-a:b", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §4.7: prop-id-param = "PROP-ID" "=" 1*255(ALPHA / DIGIT / "-"/ "_").
                { "NOTE;PROP-ID=a.b:c", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;PROP-ID=\"\":a", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;PROP-ID=" + new string('a', 256) + ":b", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;PROP-ID=a,b:c", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §4.8: script-param = 4ALPHA.
                { "NOTE;SCRIPT=Lat:a", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;SCRIPT=Latin:a", VcardDiagnosticKind.InvalidParameter },
                { "NOTE;SCRIPT=Lat1:a", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §4.9 and §4.10: "on an IMPP or a SOCIALPROFILE property".
                { "NOTE;SERVICE-TYPE=a:b", VcardDiagnosticKind.InvalidParameter },
                { "TEL;USERNAME=a:1", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §4.10: "The IMPP or SOCIALPROFILE value type MUST be URI."
                { "SOCIALPROFILE;VALUE=text;SERVICE-TYPE=a;USERNAME=b:c", VcardDiagnosticKind.InvalidParameter },

                // ref. RFC 9554 §5: "This value can be used with the TYPE parameter applied on the ADR property."
                { "NOTE;TYPE=billing:a", VcardDiagnosticKind.InvalidParameter },
                { "TEL;TYPE=delivery:1", VcardDiagnosticKind.InvalidParameter }
            };
        }

        [Theory]
        [MemberData(nameof(BrokenRuleData))]
        public void Validate_RuleBrokenWithTheOptIn_ReportsIt(string property, VcardDiagnosticKind expected)
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

        public static TheoryData<string[]> BrokenCardRuleData()
        {
            return new TheoryData<string[]>
            {
                // ref. RFC 9554 §3.2: "Multiple occurrences of this property MUST be distinguished by the LANGUAGE parameter."
                new[] { "GRAMGENDER:common", "GRAMGENDER:neuter" },
                new[] { "GRAMGENDER;LANGUAGE=de:common", "GRAMGENDER;LANGUAGE=DE:neuter" },

                // ref. RFC 9554 §4.6: "The ALTID parameter MUST be set with equal values on both the related property and the property having the PHONETIC parameter set."
                new[] { "ADR;PHONETIC=ipa:;;a;;;;" },
                new[] { "ADR;ALTID=1;PHONETIC=ipa:;;a;;;;" },
                new[] { "ADR;ALTID=1:;;a;;;;", "ADR;ALTID=2;PHONETIC=ipa:;;b;;;;" },
                new[] { "N;PHONETIC=ipa:a;;;;" },

                // ref. RFC 9554 §4.6: "The SCRIPT parameter MUST be set in addition to the PHONETIC parameter."
                new[] { "ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=script:;;b;;;;" },

                // ref. RFC 9554 §4.6: "a component value also MUST be set at that same position in the related property."
                new[] { "ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=ipa:;;b;c;;;" },
                new[] { "N;ALTID=1:a;;;;", "N;ALTID=1;PHONETIC=ipa:b;c;;;" },

                // ref. RFC 9554 §4.6: "at most, one of these properties MAY not have the LANGUAGE parameter set", and "The LANGUAGE parameters MUST NOT have equal values."
                new[] { "ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=ipa:;;b;;;;", "ADR;ALTID=1;PHONETIC=piny:;;c;;;;" },
                new[] { "ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=ipa;LANGUAGE=yue:;;b;;;;", "ADR;ALTID=1;PHONETIC=piny;LANGUAGE=YUE:;;c;;;;" }
            };
        }

        [Theory]
        [MemberData(nameof(BrokenCardRuleData))]
        public void Validate_RuleOfSeveralPropertiesBroken_ReportsAnInvalidParameter(string[] properties)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(properties), new VcardOptions { AllowInvalidValues = true });

            // act
            var testResult = card.Validate();

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(VcardDiagnosticKind.InvalidParameter);
        }

        [Theory]
        [MemberData(nameof(BrokenCardRuleData))]
        public void ReadCard_RuleOfSeveralPropertiesBrokenWithDefaultOptions_Throws(string[] properties)
        {
            // arrange
            var text = VcardCardTestData.Card(properties);

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        public static TheoryData<string[]> ConformingPropertiesData()
        {
            return new TheoryData<string[]>
            {
                // ref. RFC 9554 §3.2: the values, and a token its ABNF allows, each with a LANGUAGE of its own or none.
                new[] { "GRAMGENDER:animate" },
                new[] { "GRAMGENDER:x-a" },
                new[] { "GRAMGENDER:common", "GRAMGENDER;LANGUAGE=de:neuter", "GRAMGENDER;LANGUAGE=fr:feminine" },

                // ref. RFC 9554 §3.1 and §3.3: any-param.
                new[] { "CREATED;VALUE=timestamp;X-A=1:20220705T093412Z" },
                new[] { "LANGUAGE;X-A=1:de-AT" },

                // ref. RFC 9554 §3.4: each of its parameters.
                new[] { "PRONOUNS;LANGUAGE=en;PREF=1;TYPE=work;ALTID=a;X-A=1:they/them" },

                // ref. RFC 9554 §3.5, §4.9 and §4.10: on IMPP and SOCIALPROFILE, and with a URI.
                new[] { "SOCIALPROFILE;VALUE=uri;SERVICE-TYPE=a;USERNAME=b;X-A=1:urn:c" },
                new[] { "IMPP;SERVICE-TYPE=a;USERNAME=b:xmpp:c@example.com" },

                // ref. RFC 9554 §4.1 to §4.4 and §4.7: on any property, including one whose ABNF has no any-param.
                new[] { "CONTACT-URI;AUTHOR=\"mailto:a@example.com\";AUTHOR-NAME=b;CREATED=20221122T151823Z;DERIVED=false;PROP-ID=c:https://example.com/" },
                new[] { "X-A;AUTHOR=\"mailto:a@example.com\":b" },

                // ref. RFC 5234 §2.3 and RFC 6350 §5: these aren't quoted, so they are case-insensitive.
                new[] { "NOTE;DERIVED=TRUE:a" },
                new[] { "ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=IPA:;;b;;;;" },
                new[] { "ADR;TYPE=BILLING:;;;;;;" },

                // ref. RFC 9554 §4.6: every system; several with one lacking a LANGUAGE; and a related property with more components than its phonetic one.
                new[] { "ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=ipa:;;b;;;;" },
                new[] { "ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=jyut:;;b;;;;" },
                new[] { "ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=piny:;;b;;;;" },
                new[] { "ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=script;SCRIPT=Latn:;;b;;;;" },
                new[] { "ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=ipa:;;b;;;;", "ADR;ALTID=1;PHONETIC=piny;LANGUAGE=zh:;;c;;;;" },
                new[] { "ADR;ALTID=1:;;a;b;;;", "ADR;ALTID=1;PHONETIC=ipa:;;c;;;;" },
                new[] { "ADR;ALTID=1;PHONETIC=ipa:;;c;;;;", "ADR;ALTID=1:;;a;;;;" },
                new[] { "ADR;ALTID=1;LANGUAGE=en:;;a;;;;", "ADR;ALTID=1;LANGUAGE=fr:;;a;b;;;", "ADR;ALTID=1;PHONETIC=ipa:;;c;d;;;" },

                // ref. RFC 9554 §4.6, on the components of §2.1 and §2.2.
                new[] { "N;ALTID=1:a;;;;;b;", "N;ALTID=1;PHONETIC=ipa:c;;;;;d;" },
                new[] { "ADR;ALTID=1:;;a;;;;;;;;;b;;;;;;", "ADR;ALTID=1;PHONETIC=ipa:;;c;;;;;;;;;d;;;;;;" },

                // ref. RFC 6350 §3.3: any-param lets an unknown property have these.
                new[] { "X-A;PHONETIC=ipa;SCRIPT=Latn:a" },
                new[] { "X-A;TYPE=billing:a" },
                new[] { "X-A;SERVICE-TYPE=a;USERNAME=b:c" },

                // ref. RFC 9554 §4.7: the longest, and duplicates, whose handling "is specific to the application".
                new[] { "NOTE;PROP-ID=" + new string('a', 255) + ":b" },
                new[] { "NOTE;PROP-ID=A-z_0:a" },
                new[] { "NOTE;PROP-ID=a:b", "NOTE;PROP-ID=a:c" },
                new[] { "NOTE;PROP-ID=a:b", "TITLE;PROP-ID=a:c" }
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

        public static TheoryData<string, Func<VcardCard, object>, object> TypedMemberData()
        {
            return new TheoryData<string, Func<VcardCard, object>, object>
            {
                { "CREATED:20220705T093412Z", card => card.Created.Value.ToString(VcardValueFormat.Text), ExampleTimestamp },
                { "LANGUAGE:de-AT", card => card.DefaultLanguage.Value.ToString(), "de-AT" },
                { "GRAMGENDER;LANGUAGE=de:feminine", card => card.GrammaticalGenders[0].Value, "feminine" },
                { "GRAMGENDER;LANGUAGE=de:feminine", card => card.GrammaticalGenders[0].Language.Value.ToString(), "de" },
                { "PRONOUNS;LANGUAGE=en;PREF=1:xe/xir", card => card.Pronouns[0].Value, "xe/xir" },
                { "SOCIALPROFILE:https://example.com/@foo", card => card.SocialProfiles[0].Value.Uri.OriginalString, ExampleProfile },
                { "SOCIALPROFILE;SERVICE-TYPE=SomeSite;VALUE=text:peter94", card => card.SocialProfiles[0].Value.Text, "peter94" },
                { "SOCIALPROFILE;SERVICE-TYPE=SomeSite;VALUE=text:peter94", card => card.SocialProfiles[0].ServiceType, "SomeSite" },
                { "SOCIALPROFILE;USERNAME=\"The Foo\":https://example.com/@foo", card => card.SocialProfiles[0].Username, "The Foo" },
                { "IMPP;SERVICE-TYPE=\"XMPP\":xmpp:a@example.com", card => card.InstantMessaging[0].ServiceType, "XMPP" },
                { "NOTE;AUTHOR=\"mailto:john@example.com\":a", card => card.Notes[0].Author.OriginalString, "mailto:john@example.com" },
                { "NOTE;AUTHOR-NAME=John Doe:a", card => card.Notes[0].AuthorName, "John Doe" },
                { "NOTE;CREATED=20221122T151823Z:a", card => card.Notes[0].Created.Value.ToString(VcardValueFormat.Text), "20221122T151823Z" },
                { "NOTE;DERIVED=TRUE:a", card => card.Notes[0].IsDerived, true },
                { "NOTE;DERIVED=false:a", card => card.Notes[0].IsDerived, false },
                { "NOTE:a", card => card.Notes[0].IsDerived, false },
                { "NOTE;PROP-ID=p827:a", card => card.Notes[0].PropertyId, "p827" },
                { "NOTE;SCRIPT=Latn:a", card => card.Notes[0].Script, "Latn" },
                { "ADR;LABEL=a:;;;;;;", card => card.Addresses[0].Label, "a" },
                { "ADR;TYPE=billing:;;;;;;", card => card.Addresses[0].Types.Single(), "billing" }
            };
        }

        [Theory]
        [MemberData(nameof(TypedMemberData))]
        public void Member_OfAnExtension_GivesItsValue(string property, Func<VcardCard, object> member, object expected)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property));

            // act
            var testResult = member(card);

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<string, Func<VcardCard, object>> InvalidTypedMemberData()
        {
            return new TheoryData<string, Func<VcardCard, object>>
            {
                { "NOTE;AUTHOR=a:b", card => card.Notes[0].Author },
                { "NOTE;CREATED=a:b", card => card.Notes[0].Created },
                { "NOTE;PROP-ID=a.b:c", card => card.Notes[0].PropertyId },
                { "NOTE;SCRIPT=a:b", card => card.Notes[0].Script },
                { "NOTE;PHONETIC=ipa:a", card => card.Notes[0].Phonetic },
                { "NOTE;SERVICE-TYPE=a:b", card => card.Notes[0].ServiceType },
                { "NOTE;USERNAME=a:b", card => card.Notes[0].Username },
                { "ADR;ALTID=1;PHONETIC=klingon:;;;;;;", card => card.Addresses[0].Phonetic },
                { "SOCIALPROFILE;VALUE=text;SERVICE-TYPE=a;USERNAME=b:c", card => card.SocialProfiles[0].Username },
                { "CREATED:a", card => card.Created },
                { "LANGUAGE:not a tag", card => card.DefaultLanguage }
            };
        }

        [Theory]
        [MemberData(nameof(InvalidTypedMemberData))]
        public void Member_OfAnExtensionThatIsNotValid_IsNull(string property, Func<VcardCard, object> member)
        {
            // arrange
            var options = new VcardOptions { AllowInvalidValues = true };
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property), options);

            // act
            var testResult = member(card);

            // assert
            testResult.Should().BeNull();
        }

        public static TheoryData<string, VcardPhoneticSystem> PhoneticData()
        {
            return new TheoryData<string, VcardPhoneticSystem>
            {
                { "ADR;PHONETIC=ipa:;;;;;;", VcardPhoneticSystem.Ipa },
                { "ADR;PHONETIC=jyut:;;;;;;", VcardPhoneticSystem.Jyutping },
                { "ADR;PHONETIC=piny:;;;;;;", VcardPhoneticSystem.Pinyin },
                { "ADR;PHONETIC=script:;;;;;;", VcardPhoneticSystem.Script },
                { "ADR;PHONETIC=JYUT:;;;;;;", VcardPhoneticSystem.Jyutping }
            };
        }

        [Theory]
        [MemberData(nameof(PhoneticData))]
        public void Phonetic_EachSystem_GivesItsMember(string property, VcardPhoneticSystem expected)
        {
            // arrange
            var options = new VcardOptions { AllowInvalidValues = true };
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property), options);

            // act
            var testResult = card.Addresses[0].Phonetic;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Script_WithPhonetic_GivesTheParameter()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=script;SCRIPT=Latn:;;b;;;;"));

            // act
            var testResult = card.Addresses[1];

            // assert
            testResult.Phonetic.Should().Be(VcardPhoneticSystem.Script);
            testResult.Script.Should().Be("Latn");
        }

        [Fact]
        public void Name_PhoneticExampleInEitherOrder_GivesTheRelatedProperty()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(Rfc9554PhoneticProperty, Rfc9554PhoneticRelated));

            // act
            var testResult = card.Name;

            // assert
            testResult.Phonetic.Should().BeNull();
            testResult.Value.FamilyNames.Should().Equal("\x5B6B");
        }

        [Fact]
        public void Name_OnlyAPhoneticProperty_GivesIt()
        {
            // arrange
            var options = new VcardOptions { AllowInvalidValues = true };
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N;ALTID=1;PHONETIC=ipa:a;;;;"), options);

            // act
            var testResult = card.Name;

            // assert
            testResult.Phonetic.Should().Be(VcardPhoneticSystem.Ipa);
        }

        [Fact]
        public void GetAlternatives_PhoneticExample_GroupsThemByAltId()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(Rfc9554PhoneticRelated, Rfc9554PhoneticProperty));

            // act
            var testResult = card.GetAlternatives(VcardPropertyNames.Name);

            // assert
            testResult.Single().Count.Should().Be(2);
        }

        [Fact]
        public void GetDuplicatePropertyIds_OfSeveralKinds_GivesOnlySiblingsWithTheSameId()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(
                "NOTE;PROP-ID=a:first", "TITLE;PROP-ID=a:other name", "NOTE;PROP-ID=b:other id", "NOTE:no id", "note;PROP-ID=A:second"));

            // act
            var testResult = card.GetDuplicatePropertyIds();

            // assert
            VcardCardTestData.ValuesOf(testResult).Should().BeEquivalentTo(new[] { new[] { "first", "second" } }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void GetDuplicatePropertyIds_InvalidValues_LeavesThemOut()
        {
            // arrange
            var options = new VcardOptions { AllowInvalidValues = true };
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE;PROP-ID=a.b:c", "NOTE;PROP-ID=a.b:d"), options);

            // act
            var testResult = card.GetDuplicatePropertyIds();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void GrammaticalGenders_ValueThatIsNotAToken_LeavesItOut()
        {
            // arrange
            var options = new VcardOptions { AllowInvalidValues = true };
            var card = VcardCardTestData.Read(VcardCardTestData.Card("GRAMGENDER:a b", "GRAMGENDER;LANGUAGE=de:common"), options);

            // act
            var testResult = card.GrammaticalGenders.Single();

            // assert
            testResult.Value.Should().Be("common");
        }

        [Fact]
        public void Birthday_FirstWithAPhoneticItDoesNotAllow_GivesTheFirst()
        {
            // arrange
            var options = new VcardOptions { AllowInvalidValues = true, AllowCardinalityViolations = true };
            var card = VcardCardTestData.Read(VcardCardTestData.Card("BDAY;PHONETIC=ipa:19960415", "BDAY:19970101"), options);

            // act
            var testResult = card.Birthday;

            // assert
            testResult.Line.Value.Should().Be("19960415");
        }

        [Fact]
        public void WriteCard_PhoneticAddressAddedToAReadAddressWithNoStreet_Writes()
        {
            // arrange
            // ref. RFC 9554 §4.6: the phonetic line can't have a street value that the related property doesn't.
            var card = VcardCardTestData.Read(VcardCardTestData.Card("ADR;ALTID=1:;;;;;;;;;;1;a;;;;;;"));
            card.Addresses.Add(VcardAddress.Parse(";;;;;;;;;;1;b;;;;;;".AsSpan(), VcardValueFormat.Text), new VcardPropertyParameters { AltId = "1", Phonetic = VcardPhoneticSystem.Ipa });

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(VcardCardTestData.Card("ADR;ALTID=1:;;;;;;;;;;1;a;;;;;;", "ADR;ALTID=1;PHONETIC=ipa:;;;;;;;;;;1;b;;;;;;"));
        }

        [Fact]
        public void Add_PhoneticAddress_GivesTheFormItWrites()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            var address = VcardAddress.Parse(";;;;;;;;;;1;b;;;;;;".AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = card.Addresses.Add(address, new VcardPropertyParameters { AltId = "1", Phonetic = VcardPhoneticSystem.Ipa });

            // assert
            testResult.Value.StreetAddress.Should().BeEmpty();
        }

        [Fact]
        public void GetDuplicatePropertyIds_NoDuplicates_GivesNothing()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE;PROP-ID=a:b", "NOTE;PROP-ID=c:d", "TITLE;PROP-ID=a:e"));

            // act
            var testResult = card.GetDuplicatePropertyIds();

            // assert
            testResult.Should().BeEmpty();
        }

        public static TheoryData<string[], string> RoundTripData()
        {
            return new TheoryData<string[], string>
            {
                // ref. RFC 9554 §2.1 and §2.2: the new components stay in their places.
                { new[] { "ADR:;;123 Main Street;Any Town;CA;91921-1234;U.S.A;;;;123;Main Street;;;;;;" }, "ADR:;;123 Main Street;Any Town;CA;91921-1234;U.S.A;;;;123;Main Street;;;;;;" },
                { new[] { Rfc9554NameExample }, Rfc9554NameExample },

                // ref. RFC 9555 §3.2 and §3.3: JSPROP, JSPTR and JSCOMPS, which this package doesn't recognise, are written back unchanged.
                { new[] { "JSPROP;JSPTR=\"example.com:foo\":\"bar\"" }, "JSPROP;JSPTR=\"example.com:foo\":\"bar\"" },
                { new[] { "N;JSCOMPS=\";1;0\":Doe;Jane;;;" }, "N;JSCOMPS=\";1;0\":Doe;Jane;;;" },
                { new[] { "ADR;JSCOMPS=\";2;3\":;;a;b;;;" }, "ADR;JSCOMPS=\";2;3\":;;a;b;;;" },

                // ref. RFC 9554 §4.9 and §4.10, and its erratum 8652: the case of these is significant, and RFC 6350 §5 makes it so only in quotes.
                { new[] { "SOCIALPROFILE;SERVICE-TYPE=Mastodon:https://example.com/@foo" }, "SOCIALPROFILE;SERVICE-TYPE=\"Mastodon\":https://example.com/@foo" },
                { new[] { "IMPP;USERNAME=TheFoo:xmpp:a@example.com" }, "IMPP;USERNAME=\"TheFoo\":xmpp:a@example.com" },

                // Parameters that aren't quoted unless they have to be.
                { new[] { "NOTE;PROP-ID=p827;DERIVED=true;SCRIPT=Latn:a" }, "NOTE;PROP-ID=p827;DERIVED=true;SCRIPT=Latn:a" },
                { new[] { "NOTE;AUTHOR=\"mailto:john@example.com\":a" }, "NOTE;AUTHOR=\"mailto:john@example.com\":a" }
            };
        }

        [Theory]
        [MemberData(nameof(RoundTripData))]
        public void WriteCard_CardRead_WritesItsPropertiesBack(string[] properties, string expected)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(properties));

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(VcardCardTestData.Card(expected));
        }

        [Fact]
        public void ExtensionProperties_Rfc9555Property_HoldsItWithItsParameter()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("JSPROP;JSPTR=a:b"));

            // act
            var testResult = card.ExtensionProperties.Single();

            // assert
            testResult.Name.Should().Be("JSPROP");
            testResult.ExtensionParameters.Single().Name.Should().Be("JSPTR");
        }

        [Fact]
        public void ExtensionParameters_ParametersOfRfc9554_LeavesThemOut()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(
                "NOTE;AUTHOR=\"urn:a\";AUTHOR-NAME=b;CREATED=20221122T151823Z;DERIVED=true;PROP-ID=c;SCRIPT=Latn;JSPTR=d:e"));

            // act
            var testResult = card.Notes[0].ExtensionParameters;

            // assert
            testResult.Single().Name.Should().Be("JSPTR");
        }

        public static TheoryData<string> Rfc9554PropertyNameData()
        {
            return new TheoryData<string>
            {
                VcardPropertyNames.Created,
                VcardPropertyNames.GrammaticalGender,
                VcardPropertyNames.DefaultLanguage,
                VcardPropertyNames.Pronouns,
                VcardPropertyNames.SocialProfile
            };
        }

        [Theory]
        [MemberData(nameof(Rfc9554PropertyNameData))]
        public void IsDefinedIn_PropertyOfRfc9554_IsOnlyInVersion40(string name)
        {
            // arrange
            var info = VcardPropertyInfo.Find(name);

            // act
            var testResult = new[] { info.IsDefinedIn(VcardVersion.V4_0), info.IsDefinedIn(VcardVersion.V3_0), info.IsDefinedIn(VcardVersion.V2_1) };

            // assert
            testResult.Should().Equal(true, false, false);
        }

        [Theory]
        [InlineData(VcardPropertyNames.Created, VcardCardinality.AtMostOne)]
        [InlineData(VcardPropertyNames.GrammaticalGender, VcardCardinality.Any)]
        [InlineData(VcardPropertyNames.DefaultLanguage, VcardCardinality.AtMostOne)]
        [InlineData(VcardPropertyNames.Pronouns, VcardCardinality.Any)]
        [InlineData(VcardPropertyNames.SocialProfile, VcardCardinality.Any)]
        public void Cardinality_PropertyOfRfc9554_IsWhatTheRfcSays(string name, VcardCardinality expected)
        {
            // arrange
            var info = VcardPropertyInfo.Find(name);

            // act
            var testResult = info.Cardinality;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("CREATED:20220705T093412Z", "CREATED:20220705T093412Z")]
        [InlineData("LANGUAGE:de-AT", "LANGUAGE:de-AT")]
        public void ReadCard_SecondInstanceOfAPropertyWithOne_Throws(string first, string second)
        {
            // arrange
            var text = VcardCardTestData.Card(first, second);

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        public static TheoryData<Action<VcardCard>, string> AddData()
        {
            var timestamp = VcardTimestamp.Parse(ExampleTimestamp.AsSpan(), VcardValueFormat.Text);
            var german = Bcp47LanguageTag.Create("de");
            return new TheoryData<Action<VcardCard>, string>
            {
                { card => card.SetCreated(timestamp), "CREATED:20220705T093412Z" },
                { card => card.SetDefaultLanguage(german), "LANGUAGE:de" },
                { card => card.GrammaticalGenders.Add("feminine", new VcardPropertyParameters { Language = german }), "GRAMGENDER;LANGUAGE=de:feminine" },
                { card => card.Pronouns.Add("they/them", new VcardPropertyParameters { Pref = 1 }), "PRONOUNS;PREF=1:they/them" },
                { card => card.SocialProfiles.Add(new VcardTypedValue(new Uri(ExampleProfile))), "SOCIALPROFILE:https://example.com/@foo" },
                { card => card.SocialProfiles.Add(new VcardTypedValue("peter94"), new VcardPropertyParameters { ServiceType = "SomeSite" }), "SOCIALPROFILE;VALUE=text;SERVICE-TYPE=\"SomeSite\":peter94" },
                { card => card.SocialProfiles.Add(new VcardTypedValue(new Uri(ExampleProfile)), new VcardPropertyParameters { Username = "The Foo" }), "SOCIALPROFILE;USERNAME=\"The Foo\":https://example.com/@foo" },
                { card => card.Notes.Add("a", new VcardPropertyParameters { Author = new Uri("mailto:john@example.com") }), "NOTE;AUTHOR=\"mailto:john@example.com\":a" },
                { card => card.Notes.Add("a", new VcardPropertyParameters { AuthorName = "John Doe" }), "NOTE;AUTHOR-NAME=John Doe:a" },
                { card => card.Notes.Add("a", new VcardPropertyParameters { Created = timestamp }), "NOTE;CREATED=20220705T093412Z:a" },
                { card => card.Notes.Add("a", new VcardPropertyParameters { Derived = true }), "NOTE;DERIVED=true:a" },
                { card => card.Notes.Add("a", new VcardPropertyParameters { Derived = false }), "NOTE;DERIVED=false:a" },
                { card => card.Notes.Add("a", new VcardPropertyParameters { PropertyId = "p827" }), "NOTE;PROP-ID=p827:a" },
                { card => card.Notes.Add("a", new VcardPropertyParameters { Script = "Latn" }), "NOTE;SCRIPT=Latn:a" },
                { card => card.Addresses.Add(VcardAddress.Parse(";;a;;;;".AsSpan(), VcardValueFormat.Text), new VcardPropertyParameters { Types = { "billing" } }), "ADR;TYPE=billing:;;a;;;;" }
            };
        }

        [Theory]
        [MemberData(nameof(AddData))]
        public void WriteCard_AddedThroughTheModel_WritesTheLine(Action<VcardCard> change, string expected)
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            change(card);

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(VcardCardTestData.Card(expected));
        }

        public static TheoryData<VcardPhoneticSystem, string> PhoneticAddData()
        {
            return new TheoryData<VcardPhoneticSystem, string>
            {
                { VcardPhoneticSystem.Ipa, "ADR;ALTID=1;PHONETIC=ipa:;;b;;;;" },
                { VcardPhoneticSystem.Jyutping, "ADR;ALTID=1;PHONETIC=jyut:;;b;;;;" },
                { VcardPhoneticSystem.Pinyin, "ADR;ALTID=1;PHONETIC=piny:;;b;;;;" }
            };
        }

        [Theory]
        [MemberData(nameof(PhoneticAddData))]
        public void WriteCard_PhoneticAddedThroughTheModel_WritesTheLine(VcardPhoneticSystem system, string expected)
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            card.Addresses.Add(VcardAddress.Parse(";;a;;;;".AsSpan(), VcardValueFormat.Text), new VcardPropertyParameters { AltId = "1" });
            card.Addresses.Add(VcardAddress.Parse(";;b;;;;".AsSpan(), VcardValueFormat.Text), new VcardPropertyParameters { AltId = "1", Phonetic = system });

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(VcardCardTestData.Card("ADR;ALTID=1:;;a;;;;", expected));
        }

        [Fact]
        public void Phonetic_ValueThatIsNotAMember_Throws()
        {
            // arrange
            var parameters = new VcardPropertyParameters();

            // act
            Action testAction = () => parameters.Phonetic = (VcardPhoneticSystem)4;

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void WriteCard_SocialProfileAsTextWithoutAServiceType_Throws()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            card.SocialProfiles.Add(new VcardTypedValue("peter94"));

            // act
            Action testAction = () => VcardCardTestData.Write(card);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        public static TheoryData<string, Action<VcardCard>> DerivedSetterData()
        {
            return new TheoryData<string, Action<VcardCard>>
            {
                { "N;DERIVED=true:a;;;;", card => card.SetName(new VcardName(new[] { "b" }, new string[0], new string[0], new string[0], new string[0])) },
                { "PRODID;DERIVED=TRUE:a", card => card.SetProductId("b") },
                { "KIND;DERIVED=true:individual", card => card.SetKind(VcardKind.Group) },
                { "CREATED;DERIVED=true:20220705T093412Z", card => card.SetCreated(VcardTimestamp.Parse(ExampleTimestamp.AsSpan(), VcardValueFormat.Text)) }
            };
        }

        [Theory]
        [MemberData(nameof(DerivedSetterData))]
        public void Setter_PropertyWithDerivedTrue_Throws(string property, Action<VcardCard> change)
        {
            // arrange
            // ref. RFC 9554 §4.4: 'When present with a value of "true", clients MUST NOT update the property.'
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property));

            // act
            Action testAction = () => change(card);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Setter_PropertyWithDerivedTrue_LeavesTheCardAsItWas()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("PRODID;DERIVED=true:a"));

            Action setProductId = () => card.SetProductId("b");
            setProductId.Should().Throw<InvalidOperationException>();

            // act
            var testResult = card.ProductId.Value;

            // assert
            testResult.Should().Be("a");
        }

        [Fact]
        public void Setter_PropertyWithDerivedFalse_ReplacesIt()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("PRODID;DERIVED=false:a"));

            // act
            card.SetProductId("b");

            // assert
            card.ProductId.Value.Should().Be("b");
        }

        [Fact]
        public void Setter_AfterRemovingAPropertyWithDerivedTrue_SetsIt()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("PRODID;DERIVED=true:a"));
            card.RemoveProperties(VcardPropertyNames.ProductId);

            // act
            card.SetProductId("b");

            // assert
            card.ProductId.Value.Should().Be("b");
        }

        // Name components

        public static TheoryData<string, string> BackwardCompatibleNameData()
        {
            return new TheoryData<string, string>
            {
                // ref. RFC 9554 §2.2: the value goes in the new component and its backwards-compatible counterpart.
                { "Doe;John;;;;Smith;", "Doe,Smith;John;;;;Smith;" },
                { "Doe;John;;;M.D.;;Jr.", "Doe;John;;;M.D.,Jr.;;Jr." },
                { "Doe;John;;;;Smith;Jr.", "Doe,Smith;John;;;Jr.;Smith;Jr." },

                // Already in both, as in the example of RFC 9554 §2.2.
                { "Stevenson;John;Philip,Paul;Dr.;Jr.,M.D.,A.C.P.;;Jr.", "Stevenson;John;Philip,Paul;Dr.;Jr.,M.D.,A.C.P.;;Jr." },

                // Nothing to do.
                { "Public;John;Quinlan;Mr.;Esq.", "Public;John;Quinlan;Mr.;Esq." }
            };
        }

        [Theory]
        [MemberData(nameof(BackwardCompatibleNameData))]
        public void WithBackwardCompatibleComponents_Name_AddsTheNewValuesToTheOldComponents(string value, string expected)
        {
            // arrange
            var name = VcardName.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = name.WithBackwardCompatibleComponents();

            // assert
            testResult.ToString(VcardValueFormat.Text).Should().Be(expected);
        }

        [Fact]
        public void WithBackwardCompatibleComponents_NameNotInitialised_Throws()
        {
            // arrange
            var name = default(VcardName);

            // act
            Action testAction = () => name.WithBackwardCompatibleComponents();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void HonorificSuffixesExcludingGenerations_Rfc9554Example_LeavesOutTheGeneration()
        {
            // arrange
            // ref. RFC 9554 §2.2: 'a "Jr." that occurs in both honorific suffixes and generation should only be handled as a generational marker.'
            var name = VcardName.Parse("Stevenson;John;Philip,Paul;Dr.;Jr.,M.D.,A.C.P.;;Jr.".AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = name.HonorificSuffixesExcludingGenerations;

            // assert
            testResult.Should().Equal("M.D.", "A.C.P.");
        }

        [Fact]
        public void FamilyNamesExcludingSecondarySurnames_SurnameInBoth_LeavesItOut()
        {
            // arrange
            var name = VcardName.Parse("Doe,Smith;John;;;;Smith;".AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = name.FamilyNamesExcludingSecondarySurnames;

            // assert
            testResult.Should().Equal("Doe");
        }

        [Fact]
        public void SetName_NameWithNewComponents_WritesThemToTheOldComponentsToo()
        {
            // arrange
            var name = VcardName.Parse("Doe;John;;;;Smith;Jr.".AsSpan(), VcardValueFormat.Text);
            var card = VcardCardTestData.NewCard();
            card.SetName(name);

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(VcardCardTestData.Card("N:Doe,Smith;John;;;Jr.;Smith;Jr."));
        }

        [Fact]
        public void ReadCard_NameWrittenByTheModel_IsReadableWithoutRfc9554()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            card.SetName(VcardName.Parse("Doe;John;;;;Smith;Jr.".AsSpan(), VcardValueFormat.Text));

            // act
            var testResult = VcardCardTestData.Read(VcardCardTestData.Write(card)).Name.Value;

            // assert
            // What a consumer that doesn't know RFC 9554 reads, and what one that does reads, are both there.
            testResult.FamilyNames.Should().Equal("Doe", "Smith");
            testResult.HonorificSuffixes.Should().Equal("Jr.");
            testResult.SecondarySurnames.Should().Equal("Smith");
            testResult.Generations.Should().Equal("Jr.");
        }

        [Fact]
        public void Properties_LineAddedDirectly_IsLeftAsItIs()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            card.Properties.Add(new VcardContentLine(VcardPropertyNames.Name, "Doe;John;;;;Smith;Jr."));

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(VcardCardTestData.Card("N:Doe;John;;;;Smith;Jr."));
        }

        // Address components

        public static TheoryData<string, string> BackwardCompatibleAddressData()
        {
            return new TheoryData<string, string>
            {
                // ref. RFC 9554 §2.1: the example, whose street number and name are "combined in the street component for backwards compatibility".
                { ";;;Any Town;CA;91921-1234;U.S.A;;;;123;Main Street;;;;;;", MainStreetAddress + ";;;;123;Main Street;;;;;;" },

                // Each of the new components, in the order of the street address.
                { ";;;;;;;Room;Apartment;Floor;Number;Name;Building;Block;Subdistrict;District;Landmark;Direction",
                    ";;Number Name Direction Building Block Floor Apartment Room Landmark Subdistrict District;;;;;Room;Apartment;Floor;Number;Name;Building;Block;Subdistrict;District;Landmark;Direction" },

                // The items of a component.
                { ";;;;;;;;;;;A,B;;;;;;", ";;A B;;;;;;;;;A,B;;;;;;" },

                // A street address with no text in it is no street address.
                { ";;,;;;;;;;;123;;;;;;;", ";;123;;;;;;;;123;;;;;;;" },

                // A street address is left as it is.
                { ";;Other;;;;;;;;123;Main Street;;;;;;", ";;Other;;;;;;;;123;Main Street;;;;;;" },

                // Nothing to do.
                { MainStreetAddress, MainStreetAddress },
                { ";;;;;;", ";;;;;;" }
            };
        }

        [Theory]
        [MemberData(nameof(BackwardCompatibleAddressData))]
        public void WithBackwardCompatibleComponents_Address_CombinesTheNewValuesInTheStreetAddress(string value, string expected)
        {
            // arrange
            var address = VcardAddress.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = address.WithBackwardCompatibleComponents();

            // assert
            testResult.ToString(VcardValueFormat.Text).Should().Be(expected);
        }

        [Fact]
        public void WithBackwardCompatibleComponents_AddressNotInitialised_Throws()
        {
            // arrange
            var address = default(VcardAddress);

            // act
            Action testAction = () => address.WithBackwardCompatibleComponents();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineData(MainStreetAddress, false)]
        [InlineData(";;;;;;;;;;;;;;;;;", false)]
        [InlineData(";;;;;;;,;;;;;;;;;;", false)]
        [InlineData(";;;;;;;a;;;;;;;;;;", true)]
        [InlineData(";;;;;;;;;;;;;;;;;a", true)]
        public void HasDetailedComponents_Address_SaysIfOneHasAValue(string value, bool expected)
        {
            // arrange
            var address = VcardAddress.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = address.HasDetailedComponents;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Add_AddressWithNewComponents_GivesTheFormItWrites()
        {
            // arrange
            var address = VcardAddress.Parse(";;;Any Town;CA;91921-1234;U.S.A;;;;123;Main Street;;;;;;".AsSpan(), VcardValueFormat.Text);
            var card = VcardCardTestData.NewCard();

            // act
            var testResult = card.Addresses.Add(address);

            // assert
            testResult.Value.StreetAddress.Should().Equal("123 Main Street");
        }

        [Fact]
        public void WriteCard_AddressWithNewComponents_WritesThemToTheStreetAddressToo()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            card.Addresses.Add(VcardAddress.Parse(";;;Any Town;CA;91921-1234;U.S.A;;;;123;Main Street;;;;;;".AsSpan(), VcardValueFormat.Text));

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(VcardCardTestData.Card("ADR:;;123 Main Street;Any Town;CA;91921-1234;U.S.A;;;;123;Main Street;;;;;;"));
        }

        [Fact]
        public void ReadCard_Rfc9554AddressExample_GivesBothForms()
        {
            // arrange
            var text = VcardCardTestData.Card(Rfc9554AddressExample);

            // act
            var testResult = VcardCardTestData.Read(text).Addresses[0].Value;

            // assert
            // ref. RFC 9554 §2.1: a consumer that doesn't know the RFC reads the street component, and one that does is told to ignore it.
            testResult.StreetAddress.Should().Equal("123 Main Street");
            testResult.StreetNumber.Should().Equal("123");
            testResult.StreetName.Should().Equal("Main Street");
            testResult.HasDetailedComponents.Should().BeTrue();
        }

        [Theory]
        [InlineData("Doe;John;;;;Smith;", VcardVersion.V3_0)]
        [InlineData("Doe;John;;;;Smith;", VcardVersion.V2_1)]
        public void IsDefinedIn_NameWithNewComponentsAfterFolding_IsStillOnlyInVersion40(string value, VcardVersion version)
        {
            // arrange
            // ref. RFC 9554 §2.2: the new components don't go away when the old ones hold their values too, so #309's writer has them to report as lossy.
            var name = VcardName.Parse(value.AsSpan(), VcardValueFormat.Text).WithBackwardCompatibleComponents();

            // act
            var testResult = name.IsDefinedIn(version);

            // assert
            testResult.Should().BeFalse();
        }

        // Options

        [Fact]
        public void ReadCard_PhoneticSystemNotInTheRegistryWithDefaultOptions_Throws()
        {
            // arrange
            var text = VcardCardTestData.Card("ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=x-a:;;b;;;;");

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Fact]
        public void ReadCard_PhoneticSystemNotInTheRegistryWithTheOptIn_KeepsTheLine()
        {
            // arrange
            var text = VcardCardTestData.Card("ADR;ALTID=1:;;a;;;;", "ADR;ALTID=1;PHONETIC=x-a:;;b;;;;");

            // act
            var testResult = VcardCardTestData.Read(text, new VcardOptions { AllowInvalidValues = true });

            // assert
            testResult.Addresses[1].Line.GetParameter(VcardParameterNames.Phonetic).Values.Single().Should().Be("x-a");
            testResult.Addresses[1].Phonetic.Should().BeNull();
        }
    }
}
