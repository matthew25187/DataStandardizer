using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

// TzDataTimezoneAttribute and the extensions that read it are obsolete, but remain the reference for the new members.
#pragma warning disable CS0618

public class TzDataTimezoneLocationTests
{
    private const string UnknownIdentifier = "Europe/Atlantis";

    // The precision to which a coordinate in degrees and minutes is compared, after conversion to decimal degrees.
    private const double CoordinateTolerance = 1e-9;

    // Europe/Zurich in zone1970.tab of TZ Database 2026e: the ISO 6709 coordinates +4723+00832 in decimal degrees,
    // the country codes CH,DE,LI (ISO 3166-1 alpha-2 codes) and the comment.
    private const double ZurichLatitude = 47 + 23 / 60D;
    private const double ZurichLongitude = 8 + 32 / 60D;
    private const string Switzerland = "CH";
    private const string Germany = "DE";
    private const string Liechtenstein = "LI";
    private const string ZurichComment = "Büsingen";

    private const string ObsoleteAttributeMessage = "Use the Latitude, Longitude, IsoCountryCodes and Comment members of TzDataTimezone instead.";

    // A field and a cast instance of every timezone field, with the attribute of the field.
    private static IEnumerable<(string Instance, TzDataTimezone Timezone, TzDataTimezoneAttribute Attribute)> GetFieldAndCastInstances()
    {
        foreach (var field in TimezoneFieldCatalog.Fields)
        {
            var identifier = TimezoneFieldCatalog.GetIdentifier(field);
            var attribute = field.GetCustomAttribute<TzDataTimezoneAttribute>()!;
            yield return ($"field {identifier}", (TzDataTimezone)field.GetValue(null)!, attribute);
            yield return ($"cast instance {identifier}", (TzDataTimezone)identifier, attribute);
        }
    }

    public static TheoryData<string, TzDataTimezone, double> AttributeLatitude_TestCases
    {
        get
        {
            var testCases = new TheoryData<string, TzDataTimezone, double>();
            foreach (var (instance, timezone, attribute) in GetFieldAndCastInstances())
                testCases.Add(instance, timezone, attribute.Latitude);

            return testCases;
        }
    }

    public static TheoryData<string, TzDataTimezone, double> AttributeLongitude_TestCases
    {
        get
        {
            var testCases = new TheoryData<string, TzDataTimezone, double>();
            foreach (var (instance, timezone, attribute) in GetFieldAndCastInstances())
                testCases.Add(instance, timezone, attribute.Longitude);

            return testCases;
        }
    }

    public static TheoryData<string, TzDataTimezone, string[]> AttributeIsoCountryCodes_TestCases
    {
        get
        {
            var testCases = new TheoryData<string, TzDataTimezone, string[]>();
            foreach (var (instance, timezone, attribute) in GetFieldAndCastInstances())
                testCases.Add(instance, timezone, attribute.IsoCountryCodes);

            return testCases;
        }
    }

    public static TheoryData<string, TzDataTimezone, string?> AttributeComment_TestCases
    {
        get
        {
            var testCases = new TheoryData<string, TzDataTimezone, string?>();
            foreach (var (instance, timezone, attribute) in GetFieldAndCastInstances())
                testCases.Add(instance, timezone, attribute.Comment);

            return testCases;
        }
    }

    [Theory]
    [MemberData(nameof(AttributeLatitude_TestCases), DisableDiscoveryEnumeration = true)]
    public void Latitude_TimezoneFieldOrCastInstance_ReturnsLatitudeOfAttribute(string testInstance, TzDataTimezone testTimezone, double expectedResult)
    {
        // act
        var testResult = testTimezone.Latitude;

        // assert
        testResult.Should().Be(expectedResult, "the latitude of {0} is that of the attribute of its field", testInstance);
    }

