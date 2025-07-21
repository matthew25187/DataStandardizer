using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.ISO3166.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Iso3166ExtensionTests
    {
        #region Test: GetEnglishName_OnIso3166Part1Alpha2CountryCode

        [Theory]
        [MemberData(nameof(GetEnglishName_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator))]
        public void GetEnglishName_OnIso3166Part1Alpha2CountryCode_ReturnsEnglishName(Iso3166Part1Alpha2 testCode, Iso3166CountryName nameType, string expectedResult)
        {
            // act
            var testResult = testCode.GetEnglishName(nameType);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetEnglishName_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso3166Part1Alpha2.BO, Iso3166CountryName.Short, "Bolivia (Plurinational State of)" };
                    yield return new object[] { Iso3166Part1Alpha2.BO, Iso3166CountryName.ShortUpper, "BOLIVIA, PLURINATIONAL STATE OF" };
                    yield return new object[] { Iso3166Part1Alpha2.BO, Iso3166CountryName.Full, "the Plurinational State of Bolivia" };
                }
            }
        }

        #endregion

        #region Test: GetNativeName_OnIso3166Part1Alpha2CountryCode

        [Theory]
        [MemberData(nameof(GetNativeName_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(GetNativeName_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator))]
        public void GetNativeName_OnIso3166Part1Alpha2CountryCode_ReturnsNativeName(Iso3166Part1Alpha2 testCode, string languageCode, Iso3166CountryName nameType, string? expectedResult)
        {
            // act
            var testResult = testCode.GetNativeName(languageCode, nameType);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetNativeName_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator
        {
            public static IEnumerable<object?[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso3166Part1Alpha2.BO, "en", Iso3166CountryName.Short, "Bolivia (Plurinational State of)" };
                    yield return new object[] { Iso3166Part1Alpha2.BO, "en", Iso3166CountryName.ShortUpper, "BOLIVIA, PLURINATIONAL STATE OF" };
                    yield return new object[] { Iso3166Part1Alpha2.BO, "en", Iso3166CountryName.Full, "the Plurinational State of Bolivia" };
                    yield return new object[] { Iso3166Part1Alpha2.BO, "fra", Iso3166CountryName.Short, "Bolivie (État plurinational de)" };
                    yield return new object[] { Iso3166Part1Alpha2.BO, "fra", Iso3166CountryName.ShortUpper, "BOLIVIE, ÉTAT PLURINATIONAL DE" };
                    yield return new object[] { Iso3166Part1Alpha2.BO, "fra", Iso3166CountryName.Full, "l'État plurinational de Bolivie" };
                    yield return new object?[] { Iso3166Part1Alpha2.BO, "es", Iso3166CountryName.Full, null };
                    yield return new object?[] { Iso3166Part1Alpha2.BO, "spa", Iso3166CountryName.Full, null };
                }
            }
        }

        #endregion

        #region Test: IsIndependent_OnIso3166Part1Alpha2CountryCode

        [Theory]
        [MemberData(nameof(IsIndependent_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(IsIndependent_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator))]
        public void IsIndependent_OnIso3166Part1Alpha2CountryCode_ReturnsIndependenceFlag(Iso3166Part1Alpha2 testCode, bool expectedResult)
        {
            // act
            var testResult = testCode.IsIndependent();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class IsIndependent_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso3166Part1Alpha2.AR, true };
                    yield return new object[] { Iso3166Part1Alpha2.AS, false };
                }
            }
        }

        #endregion

        #region Test: GetEnglishName_OnIso3166Part1Alpha3CountryCode

        [Theory]
        [MemberData(nameof(GetEnglishName_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator))]
        public void GetEnglishName_OnIso3166Part1Alpha3CountryCode_ReturnsEnglishName(Iso3166Part1Alpha3 testCode, Iso3166CountryName nameType, string expectedResult)
        {
            // act
            var testResult = testCode.GetEnglishName(nameType);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetEnglishName_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso3166Part1Alpha3.DEU, Iso3166CountryName.Short, "Germany" };
                    yield return new object[] { Iso3166Part1Alpha3.DEU, Iso3166CountryName.ShortUpper, "GERMANY" };
                    yield return new object[] { Iso3166Part1Alpha3.DEU, Iso3166CountryName.Full, "the Federal Republic of Germany" };
                }
            }
        }

        #endregion

        #region Test: GetNativeName_OnIso3166Part1Alpha3CountryCode

        [Theory]
        [MemberData(nameof(GetNativeName_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(GetNativeName_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator))]
        public void GetNativeName_OnIso3166Part1Alpha3CountryCode_ReturnsNativeName(Iso3166Part1Alpha3 testCode, string languageCode, Iso3166CountryName nameType, string? expectedResult)
        {
            // act
            var testResult = testCode.GetNativeName(languageCode, nameType);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetNativeName_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator
        {
            public static IEnumerable<object?[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso3166Part1Alpha3.ITA, "en", Iso3166CountryName.Short, "Italy" };
                    yield return new object[] { Iso3166Part1Alpha3.ITA, "en", Iso3166CountryName.ShortUpper, "ITALY" };
                    yield return new object[] { Iso3166Part1Alpha3.ITA, "en", Iso3166CountryName.Full, "the Republic of Italy" };
                    yield return new object[] { Iso3166Part1Alpha3.ITA, "fra", Iso3166CountryName.Short, "Italie (l')" };
                    yield return new object[] { Iso3166Part1Alpha3.ITA, "fra", Iso3166CountryName.ShortUpper, "ITALIE" };
                    yield return new object[] { Iso3166Part1Alpha3.ITA, "fra", Iso3166CountryName.Full, "la République italienne" };
                    yield return new object?[] { Iso3166Part1Alpha3.ITA, "it", Iso3166CountryName.Full, null };
                    yield return new object?[] { Iso3166Part1Alpha3.ITA, "ita", Iso3166CountryName.Full, null };
                }
            }
        }

        #endregion

        #region Test: IsIndependent_OnIso3166Part1Alpha3CountryCode

        [Theory]
        [MemberData(nameof(IsIndependent_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(IsIndependent_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator))]
        public void IsIndependent_OnIso3166Part1Alpha3CountryCode_ReturnsIndependenceFlag(Iso3166Part1Alpha3 testCode, bool expectedResult)
        {
            // act
            var testResult = testCode.IsIndependent();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class IsIndependent_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso3166Part1Alpha3.FRA, true };
                    yield return new object[] { Iso3166Part1Alpha3.ATF, false };
                }
            }
        }

        #endregion

        #region Test: GetSubdivisionCategoryIdentifier_OnIso3166Part2SubdivisionCode

        [Theory]
        [MemberData(nameof(GetSubdivisionCategoryIdentifier_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionCategoryIdentifier_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
        public void GetSubdivisionCategoryIdentifier_OnIso3166Part2SubdivisionCode_ReturnsCategoryIdentifier(Iso3166Part2 testCode, ushort expectedResult)
        {
            // act
            var testResult = testCode.GetSubdivisionCategoryIdentifier();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetSubdivisionCategoryIdentifier_OnIso3166Part2SubdivisionCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso3166Part2.BZ._BZ, 297 };
                    yield return new object[] { Iso3166Part2.AR._C, 182 };
                    yield return new object[] { Iso3166Part2.AR._A, 107 };
                }
            }
        }

        #endregion

        #region Test: GetSubdivisionCategoryName_OnIso3166Part2SubdivisionCode

        [Theory]
        [MemberData(nameof(GetSubdivisionCategoryName_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionCategoryName_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
        public void GetSubdivisionCategoryName_OnIso3166Part2SubdivisionCode_ReturnsCategoryName(Iso3166Part2 testCode, string languageCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetSubdivisionCategoryName(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetSubdivisionCategoryName_OnIso3166Part2SubdivisionCode_TestCaseGenerator
        {
            public static IEnumerable<object?[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso3166Part2.CY._01, "el", "eparchia" };
                    yield return new object[] { Iso3166Part2.CY._02, "eng", "district" };
                    yield return new object[] { Iso3166Part2.CY._03, "fr", "district" };
                    yield return new object[] { Iso3166Part2.CY._04, "tur", "kaza" };
                    yield return new object?[] { Iso3166Part2.CY._05, "de", null };
                }
            }
        }

        #endregion

        #region Test: GetSubdivisionCategoryNamePlural_OnIso3166Part2SubdivisionCode

        [Theory]
        [MemberData(nameof(GetSubdivisionCategoryNamePlural_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionCategoryNamePlural_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
        public void GetSubdivisionCategoryNamePlural_OnIso3166Part2SubdivisionCode_ReturnsCategoryNamePlural(Iso3166Part2 testCode, string languageCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetSubdivisionCategoryNamePlural(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetSubdivisionCategoryNamePlural_OnIso3166Part2SubdivisionCode_TestCaseGenerator
        {
            public static IEnumerable<object?[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso3166Part2.FR._NOR, "en", "metropolitan regions" };
                    yield return new object?[] { Iso3166Part2.FR._NOR, "fra", null };
                    yield return new object?[] { Iso3166Part2.FR._NOR, "ar", null };
                }
            }
        }

        #endregion

        #region Test: GetSubdivisionCode_OnIso3166Part2SubdivisionCode

        [Theory]
        [MemberData(nameof(GetSubdivisionCode_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionCode_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
        public void GetSubdivisionCode_OnIso3166Part2SubdivisionCode_ReturnsSubdivisionCode(Iso3166Part2 testCode, string expectedResult)
        {
            // act
            var testResult = testCode.GetSubdivisionCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetSubdivisionCode_OnIso3166Part2SubdivisionCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso3166Part2.GR._69, "GR-69" };
                    yield return new object[] { Iso3166Part2.GR._A, "GR-A" };
                }
            }
        }

        #endregion

        #region Test: GetSubdivisionNativeName_OnIso3166Part2SubdivisionCode

        [Theory]
        [MemberData(nameof(GetSubdivisionNativeName_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionNativeName_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
        public void GetSubdivisionNativeName_OnIso3166Part2SubdivisionCode_ReturnsNativeName(Iso3166Part2 testCode, string languageCode, string? romanizationSystem, string? expectedResult)
        {
            // act
            var testResult = testCode.GetSubdivisionNativeName(languageCode, romanizationSystem);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetSubdivisionNativeName_OnIso3166Part2SubdivisionCode_TestCaseGenerator
        {
            public static IEnumerable<object?[]> TestCases
            {
                get
                {
                    yield return new object?[] { Iso3166Part2.CA._NT, "en", null, "Northwest Territories" };
                    yield return new object?[] { Iso3166Part2.CA._NT, "fra", null, "Territoires du Nord-Ouest" };
                    yield return new object?[] { Iso3166Part2.CA._NS, "la", null, null };

                    yield return new object[] { Iso3166Part2.KM._A, "ar", "BGN/PCGN 1956", "Anjwān" };
                    yield return new object[] { Iso3166Part2.KM._A, "ara", "conventional names", "Andjouân" };
                }
            }
        }

        #endregion

        #region Test: GetSubdivisionNativeNameLocalVariant_OnIso3166Part2SubdivisionCode

        [Theory]
        [MemberData(nameof(GetSubdivisionNativeNameLocalVariant_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionNativeNameLocalVariant_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
        public void GetSubdivisionNativeNameLocalVariant_OnIso3166Part2SubdivisionCode_ReturnsNativeNamePlural(Iso3166Part2 testCode, string languageCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetSubdivisionNativeNameLocalVariant(languageCode);

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetSubdivisionNativeNameLocalVariant_OnIso3166Part2SubdivisionCode_TestCaseGenerator
        {
            public static IEnumerable<object?[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso3166Part2.CL._LI, "es", "O'Higgins" };
                    yield return new object?[] { Iso3166Part2.CL._RM, "spa", null };
                    yield return new object?[] { Iso3166Part2.CL._TA, "de", null };
                }
            }
        }

        #endregion

        #region Test: GetSubdivisionParentCode_OnIso3166Part2SubdivisionCode

        [Theory]
        [MemberData(nameof(GetSubdivisionParentCode_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionParentCode_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
        public void GetSubdivisionParentCode_OnIso3166Part2SubdivisionCode_ReturnsParentCode(Iso3166Part2 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetSubdivisionParentCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetSubdivisionParentCode_OnIso3166Part2SubdivisionCode_TestCaseGenerator
        {
            public static IEnumerable<object?[]> TestCases
            {
                get
                {
                    yield return new object?[] { Iso3166Part2.FI._11, null };
                    yield return new object?[] { Iso3166Part2.GB._ENG, null };
                    yield return new object?[] { Iso3166Part2.GB._NIR, null };
                    yield return new object[] { Iso3166Part2.GB._DEV, "GB-ENG" };
                    yield return new object[] { Iso3166Part2.GB._BFS, "GB-NIR" };
                    yield return new object[] { Iso3166Part2.GB._ANS, "GB-SCT" };
                    yield return new object[] { Iso3166Part2.GB._CRF, "GB-WLS" };
                }
            }
        }

        #endregion
    }
}