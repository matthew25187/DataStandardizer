using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TzDataTimezoneConversionTests
{
    private const string UnknownIdentifier = "Europe/Atlantis";
    private const string NotKnownBecause = "'{0}' is not the identifier of a known timezone";

    // A link listed in zone.tab of TZ Database 2026e, which has a field of its own.
    private const string OsloIdentifier = "Europe/Oslo";

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

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void ExplicitConversion_TimezoneIdentifier_ReturnsField(string testIdentifier)
    {
        // arrange
        var expectedResult = TimezoneFieldCatalog.Timezones[testIdentifier];

        // act
        var testResult = (TzDataTimezone)testIdentifier;

        // assert
        testResult.Should().Be(expectedResult, "{0} converts to the field with that identifier", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void ZoneLines_ConvertedTimezoneIdentifier_AreThoseOfField(string testIdentifier)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var expectedResult = TimezoneFieldCatalog.Timezones[testIdentifier].ZoneLines;

        // act
        var testResult = testTimezone.ZoneLines;

        // assert
        testResult.Should().Equal(expectedResult, (convertedZoneLine, fieldZoneLine) => ReferenceEquals(convertedZoneLine, fieldZoneLine), "{0} converts to a timezone with the zone lines of its field", testIdentifier);
    }

    [Fact]
    public void ExplicitConversion_ZoneTabLink_ReturnsLinkField()
    {
        // act
        var testResult = (TzDataTimezone)OsloIdentifier;

        // assert
        testResult.Should().Be(TzDataTimezone.Europe.Oslo, "a link listed in zone.tab has a field of its own");
    }

    [Theory]
    [MemberData(nameof(UnknownIdentifier_TestCases))]
    public void ExplicitConversion_UnknownIdentifier_ThrowsInvalidCastException(string testIdentifier)
    {
        // act
        Func<TzDataTimezone> testAction = () => (TzDataTimezone)testIdentifier;

        // assert
        testAction.Should().Throw<InvalidCastException>(NotKnownBecause, testIdentifier);
    }

    [Fact]
    public void ExplicitConversion_Null_ThrowsInvalidCastException()
    {
        // act
        Func<TzDataTimezone> testAction = () => (TzDataTimezone)(string)null!;

        // assert
        testAction.Should().Throw<InvalidCastException>();
    }
}
