using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    /// <summary>
    /// Tests of the span and generic-parsing members of <see cref="Bcp47LanguageTag"/> (#229).
    /// </summary>
    /// <remarks>
    /// The <c>net48</c> run tests the package's <c>netstandard2.0</c> build, which has the span members but not the .NET 6 and .NET 7
    /// interfaces; the <c>net8.0</c> and <c>net10.0</c> runs test everything.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Bcp47LanguageTagSpanTests : IClassFixture<Bcp47LanguageTagFixture>
    {
        #region Declarations

        private delegate bool SpanCheck(ReadOnlySpan<char> subtag);

        /// <summary>
        /// Characters around a span taken from the middle of a string. A check that read outside its span would see a longer subtag.
        /// </summary>
        private const string Padding = "zz";

        private static readonly Dictionary<string, Func<string, bool>> StringChecks = new Dictionary<string, Func<string, bool>>
        {
            { nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), Bcp47LanguageTag.CheckExtendedLanguageSubtag },
            { nameof(Bcp47LanguageTag.CheckExtensionSubtag), Bcp47LanguageTag.CheckExtensionSubtag },
            { nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), Bcp47LanguageTag.CheckPrimaryLanguageSubtag },
            { nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), Bcp47LanguageTag.CheckPrivateUseSubtag },
            { nameof(Bcp47LanguageTag.CheckRegionSubtag), Bcp47LanguageTag.CheckRegionSubtag },
            { nameof(Bcp47LanguageTag.CheckScriptSubtag), Bcp47LanguageTag.CheckScriptSubtag },
            { nameof(Bcp47LanguageTag.CheckVariantSubtag), Bcp47LanguageTag.CheckVariantSubtag }
        };

        private static readonly Dictionary<string, SpanCheck> SpanChecks = new Dictionary<string, SpanCheck>
        {
            { nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), Bcp47LanguageTag.CheckExtendedLanguageSubtag },
            { nameof(Bcp47LanguageTag.CheckExtensionSubtag), Bcp47LanguageTag.CheckExtensionSubtag },
            { nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), Bcp47LanguageTag.CheckPrimaryLanguageSubtag },
            { nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), Bcp47LanguageTag.CheckPrivateUseSubtag },
            { nameof(Bcp47LanguageTag.CheckRegionSubtag), Bcp47LanguageTag.CheckRegionSubtag },
            { nameof(Bcp47LanguageTag.CheckScriptSubtag), Bcp47LanguageTag.CheckScriptSubtag },
            { nameof(Bcp47LanguageTag.CheckVariantSubtag), Bcp47LanguageTag.CheckVariantSubtag }
        };

        /// <summary>
        /// Subtags that between them are valid and invalid for every check.
        /// </summary>
        private static readonly string[] Subtags =
        {
            "", "-", "a", "x", "en", "EN", "zh", "qaa", "yue", "YUE", "abc", "Hant", "hant", "Zzzz", "TW", "tw", "419", "999", "AA", "CQ",
            "1994", "rozaj", "1abc", "abcdefghi", "u-ca-hebrew", "u-ca-hebrew-tz-jeruslm", "u-a", "u", "x-private", "X-a-b", "x-", "x-abcdefghi",
            "u-ca-x-a", "en\n", "İ"
        };

        /// <summary>
        /// Strings that are well-formed language tags.
        /// </summary>
        private static readonly string[] ValidLanguageTagStrings =
        {
            "en", "es-419", "zh-Hant-TW", "zh-yue-HK", "sr-Latn-RS", "de-CH-1901", "sl-rozaj-biske-1994", "hy-Latn-IT-arevela", "gsw-u-sd-chzh",
            "he-IL-u-ca-hebrew-tz-jeruslm", "en-US-x-twain", "x-private", "i-klingon", "zh-min-nan", "EN-us"
        };

        /// <summary>
        /// Strings that are not well-formed language tags, including two that only repeat a subtag.
        /// </summary>
        private static readonly string[] InvalidLanguageTagStrings =
        {
            "", "e", "en-", "-en", "en--US", "abcdefghi", "en-US-US", "zh-Hant-Hans", "de-CH-123456789", "en-US-a", "en-x-", "x-abcdefghi",
            "de-1901-1901", "en-a-bbb-a-ccc"
        };

        public static TheoryData<string> ValidLanguageTags => new TheoryData<string>(ValidLanguageTagStrings);

        public static TheoryData<string> InvalidLanguageTags => new TheoryData<string>(InvalidLanguageTagStrings);

        public static TheoryData<string, string> CheckCases
        {
            get
            {
                var data = new TheoryData<string, string>();
                foreach (var checkName in StringChecks.Keys)
                {
                    foreach (var subtag in Subtags)
                    {
                        data.Add(checkName, subtag);
                    }
                }

                return data;
            }
        }

        private readonly Bcp47LanguageTagFixture _fixture;

        #endregion

        public Bcp47LanguageTagSpanTests(Bcp47LanguageTagFixture fixture)
        {
            _fixture = fixture;
        }

        private static ReadOnlySpan<char> PaddedSpan(string value)
        {
            return (Padding + value + Padding).AsSpan(Padding.Length, value.Length);
        }

        #region Test: Check*(ReadOnlySpan<char>)

        [Theory]
        [MemberData(nameof(CheckCases))]
        public void CheckSubtag_WithSpan_AgreesWithString(string checkName, string testSubtag)
        {
            // arrange
            var expectedResult = StringChecks[checkName](testSubtag);

            // act
            var result = SpanChecks[checkName](testSubtag.AsSpan());
            var paddedResult = SpanChecks[checkName](PaddedSpan(testSubtag));

            // assert
            result.Should().Be(expectedResult, "{0} must give the same result for a span as for a string", checkName);
            paddedResult.Should().Be(expectedResult, "{0} must read only the characters in the span", checkName);
        }

        [Fact]
        public void CheckSubtag_WithSpanCases_CoverValidAndInvalidForEveryCheck()
        {
            // assert
            foreach (var check in StringChecks)
            {
                Subtags.Should().Contain(subtag => check.Value(subtag), "the cases must include a subtag that {0} accepts", check.Key);
                Subtags.Should().Contain(subtag => !check.Value(subtag), "the cases must include a subtag that {0} rejects", check.Key);
            }
        }

        [Fact]
        public void CheckSubtag_WithDefaultSpan_ReturnsFalse()
        {
            // assert
            foreach (var check in SpanChecks)
            {
                check.Value(default).Should().BeFalse("{0} must reject an empty span rather than throw", check.Key);
            }
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString(ReadOnlySpan<char>)

        [Theory]
        [MemberData(nameof(ValidLanguageTags))]
        public void IsWellFormedLanguageTagString_WithValidSpan_ReturnsTrue(string testLanguageTag)
        {
            // act
            var result = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag.AsSpan());
            var paddedResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(PaddedSpan(testLanguageTag));

            // assert
            result.Should().BeTrue();
            paddedResult.Should().BeTrue("only the characters in the span are read");
        }

        [Theory]
        [MemberData(nameof(InvalidLanguageTags))]
        public void IsWellFormedLanguageTagString_WithInvalidSpan_ReturnsFalse(string testLanguageTag)
        {
            // act
            var result = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag.AsSpan());

            // assert
            result.Should().BeFalse();
            Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag).Should().BeFalse("the string overload must agree");
        }

        #endregion

        #region Test: TryCreate(ReadOnlySpan<char>, ...)

        [Theory]
        [MemberData(nameof(ValidLanguageTags))]
        public void TryCreate_WithValidSpan_ReturnsSameTagAsString(string testLanguageTag)
        {
            // arrange
            var expectedResult = Bcp47LanguageTag.Create(testLanguageTag);

            // act
            var success = Bcp47LanguageTag.TryCreate(PaddedSpan(testLanguageTag), out var result);

            // assert
            success.Should().BeTrue();
            ((string)result).Should().Be(testLanguageTag, "the stored string is a copy of the span, with its case kept");
            result.PrimaryLanguageSubtag.Should().Be(expectedResult.PrimaryLanguageSubtag);
            result.ExtendedLanguageSubtags.Should().Equal(expectedResult.ExtendedLanguageSubtags);
            result.ScriptSubtag.Should().Be(expectedResult.ScriptSubtag);
            result.RegionSubtag.Should().Be(expectedResult.RegionSubtag);
            result.VariantSubtags.Should().Equal(expectedResult.VariantSubtags);
            result.ExtensionSubtags.Should().Equal(expectedResult.ExtensionSubtags);
            result.PrivateUseSubtag.Should().Be(expectedResult.PrivateUseSubtag);
        }

        [Theory]
        [MemberData(nameof(InvalidLanguageTags))]
        public void TryCreate_WithInvalidSpan_ReturnsFalseAndDefault(string testLanguageTag)
        {
            // act
            var success = Bcp47LanguageTag.TryCreate(testLanguageTag.AsSpan(), out var result);

            // assert
            success.Should().BeFalse();
            result.Should().Be(default(Bcp47LanguageTag));
        }

        [Theory]
        [InlineData("en-GB")]
        [InlineData("zh-yue-HK")]
        [InlineData("sl-rozaj-biske-1994")]
        [InlineData("i-klingon")]
        public void TryCreate_WithRegistryAndValidSpan_ReturnsSameTagAsString(string testLanguageTag)
        {
            // arrange
            var expectedResult = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry);

            // act
            var success = Bcp47LanguageTag.TryCreate(PaddedSpan(testLanguageTag), _fixture.IanaSubtagRegistry, out var result);

            // assert
            success.Should().BeTrue();
            ((string)result).Should().Be(testLanguageTag);
            result.PrimaryLanguageSubtag.Should().Be(expectedResult.PrimaryLanguageSubtag);
            result.ExtendedLanguageSubtags.Should().Equal(expectedResult.ExtendedLanguageSubtags);
            result.RegionSubtag.Should().Be(expectedResult.RegionSubtag);
            result.VariantSubtags.Should().Equal(expectedResult.VariantSubtags);
        }

        [Theory]
        [InlineData("xyz")]
        [InlineData("en-US-US")]
        [InlineData("de-1901-1901")]
        [InlineData("")]
        public void TryCreate_WithRegistryAndInvalidSpan_ReturnsFalse(string testLanguageTag)
        {
            // arrange
            Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, out _).Should().BeFalse("the case must be one the string overload rejects");

            // act
            var success = Bcp47LanguageTag.TryCreate(testLanguageTag.AsSpan(), _fixture.IanaSubtagRegistry, out var result);

            // assert
            success.Should().BeFalse();
            result.Should().Be(default(Bcp47LanguageTag));
        }

        [Fact]
        public void TryCreate_WithSpanAndNullRegistry_ReturnsFalse()
        {
            // act
            var success = Bcp47LanguageTag.TryCreate("en".AsSpan(), (SubtagRegistry.SubtagRegistry)null, out var result);

            // assert
            success.Should().BeFalse("the string overload returns false for a null registry too");
            result.Should().Be(default(Bcp47LanguageTag));
        }

        #endregion

        #region Test: AsSpan

        [Fact]
        public void AsSpan_OnValidLanguageTag_ReturnsStoredStringWithoutCopying()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create("zh-Hant-TW");
            var expectedResult = ((string)testLanguageTag).AsSpan();

            // act
            var result = testLanguageTag.AsSpan();

            // assert
            (result == expectedResult).Should().BeTrue("the span must refer to the stored string itself");
            result.ToString().Should().Be("zh-Hant-TW");
        }

        [Fact]
        public void AsSpan_OnDefaultLanguageTag_ReturnsEmptySpan()
        {
            // act
            var result = default(Bcp47LanguageTag).AsSpan();

            // assert
            result.IsEmpty.Should().BeTrue("an uninitialised tag converts to a null string, which has no characters");
        }

        #endregion

