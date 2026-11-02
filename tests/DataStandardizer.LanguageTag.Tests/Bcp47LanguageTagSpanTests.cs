using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    public class Bcp47LanguageTagSpanTests
    {
        #region Declarations

        public delegate bool SpanCheck(ReadOnlySpan<char> subtag);

        public delegate (bool Success, string Result) SpanTryParse(SpanInput input);

        /// <summary>
        /// Serbian written in Latin script as used in Serbia, an example from RFC 5646 Appendix A.
        /// </summary>
        private const string SerbianLatinSerbia = "sr-Latn-RS";

        /// <summary>
        /// The number of characters in <see cref="SerbianLatinSerbia"/>.
        /// </summary>
        private const int SerbianLatinSerbiaLength = 10;

        /// <summary>
        /// One character too few for <see cref="SerbianLatinSerbia"/>.
        /// </summary>
        private const int ShortDestinationLength = 9;

        /// <summary>
        /// Swiss German in the orthography of 1901, an example from RFC 5646 Appendix A.
        /// </summary>
        private const string SwissGerman1901 = "de-CH-1901";

        /// <summary>
        /// Chinese written in traditional script as used in Taiwan (RFC 5646 §2.2.3, §2.2.4).
        /// </summary>
        private const string TraditionalChineseTaiwan = "zh-Hant-TW";

        /// <summary>
        /// Cantonese, as an Extended Language subtag of Chinese, as used in Hong Kong, an example from RFC 5646 Appendix A.
        /// </summary>
        private const string CantoneseHongKong = "zh-yue-HK";

        /// <summary>
        /// Resian dialect of Slovenian in the San Giorgio / Bila spelling of 1994, from the RFC 5646 Appendix A examples sl-rozaj-biske and the 1994 variant (§2.2.5).
        /// </summary>
        private const string ResianSanGiorgio1994 = "sl-rozaj-biske-1994";

        /// <summary>
        /// The irregular grandfathered tag for Klingon (RFC 5646 §2.1, §2.2.8).
        /// </summary>
        private const string KlingonGrandfathered = "i-klingon";

        /// <summary>
        /// A tag that is only a Private Use subtag (RFC 5646 §2.2.7).
        /// </summary>
        private const string PrivateUseOnly = "x-private";

        /// <summary>
        /// A variant subtag repeated, which a well-formed tag may do and a valid one may not (RFC 5646 §2.2.5¶5, §2.2.9).
        /// </summary>
        private const string RepeatedVariant = "de-1901-1901";

        /// <summary>
        /// A Region subtag repeated, which a tag can't have (RFC 5646 §2.1).
        /// </summary>
        private const string RepeatedRegion = "en-US-US";

        /// <summary>
        /// What <see cref="Bcp47LanguageTag.ToString"/> returns for an uninitialised tag: the name of the type, as for any struct.
        /// </summary>
        private const string UninitialisedToString = "DataStandardizer.LanguageTag.Bcp47LanguageTag";

        /// <summary>
        /// Strings that are well-formed language tags.
        /// </summary>
        private static readonly string[] ValidLanguageTagStrings =
        {
            "en", "es-419", TraditionalChineseTaiwan, CantoneseHongKong, SerbianLatinSerbia, SwissGerman1901, ResianSanGiorgio1994, "hy-Latn-IT-arevela", "gsw-u-sd-chzh",
            "he-IL-u-ca-hebrew-tz-jeruslm", "en-US-x-twain", PrivateUseOnly, KlingonGrandfathered, "zh-min-nan", "EN-us"
        };

        /// <summary>
        /// Strings that are not well-formed language tags.
        /// </summary>
        private static readonly string[] InvalidLanguageTagStrings =
        {
            "", "e", "en-", "-en", "en--US", "abcdefghi", RepeatedRegion, "zh-Hant-Hans", "de-CH-123456789", "en-US-a", "en-x-", "x-abcdefghi"
        };

        /// <summary>
        /// Strings that are well-formed language tags but repeat a variant subtag or an extension singleton (RFC 5646 §2.2.9).
        /// </summary>
        private static readonly string[] RepeatedSubtagStrings =
        {
            RepeatedVariant, "en-a-bbb-a-ccc"
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
        /// Subtags that each check accepts and rejects, by the ABNF of each kind of subtag in RFC 5646 §2.1. No check looks a subtag up, so <c>999</c> has the shape of a region.
        /// </summary>
        private static readonly (string CheckName, string Subtag, bool Expected)[] CheckCases =
        {
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "en", true),
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "EN", true),
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "qaa", true),
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "yue", true),
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "Hant", true),
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "rozaj", true),
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "", false),
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "a", false),
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "419", false),
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "1994", false),
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "abcdefghi", false),
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "en\n", false),
            (nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), "\x0130", false),
            (nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), "yue", true),
            (nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), "YUE", true),
            (nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), "abc", true),
            (nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), "", false),
            (nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), "en", false),
            (nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), "Hant", false),
            (nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), "419", false),
            (nameof(Bcp47LanguageTag.CheckScriptSubtag), "Hant", true),
            (nameof(Bcp47LanguageTag.CheckScriptSubtag), "hant", true),
            (nameof(Bcp47LanguageTag.CheckScriptSubtag), "Zzzz", true),
            (nameof(Bcp47LanguageTag.CheckScriptSubtag), "", false),
            (nameof(Bcp47LanguageTag.CheckScriptSubtag), "en", false),
            (nameof(Bcp47LanguageTag.CheckScriptSubtag), "TW", false),
            (nameof(Bcp47LanguageTag.CheckScriptSubtag), "1994", false),
            (nameof(Bcp47LanguageTag.CheckRegionSubtag), "TW", true),
            (nameof(Bcp47LanguageTag.CheckRegionSubtag), "tw", true),
            (nameof(Bcp47LanguageTag.CheckRegionSubtag), "419", true),
            (nameof(Bcp47LanguageTag.CheckRegionSubtag), "AA", true),
            (nameof(Bcp47LanguageTag.CheckRegionSubtag), "", false),
            (nameof(Bcp47LanguageTag.CheckRegionSubtag), "a", false),
            (nameof(Bcp47LanguageTag.CheckRegionSubtag), "999", true),
            (nameof(Bcp47LanguageTag.CheckRegionSubtag), "12", false),
            (nameof(Bcp47LanguageTag.CheckRegionSubtag), "Hant", false),
            (nameof(Bcp47LanguageTag.CheckVariantSubtag), "rozaj", true),
            (nameof(Bcp47LanguageTag.CheckVariantSubtag), "1994", true),
            (nameof(Bcp47LanguageTag.CheckVariantSubtag), "1abc", true),
            (nameof(Bcp47LanguageTag.CheckVariantSubtag), "", false),
            (nameof(Bcp47LanguageTag.CheckVariantSubtag), "en", false),
            (nameof(Bcp47LanguageTag.CheckVariantSubtag), "abcd", false),
            (nameof(Bcp47LanguageTag.CheckVariantSubtag), "abcdefghi", false),
            (nameof(Bcp47LanguageTag.CheckExtensionSubtag), "u-ca-hebrew", true),
            (nameof(Bcp47LanguageTag.CheckExtensionSubtag), "u-ca-hebrew-tz-jeruslm", true),
            (nameof(Bcp47LanguageTag.CheckExtensionSubtag), "", false),
            (nameof(Bcp47LanguageTag.CheckExtensionSubtag), "u", false),
            (nameof(Bcp47LanguageTag.CheckExtensionSubtag), "u-a", false),
            (nameof(Bcp47LanguageTag.CheckExtensionSubtag), "u-ca-x-a", false),
            (nameof(Bcp47LanguageTag.CheckExtensionSubtag), PrivateUseOnly, false),
            (nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), PrivateUseOnly, true),
            (nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), "X-a-b", true),
            (nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), "", false),
            (nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), "x", false),
            (nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), "x-", false),
            (nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), "x-abcdefghi", false),
            (nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), "u-ca-hebrew", false)
        };

        /// <summary>
        /// Each case of <see cref="CheckCases"/>, as a span over the whole of the subtag and as one from the middle of a longer string.
        /// </summary>
        public static TheoryData<string, SpanCheck, SpanInput, bool> SpanCheckCases
        {
            get
            {
                var data = new TheoryData<string, SpanCheck, SpanInput, bool>();
                foreach (var checkCase in CheckCases)
                {
                    data.Add(checkCase.CheckName, SpanChecks[checkCase.CheckName], SpanInput.Whole(checkCase.Subtag), checkCase.Expected);
                    data.Add(checkCase.CheckName, SpanChecks[checkCase.CheckName], SpanInput.Padded(checkCase.Subtag), checkCase.Expected);
                }

                return data;
            }
        }

        public static TheoryData<string, SpanCheck> EverySpanCheck
        {
            get
            {
                var data = new TheoryData<string, SpanCheck>();
                foreach (var check in SpanChecks)
                {
                    data.Add(check.Key, check.Value);
                }

                return data;
            }
        }

        public static TheoryData<SpanInput> ValidLanguageTagSpans => CreateSpanInputs(ValidLanguageTagStrings);

        public static TheoryData<SpanInput> InvalidLanguageTagSpans => CreateSpanInputs(InvalidLanguageTagStrings);

        public static TheoryData<string> ValidLanguageTags => new TheoryData<string>(ValidLanguageTagStrings);

        public static TheoryData<string> InvalidLanguageTags => new TheoryData<string>(InvalidLanguageTagStrings);

        public static TheoryData<SpanInput> RepeatedSubtagSpans => CreateSpanInputs(RepeatedSubtagStrings);

        /// <summary>
        /// Each valid language tag, from the middle of a longer string, with the tag itself.
        /// </summary>
        public static TheoryData<SpanInput, string> PaddedValidLanguageTags
        {
            get
            {
                var data = new TheoryData<SpanInput, string>();
                foreach (var languageTag in ValidLanguageTagStrings)
                {
                    data.Add(SpanInput.Padded(languageTag), languageTag);
                }

                return data;
            }
        }

        private static TheoryData<SpanInput> CreateSpanInputs(IEnumerable<string> values)
        {
            var data = new TheoryData<SpanInput>();
            foreach (var value in values)
            {
                data.Add(SpanInput.Whole(value));
                data.Add(SpanInput.Padded(value));
            }

            return data;
        }

        #endregion

        #region Test: Check*(ReadOnlySpan<char>)

        [Theory]
        [MemberData(nameof(SpanCheckCases), DisableDiscoveryEnumeration = true)]
        public void CheckSubtag_WithSpan_ReturnsWhetherSubtagIsValid(string checkName, SpanCheck check, SpanInput testSubtag, bool expectedResult)
        {
            // act
            var testResult = check(testSubtag.AsSpan());

            // assert
            testResult.Should().Be(expectedResult, "{0} must check only the characters of the span {1}", checkName, testSubtag);
        }

        [Theory]
        [MemberData(nameof(EverySpanCheck), DisableDiscoveryEnumeration = true)]
        public void CheckSubtag_WithDefaultSpan_ReturnsFalse(string checkName, SpanCheck check)
        {
            // act
            var testResult = check(default);

            // assert
            testResult.Should().BeFalse("{0} must reject an empty span rather than throw", checkName);
        }

        #endregion

        #region Test: IsWellFormed(ReadOnlySpan<char>)

        [Theory]
        [MemberData(nameof(ValidLanguageTagSpans), DisableDiscoveryEnumeration = true)]
        public void IsWellFormed_WithValidSpan_ReturnsTrue(SpanInput testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormed(testLanguageTag.AsSpan());

            // assert
            testResult.Should().BeTrue("only the characters in the span are read");
        }

        [Theory]
        [MemberData(nameof(InvalidLanguageTagSpans), DisableDiscoveryEnumeration = true)]
        public void IsWellFormed_WithInvalidSpan_ReturnsFalse(SpanInput testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormed(testLanguageTag.AsSpan());

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(InvalidLanguageTags))]
        public void IsWellFormed_WithInvalidString_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormed(testLanguageTag);

            // assert
            testResult.Should().BeFalse("the string overload must agree with the span overload");
        }

        [Theory]
        [MemberData(nameof(RepeatedSubtagSpans), DisableDiscoveryEnumeration = true)]
        public void IsWellFormed_WithRepeatedSubtagSpan_ReturnsTrue(SpanInput testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormed(testLanguageTag.AsSpan());

            // assert
            testResult.Should().BeTrue("RFC 5646 §2.2.9 lists repeated variant subtags and extension singletons under validity alone");
        }

        #endregion

        #region Test: TryCreate(ReadOnlySpan<char>, ...)

        [Theory]
        [MemberData(nameof(PaddedValidLanguageTags), DisableDiscoveryEnumeration = true)]
        public void TryCreate_WithValidSpan_ReturnsTrueAndCopyOfSpan(SpanInput testLanguageTag, string expectedResult)
        {
            // act
            var testResult = (Bcp47LanguageTag.TryCreate(testLanguageTag.AsSpan(), out var languageTag), (string)languageTag);

            // assert
            testResult.Should().Be((true, expectedResult), "the stored string is a copy of the span, with its case kept");
        }

        /// <summary>
        /// Language tags from the middle of a longer string, each with the subtags it decomposes into (RFC 5646 §2.2).
        /// </summary>
        public static TheoryData<SpanInput, LanguageTagSubtags> PaddedLanguageTagsWithSubtags => new TheoryData<SpanInput, LanguageTagSubtags>
        {
            { SpanInput.Padded(CantoneseHongKong), new LanguageTagSubtags("zh", new[] { "yue" }, null, "HK", new string[0], new Bcp47KeyedSubtag[0], null) },
            { SpanInput.Padded(SerbianLatinSerbia), new LanguageTagSubtags("sr", new string[0], "Latn", "RS", new string[0], new Bcp47KeyedSubtag[0], null) },
            { SpanInput.Padded(ResianSanGiorgio1994), new LanguageTagSubtags("sl", new string[0], null, null, new[] { "rozaj", "biske", "1994" }, new Bcp47KeyedSubtag[0], null) },
            {
                SpanInput.Padded("he-IL-u-ca-hebrew-tz-jeruslm"),
                new LanguageTagSubtags("he", new string[0], null, "IL", new string[0], new[] { new Bcp47KeyedSubtag("u-ca-hebrew-tz-jeruslm") }, null)
            },
            { SpanInput.Padded("en-US-x-twain"), new LanguageTagSubtags("en", new string[0], null, "US", new string[0], new Bcp47KeyedSubtag[0], new Bcp47KeyedSubtag("x-twain")) },
            { SpanInput.Padded(KlingonGrandfathered), LanguageTagSubtags.None }
        };

        [Theory]
        [MemberData(nameof(PaddedLanguageTagsWithSubtags), DisableDiscoveryEnumeration = true)]
        public void Subtags_OnLanguageTagCreatedFromSpan_AreTheSubtagsOfTheSpan(SpanInput testLanguageTag, LanguageTagSubtags expectedResult)
        {
            // arrange
            Bcp47LanguageTag.TryCreate(testLanguageTag.AsSpan(), out var languageTag);

            // act
            var testResult = LanguageTagSubtags.Of(languageTag);

            // assert
            testResult.Should().BeEquivalentTo(expectedResult, options => options.WithStrictOrdering());
        }

        [Theory]
        [MemberData(nameof(InvalidLanguageTagSpans), DisableDiscoveryEnumeration = true)]
        public void TryCreate_WithInvalidSpan_ReturnsFalseAndDefault(SpanInput testLanguageTag)
        {
            // act
            var testResult = (Bcp47LanguageTag.TryCreate(testLanguageTag.AsSpan(), out var languageTag), languageTag);

            // assert
            testResult.Should().Be((false, default(Bcp47LanguageTag)));
        }

        [Theory]
        [MemberData(nameof(RepeatedSubtagSpans), DisableDiscoveryEnumeration = true)]
        public void TryCreate_WithRepeatedSubtagSpan_ReturnsTrue(SpanInput testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag.AsSpan(), out _);

            // assert
            testResult.Should().BeTrue("RFC 5646 §2.2.9 lists repeated variant subtags and extension singletons under validity alone");
        }

        [Theory]
        [MemberData(nameof(RepeatedSubtagSpans), DisableDiscoveryEnumeration = true)]
        public void TryCreate_WithIsoCodeListsAndRepeatedSubtagSpan_ReturnsFalseAndDefault(SpanInput testLanguageTag)
        {
            // act
            var testResult = (Bcp47LanguageTag.TryCreate(testLanguageTag.AsSpan(), LanguageTagConformance.IsoCodeLists, out var languageTag), languageTag);

            // assert
            testResult.Should().Be((false, default(Bcp47LanguageTag)), "the ISO code lists approximate validity, so a variant subtag or extension singleton must not repeat");
        }

        public static TheoryData<SpanInput, string> PaddedRegisteredLanguageTags => new TheoryData<SpanInput, string>
        {
            { SpanInput.Padded("en-GB"), "en-GB" },
            { SpanInput.Padded(CantoneseHongKong), CantoneseHongKong },
            { SpanInput.Padded(ResianSanGiorgio1994), ResianSanGiorgio1994 },
            { SpanInput.Padded(KlingonGrandfathered), KlingonGrandfathered }
        };

        [Theory]
        [MemberData(nameof(PaddedRegisteredLanguageTags), DisableDiscoveryEnumeration = true)]
        public void TryCreate_WithRegistryAndValidSpan_ReturnsTrueAndCopyOfSpan(SpanInput testLanguageTag, string expectedResult)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = (Bcp47LanguageTag.TryCreate(testLanguageTag.AsSpan(), subtagRegistry, out var languageTag), (string)languageTag);

            // assert
            testResult.Should().Be((true, expectedResult));
        }

        public static TheoryData<SpanInput, LanguageTagSubtags> PaddedRegisteredLanguageTagsWithSubtags => new TheoryData<SpanInput, LanguageTagSubtags>
        {
            { SpanInput.Padded(CantoneseHongKong), new LanguageTagSubtags("zh", new[] { "yue" }, null, "HK", new string[0], new Bcp47KeyedSubtag[0], null) },
            { SpanInput.Padded(ResianSanGiorgio1994), new LanguageTagSubtags("sl", new string[0], null, null, new[] { "rozaj", "biske", "1994" }, new Bcp47KeyedSubtag[0], null) }
        };

        [Theory]
        [MemberData(nameof(PaddedRegisteredLanguageTagsWithSubtags), DisableDiscoveryEnumeration = true)]
        public void Subtags_OnLanguageTagCreatedFromSpanAndRegistry_AreTheSubtagsOfTheSpan(SpanInput testLanguageTag, LanguageTagSubtags expectedResult)
        {
            // arrange
            Bcp47LanguageTag.TryCreate(testLanguageTag.AsSpan(), IanaSubtagRegistryResource.Shared, out var languageTag);

            // act
            var testResult = LanguageTagSubtags.Of(languageTag);

            // assert
            testResult.Should().BeEquivalentTo(expectedResult, options => options.WithStrictOrdering());
        }

        [Theory]
        [InlineData("xyz")]
        [InlineData(RepeatedRegion)]
        [InlineData(RepeatedVariant)]
        [InlineData("")]
        public void TryCreate_WithRegistryAndInvalidSpan_ReturnsFalse(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = (Bcp47LanguageTag.TryCreate(testLanguageTag.AsSpan(), subtagRegistry, out var languageTag), languageTag);

            // assert
            testResult.Should().Be((false, default(Bcp47LanguageTag)));
        }

        [Fact]
        public void TryCreate_WithSpanAndNullRegistry_ReturnsFalse()
        {
            // act
            var testResult = (Bcp47LanguageTag.TryCreate("en".AsSpan(), (SubtagRegistry.SubtagRegistry)null, out var languageTag), languageTag);

            // assert
            testResult.Should().Be((false, default(Bcp47LanguageTag)), "the string overload returns false for a null registry too");
        }

        #endregion

        #region Test: AsSpan

        [Fact]
        public void AsSpan_OnValidLanguageTag_ReturnsStoredStringWithoutCopying()
        {
            // arrange
            const string languageTagString = TraditionalChineseTaiwan;
            var testLanguageTag = Bcp47LanguageTag.Create(languageTagString);

            // act
            var testResult = testLanguageTag.AsSpan();

            // assert
            (testResult == languageTagString.AsSpan()).Should().BeTrue("the span must refer to the string the tag was created from");
        }

        [Fact]
        public void AsSpan_OnDefaultLanguageTag_ReturnsEmptySpan()
        {
            // arrange
            var testLanguageTag = default(Bcp47LanguageTag);

            // act
            var testResult = testLanguageTag.AsSpan();

            // assert
            testResult.IsEmpty.Should().BeTrue("an uninitialised tag converts to a null string, which has no characters");
        }

        #endregion

