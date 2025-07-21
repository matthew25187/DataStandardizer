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
        public void GetCountryOrAreaName_WithM49CodeByAlpha2Code_ReturnsCountryOrAreaName(UnM49ByAlpha2Code testCode, string languageCode, string expectedResult)
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
                    yield return new object[] { UnM49ByAlpha2Code.DO, "en", "Dominican Republic" };
                    yield return new object[] { UnM49ByAlpha2Code.MS, "zh", "蒙特塞拉特" };
                    yield return new object[] { UnM49ByAlpha2Code.PH, "ru", "Филиппины" };
                    yield return new object[] { UnM49ByAlpha2Code.UA, "fr", "Ukraine" };
                    yield return new object[] { UnM49ByAlpha2Code.NL, "es", "Países Bajos (Reino de los)" };
                    yield return new object[] { UnM49ByAlpha2Code.NC, "ar", "كاليدونيا الجديدة" };
                }
            }
        }

        #endregion

        #region Test: GetCountryOrAreaName_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetCountryOrAreaName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetCountryOrAreaName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetCountryOrAreaName_WithM49CodeByAlpha3Code_ReturnsCountryOrAreaName(UnM49ByAlpha3Code testCode, string languageCode, string expectedResult)
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
                    yield return new object[] { UnM49ByAlpha3Code.DOM, "en", "Dominican Republic" };
                    yield return new object[] { UnM49ByAlpha3Code.MSR, "zh", "蒙特塞拉特" };
                    yield return new object[] { UnM49ByAlpha3Code.PHL, "ru", "Филиппины" };
                    yield return new object[] { UnM49ByAlpha3Code.UKR, "fr", "Ukraine" };
                    yield return new object[] { UnM49ByAlpha3Code.NLD, "es", "Países Bajos (Reino de los)" };
                    yield return new object[] { UnM49ByAlpha3Code.NCL, "ar", "كاليدونيا الجديدة" };
                }
            }
        }

        #endregion

        #region Test: GetGlobalCode_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetGlobalCode_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalCode_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetGlobalCode_WithM49CodeByAlpha2Code_ReturnsGlobalCode(UnM49ByAlpha2Code testCode, ushort expectedResult)
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
                get { yield return new object[] { UnM49ByAlpha2Code.AT, 1 }; }
            }
        }

        #endregion

        #region Test: GetGlobalCode_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetGlobalCode_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalCode_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetGlobalCode_WithM49CodeByAlpha3Code_ReturnsGlobalCode(UnM49ByAlpha3Code testCode, ushort expectedResult)
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
                get { yield return new object[] { UnM49ByAlpha3Code.AUT, 1 }; }
            }
        }

        #endregion

        #region Test: GetGlobalName_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetGlobalName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetGlobalName_WithM49CodeByAlpha2Code_ReturnsGlobalName(UnM49ByAlpha2Code testCode, string languageCode, string expectedResult)
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
                    yield return new object[] { UnM49ByAlpha2Code.AT, "en", "World" };
                    yield return new object[] { UnM49ByAlpha2Code.AT, "zh", "世界" };
                    yield return new object[] { UnM49ByAlpha2Code.AT, "ru", "Весь мир" };
                    yield return new object[] { UnM49ByAlpha2Code.AT, "fr", "Monde" };
                    yield return new object[] { UnM49ByAlpha2Code.AT, "es", "Mundo" };
                    yield return new object[] { UnM49ByAlpha2Code.AT, "ar", "العالم" };
                }
            }
        }

        #endregion

        #region Test: GetGlobalName_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetGlobalName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetGlobalName_WithM49CodeByAlpha3Code_ReturnsGlobalName(UnM49ByAlpha3Code testCode, string languageCode, string expectedResult)
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
                    yield return new object[] { UnM49ByAlpha3Code.AUT, "en", "World" };
                    yield return new object[] { UnM49ByAlpha3Code.AUT, "zh", "世界" };
                    yield return new object[] { UnM49ByAlpha3Code.AUT, "ru", "Весь мир" };
                    yield return new object[] { UnM49ByAlpha3Code.AUT, "fr", "Monde" };
                    yield return new object[] { UnM49ByAlpha3Code.AUT, "es", "Mundo" };
                    yield return new object[] { UnM49ByAlpha3Code.AUT, "ar", "العالم" };
                }
            }
        }

        #endregion

        #region Test: GetIntermediateRegionCode_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetIntermediateRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIntermediateRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetIntermediateRegionCode_WithM49CodeByAlpha2Code_ReturnsIntermediateRegionCode(UnM49ByAlpha2Code testCode, ushort expectedResult)
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
                get { yield return new object[] { UnM49ByAlpha2Code.GY, 005 }; }
            }
        }

        #endregion

        #region Test: GetIntermediateRegionCode_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetIntermediateRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIntermediateRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetIntermediateRegionCode_WithM49CodeByAlpha3Code_ReturnsIntermediateRegionCode(UnM49ByAlpha3Code testCode, ushort expectedResult)
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
                get { yield return new object[] { UnM49ByAlpha3Code.GUY, 005 }; }
            }
        }

        #endregion

        #region Test: GetIntermediateRegionName_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetIntermediateRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIntermediateRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetIntermediateRegionName_WithM49CodeByAlpha2Code_ReturnsIntermediateRegionName(UnM49ByAlpha2Code testCode, string languageCode, string expectedResult)
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
                    yield return new object[] { UnM49ByAlpha2Code.GY, "en", "South America" };
                    yield return new object[] { UnM49ByAlpha2Code.GY, "zh", "南美洲" };
                    yield return new object[] { UnM49ByAlpha2Code.GY, "ru", "Южная Америка" };
                    yield return new object[] { UnM49ByAlpha2Code.GY, "fr", "Amérique du Sud" };
                    yield return new object[] { UnM49ByAlpha2Code.GY, "es", "América del Sur" };
                    yield return new object[] { UnM49ByAlpha2Code.GY, "ar", "أمريكا الجنوبية" };
                }
            }
        }

        #endregion

        #region Test: GetIntermediateRegionName_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetIntermediateRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIntermediateRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetIntermediateRegionName_WithM49CodeByAlpha3Code_ReturnsIntermediateRegionName(UnM49ByAlpha3Code testCode, string languageCode, string expectedResult)
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
                    yield return new object[] { UnM49ByAlpha3Code.GUY, "en", "South America" };
                    yield return new object[] { UnM49ByAlpha3Code.GUY, "zh", "南美洲" };
                    yield return new object[] { UnM49ByAlpha3Code.GUY, "ru", "Южная Америка" };
                    yield return new object[] { UnM49ByAlpha3Code.GUY, "fr", "Amérique du Sud" };
                    yield return new object[] { UnM49ByAlpha3Code.GUY, "es", "América del Sur" };
                    yield return new object[] { UnM49ByAlpha3Code.GUY, "ar", "أمريكا الجنوبية" };
                }
            }
        }

        #endregion

        [Fact]
        public void GetM49Codes_WithM49CodesFromByAlpha2CodeEnum_ReturnsAllM49Codes()
        {
            // arrange
            var m49Codes = Enum.GetValues<UnM49ByAlpha2Code>().Cast<ushort>();
            var expectedResult = _hierarchicalM49Codes.Union(m49Codes);

            // act
            var testResult = UnM49Extensions.GetM49Codes(typeof(UnM49ByAlpha2Code));

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public void GetM49Codes_WithM49CodesFromByAlpha3CodeEnum_ReturnsAllM49Codes()
        {
            // arrange
            var m49Codes = Enum.GetValues<UnM49ByAlpha3Code>().Cast<ushort>();
            var expectedResult = _hierarchicalM49Codes.Union(m49Codes);

            // act
            var testResult = UnM49Extensions.GetM49Codes(typeof(UnM49ByAlpha3Code));

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        #region Test: GetRegionCode_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetRegionCode_WithM49CodeByAlpha2Code_ReturnsRegionCode(UnM49ByAlpha2Code testCode, ushort expectedResult)
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
                get { yield return new object[] { UnM49ByAlpha2Code.CG, 2 }; }
            }
        }

        #endregion

        #region Test: GetRegionCode_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetRegionCode_WithM49CodeByAlpha3Code_ReturnsRegionCode(UnM49ByAlpha3Code testCode, ushort expectedResult)
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
                get { yield return new object[] { UnM49ByAlpha3Code.COG, 2 }; }
            }
        }

        #endregion

        #region Test: GetRegionName_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetRegionName_WithM49CodeByAlpha2Code_ReturnsRegionName(UnM49ByAlpha2Code testCode, string languageCode, string expectedResult)
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
                    yield return new object[] { UnM49ByAlpha2Code.CG, "en", "Africa" };
                    yield return new object[] { UnM49ByAlpha2Code.CG, "zh", "非洲" };
                    yield return new object[] { UnM49ByAlpha2Code.CG, "ru", "Африка" };
                    yield return new object[] { UnM49ByAlpha2Code.CG, "fr", "Afrique" };
                    yield return new object[] { UnM49ByAlpha2Code.CG, "es", "África" };
                    yield return new object[] { UnM49ByAlpha2Code.CG, "ar", "أفريقيا" };
                }
            }
        }

        #endregion

        #region Test: GetRegionName_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetRegionName_WithM49CodeByAlpha3Code_ReturnsRegionName(UnM49ByAlpha3Code testCode, string languageCode, string expectedResult)
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
                    yield return new object[] { UnM49ByAlpha3Code.COG, "en", "Africa" };
                    yield return new object[] { UnM49ByAlpha3Code.COG, "zh", "非洲" };
                    yield return new object[] { UnM49ByAlpha3Code.COG, "ru", "Африка" };
                    yield return new object[] { UnM49ByAlpha3Code.COG, "fr", "Afrique" };
                    yield return new object[] { UnM49ByAlpha3Code.COG, "es", "África" };
                    yield return new object[] { UnM49ByAlpha3Code.COG, "ar", "أفريقيا" };
                }
            }
        }

        #endregion

        #region Test: GetSubRegionCode_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetSubRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetSubRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetSubRegionCode_WithM49CodeByAlpha2Code_ReturnsSubRegionCode(UnM49ByAlpha2Code testCode, ushort expectedResult)
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
                get { yield return new object[] { UnM49ByAlpha2Code.PA, 419 }; }
            }
        }

        #endregion

        #region Test: GetSubRegionCode_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetSubRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetSubRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetSubRegionCode_WithM49CodeByAlpha3Code_ReturnsSubRegionCode(UnM49ByAlpha3Code testCode, ushort expectedResult)
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
                get { yield return new object[] { UnM49ByAlpha3Code.PAN, 419 }; }
            }
        }

        #endregion

        #region Test: GetSubRegionName_WithM49CodeByAlpha2Code

        [Theory]
        [MemberData(nameof(GetSubRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetSubRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
        public void GetSubRegionName_WithM49CodeByAlpha2Code_ReturnsSubRegionName(UnM49ByAlpha2Code testCode, string languageCode, string expectedResult)
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
                    yield return new object[] { UnM49ByAlpha2Code.PA, "en", "Latin America and the Caribbean" };
                    yield return new object[] { UnM49ByAlpha2Code.PA, "zh", "拉丁美洲和加勒比" };
                    yield return new object[] { UnM49ByAlpha2Code.PA, "ru", "Латинская Америка и Карибский бассейн" };
                    yield return new object[] { UnM49ByAlpha2Code.PA, "fr", "Amérique latine et Caraïbes" };
                    yield return new object[] { UnM49ByAlpha2Code.PA, "es", "América Latina y el Caribe" };
                    yield return new object[] { UnM49ByAlpha2Code.PA, "ar", "أمريكا اللاتينية ومنطقة البحر الكاريبي" };
                }
            }
        }

        #endregion

        #region Test: GetSubRegionName_WithM49CodeByAlpha3Code

        [Theory]
        [MemberData(nameof(GetSubRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetSubRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
        public void GetSubRegionName_WithM49CodeByAlpha3Code_ReturnsSubRegionName(UnM49ByAlpha3Code testCode, string languageCode, string expectedResult)
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
                    yield return new object[] { UnM49ByAlpha2Code.PA, "en", "Latin America and the Caribbean" };
                    yield return new object[] { UnM49ByAlpha2Code.PA, "zh", "拉丁美洲和加勒比" };
                    yield return new object[] { UnM49ByAlpha2Code.PA, "ru", "Латинская Америка и Карибский бассейн" };
                    yield return new object[] { UnM49ByAlpha2Code.PA, "fr", "Amérique latine et Caraïbes" };
                    yield return new object[] { UnM49ByAlpha2Code.PA, "es", "América Latina y el Caribe" };
                    yield return new object[] { UnM49ByAlpha2Code.PA, "ar", "أمريكا اللاتينية ومنطقة البحر الكاريبي" };
                }
            }
        }

        #endregion
    }
}