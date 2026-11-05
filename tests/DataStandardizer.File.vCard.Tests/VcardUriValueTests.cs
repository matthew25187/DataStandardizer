using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the escaping of a URI value in a content line.  ref. RFC 6350 §3.4: a comma in a value "MUST be escaped", which its errata 3845 and 3846 apply to URI values.
    /// </summary>
    public class VcardUriValueTests
    {
        public static TheoryData<string, string> EscapedValueData()
        {
            return new TheoryData<string, string>
            {
                // ref. RFC 6350 erratum 3846.
                { "geo:37.386013\\,-122.082932", "geo:37.386013,-122.082932" },

                // ref. RFC 6350 erratum 3845.
                { "data:image/jpeg;base64\\,MIICajCCAdOgAwIBAgICBEUwDQYJKoZIhv", "data:image/jpeg;base64,MIICajCCAdOgAwIBAgICBEUwDQYJKoZIhv" },

                // ref. RFC 6350 §3.4: "SEMICOLON characters in non-compound properties MAY be escaped."
                { "tel:+1-555-555-5555\\;ext=5555", "tel:+1-555-555-5555;ext=5555" },
                { "tel:+1-555-555-5555;ext=5555", "tel:+1-555-555-5555;ext=5555" }
            };
        }

        [Theory]
        [MemberData(nameof(EscapedValueData))]
        public void TryParseUri_EscapedValue_GivesTheUriWithoutItsEscaping(string value, string expected)
        {
            // act
            Uri result;
            var testResult = VcardValue.TryParseUri(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.OriginalString.Should().Be(expected);
        }

        [Theory]
        [InlineData("geo:37.386013,-122.082932")]
        [InlineData("ldap://ldap.example.com/cn=Babs%20Jensen,%20o=Babsco")]
        [InlineData("http://example.com/\\n")]
        [InlineData("http://example.com/\\\\")]
        [InlineData("http://example.com/\\")]
        public void TryParseUri_BareCommaOrOtherBackslash_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardValue.TryParseUri(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryFormatUri_UriWithComma_EscapesIt()
        {
            // arrange
            const string Expected = "geo:37.386013\\,-122.082932";
            var uri = new Uri("geo:37.386013,-122.082932");
            var destination = new char[Expected.Length];

            // act
            int charsWritten;
            var testResult = VcardValue.TryFormatUri(uri, destination, out charsWritten, VcardValueFormat.Text);

            // assert
            testResult.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be(Expected);
        }

        [Fact]
        public void TryFormatUri_UriWithSemicolon_LeavesItAsItIs()
        {
            // arrange
            const string Expected = "tel:+1-555-555-5555;ext=5555";
            var uri = new Uri(Expected);
            var destination = new char[Expected.Length];

            // act
            int charsWritten;
            VcardValue.TryFormatUri(uri, destination, out charsWritten, VcardValueFormat.Text);

            // assert
            new string(destination, 0, charsWritten).Should().Be(Expected);
        }

        [Fact]
        public void ToString_TypedValueOfAUriWithComma_EscapesIt()
        {
            // arrange
            var value = new VcardTypedValue(new Uri("geo:1,2"));

            // act
            var testResult = value.ToString();

            // assert
            testResult.Should().Be("geo:1\\,2");
        }

        [Fact]
        public void ClientPidMap_UriWithComma_IsEscapedInTheValue()
        {
            // arrange
            var map = new VcardClientPidMap(1, new Uri("urn:a,b"));

            // act
            var testResult = map.ToString();

            // assert
            testResult.Should().Be("1;urn:a\\,b");
        }

        [Fact]
        public void TryFormat_ClientPidMapUriWithComma_EscapesIt()
        {
            // arrange
            const string Expected = "1;urn:a\\,b";
            var map = new VcardClientPidMap(1, new Uri("urn:a,b"));
            var destination = new char[Expected.Length];

            // act
            int charsWritten;
            map.TryFormat(destination, out charsWritten, VcardValueFormat.Text);

            // assert
            new string(destination, 0, charsWritten).Should().Be(Expected);
        }

        [Fact]
        public void TryParse_ClientPidMapWithEscapedComma_GivesTheUriWithoutItsEscaping()
        {
            // act
            VcardClientPidMap result;
            var testResult = VcardClientPidMap.TryParse("1;urn:a\\,b".AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Uri.OriginalString.Should().Be("urn:a,b");
        }

        [Fact]
        public void TryParse_ClientPidMapWithBareComma_ReturnsFalse()
        {
            // act
            var testResult = VcardClientPidMap.TryParse("1;urn:a,b".AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParseParts_ClientPidMapWithEscapedComma_GivesTheTextAsItAppears()
        {
            // act
            long sourceId;
            ReadOnlySpan<char> uri;
            var testResult = VcardClientPidMap.TryParseParts("1;urn:a\\,b".AsSpan(), VcardValueFormat.Text, out sourceId, out uri);

            // assert
            testResult.Should().BeTrue();
            uri.ToString().Should().Be("urn:a\\,b");
        }

        [Fact]
        public void ToString_GeoWithEscapedComma_WritesItEscaped()
        {
            // arrange
            var geo = VcardGeo.Parse(VcardValueExamples.GeoValue.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = geo.ToString();

            // assert
            testResult.Should().Be(VcardValueExamples.GeoValue);
        }

        [Fact]
        public void TryParse_GeoWithBareComma_ReturnsFalse()
        {
            // act
            var testResult = VcardGeo.TryParse(VcardValueExamples.GeoUri.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParse_GeoUriFromACard_GivesItsCoordinates()
        {
            // arrange
            const double ExpectedLatitude = 37.386013;
            var card = VcardCardTestData.Read(VcardCardTestData.Card("GEO:geo:37.386013\\,-122.082932"));

            // act
            VcardGeo result;
            var testResult = VcardGeo.TryParse(card.GeoLocations[0].Value, out result);

            // assert
            testResult.Should().BeTrue();
            result.Latitude.Should().Be(ExpectedLatitude);
        }

        [Fact]
        public void TryParse_RelativeUri_ReturnsFalse()
        {
            // arrange
            var uri = new Uri("a/b", UriKind.Relative);

            // act
            var testResult = VcardGeo.TryParse(uri, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParse_NullUri_ThrowsArgumentNullException()
        {
            // arrange
            const Uri NoUri = null;

            // act
            Action testAction = () => VcardGeo.TryParse(NoUri, out _);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        public static TheoryData<string> BareCommaData()
        {
            return new TheoryData<string>
            {
                // ref. RFC 6474 §2.1 and §2.2 as printed, which its errata 4557 and 4559 correct.
                "BIRTHPLACE;VALUE=uri:geo:46.769307,-71.283079",
                "DEATHPLACE;VALUE=uri:geo:41.731944,-49.945833",

                // ref. RFC 6715 §2.4 as printed.
                "ORG-DIRECTORY;PREF=1:ldap://ldap.tech.example/o=Example%20Tech,ou=Engineering",

                // ref. RFC 6350 §6.2.4 and §6.5.2 as printed, which its errata 3845 and 3846 correct, and §6.1.3, §6.7.5 and §8, which those errata say are the same.
                "PHOTO:data:image/jpeg;base64,MIICajCCAdOgAwIBAgICBEUwDQYJKoZIhv",
                "GEO:geo:37.386013,-122.082932",
                "SOURCE:ldap://ldap.example.com/cn=Babs%20Jensen,%20o=Babsco,%20c=US",
                "SOUND:data:audio/basic;base64,MIICajCCAdOgAwIBAgICBEUwDQYJKoZIh",
                "GEO;TYPE=work:geo:46.772673,-71.282945",

                // The other places a URI value can be.
                "TEL;VALUE=uri:tel:+1,2",
                "UID:urn:a,b",
                "CLIENTPIDMAP:1;urn:a,b"
            };
        }

        [Theory]
        [MemberData(nameof(BareCommaData))]
        public void ReadCard_BareCommaInUriByDefault_Throws(string property)
        {
            // arrange
            var text = VcardCardTestData.Card(property);

            // act
            Action testAction = () => VcardCardTestData.Read(text);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Theory]
        [MemberData(nameof(BareCommaData))]
        public void Validate_BareCommaInUriWithOption_FindsNoProblem(string property)
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card(property), new VcardOptions { AcceptUnescapedCommasInUris = true });

            // act
            var testResult = card.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void ReadCard_BareCommaWithOption_GivesTheTypedValue()
        {
            // arrange
            var options = new VcardOptions { AcceptUnescapedCommasInUris = true };

            // act
            var testResult = VcardCardTestData.Read(VcardCardTestData.Card("GEO:geo:1,2"), options);

            // assert
            testResult.GeoLocations[0].Value.OriginalString.Should().Be("geo:1,2");
        }

        [Fact]
        public void WriteCard_BareCommaReadWithOption_WritesItEscaped()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("GEO:geo:1,2"), new VcardOptions { AcceptUnescapedCommasInUris = true });

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(VcardCardTestData.Card("GEO:geo:1\\,2"));
        }

        [Theory]
        [InlineData("GEO:geo:1\\,2", "geo:1\\,2")]
        [InlineData("GEO:geo:1\\,2,3", "geo:1\\,2\\,3")]
        [InlineData("X-A:a,b", "a,b")]
        [InlineData("CATEGORIES:a,b", "a,b")]
        public void ReadCard_OptionOnOtherValues_LeavesThemAsTheyAre(string property, string expected)
        {
            // arrange
            var options = new VcardOptions { AcceptUnescapedCommasInUris = true };

            // act
            var testResult = VcardCardTestData.Read(VcardCardTestData.Card(property), options);

            // assert
            testResult.Properties.Last().Value.Should().Be(expected);
        }

        [Fact]
        public void ReadCard_BareCommaInTextWithOption_StillThrows()
        {
            // arrange
            var text = VcardCardTestData.Card("NOTE:a,b");
            var options = new VcardOptions { AcceptUnescapedCommasInUris = true };

            // act
            Action testAction = () => VcardCardTestData.Read(text, options);

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Fact]
        public void WriteCard_LineWithBareCommaAddedDirectly_Throws()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            card.Properties.Add(new VcardContentLine(VcardPropertyNames.Geo, "geo:1,2"));

            // act
            Action testAction = () => VcardCardTestData.Write(card, new VcardOptions { AcceptUnescapedCommasInUris = true });

            // assert
            testAction.Should().Throw<VcardFileException>();
        }

        [Fact]
        public void WriteCard_GeoLocationWithComma_WritesItEscaped()
        {
            // arrange
            var card = VcardCardTestData.NewCard();
            card.GeoLocations.Add(new Uri("geo:1,2"));

            // act
            var testResult = VcardCardTestData.Write(card);

            // assert
            testResult.Should().Be(VcardCardTestData.Card("GEO:geo:1\\,2"));
        }

        [Fact]
        public void GeoLocation_ParameterOfAnAddress_HasNoEscaping()
        {
            // arrange
            // ref. RFC 6350 §6.3.1: the GEO parameter is a quoted string, to which §3.4 on property values doesn't apply.
            var card = VcardCardTestData.Read(VcardCardTestData.Card("ADR;GEO=\"geo:12.3457,78.910\":;;;;;;"));

            // act
            var testResult = card.Addresses[0].GeoLocation;

            // assert
            testResult.OriginalString.Should().Be("geo:12.3457,78.910");
        }

        [Fact]
        public void FindByUid_UidWithEscapedComma_FindsItByTheTextWithoutItsEscaping()
        {
            // arrange
            var card = VcardCardTestData.Read(VcardCardTestData.Card("UID:urn:a\\,b"));

            // act
            var testResult = VcardCard.FindByUid(new[] { card }, "urn:a,b");

            // assert
            testResult.Should().BeSameAs(card);
        }

        [Fact]
        public void ResolveMembers_MemberWithEscapedComma_FindsTheCard()
        {
            // arrange
            var group = VcardCardTestData.Read(VcardCardTestData.Card("KIND:group", "MEMBER:urn:a\\,b"));
            var member = VcardCardTestData.Read(VcardCardTestData.Card("UID:urn:a\\,b"));

            // act
            var testResult = group.ResolveMembers(new[] { member });

            // assert
            testResult.Should().Equal(member);
        }
    }
}
