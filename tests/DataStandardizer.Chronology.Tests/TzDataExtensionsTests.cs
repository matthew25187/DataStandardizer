#pragma warning disable CS0618 // The obsolete metadata accessors are tested until they are removed.

using FluentAssertions;

namespace DataStandardizer.Chronology.Tests
{
    public class TzDataExtensionsTests
    {
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

        public static TheoryData<TzDataTimezone, string> GetComment_TimezoneHasComment_TestCases => new()
        {
            { TzDataTimezone.Europe.Zurich, ZurichComment },
            { TzDataTimezone.America.Argentina.Buenos_Aires, BuenosAiresComment },
        };

        public static TheoryData<TzDataTimezone> GetComment_TimezoneHasNoComment_TestCases => new()
        {
            TzDataTimezone.Europe.Andorra,
            TzDataTimezone.Africa.Cairo,
        };

        public static TheoryData<TzDataTimezone, string[]> GetIsoCountryCodes_TestCases => new()
        {
            { TzDataTimezone.Australia.Darwin, [Australia] },
            { TzDataTimezone.Asia.Dubai, [UnitedArabEmirates, Oman, Reunion, Seychelles, FrenchSouthernTerritories] },
        };

        public static TheoryData<TzDataTimezone, double> GetLatitude_TestCases => new()
        {
            { TzDataTimezone.America.Adak, AdakLatitude },
            { TzDataTimezone.America.North_Dakota.New_Salem, NewSalemLatitude },
        };

        public static TheoryData<TzDataTimezone, double> GetLongitude_TestCases => new()
        {
            { TzDataTimezone.Europe.Tallinn, TallinnLongitude },
            { TzDataTimezone.America.Argentina.Buenos_Aires, BuenosAiresLongitude },
        };

        [Theory]
        [MemberData(nameof(GetComment_TimezoneHasComment_TestCases))]
        public void GetComment_TimezoneHasComment_ReturnsComment(TzDataTimezone testTimezone, string expectedResult)
        {
            // act
            var testResult = testTimezone.GetComment();

            // assert
            testResult.Should().Be(expectedResult, "the comment for timezone {0} is {1}", testTimezone, expectedResult);
        }

        [Theory]
        [MemberData(nameof(GetComment_TimezoneHasNoComment_TestCases))]
        public void GetComment_TimezoneHasNoComment_ReturnsNull(TzDataTimezone testTimezone)
        {
            // act
            var testResult = testTimezone.GetComment();

            // assert
            testResult.Should().BeNull("there is no comment for timezone {0}", testTimezone);
        }

        [Theory]
        [MemberData(nameof(GetIsoCountryCodes_TestCases))]
        public void GetIsoCountryCodes_Timezone_ReturnsCountryCodesForTimezone(TzDataTimezone testTimezone, string[] expectedResult)
        {
            // act
            var testResult = testTimezone.GetIsoCountryCodes();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult, "these are the country codes associated with timezone {0}", testTimezone);
        }

        [Theory]
        [MemberData(nameof(GetLatitude_TestCases))]
        public void GetLatitude_Timezone_ReturnsLatitudeOfPrincipalLocation(TzDataTimezone testTimezone, double expectedResult)
        {
            // act
            var testResult = testTimezone.GetLatitude();

            // assert
            testResult.Should().Be(expectedResult, "{0} is the latitude of the principal location for timezone {1}", expectedResult, testTimezone);
        }

        [Theory]
        [MemberData(nameof(GetLongitude_TestCases))]
        public void GetLongitude_Timezone_ReturnsLongitudeOfPrincipalLocation(TzDataTimezone testTimezone, double expectedResult)
        {
            // act
            var testResult = testTimezone.GetLongitude();

            // assert
            testResult.Should().Be(expectedResult, "{0} is the longitude of the principal location for timezone {1}", expectedResult, testTimezone);
        }
    }
}
