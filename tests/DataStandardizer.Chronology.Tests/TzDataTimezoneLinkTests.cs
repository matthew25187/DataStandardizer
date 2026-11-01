using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TzDataTimezoneLinkTests
{
    // The precision to which a coordinate in degrees and minutes is compared, after conversion to decimal degrees.
    private const double CoordinateTolerance = 1e-9;

    // The ISO 6709 coordinates of zone.tab in TZ Database 2026e, in decimal degrees.
    private const double OsloLatitude = 59 + 55 / 60.0;            // +5955
    private const double OsloLongitude = 10 + 45 / 60.0;           // +01045
    private const double StockholmLatitude = 59 + 20 / 60.0;       // +5920
    private const double StockholmLongitude = 18 + 3 / 60.0;       // +01803
    private const double KualaLumpurLatitude = 3 + 10 / 60.0;      // +0310
    private const double KualaLumpurLongitude = 101 + 42 / 60.0;   // +10142
    private const double LongyearbyenLatitude = 78.0;              // +7800
    private const double LongyearbyenLongitude = 16.0;             // +01600
    private const double ReykjavikLatitude = 64 + 9 / 60.0;        // +6409
    private const double ReykjavikLongitude = -(21 + 51 / 60.0);   // -02151
    private const double NassauLatitude = 25 + 5 / 60.0;           // +2505
    private const double NassauLongitude = -(77 + 21 / 60.0);      // -07721
    private const double McMurdoLatitude = -(77 + 50 / 60.0);      // -7750
    private const double McMurdoLongitude = 166 + 36 / 60.0;       // +16636

    // The country codes of zone.tab in TZ Database 2026e (ISO 3166-1 alpha-2 codes).
    private const string Norway = "NO";
    private const string Sweden = "SE";
    private const string Malaysia = "MY";
    private const string SvalbardAndJanMayen = "SJ";
    private const string Iceland = "IS";
    private const string Bahamas = "BS";
    private const string Antarctica = "AQ";

    // The comments of zone.tab in TZ Database 2026e.
    private const string KualaLumpurComment = "Malaysia (peninsula)";
    private const string McMurdoComment = "New Zealand time - McMurdo, South Pole";

    // Deprecated names in the backward file of TZ Database 2026e, which have no fields.
    private const string CalcuttaName = "Asia/Calcutta";
    private const string UsEasternName = "US/Eastern";
    private const string KievName = "Europe/Kiev";
    private const string BuenosAiresName = "America/Buenos_Aires";
    private const string IcelandName = "Iceland";
    private const string NewZealandName = "NZ";

    // The identifier of Europe/Oslo, and the name that ToString gives it.
    private const string OsloIdentifier = "Europe/Oslo";
    private const string OsloName = "Oslo";

    private static readonly Lazy<IReadOnlyDictionary<string, TzDataTimezone.RegistryEntry>> DeprecatedLinks = new(() => new TzDataTimezone.Registry().GetDeprecatedLinks());

    // Links listed in zone.tab, with the location given there and the canonical timezone at the end of their chain of links.
    private static readonly (TzDataTimezone Timezone, TzDataTimezone Canonical, string CountryCode, double Latitude, double Longitude, string? Comment)[] ZoneTabLinkCases =
    [
        (TzDataTimezone.Europe.Oslo, TzDataTimezone.Europe.Berlin, Norway, OsloLatitude, OsloLongitude, null),
        (TzDataTimezone.Europe.Stockholm, TzDataTimezone.Europe.Berlin, Sweden, StockholmLatitude, StockholmLongitude, null),
        (TzDataTimezone.Asia.Kuala_Lumpur, TzDataTimezone.Asia.Singapore, Malaysia, KualaLumpurLatitude, KualaLumpurLongitude, KualaLumpurComment),
        (TzDataTimezone.Arctic.Longyearbyen, TzDataTimezone.Europe.Berlin, SvalbardAndJanMayen, LongyearbyenLatitude, LongyearbyenLongitude, null),
        (TzDataTimezone.Atlantic.Reykjavik, TzDataTimezone.Africa.Abidjan, Iceland, ReykjavikLatitude, ReykjavikLongitude, null),
        (TzDataTimezone.America.Nassau, TzDataTimezone.America.Toronto, Bahamas, NassauLatitude, NassauLongitude, null),
        (TzDataTimezone.Antarctica.McMurdo, TzDataTimezone.Pacific.Auckland, Antarctica, McMurdoLatitude, McMurdoLongitude, McMurdoComment),
    ];

    // Timezones listed in zone1970.tab, which are not links.
    private static readonly TzDataTimezone[] CanonicalTimezones =
    [
        TzDataTimezone.Europe.Zurich,
        TzDataTimezone.Europe.Berlin,
        TzDataTimezone.Asia.Singapore,
        TzDataTimezone.Africa.Abidjan,
        TzDataTimezone.America.Toronto,
        TzDataTimezone.Pacific.Auckland,
        TzDataTimezone.Asia.Kolkata,
    ];

    public static TheoryData<string> TimezoneField_TestCases => TimezoneFieldCatalog.CreateIdentifierTestCases();

    public static TheoryData<string> DeprecatedLink_TestCases
    {
        get
        {
            var testCases = new TheoryData<string>();
            foreach (var name in DeprecatedLinks.Value.Keys)
                testCases.Add(name);

            return testCases;
        }
    }

    // A field and a cast instance of each link.
    public static TheoryData<TzDataTimezone> ZoneTabLink_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone>();
            foreach (var testCase in ZoneTabLinkCases)
            {
                testCases.Add(testCase.Timezone);
                testCases.Add((TzDataTimezone)(string)testCase.Timezone!);
            }

            return testCases;
        }
    }

    // A field and a cast instance of each link, with its canonical timezone.
    public static TheoryData<TzDataTimezone, TzDataTimezone> ZoneTabLinkCanonical_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, TzDataTimezone>();
            foreach (var testCase in ZoneTabLinkCases)
            {
                testCases.Add(testCase.Timezone, testCase.Canonical);
                testCases.Add((TzDataTimezone)(string)testCase.Timezone!, testCase.Canonical);
            }

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, string> ZoneTabLinkCountryCode_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, string>();
            foreach (var testCase in ZoneTabLinkCases)
                testCases.Add(testCase.Timezone, testCase.CountryCode);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, double> ZoneTabLinkLatitude_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, double>();
            foreach (var testCase in ZoneTabLinkCases)
                testCases.Add(testCase.Timezone, testCase.Latitude);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, double> ZoneTabLinkLongitude_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, double>();
            foreach (var testCase in ZoneTabLinkCases)
                testCases.Add(testCase.Timezone, testCase.Longitude);

            return testCases;
        }
    }

    public static TheoryData<TzDataTimezone, string?> ZoneTabLinkComment_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone, string?>();
            foreach (var testCase in ZoneTabLinkCases)
                testCases.Add(testCase.Timezone, testCase.Comment);

            return testCases;
        }
    }

    // A field and a cast instance of each canonical timezone.
    public static TheoryData<TzDataTimezone> CanonicalTimezone_TestCases
    {
        get
        {
            var testCases = new TheoryData<TzDataTimezone>();
            foreach (var timezone in CanonicalTimezones)
            {
                testCases.Add(timezone);
                testCases.Add((TzDataTimezone)(string)timezone!);
            }

            return testCases;
        }
    }

    // Deprecated names in backward, with the canonical timezone they parse to.
    public static TheoryData<string, TzDataTimezone> DeprecatedName_TestCases => new()
    {
        { CalcuttaName, TzDataTimezone.Asia.Kolkata },
        { UsEasternName, TzDataTimezone.America.New_York },
        { KievName, TzDataTimezone.Europe.Kyiv },
        { BuenosAiresName, TzDataTimezone.America.Argentina.Buenos_Aires },
        { IcelandName, TzDataTimezone.Africa.Abidjan },
        { NewZealandName, TzDataTimezone.Pacific.Auckland },
    };

    public static TheoryData<string> DeprecatedNameOnly_TestCases => new()
    {
        CalcuttaName,
        UsEasternName,
        KievName,
        BuenosAiresName,
        IcelandName,
        NewZealandName,
    };

    public static TheoryData<TzDataTimezone> InvalidInstance_TestCases => new()
    {
        default(TzDataTimezone),
        (TzDataTimezone)"Europe/Atlantis",
        (TzDataTimezone)CalcuttaName,
    };

    private static TzDataZoneLine[] GetZoneLineArray(TzDataTimezone timezone)
    {
        timezone.TryGetRegistryEntry(out var entry).Should().BeTrue("{0} is a timezone field", (string?)timezone);
        return entry!.ZoneLines;
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void Canonical_TimezoneField_IsTimezoneField(string testIdentifier)
    {
        // arrange
        var testTimezone = TimezoneFieldCatalog.Timezones[testIdentifier];

        // act
        var testResult = (string?)testTimezone.Canonical;

        // assert
        TimezoneFieldCatalog.Timezones.Should().ContainKey(testResult!, "the canonical timezone of {0} is a timezone field", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(TimezoneField_TestCases), DisableDiscoveryEnumeration = true)]
    public void IsLink_CanonicalOfTimezoneField_ReturnsFalse(string testIdentifier)
    {
        // arrange
        var testCanonical = TimezoneFieldCatalog.Timezones[testIdentifier].Canonical;

        // act
        var testResult = testCanonical.IsLink;

        // assert
        testResult.Should().BeFalse("chains of links resolve to the canonical timezone at their end, so the canonical timezone of {0} is not a link", testIdentifier);
    }

    [Theory]
    [MemberData(nameof(ZoneTabLink_TestCases))]
    public void IsLink_ZoneTabLink_ReturnsTrue(TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.IsLink;

        // assert
        testResult.Should().BeTrue("{0} is a link", testTimezone);
    }

    [Theory]
    [MemberData(nameof(ZoneTabLinkCanonical_TestCases))]
    public void Canonical_ZoneTabLink_ReturnsCanonicalTimezone(TzDataTimezone testTimezone, TzDataTimezone expectedResult)
    {
        // act
        var testResult = testTimezone.Canonical;

        // assert
        testResult.Should().Be(expectedResult, "{0} is at the end of the chain of links from {1}", expectedResult, testTimezone);
    }

    [Theory]
    [MemberData(nameof(ZoneTabLinkCanonical_TestCases))]
    public void RegistryEntry_ZoneTabLink_SharesZoneLinesOfCanonicalTimezone(TzDataTimezone testTimezone, TzDataTimezone testCanonical)
    {
        // arrange
        var expectedResult = GetZoneLineArray(testCanonical);

        // act
        var testResult = GetZoneLineArray(testTimezone);

        // assert
        testResult.Should().BeSameAs(expectedResult, "a link shares the zone lines array of its canonical timezone");
    }

    [Theory]
    [MemberData(nameof(ZoneTabLinkCountryCode_TestCases))]
    public void IsoCountryCodes_ZoneTabLink_ReturnsCountryCodeFromZoneTab(TzDataTimezone testTimezone, string expectedResult)
    {
        // act
        var testResult = testTimezone.IsoCountryCodes;

        // assert
        testResult.Should().Equal(expectedResult);
    }

    [Theory]
    [MemberData(nameof(ZoneTabLinkLatitude_TestCases))]
    public void Latitude_ZoneTabLink_ReturnsLatitudeFromZoneTab(TzDataTimezone testTimezone, double expectedResult)
    {
        // act
        var testResult = testTimezone.Latitude;

        // assert
        testResult.Should().BeApproximately(expectedResult, CoordinateTolerance);
    }

    [Theory]
    [MemberData(nameof(ZoneTabLinkLongitude_TestCases))]
    public void Longitude_ZoneTabLink_ReturnsLongitudeFromZoneTab(TzDataTimezone testTimezone, double expectedResult)
    {
        // act
        var testResult = testTimezone.Longitude;

        // assert
        testResult.Should().BeApproximately(expectedResult, CoordinateTolerance);
    }

    [Theory]
    [MemberData(nameof(ZoneTabLinkComment_TestCases))]
    public void Comment_ZoneTabLink_ReturnsCommentFromZoneTab(TzDataTimezone testTimezone, string? expectedResult)
    {
        // act
        var testResult = testTimezone.Comment;

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [MemberData(nameof(CanonicalTimezone_TestCases))]
    public void IsLink_CanonicalTimezone_ReturnsFalse(TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.IsLink;

        // assert
        testResult.Should().BeFalse("{0} is not a link", testTimezone);
    }

    [Theory]
    [MemberData(nameof(CanonicalTimezone_TestCases))]
    public void Canonical_CanonicalTimezone_ReturnsItself(TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.Canonical;

        // assert
        testResult.Should().Be(testTimezone, "{0} is its own canonical timezone", testTimezone);
    }

    [Fact]
    public void Equality_LinkAndItsCanonicalTimezone_AreNotEqual()
    {
        // act
        var testResult = TzDataTimezone.Europe.Oslo == TzDataTimezone.Europe.Berlin;

        // assert
        testResult.Should().BeFalse("equality is based on the identifier");
    }

    [Fact]
    public void Equality_CanonicalOfLinkAndCanonicalTimezone_AreEqual()
    {
        // arrange
        var testLinkCanonical = TzDataTimezone.Europe.Oslo.Canonical;

        // act
        var testResult = testLinkCanonical == TzDataTimezone.Europe.Berlin;

        // assert
        testResult.Should().BeTrue("Europe/Oslo has the zone lines of Europe/Berlin");
    }

    [Fact]
    public void ExplicitConversionToString_Link_ReturnsOwnIdentifier()
    {
        // act
        var testResult = (string?)TzDataTimezone.Europe.Oslo;

        // assert
        testResult.Should().Be(OsloIdentifier, "a link keeps its own identifier");
    }

    [Fact]
    public void ToString_Link_ReturnsOwnName()
    {
        // act
        var testResult = TzDataTimezone.Europe.Oslo.ToString();

        // assert
        testResult.Should().Be(OsloName);
    }

    [Theory]
    [MemberData(nameof(DeprecatedName_TestCases))]
    public void Parse_DeprecatedName_ReturnsCanonicalField(string testName, TzDataTimezone expectedResult)
    {
        // act
        var testResult = TzDataTimezone.Parse(testName);

        // assert
        testResult.Should().Be(expectedResult, "{0} is a deprecated name for {1}", testName, (string?)expectedResult);
    }

    [Theory]
    [MemberData(nameof(DeprecatedNameOnly_TestCases))]
    public void IsLink_ParsedDeprecatedName_ReturnsFalse(string testName)
    {
        // arrange
        var testTimezone = TzDataTimezone.Parse(testName);

        // act
        var testResult = testTimezone.IsLink;

        // assert
        testResult.Should().BeFalse("{0} parses to the canonical timezone at the end of its chain of links", testName);
    }

    [Theory]
    [MemberData(nameof(DeprecatedName_TestCases))]
    public void RegistryEntry_ParsedDeprecatedName_HasZoneLinesOfCanonicalField(string testName, TzDataTimezone testCanonical)
    {
        // arrange
        var testTimezone = TzDataTimezone.Parse(testName);
        var expectedResult = GetZoneLineArray(testCanonical);

        // act
        var testResult = GetZoneLineArray(testTimezone);

        // assert
        testResult.Should().BeSameAs(expectedResult, "{0} parses to a timezone with the zone lines of its canonical field", testName);
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
    }

    [Theory]
    [MemberData(nameof(DeprecatedLink_TestCases))]
    public void Timezones_DeprecatedLink_HaveNoField(string testName)
    {
        // act
        var testResult = TimezoneFieldCatalog.Timezones.ContainsKey(testName);

        // assert
        testResult.Should().BeFalse("deprecated links have no fields");
    }

    [Theory]
    [MemberData(nameof(DeprecatedLink_TestCases))]
    public void Parse_DeprecatedLink_ReturnsTimezoneField(string testName)
    {
        // act
        var testResult = (string?)TzDataTimezone.Parse(testName);

        // assert
        TimezoneFieldCatalog.Timezones.Should().ContainKey(testResult!, "{0} parses to a timezone field", testName);
    }

    [Theory]
    [MemberData(nameof(DeprecatedLink_TestCases))]
    public void IsLink_ParsedDeprecatedLink_ReturnsFalse(string testName)
    {
        // arrange
        var testTimezone = TzDataTimezone.Parse(testName);

        // act
        var testResult = testTimezone.IsLink;

        // assert
        testResult.Should().BeFalse("{0} parses to the canonical timezone at the end of its chain of links", testName);
    }

    [Theory]
    [MemberData(nameof(InvalidInstance_TestCases))]
    public void IsLink_InvalidInstance_ThrowsInvalidOperationException(TzDataTimezone testTimezone)
    {
        // act
        Func<bool> testAction = () => testTimezone.IsLink;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(InvalidInstance_TestCases))]
    public void Canonical_InvalidInstance_ThrowsInvalidOperationException(TzDataTimezone testTimezone)
    {
        // act
        Func<TzDataTimezone> testAction = () => testTimezone.Canonical;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }
}
