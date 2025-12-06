using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using DataStandardizer.Core;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using FluentAssertions;

namespace DataStandardizer.LanguageTag.Tests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Bcp47LanguageTagTests : IClassFixture<Bcp47LanguageTagFixture>
{
    #region Declarations

    private static readonly char[] TestCharacters;
    private static readonly char[] TestDigits;
    private static readonly char[] TestLetters;
    private static readonly string[] _validLanguageTags;

    private readonly Bcp47LanguageTagFixture _fixture;

    #endregion

    static Bcp47LanguageTagTests()
    {
        const int digitCharacterBaseOffset = 48, letterCharacterBaseOffset = 97;
        TestDigits = Enumerable.Range(digitCharacterBaseOffset, 10).Select(characterNumber => (char)characterNumber).ToArray();
        TestLetters = Enumerable.Range(letterCharacterBaseOffset, 26).Select(characterNumber => (char)characterNumber).ToArray();
        TestCharacters = TestDigits.Concat(TestLetters).ToArray();

        _validLanguageTags = new[]
        {
            "af", "am", "ar", "arn", "ary", "as", "az", "ba", "be", "bg", "bn", "bo", "br", "bs", "ca", "ckb", "co", "cs", "cy", "da", "de", "dsb", "dv", "el", "en", "es", "et", "eu", "fa", "fi", "fil", "fo", "fr", "fy", "ga", "gd", "gil", "gl",
            "gsw", "gu", "ha", "he", "hi", "hr", "hsb", "hu", "hy", "id", "ig", "ii", "is", "it", "iu", "ja", "ka", "kk", "kl", "km", "kn", "ko", "kok", "ku", "ky", "lb", "lo", "lt", "lv", "mi", "mk", "ml", "mn", "moh", "mr", "ms", "mt", "my", "nb",
            "ne", "nl", "nn", "no", "oc", "or", "pap", "pa", "pl", "prs", "ps", "pt", "quc", "qu", "rm", "ro", "ru", "rw", "sa", "sah", "se", "si", "sk", "sl", "sma", "smj", "smn", "sms", "sq", "sr", "st", "sv", "sw", "syc", "ta", "te", "tg", "th",
            "tk", "tn", "tr", "tt", "tzm", "ug", "uk", "ur", "uz", "vi", "wo", "xh", "yo", "zh", "zu", "es-419", "rm-sursilv", "sr-Cyrl", "nan-Hant-TW", "yue-Hant-HK", "gsw-u-sd-chzh"
        };
    }

    public Bcp47LanguageTagTests(Bcp47LanguageTagFixture fixture)
    {
        _fixture = fixture;
    }

    #region Test: CheckExtendedLanguageSubtag_WithInvalidSubtag

    [Theory]
    [InlineData("a"), InlineData("ab"), InlineData("abcd"), InlineData("123"), InlineData("a2c")]
    public void CheckExtendedLanguageSubtag_WithInvalidSubtag_ReturnsFalse(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckExtendedLanguageSubtag(testSubtag);

        // assert
        testResult.Should().BeFalse("the subtag {0} is not a valid Extended Language subtag", testSubtag);
    }

    #endregion

    #region Test: CheckExtendedLanguageSubtag_WithValidSubtag

    [Theory]
    [ClassData(typeof(CheckExtendedLanguageSubtag_WithValidSubtag_TestCaseFactory))]
    public void CheckExtendedLanguageSubtag_WithValidSubtag_ReturnsTrue(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckExtendedLanguageSubtag(testSubtag);

        // assert
        testResult.Should().BeTrue("the subtag {0} is a valid Extended Language subtag", testSubtag);
    }

