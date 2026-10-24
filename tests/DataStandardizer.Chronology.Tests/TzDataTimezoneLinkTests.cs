using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TzDataTimezoneLinkTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, TzDataTimezone>> TimezoneFields = new(() => GetTimezoneFields(typeof(TzDataTimezone)).ToDictionary(timezone => (string)timezone!, StringComparer.Ordinal));

    private static readonly Lazy<IReadOnlyDictionary<string, TzDataTimezone.RegistryEntry>> DeprecatedLinks = new(() => new TzDataTimezone.Registry().GetDeprecatedLinks());

    public static IEnumerable<object[]> TimezoneField_TestCases => TimezoneFields.Value.Keys.Select(identifier => new object[] { identifier });

    public static IEnumerable<object[]> DeprecatedLink_TestCases => DeprecatedLinks.Value.Keys.Select(name => new object[] { name });

    // Links listed in zone.tab, with the location given there and the canonical timezone at the end of their chain of links.
    public static IEnumerable<object?[]> ZoneTabLink_TestCases =>
    [
        [TzDataTimezone.Europe.Oslo, TzDataTimezone.Europe.Berlin, "NO", 59 + 55 / 60.0, 10 + 45 / 60.0, null],
        [TzDataTimezone.Europe.Stockholm, TzDataTimezone.Europe.Berlin, "SE", 59 + 20 / 60.0, 18 + 3 / 60.0, null],
        [TzDataTimezone.Asia.Kuala_Lumpur, TzDataTimezone.Asia.Singapore, "MY", 3 + 10 / 60.0, 101 + 42 / 60.0, "Malaysia (peninsula)"],
        [TzDataTimezone.Arctic.Longyearbyen, TzDataTimezone.Europe.Berlin, "SJ", 78.0, 16.0, null],
        [TzDataTimezone.Atlantic.Reykjavik, TzDataTimezone.Africa.Abidjan, "IS", 64 + 9 / 60.0, -(21 + 51 / 60.0), null],
        [TzDataTimezone.America.Nassau, TzDataTimezone.America.Toronto, "BS", 25 + 5 / 60.0, -(77 + 21 / 60.0), null],
        [TzDataTimezone.Antarctica.McMurdo, TzDataTimezone.Pacific.Auckland, "AQ", -(77 + 50 / 60.0), 166 + 36 / 60.0, "New Zealand time - McMurdo, South Pole"],
    ];

    // Deprecated names in backward, which have no fields, with the canonical timezone they parse to.
    public static IEnumerable<object[]> DeprecatedName_TestCases =>
    [
        ["Asia/Calcutta", TzDataTimezone.Asia.Kolkata],
        ["US/Eastern", TzDataTimezone.America.New_York],
        ["Europe/Kiev", TzDataTimezone.Europe.Kyiv],
        ["America/Buenos_Aires", TzDataTimezone.America.Argentina.Buenos_Aires],
        ["Iceland", TzDataTimezone.Africa.Abidjan],
        ["NZ", TzDataTimezone.Pacific.Auckland],
    ];

    public static IEnumerable<object[]> InvalidInstance_TestCases =>
    [
        [default(TzDataTimezone)],
        [(TzDataTimezone)"Europe/Atlantis"],
        [(TzDataTimezone)"Asia/Calcutta"],
    ];

    private static IEnumerable<TzDataTimezone> GetTimezoneFields(Type hostType)
    {
        var fields = hostType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.FieldType == typeof(TzDataTimezone))
            .Select(field => (TzDataTimezone)field.GetValue(null)!);
        var nestedFields = hostType.GetNestedTypes(BindingFlags.Public)
            .SelectMany(GetTimezoneFields);

        return fields.Concat(nestedFields);
    }

    private static TzDataZoneLine[] GetZoneLineArray(TzDataTimezone timezone)
    {
        timezone.TryGetRegistryEntry(out var entry).Should().BeTrue("{0} is a timezone field", (string?)timezone);
        return entry!.ZoneLines;
    }

    [Fact]
    public void LinkFields_AreFound()
    {
        // act
        var testResult = TimezoneFields.Value.Values.Count(timezone => timezone.IsLink);

        // assert
        testResult.Should().BeGreaterThan(100, "zone.tab lists more than 100 timezones that are links");
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases))]
    public void Canonical_TimezoneField_IsCanonicalTimezoneField(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFields.Value[testIdentifier];

        // act
        var testResult = testTimezone.Canonical;

        // assert
        TimezoneFields.Value.Should().ContainKey((string)testResult!, "the canonical timezone of {0} is a timezone field", testIdentifier);
        testResult.IsLink.Should().BeFalse("chains of links resolve to the canonical timezone at their end, so the canonical timezone of {0} is not a link", testIdentifier);
        GetZoneLineArray(testTimezone).Should().BeSameAs(GetZoneLineArray(testResult), "{0} has the zone lines of its canonical timezone", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases))]
    public void Canonical_TimezoneField_IsItselfOnlyIfNotLink(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFields.Value[testIdentifier];

        // act
        var testIsLink = testTimezone.IsLink;
        var testCanonical = testTimezone.Canonical;

        // assert
        (testCanonical == testTimezone).Should().Be(!testIsLink, "{0} is its own canonical timezone only if it is not a link", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases))]
    public void IsLinkAndCanonical_CastInstance_ReturnSameValuesAsField(string testIdentifier)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var expectedResult = TimezoneFields.Value[testIdentifier];

        // act
        var testIsLink = testTimezone.IsLink;
        var testCanonical = testTimezone.Canonical;

        // assert
        testIsLink.Should().Be(expectedResult.IsLink, "a cast instance of {0} is a link only if its field is", testIdentifier);
        testCanonical.Should().Be(expectedResult.Canonical, "a cast instance of {0} has the canonical timezone of its field", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(ZoneTabLink_TestCases))]
    public void Members_ZoneTabLink_ReturnLinkAndLocationFromZoneTab(TzDataTimezone testTimezone, TzDataTimezone expectedCanonical, string expectedCountryCode, double expectedLatitude, double expectedLongitude, string? expectedComment)
    {
        // act
        var testIsLink = testTimezone.IsLink;
        var testCanonical = testTimezone.Canonical;

        // assert
        testIsLink.Should().BeTrue();
        testCanonical.Should().Be(expectedCanonical);
        testCanonical.IsLink.Should().BeFalse();
        GetZoneLineArray(testTimezone).Should().BeSameAs(GetZoneLineArray(expectedCanonical), "a link shares the zone lines array of its canonical timezone");
        testTimezone.ZoneLines.Should().Equal(expectedCanonical.ZoneLines);
        testTimezone.IsoCountryCodes.Should().Equal(expectedCountryCode);
        testTimezone.Latitude.Should().BeApproximately(expectedLatitude, 1e-9);
        testTimezone.Longitude.Should().BeApproximately(expectedLongitude, 1e-9);
        testTimezone.Comment.Should().Be(expectedComment);
    }

    [Fact]
    public void Equality_LinkAndCanonicalTimezone_AreNotEqualButShareCanonical()
    {
        // arrange
        var testLink = TzDataTimezone.Europe.Oslo;
        var testCanonical = TzDataTimezone.Europe.Berlin;

        // act
        var testEqual = testLink == testCanonical;
        var testCanonicalEqual = testLink.Canonical == testCanonical.Canonical;

        // assert
        testEqual.Should().BeFalse("equality is based on the identifier");
        testCanonicalEqual.Should().BeTrue("both have the zone lines of Europe/Berlin");
        ((string?)testLink).Should().Be("Europe/Oslo", "a link keeps its own identifier");
        testLink.ToString().Should().Be("Oslo");
    }

    [Fact]
    public void Canonical_CanonicalTimezone_ReturnsSameInstance()
    {
        // arrange
        var testTimezone = TzDataTimezone.Europe.Zurich;

        // act
        var testIsLink = testTimezone.IsLink;
        var testResult = testTimezone.Canonical;

        // assert
        testIsLink.Should().BeFalse();
        testResult.Should().Be(testTimezone);
        testResult.ZoneLines.Should().Equal(testTimezone.ZoneLines);
    }

    [Theory]
    [MemberData(nameof(DeprecatedName_TestCases))]
    public void Parse_DeprecatedName_ReturnsCanonicalField(string testName, TzDataTimezone expectedResult)
    {
        // act
        var testResult = TzDataTimezone.Parse(testName);

        // assert
        testResult.Should().Be(expectedResult, "{0} is a deprecated name for {1}", testName, (string?)expectedResult);
        testResult.IsLink.Should().BeFalse();
        testResult.ZoneLines.Should().Equal(expectedResult.ZoneLines);
        testResult.IsoCountryCodes.Should().Equal(expectedResult.IsoCountryCodes);
    }

    [Theory]
    [MemberData(nameof(DeprecatedName_TestCases))]
    public void TryParse_DeprecatedName_ReturnsTrueAndCanonicalField(string testName, TzDataTimezone expectedResult)
    {
        // act
        var testSuccess = TzDataTimezone.TryParse(testName, out var testResult);

        // assert
        testSuccess.Should().BeTrue("{0} is a deprecated name for {1}", testName, (string?)expectedResult);
        testResult.Should().Be(expectedResult);
        testResult.ZoneLines.Should().Equal(expectedResult.ZoneLines);
    }

    [Theory]
    [MemberData(nameof(DeprecatedLink_TestCases))]
    public void Parse_DeprecatedLink_ReturnsCanonicalFieldWithoutField(string testName)
    {
        // act
        var testResult = TzDataTimezone.Parse(testName);

        // assert
        TimezoneFields.Value.Should().NotContainKey(testName, "deprecated links have no fields");
        TimezoneFields.Value.Should().ContainKey((string)testResult!, "{0} parses to a timezone field", testName);
        testResult.IsLink.Should().BeFalse("{0} parses to the canonical timezone at the end of its chain of links", testName);
    }

    [Theory]
    [MemberData(nameof(InvalidInstance_TestCases))]
    public void IsLink_InvalidInstance_ThrowsInvalidOperationException(TzDataTimezone testTimezone)
    {
        // act
        var testAction = () => testTimezone.IsLink;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(InvalidInstance_TestCases))]
    public void Canonical_InvalidInstance_ThrowsInvalidOperationException(TzDataTimezone testTimezone)
    {
        // act
        var testAction = () => testTimezone.Canonical;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }
}
