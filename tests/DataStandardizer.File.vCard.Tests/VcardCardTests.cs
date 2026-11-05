using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    public class VcardCardTests
    {
        private const string GroupKind = "group";

        public static TheoryData<string, VcardKind> KindData()
        {
            // ref. RFC 6350 §6.1.4, RFC 6473 §2 and RFC 6869 §2.
            return new TheoryData<string, VcardKind>
            {
                { "KIND:individual", VcardKind.Individual },
                { "KIND:group", VcardKind.Group },
                { "KIND:GROUP", VcardKind.Group },
                { "KIND:org", VcardKind.Organization },
                { "KIND:location", VcardKind.Location },
                { "KIND:application", VcardKind.Application },
                { "KIND:APPLICATION", VcardKind.Application },
                { "KIND:device", VcardKind.Device },
                { "KIND:x-robot", VcardKind.Other }
            };
        }

        [Theory]
        [MemberData(nameof(KindData))]
        public void Kind_CardHasAValue_GivesIt(string kindLine, VcardKind expected)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(kindLine));

            // act
            var testResult = card.Kind;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Kind_NoProperty_IsIndividual()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card());

            // act
            var testResult = card.Kind;

            // assert
            testResult.Should().Be(VcardKind.Individual);
        }

        [Fact]
        public void KindText_OtherValue_KeepsItAsWritten()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("KIND:x-robot"));

            // act
            var testResult = card.KindText;

            // assert
            testResult.Should().Be("x-robot");
        }

        public static TheoryData<VcardKind, string> SetKindData()
        {
            return new TheoryData<VcardKind, string>
            {
                { VcardKind.Individual, "individual" },
                { VcardKind.Group, "group" },
                { VcardKind.Organization, "org" },
                { VcardKind.Location, "location" },
                { VcardKind.Application, "application" },
                { VcardKind.Device, "device" }
            };
        }

        [Theory]
        [MemberData(nameof(SetKindData))]
        public void SetKind_Kind_WritesItsText(VcardKind kind, string expected)
        {
            // arrange
            var card = new VcardCard();

            // act
            card.SetKind(kind);

            // assert
            card.GetProperties(VcardPropertyNames.Kind).Single().Value.Should().Be(expected);
        }

        [Fact]
        public void SetKind_Other_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            var card = new VcardCard();

            // act
            Action testAction = () => card.SetKind(VcardKind.Other);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void SetName_AlreadyHasAlternatives_ReplacesThemInThePlaceOfTheFirst()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N;ALTID=1;LANGUAGE=ja:a;;;;", "NOTE:b", "N;ALTID=1;LANGUAGE=en:c;;;;"));

            // act
            card.SetName(VcardName.Parse("d;;;;".AsSpan(), VcardValueFormat.Text));

            // assert
            card.Properties.Select(line => line.Value).Should().Equal("Jane Doe", "d;;;;", "b");
        }

        [Fact]
        public void SetBirthday_Text_WritesValueParameter()
        {
            // arrange
            var card = new VcardCard();

            // act
            card.SetBirthday(new VcardTypedValue("circa 1800"));

            // assert
            LineDescription.Of(card.Properties.Single()).Should().Be("0/0 BDAY;VALUE=text:circa 1800");
        }

        [Fact]
        public void SetBirthday_Date_WritesNoValueParameter()
        {
            // arrange
            var card = new VcardCard();
            var date = VcardDateAndOrTime.Parse("19960415".AsSpan(), VcardValueFormat.Text);

            // act
            card.SetBirthday(new VcardTypedValue(date));

            // assert
            LineDescription.Of(card.Properties.Single()).Should().Be("0/0 BDAY:19960415");
        }

        [Fact]
        public void SetBirthday_Uri_ThrowsAndLeavesTheCardAsItWas()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("BDAY:19960415"));

            // act
            Action testAction = () => card.SetBirthday(new VcardTypedValue(new Uri("http://example.com/")));

            // assert
            testAction.Should().Throw<ArgumentException>();
            card.Birthday.Value.DateAndOrTime.ToString(VcardValueFormat.Text).Should().Be("19960415");
        }

        [Fact]
        public void SetUid_DefaultValue_ThrowsArgumentException()
        {
            // arrange
            var card = new VcardCard();

            // act
            Action testAction = () => card.SetUid(default(VcardTypedValue));

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetName_DefaultValue_ThrowsAndLeavesTheCardAsItWas()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N:a;;;;"));

            // act
            Action testAction = () => card.SetName(default(VcardName));

            // assert
            testAction.Should().Throw<ArgumentException>();
            card.Name.Value.ToString(VcardValueFormat.Text).Should().Be("a;;;;");
        }

        [Fact]
        public void SetName_EmptyComponents_IsNotTakenForADefaultValue()
        {
            // arrange
            var card = new VcardCard();

            // act
            card.SetName(VcardName.Parse(";;;;".AsSpan(), VcardValueFormat.Text));

            // assert
            card.Properties.Single().Value.Should().Be(";;;;");
        }

        [Fact]
        public void Add_AdditionalValueParameter_ThrowsArgumentException()
        {
            // arrange
            var card = new VcardCard();
            var parameters = new VcardPropertyParameters();
            parameters.AdditionalParameters.Add(new VcardContentLineParameter("VALUE", "uri"));

            // act
            Action testAction = () => card.Telephones.Add(new VcardTypedValue("1"), parameters);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddExtensionProperty_AdditionalValueParameter_KeepsIt()
        {
            // arrange
            var card = new VcardCard();
            var parameters = new VcardPropertyParameters();
            parameters.AdditionalParameters.Add(new VcardContentLineParameter("VALUE", "uri"));

            // act
            card.AddExtensionProperty("X-A", "urn:a", parameters);

            // assert
            LineDescription.Of(card.Properties.Single()).Should().Be("0/0 X-A;VALUE=uri:urn:a");
        }

        [Fact]
        public void Add_ListChangedAfterwards_DoesNotChangeTheInstanceReturned()
        {
            // arrange
            var card = new VcardCard();
            var nicknames = new[] { "Jim" };
            var testResult = card.Nicknames.Add(nicknames);

            // act
            nicknames[0] = "Tim";

            // assert
            testResult.Value.Should().Equal("Jim");
        }

        [Fact]
        public void Add_XmlThatIsNotOneElement_ThrowsArgumentException()
        {
            // arrange
            var card = new VcardCard();

            // act
            Action testAction = () => card.Xml.Add("not an element");

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Remove_InstanceOfAnotherProperty_ReturnsFalseAndKeepsIt()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE:a"));
            var note = card.Notes[0];

            // act
            var testResult = card.Emails.Remove(note);

            // assert
            testResult.Should().BeFalse();
            card.Notes.Should().ContainSingle();
        }

        [Fact]
        public void FindByUid_TextThatLooksLikeAnEquivalentUri_DoesNotMatch()
        {
            // arrange
            // ref. RFC 6350 §6.7.6: free-form text is not a URI, so RFC 3986 §6 doesn't apply to it.
            var card = new VcardCard();
            card.SetUid(new VcardTypedValue("HTTP://EXAMPLE.com/a"));

            // act
            var testResult = VcardCard.FindByUid(new[] { card }, "http://example.com/a");

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void ResolveMembers_NullInTheCards_ThrowsArgumentException()
        {
            // arrange
            var card = new VcardCard();
            var cards = new VcardCard[] { null };

            // act
            Action testAction = () => card.ResolveMembers(cards);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Properties_AddNull_ThrowsArgumentNullException()
        {
            // arrange
            var card = new VcardCard();

            // act
            Action testAction = () => card.Properties.Add(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Add_ParametersGiven_WritesThem()
        {
            // arrange
            var card = new VcardCard();
            var parameters = new VcardPropertyParameters { Group = "item", Pref = 1, AltId = "2" };
            parameters.Types.Add("work");
            parameters.Pid.Add(new VcardPid(1, 2));
            parameters.AdditionalParameters.Add(new VcardContentLineParameter("X-A", "b"));

            // act
            card.Emails.Add("a@example.com", parameters);

            // assert
            LineDescription.Of(card.Properties.Single()).Should().Be("0/0 item.EMAIL;PREF=1;ALTID=2;PID=1.2;TYPE=work;X-A=b:a@example.com");
        }

        [Fact]
        public void Add_TextWithSpecialCharacters_EscapesItAndGivesItBack()
        {
            // arrange
            const string Text = "a, b\\c\nd";
            var card = new VcardCard();

            // act
            var testResult = card.Notes.Add(Text);

            // assert
            testResult.Line.Value.Should().Be("a\\, b\\\\c\\nd");
            card.Notes.Single().Value.Should().Be(Text);
        }

        [Fact]
        public void Add_ListValue_WritesItWithCommas()
        {
            // arrange
            var card = new VcardCard();

            // act
            card.Nicknames.Add(new[] { "Jim", "Jim, Jr." });

            // assert
            card.Properties.Single().Value.Should().Be("Jim,Jim\\, Jr.");
            card.Nicknames[0].Value.Should().Equal("Jim", "Jim, Jr.");
        }

        [Fact]
        public void Add_NullValue_ThrowsArgumentNullException()
        {
            // arrange
            var card = new VcardCard();

            // act
            Action testAction = () => card.Notes.Add(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Pref_OutOfRangeInParameters_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            const int AboveTheLargestPreference = 101;
            var parameters = new VcardPropertyParameters();

            // act
            Action testAction = () => parameters.Pref = AboveTheLargestPreference;

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Remove_Instance_TakesItFromTheCard()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE:a", "NOTE:b"));

            // act
            var testResult = card.Notes.Remove(card.Notes[0]);

            // assert
            testResult.Should().BeTrue();
            card.Notes.Select(note => note.Value).Should().Equal("b");
        }

        [Fact]
        public void Clear_InstancesWithAnInvalidValue_RemovesThemToo()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("URL:http://example.com/", "URL:not a uri"), new VcardOptions { AllowInvalidValues = true });

            // act
            card.Urls.Clear();

            // assert
            card.GetProperties(VcardPropertyNames.Url).Should().BeEmpty();
        }

        [Fact]
        public void GetPreferred_DifferentPrefs_GivesTheLowest()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TEL:none", "TEL;PREF=20:twenty", "TEL;PREF=5:five", "TEL;PREF=5:other five"));

            // act
            var testResult = card.GetPreferred(VcardPropertyNames.Telephone);

            // assert
            testResult.Value.Should().Be("five");
        }

        [Fact]
        public void GetPreferred_NoPref_GivesTheFirst()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TEL:one", "TEL:two"));

            // act
            var testResult = card.GetPreferred(VcardPropertyNames.Telephone);

            // assert
            testResult.Value.Should().Be("one");
        }

        [Fact]
        public void GetPreferred_NoInstances_ReturnsNull()
        {
            // arrange
            var card = new VcardCard();

            // act
            var testResult = card.GetPreferred(VcardPropertyNames.Telephone);

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void GetPreferredOfList_DifferentPrefs_GivesTheLowest()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("EMAIL:none@example.com", "EMAIL;PREF=2:two@example.com"));

            // act
            var testResult = card.Emails.GetPreferred();

            // assert
            testResult.Value.Should().Be("two@example.com");
        }

        [Fact]
        public void GetAlternatives_SomeWithAltId_GroupsThemInOrderOfFirstAppearance()
        {
            // arrange
            // ref. RFC 6350 §5.4: "Property instances without the ALTID parameter MUST NOT be considered an alternative representation of any other property instance."
            var card = VcardCardTestData.Read(VcardCardTestData.Card(
                "TITLE;ALTID=1;LANGUAGE=fr:Patron",
                "TITLE:Solo",
                "TITLE;ALTID=2;LANGUAGE=en:Chief",
                "TITLE:Other",
                "TITLE;ALTID=1;LANGUAGE=en:Boss"));

            // act
            var testResult = card.GetAlternatives(VcardPropertyNames.Title);

            // assert
            VcardCardTestData.ValuesOf(testResult).Should().BeEquivalentTo(
                new List<List<string>>
                {
                    new List<string> { "Patron", "Boss" },
                    new List<string> { "Solo" },
                    new List<string> { "Chief" },
                    new List<string> { "Other" }
                },
                options => options.WithStrictOrdering());
        }

        [Fact]
        public void GetAlternatives_SameAltIdOnAnotherProperty_DoesNotGroupThem()
        {
            // arrange
            // ref. RFC 6350 §5.4: ALTID values "MAY be reused for different property names".
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TITLE;ALTID=1:a", "NOTE;ALTID=1:b"));

            // act
            var testResult = card.GetAlternatives(VcardPropertyNames.Title);

            // assert
            testResult.Single().Select(line => line.Value).Should().Equal("a");
        }

        [Fact]
        public void GetAlternativesOfList_SameAltId_GroupsThem()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TITLE;ALTID=1;LANGUAGE=fr:Patron", "TITLE;ALTID=1;LANGUAGE=en:Boss"));

            // act
            var testResult = card.Titles.GetAlternatives();

            // assert
            testResult.Single().Select(title => title.Value).Should().Equal("Patron", "Boss");
        }

        [Fact]
        public void ResolveMembers_SomeAreNotCards_LeavesThemOut()
        {
            // arrange
            const string MemberUid = "urn:uuid:03a0e51f-d1aa-4385-8a53-e29025acd8af";
            var group = new VcardCard();
            group.SetKind(GroupKind);
            group.Members.Add(new Uri("mailto:subscriber1@example.com"));
            group.Members.Add(new Uri(MemberUid));
            var member = new VcardCard();
            member.SetUid(new VcardTypedValue(new Uri(MemberUid)));

            // act
            var testResult = group.ResolveMembers(new[] { member });

            // assert
            testResult.Should().Equal(member);
        }

        [Fact]
        public void FindByUid_FreeFormText_MatchesItExactly()
        {
            // arrange
            var card = new VcardCard();
            card.SetUid(new VcardTypedValue("My Uid"));

            // act
            var testResult = VcardCard.FindByUid(new[] { card }, "My Uid");

            // assert
            testResult.Should().BeSameAs(card);
        }

        [Fact]
        public void FindByUid_EquivalentUris_MatchesThem()
        {
            // arrange
            // ref. RFC 3986 §6.2.2.1: the scheme and host are case-insensitive.
            var card = new VcardCard();
            card.SetUid(new VcardTypedValue(new Uri("HTTP://EXAMPLE.com/a")));

            // act
            var testResult = VcardCard.FindByUid(new[] { card }, "http://example.com/a");

            // assert
            testResult.Should().BeSameAs(card);
        }

        [Fact]
        public void FindByUid_NullInTheCards_ThrowsArgumentException()
        {
            // arrange
            var cards = new VcardCard[] { null };

            // act
            Action testAction = () => VcardCard.FindByUid(cards, "a");

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Birthday_UnknownCalendarScale_IsLeftOutAsTheRfcSays()
        {
            // arrange
            // ref. RFC 6350 §5.8: an implementation "MUST ignore properties with a CALSCALE parameter value that it does not understand".
            var card = VcardCardTestData.Read(VcardCardTestData.Card("BDAY;CALSCALE=x-mayan:19960415"));

            // act
            var testResult = card.Birthday;

            // assert
            testResult.Should().BeNull();
            card.GetProperties(VcardPropertyNames.Birthday).Should().ContainSingle();
        }

        [Fact]
        public void ExtensionProperties_PropertyOnlyAnOlderVersionDefines_IncludesIt()
        {
            // arrange
            // ref. RFC 6350 Appendix A: MAILER is gone from vCard 4.0, so here it is unknown.
            var card = VcardCardTestData.Read(VcardCardTestData.Card("MAILER:a", "X-B:c"));

            // act
            var testResult = card.ExtensionProperties;

            // assert
            testResult.Select(property => property.Name).Should().Equal("MAILER", "X-B");
        }

        [Fact]
        public void ExtensionParameters_MixOfParameters_GivesOnlyThoseRfc6350DoesNotDefine()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE;LANGUAGE=en;X-A=b:c"));

            // act
            var testResult = card.Notes[0].ExtensionParameters;

            // assert
            testResult.Select(parameter => parameter.Name).Should().Equal("X-A");
        }

        [Fact]
        public void AddExtensionProperty_PropertyRfc6350Defines_ThrowsArgumentException()
        {
            // arrange
            var card = new VcardCard();

            // act
            Action testAction = () => card.AddExtensionProperty(VcardPropertyNames.Note, "a");

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        public static TheoryData<Func<VcardProperty<VcardTypedValue>, object>, object> WrapperData()
        {
            return new TheoryData<Func<VcardProperty<VcardTypedValue>, object>, object>
            {
                { property => property.Name, "TEL" },
                { property => property.Group, "item" },
                { property => property.AltId, "a" },
                { property => property.Pref, 7 },
                { property => property.Language.Value.ToString(), "en" },
                { property => property.MediaType.Value.ToString(), "text/plain" },
                { property => property.CalendarScale, "gregorian" },
                { property => property.Label, "b" },
                { property => property.GeoLocation.OriginalString, "geo:1,2" },
                { property => property.TimeZone, "c" }
            };
        }

        [Theory]
        [MemberData(nameof(WrapperData))]
        public void Member_LineHasTheParameter_GivesIt(Func<VcardProperty<VcardTypedValue>, object> member, object expected)
        {
            // arrange
            var card = new VcardCard();
            card.Properties.Add(new VcardContentLine("item", "TEL", new[]
            {
                new VcardContentLineParameter("ALTID", "a"),
                new VcardContentLineParameter("PREF", "7"),
                new VcardContentLineParameter("LANGUAGE", "en"),
                new VcardContentLineParameter("MEDIATYPE", "text/plain"),
                new VcardContentLineParameter("CALSCALE", "gregorian"),
                new VcardContentLineParameter("LABEL", "b"),
                new VcardContentLineParameter("GEO", "geo:1,2"),
                new VcardContentLineParameter("TZ", "c")
            }, "1"));

            // act
            var testResult = member(card.Telephones[0]);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Pid_SeveralValues_GivesThemInOrder()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TEL;PID=3.1,4.2;VALUE=uri:tel:+1-555-555-5555", "CLIENTPIDMAP:1;urn:a", "CLIENTPIDMAP:2;urn:b"));

            // act
            var testResult = card.Telephones[0].Pid;

            // assert
            testResult.Should().Equal(new VcardPid(3, 1), new VcardPid(4, 2));
        }

        [Fact]
        public void SortAs_QuotedList_SplitsIt()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N;SORT-AS=\"Harten,Rene\":van der Harten;Rene,J.;Sir;R.D.O.N.;"));

            // act
            var testResult = card.Name.SortAs;

            // assert
            testResult.Should().Equal("Harten", "Rene");
        }

        [Fact]
        public void Types_ListCastToArray_IsNotAnArray()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TEL;TYPE=home:1"));

            // act
            var testResult = card.Telephones[0].Types;

            // assert
            (testResult is string[]).Should().BeFalse();
        }

        [Fact]
        public void Validate_CardFromCode_FindsEveryProblem()
        {
            // arrange
            var card = new VcardCard();
            card.Properties.Add(new VcardContentLine("URL", "not a uri"));
            card.Properties.Add(new VcardContentLine("MEMBER", "urn:a"));

            // act
            var testResult = card.Validate();

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(
                VcardDiagnosticKind.InvalidValue, VcardDiagnosticKind.CardinalityViolation, VcardDiagnosticKind.MissingRequiredProperty);
        }

        [Fact]
        public void ToString_Card_CountsTheProperties()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            var testResult = card.ToString();

            // assert
            testResult.Should().Be("VCARD (1 properties)");
        }

        [Fact]
        public void DiagnosticToString_LineNumberKnown_NamesTheLine()
        {
            // arrange
            const long LineNumber = 3;
            var diagnostic = new VcardDiagnostic(VcardDiagnosticKind.InvalidValue, "a", "b", null, LineNumber, 0);

            // act
            var testResult = diagnostic.ToString();

            // assert
            testResult.Should().Be("Line 3: a");
        }
    }
}