    [Theory]
    [MemberData(nameof(AttributeLongitude_TestCases), DisableDiscoveryEnumeration = true)]
    public void Longitude_TimezoneFieldOrCastInstance_ReturnsLongitudeOfAttribute(string testInstance, TzDataTimezone testTimezone, double expectedResult)
    {
        // act
        var testResult = testTimezone.Longitude;

        // assert
        testResult.Should().Be(expectedResult, "the longitude of {0} is that of the attribute of its field", testInstance);
    }

    [Theory]
    [MemberData(nameof(AttributeIsoCountryCodes_TestCases), DisableDiscoveryEnumeration = true)]
    public void IsoCountryCodes_TimezoneFieldOrCastInstance_ReturnsCountryCodesOfAttributeInOrder(string testInstance, TzDataTimezone testTimezone, string[] expectedResult)
    {
        // act
        var testResult = testTimezone.IsoCountryCodes;

        // assert
        testResult.Should().Equal(expectedResult, "the country codes of {0} are those of the attribute of its field, in order", testInstance);
    }

    [Theory]
    [MemberData(nameof(AttributeComment_TestCases), DisableDiscoveryEnumeration = true)]
    public void Comment_TimezoneFieldOrCastInstance_ReturnsCommentOfAttribute(string testInstance, TzDataTimezone testTimezone, string? expectedResult)
    {
        // act
        var testResult = testTimezone.Comment;

        // assert
        testResult.Should().Be(expectedResult, "the comment of {0} is that of the attribute of its field", testInstance);
    }

    [Fact]
    public void Latitude_EuropeZurich_ReturnsLatitudeFromZone1970Tab()
    {
        // act
        var testResult = TzDataTimezone.Europe.Zurich.Latitude;

        // assert
        testResult.Should().BeApproximately(ZurichLatitude, CoordinateTolerance, "zone1970.tab gives the latitude of Zurich as +4723");
    }

    [Fact]
    public void Longitude_EuropeZurich_ReturnsLongitudeFromZone1970Tab()
    {
        // act
        var testResult = TzDataTimezone.Europe.Zurich.Longitude;

        // assert
        testResult.Should().BeApproximately(ZurichLongitude, CoordinateTolerance, "zone1970.tab gives the longitude of Zurich as +00832");
    }

    [Fact]
    public void IsoCountryCodes_EuropeZurich_ReturnsCountryCodesFromZone1970Tab()
    {
        // act
        var testResult = TzDataTimezone.Europe.Zurich.IsoCountryCodes;

        // assert
        testResult.Should().Equal([Switzerland, Germany, Liechtenstein], "zone1970.tab lists CH,DE,LI for Europe/Zurich");
    }

