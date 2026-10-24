using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

// TzDataTimezoneAttribute and the extensions that read it are obsolete, but remain the reference for the new members.
#pragma warning disable CS0618

public class TzDataTimezoneLocationTests
{
    private static readonly Lazy<IReadOnlyDictionary<string, FieldInfo>> TimezoneFields = new(() => GetTimezoneFields(typeof(TzDataTimezone)).ToDictionary(GetIdentifier, StringComparer.Ordinal));

    public static IEnumerable<object[]> TimezoneField_TestCases => TimezoneFields.Value.Keys.Select(identifier => new object[] { identifier });

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

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases))]
    public void Members_TimezoneField_ReturnSameValuesAsAttribute(string testIdentifier)
    {
        // arrange
        var testField = TimezoneFields.Value[testIdentifier];
        var testTimezone = (TzDataTimezone)testField.GetValue(null)!;
        var expectedResult = testField.GetCustomAttribute<TzDataTimezoneAttribute>()!;

        // act
        var testLatitude = testTimezone.Latitude;
        var testLongitude = testTimezone.Longitude;
        var testIsoCountryCodes = testTimezone.IsoCountryCodes;
        var testComment = testTimezone.Comment;

        // assert
        testLatitude.Should().Be(expectedResult.Latitude, "the latitude of {0} is that of its attribute", testIdentifier);
        testLongitude.Should().Be(expectedResult.Longitude, "the longitude of {0} is that of its attribute", testIdentifier);
        testIsoCountryCodes.Should().Equal(expectedResult.IsoCountryCodes, "the country codes of {0} are those of its attribute, in order", testIdentifier);
        testComment.Should().Be(expectedResult.Comment, "the comment of {0} is that of its attribute", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases))]
    public void Members_CastInstance_ReturnSameValuesAsField(string testIdentifier)
    {
        // arrange
        var testTimezone = (TzDataTimezone)testIdentifier;
        var expectedResult = (TzDataTimezone)TimezoneFields.Value[testIdentifier].GetValue(null)!;

        // act
        var testLatitude = testTimezone.Latitude;
        var testLongitude = testTimezone.Longitude;
        var testIsoCountryCodes = testTimezone.IsoCountryCodes;
        var testComment = testTimezone.Comment;

        // assert
        testLatitude.Should().Be(expectedResult.Latitude, "a cast instance has the latitude of the field with the same identifier");
        testLongitude.Should().Be(expectedResult.Longitude, "a cast instance has the longitude of the field with the same identifier");
        testIsoCountryCodes.Should().Equal(expectedResult.IsoCountryCodes, "a cast instance has the country codes of the field with the same identifier");
        testComment.Should().Be(expectedResult.Comment, "a cast instance has the comment of the field with the same identifier");
    }

    [Fact]
    public void Members_EuropeZurich_ReturnExpectedValues()
    {
        // arrange
        var testTimezone = TzDataTimezone.Europe.Zurich;

        // act
        var testLatitude = testTimezone.Latitude;
        var testLongitude = testTimezone.Longitude;
        var testIsoCountryCodes = testTimezone.IsoCountryCodes;
        var testComment = testTimezone.Comment;

        // assert
        testLatitude.Should().BeApproximately(47 + 23 / 60D, 1e-9, "zone1970.tab gives the latitude of Zurich as +4723");
        testLongitude.Should().BeApproximately(8 + 32 / 60D, 1e-9, "zone1970.tab gives the longitude of Zurich as +00832");
        testIsoCountryCodes.Should().Equal(new[] { "CH", "DE", "LI" }, "zone1970.tab lists CH,DE,LI for Europe/Zurich");
        testComment.Should().Be("Büsingen", "zone1970.tab gives Europe/Zurich the comment Büsingen");
    }

    [Fact]
    public void Comment_TimezoneHasNoComment_ReturnsNull()
    {
        // act
        var testResult = TzDataTimezone.Europe.Andorra.Comment;

        // assert
        testResult.Should().BeNull("zone1970.tab gives Europe/Andorra no comment");
    }

    [Fact]
    public void IsoCountryCodes_IsReadOnly()
    {
        // act
        var testResult = TzDataTimezone.Asia.Dubai.IsoCountryCodes;

        // assert
        testResult.Should().NotBeAssignableTo<string[]>("the country codes are shared, and must not be modified");
    }

    public static IEnumerable<object[]> Member_TestCases
    {
        get
        {
            yield return new object[] { nameof(TzDataTimezone.Latitude), new Func<TzDataTimezone, object?>(timezone => timezone.Latitude) };
            yield return new object[] { nameof(TzDataTimezone.Longitude), new Func<TzDataTimezone, object?>(timezone => timezone.Longitude) };
            yield return new object[] { nameof(TzDataTimezone.IsoCountryCodes), new Func<TzDataTimezone, object?>(timezone => timezone.IsoCountryCodes) };
            yield return new object[] { nameof(TzDataTimezone.Comment), new Func<TzDataTimezone, object?>(timezone => timezone.Comment) };
        }
    }

    [Theory]
    [MemberData(nameof(Member_TestCases))]
    public void Member_DefaultInstance_ThrowsInvalidOperationException(string testMemberName, Func<TzDataTimezone, object?> testMember)
    {
        // arrange
        var testTimezone = default(TzDataTimezone);

        // act
        var testAction = () => testMember(testTimezone);

        // assert
        testAction.Should().Throw<InvalidOperationException>("the default instance has no identifier, so {0} cannot be found", testMemberName);
    }

    [Theory]
    [MemberData(nameof(Member_TestCases))]
    public void Member_UnknownIdentifier_ThrowsInvalidOperationException(string testMemberName, Func<TzDataTimezone, object?> testMember)
    {
        // arrange
        var testTimezone = (TzDataTimezone)"Europe/Atlantis";

        // act
        var testAction = () => testMember(testTimezone);

        // assert
        testAction.Should().Throw<InvalidOperationException>("no timezone field has the identifier, so {0} cannot be found", testMemberName);
    }

    [Fact]
    public void ObsoleteExtensions_DefaultInstance_ReturnDefaults()
    {
        // arrange
        var testTimezone = default(TzDataTimezone);

        // act and assert
        testTimezone.GetLatitude().Should().Be(0, "the obsolete accessor returns 0 for an unknown timezone");
        testTimezone.GetLongitude().Should().Be(0, "the obsolete accessor returns 0 for an unknown timezone");
        testTimezone.GetIsoCountryCodes().Should().BeEmpty("the obsolete accessor returns no country codes for an unknown timezone");
        testTimezone.GetComment().Should().BeNull("the obsolete accessor returns null for an unknown timezone");
    }

    [Fact]
    public void ObsoleteExtensions_UnknownIdentifier_ReturnDefaults()
    {
        // arrange
        var testTimezone = (TzDataTimezone)"Europe/Atlantis";

        // act and assert
        testTimezone.GetLatitude().Should().Be(0, "the obsolete accessor returns 0 for an unknown timezone");
        testTimezone.GetLongitude().Should().Be(0, "the obsolete accessor returns 0 for an unknown timezone");
        testTimezone.GetIsoCountryCodes().Should().BeEmpty("the obsolete accessor returns no country codes for an unknown timezone");
        testTimezone.GetComment().Should().BeNull("the obsolete accessor returns null for an unknown timezone");
    }

    [Fact]
    public void TzDataTimezoneAttribute_IsObsolete()
    {
        // act
        var testResult = typeof(TzDataTimezoneAttribute).GetCustomAttribute<ObsoleteAttribute>();

        // assert
        testResult.Should().NotBeNull("the attribute is replaced by members of TzDataTimezone");
        testResult!.Message.Should().Be("Use the Latitude, Longitude, IsoCountryCodes and Comment members of TzDataTimezone instead.");
        testResult.IsError.Should().BeFalse("the attribute is deprecated, not yet removed");
    }

    [Theory]
    [InlineData(nameof(TzDataExtensions.GetComment), "Use TzDataTimezone.Comment instead.")]
    [InlineData(nameof(TzDataExtensions.GetIsoCountryCodes), "Use TzDataTimezone.IsoCountryCodes instead.")]
    [InlineData(nameof(TzDataExtensions.GetLatitude), "Use TzDataTimezone.Latitude instead.")]
    [InlineData(nameof(TzDataExtensions.GetLongitude), "Use TzDataTimezone.Longitude instead.")]
    public void ObsoleteExtension_IsObsolete(string testMethodName, string expectedMessage)
    {
        // arrange
        var testMethod = typeof(TzDataExtensions).GetMethod(testMethodName, new[] { typeof(TzDataTimezone) })!;

        // act
        var testResult = testMethod.GetCustomAttribute<ObsoleteAttribute>();

        // assert
        testResult.Should().NotBeNull("{0} is replaced by a member of TzDataTimezone", testMethodName);
        testResult!.Message.Should().Be(expectedMessage);
        testResult.IsError.Should().BeFalse("{0} is deprecated, not yet removed", testMethodName);
    }
}
