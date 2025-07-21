using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.ISO639.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Iso639ExtensionTests
    {
        #region Test: GetEnglishName_OnIso639Part1LanguageCode

        [Theory]
        [MemberData(nameof(GetEnglishName_OnIso639Part1LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso639Part1LanguageCode_TestCaseGenerator))]
        public void GetEnglishName_OnIso639Part1LanguageCode_ReturnsEnglishName(Iso639Part1 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetEnglishName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetEnglishName_OnIso639Part1LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part1.en, "English" }; }
            }
        }

        #endregion

        #region Test: GetEnglishName_OnIso639Part2BLanguageCode

        [Theory]
        [MemberData(nameof(GetEnglishName_OnIso639Part2BLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso639Part2BLanguageCode_TestCaseGenerator))]
        public void GetEnglishName_OnIso639Part2BLanguageCode_ReturnsEnglishName(Iso639Part2B testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetEnglishName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetEnglishName_OnIso639Part2BLanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part2B.eng, "English" }; }
            }
        }

        #endregion

        #region Test: GetEnglishName_OnIso639Part2TLanguageCode

        [Theory]
        [MemberData(nameof(GetEnglishName_OnIso639Part2TLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso639Part2TLanguageCode_TestCaseGenerator))]
        public void GetEnglishName_OnIso639Part2TLanguageCode_ReturnsEnglishName(Iso639Part2T testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetEnglishName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetEnglishName_OnIso639Part2TLanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part2T.eng, "English" }; }
            }
        }

        #endregion

        #region Test: GetEnglishName_OnIso639Part3LanguageCode

        [Theory]
        [MemberData(nameof(GetEnglishName_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso639Part3LanguageCode_TestCaseGenerator))]
        public void GetEnglishName_OnIso639Part3LanguageCode_ReturnsEnglishName(Iso639Part3 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetEnglishName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetEnglishName_OnIso639Part3LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part3.eng, "English" }; }
            }
        }

        #endregion

        #region Test: GetEnglishName_OnIso639Part5LanguageCode

        [Theory]
        [MemberData(nameof(GetEnglishName_OnIso639Part5LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso639Part5LanguageCode_TestCaseGenerator))]
        public void GetEnglishName_OnIso639Part5LanguageCode_ReturnsEnglishName(Iso639Part5 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetEnglishName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetEnglishName_OnIso639Part5LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part5.gem, "Germanic languages" }; }
            }
        }

        #endregion

        #region Test: GetEnglishNames_OnIso639Part1LanguageCode

        [Theory]
        [MemberData(nameof(GetEnglishNames_OnIso639Part1LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishNames_OnIso639Part1LanguageCode_TestCaseGenerator))]
        public void GetEnglishNames_OnIso639Part1LanguageCode_ReturnsEnglishNames(Iso639Part1 testCode, string[] expectedResult)
        {
            // act
            var testResult = testCode.GetEnglishNames();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        private class GetEnglishNames_OnIso639Part1LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part1.ca, new String[] { "Catalan", "Valencian" } }; }
            }
        }

        #endregion

        #region Test: GetEnglishNames_OnIso639Part2BLanguageCode

        [Theory]
        [MemberData(nameof(GetEnglishNames_OnIso639Part2BLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishNames_OnIso639Part2BLanguageCode_TestCaseGenerator))]
        public void GetEnglishNames_OnIso639Part2BLanguageCode_ReturnsEnglishNames(Iso639Part2B testCode, string[] expectedResult)
        {
            // act
            var testResult = testCode.GetEnglishNames();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        private class GetEnglishNames_OnIso639Part2BLanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part2B.cat, new String[] { "Catalan", "Valencian" } }; }
            }
        }

        #endregion

        #region Test: GetEnglishNames_OnIso639Part2TLanguageCode

        [Theory]
        [MemberData(nameof(GetEnglishNames_OnIso639Part2TLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishNames_OnIso639Part2TLanguageCode_TestCaseGenerator))]
        public void GetEnglishNames_OnIso639Part2TLanguageCode_ReturnsEnglishNames(Iso639Part2T testCode, string[] expectedResult)
        {
            // act
            var testResult = testCode.GetEnglishNames();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        private class GetEnglishNames_OnIso639Part2TLanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part2T.cat, new String[] { "Catalan", "Valencian" } }; }
            }
        }

        #endregion

        #region Test: GetEnglishNames_OnIso639Part5LanguageCode

        [Theory]
        [MemberData(nameof(GetEnglishNames_OnIso639Part5LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishNames_OnIso639Part5LanguageCode_TestCaseGenerator))]
        public void GetEnglishNames_OnIso639Part5LanguageCode_ReturnsEnglishNames(Iso639Part5 testCode, string[] expectedResult)
        {
            // act
            var testResult = testCode.GetEnglishNames();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        private class GetEnglishNames_OnIso639Part5LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part5.gem, new string[] { "Germanic languages" } }; }
            }
        }

        #endregion

        #region Test: GetFrenchName_OnIso639Part1LanguageCode

        [Theory]
        [MemberData(nameof(GetFrenchName_OnIso639Part1LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchName_OnIso639Part1LanguageCode_TestCaseGenerator))]
        public void GetFrenchName_OnIso639Part1LanguageCode_ReturnsFrenchName(Iso639Part1 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetFrenchName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetFrenchName_OnIso639Part1LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part1.fr, "français" }; }
            }
        }

        #endregion

        #region Test: GetFrenchName_OnIso639Part2BLanguageCode

        [Theory]
        [MemberData(nameof(GetFrenchName_OnIso639Part2BLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchName_OnIso639Part2BLanguageCode_TestCaseGenerator))]
        public void GetFrenchName_OnIso639Part2BLanguageCode_ReturnsFrenchName(Iso639Part2B testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetFrenchName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetFrenchName_OnIso639Part2BLanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part2B.fre, "français" }; }
            }
        }

        #endregion

        #region Test: GetFrenchName_OnIso639Part2TLanguageCode

        [Theory]
        [MemberData(nameof(GetFrenchName_OnIso639Part2TLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchName_OnIso639Part2TLanguageCode_TestCaseGenerator))]
        public void GetFrenchName_OnIso639Part2TLanguageCode_ReturnsFrenchName(Iso639Part2T testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetFrenchName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetFrenchName_OnIso639Part2TLanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part2T.fra, "français" }; }
            }
        }

        #endregion

        #region Test: GetFrenchName_OnIso639Part5LanguageCode

        [Theory]
        [MemberData(nameof(GetFrenchName_OnIso639Part5LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchName_OnIso639Part5LanguageCode_TestCaseGenerator))]
        public void GetFrenchName_OnIso639Part5LanguageCode_ReturnsFrenchName(Iso639Part5 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetFrenchName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetFrenchName_OnIso639Part5LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part5.ine, "indo-européennes, langues" }; }
            }
        }

        #endregion

        #region Test: GetFrenchNames_OnIso639Part1LanguageCode

        [Theory]
        [MemberData(nameof(GetFrenchNames_OnIso639Part1LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchNames_OnIso639Part1LanguageCode_TestCaseGenerator))]
        public void GetFrenchNames_OnIso639Part1LanguageCode_ReturnsFrenchNames(Iso639Part1 testCode, string[] expectedResult)
        {
            // act
            var testResult = testCode.GetFrenchNames();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        private class GetFrenchNames_OnIso639Part1LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part1.gd, new string[] { "gaélique", "gaélique écossais" } }; }
            }
        }

        #endregion

        #region Test: GetFrenchNames_OnIso639Part2BLanguageCode

        [Theory]
        [MemberData(nameof(GetFrenchNames_OnIso639Part2BLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchNames_OnIso639Part2BLanguageCode_TestCaseGenerator))]
        public void GetFrenchNames_OnIso639Part2BLanguageCode_ReturnsFrenchNames(Iso639Part2B testCode, string[] expectedResult)
        {
            // act
            var testResult = testCode.GetFrenchNames();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        private class GetFrenchNames_OnIso639Part2BLanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part2B.gsw, new string[] { "suisse alémanique", "alémanique", "alsacien" } }; }
            }
        }

        #endregion

        #region Test: GetFrenchNames_OnIso639Part2TLanguageCode

        [Theory]
        [MemberData(nameof(GetFrenchNames_OnIso639Part2TLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchNames_OnIso639Part2TLanguageCode_TestCaseGenerator))]
        public void GetFrenchNames_OnIso639Part2TLanguageCode_ReturnsFrenchNames(Iso639Part2T testCode, string[] expectedResult)
        {
            // act
            var testResult = testCode.GetFrenchNames();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        private class GetFrenchNames_OnIso639Part2TLanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part2T.nno, new string[] { "norvégien nynorsk", "nynorsk, norvégien" } }; }
            }
        }

        #endregion

        #region Test: GetFrenchNames_OnIso639Part5LanguageCode

        [Theory]
        [MemberData(nameof(GetFrenchNames_OnIso639Part5LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchNames_OnIso639Part5LanguageCode_TestCaseGenerator))]
        public void GetFrenchNames_OnIso639Part5LanguageCode_ReturnsFrenchNames(Iso639Part5 testCode, string[] expectedResult)
        {
            // act
            var testResult = testCode.GetFrenchNames();

            // assert
            testResult.Should().BeEquivalentTo(expectedResult);
        }

        private class GetFrenchNames_OnIso639Part5LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part5.cel, new string[] { "celtiques, langues", "celtes, langues" } }; }
            }
        }

        #endregion

        #region Test: GetInvertedName_OnIso639Part3LanguageCode

        [Theory]
        [MemberData(nameof(GetInvertedName_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetInvertedName_OnIso639Part3LanguageCode_TestCaseGenerator))]
        public void GetInvertedName_OnIso639Part3LanguageCode_ReturnsInvertedName(Iso639Part3 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetInvertedName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetInvertedName_OnIso639Part3LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part3.rcf, "Creole French, Réunion" }; }
            }
        }

        #endregion

        #region Test: GetLanguageType_OnIso639Part3LanguageCode

        [Theory]
        [MemberData(nameof(GetLanguageType_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetLanguageType_OnIso639Part3LanguageCode_TestCaseGenerator))]
        public void GetLanguageType_OnIso639Part3LanguageCode_ReturnsLanguageType(Iso639Part3 testCode, Iso639LanguageType expectedResult)
        {
            // act
            var testResult = testCode.GetLanguageType();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetLanguageType_OnIso639Part3LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso639Part3.deu, Iso639LanguageType.Living };
                    yield return new object[] { Iso639Part3.arc, Iso639LanguageType.Historical };
                    yield return new object[] { Iso639Part3.chh, Iso639LanguageType.Extinct };
                    yield return new object[] { Iso639Part3.epo, Iso639LanguageType.Constructed };
                    yield return new object[] { Iso639Part3.und, Iso639LanguageType.Special };
                }
            }
        }

        #endregion

        #region Test: GetMacrolanguageCode_OnIso639Part3LanguageCode

        [Theory]
        [MemberData(nameof(GetMacrolanguageCode_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetMacrolanguageCode_OnIso639Part3LanguageCode_TestCaseGenerator))]
        public void GetMacrolanguageCode_OnIso639Part3LanguageCode_ReturnsMacroLanguageCode(Iso639Part3 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetMacrolanguageCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetMacrolanguageCode_OnIso639Part3LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part3.ind, "msa" }; }
            }
        }

        #endregion

        #region Test: GetPart1Code_OnIso639Part2BLanguageCode

        [Theory]
        [MemberData(nameof(GetPart1Code_OnIso639Part2BLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart1Code_OnIso639Part2BLanguageCode_TestCaseGenerator))]
        public void GetPart1Code_OnIso639Part2BLanguageCode_ReturnsPart1LanguageCode(Iso639Part2B testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetPart1Code();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetPart1Code_OnIso639Part2BLanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part2B.arm, "hy" }; }
            }
        }

        #endregion

        #region Test: GetPart1Code_OnIso639Part2TLanguageCode

        [Theory]
        [MemberData(nameof(GetPart1Code_OnIso639Part2TLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart1Code_OnIso639Part2TLanguageCode_TestCaseGenerator))]
        public void GetPart1Code_OnIso639Part2TLanguageCode_ReturnsPart1LanguageCode(Iso639Part2T testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetPart1Code();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetPart1Code_OnIso639Part2TLanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part2T.hye, "hy" }; }
            }
        }

        #endregion

        #region Test: GetPart1Code_OnIso639Part3LanguageCode

        [Theory]
        [MemberData(nameof(GetPart1Code_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart1Code_OnIso639Part3LanguageCode_TestCaseGenerator))]
        public void GetPart1Code_OnIso639Part3LanguageCode_ReturnsPart1LanguageCode(Iso639Part3 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetPart1Code();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetPart1Code_OnIso639Part3LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part3.mri, "mi" }; }
            }
        }

        #endregion

        #region Test: GetPart2BCode_OnIso639Part1LanguageCode

        [Theory]
        [MemberData(nameof(GetPart2BCode_OnIso639Part1LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart2BCode_OnIso639Part1LanguageCode_TestCaseGenerator))]
        public void GetPart2BCode_OnIso639Part1LanguageCode_ReturnsPart2BLanguageCode(Iso639Part1 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetPart2BCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetPart2BCode_OnIso639Part1LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part1.mi, "mao" }; }
            }
        }

        #endregion

        #region Test: GetPart2BCode_OnIso639Part3LanguageCode

        [Theory]
        [MemberData(nameof(GetPart2BCode_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart2BCode_OnIso639Part3LanguageCode_TestCaseGenerator))]
        public void GetPart2BCode_OnIso639Part3LanguageCode_ReturnsPart3LanguageCode(Iso639Part3 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetPart2BCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetPart2BCode_OnIso639Part3LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part3.mri, "mao" }; }
            }
        }

        #endregion

        #region Test: GetPart2TCode_OnIso639Part1LanguageCode

        [Theory]
        [MemberData(nameof(GetPart2TCode_OnIso639Part1LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart2TCode_OnIso639Part1LanguageCode_TestCaseGenerator))]
        public void GetPart2TCode_OnIso639Part1LanguageCode_ReturnsPart2TLanguageCode(Iso639Part1 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetPart2TCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetPart2TCode_OnIso639Part1LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part1.mi, "mri" }; }
            }
        }

        #endregion

        #region Test: GetPart2TCode_OnIso639Part3LanguageCode

        [Theory]
        [MemberData(nameof(GetPart2TCode_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart2TCode_OnIso639Part3LanguageCode_TestCaseGenerator))]
        public void GetPart2TCode_OnIso639Part3LanguageCode_ReturnsPart2TLanguageCode(Iso639Part3 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetPart2TCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetPart2TCode_OnIso639Part3LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part3.mri, "mri" }; }
            }
        }

        #endregion

        #region Test: GetPrintName_OnIso639Part3LanguageCode

        [Theory]
        [MemberData(nameof(GetPrintName_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPrintName_OnIso639Part3LanguageCode_TestCaseGenerator))]
        public void GetPrintName_OnIso639Part3LanguageCode_ReturnsPrintName(Iso639Part3 testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetPrintName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetPrintName_OnIso639Part3LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get { yield return new object[] { Iso639Part3.hun, "Hungarian" }; }
            }
        }

        #endregion

        #region Test: GetScope_OnIso639Part3LanguageCode

        [Theory]
        [MemberData(nameof(GetScope_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetScope_OnIso639Part3LanguageCode_TestCaseGenerator))]
        public void GetScope_OnIso639Part3LanguageCode_ReturnsScope(Iso639Part3 testCode, Iso639LanguageScope expectedResult)
        {
            // act
            var testResult = testCode.GetScope();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetScope_OnIso639Part3LanguageCode_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso639Part3.eng, Iso639LanguageScope.Individual };
                    yield return new object[] { Iso639Part3.est, Iso639LanguageScope.Macrolanguage };
                    yield return new object[] { Iso639Part3.und, Iso639LanguageScope.Special };
                }
            }
        }

        #endregion
    }
}