#if NET6_0_OR_GREATER
        #region Test: TryFormat

        [Fact]
        public void TryFormat_WithLongEnoughDestination_CopiesLanguageTag()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create("sr-Latn-RS");
            Span<char> destination = stackalloc char[16];

            // act
            var success = testLanguageTag.TryFormat(destination, out var charsWritten, "G", System.Globalization.CultureInfo.InvariantCulture);

            // assert
            success.Should().BeTrue();
            charsWritten.Should().Be(10);
            destination.Slice(0, charsWritten).ToString().Should().Be("sr-Latn-RS");
        }

        [Fact]
        public void TryFormat_WithExactLengthDestination_CopiesLanguageTag()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create("sr-Latn-RS");
            var destination = new char[10];

            // act
            var success = testLanguageTag.TryFormat(destination, out var charsWritten, default, null);

            // assert
            success.Should().BeTrue();
            charsWritten.Should().Be(10);
            new string(destination).Should().Be("sr-Latn-RS");
        }

        [Fact]
        public void TryFormat_WithShortDestination_ReturnsFalseAndWritesNothing()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create("sr-Latn-RS");
            var destination = new char[9];

            // act
            var success = testLanguageTag.TryFormat(destination, out var charsWritten, default, null);

            // assert
            success.Should().BeFalse();
            charsWritten.Should().Be(0);
            destination.Should().OnlyContain(character => character == '\0');
        }

        [Fact]
        public void TryFormat_OnDefaultLanguageTag_WritesToString()
        {
            // arrange
            var testLanguageTag = default(Bcp47LanguageTag);
            var destination = new char[256];

            // act
            var success = testLanguageTag.TryFormat(destination, out var charsWritten, default, null);

            // assert
            success.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be(testLanguageTag.ToString());
        }

        [Fact]
        public void Format_InInterpolatedStringWithFormat_WritesLanguageTag()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create("de-CH-1901");

            // act
            var result = $"[{testLanguageTag:X}] [{testLanguageTag,12}]";
            var formattableResult = ((IFormattable)testLanguageTag).ToString("X", System.Globalization.CultureInfo.InvariantCulture);

            // assert
            result.Should().Be("[de-CH-1901] [  de-CH-1901]", "the format is ignored and alignment still applies");
            formattableResult.Should().Be("de-CH-1901");
        }

        #endregion
