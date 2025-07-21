using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.ISO15924.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Iso15924ExtensionsTests
    {
        #region Test: GetAge_OnScriptCode

        [Theory]
        [MemberData(nameof(GetAge_OnScriptCode_TestCaseGenerator.TestCases), MemberType = typeof(GetAge_OnScriptCode_TestCaseGenerator))]
        public void GetAge_OnScriptCode_ReturnsScriptAge(Iso15924 testCode, double? expectedResult)
        {
            // act
            var testResult = testCode.GetAge();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetAge_OnScriptCode_TestCaseGenerator
        {
            public static IEnumerable<object?[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso15924.Adlm, 9.0 };
                    yield return new object?[] { Iso15924.Afak, null };
                    yield return new object[] { Iso15924.Arab, 1.1 };
                }
            }
        }

        #endregion

        #region Test: GetAlias_OnScriptCode

        [Theory]
        [MemberData(nameof(GetAlias_OnScriptCode_TestCaseGenerator.TestCases), MemberType = typeof(GetAlias_OnScriptCode_TestCaseGenerator))]
        public void GetAlias_OnScriptCode_ReturnsScriptAlias(Iso15924 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetAlias();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetAlias_OnScriptCode_TestCaseGenerator
        {
            public static IEnumerable<object?[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso15924.Ahom, "Ahom" };
                    yield return new object[] { Iso15924.Arab, "Arabic" };
                    yield return new object[] { Iso15924.Aghb, "Caucasian_Albanian" };
                    yield return new object?[] { Iso15924.Berf, null };
                }
            }
        }

        #endregion

        #region Test: GetDate_OnScriptCode

        [Theory]
        [MemberData(nameof(GetDate_OnScriptCode_TestCaseGenerator.TestCases), MemberType = typeof(GetDate_OnScriptCode_TestCaseGenerator))]
        public void GetDate_OnScriptCode_ReturnsScriptDate(Iso15924 testCode, DateOnly expectedResult)
        {
            // act
            var testResult = testCode.GetDate();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetDate_OnScriptCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso15924.Chis, new DateOnly(2023, 09, 12) };
                    yield return new object[] { Iso15924.Bopo, new DateOnly(2004, 05, 01) };
                }
            }
        }

        #endregion

        #region Test: GetEnglishName_OnScriptCode

        [Theory]
        [MemberData(nameof(GetEnglishName_OnScriptCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnScriptCode_TestCaseGenerator))]
        public void GetEnglishName_OnScriptCode_ReturnsEnglishName(Iso15924 testCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetEnglishName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetEnglishName_OnScriptCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso15924.Cham, "Cham" };
                    yield return new object[] { Iso15924.Chrs, "Chorasmian" };
                    yield return new object[] { Iso15924.Cans, "Unified Canadian Aboriginal Syllabics" };
                    yield return new object[] { Iso15924.Ahom, "Ahom, Tai Ahom" };
                }
            }
        }

        #endregion

        #region Test: GetFrenchName_OnScriptCode

        [Theory]
        [MemberData(nameof(GetFrenchName_OnScriptCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchName_OnScriptCode_TestCaseGenerator))]
        public void GetFrenchName_OnScriptCode_ReturnsFrenchName(Iso15924 testCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetFrenchName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetFrenchName_OnScriptCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso15924.Ahom, "âhom" };
                    yield return new object[] { Iso15924.Bopo, "bopomofo" };
                    yield return new object[] { Iso15924.Blis, "symboles Bliss" };
                    yield return new object[] { Iso15924.Aran, "arabe (variante nastalique)" };
                }
            }
        }

        #endregion
    }
}