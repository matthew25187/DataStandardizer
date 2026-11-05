using System;
using System.Collections.Generic;
using System.Linq;
using DataStandardizer.Geography;
using DataStandardizer.LanguageTag;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    public class IVcard40CardTests
    {
        private static readonly Uri ExampleUri = new Uri("http://example.com/");

        private static Bcp47LanguageTag EnglishTag => Bcp47LanguageTag.Create("en");

        private static VcardName NameValue => VcardName.Parse("Doe;Jane;;;".AsSpan(), VcardValueFormat.Text);

        private static VcardAddress AddressValue => VcardAddress.Parse(";;1 Main St;Town;;;".AsSpan(), VcardValueFormat.Text);

        private static VcardOrganization OrganizationValue => VcardOrganization.Parse("Example".AsSpan(), VcardValueFormat.Text);

        [Fact]
        public void Members_OfTheInterface_DoNotIncludeTheRawProperties()
        {
            // arrange
            var type = typeof(IVcard40Card);

            // act
            var testResult = type.GetProperty(nameof(VcardCard.Properties));

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void Members_OfThePropertyInterface_DoNotIncludeTheRawLine()
        {
            // arrange
            var type = typeof(IVcard40Property<string>);

            // act
            var testResult = type.GetProperty(nameof(VcardProperty<string>.Line));

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void AsVersion40_AnyCard_IsTheSameObject()
        {
            // arrange
            var card = new VcardCard();

            // act
            var testResult = card.AsVersion40();

            // assert
            testResult.Should().BeSameAs(card);
        }

        public static TheoryData<string, Action<IVcard40Card>, string> AdaptedAddData()
        {
            return new TheoryData<string, Action<IVcard40Card>, string>
            {
                { "SOURCE", card => card.Sources.Add(ExampleUri), "0/0 SOURCE:http://example.com/" },
                { "XML", card => card.Xml.Add("<a xmlns=\"urn:b\"/>"), "0/0 XML:<a xmlns=\"urn:b\"/>" },
                { "FN", card => card.FormattedNames.Add("a"), "0/0 FN:a" },
                { "NICKNAME", card => card.Nicknames.Add(new[] { "a", "b" }), "0/0 NICKNAME:a,b" },
                { "PHOTO", card => card.Photos.Add(ExampleUri), "0/0 PHOTO:http://example.com/" },
                { "ADR", card => card.Addresses.Add(AddressValue), "0/0 ADR:;;1 Main St;Town;;;" },
                { "TEL", card => card.Telephones.Add(new VcardTypedValue("1")), "0/0 TEL:1" },
                { "TEL", card => card.Telephones.Add(new VcardTypedValue(new Uri("tel:+1"))), "0/0 TEL;VALUE=uri:tel:+1" },
                { "EMAIL", card => card.Emails.Add("a@example.com"), "0/0 EMAIL:a@example.com" },
                { "IMPP", card => card.InstantMessaging.Add(new Uri("xmpp:a@example.com")), "0/0 IMPP:xmpp:a@example.com" },
                { "LANG", card => card.Languages.Add(EnglishTag), "0/0 LANG:en" },
                { "TZ", card => card.TimeZones.Add(new VcardTypedValue("a")), "0/0 TZ:a" },
                { "TZ", card => card.TimeZones.Add(new VcardTypedValue(VcardUtcOffset.Parse("-0500".AsSpan(), VcardValueFormat.Text))), "0/0 TZ;VALUE=utc-offset:-0500" },
                { "GEO", card => card.GeoLocations.Add(new Uri("geo:1,2")), "0/0 GEO:geo:1\\,2" },
                { "TITLE", card => card.Titles.Add("a"), "0/0 TITLE:a" },
                { "ROLE", card => card.Roles.Add("a"), "0/0 ROLE:a" },
                { "LOGO", card => card.Logos.Add(ExampleUri), "0/0 LOGO:http://example.com/" },
                { "ORG", card => card.Organizations.Add(OrganizationValue), "0/0 ORG:Example" },
                { "MEMBER", card => card.Members.Add(ExampleUri), "0/0 MEMBER:http://example.com/" },
                { "RELATED", card => card.Related.Add(new VcardTypedValue(ExampleUri)), "0/0 RELATED:http://example.com/" },
                { "RELATED", card => card.Related.Add(new VcardTypedValue("a")), "0/0 RELATED;VALUE=text:a" },
                { "CATEGORIES", card => card.Categories.Add(new[] { "a", "b" }), "0/0 CATEGORIES:a,b" },
                { "NOTE", card => card.Notes.Add("a"), "0/0 NOTE:a" },
                { "SOUND", card => card.Sounds.Add(ExampleUri), "0/0 SOUND:http://example.com/" },
                { "CLIENTPIDMAP", card => card.ClientPidMaps.Add(new VcardClientPidMap(1, new Uri("urn:a"))), "0/0 CLIENTPIDMAP:1;urn:a" },
                { "URL", card => card.Urls.Add(ExampleUri), "0/0 URL:http://example.com/" },
                { "KEY", card => card.Keys.Add(new VcardTypedValue(ExampleUri)), "0/0 KEY:http://example.com/" },
                { "KEY", card => card.Keys.Add(new VcardTypedValue("a")), "0/0 KEY;VALUE=text:a" },
                { "FBURL", card => card.FreeBusyUrls.Add(ExampleUri), "0/0 FBURL:http://example.com/" },
                { "CALADRURI", card => card.CalendarAddressUris.Add(ExampleUri), "0/0 CALADRURI:http://example.com/" },
                { "CALURI", card => card.CalendarUris.Add(ExampleUri), "0/0 CALURI:http://example.com/" },
                { "KIND", card => card.SetKind(VcardKind.Group), "0/0 KIND:group" },
                { "KIND", card => card.SetKind("x-robot"), "0/0 KIND:x-robot" },
                { "N", card => card.SetName(NameValue), "0/0 N:Doe;Jane;;;" },
                { "N", card => card.SetName(NameValue, new VcardPropertyParameters { Language = EnglishTag }), "0/0 N;LANGUAGE=en:Doe;Jane;;;" },
                { "BDAY", card => card.SetBirthday(new VcardTypedValue("a")), "0/0 BDAY;VALUE=text:a" },
                { "BDAY", card => card.SetBirthday(new VcardTypedValue("a"), new VcardPropertyParameters { AltId = "b" }), "0/0 BDAY;VALUE=text;ALTID=b:a" },
                { "ANNIVERSARY", card => card.SetAnniversary(new VcardTypedValue("a")), "0/0 ANNIVERSARY;VALUE=text:a" },
                { "ANNIVERSARY", card => card.SetAnniversary(new VcardTypedValue("a"), new VcardPropertyParameters { AltId = "b" }), "0/0 ANNIVERSARY;VALUE=text;ALTID=b:a" },
                { "GENDER", card => card.SetGender(new VcardGender(VcardSex.Female, null)), "0/0 GENDER:F" },
                { "GENDER", card => card.SetGender(new VcardGender(VcardSex.Female, null), new VcardPropertyParameters { Group = "b" }), "0/0 b.GENDER:F" },
                { "PRODID", card => card.SetProductId("a"), "0/0 PRODID:a" },
                { "REV", card => card.SetRevision(VcardTimestamp.Parse("19951031T222710Z".AsSpan(), VcardValueFormat.Text)), "0/0 REV:19951031T222710Z" },
                { "UID", card => card.SetUid(new VcardTypedValue(new Uri("urn:a"))), "0/0 UID:urn:a" },
                { "UID", card => card.SetUid(new VcardTypedValue("a")), "0/0 UID;VALUE=text:a" },
                { "BIRTHPLACE", card => card.SetBirthPlace(new VcardTypedValue("a")), "0/0 BIRTHPLACE:a" },
                { "BIRTHPLACE", card => card.SetBirthPlace(new VcardTypedValue("a"), new VcardPropertyParameters { Group = "b" }), "0/0 b.BIRTHPLACE:a" },
                { "DEATHPLACE", card => card.SetDeathPlace(new VcardTypedValue(new Uri("urn:a"))), "0/0 DEATHPLACE;VALUE=uri:urn:a" },
                { "DEATHPLACE", card => card.SetDeathPlace(new VcardTypedValue("a"), new VcardPropertyParameters { Group = "b" }), "0/0 b.DEATHPLACE:a" },
                { "DEATHDATE", card => card.SetDeathDate(new VcardTypedValue("a")), "0/0 DEATHDATE;VALUE=text:a" },
                { "DEATHDATE", card => card.SetDeathDate(new VcardTypedValue("a"), new VcardPropertyParameters { Group = "b" }), "0/0 b.DEATHDATE;VALUE=text:a" },
                { "EXPERTISE", card => card.Expertise.Add("a"), "0/0 EXPERTISE:a" },
                { "HOBBY", card => card.Hobbies.Add("a"), "0/0 HOBBY:a" },
                { "INTEREST", card => card.Interests.Add("a"), "0/0 INTEREST:a" },
                { "ORG-DIRECTORY", card => card.OrganizationDirectories.Add(ExampleUri), "0/0 ORG-DIRECTORY:http://example.com/" },
                { "CONTACT-URI", card => card.ContactUris.Add(ExampleUri), "0/0 CONTACT-URI:http://example.com/" },
                { "CREATED", card => card.SetCreated(VcardTimestamp.Parse("19951031T222710Z".AsSpan(), VcardValueFormat.Text)), "0/0 CREATED:19951031T222710Z" },
                { "LANGUAGE", card => card.SetDefaultLanguage(EnglishTag), "0/0 LANGUAGE:en" },
                { "GRAMGENDER", card => card.GrammaticalGenders.Add("common"), "0/0 GRAMGENDER:common" },
                { "PRONOUNS", card => card.Pronouns.Add("a"), "0/0 PRONOUNS:a" },
                { "SOCIALPROFILE", card => card.SocialProfiles.Add(new VcardTypedValue(ExampleUri)), "0/0 SOCIALPROFILE:http://example.com/" },
                { "X-A", card => card.AddExtensionProperty("X-A", "a"), "0/0 X-A:a" },
                { "X-A", card => card.AddExtensionProperty("X-A", "a", new VcardPropertyParameters { Group = "b" }), "0/0 b.X-A:a" }
            };
        }

        [Theory]
        [MemberData(nameof(AdaptedAddData))]
        public void SetterOrAdd_ThroughTheInterface_WritesTheCanonicalLine(string name, Action<IVcard40Card> change, string expected)
        {
            // arrange
            var card = new VcardCard();
            var version40 = card.AsVersion40();

            // act
            change(version40);

            // assert
            var line = card.Properties.Single();
            line.Name.Should().Be(name);
            LineDescription.Of(line).Should().Be(expected);
        }

        public static TheoryData<string, Func<IVcard40Card, object>, object> AdaptedGetData()
        {
            return new TheoryData<string, Func<IVcard40Card, object>, object>
            {
                { "SOURCE:http://example.com/", card => card.Sources[0].Value.OriginalString, "http://example.com/" },
                { "XML:<a xmlns=\"urn:b\"/>", card => card.Xml[0].Value, "<a xmlns=\"urn:b\"/>" },
                { "NICKNAME:a,b", card => card.Nicknames[0].Value[1], "b" },
                { "PHOTO:http://example.com/", card => card.Photos[0].Value.OriginalString, "http://example.com/" },
                { "ADR:;;1 Main St;Town;;;", card => card.Addresses[0].Value.ToString(VcardValueFormat.Text), ";;1 Main St;Town;;;" },
                { "TEL;VALUE=uri:tel:+1", card => card.Telephones[0].Value.Uri.OriginalString, "tel:+1" },
                { "EMAIL:a@example.com", card => card.Emails[0].Value, "a@example.com" },
                { "IMPP:xmpp:a@example.com", card => card.InstantMessaging[0].Value.OriginalString, "xmpp:a@example.com" },
                { "LANG:en", card => card.Languages[0].Value.ToString(), "en" },
                { "TZ;VALUE=utc-offset:-0500", card => card.TimeZones[0].Value.UtcOffset.ToString(VcardValueFormat.Text), "-0500" },
                { "GEO:geo:1\\,2", card => card.GeoLocations[0].Value.OriginalString, "geo:1,2" },
                { "TITLE:a", card => card.Titles[0].Value, "a" },
                { "ROLE:a", card => card.Roles[0].Value, "a" },
                { "LOGO:http://example.com/", card => card.Logos[0].Value.OriginalString, "http://example.com/" },
                { "ORG:a;b", card => card.Organizations[0].Value.ToString(VcardValueFormat.Text), "a;b" },
                { "RELATED;VALUE=text:a", card => card.Related[0].Value.Text, "a" },
                { "CATEGORIES:a,b", card => card.Categories[0].Value[0], "a" },
                { "NOTE:a", card => card.Notes[0].Value, "a" },
                { "SOUND:http://example.com/", card => card.Sounds[0].Value.OriginalString, "http://example.com/" },
                { "CLIENTPIDMAP:1;urn:a", card => card.ClientPidMaps[0].Value.SourceId, 1L },
                { "URL:http://example.com/", card => card.Urls[0].Value.OriginalString, "http://example.com/" },
                { "KEY;VALUE=text:a", card => card.Keys[0].Value.Text, "a" },
                { "FBURL:http://example.com/", card => card.FreeBusyUrls[0].Value.OriginalString, "http://example.com/" },
                { "CALADRURI:http://example.com/", card => card.CalendarAddressUris[0].Value.OriginalString, "http://example.com/" },
                { "CALURI:http://example.com/", card => card.CalendarUris[0].Value.OriginalString, "http://example.com/" },
                { "KIND:group", card => card.Kind, VcardKind.Group },
                { "KIND:x-robot", card => card.KindText, "x-robot" },
                { "N:Doe;Jane;;;", card => card.Name.Value.ToString(VcardValueFormat.Text), "Doe;Jane;;;" },
                { "BDAY;VALUE=text:a", card => card.Birthday.Value.Text, "a" },
                { "ANNIVERSARY;VALUE=text:a", card => card.Anniversary.Value.Text, "a" },
                { "GENDER:F", card => card.Gender.Value.Sex, VcardSex.Female },
                { "PRODID:a", card => card.ProductId.Value, "a" },
                { "REV:19951031T222710Z", card => card.Revision.Value.ToString(VcardValueFormat.Text), "19951031T222710Z" },
                { "UID;VALUE=text:a", card => card.Uid.Value.Text, "a" },
                { "BIRTHPLACE:a", card => card.BirthPlace.Value.Text, "a" },
                { "DEATHPLACE:a", card => card.DeathPlace.Value.Text, "a" },
                { "DEATHDATE;VALUE=text:a", card => card.DeathDate.Value.Text, "a" },
                { "EXPERTISE;LEVEL=expert:a", card => card.Expertise[0].Level, VcardLevel.Expert },
                { "HOBBY;INDEX=2:a", card => card.Hobbies[0].Index, 2L },
                { "INTEREST:a", card => card.Interests[0].Value, "a" },
                { "ORG-DIRECTORY:http://example.com/", card => card.OrganizationDirectories[0].Value.OriginalString, "http://example.com/" },
                { "CONTACT-URI:http://example.com/", card => card.ContactUris[0].Value.OriginalString, "http://example.com/" },
                { "ADR;CC=US:;;;;;;", card => card.Addresses[0].CountryCode, Iso3166Part1Alpha2Country.US },
                { "KIND:application", card => card.Kind, VcardKind.Application },
                { "KIND:device", card => card.Kind, VcardKind.Device },
                { "CREATED:19951031T222710Z", card => card.Created.Value.ToString(VcardValueFormat.Text), "19951031T222710Z" },
                { "LANGUAGE:en", card => card.DefaultLanguage.Value.ToString(), "en" },
                { "GRAMGENDER:common", card => card.GrammaticalGenders[0].Value, "common" },
                { "PRONOUNS:a", card => card.Pronouns[0].Value, "a" },
                { "SOCIALPROFILE;VALUE=text;SERVICE-TYPE=a:b", card => card.SocialProfiles[0].ServiceType, "a" },
                { "SOCIALPROFILE;USERNAME=a:urn:b", card => card.SocialProfiles[0].Username, "a" },
                { "NOTE;AUTHOR=\"urn:a\":b", card => card.Notes[0].Author.OriginalString, "urn:a" },
                { "NOTE;AUTHOR-NAME=a:b", card => card.Notes[0].AuthorName, "a" },
                { "NOTE;CREATED=19951031T222710Z:b", card => card.Notes[0].Created.Value.ToString(VcardValueFormat.Text), "19951031T222710Z" },
                { "NOTE;DERIVED=true:b", card => card.Notes[0].IsDerived, true },
                { "NOTE;PROP-ID=a:b", card => card.Notes[0].PropertyId, "a" },
                { "NOTE;SCRIPT=Latn:b", card => card.Notes[0].Script, "Latn" },
                { "ADR;ALTID=1:;;a;;;;\r\nADR;ALTID=1;PHONETIC=ipa:;;b;;;;", card => card.Addresses[1].Phonetic, VcardPhoneticSystem.Ipa },
                { "X-A:a", card => card.ExtensionProperties[0].Value, "a" }
            };
        }

        [Theory]
        [MemberData(nameof(AdaptedGetData))]
        public void Getter_ThroughTheInterface_GivesTheCommonValue(string property, Func<IVcard40Card, object> member, object expected)
        {
            // arrange
            var options = new VcardOptions { AllowCardinalityViolations = true };
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property), options);
            var version40 = card.AsVersion40();

            // act
            var testResult = member(version40);

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<Func<IVcard40Property<string>, object>, object> PropertyInterfaceData()
        {
            return new TheoryData<Func<IVcard40Property<string>, object>, object>
            {
                { property => property.Name, "TITLE" },
                { property => property.Value, "a" },
                { property => property.Group, "item" },
                { property => property.AltId, "b" },
                { property => property.Pref, 3 },
                { property => property.Language.Value.ToString(), "en" },
                { property => property.Types.Single(), "work" },
                { property => property.Pid.Single(), new VcardPid(2) },
                { property => property.ExtensionParameters.Single().Name, "X-A" },
                { property => property.MediaType, null },
                { property => property.CalendarScale, null },
                { property => property.Label, null },
                { property => property.GeoLocation, null },
                { property => property.TimeZone, null },
                { property => property.SortAs.Count, 0 }
            };
        }

        [Theory]
        [MemberData(nameof(PropertyInterfaceData))]
        public void Member_OfThePropertyInterface_GivesTheCommonValue(Func<IVcard40Property<string>, object> member, object expected)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("item.TITLE;ALTID=b;PREF=3;LANGUAGE=en;TYPE=work;PID=2;X-A=c:a"));
            var property = card.AsVersion40().Titles[0];

            // act
            var testResult = member(property);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Add_ThroughTheInterface_ShowsInTheCommonMember()
        {
            // arrange
            var card = new VcardCard();
            var parameters = new VcardPropertyParameters { Pref = 2 };

            // act
            var testResult = card.AsVersion40().Emails.Add("a@example.com", parameters);

            // assert
            testResult.Pref.Should().Be(2);
            card.Emails[0].Value.Should().Be("a@example.com");
        }

        [Fact]
        public void Remove_ThroughTheInterface_TakesItFromTheCard()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE:a"));
            var notes = card.AsVersion40().Notes;

            // act
            var testResult = notes.Remove(notes[0]);

            // assert
            testResult.Should().BeTrue();
            card.Notes.Count.Should().Be(0);
        }

        [Fact]
        public void Remove_ThroughTheInterfaceWithAnotherImplementation_ReturnsFalse()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE:a"));
            var notes = card.AsVersion40().Notes;

            // act
            var testResult = notes.Remove(new OtherProperty());

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Clear_ThroughTheInterface_EmptiesTheList()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE:a", "NOTE:b"));
            var notes = card.AsVersion40().Notes;

            // act
            notes.Clear();

            // assert
            notes.Count.Should().Be(0);
        }

        [Fact]
        public void GetEnumerator_ThroughTheInterface_GivesEachInstance()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE:a", "NOTE:b"));

            // act
            var testResult = card.AsVersion40().Notes.Select(note => note.Value).ToList();

            // assert
            testResult.Should().Equal("a", "b");
        }

        [Fact]
        public void GetPreferred_ThroughTheInterface_GivesTheLowest()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE;PREF=2:a", "NOTE;PREF=1:b"));

            // act
            var testResult = card.AsVersion40().Notes.GetPreferred();

            // assert
            testResult.Value.Should().Be("b");
        }

        [Fact]
        public void GetAlternatives_ThroughTheInterface_GroupsThem()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE;ALTID=1:a", "NOTE;ALTID=1:b"));

            // act
            var testResult = card.AsVersion40().Notes.GetAlternatives();

            // assert
            testResult.Single().Select(note => note.Value).Should().Equal("a", "b");
        }

        [Fact]
        public void RemoveProperties_ThroughTheInterface_GivesTheCount()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("NOTE:a", "NOTE:b"));

            // act
            var testResult = card.AsVersion40().RemoveProperties(VcardPropertyNames.Note);

            // assert
            testResult.Should().Be(2);
        }

        [Fact]
        public void Validate_ThroughTheInterface_GivesTheProblems()
        {
            // arrange
            var card = new VcardCard();

            // act
            var testResult = card.AsVersion40().Validate();

            // assert
            VcardCardTestData.KindsOf(testResult).Should().Equal(VcardDiagnosticKind.MissingRequiredProperty);
        }

        [Fact]
        public void WriteCard_ChangedThroughTheInterface_ReadsBackTheSame()
        {
            // arrange
            var card = new VcardCard();
            var version40 = card.AsVersion40();
            version40.FormattedNames.Add("Jane Doe, Esq.");
            version40.SetName(NameValue);
            version40.Telephones.Add(new VcardTypedValue(new Uri("tel:+1-555-555-5555")), new VcardPropertyParameters { Pref = 1 });

            // act
            var testResult = VcardCardTestData.Read(VcardCardTestData.Write(card)).AsVersion40();

            // assert
            testResult.FormattedNames[0].Value.Should().Be("Jane Doe, Esq.");
            testResult.Name.Value.Should().Be(NameValue);
            testResult.Telephones[0].Value.Uri.OriginalString.Should().Be("tel:+1-555-555-5555");
            testResult.Telephones[0].Pref.Should().Be(1);
        }

        /// <summary>
        /// An implementation of the interface that isn't from a card.
        /// </summary>
        private sealed class OtherProperty : IVcard40Property<string>
        {
            public string Name => "NOTE";

            public string Value => "a";

            public Iso3166Part1Alpha2Country? CountryCode => null;

            public VcardTimestamp? Created => null;

            public bool IsDerived => false;

            public VcardPhoneticSystem? Phonetic => null;

            public Uri Author => null;

            public string AuthorName => null;

            public string PropertyId => null;

            public string Script => null;

            public string ServiceType => null;

            public string Username => null;

            public IReadOnlyList<VcardContentLineParameter> ExtensionParameters => new VcardContentLineParameter[0];

            public long? Index => null;

            public VcardLevel? Level => null;

            public Bcp47LanguageTag? Language => null;

            public DataStandardizer.Communication.Mime.MimeMediaType? MediaType => null;

            public IReadOnlyList<VcardPid> Pid => new VcardPid[0];

            public int? Pref => null;

            public IReadOnlyList<string> SortAs => new string[0];

            public IReadOnlyList<string> Types => new string[0];

            public string AltId => null;

            public string CalendarScale => null;

            public Uri GeoLocation => null;

            public string Group => null;

            public string Label => null;

            public string TimeZone => null;
        }
    }
}
