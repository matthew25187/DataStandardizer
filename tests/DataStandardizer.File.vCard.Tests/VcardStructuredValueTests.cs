using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of <see cref="VcardName"/>, <see cref="VcardAddress"/> and <see cref="VcardOrganization"/>, and the view of their components.
    /// </summary>
    public class VcardStructuredValueTests
    {
        private static readonly string[] None = new string[0];

        [Fact]
        public void TryParse_Rfc6350Name_GivesItsComponents()
        {
            // act
            VcardName result;
            var testResult = VcardName.TryParse(VcardValueExamples.NameLists.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.FamilyNames.Should().Equal("Stevenson");
            result.GivenNames.Should().Equal("John");
            result.AdditionalNames.Should().Equal("Philip", "Paul");
            result.HonorificPrefixes.Should().Equal("Dr.");
            result.HonorificSuffixes.Should().Equal("Jr.", "M.D.", "A.C.P.");
            result.SecondarySurnames.Should().BeEmpty();
            result.Generations.Should().BeEmpty();
        }

        [Fact]
        public void TryParse_Rfc9554Name_GivesItsNewComponents()
        {
            // act
            var testResult = VcardName.Parse(VcardValueExamples.NameRfc9554.AsSpan(), VcardValueFormat.Text);

            // assert
            testResult.SecondarySurnames.Should().BeEmpty();
            testResult.Generations.Should().Equal("Jr.");
        }

        [Theory]
        [InlineData(VcardValueExamples.NameSimple)]
        [InlineData(VcardValueExamples.NameLists)]
        [InlineData(VcardValueExamples.NameRfc9554)]
        [InlineData(";;;;")]
        [InlineData("a\\,b;c\\;d;e\\\\f;g\\nh;i")]
        public void ToString_ParsedVersion40Name_WritesTheSameText(string value)
        {
            // arrange
            var name = VcardName.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = name.ToString(VcardValueFormat.Text);

            // assert
            testResult.Should().Be(value);
        }

        [Fact]
        public void ToString_SevenEmptyTrailingComponents_WritesFive()
        {
            // arrange
            var name = VcardName.Parse("a;b;c;d;e;;".AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = name.ToString();

            // assert
            testResult.Should().Be("a;b;c;d;e");
        }

        [Fact]
        public void TryParse_EscapedText_RemovesTheEscaping()
        {
            // act
            var testResult = VcardName.Parse("a\\,b;c\\;d;e\\\\f;g\\nh;i\\Nj".AsSpan(), VcardValueFormat.Text);

            // assert
            testResult.FamilyNames.Should().Equal("a,b");
            testResult.GivenNames.Should().Equal("c;d");
            testResult.AdditionalNames.Should().Equal("e\\f");
            testResult.HonorificPrefixes.Should().Equal("g\nh");
            testResult.HonorificSuffixes.Should().Equal("i\nj");
        }

        [Fact]
        public void TryParse_EmptyItemsOfAList_AreKept()
        {
            // act
            var testResult = VcardName.Parse("a,,b;,;;;".AsSpan(), VcardValueFormat.Text);

            // assert
            testResult.FamilyNames.Should().Equal("a", "", "b");
            testResult.GivenNames.Should().Equal("", "");
            testResult.ToString().Should().Be("a,,b;,;;;");
        }

        [Theory]
        [InlineData("")]
        [InlineData("a;b;c;d")]
        [InlineData("a;b;c;d;e;f")]
        [InlineData("a;b;c;d;e;f;g;h")]
        [InlineData("a\\:b;;;;")]
        [InlineData("a\\;;;;")]
        [InlineData("a;;;;\\")]
        [InlineData("a\x01;;;;")]
        public void TryParse_InvalidVersion40Name_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardName.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("Public", VcardVersion.V3_0)]
        [InlineData("Public;John", VcardVersion.V3_0)]
        [InlineData(VcardValueExamples.NameLists, VcardVersion.V3_0)]
        [InlineData(VcardValueExamples.Version21Name, VcardVersion.V2_1)]
        [InlineData(VcardValueExamples.Version21NameTwoComponents, VcardVersion.V2_1)]
        public void TryParse_LegacyVersion_AcceptsWhatItsGrammarAllows(string value, VcardVersion version)
        {
            // act
            var testResult = VcardName.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [InlineData("a;b;c;d;e;f", VcardVersion.V3_0)]
        [InlineData(VcardValueExamples.NameRfc9554, VcardVersion.V3_0)]
        [InlineData("a\\:b", VcardVersion.V3_0)]
        [InlineData("a;b;c;d;e;f", VcardVersion.V2_1)]
        [InlineData("a\nb", VcardVersion.V2_1)]
        public void TryParse_LegacyVersion_RejectsWhatItsGrammarDoesNotAllow(string value, VcardVersion version)
        {
            // act
            var testResult = VcardName.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParse_Version21Name_TakesACommaAsText()
        {
            // act
            var testResult = VcardName.Parse(VcardValueExamples.Version21Name.AsSpan(), VcardValueFormat.Text, VcardVersion.V2_1);

            // assert
            testResult.HonorificSuffixes.Should().Equal("III, Esq.");
            testResult.ToString(VcardValueFormat.Text, VcardVersion.V2_1).Should().Be(VcardValueExamples.Version21Name);
        }

        [Fact]
        public void TryParse_Version21Escapes_GivesASemicolonAndALineBreakAndKeepsOtherBackslashes()
        {
            // arrange
            const string value = "a\\;b;c\\\r\nd;e\\f";

            // act
            var testResult = VcardName.Parse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V2_1);

            // assert
            testResult.FamilyNames.Should().Equal("a;b");
            testResult.GivenNames.Should().Equal("c\nd");
            testResult.AdditionalNames.Should().Equal("e\\f");
        }

        [Fact]
        public void ToString_LineBreakInVersion21_WritesABackslashAndALineBreak()
        {
            // arrange
            var name = new VcardName(new[] { "a\nb" }, None, None, None, None);

            // act
            var testResult = name.ToString(VcardValueFormat.Text, VcardVersion.V2_1);

            // assert
            testResult.Should().Be("a\\\r\nb;;;;");
        }

        [Theory]
        [InlineData(VcardVersion.V4_0, true)]
        [InlineData(VcardVersion.V3_0, false)]
        [InlineData(VcardVersion.V2_1, false)]
        public void IsDefinedIn_NameWithRfc9554Component_IsOnlyInVersion40(VcardVersion version, bool expected)
        {
            // arrange
            var name = VcardName.Parse(VcardValueExamples.NameRfc9554.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = name.IsDefinedIn(version);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardVersion.V4_0, true)]
        [InlineData(VcardVersion.V3_0, true)]
        [InlineData(VcardVersion.V2_1, false)]
        public void IsDefinedIn_NameWithSeveralItems_IsNotInVersion21(VcardVersion version, bool expected)
        {
            // arrange
            var name = VcardName.Parse(VcardValueExamples.NameLists.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = name.IsDefinedIn(version);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void IsDefinedIn_ItemEndingInABackslashInVersion21_IsFalse()
        {
            // arrange
            var name = new VcardName(new[] { "a\\" }, None, None, None, None);

            // act
            var testResult = name.IsDefinedIn(VcardVersion.V2_1);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryFormat_NameThatTheVersionCannotWrite_Throws()
        {
            // arrange
            var name = VcardName.Parse(VcardValueExamples.NameRfc9554.AsSpan(), VcardValueFormat.Text);
            var destination = new char[128];

            // act
            Action testAction = () => name.TryFormat(destination, out _, VcardValueFormat.Text, VcardVersion.V3_0);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TryFormat_DestinationTooShort_ReturnsFalseAndWritesNothing()
        {
            // arrange
            var name = VcardName.Parse(VcardValueExamples.NameSimple.AsSpan(), VcardValueFormat.Text);
            var destination = new char[4];

            // act
            int charsWritten;
            var testResult = name.TryFormat(destination, out charsWritten, VcardValueFormat.Text);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void Constructor_GivenComponents_WritesThem()
        {
            // arrange
            const string expected = "Stevenson;John;Philip,Paul;Dr.;Jr.,M.D.,A.C.P.;;Jr.";

            // act
            var testResult = new VcardName(new[] { "Stevenson" }, new[] { "John" }, new[] { "Philip", "Paul" }, new[] { "Dr." }, new[] { "Jr.", "M.D.", "A.C.P." }, None, new[] { "Jr." });

            // assert
            testResult.ToString().Should().Be(expected);
        }

        [Fact]
        public void Constructor_ComponentOfOneEmptyItem_EqualsAnEmptyComponent()
        {
            // arrange
            var emptyItem = new VcardName(new[] { "" }, None, None, None, None);
            var noItems = new VcardName(None, None, None, None, None);

            // act
            var testResult = emptyItem == noItems;

            // assert
            testResult.Should().BeTrue();
            emptyItem.GetHashCode().Should().Be(noItems.GetHashCode());
        }

        [Fact]
        public void Components_ParsedName_CannotBeChangedByACast()
        {
            // arrange
            var name = VcardName.Parse(VcardValueExamples.NameSimple.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = name.FamilyNames as IList;

            // assert
            testResult.IsReadOnly.Should().BeTrue();
            (name.FamilyNames is string[]).Should().BeFalse();
        }

        public static TheoryData<Action> InvalidNameConstructors()
        {
            return new TheoryData<Action>
            {
                () => new VcardName(null, None, None, None, None),
                () => new VcardName(new string[] { null }, None, None, None, None),
                () => new VcardName(None, None, None, None, None, null, None),
            };
        }

        [Theory]
        [MemberData(nameof(InvalidNameConstructors), DisableDiscoveryEnumeration = true)]
        public void Constructor_NullComponentOrItem_Throws(Action testAction)
        {
            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_ControlCharacterInAnItem_Throws()
        {
            // act
            Action testAction = () => new VcardName(new[] { "a\x01" }, None, None, None, None);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TryParseView_Rfc6350Name_GivesEachComponentAndItemWithItsEscaping()
        {
            // arrange
            const string value = "a\\,b;c,d;;;";

            // act
            VcardStructuredValueView view;
            var testResult = VcardName.TryParseView(value.AsSpan(), VcardValueFormat.Text, out view);

            // assert
            testResult.Should().BeTrue();
            view.ComponentCount.Should().Be(5);
            view.GetComponent(VcardName.FamilyNamesComponent).RawValue.ToString().Should().Be("a\\,b");
            ItemsOf(view, VcardName.GivenNamesComponent).Should().Equal("c", "d");
            view.GetComponent(VcardName.AdditionalNamesComponent).IsEmpty.Should().BeTrue();
            ItemsOf(view, VcardName.AdditionalNamesComponent).Should().BeEmpty();
        }

        [Fact]
        public void GetEnumerator_Organization_GivesEachComponentInOrder()
        {
            // act
            var testResult = RawComponentsOfOrganization(VcardValueExamples.Organization);

            // assert
            testResult.Should().Equal("ABC\\, Inc.", "North American Division", "Marketing");
        }

        [Fact]
        public void GetEnumerator_EmptyComponents_GivesThemToo()
        {
            // act
            var testResult = RawComponentsOfOrganization(";a;");

            // assert
            testResult.Should().Equal("", "a", "");
        }

        [Fact]
        public void GetEnumerator_DefaultView_GivesNothing()
        {
            // act
            var testResult = RawComponentsOfDefaultView();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void GetComponent_ComponentTheValueDoesNotHave_IsEmpty()
        {
            // arrange
            VcardName.TryParseView(VcardValueExamples.NameSimple.AsSpan(), VcardValueFormat.Text, out var view);

            // act
            var testResult = view.GetComponent(VcardName.GenerationComponent).IsEmpty;

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void GetComponent_NegativeIndex_Throws()
        {
            // act
            Action testAction = () => GetComponentOfParsedName(-1);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void UnescapeItem_EscapedItem_GivesItsText()
        {
            // arrange
            const string value = "a\\,b;;;;";
            VcardName.TryParseView(value.AsSpan(), VcardValueFormat.Text, out var view);
            var destination = new char[value.Length];

            // act
            var testResult = view.UnescapeItem(view.GetComponent(VcardName.FamilyNamesComponent).RawValue, destination);

            // assert
            new string(destination, 0, testResult).Should().Be("a,b");
        }

        [Fact]
        public void UnescapeItem_DestinationTooShort_Throws()
        {
            // arrange
            var destination = new char[1];

            // act
            Action testAction = () => UnescapeWithParsedName("abc", destination);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TryParse_Rfc6350Address_GivesItsComponents()
        {
            // act
            VcardAddress result;
            var testResult = VcardAddress.TryParse(VcardValueExamples.AddressSimple.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.PostOfficeBox.Should().BeEmpty();
            result.ExtendedAddress.Should().BeEmpty();
            result.StreetAddress.Should().Equal("123 Main Street");
            result.Locality.Should().Equal("Any Town");
            result.Region.Should().Equal("CA");
            result.PostalCode.Should().Equal("91921-1234");
            result.CountryName.Should().Equal("U.S.A.");
            result.Room.Should().BeEmpty();
        }

        [Fact]
        public void TryParse_Rfc9554Address_GivesItsNewComponents()
        {
            // act
            var testResult = VcardAddress.Parse(VcardValueExamples.AddressRfc9554.AsSpan(), VcardValueFormat.Text);

            // assert
            testResult.StreetNumber.Should().Equal("123");
            testResult.StreetName.Should().Equal("Main Street");
            testResult.Direction.Should().BeEmpty();
        }

        // ref. RFC 9554 §2.1: the order of the components of ADR-value.
        public static TheoryData<Func<VcardAddress, IReadOnlyList<string>>, int, string> AddressComponents()
        {
            return new TheoryData<Func<VcardAddress, IReadOnlyList<string>>, int, string>
            {
                { address => address.PostOfficeBox, VcardAddress.PostOfficeBoxComponent, "pobox" },
                { address => address.ExtendedAddress, VcardAddress.ExtendedAddressComponent, "ext" },
                { address => address.StreetAddress, VcardAddress.StreetAddressComponent, "street" },
                { address => address.Locality, VcardAddress.LocalityComponent, "locality" },
                { address => address.Region, VcardAddress.RegionComponent, "region" },
                { address => address.PostalCode, VcardAddress.PostalCodeComponent, "code" },
                { address => address.CountryName, VcardAddress.CountryNameComponent, "country" },
                { address => address.Room, VcardAddress.RoomComponent, "room" },
                { address => address.Apartment, VcardAddress.ApartmentComponent, "apartment" },
                { address => address.Floor, VcardAddress.FloorComponent, "floor" },
                { address => address.StreetNumber, VcardAddress.StreetNumberComponent, "streetnumber" },
                { address => address.StreetName, VcardAddress.StreetNameComponent, "streetname" },
                { address => address.Building, VcardAddress.BuildingComponent, "building" },
                { address => address.Block, VcardAddress.BlockComponent, "block" },
                { address => address.Subdistrict, VcardAddress.SubdistrictComponent, "subdistrict" },
                { address => address.District, VcardAddress.DistrictComponent, "district" },
                { address => address.Landmark, VcardAddress.LandmarkComponent, "landmark" },
                { address => address.Direction, VcardAddress.DirectionComponent, "direction" },
            };
        }

        [Theory]
        [MemberData(nameof(AddressComponents), DisableDiscoveryEnumeration = true)]
        public void Component_EveryPosition_GivesTheItemsInThatPosition(Func<VcardAddress, IReadOnlyList<string>> member, int position, string expected)
        {
            // arrange
            const string value = "pobox;ext;street;locality;region;code;country;room;apartment;floor;streetnumber;streetname;building;block;subdistrict;district;landmark;direction";
            var address = VcardAddress.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = member(address);

            // assert
            testResult.Should().Equal(expected);
            address.GetComponent(position).Should().Equal(expected);
        }

        [Theory]
        [InlineData(VcardValueExamples.AddressSimple)]
        [InlineData(VcardValueExamples.AddressRfc9554)]
        [InlineData(";;;;;;")]
        [InlineData("a,b;;;;;;")]
        public void ToString_ParsedVersion40Address_WritesTheSameText(string value)
        {
            // arrange
            var address = VcardAddress.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = address.ToString(VcardValueFormat.Text);

            // assert
            testResult.Should().Be(value);
        }

        [Theory]
        [InlineData("")]
        [InlineData(";;;;;")]
        [InlineData(";;;;;;;")]
        [InlineData(";;;;;;;;;;;;;;;;")]
        [InlineData(";;;;;;;;;;;;;;;;;;")]
        public void TryParse_Version40AddressWithAnotherNumberOfComponents_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardAddress.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("a", VcardVersion.V3_0, true)]
        [InlineData("a\\,b;c", VcardVersion.V3_0, true)]
        [InlineData("a,b;c", VcardVersion.V3_0, false)]
        [InlineData(";;;;;;;", VcardVersion.V3_0, false)]
        [InlineData("a,b;c", VcardVersion.V2_1, true)]
        [InlineData(";;;;;;;", VcardVersion.V2_1, false)]
        public void TryParse_LegacyAddress_FollowsTheGrammarOfTheVersion(string value, VcardVersion version, bool expected)
        {
            // act
            var testResult = VcardAddress.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardVersion.V4_0, true)]
        [InlineData(VcardVersion.V3_0, false)]
        [InlineData(VcardVersion.V2_1, false)]
        public void IsDefinedIn_AddressWithSeveralItems_IsOnlyInVersion40(VcardVersion version, bool expected)
        {
            // arrange
            var address = VcardAddress.Parse("a,b;;;;;;".AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = address.IsDefinedIn(version);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToString_AddressInVersion30_EscapesACommaAndWritesSevenComponents()
        {
            // arrange
            var address = VcardAddress.Parse("a\\,b;c".AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0);

            // act
            var testResult = address.ToString(VcardValueFormat.Text, VcardVersion.V3_0);

            // assert
            testResult.Should().Be("a\\,b;c;;;;;");
        }

        [Fact]
        public void Constructor_GivenSevenComponents_WritesThem()
        {
            // act
            var testResult = new VcardAddress(None, None, new[] { "123 Main Street" }, new[] { "Any Town" }, new[] { "CA" }, new[] { "91921-1234" }, new[] { "U.S.A." });

            // assert
            testResult.ToString().Should().Be(VcardValueExamples.AddressSimple);
        }

        [Fact]
        public void Constructor_GivenEighteenComponents_WritesThem()
        {
            // arrange
            var components = new IEnumerable<string>[]
            {
                None, None, new[] { "123 Main Street" }, new[] { "Any Town" }, new[] { "CA" }, new[] { "91921-1234" }, new[] { "U.S.A" },
                None, None, None, new[] { "123" }, new[] { "Main Street" }, None, None, None, None, None, None
            };

            // act
            var testResult = new VcardAddress(components);

            // assert
            testResult.ToString().Should().Be(VcardValueExamples.AddressRfc9554);
        }

        [Fact]
        public void Constructor_NotEighteenComponents_Throws()
        {
            // act
            Action testAction = () => new VcardAddress(new IEnumerable<string>[] { None });

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetComponent_PositionOutOfRange_Throws()
        {
            // arrange
            var address = VcardAddress.Parse(VcardValueExamples.AddressSimple.AsSpan(), VcardValueFormat.Text);

            // act
            Action testAction = () => address.GetComponent(18);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void TryParse_Rfc6350Organization_GivesItsNameAndUnits()
        {
            // act
            VcardOrganization result;
            var testResult = VcardOrganization.TryParse(VcardValueExamples.Organization.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Name.Should().Be("ABC, Inc.");
            result.Units.Should().Equal("North American Division", "Marketing");
            result.ToString().Should().Be(VcardValueExamples.Organization);
        }

        [Theory]
        [InlineData("a,b", VcardVersion.V4_0, false)]
        [InlineData("a,b", VcardVersion.V3_0, false)]
        [InlineData("a,b", VcardVersion.V2_1, true)]
        [InlineData("", VcardVersion.V4_0, true)]
        [InlineData(";Unit", VcardVersion.V4_0, true)]
        [InlineData("a\\:b", VcardVersion.V4_0, false)]
        public void TryParse_Organization_FollowsTheGrammarOfTheVersion(string value, VcardVersion version, bool expected)
        {
            // act
            var testResult = VcardOrganization.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Constructor_NameAndUnits_EscapesWhatItWrites()
        {
            // arrange
            var organization = new VcardOrganization("ABC, Inc.", new[] { "North American Division", "Marketing" });

            // act
            var testResult = organization.ToString(VcardValueFormat.Text);

            // assert
            testResult.Should().Be(VcardValueExamples.Organization);
        }

        [Fact]
        public void TryParseView_Rfc6350Organization_GivesEachComponentAsOneItem()
        {
            // act
            VcardOrganization.TryParseView(VcardValueExamples.Organization.AsSpan(), VcardValueFormat.Text, out var testResult);

            // assert
            testResult.ComponentCount.Should().Be(3);
            ItemsOf(testResult, 0).Should().Equal("ABC\\, Inc.");
        }

        [Fact]
        public void Equals_OrganizationsWithTheSameText_IsTrue()
        {
            // arrange
            var organization = new VcardOrganization("ABC, Inc.", new[] { "Marketing" });
            var other = VcardOrganization.Parse("ABC\\, Inc.;Marketing".AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = organization == other;

            // assert
            testResult.Should().BeTrue();
            organization.GetHashCode().Should().Be(other.GetHashCode());
        }

        [Fact]
        public void Equals_OrganizationsWithDifferentUnits_IsFalse()
        {
            // arrange
            var organization = new VcardOrganization("a", new[] { "b" });
            var other = new VcardOrganization("a", new[] { "c" });

            // act
            var testResult = organization != other;

            // assert
            testResult.Should().BeTrue();
        }

        public static TheoryData<Action> UninitializedMembers()
        {
            return new TheoryData<Action>
            {
                () => _ = default(VcardName).FamilyNames,
                () => _ = default(VcardName).Generations,
                () => default(VcardName).IsDefinedIn(VcardVersion.V4_0),
                () => _ = default(VcardAddress).PostOfficeBox,
                () => _ = default(VcardAddress).Direction,
                () => default(VcardAddress).GetComponent(0),
                () => default(VcardAddress).IsDefinedIn(VcardVersion.V4_0),
                () => _ = default(VcardOrganization).Name,
                () => _ = default(VcardOrganization).Units,
                () => default(VcardOrganization).IsDefinedIn(VcardVersion.V4_0),
            };
        }

        [Theory]
        [MemberData(nameof(UninitializedMembers), DisableDiscoveryEnumeration = true)]
        public void Member_UninitializedInstance_Throws(Action testAction)
        {
            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        public static TheoryData<Func<string>, string> UninitializedToString()
        {
            return new TheoryData<Func<string>, string>
            {
                { () => default(VcardName).ToString(), typeof(VcardName).FullName },
                { () => default(VcardAddress).ToString(), typeof(VcardAddress).FullName },
                { () => default(VcardOrganization).ToString(), typeof(VcardOrganization).FullName },
            };
        }

        [Theory]
        [MemberData(nameof(UninitializedToString), DisableDiscoveryEnumeration = true)]
        public void ToString_UninitializedInstance_GivesTheTypeName(Func<string> toStringCall, string expected)
        {
            // act
            var testResult = toStringCall();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Equals_TwoUninitializedInstances_IsTrue()
        {
            // act
            var testResult = default(VcardName) == default(VcardName);

            // assert
            testResult.Should().BeTrue();
        }

        /// <summary>
        /// Copies the text of each component, since they are spans that can't leave the method.
        /// </summary>
        private static List<string> RawComponentsOfOrganization(string value)
        {
            VcardOrganization.TryParseView(value.AsSpan(), VcardValueFormat.Text, out var view);
            var components = new List<string>();
            foreach (var component in view)
            {
                components.Add(component.RawValue.ToString());
            }

            return components;
        }

        private static List<string> RawComponentsOfDefaultView()
        {
            var components = new List<string>();
            foreach (var component in default(VcardStructuredValueView))
            {
                components.Add(component.RawValue.ToString());
            }

            return components;
        }

        // A view can't be captured by a delegate, so these get one and make the call that is under test.
        private static void GetComponentOfParsedName(int index)
        {
            VcardName.TryParseView(VcardValueExamples.NameSimple.AsSpan(), VcardValueFormat.Text, out var view);
            view.GetComponent(index);
        }

        private static void UnescapeWithParsedName(string item, char[] destination)
        {
            VcardName.TryParseView(VcardValueExamples.NameSimple.AsSpan(), VcardValueFormat.Text, out var view);
            view.UnescapeItem(item.AsSpan(), destination);
        }

        /// <summary>
        /// Copies the items of a component, since they are spans that can't leave the method.
        /// </summary>
        private static List<string> ItemsOf(VcardStructuredValueView view, int component)
        {
            var items = new List<string>();
            foreach (var item in view.GetComponent(component))
            {
                items.Add(item.ToString());
            }

            return items;
        }
    }
}
