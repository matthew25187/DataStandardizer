using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of <see cref="IVcard30Card"/>: its members are of vCard 3.0, and each getter and setter goes through the canonical form.
    /// </summary>
    public class IVcard30CardTests
    {
        private const string Base64 = "AQID";
        private static readonly byte[] Data = { 1, 2, 3 };
        private static readonly Uri ExampleUri = new Uri("http://example.com/a,b");

        private static IVcard30Card View(params string[] lines)
        {
            return VcardCardTestData.Read(Vcard30TestData.Card(lines)).AsVersion30();
        }

        public static TheoryData<string> Version40MemberData()
        {
            return new TheoryData<string>
            {
                "Kind", "KindText", "Gender", "Anniversary", "Members", "Related", "Languages", "ClientPidMaps", "Xml", "CalendarScale",
                "BirthPlace", "DeathPlace", "DeathDate", "Expertise", "Hobbies", "Interests", "OrganizationDirectories", "ContactUris",
                "Created", "GrammaticalGenders", "DefaultLanguage", "Pronouns", "SocialProfiles", "Properties"
            };
        }

        [Theory]
        [MemberData(nameof(Version40MemberData))]
        public void Interface_MemberVersion30DoesNotDefine_IsNotThere(string name)
        {
            // act
            var testResult = typeof(IVcard30Card).GetMember(name);

            // assert
            testResult.Should().BeEmpty();
        }

        public static TheoryData<string> Version40PropertyMemberData()
        {
            return new TheoryData<string> { "Pref", "Pid", "AltId", "MediaType", "SortAs", "Label", "GeoLocation", "TimeZone", "CalendarScale", "Index", "Level", "Line" };
        }

        [Theory]
        [MemberData(nameof(Version40PropertyMemberData))]
        public void PropertyInterface_MemberVersion30DoesNotDefine_IsNotThere(string name)
        {
            // act
            var testResult = typeof(IVcard30Property<string>).GetMember(name);

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void Interface_OtherVersions_AreNotInherited()
        {
            // act
            var testResult = typeof(IVcard30Card).GetInterfaces();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void AsVersion30_Card_IsTheSameObject()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            var testResult = card.AsVersion30();

            // assert
            testResult.Should().BeSameAs(card);
        }

        [Fact]
        public void IsPreferred_TypePrefOnALine_IsTrueAndTypesLeavesItOut()
        {
            // act
            var testResult = View("TEL;TYPE=work,pref:+1").Telephones.Single();

            // assert
            testResult.IsPreferred.Should().BeTrue();
            testResult.Types.Should().Equal("work");
        }

        [Fact]
        public void IsPreferred_NotTheLowestPref_IsFalse()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TEL;PREF=2:+1", "TEL;PREF=1:+2")).AsVersion30();

            // act
            var testResult = card.Telephones;

            // assert
            testResult[0].IsPreferred.Should().BeFalse();
            testResult[1].IsPreferred.Should().BeTrue();
        }

        [Fact]
        public void Add_IsPreferred_HoldsItAsThePrefOfTheCanonicalForm()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            card.AsVersion30().Emails.Add("a@example.com", new Vcard30PropertyParameters { IsPreferred = true, Group = "g", Types = { "internet" }, Language = "i-klingon" });

            // assert
            card.Emails.Single().Pref.Should().Be(1);
            card.Emails.Single().Group.Should().Be("g");
            card.Emails.Single().Types.Should().Equal("internet");
            card.Emails.Single().Line.GetParameter(VcardParameterNames.Language).Values.Should().Equal("i-klingon");
        }

        [Fact]
        public void GeoLocations_Geo_GivesLatitudeAndLongitude()
        {
            // act
            var testResult = View("GEO:37.386013;-122.082932").GeoLocations.Single().Value;

            // assert
            testResult.ToString(VcardValueFormat.Text, VcardVersion.V3_0).Should().Be("37.386013;-122.082932");
        }

        [Fact]
        public void GeoLocations_UriThatIsNotALatitudeAndLongitude_IsHidden()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("GEO:http://example.com")).AsVersion30();

            // act
            var testResult = card.GeoLocations.Count;

            // assert
            testResult.Should().Be(0);
        }

        [Fact]
        public void GeoLocations_Add_HoldsAGeoUri()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            var geo = VcardGeo.Parse("37.386013;-122.082932".AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0);

            // act
            card.AsVersion30().GeoLocations.Add(geo);

            // assert
            card.GetProperties(VcardPropertyNames.Geo).Single().Value.Should().Be("geo:37.386013\\,-122.082932");
        }

        [Fact]
        public void Telephones_TelUri_GivesItsNumberAndHidesAnotherUri()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TEL;VALUE=uri:tel:+1-555;ext=5", "TEL;VALUE=uri:sip:a@example.com", "TEL:+2")).AsVersion30();

            // act
            var testResult = card.Telephones;

            // assert
            testResult.Should().HaveCount(2);
            testResult[0].Value.Should().Be("+1-555");
            testResult[1].Value.Should().Be("+2");
        }

        [Fact]
        public void Telephones_Add_HoldsText()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            var testResult = card.AsVersion30().Telephones.Add("+1");

            // assert
            testResult.Value.Should().Be("+1");
            card.Telephones.Single().Value.Text.Should().Be("+1");
        }

        [Fact]
        public void Photos_InlineData_GivesATokenAndBytes()
        {
            // act
            var testResult = View("PHOTO;ENCODING=b;TYPE=JPEG:" + Base64).Photos.Single().Value;

            // assert
            testResult.TypeToken.Should().Be("JPEG");
            testResult.GetData().Should().Equal(Data);
            testResult.Uri.Should().BeNull();
        }

        [Fact]
        public void Photos_Uri_GivesATokenAndTheUri()
        {
            // act
            var testResult = View("PHOTO;VALUE=uri;TYPE=GIF:http://example.com/a.gif").Photos.Single().Value;

            // assert
            testResult.TypeToken.Should().Be("GIF");
            testResult.Uri.OriginalString.Should().Be("http://example.com/a.gif");
            testResult.GetData().Should().BeNull();
        }

        [Fact]
        public void Photos_AddData_HoldsADataUri()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            card.AsVersion30().Photos.Add(VcardMediaValue.FromData(Data, "JPEG"));

            // assert
            card.GetProperties(VcardPropertyNames.Photo).Single().Value.Should().Be("data:image/jpeg;base64\\," + Base64);
        }

        [Fact]
        public void Photos_AddUri_HoldsAMediaType()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            card.AsVersion30().Photos.Add(VcardMediaValue.FromUri(new Uri("http://example.com/a.gif"), "GIF"));

            // assert
            card.Photos.Single().MediaType.ToString().Should().Be("image/gif");
        }

        [Fact]
        public void Keys_Text_GivesItsTextAndType()
        {
            // act
            var testResult = View("KEY;TYPE=PGP:abc").Keys.Single().Value;

            // assert
            testResult.Text.Should().Be("abc");
            testResult.TypeToken.Should().Be("PGP");
        }

        [Fact]
        public void Keys_AddText_HoldsTextWithItsType()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            card.AsVersion30().Keys.Add(VcardMediaValue.FromText("abc", "PGP"));

            // assert
            card.Keys.Single().Value.Text.Should().Be("abc");
            card.Keys.Single().Types.Should().Equal("PGP");
        }

        [Fact]
        public void Keys_AddData_HoldsADataUri()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            card.AsVersion30().Keys.Add(VcardMediaValue.FromData(Data, "PGP"));

            // assert
            card.GetProperties(VcardPropertyNames.Key).Single().Value.Should().Be("data:application/pgp-keys;base64\\," + Base64);
        }

        [Fact]
        public void Labels_LabelOnAnAddress_GivesIt()
        {
            // act
            var testResult = View("ADR;TYPE=home:;;1 First St;;;;", "LABEL;TYPE=home:First\\nStreet").Labels.Single();

            // assert
            testResult.Name.Should().Be(VcardPropertyNames.PostalLabel);
            testResult.Value.Should().Be("First\nStreet");
            testResult.Types.Should().Equal("home");
        }

        [Fact]
        public void Labels_LabelOfItsOwn_GivesIt()
        {
            // act
            var testResult = View("LABEL:Nowhere").Labels.Single().Value;

            // assert
            testResult.Should().Be("Nowhere");
        }

        [Fact]
        public void Labels_AddWithTheTypesOfAnAddress_PutsItOnThatAddress()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("ADR;TYPE=home:;;1 First St;;;;"));

            // act
            var testResult = card.AsVersion30().Labels.Add("First", new Vcard30PropertyParameters { Types = { "home" } });

            // assert
            testResult.Value.Should().Be("First");
            card.Addresses.Single().Label.Should().Be("First");
            card.GetProperties(VcardPropertyNames.PostalLabel).Should().BeEmpty();
        }

        [Fact]
        public void Labels_RemoveLabelOnAnAddress_TakesItOffTheAddress()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("ADR:;;1 First St;;;;", "LABEL:First"));
            var labels = card.AsVersion30().Labels;

            // act
            var testResult = labels.Remove(labels.Single());

            // assert
            testResult.Should().BeTrue();
            card.Addresses.Single().Label.Should().BeNull();
        }

        [Fact]
        public void Labels_Clear_RemovesBothKinds()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("ADR:;;1 First St;;;;", "LABEL:First", "LABEL;X-A=1:Other"));

            // act
            card.AsVersion30().Labels.Clear();

            // assert
            card.AsVersion30().Labels.Should().BeEmpty();
        }

        [Fact]
        public void SortString_OnTheName_GivesIt()
        {
            // act
            var testResult = View("SORT-STRING:Doe").SortString;

            // assert
            testResult.Should().Be("Doe");
        }

        [Fact]
        public void SetSortString_CardWithAName_HoldsItOnTheName()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("SORT-STRING:Old"));

            // act
            card.AsVersion30().SetSortString("New");

            // assert
            card.Name.SortAs.Should().Equal("New");
            card.AsVersion30().SortString.Should().Be("New");
        }

        [Fact]
        public void SetSortString_CardWithNoNameOrOrganization_HoldsItAsAProperty()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            card.AsVersion30().SetSortString("New");

            // assert
            card.AsVersion30().SortString.Should().Be("New");
            card.GetProperties(VcardPropertyNames.SortString).Should().ContainSingle();
        }

        [Fact]
        public void Birthday_DateOfVersion30_GivesIt()
        {
            // act
            var testResult = View("BDAY:1996-04-15").Birthday.Value;

            // assert
            testResult.ToString(VcardValueFormat.Text, VcardVersion.V3_0).Should().Be("1996-04-15");
        }

        [Fact]
        public void Birthday_DateVersion30CanNotShow_IsHidden()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("BDAY:--0415")).AsVersion30();

            // act
            var testResult = card.Birthday;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void SetBirthday_DateVersion30CanNotHold_ThrowsArgumentException()
        {
            // arrange
            IVcard30Card card = VcardCardTestData.NewCard();
            var date = VcardDateAndOrTime.Parse("--0415".AsSpan(), VcardValueFormat.Text);

            // act
            Action testAction = () => card.SetBirthday(date);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetBirthday_Date_HoldsItInCanonicalForm()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            var date = VcardDateAndOrTime.Parse("1996-04-15".AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0);

            // act
            card.AsVersion30().SetBirthday(date);

            // assert
            card.GetProperties(VcardPropertyNames.Birthday).Single().Value.Should().Be("19960415");
        }

        [Fact]
        public void Name_ValueWithComponentsOfRfc9554_HidesThem()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N:Doe;John;;;;Smith;Jr.")).AsVersion30();

            // act
            var testResult = card.Name.Value;

            // assert
            testResult.IsDefinedIn(VcardVersion.V3_0).Should().BeTrue();
            testResult.FamilyNames.Should().Equal("Doe");
        }

        [Fact]
        public void Addresses_ValueVersion30CanNotShow_IsHidden()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("ADR:a,b;;;;;;")).AsVersion30();

            // act
            var testResult = card.Addresses.Count;

            // assert
            testResult.Should().Be(0);
        }

        [Fact]
        public void TimeZones_Offset_GivesItAndHidesAUri()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TZ;VALUE=utc-offset:-0500", "TZ;VALUE=uri:http://example.com")).AsVersion30();

            // act
            var testResult = card.TimeZones.Single().Value;

            // assert
            testResult.ValueType.Should().Be(VcardValueType.UtcOffset);
        }

        [Fact]
        public void Uid_TextAndUri_GivesThemAsText()
        {
            // arrange
            var first = View("UID:abc");
            var second = View("UID:urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6");

            // act
            var testResult = new[] { first.Uid.Value, second.Uid.Value };

            // assert
            testResult.Should().Equal("abc", "urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6");
        }

        public static TheoryData<string, string[], string> SetUidData()
        {
            return new TheoryData<string, string[], string>
            {
                { "abc", new[] { "VALUE=text" }, "abc" },
                { "urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6", new string[0], "urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6" }
            };
        }

        [Theory]
        [MemberData(nameof(SetUidData))]
        public void SetUid_Value_HoldsItInCanonicalForm(string value, string[] expectedParameters, string expectedValue)
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            card.AsVersion30().SetUid(value);

            // assert
            var line = card.GetProperties(VcardPropertyNames.Uid).Single();
            Vcard30TestData.ParametersOf(line).Should().Equal(expectedParameters);
            line.Value.Should().Be(expectedValue);
        }

        public static TheoryData<Action<IVcard30Card>, Func<IVcard30Card, string>> SimpleSetterData()
        {
            return new TheoryData<Action<IVcard30Card>, Func<IVcard30Card, string>>
            {
                { card => card.SetMailer("a"), card => card.Mailer.Value },
                { card => card.SetProductId("a"), card => card.ProductId.Value },
                { card => card.SetClassification("a"), card => card.Classification.Value },
                { card => card.SetSourceName("a"), card => card.SourceName.Value },
                { card => card.SetProfile("a"), card => card.Profile.Value },
                { card => card.FormattedNames.Add("a"), card => card.FormattedNames.Last().Value },
                { card => card.Emails.Add("a"), card => card.Emails.Single().Value },
                { card => card.Titles.Add("a"), card => card.Titles.Single().Value },
                { card => card.Roles.Add("a"), card => card.Roles.Single().Value },
                { card => card.Notes.Add("a"), card => card.Notes.Single().Value }
            };
        }

        [Theory]
        [MemberData(nameof(SimpleSetterData))]
        public void Setter_TextValue_RoundTripsThroughTheCanonicalForm(Action<IVcard30Card> set, Func<IVcard30Card, string> get)
        {
            // arrange
            IVcard30Card card = VcardCardTestData.NewCard();

            // act
            set(card);

            // assert
            get(card).Should().Be("a");
        }

        public static TheoryData<Func<IVcard30Card, IVcard30PropertyList<Uri>>> UriListData()
        {
            return new TheoryData<Func<IVcard30Card, IVcard30PropertyList<Uri>>>
            {
                card => card.Urls,
                card => card.Sources,
                card => card.InstantMessaging,
                card => card.FreeBusyUrls,
                card => card.CalendarAddressUris,
                card => card.CalendarAccessUris,
                card => card.CalendarUris
            };
        }

        [Theory]
        [MemberData(nameof(UriListData))]
        public void Add_UriWithAComma_RoundTripsThroughTheCanonicalForm(Func<IVcard30Card, IVcard30PropertyList<Uri>> listOf)
        {
            // arrange
            IVcard30Card card = VcardCardTestData.NewCard();

            // act
            listOf(card).Add(ExampleUri);

            // assert
            listOf(card).Single().Value.Should().Be(ExampleUri);
        }

        [Fact]
        public void Remove_Instance_RemovesItFromTheCard()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("EMAIL:a@example.com", "EMAIL:b@example.com"));
            var emails = card.AsVersion30().Emails;

            // act
            var testResult = emails.Remove(emails[0]);

            // assert
            testResult.Should().BeTrue();
            card.Emails.Single().Value.Should().Be("b@example.com");
        }

        [Fact]
        public void Remove_InstanceOfAnotherList_LeavesItAndReturnsFalse()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("EMAIL:a@example.com")).AsVersion30();
            var formattedName = card.FormattedNames.Single();

            // act
            var testResult = card.Emails.Remove(formattedName);

            // assert
            testResult.Should().BeFalse();
            card.FormattedNames.Should().ContainSingle();
        }

        [Fact]
        public void Labels_RemoveInstanceOfAnotherList_LeavesItAndReturnsFalse()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("EMAIL:a@example.com")).AsVersion30();
            var email = card.Emails.Single();

            // act
            var testResult = card.Labels.Remove(email);

            // assert
            testResult.Should().BeFalse();
            card.Emails.Should().ContainSingle();
        }

        [Fact]
        public void Labels_AddBesideALabelOfItsOwn_GivesTheNewLabel()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("LABEL:Nowhere", "ADR;TYPE=home:;;1 First St;;;;"));

            // act
            var testResult = card.AsVersion30().Labels.Add("First", new Vcard30PropertyParameters { Types = { "home" } });

            // assert
            testResult.Value.Should().Be("First");
            testResult.Types.Should().Equal("home");
        }

        public static TheoryData<string> PreferenceVersion30CanNotSayData()
        {
            return new TheoryData<string> { "PHOTO;PREF=1:http://example.com/a.jpg" };
        }

        [Theory]
        [MemberData(nameof(PreferenceVersion30CanNotSayData))]
        public void IsPreferred_TypeWhoseTypeSaysItsFormat_IsFalse(string line)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(line)).AsVersion30();

            // act
            var testResult = card.Photos.Single();

            // assert
            testResult.IsPreferred.Should().BeFalse();
        }

        public static TheoryData<VcardMediaValue> UnnamableTokenData()
        {
            return new TheoryData<VcardMediaValue>
            {
                VcardMediaValue.FromData(Data, "JPEG 2000"),
                VcardMediaValue.FromUri(new Uri("http://example.com/a.jpg"), "JPEG 2000")
            };
        }

        [Theory]
        [MemberData(nameof(UnnamableTokenData))]
        public void Photos_AddTokenThatCanNotNameAMediaType_ThrowsArgumentException(VcardMediaValue value)
        {
            // arrange
            IVcard30Card card = VcardCardTestData.NewCard();

            // act
            Action testAction = () => card.Photos.Add(value);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

#if NETFRAMEWORK
        [Fact]
        public void Photos_AddDataTooLongForAUriOnNetFramework_ThrowsArgumentException()
        {
            // arrange: ref. #619.
            IVcard30Card card = VcardCardTestData.NewCard();
            var value = VcardMediaValue.FromData(new byte[60000]);

            // act
            Action testAction = () => card.Photos.Add(value);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }
#endif

        [Fact]
        public void Clear_List_RemovesEveryInstance()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("EMAIL:a@example.com", "EMAIL:b@example.com"));

            // act
            card.AsVersion30().Emails.Clear();

            // assert
            card.Emails.Should().BeEmpty();
        }

        [Fact]
        public void Agent_NestedCard_GivesAView()
        {
            // act
            var testResult = View("AGENT:BEGIN:VCARD\\nVERSION:3.0\\nFN:Susan Thomas\\nN:Thomas\\;Susan\\;\\;\\;\\nEND:VCARD\\n").Agent;

            // assert
            testResult.FormattedNames.Single().Value.Should().Be("Susan Thomas");
        }

        [Fact]
        public void AgentUri_Uri_GivesIt()
        {
            // act
            var testResult = View("AGENT;VALUE=uri:http://example.com").AgentUri;

            // assert
            testResult.Should().Be(new Uri("http://example.com"));
        }

        [Fact]
        public void SetAgent_Card_ReplacesTheAgent()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card("AGENT;VALUE=uri:http://example.com"));
            var nested = VcardCardTestData.NewCard();

            // act
            card.AsVersion30().SetAgent(nested);

            // assert
            card.AsVersion30().AgentUri.Should().BeNull();
            card.AsVersion30().Agent.FormattedNames.Single().Value.Should().Be(VcardCardTestData.FormattedNameValue);
        }

        [Fact]
        public void SetAgent_Uri_ReplacesTheAgent()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard30TestData.Card(Vcard30TestData.NestedAgentLine(1)));

            // act
            card.AsVersion30().SetAgent(ExampleUri);

            // assert
            card.AsVersion30().Agent.Should().BeNull();
            card.AsVersion30().AgentUri.Should().Be(ExampleUri);
        }

        [Fact]
        public void ExtensionProperties_Card_GivesOnlyWhatNoVersionDefines()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("KIND:individual", "X-A:b")).AsVersion30();

            // act
            var testResult = card.ExtensionProperties;

            // assert
            testResult.Single().Name.Should().Be("X-A");
        }

        [Fact]
        public void AddExtensionProperty_NameOfAVersion_ThrowsArgumentException()
        {
            // arrange
            IVcard30Card card = VcardCardTestData.NewCard();

            // act
            Action testAction = () => card.AddExtensionProperty(VcardPropertyNames.Mailer, "a");

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddExtensionProperty_OtherName_AddsIt()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            var testResult = card.AsVersion30().AddExtensionProperty("X-A", "b");

            // assert
            testResult.Value.Should().Be("b");
            card.GetProperties("X-A").Should().ContainSingle();
        }

        public static TheoryData<string[], string, int> RemovePropertiesData()
        {
            return new TheoryData<string[], string, int>
            {
                { new[] { "EMAIL:a", "EMAIL:b" }, VcardPropertyNames.Email, 2 },
                { new[] { "ADR:;;1 First St;;;;", "LABEL:First", "LABEL;X-A=1:Other" }, VcardPropertyNames.PostalLabel, 2 },
                { new[] { "SORT-STRING:a" }, VcardPropertyNames.SortString, 1 },
                { new[] { "AGENT;VALUE=uri:http://example.com" }, VcardPropertyNames.Agent, 1 }
            };
        }

        [Theory]
        [MemberData(nameof(RemovePropertiesData))]
        public void RemoveProperties_NameInVersion30_RemovesItWhereverTheModelHoldsIt(string[] lines, string name, int expected)
        {
            // arrange
            var card = View(lines);

            // act
            var testResult = card.RemoveProperties(name);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Validate_CardWithWhatVersion30CanNotHold_ReportsIt()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N:Doe;Jane;;;", "KIND:group")).AsVersion30();

            // act
            var testResult = card.Validate();

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(VcardDiagnosticKind.Lossy);
        }

        [Fact]
        public void AsVersion30_ChangeThroughTheView_IsAChangeToTheCard()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            var view = card.AsVersion30();

            // act
            view.SetMailer("a");

            // assert
            card.Mailer.Value.Should().Be("a");
        }

        public static TheoryData<IReadOnlyList<string>> ListValueData()
        {
            return new TheoryData<IReadOnlyList<string>> { new[] { "Jim", "Jimmie" } };
        }

        [Theory]
        [MemberData(nameof(ListValueData))]
        public void Nicknames_Add_RoundTripsThroughTheCanonicalForm(IReadOnlyList<string> value)
        {
            // arrange
            IVcard30Card card = VcardCardTestData.NewCard();

            // act
            card.Nicknames.Add(value);

            // assert
            card.Nicknames.Single().Value.Should().Equal(value);
        }
    }
}