    private class CheckExtendedLanguageSubtag_WithValidSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var subtagCharacter in TestLetters)
            {
                yield return new object[] { string.Concat(Enumerable.Repeat(subtagCharacter, 3)) };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: CheckExtensionSubtag_WithDuplicateSubtags

    [Theory]
    [InlineData("a-bbb-a-ccc")]
    public void CheckExtensionSubtag_WithDuplicateSubtags_ReturnsFalse(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckExtensionSubtag(testSubtag);

        // assert
        testResult.Should().BeFalse("extension subtags are duplicated");
    }

    #endregion

    #region Test: CheckExtensionSubtag_WithInvalidSubtag

    [Theory]
    [InlineData("ab"), InlineData("12"), InlineData("x-1"), InlineData("x-a"), InlineData("x-123456789"), InlineData("x-abcdefghi")]
    public void CheckExtensionSubtag_WithInvalidSubtag_ReturnsFalse(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckExtensionSubtag(testSubtag);

        // assert
        testResult.Should().BeFalse("the subtag {0} is not a valid Extension subtag", testSubtag);
    }

    #endregion

    #region Test: CheckExtensionSubtag_WithValidSubtag

    [Theory]
    [ClassData(typeof(CheckExtensionSubtag_WithValidSubtag_TestCaseFactory))]
    public void CheckExtensionSubtag_WithValidSubtag_ReturnsTrue(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckExtensionSubtag(testSubtag);

        // assert
        testResult.Should().BeTrue("the subtag {0} is a valid Extension subtag", testSubtag);
    }

    private class CheckExtensionSubtag_WithValidSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var singletonCharacter in TestCharacters)
            {
                if (singletonCharacter == 'x') continue;

                foreach (var subtagCharacter in TestCharacters)
                    for (var characterCount = 2; characterCount <= 8; characterCount++)
                    for (var subtagCount = 1; subtagCount <= 2; subtagCount++)
                    {
                        var subtag = string.Concat(Enumerable.Repeat(subtagCharacter, characterCount));
                        yield return new object[] { $"{singletonCharacter}{string.Concat(Enumerable.Repeat($"-{subtag}", subtagCount))}" };
                    }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: CheckPrimaryLanguageSubtag_WithInvalidSubtag

    [Theory]
    [ClassData(typeof(CheckPrimaryLanguageSubtag_WithInvalidSubtag_TestCaseFactory))]
    public void CheckPrimaryLanguageSubtag_WithInvalidSubtag_ReturnsFalse(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckPrimaryLanguageSubtag(testSubtag);

        // assert
        testResult.Should().BeFalse("the subtag {0} is not a valid Primary Language subtag", testSubtag);
    }

    private class CheckPrimaryLanguageSubtag_WithInvalidSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var testCharacter in TestCharacters)
            {
                yield return new object[] { testCharacter.ToString() };
                yield return new object[] { string.Concat(Enumerable.Repeat(testCharacter, 4)) };
                yield return new object[] { string.Concat(Enumerable.Repeat(testCharacter, 9)) };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: CheckPrimaryLanguageSubtag_WithIso639Part1CodeSubtag

    [Theory]
    [ClassData(typeof(CheckPrimaryLanguageSubtag_WithIso639Part1CodeSubtag_TestCaseFactory))]
    public void CheckPrimaryLanguageSubtag_WithIso639Part1CodeSubtag_ReturnsTrue(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckPrimaryLanguageSubtag(testSubtag);

        // assert
        testResult.Should().BeTrue("the subtag {0} is a valid Primary Language subtag", testSubtag);
    }

    private class CheckPrimaryLanguageSubtag_WithIso639Part1CodeSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var testSubtag in StringEnum.GetNames<Iso639Part1Language>())
            {
                yield return new object[] { testSubtag };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: CheckPrimaryLanguageSubtag_WithIso639Part2TCodeSubtag

    [Theory]
    [ClassData(typeof(CheckPrimaryLanguageSubtag_WithIso639Part2TCodeSubtag_TestCaseFactory))]
    public void CheckPrimaryLanguageSubtag_WithIso639Part2TCodeSubtag_ReturnsTrue(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckPrimaryLanguageSubtag(testSubtag);

        // assert
        testResult.Should().BeTrue("the subtag {0} is a valid Primary Language subtag", testSubtag);
    }

    private class CheckPrimaryLanguageSubtag_WithIso639Part2TCodeSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var testSubtag in StringEnum.GetNames<Iso639Part2TLanguage>())
            {
                yield return new object[] { testSubtag };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: CheckPrimaryLanguageSubtag_WithIso639Part3CodeSubtag

    [Theory]
    [ClassData(typeof(CheckPrimaryLanguageSubtag_WithIso639Part3CodeSubtag_TestCaseFactory))]
    public void CheckPrimaryLanguageSubtag_WithIso639Part3CodeSubtag_ReturnsTrue(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckPrimaryLanguageSubtag(testSubtag);

        // assert
        testResult.Should().BeTrue("the subtag {0} is a valid Primary Language subtag", testSubtag);
    }

    private class CheckPrimaryLanguageSubtag_WithIso639Part3CodeSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var testSubtag in StringEnum.GetNames<Iso639Part3Language>())
            {
                yield return new object[] { testSubtag };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: CheckPrimaryLanguageSubtag_WithIso639Part5CodeSubtag

    [Theory]
    [ClassData(typeof(CheckPrimaryLanguageSubtag_WithIso639Part5CodeSubtag_TestCaseFactory))]
    public void CheckPrimaryLanguageSubtag_WithIso639Part5CodeSubtag_ReturnsTrue(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckPrimaryLanguageSubtag(testSubtag);

        // assert
        testResult.Should().BeTrue("the subtag {0} is a valid Primary Language subtag", testSubtag);
    }

    private class CheckPrimaryLanguageSubtag_WithIso639Part5CodeSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var testSubtag in StringEnum.GetNames<Iso639Part5LanguageFamily>())
            {
                yield return new object[] { testSubtag };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: CheckPrimaryLanguageSubtag_WithRegisteredSubtag

    [Theory]
    [InlineData("aaaaa"), InlineData("bbbbbb"), InlineData("ccccccc"), InlineData("dddddddd")]
    public void CheckPrimaryLanguageSubtag_WithRegisteredSubtag_ReturnsTrue(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckPrimaryLanguageSubtag(testSubtag);

        // assert
        testResult.Should().BeTrue("the subtag {0} is a valid Primary Language subtag", testSubtag);
    }

    #endregion

    #region Test: CheckPrivateUseSubtag_WithInvalidSubtag

    [Theory]
    [InlineData("x"), InlineData("x-"), InlineData("x-123456789"), InlineData("x-abcdefghi")]
    public void CheckPrivateUseSubtag_WithInvalidSubtag_ReturnsFalse(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckPrivateUseSubtag(testSubtag);

        // assert
        testResult.Should().BeFalse("the subtag {0} is not a valid Private Use subtag", testSubtag);
    }

    #endregion

    #region Test: CheckPrivateUseSubtag_WithValidSubtag

    [Theory]
    [ClassData(typeof(CheckPrivateUseSubtag_WithValidSubtag_TestCaseFactory))]
    public void CheckPrivateUseSubtag_WithValidSubtag_ReturnsTrue(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckPrivateUseSubtag(testSubtag);

        // assert
        testResult.Should().BeTrue("the subtag {0} is a valid Private Use subtag", testSubtag);
    }

    private class CheckPrivateUseSubtag_WithValidSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var subtagCharacter in TestCharacters)
                for (var characterCount = 1; characterCount <= 8; characterCount++)
                for (var subtagCount = 1; subtagCount <= 2; subtagCount++)
                {
                    var subtag = string.Concat(Enumerable.Repeat(subtagCharacter, characterCount));
                    yield return new object[] { $"x{string.Concat(Enumerable.Repeat($"-{subtag}", subtagCount))}" };
                }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: CheckRegionSubtag_WithInvalidSubtag

    [Theory]
    [InlineData("a"), InlineData("ccc"), InlineData("1"), InlineData("22"), InlineData("4444")]
    public void CheckRegionSubtag_WithInvalidSubtag_ReturnsFalse(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckRegionSubtag(testSubtag);

        // assert
        testResult.Should().BeFalse("the subtag {0} is not a valid Region subtag", testSubtag);
    }

    #endregion

    #region Test: CheckRegionSubtag_WithIso3166Part1Alpha2CodeSubtag

    [Theory]
    [ClassData(typeof(CheckRegionSubtag_WithIso3166Part1Alpha2CodeSubtag_TestCaseFactory))]
    public void CheckRegionSubtag_WithIso3166Part1Alpha2CodeSubtag_ReturnsTrue(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckRegionSubtag(testSubtag);

        // assert
        testResult.Should().BeTrue("the subtag {0} is a valid Region subtag", testSubtag);
    }

    private class CheckRegionSubtag_WithIso3166Part1Alpha2CodeSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var testSubtag in Enum.GetNames<Iso3166Part1Alpha2Country>())
            {
                yield return new object[] { testSubtag };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: CheckRegionSubtag_WithUnM49CodeSubtag

    [Theory]
    [ClassData(typeof(CheckRegionSubtag_WithUnM49CodeSubtag_TestCaseFactory))]
    public void CheckRegionSubtag_WithUnM49CodeSubtag_ReturnsTrue(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckRegionSubtag(testSubtag);

        // assert
        testResult.Should().BeTrue("the subtag {0} is a valid Region subtag", testSubtag);
    }

    private class CheckRegionSubtag_WithUnM49CodeSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        private static readonly IEnumerable<object[]> _testCases;

        static CheckRegionSubtag_WithUnM49CodeSubtag_TestCaseFactory()
        {
            var globalCodes = Enum.GetValues<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetGlobalCode()).Where(code => code is not null).Cast<ushort>();
            var regionCodes = Enum.GetValues<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetRegionCode()).Where(code => code is not null).Cast<ushort>();
            var subRegionCodes = Enum.GetValues<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetSubRegionCode()).Where(code => code is not null).Cast<ushort>();
            var intermediateRegionCode = Enum.GetValues<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetIntermediateRegionCode()).Where(code => code is not null).Cast<ushort>();
            _testCases = globalCodes.Union(regionCodes).Union(subRegionCodes).Union(intermediateRegionCode).Select(code => new object[] { $"{code:000}" }).AsEnumerable();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            return _testCases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_testCases).GetEnumerator();
        }
    }

    #endregion

    #region Test: CheckRegionSubtag_WithUnM49CodeSubtag

    [Theory]
    [InlineData("1"), InlineData("a"), InlineData("a2cd")]
    public void CheckScriptSubtag_WithInvalidSubtag_ReturnsFalse(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckScriptSubtag(testSubtag);

        // assert
        testResult.Should().BeFalse("the subtag {0} is not a valid Script subtag", testSubtag);
    }

    #endregion

    #region Test: CheckScriptSubtag_WithIso15924CodeSubtag

    [Theory]
    [ClassData(typeof(CheckScriptSubtag_WithIso15924CodeSubtag_TestCaseFactory))]
    public void CheckScriptSubtag_WithIso15924CodeSubtag_ReturnsTrue(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckScriptSubtag(testSubtag);

        // assert
        testResult.Should().BeTrue("the subtag {0} is a valid Script subtag", testSubtag);
    }

    private class CheckScriptSubtag_WithIso15924CodeSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var testSubtag in Enum.GetNames<Iso15924Script>())
            {
                yield return new object[] { testSubtag };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: CheckVariantSubtag_WithDuplicateSubtags

    [Theory]
    [InlineData("1901-1901")]
    public void CheckVariantSubtag_WithDuplicateSubtags_ReturnsFalse(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckVariantSubtag(testSubtag);

        // assert
        testResult.Should().BeFalse("variant subtags are duplicated");
    }

    #endregion

    #region Test: CheckVariantSubtag_WithInvalidSubtag

    [Theory]
    [InlineData("a"), InlineData("1"), InlineData("1bc"), InlineData("abcd"), InlineData("123"), InlineData("123456789"), InlineData("abcdefghi")]
    public void CheckVariantSubtag_WithInvalidSubtag_ReturnsFalse(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckVariantSubtag(testSubtag);

        // assert
        testResult.Should().BeFalse("the subtag {0} is not a valid Variant subtag", testSubtag);
    }

    #endregion

    #region Test: CheckVariantSubtag_WithValidSubtag

    [Theory]
    [ClassData(typeof(CheckVariantSubtag_WithValidSubtag_TestCaseFactory))]
    public void CheckVariantSubtag_WithValidSubtag_ReturnsTrue(string testSubtag)
    {
        // act
        var testResult = Bcp47LanguageTag.CheckVariantSubtag(testSubtag);

        // assert
        testResult.Should().BeTrue("the subtag {0} is a valid Variant subtag", testSubtag);
    }

    private class CheckVariantSubtag_WithValidSubtag_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var subtagCharacter in TestLetters)
                for (var characterCount = 5; characterCount <= 8; characterCount++)
                {
                    yield return new object[] { string.Concat(Enumerable.Repeat(subtagCharacter, characterCount)) };
                }

            foreach (var subtagDigit in TestDigits)
            foreach (var subtagCharacter in TestCharacters)
            {
                yield return new object[] { $"{subtagDigit}{string.Concat(Enumerable.Repeat(subtagCharacter, 3))}" };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: ExtendedLanguageSubtags_OnDefaultLanguageTag

    [Fact]
    public void ExtendedLanguageSubtags_OnDefaultLanguageTag_ThrowsInvalidOperationException()
    {
        // arrange
        var testLanguageTag = new Bcp47LanguageTag();

        // act
        Action testAction = () => _ = testLanguageTag.ExtendedLanguageSubtags;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Test: ExtendedLanguageSubtags_OnValidLanguageTagWithNoExtendedLanguageSubtags

    [Fact]
    public void ExtendedLanguageSubtags_OnValidLanguageTagWithNoExtendedLanguageSubtags_ReturnsEmptyCollection()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("en");

        // act
        var testResult = testLanguageTag.ExtendedLanguageSubtags;

        // assert
        testResult.Should().NotBeNull();
        testResult.Should().BeEmpty();
    }

    #endregion

    #region Test: ExtendedLanguageSubtags_OnValidLanguageTagWithExtendedLanguageSubtags

    [Theory]
    [ClassData(typeof(ExtendedLanguageSubtags_OnValidLanguageTagWithExtendedLanguageSubtags_TestCaseFactory))]
    public void ExtendedLanguageSubtags_OnValidLanguageTagWithExtendedLanguageSubtags_ReturnsExtendedLanguageSubtags(string testSourceLanguageTag, string[] expectedResult)
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create(testSourceLanguageTag);

        // act
        var testResult = testLanguageTag.ExtendedLanguageSubtags;

        // assert
        testResult.Should().NotBeNull();
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    private class ExtendedLanguageSubtags_OnValidLanguageTagWithExtendedLanguageSubtags_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "en-aaa", new[] { "aaa" } };
            yield return new object[] { "en-aaa-bbb", new[] { "aaa", "bbb" } };
            yield return new object[] { "en-aaa-bbb-ccc", new[] { "aaa", "bbb", "ccc" } };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: ExtensionSubtags_OnDefaultLanguageTag

    [Fact]
    public void ExtensionSubtags_OnDefaultLanguageTag_ThrowsInvalidOperationException()
    {
        // arrange
        var testLanguageTag = new Bcp47LanguageTag();

        // act
        Action testAction = () => _ = testLanguageTag.ExtensionSubtags;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Test: ExtensionSubtags_OnValidLanguageTagWithNoExtensionSubtags

    [Fact]
    public void ExtensionSubtags_OnValidLanguageTagWithNoExtensionSubtags_ReturnsEmptyCollection()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("en");

        // act
        var testResult = testLanguageTag.ExtensionSubtags;

        // assert
        testResult.Should().NotBeNull();
        testResult.Should().BeEmpty();
    }

    #endregion

    #region Test: ExtensionSubtags_OnValidLanguageTagWithExtensionSubtags

    [Theory]
    [ClassData(typeof(ExtensionSubtags_OnValidLanguageTagWithExtensionSubtags_TestCaseFactory))]
    public void ExtensionSubtags_OnValidLanguageTagWithExtensionSubtags_ReturnsExtensionSubtags(string testSourceLanguageTag, Bcp47KeyedSubtag[] expectedResult)
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create(testSourceLanguageTag);

        // act
        var testResult = testLanguageTag.ExtensionSubtags;

        // assert
        testResult.Should().NotBeNull();
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    private class ExtensionSubtags_OnValidLanguageTagWithExtensionSubtags_TestCaseFactory : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var subtagCharacter in TestCharacters)
                for (var subtagCharacterCount = 2; subtagCharacterCount <= 8; subtagCharacterCount++)
                {
                    var subtag = string.Concat(Enumerable.Repeat(subtagCharacter, subtagCharacterCount));

                    for (var subtagCount = 1; subtagCount <= 2; subtagCount++)
                    {
                        var firstExtensionSubtag = $"i{string.Concat(Enumerable.Repeat($"-{subtag}", subtagCount))}";
                        var secondExtensionSubtag = $"u{string.Concat(Enumerable.Repeat($"-{subtag}", subtagCount))}";
                        yield return new object[] { $"en-{firstExtensionSubtag}-{secondExtensionSubtag}", new[] { new Bcp47KeyedSubtag(firstExtensionSubtag), new Bcp47KeyedSubtag(secondExtensionSubtag) } };
                    }
                }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    #region Test: IsWellFormedLanguageTagString_WithInvalidLanguageTag

    [Theory]
    [InlineData(""), InlineData("zz"), InlineData("NotALanguageTag")]
    public void IsWellFormedLanguageTagString_WithInvalidLanguageTag_ReturnsFalse(string testLanguageTag)
    {
        // act
        var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

        // assert
        testResult.Should().BeFalse("the language tag {0} is not in a valid format", testLanguageTag);
    }

    #endregion

    #region Test: IsWellFormedLanguageTagString_WithValidLanguageTag

    [Theory]
    [InlineData("en", "English"),
     InlineData("es-419", "Latin American Spanish"),
     InlineData("rm-sursilv", "Romansh Sursilvan"),
     InlineData("sr-Cyrl", "Serbian written in Cyrillic script"),
     InlineData("nan-Hant-TW", "Min Nan Chinese using traditional Han characters, as spoken in Taiwan"),
     InlineData("yue-Hant-HK", "Cantonese using traditional Han characters, as spoken in Hong Kong"),
     InlineData("gsw-u-sd-chzh", "Zürich German")]
    public void IsWellFormedLanguageTagString_WithValidLanguageTag_ReturnsTrue(string testLanguageTag, string testLanguageTagDescription)
    {
        // act
        var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

        // assert
        testResult.Should().BeTrue("{0} is the language tag for {1}", testLanguageTag, testLanguageTagDescription);
    }

    #endregion

    #region Test: PrimaryLanguageSubtag_OnDefaultLanguageTag

    [Fact]
    public void PrimaryLanguageSubtag_OnDefaultLanguageTag_ThrowsInvalidOperationException()
    {
        // arrange
        var testLanguageTag = new Bcp47LanguageTag();

        // act
        Action testAction = () => _ = testLanguageTag.PrimaryLanguageSubtag;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Test: PrimaryLanguageSubtag_WhenIso639Part1Code

    [Theory]
    [ClassData(typeof(PrimaryLanguageSubtag_WhenIso639Part1Code_TestCaseFactory))]
    public void PrimaryLanguageSubtag_WhenIso639Part1Code_ReturnsPrimaryLanguageSubtag(string testLanguageTag, string expectedResult)
    {
        // arrange
        var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

        // act
        var testResult = testSubject.PrimaryLanguageSubtag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is the Primary Language subtag of language tag {1}", expectedResult, testLanguageTag);
    }

    private class PrimaryLanguageSubtag_WhenIso639Part1Code_TestCaseFactory : IEnumerable<object[]>
    {
        private static readonly IEnumerable<object[]> _testCases;

        static PrimaryLanguageSubtag_WhenIso639Part1Code_TestCaseFactory()
        {
            _testCases = StringEnum.GetNames<Iso639Part1Language>().Select(code => new object[] { $"{code}-x-test", code }).AsEnumerable();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            return _testCases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_testCases).GetEnumerator();
        }
    }

    #endregion

    #region Test: PrimaryLanguageSubtag_WhenIso639Part2TCode

    [Theory]
    [ClassData(typeof(PrimaryLanguageSubtag_WhenIso639Part2TCode_TestCaseFactory))]
    public void PrimaryLanguageSubtag_WhenIso639Part2TCode_ReturnsPrimaryLanguageSubtag(string testLanguageTag, string expectedResult)
    {
        // arrange
        var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

        // act
        var testResult = testSubject.PrimaryLanguageSubtag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is the Primary Language subtag of language tag {1}", expectedResult, testLanguageTag);
    }

    private class PrimaryLanguageSubtag_WhenIso639Part2TCode_TestCaseFactory : IEnumerable<object[]>
    {
        private static readonly IEnumerable<object[]> _testCases;

        static PrimaryLanguageSubtag_WhenIso639Part2TCode_TestCaseFactory()
        {
            _testCases = StringEnum.GetNames<Iso639Part2TLanguage>().Select(code => new object[] { $"{code}-x-test", code }).AsEnumerable();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            return _testCases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_testCases).GetEnumerator();
        }
    }

    #endregion

    #region Test: PrimaryLanguageSubtag_WhenIso639Part3Code

    [Theory]
    [ClassData(typeof(PrimaryLanguageSubtag_WhenIso639Part3Code_TestCaseFactory))]
    public void PrimaryLanguageSubtag_WhenIso639Part3Code_ReturnsPrimaryLanguageSubtag(string testLanguageTag, string expectedResult)
    {
        // arrange
        var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

        // act
        var testResult = testSubject.PrimaryLanguageSubtag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is the Primary Language subtag of the language tag {1}", expectedResult, testLanguageTag);
    }

    private class PrimaryLanguageSubtag_WhenIso639Part3Code_TestCaseFactory : IEnumerable<object[]>
    {
        private static readonly IEnumerable<object[]> _testCases;

        static PrimaryLanguageSubtag_WhenIso639Part3Code_TestCaseFactory()
        {
            _testCases = StringEnum.GetNames<Iso639Part3Language>().Select(code => new object[] { $"{code}-x-test", code }).AsEnumerable();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            return _testCases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_testCases).GetEnumerator();
        }
    }

    #endregion

    #region Test: PrimaryLanguageSubtag_WhenIso639Part5Code

    [Theory]
    [ClassData(typeof(PrimaryLanguageSubtag_WhenIso639Part5Code_TestCaseFactory))]
    public void PrimaryLanguageSubtag_WhenIso639Part5Code_ReturnsPrimaryLanguageSubtag(string testLanguageTag, string expectedResult)
    {
        // arrange
        var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

        // act
        var testResult = testSubject.PrimaryLanguageSubtag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is the Primary Language subtag of the language tag {1}", expectedResult, testLanguageTag);
    }

    private class PrimaryLanguageSubtag_WhenIso639Part5Code_TestCaseFactory : IEnumerable<object[]>
    {
        private static readonly IEnumerable<object[]> _testCases;

        static PrimaryLanguageSubtag_WhenIso639Part5Code_TestCaseFactory()
        {
            _testCases = StringEnum.GetNames<Iso639Part5LanguageFamily>().Select(code => new object[] { $"{code}-x-test", code }).AsEnumerable();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            return _testCases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_testCases).GetEnumerator();
        }
    }

    #endregion

    #region Test: PrimaryLanguageSubtag_WhenReservedPrivateUseCode

    [Theory]
    [MemberData(nameof(PrimaryLanguageSubtag_WhenReservedPrivateUseCode_TestCaseGenerator.TestCases), MemberType = typeof(PrimaryLanguageSubtag_WhenReservedPrivateUseCode_TestCaseGenerator))]
    public void PrimaryLanguageSubtag_WhenReservedPrivateUseCode_ReturnsPrimaryLanguageSubtag(string testLanguageTag, string expectedResult)
    {
        // arrange
        var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

        // act
        var testResult = testSubject.PrimaryLanguageSubtag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is the Primary Language subtag of the language tag {1}", expectedResult, testLanguageTag);
    }

    private class PrimaryLanguageSubtag_WhenReservedPrivateUseCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                for (var secondCharacterIndex = (byte)'a'; secondCharacterIndex <= (byte)'t'; secondCharacterIndex++)
                for (var thirdCharacterIndex = (byte)'a'; thirdCharacterIndex <= (byte)'z'; thirdCharacterIndex++)
                {
                    var primaryLanguageSubtag = $"q{(char)secondCharacterIndex}{(char)thirdCharacterIndex}";
                    yield return new object[] { primaryLanguageSubtag + "-x-test", primaryLanguageSubtag };
                }
            }
        }
    }

    #endregion

    #region Test: PrimaryLanguageSubtag_WhenRegisteredSubtag

    [Theory]
    [InlineData("default-x-test", "default"), InlineData("enochian-x-test", "enochian"), InlineData("klingon-x-test", "klingon"), InlineData("mingo-x-test", "mingo"), InlineData("navajo-x-test", "navajo"), InlineData("guoyu-x-test", "guoyu"),
     InlineData("hakka-x-test", "hakka"), InlineData("xiang-x-test", "xiang")]
    public void PrimaryLanguageSubtag_WhenRegisteredSubtag_ReturnsPrimaryLanguageSubtag(string testLanguageTag, string expectedResult)
    {
        // arrange
        var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

        // act
        var testResult = testSubject.PrimaryLanguageSubtag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is the Primary Language subtag of the language tag {1}", expectedResult, testLanguageTag);
    }

    #endregion

    #region Test: PrivateUseSubtag_OnDefaultLanguageTag

    [Fact]
    public void PrivateUseSubtag_OnDefaultLanguageTag_ThrowsInvalidOperationException()
    {
        // arrange
        var testLanguageTag = new Bcp47LanguageTag();

        // act
        Action testAction = () => _ = testLanguageTag.PrivateUseSubtag;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Test: PrivateUseSubtag_OnValidLanguageTagWithNoPrivateUseSubtags

    [Fact]
    public void PrivateUseSubtag_OnValidLanguageTagWithNoPrivateUseSubtags_ReturnsNull()
    {
        // arrange
        const string languageTag = "en";
        var testLanguageTag = Bcp47LanguageTag.Create(languageTag);

        // act
        var testResult = testLanguageTag.PrivateUseSubtag;

        // assert
        testResult.Should().BeNull("the language tag {0} does not contain a Private Use subtag", languageTag);
    }

    #endregion

    #region Test: PrivateUseSubtag_OnValidLanguageTagWithPrivateUseSubtag

    [Theory]
    [InlineData("en-x-a", "x-a"),
     InlineData("en-x-bb", "x-bb"),
     InlineData("en-x-ccc", "x-ccc"),
     InlineData("en-x-dddd", "x-dddd"),
     InlineData("en-x-eeeee", "x-eeeee"),
     InlineData("en-x-ffffff", "x-ffffff"),
     InlineData("en-x-ggggggg", "x-ggggggg"),
     InlineData("en-x-hhhhhhhh", "x-hhhhhhhh"),
     InlineData("en-x-1", "x-1"),
     InlineData("en-x-88888888", "x-88888888")]
    public void PrivateUseSubtag_OnValidLanguageTagWithPrivateUseSubtag_ReturnsPrivateUseSubtag(string testLanguageTag, string expectedResult)
    {
        // arrange
        var languageTag = Bcp47LanguageTag.Create(testLanguageTag);
        var expectedResultSubtag = new Bcp47KeyedSubtag(expectedResult);

        // act
        var testResult = languageTag.PrivateUseSubtag;

        // assert
        testResult.Should().NotBeNull();
        testResult.Should().Be(expectedResultSubtag, "language tag {0} contains the Private Use subtag {1}", testLanguageTag, expectedResult);
    }

    #endregion

    #region Test: RegionSubtag_OnDefaultLanguageTag

    [Fact]
    public void RegionSubtag_OnDefaultLanguageTag_ThrowsInvalidOperationException()
    {
        // arrange
        var testLanguageTag = new Bcp47LanguageTag();

        // act
        Action testAction = () => _ = testLanguageTag.RegionSubtag;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Test: RegionSubtag_OnValidLanguageTagWithNoRegionSubtag

    [Fact]
    public void RegionSubtag_OnValidLanguageTagWithNoRegionSubtag_ReturnsNull()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("en");

        // act
        var testResult = testLanguageTag.RegionSubtag;

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: RegionSubtag_WhenIso3166Part1Code

    [Theory]
    [ClassData(typeof(RegionSubtag_WhenIso3166Part1Code_TestCaseFactory))]
    public void RegionSubtag_WhenIso3166Part1Code_ReturnsRegionSubtag(string testLanguageTag, string expectedResult)
    {
        // arrange
        var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

        // act
        var testResult = testSubject.RegionSubtag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is the region subtag from the language tag {1}", expectedResult, testLanguageTag);
    }

    private class RegionSubtag_WhenIso3166Part1Code_TestCaseFactory : IEnumerable<object[]>
    {
        private static readonly IEnumerable<object[]> _testCases;

        static RegionSubtag_WhenIso3166Part1Code_TestCaseFactory()
        {
            _testCases = Enum.GetValues<Iso3166Part1Alpha2Country>().Select(code => new object[] { $"en-{code}", code.ToString() }).AsEnumerable();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            return _testCases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_testCases).GetEnumerator();
        }
    }

    #endregion

    #region Test: RegionSubtag_WhenSupranationalUnM49Code

    [Theory]
    [MemberData(nameof(RegionSubtag_WhenSupranationalUnM49Code_TestCaseGenerator.TestCases), MemberType = typeof(RegionSubtag_WhenSupranationalUnM49Code_TestCaseGenerator))]
    public void RegionSubtag_WhenSupranationalUnM49Code_ReturnsRegionSubtag(string testLanguageTag, string expectedResult)
    {
        // arrange
        var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

        // act
        var testResult = testSubject.RegionSubtag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is the region subtag from the language tag {1}", expectedResult, testLanguageTag);
    }

    private class RegionSubtag_WhenSupranationalUnM49Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                var globalM49Codes = Enum.GetValues<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetGlobalCode()).Where(code => code is not null).Cast<ushort>().Distinct();
                foreach (var globalM49Code in globalM49Codes)
                {
                    yield return new object[] { $"en-{globalM49Code:000}", $"{globalM49Code:000}" };
                }

                var regionM49Codes = Enum.GetValues<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetRegionCode()).Where(code => code is not null).Cast<ushort>().Distinct();
                foreach (var regionM49Code in regionM49Codes)
                {
                    yield return new object[] { $"en-{regionM49Code:000}", $"{regionM49Code:000}" };
                }

                var subRegionM49Codes = Enum.GetValues<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetSubRegionCode()).Where(code => code is not null).Cast<ushort>().Distinct();
                foreach (var subRegionM49Code in subRegionM49Codes)
                {
                    yield return new object[] { $"en-{subRegionM49Code:000}", $"{subRegionM49Code:000}" };
                }

                var intermediateRegionM49Codes = Enum.GetValues<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetIntermediateRegionCode()).Where(code => code is not null).Cast<ushort>().Distinct();
                foreach (var intermediateRegionM49Code in intermediateRegionM49Codes)
                {
                    yield return new object[] { $"en-{intermediateRegionM49Code:000}", $"{intermediateRegionM49Code:000}" };
                }
            }
        }
    }

    #endregion

    #region Test: RegionSubtag_WhenReservedPrivateUseCode

    [Theory]
    [MemberData(nameof(RegionSubtag_WhenReservedPrivateUseCode_TestCaseGenerator.TestCases), MemberType = typeof(RegionSubtag_WhenReservedPrivateUseCode_TestCaseGenerator))]
    public void RegionSubtag_WhenReservedPrivateUseCode_ReturnsRegionSubtag(string testLanguageTag, string expectedResult)
    {
        // arrange
        var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

        // act
        var testResult = testSubject.RegionSubtag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is the Region subtag of the language tag {1}", expectedResult, testLanguageTag);
    }

    private class RegionSubtag_WhenReservedPrivateUseCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { "en-AA", "AA" };

                for (var secondCharacterIndex = (byte)'M'; secondCharacterIndex <= (byte)'Z'; secondCharacterIndex++)
                {
                    var regionSubtag = $"Q{(char)secondCharacterIndex}";
                    yield return new object[] { $"en-{regionSubtag}", regionSubtag };
                }

                for (var secondCharacterIndex = (byte)'A'; secondCharacterIndex < (byte)'Z'; secondCharacterIndex++)
                {
                    var regionSubtag = $"X{(char)secondCharacterIndex}";
                    yield return new object[] { $"en-{regionSubtag}", regionSubtag };
                }

                yield return new object[] { "en-ZZ", "ZZ" };
            }
        }
    }

    #endregion

    #region Test: RegionSubtag_WhenNationalUnM49Code

    [Theory]
    [ClassData(typeof(RegionSubtag_WhenNationalUnM49Code_TestCaseFactory))]
    public void RegionSubtag_WhenNationalUnM49Code_ThrowsLanguageTagFormatException(string testLanguageTag, string testCode)
    {
        // act
        Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag);

        // assert
        testAction.Should().Throw<LanguageTagFormatException>("{0} is not a valid Region subtag", testCode).WithMessage("The language tag is not in a valid format.");
    }

    private class RegionSubtag_WhenNationalUnM49Code_TestCaseFactory : IEnumerable<object[]>
    {
        private static readonly IEnumerable<object[]> _testCases;

        static RegionSubtag_WhenNationalUnM49Code_TestCaseFactory()
        {
            _testCases = Enum.GetValues<UnM49AreaByAlpha2CountryCode>().Cast<ushort>().Select(code => new object[] { $"en-{code:000}", code.ToString("000") }).AsEnumerable();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            return _testCases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_testCases).GetEnumerator();
        }
    }

    #endregion

    #region Test: ScriptSubtag_OnDefaultLanguageTag

    [Fact]
    public void ScriptSubtag_OnDefaultLanguageTag_ThrowsInvalidOperationException()
    {
        // arrange
        var testLanguageTag = new Bcp47LanguageTag();

        // act
        Action testAction = () => _ = testLanguageTag.ScriptSubtag;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Test: ScriptSubtag_OnValidLanguageTagWithNoScriptSubtag

    [Fact]
    public void ScriptSubtag_OnValidLanguageTagWithNoScriptSubtag_ReturnsNull()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("en");

        // act
        var testResult = testLanguageTag.ScriptSubtag;

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: ScriptSubtag_WhenIso15924Code

    [Theory]
    [ClassData(typeof(ScriptSubtag_WhenIso15924Code_TestCaseFactory))]
    public void ScriptSubtag_WhenIso15924Code_ReturnsScriptSubtag(string testLanguageTag, string expectedResult)
    {
        // arrange
        var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

        // act
        var testResult = testSubject.ScriptSubtag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is the Script subtag from the language tag {1}", expectedResult, testLanguageTag);
    }

    private class ScriptSubtag_WhenIso15924Code_TestCaseFactory : IEnumerable<object[]>
    {
        private static readonly IEnumerable<object[]> _testCases;

        static ScriptSubtag_WhenIso15924Code_TestCaseFactory()
        {
            _testCases = Enum.GetValues<Iso15924Script>().Select(code => new object[] { $"en-{code}", code.ToString() }).AsEnumerable();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            return _testCases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_testCases).GetEnumerator();
        }
    }

    #endregion

    #region Test: ScriptSubtag_WhenReservedPrivateUseCodes

    [Theory]
    [MemberData(nameof(ScriptSubtag_WhenReservedPrivateUseCodes_TestCaseGenerator.TestCases), MemberType = typeof(ScriptSubtag_WhenReservedPrivateUseCodes_TestCaseGenerator))]
    public void ScriptSubtag_WhenReservedPrivateUseCodes_ReturnsScriptSubtag(string testLanguageTag, string expectedResult)
    {
        // arrange
        var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

        // act
        var testResult = testSubject.ScriptSubtag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is the Script subtag from the language tag {1}", expectedResult, testLanguageTag);
    }

    private class ScriptSubtag_WhenReservedPrivateUseCodes_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                for (var thirdCharacterIndex = (byte)'a'; thirdCharacterIndex <= (byte)'b'; thirdCharacterIndex++)
                for (int fourthCharacterIndex = (byte)'a'; fourthCharacterIndex < (byte)'x'; fourthCharacterIndex++)
                {
                    var scriptSubtag = $"Qa{(char)thirdCharacterIndex}{(char)fourthCharacterIndex}";
                    yield return new object[] { $"en-{scriptSubtag}", scriptSubtag };
                }
            }
        }
    }

    #endregion

    #region Test: VariantSubtags_OnDefaultLanguageTag

    [Fact]
    public void VariantSubtags_OnDefaultLanguageTag_ThrowsInvalidOperationException()
    {
        // arrange
        var testLanguageTag = new Bcp47LanguageTag();

        // act
        Action testAction = () => _ = testLanguageTag.VariantSubtags;

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Test: VariantSubtags_OnValidLanguageTagWithNoVariantSubtags

    [Fact]
    public void VariantSubtags_OnValidLanguageTagWithNoVariantSubtags_ReturnsEmptyCollection()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("en");

        // act
        var testResult = testLanguageTag.VariantSubtags;

        // assert
        testResult.Should().NotBeNull();
        testResult.Should().BeEmpty();
    }

    #endregion

    #region Test: VariantSubtags_OnValidLanguageTagWithVariantSubtags

    [Fact]
    public void VariantSubtags_OnValidLanguageTagWithVariantSubtags_ReturnsVariantSubtags()
    {
        // arrange
        const string variantSubtag = "sursilv";
        var testLanguageTag = Bcp47LanguageTag.Create($"rm-{variantSubtag}");

        // act
        var testResult = testLanguageTag.VariantSubtags;

        // assert
        testResult.Should().BeEquivalentTo(variantSubtag);
    }

    #endregion

    #region Test: ToIso15924_WithLanguageTagHavingNoScriptSubtag

    [Fact]
    public void ToIso15924_WithLanguageTagHavingNoScriptSubtag_ReturnsNull()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("en");

        // act
        var testResult = testLanguageTag.ToIso15924();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: ToIso15924_WithLanguageTagHavingScriptSubtag

    [Fact]
    public void ToIso15924_WithLanguageTagHavingScriptSubtag_ReturnsIso15924Code()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("sr-Cyrl");

        // act
        var testResult = testLanguageTag.ToIso15924();

        // assert
        testResult.Should().Be(Iso15924Script.Cyrl);
    }

    #endregion

    #region Test: ToIso3166Part1Alpha2_WithLanguageTagHavingNoRegionSubtag

    [Fact]
    public void ToIso3166Part1Alpha2_WithLanguageTagHavingNoRegionSubtag_ReturnsNull()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("en");

        // act
        var testResult = testLanguageTag.ToIso3166Part1Alpha2();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: ToIso3166Part1Alpha2_WithLanguageTagHavingRegionSubtag

    [Fact]
    public void ToIso3166Part1Alpha2_WithLanguageTagHavingRegionSubtag_ReturnsIso3166Part1Code()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("yue-Hant-HK");

        // act
        var testResult = testLanguageTag.ToIso3166Part1Alpha2();

        // assert
        testResult.Should().Be(Iso3166Part1Alpha2Country.HK);
    }

    #endregion

    #region Test: ToIso639Part1_WithLanguageTagHavingPrimaryLanguageSubtagNotIso639Part1Code

    [Fact]
    public void ToIso639Part1_WithLanguageTagHavingPrimaryLanguageSubtagNotIso639Part1Code_ReturnsNull()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("zzzzz");

        // act
        var testResult = testLanguageTag.ToIso639Part1();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: ToIso639Part1_WithLanguageTagHavingPrimaryLanguageSubtagIsIso639Part1Code

    [Fact]
    public void ToIso639Part1_WithLanguageTagHavingPrimaryLanguageSubtagIsIso639Part1Code_ReturnsIso639Part1Code()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("es-419");

        // act
        var testResult = testLanguageTag.ToIso639Part1();

        // assert
        testResult.Should().Be(Iso639Part1Language.es);
    }

    #endregion

    #region Test: ToIso639Part2T_WithLanguageTagHavingPrimaryLanguageSubtagNotIso639Part2TCode

    [Fact]
    public void ToIso639Part2T_WithLanguageTagHavingPrimaryLanguageSubtagNotIso639Part2TCode_ReturnsNull()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("zzzzz");

        // act
        var testResult = testLanguageTag.ToIso639Part2T();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: ToIso639Part2T_WithLanguageTagHavingPrimaryLanguageSubtagIsIso639Part2TCode

    [Fact]
    public void ToIso639Part2T_WithLanguageTagHavingPrimaryLanguageSubtagIsIso639Part2TCode_ReturnsIso639Part2TCode()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("hye-u-sd-chzh");

        // act
        var testResult = testLanguageTag.ToIso639Part2T();

        // assert
        testResult.Should().Be(Iso639Part2TLanguage.hye);
    }

    #endregion

    #region Test: ToIso639Part3_WithLanguageTagHavingPrimaryLanguageSubtagNotIso639Part3Code

    [Fact]
    public void ToIso639Part3_WithLanguageTagHavingPrimaryLanguageSubtagNotIso639Part3Code_ReturnsNull()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("zzzzz");

        // act
        var testResult = testLanguageTag.ToIso639Part3();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: ToIso639Part3_WithLanguageTagHavingPrimaryLanguageSubtagIsIso639Part3Code

    [Fact]
    public void ToIso639Part3_WithLanguageTagHavingPrimaryLanguageSubtagIsIso639Part3Code_ReturnsIso639Part3Code()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("nan-Hant-TW");

        // act
        var testResult = testLanguageTag.ToIso639Part3();

        // assert
        testResult.Should().Be(Iso639Part3Language.nan);
    }

    #endregion

    #region Test: ToIso639Part5_WithLanguageTagHavingPrimaryLanguageSubtagNotIso639Part5Code

    [Fact]
    public void ToIso639Part5_WithLanguageTagHavingPrimaryLanguageSubtagNotIso639Part5Code_ReturnsNull()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("zzzzz");

        // act
        var testResult = testLanguageTag.ToIso639Part5();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: ToIso639Part5_WithLanguageTagHavingPrimaryLanguageSubtagIsIso639Part5Code

    [Fact]
    public void ToIso639Part5_WithLanguageTagHavingPrimaryLanguageSubtagIsIso639Part5Code_ReturnsIso639Part5Code()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("sla");

        // act
        var testResult = testLanguageTag.ToIso639Part5();

        // assert
        testResult.Should().Be(Iso639Part5LanguageFamily.sla);
    }

    #endregion

    #region Test: ToUnM49_WithLanguageTagHavingNoRegionSubtag

    [Fact]
    public void ToUnM49_WithLanguageTagHavingNoRegionSubtag_ReturnsNull()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("en");

        // act
        var testResult = testLanguageTag.ToUnM49();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: ToUnM49_WithLanguageTagHavingRegionSubtag

    [Fact]
    public void ToUnM49_WithLanguageTagHavingRegionSubtag_ReturnsUnM49Code()
    {
        // arrange
        var testLanguageTag = Bcp47LanguageTag.Create("es-419");

        // act
        var testResult = testLanguageTag.ToUnM49();

        // assert
        testResult.Should().Be(419);
    }

    #endregion

    #region Test: Create_WithLanguageTagWhenLanguageTagIsNull

    [Fact]
    public void Create_WithLanguageTagWhenLanguageTagIsNull_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => Bcp47LanguageTag.Create(null!);

        // assert
        testAction.Should().Throw<ArgumentNullException>("the language tag must not be null").WithParameterName("languageTag");
    }

    #endregion

    #region Test: Create_WithLanguageTagWhenLanguageTagIsInvalid

    [Theory]
    [InlineData(""), InlineData("NotAValidLanguageTag")]
    public void Create_WithLanguageTagWhenLanguageTagIsInvalid_ThrowsLanguageTagFormatException(string testLanguageTag)
    {
        // act
        Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag);

        // assert
        testAction.Should().Throw<LanguageTagFormatException>("the language tag was not in a recognised format").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
    }

    #endregion

    #region Test: Create_WithLanguageTagWhenLanguageTagIsValid

    [Theory]
    [MemberData(nameof(Create_WithLanguageTagWhenLanguageTagIsValid_TestCaseGenerator.TestCases), MemberType = typeof(Create_WithLanguageTagWhenLanguageTagIsValid_TestCaseGenerator))]
    public void Create_WithLanguageTagWhenLanguageTagIsValid_ReturnsBcp47LanguageTag(string testLanguageTag, string expectedResult)
    {
        // act
        var languageTag = Bcp47LanguageTag.Create(testLanguageTag);
        var testResult = (string?)languageTag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is a correctly formatted language tag", testLanguageTag);
    }

    private class Create_WithLanguageTagWhenLanguageTagIsValid_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var validLanguageTag in _validLanguageTags)
                {
                    yield return new object[] { validLanguageTag, validLanguageTag };
                }

                yield return new object[] { "x-fr-CH", "x-fr-CH" }; // should be interpreted as a Private Use-only language tag
                yield return new object[] { "x-fr-CH-x-valid-private-use-subtag", "x-fr-CH-x-valid-private-use-subtag" };
            }
        }
    }

    #endregion

    #region Test: Create_WithLanguageTagAndTimeoutWhenLanguageTagIsNull

    [Fact]
    public void Create_WithLanguageTagAndTimeoutWhenLanguageTagIsNull_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => _ = Bcp47LanguageTag.Create(null!, Regex.InfiniteMatchTimeout);

        // assert
        testAction.Should().Throw<ArgumentNullException>("the language tag must not be null").WithParameterName("languageTag");
    }

    #endregion

    #region Test: Create_WithLanguageTagAndTimeoutWhenLanguageTagIsInvalid

    [Theory]
    [InlineData(""), InlineData("NotAValidLanguageTag")]
    public void Create_WithLanguageTagAndTimeoutWhenLanguageTagIsInvalid_ThrowsLanguageTagFormatException(string testLanguageTag)
    {
        // act
        Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, Regex.InfiniteMatchTimeout);

        // assert
        testAction.Should().Throw<LanguageTagFormatException>("the language tag was not in a recognised format").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
    }

    #endregion

    #region Test: Create_WithLanguageTagAndTimeoutWhenLanguageTagIsValid

    [Theory]
    [MemberData(nameof(Create_WithLanguageTagAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator.TestCases), MemberType = typeof(Create_WithLanguageTagAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator))]
    public void Create_WithLanguageTagAndTimeoutWhenLanguageTagIsValid_ReturnsBcp47LanguageTag(string testLanguageTag, TimeSpan testTimeout, string expectedResult)
    {
        // act
        var languageTag = Bcp47LanguageTag.Create(testLanguageTag, testTimeout);
        var testResult = (string?)languageTag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is a correctly formatted language tag", testLanguageTag);
    }

    private class Create_WithLanguageTagAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var validLanguageTag in _validLanguageTags)
                {
                    yield return new object[] { validLanguageTag, Regex.InfiniteMatchTimeout, validLanguageTag };
                }

                yield return new object[] { "x-fr-CH", Regex.InfiniteMatchTimeout, "x-fr-CH" };
                yield return new object[] { "x-fr-CH-x-valid-private-use-subtag",Regex.InfiniteMatchTimeout, "x-fr-CH-x-valid-private-use-subtag" };
            }
        }
    }

    #endregion

    #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsNull

    [Fact]
    public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsNull_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => _ = Bcp47LanguageTag.Create(null!, _fixture.IanaSubtagRegistry);

        // assert
        testAction.Should().Throw<ArgumentNullException>("the language tag must not be null").WithParameterName("languageTag");
    }

    #endregion

    #region Test: Create_WithLanguageTagAndSubtagRegistryWhenSubtagRegistryIsNull

    [Fact]
    public void Create_WithLanguageTagAndSubtagRegistryWhenSubtagRegistryIsNull_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => _ = Bcp47LanguageTag.Create("en", null!);

        // assert
        testAction.Should().Throw<ArgumentNullException>("the subtag registry must not be null").WithParameterName("subtagRegistry");
    }

    #endregion

    #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsInvalid

    [Theory]
    [InlineData(""), InlineData("invalid")]
    public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsInvalid_ThrowsLanguageTagFormatException(string testLanguageTag)
    {
        // act
        Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry);

        // assert
        testAction.Should().Throw<LanguageTagFormatException>("the language tag was not in a recognised format").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
    }

    #endregion

    #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid

    [Theory]
    [MemberData(nameof(Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_TestCaseGenerator.TestCases), MemberType = typeof(Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_TestCaseGenerator))]
    public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_ReturnsBcp47LanguageTag(string testLanguageTag, string expectedResult)
    {
        // act
        var languageTag = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry);
        var testResult = (string?)languageTag;

        // assert
        testResult.Should().Be(expectedResult, "the language tag '{0}' is correctly formatted", testLanguageTag);
    }

    private class Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var validLanguageTag in _validLanguageTags)
                {
                    yield return new object[] { validLanguageTag, validLanguageTag };
                }

                yield return new object[] { "x-fr-CH", "x-fr-CH" };
                yield return new object[] { "x-fr-CH-x-valid-private-use-subtag", "x-fr-CH-x-valid-private-use-subtag" };
            }
        }
    }

    #endregion

    #region Test: Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsNull

    [Fact]
    public void Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsNull_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => _ = Bcp47LanguageTag.Create(null!, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout);

        // assert
        testAction.Should().Throw<ArgumentNullException>("the language tag must not be null").WithParameterName("languageTag");
    }

    #endregion

    #region Test: Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagRegistryIsNull

    [Fact]
    public void Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagRegistryIsNull_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => _ = Bcp47LanguageTag.Create("en", null!, Regex.InfiniteMatchTimeout);

        // assert
        testAction.Should().Throw<ArgumentNullException>("the subtag registry must not be null").WithParameterName("subtagRegistry");
    }

    #endregion

    #region Test: Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsInvalid

    [Theory]
    [InlineData(""), InlineData("invalid")]
    public void Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsInvalid_ThrowsLanguageTagFormatException(string testLanguageTag)
    {
        // act
        Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout);

        // assert
        testAction.Should().Throw<LanguageTagFormatException>("the language tag was not in a recognised format").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
    }

    #endregion

    #region Test: Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid

    [Theory]
    [MemberData(nameof(Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator.TestCases), MemberType = typeof(Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator))]
    public void Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid_ReturnsBcp47LanguageTag(string testLanguageTag, TimeSpan testTimeout, string expectedResult)
    {
        // arrange
        var languageTag = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry, testTimeout);
        var testResult = (string?)languageTag;

        // assert
        testResult.Should().Be(expectedResult, "'{0}' is a correctly formatted language tag", testLanguageTag);
    }

    private class Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var validLanguageTag in _validLanguageTags)
                {
                    yield return new object[] { validLanguageTag, Regex.InfiniteMatchTimeout, validLanguageTag };
                }

                yield return new object[] { "x-fr-CH", Regex.InfiniteMatchTimeout, "x-fr-CH" };
                yield return new object[] { "x-fr-CH-x-valid-private-use-subtag",Regex.InfiniteMatchTimeout, "x-fr-CH-x-valid-private-use-subtag" };
            }
        }
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagWhenLanguageTagIsNull

    [Fact]
    public void TryCreate_WithLanguageTagWhenLanguageTagIsNull_ReturnsFalse()
    {
        // act
        var testResult = Bcp47LanguageTag.TryCreate(null, out _);

        // assert
        testResult.Should().BeFalse("the language tag must not be null");
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagWhenLanguageTagIsInvalid

    [Theory]
    [InlineData(""), InlineData("NotAValidLanguageTag")]
    public void TryCreate_WithLanguageTagWhenLanguageTagIsInvalid_ReturnsFalse(string testLanguageTag)
    {
        // act
        var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, out _);

        // assert
        testResult.Should().BeFalse("the language tag '{0}' is not correctly formatted", testLanguageTag);
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagWhenLanguageTagIsValid

    [Theory]
    [MemberData(nameof(TryCreate_WithLanguageTagWhenLanguageTagIsValid_TestCaseGenerator.TestCases), MemberType = typeof(TryCreate_WithLanguageTagWhenLanguageTagIsValid_TestCaseGenerator))]
    public void TryCreate_WithLanguageTagWhenLanguageTagIsValid_ReturnsTrue(string testLanguageTag, string expectedResult)
    {
        // arrange
        Bcp47LanguageTag languageTag;

        // act
        var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, out languageTag);

        // assert
        testResult.Should().BeTrue("language tag '{0}' is correctly formatted", (string?)languageTag);
        ((string?)languageTag).Should().Be(expectedResult, "language tag '{0}' is correctly formatted", (string?)languageTag);
    }

    private class TryCreate_WithLanguageTagWhenLanguageTagIsValid_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var validLanguageTag in _validLanguageTags)
                {
                    yield return new object[] { validLanguageTag, validLanguageTag };
                }

                yield return new object[] { "x-fr-CH", "x-fr-CH" };
                yield return new object[] { "x-fr-CH-x-valid-private-use-subtag", "x-fr-CH-x-valid-private-use-subtag" };
            }
        }
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagIsNull

    [Fact]
    public void TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagIsNull_ReturnsFalse()
    {
        // act
        var testResult = Bcp47LanguageTag.TryCreate(null, Regex.InfiniteMatchTimeout, out _);

        // assert
        testResult.Should().BeFalse("the language tag must not be null");
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagIsInvalid

    [Theory]
    [InlineData(""), InlineData("NotAValidLanguageTag")]
    public void TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagIsInvalid_ReturnsFalse(string testLanguageTag)
    {
        // act
        var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, Regex.InfiniteMatchTimeout, out _);

        // assert
        testResult.Should().BeFalse("language tag '{0}' is not correctly formatted", testLanguageTag);
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagIsValid

    [Theory]
    [MemberData(nameof(TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator.TestCases), MemberType = typeof(TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator))]
    public void TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagIsValid_ReturnsTrue(string testLanguageTag, TimeSpan testTimeout, string expectedResult)
    {
        // arrange
        Bcp47LanguageTag languageTag;

        // act
        var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, testTimeout, out languageTag);

        // assert
        testResult.Should().BeTrue("the language tag '{0}' is correctly formatted", testLanguageTag);
        ((string?)languageTag).Should().Be(expectedResult, "the language tag '{0}' is correctly formatted", testLanguageTag);
    }

    private class TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var validLanguageTag in _validLanguageTags)
                {
                    yield return new object[] { validLanguageTag, Regex.InfiniteMatchTimeout, validLanguageTag };
                }

                yield return new object[] { "x-fr-CH", Regex.InfiniteMatchTimeout, "x-fr-CH" };
                yield return new object[] { "x-fr-CH-x-valid-private-use-subtag",Regex.InfiniteMatchTimeout, "x-fr-CH-x-valid-private-use-subtag" };
            }
        }
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsNull

    [Fact]
    public void TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsNull_ReturnsFalse()
    {
        // act
        var testResult = Bcp47LanguageTag.TryCreate(null, _fixture.IanaSubtagRegistry, out _);

        // assert
        testResult.Should().BeFalse("the language tag must not be null");
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenSubtagRegistryIsNull

    [Fact]
    public void TryCreate_WithLanguageTagAndSubtagRegistryWhenSubtagRegistryIsNull_ReturnsFalse()
    {
        // act
        var testResult = Bcp47LanguageTag.TryCreate("en", null!, out _);

        // assert
        testResult.Should().BeFalse("the subtag registry must not be null");
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsInvalid

    [Theory]
    [InlineData(""), InlineData("invalid")]
    public void TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsInvalid_ReturnsFalse(string testLanguageTag)
    {
        // act
        var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, out _);

        // assert
        testResult.Should().BeFalse("the language tag '{0}' is not correctly formatted", testLanguageTag);
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid

    [Theory]
    [MemberData(nameof(TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_TestCaseGenerator.TestCases), MemberType = typeof(TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_TestCaseGenerator))]
    public void TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_ReturnsTrue(string testLanguageTag, string expectedResult)
    {
        // arrange
        Bcp47LanguageTag languageTag;

        // act
        var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, out languageTag);

        // assert
        testResult.Should().BeTrue("the language tag '{0}' is correctly formatted", testLanguageTag);
        ((string?)languageTag).Should().Be(expectedResult, "the language tag '{0}' is correctly formatted", testLanguageTag);
    }

    private class TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var validLanguageTag in _validLanguageTags)
                {
                    yield return new object[] { validLanguageTag, validLanguageTag };
                }

                yield return new object[] { "x-fr-CH", "x-fr-CH" };
                yield return new object[] { "x-fr-CH-x-valid-private-use-subtag", "x-fr-CH-x-valid-private-use-subtag" };
            }
        }
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsNull

    [Fact]
    public void TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsNull_ReturnsFalse()
    {
        // act
        var testResult = Bcp47LanguageTag.TryCreate(null, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout, out _);

        // assert
        testResult.Should().BeFalse("the language tag must not be null");
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagRegistryIsNull

    [Fact]
    public void TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagRegistryIsNull_ReturnsFalse()
    {
        // act
        var testResult = Bcp47LanguageTag.TryCreate("en", null!, Regex.InfiniteMatchTimeout, out _);

        // assert
        testResult.Should().BeFalse("the subtag registry must not be null");
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsInvalid

    [Theory]
    [InlineData(""), InlineData("invalid")]
    public void TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsInvalid_ReturnsFalse(string testLanguageTag)
    {
        // act
        var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout, out _);

        // assert
        testResult.Should().BeFalse("the language tag '{0}' is not correctly formatted", testLanguageTag);
    }

    #endregion

    #region Test: TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid

    [Theory]
    [MemberData(nameof(TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator.TestCases), MemberType = typeof(TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator))]
    public void TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid_ReturnsTrue(string testLanguageTag, TimeSpan testTimeout, string expectedResult)
    {
        // arrange
        Bcp47LanguageTag languageTag;

        // act
        var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, testTimeout, out languageTag);

        // assert
        testResult.Should().BeTrue("the language tag '{0}' is correctly formatted", testLanguageTag);
        ((string?)languageTag).Should().Be(expectedResult, "the language tag '{0}' is correctly formatted", testLanguageTag);
    }

    private class TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var validLanguageTag in _validLanguageTags)
                {
                    yield return new object[] { validLanguageTag, Regex.InfiniteMatchTimeout, validLanguageTag };
                }

                yield return new object[] { "x-fr-CH", Regex.InfiniteMatchTimeout, "x-fr-CH" };
                yield return new object[] { "x-fr-CH-x-valid-private-use-subtag",Regex.InfiniteMatchTimeout, "x-fr-CH-x-valid-private-use-subtag" };
            }
        }
    }

    #endregion
}