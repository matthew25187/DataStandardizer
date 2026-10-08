using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TzDataTimezoneParseTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, FieldInfo>> TimezoneFields = new(() => GetTimezoneFields(typeof(TzDataTimezone)).ToDictionary(GetIdentifier, StringComparer.Ordinal));

    public static IEnumerable<object[]> TimezoneField_TestCases => TimezoneFields.Value.Keys.Select(identifier => new object[] { identifier });

    public static IEnumerable<object[]> UnknownIdentifier_TestCases =>
    [
        [string.Empty],
        [" "],
        ["Europe/Atlantis"],
        ["europe/zurich"],
        ["EUROPE/ZURICH"],
        [" Europe/Zurich"],
        ["Europe/Zurich "],
        ["Europe"],
        ["Zurich"],
    ];

    private static string GetIdentifier(FieldInfo field)
    {
        string? identifier = (TzDataTimezone)field.GetValue(null)!;
        return identifier!;
    }

    private static IEnumerable<FieldInfo> GetTimezoneFields(Type hostType)
    {
        var fields = hostType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.FieldType == typeof(TzDataTimezone));
        var nestedFields = hostType.GetNestedTypes(BindingFlags.Public)
            .SelectMany(GetTimezoneFields);

        return fields.Concat(nestedFields);
    }

    private static T ParseAs<T>(string s) where T : IParsable<T>
    {
        return T.Parse(s, null);
    }

    private static bool TryParseAs<T>(string? s, out T? result) where T : IParsable<T>
    {
        return T.TryParse(s, null, out result);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases))]
    public void Parse_TimezoneIdentifier_ReturnsField(string testIdentifier)
    {
        // arrange
        var expectedResult = (TzDataTimezone)TimezoneFields.Value[testIdentifier].GetValue(null)!;

        // act
        var testResult = TzDataTimezone.Parse(testIdentifier);

        // assert
        testResult.Should().Be(expectedResult, "{0} parses to the field with that identifier", testIdentifier);
        testResult.ZoneLines.Should().Equal(expectedResult.ZoneLines, "{0} parses to a timezone with the zone lines of its field", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases))]
    public void TryParse_TimezoneIdentifier_ReturnsTrueAndField(string testIdentifier)
    {
        // arrange
        var expectedResult = (TzDataTimezone)TimezoneFields.Value[testIdentifier].GetValue(null)!;

        // act
        var testSuccess = TzDataTimezone.TryParse(testIdentifier, out var testResult);

        // assert
        testSuccess.Should().BeTrue("{0} is the identifier of a known timezone", testIdentifier);
        testResult.Should().Be(expectedResult, "{0} parses to the field with that identifier", testIdentifier);
        testResult.ZoneLines.Should().Equal(expectedResult.ZoneLines, "{0} parses to a timezone with the zone lines of its field", testIdentifier);
    }

    [Fact]
    public void Parse_EuropeZurich_ReturnsTimezoneWithMetadata()
    {
        // arrange
        var testIdentifier = "Europe/Zurich";

        // act
        var testResult = TzDataTimezone.Parse(testIdentifier);

        // assert
        testResult.Should().Be(TzDataTimezone.Europe.Zurich);
        testResult.ZoneLines.Should().Equal(TzDataTimezone.Europe.Zurich.ZoneLines);
        testResult.IsoCountryCodes.Should().Equal(TzDataTimezone.Europe.Zurich.IsoCountryCodes);
        testResult.Latitude.Should().Be(TzDataTimezone.Europe.Zurich.Latitude);
        testResult.Longitude.Should().Be(TzDataTimezone.Europe.Zurich.Longitude);
    }

    [Theory]
    [MemberData(nameof(UnknownIdentifier_TestCases))]
    public void Parse_UnknownIdentifier_ThrowsFormatException(string testIdentifier)
    {
        // act
        var testAction = () => TzDataTimezone.Parse(testIdentifier);

        // assert
        testAction.Should().Throw<FormatException>("'{0}' is not the identifier of a known timezone", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(UnknownIdentifier_TestCases))]
    public void TryParse_UnknownIdentifier_ReturnsFalseAndDefault(string testIdentifier)
    {
        // act
        var testSuccess = TzDataTimezone.TryParse(testIdentifier, out var testResult);

        // assert
        testSuccess.Should().BeFalse("'{0}' is not the identifier of a known timezone", testIdentifier);
        testResult.Should().Be(default(TzDataTimezone));
    }

    [Fact]
    public void Parse_Null_ThrowsArgumentNullException()
    {
        // act
        var testAction = () => TzDataTimezone.Parse(null!);

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
        var testResult = ParseAs<TzDataTimezone>("America/Argentina/Buenos_Aires");

        // assert
        testResult.Should().Be(TzDataTimezone.America.Argentina.Buenos_Aires);
        testResult.ZoneLines.Should().Equal(TzDataTimezone.America.Argentina.Buenos_Aires.ZoneLines);
    }

    [Fact]
    public void ParseAs_UnknownIdentifier_ThrowsFormatException()
    {
        // act
        var testAction = () => ParseAs<TzDataTimezone>("Europe/Atlantis");

        // assert
        testAction.Should().Throw<FormatException>();
    }

    [Fact]
    public void TryParseAs_TimezoneIdentifier_ReturnsTrueAndField()
    {
        // act
        var testSuccess = TryParseAs<TzDataTimezone>("Pacific/Auckland", out var testResult);

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
