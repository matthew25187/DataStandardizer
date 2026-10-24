using System.Globalization;
using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

/// <remarks>
/// The expected abbreviations are those reported by <c>zdump -v</c> for TZ Database 2026e, compiled by zic in its main format.
/// </remarks>
public class TzDataExtensionsAbbreviationTests
{
    private static IEnumerable<TzDataTimezone> GetTimezoneFields(Type hostType)
    {
        var fields = hostType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(field => field.FieldType == typeof(TzDataTimezone))
            .Select(field => (TzDataTimezone)field.GetValue(null)!);
        var nestedFields = hostType.GetNestedTypes(BindingFlags.Public)
            .SelectMany(GetTimezoneFields);

        return fields.Concat(nestedFields);
    }

    private static DateTime ParseUtc(string value) => DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);

    public static IEnumerable<object?[]> Abbreviation_TestCases
    {
        get
        {
            // America/New_York: the US rules, in winter and in summer.
            yield return new object?[] { "America/New_York", "2025-01-15T12:00:00", "EST", "EST", "EDT" };
            yield return new object?[] { "America/New_York", "2025-07-15T12:00:00", "EDT", "EST", "EDT" };
            // Europe/London: the EU rules, with the GMT/BST format.
            yield return new object?[] { "Europe/London", "2025-01-15T12:00:00", "GMT", "GMT", "BST" };
            yield return new object?[] { "Europe/London", "2025-07-15T12:00:00", "BST", "GMT", "BST" };
            // Australia/Sydney: the southern summer is daylight saving time.
            yield return new object?[] { "Australia/Sydney", "2025-01-15T12:00:00", "AEDT", "AEST", "AEDT" };
            yield return new object?[] { "Australia/Sydney", "2025-07-15T12:00:00", "AEST", "AEST", "AEDT" };
            // Europe/Dublin: negative daylight saving, so that winter time is daylight saving time.
            yield return new object?[] { "Europe/Dublin", "2025-01-15T12:00:00", "GMT", "IST", "GMT" };
            yield return new object?[] { "Europe/Dublin", "2025-07-15T12:00:00", "IST", "IST", "GMT" };
            // Numeric abbreviations, with and without minutes, and a named abbreviation for a half-hour offset.
            yield return new object?[] { "Asia/Dubai", "2025-07-15T12:00:00", "+04", "+04", null };
            yield return new object?[] { "Asia/Kathmandu", "2025-07-15T12:00:00", "+0545", "+0545", null };
            yield return new object?[] { "Asia/Kolkata", "2025-07-15T12:00:00", "IST", "IST", null };
            // Australia/Lord_Howe: a numeric abbreviation with 30 minutes of daylight saving.
            yield return new object?[] { "Australia/Lord_Howe", "2025-07-15T12:00:00", "+1030", "+1030", "+11" };
            // Asia/Tokyo: no daylight saving time since 1951.
            yield return new object?[] { "Asia/Tokyo", "2025-07-15T12:00:00", "JST", "JST", null };
            // America/New_York: local mean time, before standard time.
            yield return new object?[] { "America/New_York", "1880-07-15T12:00:00", "LMT", "LMT", null };
            // America/New_York: war time from February 1942, in force all year in 1943 and 1944, then peace time in 1945.
            yield return new object?[] { "America/New_York", "1942-01-15T12:00:00", "EST", "EST", "EWT" };
            yield return new object?[] { "America/New_York", "1943-01-15T12:00:00", "EWT", "EST", "EWT" };
            yield return new object?[] { "America/New_York", "1945-09-15T12:00:00", "EPT", "EST", "EPT" };
            yield return new object?[] { "America/New_York", "1946-01-15T12:00:00", "EST", "EST", "EDT" };
            // Europe/London: British Standard Time, +1 as standard time all year from 1968 to 1971.
            yield return new object?[] { "Europe/London", "1970-01-15T12:00:00", "BST", "BST", null };
            // Europe/London: before the first rule, standard time takes the letter of the first standard time rule.
            yield return new object?[] { "Europe/London", "1900-01-15T12:00:00", "GMT", "GMT", null };
            // Europe/Moscow: the standard time rule of October 1921 takes effect when the zone line ends, so it belongs to the next line.
            yield return new object?[] { "Europe/Moscow", "1921-01-15T12:00:00", "MSK", "MSK", "MSD" };
            // America/Argentina/Jujuy: a zone line that saves a fixed hour.
            yield return new object?[] { "America/Argentina/Jujuy", "1991-01-15T12:00:00", "-03", "-04", "-03" };
        }
    }

    [Theory]
    [MemberData(nameof(Abbreviation_TestCases))]
    public void AbbreviationMethods_Instant_ReturnAbbreviations(string testIdentifier, string testInstant, string expectedAbbreviation, string expectedStandardAbbreviation, string? expectedDaylightAbbreviation)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var testUtc = ParseUtc(testInstant);

        // act
        var testAbbreviation = testTimezone.GetAbbreviation(testUtc);
        var testStandardAbbreviation = testTimezone.GetStandardAbbreviation(testUtc);
        var testDaylightAbbreviation = testTimezone.GetDaylightAbbreviation(testUtc);

        // assert
        testAbbreviation.Should().Be(expectedAbbreviation, "{0} is known as {1} at {2}", testIdentifier, expectedAbbreviation, testInstant);
        testAbbreviation.Should().Be(testTimezone.GetOffsetInfo(testUtc).Abbreviation);
        testStandardAbbreviation.Should().Be(expectedStandardAbbreviation, "standard time in {0} is {1} at {2}", testIdentifier, expectedStandardAbbreviation, testInstant);
        testDaylightAbbreviation.Should().Be(expectedDaylightAbbreviation, "daylight saving time in {0} is {1} at {2}", testIdentifier, expectedDaylightAbbreviation ?? "not observed", testInstant);
    }

    [Theory]
    [MemberData(nameof(Abbreviation_TestCases))]
    public void AbbreviationMethods_DateTimeOffset_UseInstant(string testIdentifier, string testInstant, string expectedAbbreviation, string expectedStandardAbbreviation, string? expectedDaylightAbbreviation)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var testInstantWithOffset = new DateTimeOffset(ParseUtc(testInstant)).ToOffset(TimeSpan.FromHours(-9.5));

        // act & assert
        testTimezone.GetAbbreviation(testInstantWithOffset).Should().Be(expectedAbbreviation);
        testTimezone.GetStandardAbbreviation(testInstantWithOffset).Should().Be(expectedStandardAbbreviation);
        testTimezone.GetDaylightAbbreviation(testInstantWithOffset).Should().Be(expectedDaylightAbbreviation);
    }

    [Fact]
    public void AbbreviationMethods_WholeRange_ReturnAbbreviationForEveryTimezone()
    {
        // arrange
        var testInstants = new[] { DateTime.MinValue, new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc), DateTime.MaxValue };
        var testTimezones = GetTimezoneFields(typeof(TzDataTimezone));

        foreach (var testTimezone in testTimezones)
        {
            foreach (var testInstant in testInstants)
            {
                // act
                var testStandardAbbreviation = testTimezone.GetStandardAbbreviation(testInstant);
                var testDaylightAbbreviation = testTimezone.GetDaylightAbbreviation(testInstant);

                // assert
                testStandardAbbreviation.Should().NotBeNullOrEmpty("{0} has a standard abbreviation at {1:O}", testTimezone, testInstant);
                testDaylightAbbreviation.Should().NotBe(string.Empty, "{0} has no empty daylight abbreviation at {1:O}", testTimezone, testInstant);
            }
        }
    }
}