#endif

#if NET7_0_OR_GREATER
        #region Test: IParsable / ISpanParsable

        private static T ParseGeneric<T>(string s) where T : IParsable<T>
        {
            return T.Parse(s, null);
        }

        private static bool TryParseGeneric<T>(string s, out T result) where T : IParsable<T>
        {
            return T.TryParse(s, null, out result);
        }

        private static T ParseSpanGeneric<T>(ReadOnlySpan<char> s) where T : ISpanParsable<T>
        {
            return T.Parse(s, null);
        }

        private static bool TryParseSpanGeneric<T>(ReadOnlySpan<char> s, out T result) where T : ISpanParsable<T>
        {
            return T.TryParse(s, null, out result);
        }

        [Theory]
        [MemberData(nameof(ValidLanguageTags))]
        public void Parse_WithValidLanguageTag_ReturnsLanguageTag(string testLanguageTag)
        {
            // arrange
            var expectedResult = Bcp47LanguageTag.Create(testLanguageTag);

            // act
            var result = Bcp47LanguageTag.Parse(testLanguageTag, System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));
            var genericResult = ParseGeneric<Bcp47LanguageTag>(testLanguageTag);
            var spanResult = Bcp47LanguageTag.Parse(PaddedSpan(testLanguageTag), null);
            var genericSpanResult = ParseSpanGeneric<Bcp47LanguageTag>(PaddedSpan(testLanguageTag));

            // assert
            result.Should().Be(expectedResult);
            genericResult.Should().Be(expectedResult);
            spanResult.Should().Be(expectedResult);
            genericSpanResult.Should().Be(expectedResult);
            ((string)spanResult).Should().Be(testLanguageTag);
        }

        [Theory]
        [MemberData(nameof(InvalidLanguageTags))]
        public void Parse_WithInvalidLanguageTag_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action stringAction = () => Bcp47LanguageTag.Parse(testLanguageTag, null);
            Action spanAction = () => Bcp47LanguageTag.Parse(testLanguageTag.AsSpan(), null);

            // assert
            stringAction.Should().Throw<LanguageTagFormatException>().Which.LanguageTag.Should().Be(testLanguageTag);
            spanAction.Should().Throw<LanguageTagFormatException>().Which.LanguageTag.Should().Be(testLanguageTag);
        }

        [Fact]
        public void Parse_WithNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Parse((string)null, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("s");
        }

        [Theory]
        [MemberData(nameof(ValidLanguageTags))]
        public void TryParse_WithValidLanguageTag_ReturnsTrue(string testLanguageTag)
        {
            // arrange
            var expectedResult = Bcp47LanguageTag.Create(testLanguageTag);

            // act
            var success = Bcp47LanguageTag.TryParse(testLanguageTag, null, out var result);
            var genericSuccess = TryParseGeneric<Bcp47LanguageTag>(testLanguageTag, out var genericResult);
            var spanSuccess = Bcp47LanguageTag.TryParse(PaddedSpan(testLanguageTag), null, out var spanResult);
            var genericSpanSuccess = TryParseSpanGeneric<Bcp47LanguageTag>(PaddedSpan(testLanguageTag), out var genericSpanResult);

            // assert
            new[] { success, genericSuccess, spanSuccess, genericSpanSuccess }.Should().OnlyContain(value => value);
            new[] { result, genericResult, spanResult, genericSpanResult }.Should().OnlyContain(value => value == expectedResult);
        }

        [Theory]
        [MemberData(nameof(InvalidLanguageTags))]
        [InlineData(null)]
        public void TryParse_WithInvalidLanguageTag_ReturnsFalse(string testLanguageTag)
        {
            // act
            var success = Bcp47LanguageTag.TryParse(testLanguageTag, null, out var result);
            var spanSuccess = Bcp47LanguageTag.TryParse(testLanguageTag.AsSpan(), null, out var spanResult);

            // assert
            success.Should().BeFalse();
            spanSuccess.Should().BeFalse();
            result.Should().Be(default(Bcp47LanguageTag));
            spanResult.Should().Be(default(Bcp47LanguageTag));
        }

        #endregion
