using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.UNM49.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class UnM49ExtensionsTests
    {
        private static readonly ushort[] _hierarchicalM49Codes;

        static UnM49ExtensionsTests()
        {
            _hierarchicalM49Codes = [001, 002, 015, 202, 011, 014, 017, 018, 019, 021, 419, 005, 013, 029, 142, 030, 034, 035, 143, 145, 150, 039, 151, 154, 155, 009, 053, 054, 057, 061];
        }

        #region Test: GetCountryOrAreaName_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetCountryOrAreaName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetCountryOrAreaName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetCountryOrAreaName_WithM49CodeByAlpha2Code_ReturnsCountryOrAreaName(UnM49AreaByAlpha2CountryCode testCode, string languageCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetCountryOrAreaName(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetCountryOrAreaName_WithM49CodeByAlpha2Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.DO, "en", "Dominican Republic" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.MS, "zh", "蒙特塞拉特" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PH, "ru", "Филиппины" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.UA, "fr", "Ukraine" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.NL, "es", "Países Bajos (Reino de los)" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.NC, "ar", "كاليدونيا الجديدة" };
                }
            }
        }

        #endregion

        #region Test: GetCountryOrAreaName_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetCountryOrAreaName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetCountryOrAreaName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetCountryOrAreaName_WithM49CodeByAlpha3Code_ReturnsCountryOrAreaName(UnM49AreaByAlpha3CountryCode testCode, string languageCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetCountryOrAreaName(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetCountryOrAreaName_WithM49CodeByAlpha3Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.DOM, "en", "Dominican Republic" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.MSR, "zh", "蒙特塞拉特" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.PHL, "ru", "Филиппины" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.UKR, "fr", "Ukraine" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.NLD, "es", "Países Bajos (Reino de los)" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.NCL, "ar", "كاليدونيا الجديدة" };
                }
            }
        }

        #endregion

        #region Test: GetGlobalCode_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetGlobalCode_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalCode_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetGlobalCode_WithM49CodeByAlpha2Code_ReturnsGlobalCode(UnM49AreaByAlpha2CountryCode testCode, ushort expectedResult)
        {
            // act
            var testResult = testCode.GetGlobalCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetGlobalCode_WithM49CodeByAlpha2Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, 1 }; }
            }
        }

        #endregion

        #region Test: GetGlobalCode_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetGlobalCode_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalCode_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetGlobalCode_WithM49CodeByAlpha3Code_ReturnsGlobalCode(UnM49AreaByAlpha3CountryCode testCode, ushort expectedResult)
        {
            // act
            var testResult = testCode.GetGlobalCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetGlobalCode_WithM49CodeByAlpha3Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, 1 }; }
            }
        }

        #endregion

        #region Test: GetGlobalName_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetGlobalName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetGlobalName_WithM49CodeByAlpha2Code_ReturnsGlobalName(UnM49AreaByAlpha2CountryCode testCode, string languageCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetGlobalName(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetGlobalName_WithM49CodeByAlpha2Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, "en", "World" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, "zh", "世界" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, "ru", "Весь мир" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, "fr", "Monde" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, "es", "Mundo" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, "ar", "العالم" };
                }
            }
        }

        #endregion

        #region Test: GetGlobalName_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetGlobalName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetGlobalName_WithM49CodeByAlpha3Code_ReturnsGlobalName(UnM49AreaByAlpha3CountryCode testCode, string languageCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetGlobalName(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetGlobalName_WithM49CodeByAlpha3Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, "en", "World" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, "zh", "世界" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, "ru", "Весь мир" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, "fr", "Monde" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, "es", "Mundo" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, "ar", "العالم" };
                }
            }
        }

        #endregion

        #region Test: GetIntermediateRegionCode_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetIntermediateRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIntermediateRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetIntermediateRegionCode_WithM49CodeByAlpha2Code_ReturnsIntermediateRegionCode(UnM49AreaByAlpha2CountryCode testCode, ushort expectedResult)
        {
            // act
            var testResult = testCode.GetIntermediateRegionCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetIntermediateRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, 005 }; }
            }
        }

        #endregion

        #region Test: GetIntermediateRegionCode_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetIntermediateRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIntermediateRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetIntermediateRegionCode_WithM49CodeByAlpha3Code_ReturnsIntermediateRegionCode(UnM49AreaByAlpha3CountryCode testCode, ushort expectedResult)
        {
            // act
            var testResult = testCode.GetIntermediateRegionCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetIntermediateRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, 005 }; }
            }
        }

        #endregion

        #region Test: GetIntermediateRegionName_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetIntermediateRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIntermediateRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetIntermediateRegionName_WithM49CodeByAlpha2Code_ReturnsIntermediateRegionName(UnM49AreaByAlpha2CountryCode testCode, string languageCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetIntermediateRegionName(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetIntermediateRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, "en", "South America" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, "zh", "南美洲" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, "ru", "Южная Америка" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, "fr", "Amérique du Sud" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, "es", "América del Sur" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, "ar", "أمريكا الجنوبية" };
                }
            }
        }

        #endregion

        #region Test: GetIntermediateRegionName_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetIntermediateRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIntermediateRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetIntermediateRegionName_WithM49CodeByAlpha3Code_ReturnsIntermediateRegionName(UnM49AreaByAlpha3CountryCode testCode, string languageCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetIntermediateRegionName(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetIntermediateRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, "en", "South America" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, "zh", "南美洲" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, "ru", "Южная Америка" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, "fr", "Amérique du Sud" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, "es", "América del Sur" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, "ar", "أمريكا الجنوبية" };
                }
            }
        }

        #endregion

        [Fact]
        public void GetM49Codes_WithM49CodesFromByAlpha2CodeEnum_ReturnsAllM49Codes()
        {
            // arrange
            var m49Codes = Enum.GetValues<UnM49AreaByAlpha2CountryCode>().Cast<ushort>();
            var expectedResult = _hierarchicalM49Codes.Union(m49Codes);

            // act
            var testResult = UnM49Extensions.GetM49Codes(typeof(UnM49AreaByAlpha2CountryCode));

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GetM49Codes_WithM49CodesFromByAlpha3CodeEnum_ReturnsAllM49Codes()
        {
            // arrange
            var m49Codes = Enum.GetValues<UnM49AreaByAlpha3CountryCode>().Cast<ushort>();
            var expectedResult = _hierarchicalM49Codes.Union(m49Codes);

            // act
            var testResult = UnM49Extensions.GetM49Codes(typeof(UnM49AreaByAlpha3CountryCode));

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        #region Test: GetRegionCode_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetRegionCode_WithM49CodeByAlpha2Code_ReturnsRegionCode(UnM49AreaByAlpha2CountryCode testCode, ushort expectedResult)
        {
            // act
            var testResult = testCode.GetRegionCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, 2 }; }
            }
        }

        #endregion

        #region Test: GetRegionCode_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetRegionCode_WithM49CodeByAlpha3Code_ReturnsRegionCode(UnM49AreaByAlpha3CountryCode testCode, ushort expectedResult)
        {
            // act
            var testResult = testCode.GetRegionCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, 2 }; }
            }
        }

        #endregion

        #region Test: GetRegionName_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetRegionName_WithM49CodeByAlpha2Code_ReturnsRegionName(UnM49AreaByAlpha2CountryCode testCode, string languageCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetRegionName(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, "en", "Africa" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, "zh", "非洲" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, "ru", "Африка" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, "fr", "Afrique" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, "es", "África" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, "ar", "أفريقيا" };
                }
            }
        }

        #endregion

        #region Test: GetRegionName_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetRegionName_WithM49CodeByAlpha3Code_ReturnsRegionName(UnM49AreaByAlpha3CountryCode testCode, string languageCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetRegionName(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, "en", "Africa" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, "zh", "非洲" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, "ru", "Африка" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, "fr", "Afrique" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, "es", "África" };
                    yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, "ar", "أفريقيا" };
                }
            }
        }

        #endregion

        #region Test: GetSubRegionCode_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetSubRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetSubRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetSubRegionCode_WithM49CodeByAlpha2Code_ReturnsSubRegionCode(UnM49AreaByAlpha2CountryCode testCode, ushort expectedResult)
        {
            // act
            var testResult = testCode.GetSubRegionCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetSubRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, 419 }; }
            }
        }

        #endregion

        #region Test: GetSubRegionCode_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetSubRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetSubRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetSubRegionCode_WithM49CodeByAlpha3Code_ReturnsSubRegionCode(UnM49AreaByAlpha3CountryCode testCode, ushort expectedResult)
        {
            // act
            var testResult = testCode.GetSubRegionCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetSubRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { UnM49AreaByAlpha3CountryCode.PAN, 419 }; }
            }
        }

        #endregion

        #region Test: GetSubRegionName_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetSubRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetSubRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetSubRegionName_WithM49CodeByAlpha2Code_ReturnsSubRegionName(UnM49AreaByAlpha2CountryCode testCode, string languageCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetSubRegionName(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetSubRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "en", "Latin America and the Caribbean" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "zh", "拉丁美洲和加勒比" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "ru", "Латинская Америка и Карибский бассейн" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "fr", "Amérique latine et Caraïbes" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "es", "América Latina y el Caribe" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "ar", "أمريكا اللاتينية ومنطقة البحر الكاريبي" };
                }
            }
        }

        #endregion

        #region Test: GetSubRegionName_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetSubRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetSubRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetSubRegionName_WithM49CodeByAlpha3Code_ReturnsSubRegionName(UnM49AreaByAlpha3CountryCode testCode, string languageCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetSubRegionName(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetSubRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "en", "Latin America and the Caribbean" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "zh", "拉丁美洲和加勒比" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "ru", "Латинская Америка и Карибский бассейн" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "fr", "Amérique latine et Caraïbes" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "es", "América Latina y el Caribe" };
                    yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "ar", "أمريكا اللاتينية ومنطقة البحر الكاريبي" };
                }
            }
        }

        #endregion
    }
}