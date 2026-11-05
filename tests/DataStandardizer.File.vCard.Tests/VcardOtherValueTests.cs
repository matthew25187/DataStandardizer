using System;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of <see cref="VcardGender"/>, <see cref="VcardPid"/>, <see cref="VcardClientPidMap"/>, <see cref="VcardGeo"/> and <see cref="VcardLegacyMediaTypes"/>.
    /// </summary>
    public class VcardOtherValueTests
    {
        [Theory]
        [InlineData(VcardValueExamples.GenderMale, VcardSex.Male, null)]
        [InlineData(VcardValueExamples.GenderFemale, VcardSex.Female, null)]
        [InlineData(VcardValueExamples.GenderMaleFellow, VcardSex.Male, "Fellow")]
        [InlineData(VcardValueExamples.GenderFemaleGrrrl, VcardSex.Female, "grrrl")]
        [InlineData(VcardValueExamples.GenderOtherIntersex, VcardSex.Other, "intersex")]
        [InlineData(VcardValueExamples.GenderIdentityOnly, VcardSex.Unspecified, "it's complicated")]
        [InlineData("N", VcardSex.NotApplicable, null)]
        [InlineData("U;", VcardSex.Unknown, "")]
        [InlineData("", VcardSex.Unspecified, null)]
        public void TryParse_ValidGender_GivesItsComponentsAndWritesThemBack(string value, VcardSex sex, string identity)
        {
            // act
            VcardGender result;
            var testResult = VcardGender.TryParse(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Sex.Should().Be(sex);
            result.Identity.Should().Be(identity);
            result.ToString(VcardValueFormat.Text).Should().Be(value);
        }

        [Fact]
        public void TryParse_LowerCaseLetter_IsValid()
        {
            // act
            VcardGender result;
            var testResult = VcardGender.TryParse("m".AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Sex.Should().Be(VcardSex.Male);
        }

        [Theory]
        [InlineData("X")]
        [InlineData("MF")]
        [InlineData("M;a,b")]
        [InlineData("M;a\\:b")]
        public void TryParse_InvalidGender_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardGender.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParse_IdentityWithASemicolonAndEscapes_IsOneComponent()
        {
            // act
            var testResult = VcardGender.Parse("O;a;b\\,c".AsSpan(), VcardValueFormat.Text);

            // assert
            testResult.Identity.Should().Be("a;b,c");
            testResult.ToString().Should().Be("O;a;b\\,c");
        }

        [Fact]
        public void TryParseParts_ValidGender_GivesTheSexAndTheIdentityWithItsEscaping()
        {
            // act
            VcardSex sex;
            ReadOnlySpan<char> identity;
            var testResult = VcardGender.TryParseParts("M;a\\,b".AsSpan(), VcardValueFormat.Text, out sex, out identity);

            // assert
            testResult.Should().BeTrue();
            sex.Should().Be(VcardSex.Male);
            identity.ToString().Should().Be("a\\,b");
        }

        public static TheoryData<Action> InvalidGenderConstructors()
        {
            return new TheoryData<Action>
            {
                () => new VcardGender((VcardSex)6, null),
                () => new VcardGender((VcardSex)(-1), null),
            };
        }

        [Theory]
        [MemberData(nameof(InvalidGenderConstructors), DisableDiscoveryEnumeration = true)]
        public void Constructor_SexNotRecognised_Throws(Action testAction)
        {
            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_ControlCharacterInTheIdentity_Throws()
        {
            // act
            Action testAction = () => new VcardGender(VcardSex.Male, "a\x01");

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Equals_GendersWithAndWithoutAnEmptyIdentity_IsFalse()
        {
            // arrange
            var withIdentity = new VcardGender(VcardSex.Male, "");
            var withoutIdentity = new VcardGender(VcardSex.Male, null);

            // act
            var testResult = withIdentity != withoutIdentity;

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [InlineData("1", 1L, null)]
        [InlineData(VcardValueExamples.PidLocalAndSource, 3L, 1L)]
        [InlineData("007.02", 7L, 2L)]
        public void TryParse_ValidPid_GivesItsParts(string value, long localId, long? sourceId)
        {
            // act
            VcardPid result;
            var testResult = VcardPid.TryParse(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.LocalId.Should().Be(localId);
            result.SourceId.Should().Be(sourceId);
        }

        [Theory]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("1.0")]
        [InlineData("0.1")]
        [InlineData("1.")]
        [InlineData(".1")]
        [InlineData("1.2.3")]
        [InlineData("-1")]
        [InlineData("a")]
        [InlineData("9223372036854775808")]
        public void TryParse_InvalidPid_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardPid.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void ToString_PidWithASource_WritesBothIntegers()
        {
            // arrange
            var pid = new VcardPid(3, 1);

            // act
            var testResult = pid.ToString();

            // assert
            testResult.Should().Be(VcardValueExamples.PidLocalAndSource);
        }

        [Fact]
        public void TryFormat_PidWithoutASource_WritesOneInteger()
        {
            // arrange
            var pid = new VcardPid(3);
            var destination = new char[8];

            // act
            int charsWritten;
            var testResult = pid.TryFormat(destination, out charsWritten, VcardValueFormat.Text);

            // assert
            testResult.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be("3");
        }

        public static TheoryData<Action> InvalidPidConstructors()
        {
            return new TheoryData<Action>
            {
                () => new VcardPid(0),
                () => new VcardPid(-1),
                () => new VcardPid(1, 0),
            };
        }

        [Theory]
        [MemberData(nameof(InvalidPidConstructors), DisableDiscoveryEnumeration = true)]
        public void Constructor_IdentifierNotPositive_Throws(Action testAction)
        {
            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void TryParse_Rfc6350ClientPidMap_GivesItsPartsAndWritesThemBack()
        {
            // act
            VcardClientPidMap result;
            var testResult = VcardClientPidMap.TryParse(VcardValueExamples.ClientPidMap.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.SourceId.Should().Be(1);
            result.Uri.OriginalString.Should().Be(VcardValueExamples.ClientPidMapUri);
            result.ToString().Should().Be(VcardValueExamples.ClientPidMap);
        }

        [Theory]
        [InlineData("")]
        [InlineData("1")]
        [InlineData(";urn:uuid:a")]
        [InlineData("0;urn:uuid:a")]
        [InlineData("a;urn:uuid:a")]
        [InlineData("-1;urn:uuid:a")]
        [InlineData("1;not a uri")]
        [InlineData("1;")]
        public void TryParse_InvalidClientPidMap_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardClientPidMap.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParseParts_Rfc6350ClientPidMap_GivesTheIdentifierAndTheTextOfTheUri()
        {
            // act
            long sourceId;
            ReadOnlySpan<char> uri;
            var testResult = VcardClientPidMap.TryParseParts(VcardValueExamples.ClientPidMap.AsSpan(), VcardValueFormat.Text, out sourceId, out uri);

            // assert
            testResult.Should().BeTrue();
            sourceId.Should().Be(1);
            uri.ToString().Should().Be(VcardValueExamples.ClientPidMapUri);
        }

        public static TheoryData<Action, Type> InvalidClientPidMapConstructors()
        {
            return new TheoryData<Action, Type>
            {
                { () => new VcardClientPidMap(0, new Uri(VcardValueExamples.ClientPidMapUri)), typeof(ArgumentOutOfRangeException) },
                { () => new VcardClientPidMap(1, null), typeof(ArgumentNullException) },
                { () => new VcardClientPidMap(1, new Uri("a/b", UriKind.Relative)), typeof(ArgumentException) },
                { () => new VcardClientPidMap(1, new Uri("https://example.com/a b")), typeof(ArgumentException) },
            };
        }

        [Theory]
        [MemberData(nameof(InvalidClientPidMapConstructors), DisableDiscoveryEnumeration = true)]
        public void Constructor_InvalidArgument_Throws(Action testAction, Type expected)
        {
            // act
            var testResult = Record.Exception(testAction);

            // assert
            testResult.Should().BeOfType(expected);
        }

        [Fact]
        public void Equals_MapsOfTheSameIdentifierAndUri_IsTrue()
        {
            // arrange
            var map = new VcardClientPidMap(1, new Uri(VcardValueExamples.ClientPidMapUri));
            var other = VcardClientPidMap.Parse(VcardValueExamples.ClientPidMap.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = map == other;

            // assert
            testResult.Should().BeTrue();
            map.GetHashCode().Should().Be(other.GetHashCode());
        }

        [Fact]
        public void TryParse_Rfc6350Geo_GivesTheCoordinatesAndWritesThemBack()
        {
            // act
            VcardGeo result;
            var testResult = VcardGeo.TryParse(VcardValueExamples.GeoUri.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Latitude.Should().Be(37.386013);
            result.Longitude.Should().Be(-122.082932);
            result.Altitude.Should().BeNull();
            result.Uncertainty.Should().BeNull();
            result.UriText.Should().Be(VcardValueExamples.GeoUri);
            result.ToString(VcardValueFormat.Text).Should().Be(VcardValueExamples.GeoUri);
        }

        [Fact]
        public void TryParse_Rfc5870GeoWithAltitudeAndUncertainty_GivesThem()
        {
            // act
            var testResult = VcardGeo.Parse(VcardValueExamples.GeoUriWithUncertainty.AsSpan(), VcardValueFormat.Text);

            // assert
            testResult.Altitude.Should().Be(183);
            testResult.Uncertainty.Should().Be(40);
        }

        [Fact]
        public void TryParse_UriOfAnotherScheme_IsValidWithNoCoordinates()
        {
            // act
            VcardGeo result;
            var testResult = VcardGeo.TryParse(VcardValueExamples.UriHttp.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Latitude.Should().BeNull();
            result.Longitude.Should().BeNull();
            result.ToString().Should().Be(VcardValueExamples.UriHttp);
        }

        [Fact]
        public void TryParse_GeoInAnotherCoordinateSystem_IsValidWithNoCoordinates()
        {
            // act
            VcardGeo result;
            var testResult = VcardGeo.TryParse("geo:1000,2000;crs=other".AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Latitude.Should().BeNull();
        }

        [Theory]
        [InlineData("geo:1,2;crs=wgs84;u=3;a=b")]
        [InlineData("GEO:1,2;CRS=WGS84")]
        [InlineData("geo:1,2;flag")]
        [InlineData("geo:-90,180")]
        public void TryParse_ValidGeoUri_ReturnsTrue(string value)
        {
            // act
            var testResult = VcardGeo.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("37.386013,-122.082932")]
        [InlineData(VcardValueExamples.GeoVersion30)]
        [InlineData("geo:")]
        [InlineData("geo:1")]
        [InlineData("geo:1,")]
        [InlineData("geo:+1,2")]
        [InlineData("geo:1,2,3,4")]
        [InlineData("geo:91,0")]
        [InlineData("geo:0,181")]
        [InlineData("geo:1,2;")]
        [InlineData("geo:1,2;u=-1")]
        [InlineData("geo:1,2;u=1;u=2")]
        [InlineData("geo:1,2;crs=wgs84;crs=wgs84")]
        [InlineData("geo:1,2;u=1;crs=wgs84")]
        [InlineData("geo:1,2;a=")]
        [InlineData("geo:1,2;a=b,c")]
        public void TryParse_InvalidVersion40Geo_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardGeo.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(VcardValueExamples.GeoVersion30, VcardVersion.V3_0, VcardValueExamples.GeoUri)]
        [InlineData("+37.386013;-122.082932", VcardVersion.V3_0, VcardValueExamples.GeoUri)]
        [InlineData(VcardValueExamples.GeoVersion21, VcardVersion.V2_1, VcardValueExamples.GeoVersion21AsUri)]
        public void TryParse_LegacyGeo_IsHeldAsAGeoUri(string value, VcardVersion version, string expected)
        {
            // act
            VcardGeo result;
            var testResult = VcardGeo.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out result);

            // assert
            testResult.Should().BeTrue();
            result.UriText.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardValueExamples.GeoVersion21, VcardVersion.V3_0)]
        [InlineData(VcardValueExamples.GeoVersion30, VcardVersion.V2_1)]
        [InlineData(VcardValueExamples.GeoUri, VcardVersion.V3_0)]
        [InlineData("91;0", VcardVersion.V3_0)]
        [InlineData("0;181", VcardVersion.V3_0)]
        [InlineData("1;2;3", VcardVersion.V3_0)]
        [InlineData("1e1;2", VcardVersion.V3_0)]
        [InlineData("1,2,3", VcardVersion.V2_1)]
        public void TryParse_InvalidLegacyGeo_ReturnsFalse(string value, VcardVersion version)
        {
            // act
            var testResult = VcardGeo.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(VcardVersion.V3_0, VcardValueExamples.GeoVersion30)]
        [InlineData(VcardVersion.V2_1, "37.386013,-122.082932")]
        public void ToString_GeoUriInLegacyVersion_WritesTheFormOfTheVersion(VcardVersion version, string expected)
        {
            // arrange
            var geo = VcardGeo.Parse(VcardValueExamples.GeoUri.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = geo.ToString(VcardValueFormat.Text, version);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardValueExamples.GeoUri, true)]
        [InlineData(VcardValueExamples.GeoUriWithUncertainty, false)]
        [InlineData(VcardValueExamples.UriHttp, false)]
        [InlineData("geo:1,2;crs=wgs84", false)]
        public void IsDefinedIn_Version30_IsOnlyForAPlainLatitudeAndLongitude(string value, bool expected)
        {
            // arrange
            var geo = VcardGeo.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = geo.IsDefinedIn(VcardVersion.V3_0);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void TryFormat_UriOfAnotherSchemeInVersion30_Throws()
        {
            // arrange
            var geo = VcardGeo.Parse(VcardValueExamples.UriHttp.AsSpan(), VcardValueFormat.Text);
            var destination = new char[64];

            // act
            Action testAction = () => geo.TryFormat(destination, out _, VcardValueFormat.Text, VcardVersion.V3_0);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Constructor_LatitudeAndLongitude_WritesAGeoUri()
        {
            // arrange
            var geo = new VcardGeo(37.386013, -122.082932);

            // act
            var testResult = geo.ToString();

            // assert
            testResult.Should().Be(VcardValueExamples.GeoUri);
        }

        [Theory]
        [InlineData(90.1, 0.0)]
        [InlineData(-90.1, 0.0)]
        [InlineData(0.0, 180.1)]
        [InlineData(double.NaN, 0.0)]
        public void Constructor_CoordinateOutOfRange_Throws(double latitude, double longitude)
        {
            // act
            Action testAction = () => new VcardGeo(latitude, longitude);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData("JPEG", "image", "jpeg")]
        [InlineData("jpeg", "image", "jpeg")]
        [InlineData("GIF", "image", "gif")]
        [InlineData("PNG", "image", "png")]
        [InlineData("WAVE", "audio", "vnd.wave")]
        [InlineData("PCM", "audio", "basic")]
        [InlineData("BASIC", "audio", "basic")]
        [InlineData("MPEG2", "video", "mpeg")]
        [InlineData("AVI", "video", "vnd.avi")]
        [InlineData("X509", "application", "pkix-cert")]
        [InlineData("PGP", "application", "pgp-keys")]
        public void TryGetMediaType_KnownToken_GivesItsMediaType(string token, string type, string subtype)
        {
            // arrange
            var expected = new MimeMediaType(type, subtype);

            // act
            MimeMediaType result;
            var testResult = VcardLegacyMediaTypes.TryGetMediaType(token.AsSpan(), out result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("MET")]
        [InlineData("X-FORMAT")]
        [InlineData("")]
        public void TryGetMediaType_TokenWithNoMediaType_ReturnsFalse(string token)
        {
            // act
            var testResult = VcardLegacyMediaTypes.TryGetMediaType(token.AsSpan(), out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("image", "jpeg", VcardVersion.V3_0, "JPEG")]
        [InlineData("IMAGE", "JPEG", VcardVersion.V2_1, "JPEG")]
        [InlineData("image", "png", VcardVersion.V3_0, "PNG")]
        [InlineData("audio", "basic", VcardVersion.V3_0, "BASIC")]
        [InlineData("audio", "basic", VcardVersion.V2_1, "PCM")]
        [InlineData("audio", "wav", VcardVersion.V2_1, "WAVE")]
        [InlineData("video", "mpeg", VcardVersion.V2_1, "MPEG")]
        public void TryGetToken_MediaTypeWithAToken_GivesTheTokenOfTheVersion(string type, string subtype, VcardVersion version, string expected)
        {
            // arrange
            var mediaType = new MimeMediaType(type, subtype);

            // act
            string result;
            var testResult = VcardLegacyMediaTypes.TryGetToken(mediaType, version, out result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("image", "png", VcardVersion.V2_1)]
        [InlineData("image", "jpeg", VcardVersion.V4_0)]
        [InlineData("image", "webp", VcardVersion.V3_0)]
        public void TryGetToken_NoTokenInTheVersion_ReturnsFalse(string type, string subtype, VcardVersion version)
        {
            // arrange
            var mediaType = new MimeMediaType(type, subtype);

            // act
            var testResult = VcardLegacyMediaTypes.TryGetToken(mediaType, version, out _);

            // assert
            testResult.Should().BeFalse();
        }

        public static TheoryData<Action> UninitializedMembers()
        {
            return new TheoryData<Action>
            {
                () => _ = default(VcardGender).Sex,
                () => _ = default(VcardGender).Identity,
                () => _ = default(VcardPid).LocalId,
                () => _ = default(VcardPid).SourceId,
                () => _ = default(VcardClientPidMap).SourceId,
                () => _ = default(VcardClientPidMap).Uri,
                () => _ = default(VcardGeo).UriText,
                () => _ = default(VcardGeo).Latitude,
                () => _ = default(VcardGeo).Longitude,
                () => _ = default(VcardGeo).Altitude,
                () => _ = default(VcardGeo).Uncertainty,
                () => default(VcardGeo).IsDefinedIn(VcardVersion.V4_0),
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
                { () => default(VcardGender).ToString(), typeof(VcardGender).FullName },
                { () => default(VcardPid).ToString(), typeof(VcardPid).FullName },
                { () => default(VcardClientPidMap).ToString(), typeof(VcardClientPidMap).FullName },
                { () => default(VcardGeo).ToString(), typeof(VcardGeo).FullName },
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
    }
}