    [Fact]
    public void Comment_EuropeZurich_ReturnsCommentFromZone1970Tab()
    {
        // act
        var testResult = TzDataTimezone.Europe.Zurich.Comment;

        // assert
        testResult.Should().Be(ZurichComment, "zone1970.tab gives Europe/Zurich the comment Büsingen");
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
    public void IsoCountryCodes_AsiaDubai_IsNotWritableArray()
    {
        // act
        var testResult = TzDataTimezone.Asia.Dubai.IsoCountryCodes;

        // assert
        testResult.Should().NotBeAssignableTo<string[]>("the country codes are shared, and must not be modified");
    }

    public static TheoryData<string, Func<TzDataTimezone, object?>> Member_TestCases => new()
    {
        { nameof(TzDataTimezone.Latitude), timezone => timezone.Latitude },
        { nameof(TzDataTimezone.Longitude), timezone => timezone.Longitude },
        { nameof(TzDataTimezone.IsoCountryCodes), timezone => timezone.IsoCountryCodes },
        { nameof(TzDataTimezone.Comment), timezone => timezone.Comment },
    };

    [Theory]
    [MemberData(nameof(Member_TestCases))]
    public void Member_DefaultInstance_ThrowsInvalidOperationException(string testMemberName, Func<TzDataTimezone, object?> testMember)
    {
        // arrange
        var testTimezone = default(TzDataTimezone);

        // act
        Action testAction = () => testMember(testTimezone);

        // assert
        testAction.Should().Throw<InvalidOperationException>("the default instance has no identifier, so {0} cannot be found", testMemberName);
    }

    [Theory]
    [MemberData(nameof(Member_TestCases))]
    public void Member_UnknownIdentifier_ThrowsInvalidOperationException(string testMemberName, Func<TzDataTimezone, object?> testMember)
    {
        // arrange
        var testTimezone = (TzDataTimezone)UnknownIdentifier;

        // act
        Action testAction = () => testMember(testTimezone);

        // assert
        testAction.Should().Throw<InvalidOperationException>("no timezone field has the identifier, so {0} cannot be found", testMemberName);
    }

    public static TheoryData<TzDataTimezone> InvalidInstance_TestCases => new()
    {
        default(TzDataTimezone),
        (TzDataTimezone)UnknownIdentifier,
    };

    [Theory]
    [MemberData(nameof(InvalidInstance_TestCases))]
    public void GetLatitude_InvalidInstance_ReturnsZero(TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.GetLatitude();

        // assert
        testResult.Should().Be(0, "the obsolete accessor returns 0 for an unknown timezone");
    }

    [Theory]
    [MemberData(nameof(InvalidInstance_TestCases))]
    public void GetLongitude_InvalidInstance_ReturnsZero(TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.GetLongitude();

        // assert
        testResult.Should().Be(0, "the obsolete accessor returns 0 for an unknown timezone");
    }

    [Theory]
    [MemberData(nameof(InvalidInstance_TestCases))]
    public void GetIsoCountryCodes_InvalidInstance_ReturnsNoCountryCodes(TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.GetIsoCountryCodes();

        // assert
        testResult.Should().BeEmpty("the obsolete accessor returns no country codes for an unknown timezone");
    }

    [Theory]
    [MemberData(nameof(InvalidInstance_TestCases))]
    public void GetComment_InvalidInstance_ReturnsNull(TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.GetComment();

        // assert
        testResult.Should().BeNull("the obsolete accessor returns null for an unknown timezone");
    }

    [Fact]
    public void GetCustomAttribute_TzDataTimezoneAttribute_ReturnsObsoleteWarning()
    {
        // act
        var testResult = typeof(TzDataTimezoneAttribute).GetCustomAttribute<ObsoleteAttribute>();

        // assert
        testResult.Should().NotBeNull("the attribute is replaced by members of TzDataTimezone");
        testResult!.Message.Should().Be(ObsoleteAttributeMessage);
        testResult.IsError.Should().BeFalse("the attribute is deprecated, not yet removed");
    }

    [Theory]
    [InlineData(nameof(TzDataExtensions.GetComment), "Use TzDataTimezone.Comment instead.")]
    [InlineData(nameof(TzDataExtensions.GetIsoCountryCodes), "Use TzDataTimezone.IsoCountryCodes instead.")]
    [InlineData(nameof(TzDataExtensions.GetLatitude), "Use TzDataTimezone.Latitude instead.")]
    [InlineData(nameof(TzDataExtensions.GetLongitude), "Use TzDataTimezone.Longitude instead.")]
    public void GetCustomAttribute_ObsoleteExtension_ReturnsObsoleteWarning(string testMethodName, string expectedMessage)
    {
        // arrange
        var testMethod = typeof(TzDataExtensions).GetMethod(testMethodName, [typeof(TzDataTimezone)])!;

        // act
        var testResult = testMethod.GetCustomAttribute<ObsoleteAttribute>();

        // assert
        testResult.Should().NotBeNull("{0} is replaced by a member of TzDataTimezone", testMethodName);
        testResult!.Message.Should().Be(expectedMessage);
        testResult.IsError.Should().BeFalse("{0} is deprecated, not yet removed", testMethodName);
    }
}