#if NET6_0_OR_GREATER
        #region Test: TryFormat

        [Fact]
        public void TryFormat_WithLongEnoughDestination_CopiesLanguageTag()
        {
            // arrange
            const int destinationLength = 16;
            var testLanguageTag = Bcp47LanguageTag.Create(SerbianLatinSerbia);
            var destination = new char[destinationLength];

            // act
            var testResult = (testLanguageTag.TryFormat(destination, out var charsWritten, "G", CultureInfo.InvariantCulture), charsWritten);

            // assert
            testResult.Should().Be((true, SerbianLatinSerbiaLength));
            destination.Should().Equal('s', 'r', '-', 'L', 'a', 't', 'n', '-', 'R', 'S', '\0', '\0', '\0', '\0', '\0', '\0');
        }

        [Fact]
        public void TryFormat_WithExactLengthDestination_CopiesLanguageTag()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(SerbianLatinSerbia);
            var destination = new char[SerbianLatinSerbiaLength];

            // act
            var testResult = (testLanguageTag.TryFormat(destination, out var charsWritten, default, null), charsWritten);

            // assert
            testResult.Should().Be((true, SerbianLatinSerbiaLength));
            destination.Should().Equal('s', 'r', '-', 'L', 'a', 't', 'n', '-', 'R', 'S');
        }

        [Fact]
        public void TryFormat_WithShortDestination_ReturnsFalseAndWritesNothing()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(SerbianLatinSerbia);
            var destination = new char[ShortDestinationLength];

            // act
            var testResult = (testLanguageTag.TryFormat(destination, out var charsWritten, default, null), charsWritten);

            // assert
            testResult.Should().Be((false, 0));
            destination.Should().OnlyContain(character => character == '\0');
        }

        [Fact]
        public void TryFormat_OnDefaultLanguageTag_WritesToString()
        {
            // arrange
            const int destinationLength = 256;
            var testLanguageTag = default(Bcp47LanguageTag);
            var destination = new char[destinationLength];

            // act
            var testResult = (testLanguageTag.TryFormat(destination, out var charsWritten, default, null), charsWritten);

            // assert
            testResult.Should().Be((true, UninitialisedToString.Length));
            destination.Should().StartWith(UninitialisedToString.ToCharArray());
        }

        [Fact]
        public void Format_InCompositeFormatWithFormatAndAlignment_WritesLanguageTag()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(SwissGerman1901);

            // act
            var testResult = string.Format(CultureInfo.InvariantCulture, "[{0:X}] [{0,12}]", testLanguageTag);

            // assert
            testResult.Should().Be("[de-CH-1901] [  de-CH-1901]", "the format is ignored and alignment still applies");
        }

        [Fact]
        public void ToString_AsFormattableWithFormat_ReturnsLanguageTag()
        {
            // arrange
            IFormattable testLanguageTag = Bcp47LanguageTag.Create(SwissGerman1901);

            // act
            var testResult = testLanguageTag.ToString("X", CultureInfo.InvariantCulture);

            // assert
            testResult.Should().Be(SwissGerman1901, "the format is ignored");
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

        /// <summary>
        /// Each way to parse a language tag, taking a span from the middle of a longer string or the whole of a string.
        /// </summary>
        private static readonly Dictionary<string, Func<SpanInput, Bcp47LanguageTag>> SpanParsers = new Dictionary<string, Func<SpanInput, Bcp47LanguageTag>>
        {
            { "Parse(string) under the tr-TR culture", input => Bcp47LanguageTag.Parse(input.AsSpan().ToString(), CultureInfo.GetCultureInfo("tr-TR")) },
            { "IParsable.Parse", input => ParseGeneric<Bcp47LanguageTag>(input.AsSpan().ToString()) },
            { "Parse(ReadOnlySpan<char>)", input => Bcp47LanguageTag.Parse(input.AsSpan(), null) },
            { "ISpanParsable.Parse", input => ParseSpanGeneric<Bcp47LanguageTag>(input.AsSpan()) }
        };

        private static readonly Dictionary<string, SpanTryParse> SpanTryParsers = new Dictionary<string, SpanTryParse>
        {
            { "TryParse(string)", input => (Bcp47LanguageTag.TryParse(input.AsSpan().ToString(), null, out var result), (string)result) },
            { "IParsable.TryParse", input => (TryParseGeneric<Bcp47LanguageTag>(input.AsSpan().ToString(), out var result), (string)result) },
            { "TryParse(ReadOnlySpan<char>)", input => (Bcp47LanguageTag.TryParse(input.AsSpan(), null, out var result), (string)result) },
            { "ISpanParsable.TryParse", input => (TryParseSpanGeneric<Bcp47LanguageTag>(input.AsSpan(), out var result), (string)result) }
        };

        /// <summary>
        /// Each parser, with each valid language tag from the middle of a longer string and the tag itself.
        /// </summary>
        public static TheoryData<string, Func<SpanInput, Bcp47LanguageTag>, SpanInput, string> ValidLanguageTagParseCases
        {
            get
            {
                var data = new TheoryData<string, Func<SpanInput, Bcp47LanguageTag>, SpanInput, string>();
                foreach (var parser in SpanParsers)
                foreach (var languageTag in ValidLanguageTagStrings)
                {
                    data.Add(parser.Key, parser.Value, SpanInput.Padded(languageTag), languageTag);
                }

                return data;
            }
        }

        public static TheoryData<string, Func<SpanInput, Bcp47LanguageTag>, SpanInput, string> InvalidLanguageTagParseCases
        {
            get
            {
                var data = new TheoryData<string, Func<SpanInput, Bcp47LanguageTag>, SpanInput, string>();
                foreach (var parser in SpanParsers)
                foreach (var languageTag in InvalidLanguageTagStrings)
                {
                    data.Add(parser.Key, parser.Value, SpanInput.Whole(languageTag), languageTag);
                }

                return data;
            }
        }

        public static TheoryData<string, SpanTryParse, SpanInput, string> ValidLanguageTagTryParseCases
        {
            get
            {
                var data = new TheoryData<string, SpanTryParse, SpanInput, string>();
                foreach (var parser in SpanTryParsers)
                foreach (var languageTag in ValidLanguageTagStrings)
                {
                    data.Add(parser.Key, parser.Value, SpanInput.Padded(languageTag), languageTag);
                }

                return data;
            }
        }

        public static TheoryData<string, SpanTryParse, SpanInput> InvalidLanguageTagTryParseCases
        {
            get
            {
                var data = new TheoryData<string, SpanTryParse, SpanInput>();
                foreach (var parser in SpanTryParsers)
                foreach (var languageTag in InvalidLanguageTagStrings)
                {
                    data.Add(parser.Key, parser.Value, SpanInput.Whole(languageTag));
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(ValidLanguageTagParseCases), DisableDiscoveryEnumeration = true)]
        public void Parse_WithValidLanguageTag_ReturnsLanguageTag(string parserName, Func<SpanInput, Bcp47LanguageTag> parse, SpanInput testLanguageTag, string expectedResult)
        {
            // act
            string testResult = parse(testLanguageTag);

            // assert
            testResult.Should().Be(expectedResult, "{0} keeps the language tag as it was written", parserName);
        }

        [Theory]
        [MemberData(nameof(InvalidLanguageTagParseCases), DisableDiscoveryEnumeration = true)]
        public void Parse_WithInvalidLanguageTag_ThrowsLanguageTagFormatException(string parserName, Func<SpanInput, Bcp47LanguageTag> parse, SpanInput testLanguageTag, string expectedLanguageTag)
        {
            // act
            Action testAction = () => parse(testLanguageTag);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("{0} must reject a tag that is not well-formed", parserName).Which.LanguageTag.Should().Be(expectedLanguageTag);
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
        [MemberData(nameof(ValidLanguageTagTryParseCases), DisableDiscoveryEnumeration = true)]
        public void TryParse_WithValidLanguageTag_ReturnsTrueAndLanguageTag(string parserName, SpanTryParse tryParse, SpanInput testLanguageTag, string expectedResult)
        {
            // act
            var testResult = tryParse(testLanguageTag);

            // assert
            testResult.Should().Be((true, expectedResult), "{0} keeps the language tag as it was written", parserName);
        }

        [Theory]
        [MemberData(nameof(InvalidLanguageTagTryParseCases), DisableDiscoveryEnumeration = true)]
        public void TryParse_WithInvalidLanguageTag_ReturnsFalseAndDefault(string parserName, SpanTryParse tryParse, SpanInput testLanguageTag)
        {
            // act
            var testResult = tryParse(testLanguageTag);

            // assert
            testResult.Should().Be((false, (string)null), "{0} must reject a tag that is not well-formed", parserName);
        }

        [Fact]
        public void TryParse_WithNull_ReturnsFalseAndDefault()
        {
            // act
            var testResult = (Bcp47LanguageTag.TryParse((string)null, null, out var languageTag), languageTag);

            // assert
            testResult.Should().Be((false, default(Bcp47LanguageTag)));
        }

        #endregion
#endif
    }
}