#endif

#if NETCOREAPP3_0_OR_GREATER
        #region Test: allocation

        [Fact]
        public void SpanValidation_WithWarmCaches_DoesNotAllocate()
        {
            // arrange
            var validTags = ValidLanguageTagStrings;
            var invalidTags = InvalidLanguageTagStrings;
            var subtags = Subtags;

            // Run once first, so that the one-off work of building the code lists and compiling the methods is not counted.
            var count = Validate();

            // act
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            count += Validate();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            // assert
            count.Should().BeGreaterThan(0);
            allocated.Should().Be(0, "validating a span and failing to create a tag from one must not allocate");

            int Validate()
            {
                var result = 0;
                foreach (var tag in validTags)
                {
                    if (Bcp47LanguageTag.IsWellFormedLanguageTagString(tag.AsSpan())) result++;
                }

                foreach (var tag in invalidTags)
                {
                    if (!Bcp47LanguageTag.TryCreate(tag.AsSpan(), out _)) result++;
                }

                foreach (var subtag in subtags)
                {
                    var span = subtag.AsSpan();
                    if (Bcp47LanguageTag.CheckExtendedLanguageSubtag(span)) result++;
                    if (Bcp47LanguageTag.CheckExtensionSubtag(span)) result++;
                    if (Bcp47LanguageTag.CheckPrimaryLanguageSubtag(span)) result++;
                    if (Bcp47LanguageTag.CheckPrivateUseSubtag(span)) result++;
                    if (Bcp47LanguageTag.CheckRegionSubtag(span)) result++;
                    if (Bcp47LanguageTag.CheckScriptSubtag(span)) result++;
                    if (Bcp47LanguageTag.CheckVariantSubtag(span)) result++;
                }

                return result;
            }
        }

        #endregion
#endif
    }
}
