using FluentAssertions;

namespace DataStandardizer.Chronology.Tests
{
    public class TzDataExtensionsTests
    {
        [Theory]
        [MemberData(nameof(GetComment_TimezoneHasComment_TestCases))]
        public void GetComment_TimezoneHasComment_ReturnsComment(TzDataTimezone testTimezone, string expectedResult)
        {
            // act
            var testResult = testTimezone.GetComment();

            // assert
            testResult.Should().Be(expectedResult, "the comment for timezone {0} is {1}", testTimezone, expectedResult);
        }

        public static IEnumerable<object[]> GetComment_TimezoneHasComment_TestCases
        {
            get
            {
                yield return new object[] { TzDataTimezone.Europe.Zurich, "Büsingen" };
                yield return new object[] { TzDataTimezone.America.Argentina.Buenos_Aires, "Buenos Aires (BA, CF)" };
            }
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

        public static IEnumerable<object[]> GetComment_TimezoneHasNoComment_TestCases
        {
            get
            {
                yield return new object[] { TzDataTimezone.Europe.Andorra };
                yield return new object[] { TzDataTimezone.Africa.Cairo };
            }
        }

        [Theory]
        [MemberData(nameof(GetIsoCountryCodes_TestCases))]
        public void GetIsoCountryCodes_ReturnsCountryCodesForTimezone(TzDataTimezone testTimezone, string[] expectedResult)
        {
            // act
            var testResult = testTimezone.GetIsoCountryCodes();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult, "{0} are the country codes associated with timezone {1}", string.Join(",", expectedResult), testTimezone);
        }

        public static IEnumerable<object[]> GetIsoCountryCodes_TestCases
        {
            get
            {
                yield return new object[] { TzDataTimezone.Australia.Darwin, new string[] { "AU" } };
                yield return new object[] { TzDataTimezone.Asia.Dubai, new string[] { "AE", "OM", "RE", "SC", "TF" } };
            }
        }

        [Theory]
        [MemberData(nameof(GetLatitude_TestCases))]
        public void GetLatitude_ReturnsLatitudeOfPrincipalLocation(TzDataTimezone testTimezone, double expectedResult)
        {
            // act
            var testResult = testTimezone.GetLatitude();

            // assert
            testResult.Should().Be(expectedResult, "{0} is the latitude of the principal location for timezone {1}", expectedResult, testTimezone);
        }

        public static IEnumerable<object[]> GetLatitude_TestCases
        {
            get
            {
                yield return new object[] { TzDataTimezone.America.Adak, 51.88D };
                yield return new object[] { TzDataTimezone.America.North_Dakota.New_Salem, 46.845D };
            }
        }

        [Theory]
        [MemberData(nameof(GetLongitude_TestCases))]
        public void GetLongitude_ReturnsLongitudeOfPrincipalLocation(TzDataTimezone testTimezone, double expectedResult)
        {
            // act
            var testResult = testTimezone.GetLongitude();

            // assert
            testResult.Should().Be(expectedResult, "{0} is the longitude of the principal location for timezone {1}", expectedResult, testTimezone);
        }

        public static IEnumerable<object[]> GetLongitude_TestCases
        {
            get
            {
                yield return new object[] { TzDataTimezone.Europe.Tallinn, 24.75D };
                yield return new object[] { TzDataTimezone.America.Argentina.Buenos_Aires, -58.45D };
            }
        }
    }
}