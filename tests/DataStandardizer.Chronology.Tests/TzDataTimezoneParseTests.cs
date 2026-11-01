using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TzDataTimezoneParseTests
{
    private const string UnknownIdentifier = "Europe/Atlantis";
    private const string NotKnownBecause = "'{0}' is not the identifier of a known timezone";

    // Identifiers of zone1970.tab in TZ Database 2026e.
    private const string ZurichIdentifier = "Europe/Zurich";
    private const string BuenosAiresIdentifier = "America/Argentina/Buenos_Aires";
    private const string AucklandIdentifier = "Pacific/Auckland";

    // The precision to which a coordinate in degrees and minutes is compared, after conversion to decimal degrees.
    private const double CoordinateTolerance = 1e-9;

    // Europe/Zurich in zone1970.tab of TZ Database 2026e: the ISO 6709 coordinates +4723+00832 in decimal degrees,
    // and the country codes CH,DE,LI (ISO 3166-1 alpha-2 codes).
    private const double ZurichLatitude = 47 + 23 / 60D;
    private const double ZurichLongitude = 8 + 32 / 60D;
    private const string Switzerland = "CH";
    private const string Germany = "DE";
    private const string Liechtenstein = "LI";

    public static TheoryData<string> TimezoneField_TestCases => TimezoneFieldCatalog.CreateIdentifierTestCases();

    public static TheoryData<string> UnknownIdentifier_TestCases => new()
    {
        string.Empty,
        " ",
        UnknownIdentifier,
        "europe/zurich",
        "EUROPE/ZURICH",
        " Europe/Zurich",
        "Europe/Zurich ",
        "Europe",
        "Zurich",
    };

    private static T ParseAs<T>(string s) where T : IParsable<T>
    {
        return T.Parse(s, null);
    }

    private static bool TryParseAs<T>(string? s, out T? result) where T : IParsable<T>
    {
        return T.TryParse(s, null, out result);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void Parse_TimezoneIdentifier_ReturnsField(string testIdentifier)
    {
        // arrange
        var expectedResult = TimezoneFieldCatalog.Timezones[testIdentifier];

        // act
        var testResult = TzDataTimezone.Parse(testIdentifier);

        // assert
        testResult.Should().Be(expectedResult, "{0} parses to the field with that identifier", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void ZoneLines_ParsedTimezoneIdentifier_AreThoseOfField(string testIdentifier)
    {
        // arrange
        var testTimezone = TzDataTimezone.Parse(testIdentifier);
        var expectedResult = TimezoneFieldCatalog.Timezones[testIdentifier].ZoneLines;

        // act
        var testResult = testTimezone.ZoneLines;

        // assert
        testResult.Should().Equal(expectedResult, (parsedZoneLine, fieldZoneLine) => ReferenceEquals(parsedZoneLine, fieldZoneLine), "{0} parses to a timezone with the zone lines of its field", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void TryParse_TimezoneIdentifier_ReturnsTrueAndField(string testIdentifier)
    {
        // arrange
        var expectedResult = TimezoneFieldCatalog.Timezones[testIdentifier];

        // act
        var testSuccess = TzDataTimezone.TryParse(testIdentifier, out var testResult);

        // assert
        testSuccess.Should().BeTrue("{0} is the identifier of a known timezone", testIdentifier);
        testResult.Should().Be(expectedResult, "{0} parses to the field with that identifier", testIdentifier);
    }

    [Fact]
    public void Parse_EuropeZurich_ReturnsField()
    {
        // act
        var testResult = TzDataTimezone.Parse(ZurichIdentifier);

        // assert
        testResult.Should().Be(TzDataTimezone.Europe.Zurich);
    }

    [Fact]
    public void IsoCountryCodes_ParsedEuropeZurich_ReturnsCountryCodesFromZone1970Tab()
    {
        // arrange
        var testTimezone = TzDataTimezone.Parse(ZurichIdentifier);

        // act
        var testResult = testTimezone.IsoCountryCodes;

        // assert
        testResult.Should().Equal(Switzerland, Germany, Liechtenstein);
    }

    [Fact]
    public void Latitude_ParsedEuropeZurich_ReturnsLatitudeFromZone1970Tab()
    {
        // arrange
        var testTimezone = TzDataTimezone.Parse(ZurichIdentifier);

        // act
        var testResult = testTimezone.Latitude;

        // assert
        testResult.Should().BeApproximately(ZurichLatitude, CoordinateTolerance);
    }

    [Fact]
    public void Longitude_ParsedEuropeZurich_ReturnsLongitudeFromZone1970Tab()
    {
        // arrange
        var testTimezone = TzDataTimezone.Parse(ZurichIdentifier);

        // act
        var testResult = testTimezone.Longitude;

        // assert
        testResult.Should().BeApproximately(ZurichLongitude, CoordinateTolerance);
    }

    [Theory]
    [MemberData(nameof(UnknownIdentifier_TestCases))]
    public void Parse_UnknownIdentifier_ThrowsFormatException(string testIdentifier)
    {
        // act
        Action testAction = () => TzDataTimezone.Parse(testIdentifier);

        // assert
        testAction.Should().Throw<FormatException>(NotKnownBecause, testIdentifier);
    }

    [Theory]
    [MemberData(nameof(UnknownIdentifier_TestCases))]
    public void TryParse_UnknownIdentifier_ReturnsFalseAndDefault(string testIdentifier)
    {
        // act
        var testSuccess = TzDataTimezone.TryParse(testIdentifier, out var testResult);

        // assert
        testSuccess.Should().BeFalse(NotKnownBecause, testIdentifier);
        testResult.Should().Be(default(TzDataTimezone));
    }

    [Fact]
    public void Parse_Null_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => TzDataTimezone.Parse(null!);

        // assert
        testAction.Should().Throw<ArgumentNullException>().WithParameterName("s");
    }

    [Fact]
    public void TryParse_Null_ReturnsFalseAndDefault()
    {
        // act
        var testSuccess = TzDataTimezone.TryParse(null, out var testResult);

        // assert
        testSuccess.Should().BeFalse();
        testResult.Should().Be(default(TzDataTimezone));
    }

    [Fact]
    public void ParseAs_TimezoneIdentifier_ReturnsField()
    {
        // act
        var testResult = ParseAs<TzDataTimezone>(BuenosAiresIdentifier);

        // assert
        testResult.Should().Be(TzDataTimezone.America.Argentina.Buenos_Aires);
    }

    [Fact]
    public void ParseAs_UnknownIdentifier_ThrowsFormatException()
    {
        // act
        Action testAction = () => ParseAs<TzDataTimezone>(UnknownIdentifier);

        // assert
        testAction.Should().Throw<FormatException>();
    }

    [Fact]
    public void TryParseAs_TimezoneIdentifier_ReturnsTrueAndField()
    {
        // act
        var testSuccess = TryParseAs<TzDataTimezone>(AucklandIdentifier, out var testResult);

        // assert
        testSuccess.Should().BeTrue();
        testResult.Should().Be(TzDataTimezone.Pacific.Auckland);
    }

    [Fact]
    public void TryParseAs_Null_ReturnsFalseAndDefault()
    {
        // act
        var testSuccess = TryParseAs<TzDataTimezone>(null, out var testResult);

        // assert
        testSuccess.Should().BeFalse();
        testResult.Should().Be(default(TzDataTimezone));
    }
}
