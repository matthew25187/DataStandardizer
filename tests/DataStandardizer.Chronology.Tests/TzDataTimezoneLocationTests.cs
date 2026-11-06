using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TzDataTimezoneLocationTests
{

    // The precision to which a coordinate in degrees and minutes is compared, after conversion to decimal degrees.
    private const double CoordinateTolerance = 1e-9;

    // Europe/Zurich in zone1970.tab of TZ Database 2026e: the identifier, the ISO 6709 coordinates +4723+00832 in
    // decimal degrees, and the country codes CH,DE,LI (ISO 3166-1 alpha-2 codes).
    private const string ZurichIdentifier = "Europe/Zurich";
    private const double ZurichLatitude = 47 + 23 / 60D;
    private const double ZurichLongitude = 8 + 32 / 60D;
    private const string Switzerland = "CH";
    private const string Germany = "DE";
    private const string Liechtenstein = "LI";

    // The comments of zone1970.tab and zone.tab, in TZ Database 2026e.
    private const string ZurichComment = "Büsingen";
    private const string BuenosAiresComment = "Buenos Aires (BA, CF)";

    // The country codes of zone1970.tab, in TZ Database 2026e (ISO 3166-1 alpha-2 codes).
    private const string Australia = "AU";
    private const string UnitedArabEmirates = "AE";
    private const string Oman = "OM";
    private const string Reunion = "RE";
    private const string Seychelles = "SC";
    private const string FrenchSouthernTerritories = "TF";

    // The coordinates of zone1970.tab, in TZ Database 2026e, in decimal degrees: Adak (+515248), New Salem (+465042),
    // Tallinn (+2445) and Buenos Aires (-05827).
    private const double AdakLatitude = 51.88D;
    private const double NewSalemLatitude = 46.845D;
    private const double TallinnLongitude = 24.75D;
    private const double BuenosAiresLongitude = -58.45D;

    // The latitude and longitude of Etc/UTC, which is defined in the etcetera file of TZ Database 2026e and has no location.
    private const double NoLocationCoordinate = 0D;

    public static TheoryData<TzDataTimezone, string> Comment_TimezoneHasComment_TestCases => new()
    {
        { TzDataTimezone.Europe.Zurich, ZurichComment },
        { TzDataTimezone.America.Argentina.Buenos_Aires, BuenosAiresComment },
    };

    public static TheoryData<TzDataTimezone> Comment_TimezoneHasNoComment_TestCases => new()
    {
        TzDataTimezone.Europe.Andorra,
        TzDataTimezone.Africa.Cairo,
    };

    public static TheoryData<TzDataTimezone, string[]> IsoCountryCodes_TestCases => new()
    {
        { TzDataTimezone.Australia.Darwin, [Australia] },
        { TzDataTimezone.Asia.Dubai, [UnitedArabEmirates, Oman, Reunion, Seychelles, FrenchSouthernTerritories] },
    };

    public static TheoryData<TzDataTimezone, double> Latitude_TestCases => new()
    {
        { TzDataTimezone.America.Adak, AdakLatitude },
        { TzDataTimezone.America.North_Dakota.New_Salem, NewSalemLatitude },
    };

    public static TheoryData<TzDataTimezone, double> Longitude_TestCases => new()
    {
        { TzDataTimezone.Europe.Tallinn, TallinnLongitude },
        { TzDataTimezone.America.Argentina.Buenos_Aires, BuenosAiresLongitude },
    };

    // Europe/Zurich as its field, and as converted from its identifier, which returns the field.
    public static TheoryData<string, TzDataTimezone> Zurich_TestCases => new()
    {
        { "field", TzDataTimezone.Europe.Zurich },
        { "converted identifier", (TzDataTimezone)ZurichIdentifier },
    };

    [Theory]
    [MemberData(nameof(Comment_TimezoneHasComment_TestCases))]
    public void Comment_TimezoneHasComment_ReturnsComment(TzDataTimezone testTimezone, string expectedResult)
    {
        // act
        var testResult = testTimezone.Comment;

        // assert
        testResult.Should().Be(expectedResult, "the comment for timezone {0} is {1}", testTimezone, expectedResult);
    }

    [Theory]
    [MemberData(nameof(Comment_TimezoneHasNoComment_TestCases))]
    public void Comment_TimezoneHasNoComment_ReturnsNull(TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.Comment;

        // assert
        testResult.Should().BeNull("there is no comment for timezone {0}", testTimezone);
    }

    [Theory]
    [MemberData(nameof(IsoCountryCodes_TestCases))]
    public void IsoCountryCodes_Timezone_ReturnsCountryCodesForTimezoneInOrder(TzDataTimezone testTimezone, string[] expectedResult)
    {
        // act
        var testResult = testTimezone.IsoCountryCodes;

        // assert
        testResult.Should().Equal(expectedResult, "these are the country codes associated with timezone {0}, in the order of zone1970.tab", testTimezone);
    }

    [Theory]
    [MemberData(nameof(Latitude_TestCases))]
    public void Latitude_Timezone_ReturnsLatitudeOfPrincipalLocation(TzDataTimezone testTimezone, double expectedResult)
    {
        // act
        var testResult = testTimezone.Latitude;

        // assert
        testResult.Should().Be(expectedResult, "{0} is the latitude of the principal location for timezone {1}", expectedResult, testTimezone);
    }

    [Theory]
    [MemberData(nameof(Longitude_TestCases))]
    public void Longitude_Timezone_ReturnsLongitudeOfPrincipalLocation(TzDataTimezone testTimezone, double expectedResult)
    {
        // act
        var testResult = testTimezone.Longitude;

        // assert
        testResult.Should().Be(expectedResult, "{0} is the longitude of the principal location for timezone {1}", expectedResult, testTimezone);
    }

    [Theory]
    [MemberData(nameof(Zurich_TestCases))]
    public void Latitude_EuropeZurich_ReturnsLatitudeFromZone1970Tab(string testInstance, TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.Latitude;

        // assert
        testResult.Should().BeApproximately(ZurichLatitude, CoordinateTolerance, "zone1970.tab gives the latitude of Zurich, the {0}, as +4723", testInstance);
    }

    [Theory]
    [MemberData(nameof(Zurich_TestCases))]
    public void Longitude_EuropeZurich_ReturnsLongitudeFromZone1970Tab(string testInstance, TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.Longitude;

        // assert
        testResult.Should().BeApproximately(ZurichLongitude, CoordinateTolerance, "zone1970.tab gives the longitude of Zurich, the {0}, as +00832", testInstance);
    }

    [Theory]
    [MemberData(nameof(Zurich_TestCases))]
    public void IsoCountryCodes_EuropeZurich_ReturnsCountryCodesFromZone1970Tab(string testInstance, TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.IsoCountryCodes;

        // assert
        testResult.Should().Equal([Switzerland, Germany, Liechtenstein], "zone1970.tab lists CH,DE,LI for Europe/Zurich, the {0}", testInstance);
    }

    [Theory]
    [MemberData(nameof(Zurich_TestCases))]
    public void Comment_EuropeZurich_ReturnsCommentFromZone1970Tab(string testInstance, TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.Comment;

        // assert
        testResult.Should().Be(ZurichComment, "zone1970.tab gives Europe/Zurich, the {0}, the comment Büsingen", testInstance);
    }

    [Fact]
    public void IsoCountryCodes_AsiaDubai_IsNotWritableArray()
    {
        // act
        var testResult = TzDataTimezone.Asia.Dubai.IsoCountryCodes;

        // assert
        testResult.Should().NotBeAssignableTo<string[]>("the country codes are shared, and must not be modified");
    }

    public static TheoryData<string, TzDataTimezone> EtcUtc_TestCases => TimezoneFieldCatalog.CreateEtcUtcTestCases();

    [Theory]
    [MemberData(nameof(EtcUtc_TestCases))]
    public void Latitude_EtcUtc_ReturnsZero(string testInstance, TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.Latitude;

        // assert
        testResult.Should().Be(NoLocationCoordinate, "Etc/UTC, the {0}, has no location", testInstance);
    }

    [Theory]
    [MemberData(nameof(EtcUtc_TestCases))]
    public void Longitude_EtcUtc_ReturnsZero(string testInstance, TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.Longitude;

        // assert
        testResult.Should().Be(NoLocationCoordinate, "Etc/UTC, the {0}, has no location", testInstance);
    }

    [Theory]
    [MemberData(nameof(EtcUtc_TestCases))]
    public void IsoCountryCodes_EtcUtc_ReturnsEmpty(string testInstance, TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.IsoCountryCodes;

        // assert
        testResult.Should().BeEmpty("Etc/UTC, the {0}, is used in no country", testInstance);
    }

    [Theory]
    [MemberData(nameof(EtcUtc_TestCases))]
    public void Comment_EtcUtc_ReturnsNull(string testInstance, TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.Comment;

        // assert
        testResult.Should().BeNull("Etc/UTC, the {0}, is not listed in zone1970.tab, so has no comment", testInstance);
    }
}
