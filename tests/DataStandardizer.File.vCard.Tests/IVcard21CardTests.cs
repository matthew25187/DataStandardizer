using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of <see cref="IVcard21Card"/>: its members are of vCard 2.1, and each getter and setter goes through the canonical form.
    /// </summary>
    public class IVcard21CardTests
    {
        private const string Base64 = "AQID";
        private static readonly byte[] Data = { 1, 2, 3 };
        private static readonly Uri ExampleUri = new Uri("http://example.com/a,b");

        private static IVcard21Card View(params string[] lines)
        {
            return VcardCardTestData.Read(Vcard21TestData.Card(lines)).AsVersion21();
        }

        public static TheoryData<string> LaterVersionMemberData()
        {
            return new TheoryData<string>
            {
                // vCard 3.0 added these (RFC 2426 §5, RFC 2739, RFC 4770), and vCard 4.0 the rest.
                "Nicknames", "Categories", "Sources", "InstantMessaging", "FreeBusyUrls", "CalendarAddressUris", "CalendarAccessUris", "CalendarUris",
                "ProductId", "SortString", "Classification", "SourceName", "Profile",
                "Kind", "Gender", "Anniversary", "Members", "Related", "Languages", "ClientPidMaps", "Xml", "Properties"
            };
        }

        [Theory]
        [MemberData(nameof(LaterVersionMemberData))]
        public void Interface_MemberVersion21DoesNotDefine_IsNotThere(string name)
        {
            // act
            var testResult = typeof(IVcard21Card).GetMember(name);

            // assert
            testResult.Should().BeEmpty();
        }

        public static TheoryData<string> LaterVersionPropertyMemberData()
        {
            return new TheoryData<string> { "Pref", "Pid", "AltId", "MediaType", "SortAs", "Label", "Encoding", "Charset", "Line" };
        }

        [Theory]
        [MemberData(nameof(LaterVersionPropertyMemberData))]
        public void PropertyInterface_MemberVersion21DoesNotDefine_IsNotThere(string name)
        {
            // act
            var testResult = typeof(IVcard21Property<string>).GetMember(name);

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void Interface_OtherVersions_AreNotInherited()
        {
            // act
            var testResult = typeof(IVcard21Card).GetInterfaces();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void AsVersion21_Card_IsTheSameObject()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            var testResult = card.AsVersion21();

            // assert
            testResult.Should().BeSameAs(card);
        }

        [Fact]
        public void Types_KnownTypes_GivesTheBareTokensWithoutPref()
        {
            // act
            var testResult = View("TEL;WORK;PREF;VOICE;TYPE=X-CUSTOM:+1").Telephones.Single();

            // assert
            testResult.Types.Should().Equal("WORK", "VOICE", "X-CUSTOM");
            testResult.IsPreferred.Should().BeTrue();
        }

        [Fact]
        public void Types_TypeOfALaterVersion_IsHidden()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TEL;TYPE=text,cell:+1")).AsVersion21();

            // act
            var testResult = card.Telephones.Single().Types;

            // assert
            testResult.Should().Equal("CELL");
        }

        [Fact]
        public void IsPreferred_NotTheLowestPref_IsFalse()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TEL;PREF=2:+1", "TEL;PREF=1:+2")).AsVersion21();

            // act
            var testResult = card.Telephones;

            // assert
            testResult[0].IsPreferred.Should().BeFalse();
            testResult[1].IsPreferred.Should().BeTrue();
        }

        [Fact]
        public void Add_Parameters_HoldsThemInCanonicalForm()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            var parameters = new Vcard21PropertyParameters { IsPreferred = true, Group = "g", Types = { "INTERNET" }, Language = "i-klingon" };

            // act
            card.AsVersion21().Emails.Add("a@example.com", parameters);

            // assert
            card.Emails.Single().Pref.Should().Be(1);
            card.Emails.Single().Group.Should().Be("g");
            card.Emails.Single().Types.Should().Equal("internet");
            card.Emails.Single().Line.GetParameter(VcardParameterNames.Language).Values.Should().Equal("i-klingon");
        }

        public static TheoryData<Vcard21PropertyParameters> UnwritableParametersData()
        {
            return new TheoryData<Vcard21PropertyParameters>
            {
                // ref. vCard 2.1 §2.9: ptypeval = knowntype / "X-" word, and an extension parameter is "X-" word "=" word.
                new Vcard21PropertyParameters { Types = { "iphone" } },
                new Vcard21PropertyParameters { Language = "es-419" },
                new Vcard21PropertyParameters { AdditionalParameters = { new VcardContentLineParameter("FOO", "a") } },
                new Vcard21PropertyParameters { AdditionalParameters = { new VcardContentLineParameter("X-FOO", "a b") } }
            };
        }

        [Theory]
        [MemberData(nameof(UnwritableParametersData))]
        public void Add_ParameterVersion21CanNotHold_ThrowsArgumentException(Vcard21PropertyParameters parameters)
        {
            // arrange
            var card = VcardCardTestData.NewCard().AsVersion21();

            // act
            Action testAction = () => card.Emails.Add("a@example.com", parameters);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TimeZones_Offset_GivesItAndHidesText()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TZ;VALUE=utc-offset:-0500", "TZ:America/New_York")).AsVersion21();

            // act
            var testResult = card.TimeZones;

            // assert
            testResult.Single().Value.Should().Be(VcardUtcOffset.Parse("-0500".AsSpan(), VcardValueFormat.Text));
        }

        [Fact]
        public void GeoLocations_Geo_GivesLatitudeAndLongitude()
        {
            // act
            var testResult = View("GEO:37.24,-17.87").GeoLocations.Single().Value;

            // assert
            testResult.ToString(VcardValueFormat.Text, VcardVersion.V2_1).Should().Be("37.24,-17.87");
        }

        [Fact]
        public void Telephones_TelUri_GivesItsNumber()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("TEL;VALUE=uri:tel:+1-555;ext=1")).AsVersion21();

            // act
            var testResult = card.Telephones.Single().Value;

            // assert
            testResult.Should().Be("+1-555");
        }

        [Fact]
        public void Photos_InlineData_GivesATokenAndBytes()
        {
            // act
            var testResult = View("PHOTO;ENCODING=BASE64;TYPE=GIF:" + Base64 + "\r\n").Photos.Single().Value;

            // assert
            testResult.TypeToken.Should().Be("GIF");
            testResult.GetData().Should().Equal(Data);
        }

        [Fact]
        public void Photos_DataOfAFormatWithNoKnownType_GivesNoToken()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("PHOTO:data:image/png;base64\\," + Base64)).AsVersion21();

            // act
            var testResult = card.Photos.Single().Value;

            // assert
            testResult.TypeToken.Should().BeNull();
            testResult.GetData().Should().Equal(Data);
        }

        [Fact]
        public void Photos_AddData_HoldsADataUri()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            card.AsVersion21().Photos.Add(VcardMediaValue.FromData(Data, "GIF"));

            // assert
            card.GetProperties(VcardPropertyNames.Photo).Single().Value.Should().Be("data:image/gif;base64\\," + Base64);
        }

        [Fact]
        public void Photos_AddUri_HoldsAMediaType()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            card.AsVersion21().Photos.Add(VcardMediaValue.FromUri(new Uri("http://example.com/a.jpg"), "JPEG"));

            // assert
            card.Photos.Single().MediaType.ToString().Should().Be("image/jpeg");
        }

        public static TheoryData<VcardMediaValue> UnwritablePhotoData()
        {
            return new TheoryData<VcardMediaValue>
            {
                // ref. vCard 2.1 §2.9: PNG isn't a knowntype, and PICT has no media type; and a picture is never text.
                VcardMediaValue.FromData(Data, "PNG"),
                VcardMediaValue.FromData(Data, "PICT"),
                VcardMediaValue.FromText("a")
            };
        }

        [Theory]
        [MemberData(nameof(UnwritablePhotoData))]
        public void Photos_AddValueVersion21CanNotHold_ThrowsArgumentException(VcardMediaValue value)
        {
            // arrange
            var card = VcardCardTestData.NewCard().AsVersion21();

            // act
            Action testAction = () => card.Photos.Add(value);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Sounds_Text_GivesIt()
        {
            // act
            var testResult = View("SOUND:JON Q PUBLIK").Sounds.Single().Value;

            // assert
            testResult.Text.Should().Be("JON Q PUBLIK");
        }

        [Fact]
        public void Sounds_AddText_HoldsText()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            card.AsVersion21().Sounds.Add(VcardMediaValue.FromText("JON Q PUBLIK"));

            // assert
            Vcard30TestData.ParametersOf(card.GetProperties(VcardPropertyNames.Sound).Single()).Should().Equal("VALUE=text");
        }

        [Fact]
        public void Keys_Text_GivesItsTextAndType()
        {
            // act
            var testResult = View("KEY;PGP:abc").Keys.Single().Value;

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
            card.AsVersion21().Keys.Add(VcardMediaValue.FromText("abc", "X509"));

            // assert
            Vcard30TestData.ParametersOf(card.GetProperties(VcardPropertyNames.Key).Single()).Should().Equal("VALUE=text", "TYPE=x509");
        }

        [Fact]
        public void Labels_LabelOnAnAddress_GivesIt()
        {
            // act
            var testResult = View("ADR;HOME:;;1 Main St;Town;;;", "LABEL;HOME;ENCODING=QUOTED-PRINTABLE:1 Main St=0D=0ATown").Labels.Single();

            // assert
            testResult.Value.Should().Be("1 Main St\nTown");
            testResult.Types.Should().Equal("HOME");
        }

        [Fact]
        public void Birthday_Date_GivesIt()
        {
            // act
            var testResult = View("BDAY:1995-04-15").Birthday.Value;

            // assert
            testResult.ToString(VcardValueFormat.Text, VcardVersion.V4_0).Should().Be("19950415");
        }

        [Fact]
        public void Birthday_DateAndTime_IsHidden()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("BDAY:19950415T120000")).AsVersion21();

            // act
            var testResult = card.Birthday;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void SetBirthday_PartialDate_ThrowsArgumentException()
        {
            // arrange
            var card = VcardCardTestData.NewCard().AsVersion21();
            var date = VcardDate.Parse("--0415".AsSpan(), VcardValueFormat.Text);

            // act
            Action testAction = () => card.SetBirthday(date);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetName_SeveralValuesInAComponent_ThrowsArgumentException()
        {
            // arrange
            var card = VcardCardTestData.NewCard().AsVersion21();
            var name = VcardName.Parse("Doe;Jane,Janet;;;".AsSpan(), VcardValueFormat.Text);

            // act
            Action testAction = () => card.SetName(name);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Name_SeveralValuesInAComponent_IsHidden()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N:Doe;Jane,Janet;;;")).AsVersion21();

            // act
            var testResult = card.Name;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void Agent_NestedCard_GivesAView()
        {
            // act
            var testResult = View("AGENT:", "BEGIN:VCARD", "VERSION:2.1", "N:Friday;Fred", "END:VCARD").Agent;

            // assert
            testResult.Name.Value.FamilyNames.Should().Equal("Friday");
        }

        [Fact]
        public void AgentUri_Url_GivesIt()
        {
            // act
            var testResult = View("AGENT;VALUE=URL:http://example.com/fred.vcf").AgentUri;

            // assert
            testResult.Should().Be(new Uri("http://example.com/fred.vcf"));
        }

        [Fact]
        public void SetAgent_Card_IsWrittenAfterTheAgentLine()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard21TestData.Card());
            var agent = VcardCardTestData.Read(Vcard21TestData.BareCard("N:Friday;Fred"));

            // act
            card.AsVersion21().SetAgent(agent);

            // assert
            Vcard21TestData.Write(card).Should().Be(Vcard21TestData.WrittenCardStart + "AGENT:BEGIN:VCARD\r\nVERSION:2.1\r\nN:Friday;Fred;;;\r\nEND:VCARD\r\n" + Vcard21TestData.WrittenCardEnd);
        }

        [Fact]
        public void SetAgent_Uri_ReplacesTheAgent()
        {
            // arrange
            var card = VcardCardTestData.Read(Vcard21TestData.Card("AGENT;VALUE=URL:http://example.com/a")).AsVersion21();

            // act
            card.SetAgent(new Uri("http://example.com/b"));

            // assert
            card.AgentUri.Should().Be(new Uri("http://example.com/b"));
        }

        [Fact]
        public void ExtensionProperties_Card_GivesOnlyThoseNamedX()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("X-A:1", "FOO:2", "NOTE:3")).AsVersion21();

            // act
            var testResult = card.ExtensionProperties;

            // assert
            testResult.Select(property => property.Name).Should().Equal("X-A");
        }

        public static TheoryData<string> NonExtensionNameData()
        {
            return new TheoryData<string> { "NOTE", "FOO", "X-A.B" };
        }

        [Theory]
        [MemberData(nameof(NonExtensionNameData))]
        public void AddExtensionProperty_NameThatIsNotAnExtensionOfVersion21_ThrowsArgumentException(string name)
        {
            // arrange
            var card = VcardCardTestData.NewCard().AsVersion21();

            // act
            Action testAction = () => card.AddExtensionProperty(name, "a");

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddExtensionProperty_NameOfAnExtension_AddsIt()
        {
            // arrange
            var card = VcardCardTestData.NewCard();

            // act
            card.AsVersion21().AddExtensionProperty("X-A", "b");

            // assert
            card.GetProperties("X-A").Single().Value.Should().Be("b");
        }

        public static TheoryData<string[], string, int> RemovePropertiesData()
        {
            return new TheoryData<string[], string, int>
            {
                { new[] { "EMAIL:a", "EMAIL:b" }, VcardPropertyNames.Email, 2 },
                { new[] { "ADR:;;1 First St;;;;", "LABEL:First", "LABEL;X-A=1:Other" }, VcardPropertyNames.PostalLabel, 2 },
                { new[] { "AGENT;VALUE=URL:http://example.com" }, VcardPropertyNames.Agent, 1 }
            };
        }

        [Theory]
        [MemberData(nameof(RemovePropertiesData))]
        public void RemoveProperties_NameInVersion21_RemovesItWhereverTheModelHoldsIt(string[] lines, string name, int expected)
        {
            // arrange
            var card = View(lines);

            // act
            var testResult = card.RemoveProperties(name);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Validate_CardWithWhatVersion21CanNotHold_ReportsIt()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("N:Doe;Jane;;;", "NICKNAME:Jim")).AsVersion21();

            // act
            var testResult = card.Validate();

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(VcardDiagnosticKind.Lossy);
        }

        public static TheoryData<Action<IVcard21Card>, Func<IVcard21Card, string>> SimpleSetterData()
        {
            return new TheoryData<Action<IVcard21Card>, Func<IVcard21Card, string>>
            {
                { card => card.SetMailer("a"), card => card.Mailer.Value },
                { card => card.SetUid("a"), card => card.Uid.Value },
                { card => card.FormattedNames.Add("a"), card => card.FormattedNames.Last().Value },
                { card => card.Emails.Add("a"), card => card.Emails.Single().Value },
                { card => card.Titles.Add("a"), card => card.Titles.Single().Value },
                { card => card.Roles.Add("a"), card => card.Roles.Single().Value },
                { card => card.Notes.Add("a"), card => card.Notes.Single().Value },
                { card => card.Telephones.Add("a"), card => card.Telephones.Single().Value },
                { card => card.Labels.Add("a"), card => card.Labels.Single().Value }
            };
        }

        [Theory]
        [MemberData(nameof(SimpleSetterData))]
        public void Setter_TextValue_RoundTripsThroughTheCanonicalForm(Action<IVcard21Card> set, Func<IVcard21Card, string> get)
        {
            // arrange
            IVcard21Card card = VcardCardTestData.NewCard();

            // act
            set(card);

            // assert
            get(card).Should().Be("a");
        }

        [Fact]
        public void Urls_AddUriWithAComma_RoundTripsThroughTheCanonicalForm()
        {
            // arrange
            IVcard21Card card = VcardCardTestData.NewCard();

            // act
            card.Urls.Add(ExampleUri);

            // assert
            card.Urls.Single().Value.Should().Be(ExampleUri);
        }

        [Fact]
        public void AsVersion21_CardSetThroughTheView_WritesAndReadsBackTheSame()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            var view = card.AsVersion21();
            view.SetName(VcardName.Parse("Doe;Jane;;;".AsSpan(), VcardValueFormat.Text));
            view.Telephones.Add("+1-555", new Vcard21PropertyParameters { Types = { "CELL" }, IsPreferred = true });
            view.Notes.Add("Caf\x00E9\nline two");
            view.Photos.Add(VcardMediaValue.FromData(Data, "JPEG"));

            // act
            var testResult = VcardCardTestData.Read(Vcard21TestData.Write(card)).AsVersion21();

            // assert
            testResult.Name.Value.FamilyNames.Should().Equal("Doe");
            testResult.Telephones.Single().Types.Should().Equal("CELL");
            testResult.Telephones.Single().IsPreferred.Should().BeTrue();
            testResult.Notes.Single().Value.Should().Be("Caf\x00E9\nline two");
            testResult.Photos.Single().Value.GetData().Should().Equal(Data);
        }
    }
}
