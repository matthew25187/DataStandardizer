// The matchTimeout overloads are obsolete but stay tested until v2.0 removes them (#256).
#pragma warning disable CS0618

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using DataStandardizer.Core;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
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

        /// <summary>
        /// A well-formed language tag long enough that matching it can't finish within a one-tick timeout.
        /// </summary>
        private static readonly string TimeoutLanguageTag = "en-x" + string.Concat(Enumerable.Repeat("-abcdefgh", 100000));

        /// <summary>
        /// Run an action with the current culture of the thread set to a given culture, then restore the culture.
        /// </summary>
        private static T RunUnderCulture<T>(string cultureName, Func<T> action)
        {
            var originalCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = new CultureInfo(cultureName);
            try
            {
                return action();
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        /// <summary>
        /// Subtag checks, each paired with a subtag it accepts followed by a line feed. A trailing line feed isn't part of any subtag
        /// (RFC 5646 §2.1), so every check must reject it.
        /// </summary>
        public static TheoryData<string, Func<string, bool>, string> TrailingNewlineSubtags => new TheoryData<string, Func<string, bool>, string>()
        {
            { nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), Bcp47LanguageTag.CheckPrimaryLanguageSubtag, "en\n" },
            { nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), Bcp47LanguageTag.CheckExtendedLanguageSubtag, "yue\n" },
            { nameof(Bcp47LanguageTag.CheckScriptSubtag), Bcp47LanguageTag.CheckScriptSubtag, "Latn\n" },
            { nameof(Bcp47LanguageTag.CheckRegionSubtag), Bcp47LanguageTag.CheckRegionSubtag, "US\n" },
            { nameof(Bcp47LanguageTag.CheckVariantSubtag), Bcp47LanguageTag.CheckVariantSubtag, "sursilv\n" },
            { nameof(Bcp47LanguageTag.CheckExtensionSubtag), Bcp47LanguageTag.CheckExtensionSubtag, "u-sd-chzh\n" },
            { nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), Bcp47LanguageTag.CheckPrivateUseSubtag, "x-private\n" }
        };

        /// <summary>
        /// Subtag checks, each paired with a subtag it accepts with one ASCII letter replaced by a non-ASCII case equivalent: the
        /// Kelvin sign U+212A for K or the long s U+017F for S. Subtags have only ASCII letters and digits (RFC 5646 §2.1), so every
        /// check must reject it.
        /// </summary>
        public static TheoryData<string, Func<string, bool>, string> NonAsciiCaseEquivalentSubtags => new TheoryData<string, Func<string, bool>, string>()
        {
            { nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), Bcp47LanguageTag.CheckPrimaryLanguageSubtag, "\u212Ao" },
            { nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), Bcp47LanguageTag.CheckPrimaryLanguageSubtag, "\u017Fv" },
            { nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), Bcp47LanguageTag.CheckExtendedLanguageSubtag, "\u017Fgn" },
            { nameof(Bcp47LanguageTag.CheckScriptSubtag), Bcp47LanguageTag.CheckScriptSubtag, "\u212Ana" },
            { nameof(Bcp47LanguageTag.CheckRegionSubtag), Bcp47LanguageTag.CheckRegionSubtag, "\u212AR" },
            { nameof(Bcp47LanguageTag.CheckVariantSubtag), Bcp47LanguageTag.CheckVariantSubtag, "\u017Fursilv" },
            { nameof(Bcp47LanguageTag.CheckExtensionSubtag), Bcp47LanguageTag.CheckExtensionSubtag, "u-\u017Fd-chzh" },
            { nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), Bcp47LanguageTag.CheckPrivateUseSubtag, "x-\u212A" }
        };

        /// <summary>
        /// Language tags that repeat a variant subtag (RFC 5646 §2.2.5¶5) or an extension singleton (§2.2.6¶3), each paired with
        /// the same tag without the repetition. The comparison is case-insensitive (§2.1.1). Private use subtags may repeat.
        /// </summary>
        public static TheoryData<string, string> DuplicateSubtagLanguageTags => new TheoryData<string, string>()
        {
            { "ca-valencia-valencia", "ca-valencia" },
            { "ca-valencia-VALENCIA", "ca-valencia" },
            { "en-1901-1901", "en-1901" },
            { "sl-rozaj-biske-rozaj", "sl-rozaj-biske" },
            { "en-a-bbb-a-ccc", "en-a-bbb-b-ccc" },
            { "en-a-bbb-A-ccc", "en-a-bbb-b-ccc" },
            { "en-a-bbb-b-ccc-a-ddd", "en-a-bbb-b-ccc-c-ddd" },
            { "en-a-bbb-a-ccc-x-a-a", "en-a-bbb-x-a-a" },
        };

        /// <summary>
        /// The 26 grandfathered language tags (RFC 5646 §2.1, §2.2.8): 17 irregular, then 9 regular, plus irregular and regular tags in other cases (§2.1.1).
        /// </summary>
        public static TheoryData<string> GrandfatheredLanguageTags => new TheoryData<string>()
        {
            "en-GB-oed", "i-ami", "i-bnn", "i-default", "i-enochian", "i-hak", "i-klingon", "i-lux", "i-mingo", "i-navajo", "i-pwn", "i-tao", "i-tay", "i-tsu",
            "sgn-BE-FR", "sgn-BE-NL", "sgn-CH-DE",
            "art-lojban", "cel-gaulish", "no-bok", "no-nyn", "zh-guoyu", "zh-hakka", "zh-min", "zh-min-nan", "zh-xiang",
            "EN-gb-OED", "I-KLINGON", "Art-Lojban",
        };

        /// <summary>
        /// Tags that start with an irregular grandfathered tag but are not one, and do not match the <c>langtag</c> production either.
        /// </summary>
        public static TheoryData<string> GrandfatheredPrefixLanguageTags => new TheoryData<string>()
        {
            "en-GB-oed-x-foo", "i-klingon-x-a", "i-klingon-i-klingon", "sgn-BE-FR-BE",
        };

        /// <summary>
        /// Four-letter Primary Language subtags, which are reserved for future standardisation (RFC 5646 §2.1, §2.2.1¶4), in various cases (§2.1.1).
        /// </summary>
        public static TheoryData<string> ReservedPrimaryLanguageSubtags => new TheoryData<string>()
        {
            "abcd", "ABCD", "Wxyz",
        };

        /// <summary>
        /// Subtags of four to eight characters that contain a digit, and so are neither reserved (<c>4ALPHA</c>) nor registered (<c>5*8ALPHA</c>) Primary Language subtags (RFC 5646 §2.1, §2.2.1).
        /// </summary>
        public static TheoryData<string> NonAlphaPrimaryLanguageSubtags => new TheoryData<string>()
        {
            "1234", "abc1", "12345", "abcd1", "1bcde", "a1b2c3d4",
        };

        /// <summary>
        /// Language tags with Extended Language subtags after a Primary Language subtag of four to eight letters, each with a control tag that has them after a two- or three-letter one (RFC 5646 §2.1, §2.2.2).
        /// </summary>
        public static TheoryData<string, string> MisplacedExtendedLanguageSubtagLanguageTags => new TheoryData<string, string>()
        {
            { "abcde-aaa", "zh-aaa" },
            { "abcd-yue", "zh-yue" },
            { "klingon-aaa-bbb", "tlh-aaa-bbb" },
            { "abcdefgh-cmn-Hans-CN", "zh-cmn-Hans-CN" },
        };

        /// <summary>
        /// The UN M.49 code for the Channel Islands, which RFC 5646 §2.2.4¶4E allows to be registered as a Region subtag, but which is not registered.
        /// </summary>
        private const ushort ChannelIslandsUnM49Code = 830;

        /// <summary>
        /// Language tags with the unregistered Region subtag <c>830</c> (RFC 5646 §2.2.4¶4E-F), each with a control tag that has a registered UN M.49 Region subtag in its place.
        /// </summary>
        public static TheoryData<string, string> ChannelIslandsLanguageTags => new TheoryData<string, string>()
        {
            { "en-830", "en-154" },
            { "fr-Latn-830", "fr-Latn-154" },
            { "en-830-x-foo", "en-154-x-foo" },
        };

        /// <summary>
        /// Language tags whose subtags are registered only through the private use range records <c>qaa..qtz</c>, <c>Qaaa..Qabx</c>, <c>QM..QZ</c> and <c>XA..XZ</c> (RFC 5646 §3.1.1, §3.1.4),
        /// at each end of a range and inside it, in various cases (§2.1.1). <c>Qaay</c> and <c>Qaaz</c> are left out until the default rules accept them (#323).
        /// </summary>
        public static TheoryData<string> PrivateUseRangeLanguageTags => new TheoryData<string>()
        {
            "qaa", "qtz", "qbm", "QTZ", "en-Qaaa", "en-Qaba", "en-Qabx", "en-QM", "en-QS", "en-QZ", "en-XA", "en-XZ", "qaa-Qabx-XZ",
        };

        /// <summary>
        /// Language tags with an unregistered Script or Region subtag just past the end of a private use range record, each with a control tag that has the end of the range in its place.
        /// </summary>
        public static TheoryData<string, string> OutsidePrivateUseRangeLanguageTags => new TheoryData<string, string>()
        {
            { "en-Qaby", "en-Qabx" },
            { "en-QL", "en-QM" },
        };

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

        #region Test: CheckExtensionSubtag_WithMoreThanOneExtension

        [Theory]
        [InlineData("a-bbb-a-ccc"), InlineData("a-bbb-b-ccc")]
        public void CheckExtensionSubtag_WithMoreThanOneExtension_ReturnsFalse(string testSubtag)
        {
            // act
            var testResult = Bcp47LanguageTag.CheckExtensionSubtag(testSubtag);

            // assert
            testResult.Should().BeFalse("'{0}' is more than one extension; repeated singletons are rejected when the whole tag is checked", testSubtag);
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
                    yield return new object[] { string.Concat(Enumerable.Repeat(testCharacter, 9)) };
                }

                // Four to eight letters are reserved or registered subtags (ref. RFC 5646 §2.2.1¶4-5), but four to eight digits are neither.
                foreach (var testDigit in TestDigits)
                {
                    yield return new object[] { string.Concat(Enumerable.Repeat(testDigit, 4)) };
                    yield return new object[] { string.Concat(Enumerable.Repeat(testDigit, 8)) };
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

        #region Test: CheckPrimaryLanguageSubtag_WithReservedSubtag

        [Theory]
        [MemberData(nameof(ReservedPrimaryLanguageSubtags))]
        public void CheckPrimaryLanguageSubtag_WithReservedSubtag_ReturnsTrue(string testSubtag)
        {
            // act
            var testResult = Bcp47LanguageTag.CheckPrimaryLanguageSubtag(testSubtag);

            // assert
            testResult.Should().BeTrue("the subtag '{0}' is a reserved Primary Language subtag, which is well-formed (RFC 5646 §2.1, §2.2.1¶4)", testSubtag);
        }

        #endregion

        #region Test: CheckPrimaryLanguageSubtag_WithNonAlphaSubtag

        [Theory]
        [MemberData(nameof(NonAlphaPrimaryLanguageSubtags))]
        public void CheckPrimaryLanguageSubtag_WithNonAlphaSubtag_ReturnsFalse(string testSubtag)
        {
            // act
            var testResult = Bcp47LanguageTag.CheckPrimaryLanguageSubtag(testSubtag);

            // assert
            testResult.Should().BeFalse("the subtag '{0}' contains a digit, and a Primary Language subtag is letters only (RFC 5646 §2.1, §2.2.1)", testSubtag);
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

        #region Test: CheckSubtag_WithTrailingNewline

        [Theory]
        [MemberData(nameof(TrailingNewlineSubtags))]
        public void CheckSubtag_WithTrailingNewline_ReturnsFalse(string checkName, Func<string, bool> check, string testSubtag)
        {
            // act
            var testResult = check(testSubtag);

            // assert
            testResult.Should().BeFalse("{0} must not accept a subtag followed by a line feed", checkName);
        }

        #endregion

        #region Test: CheckSubtag_WithNonAsciiCaseEquivalent

        [Theory]
        [MemberData(nameof(NonAsciiCaseEquivalentSubtags))]
        public void CheckSubtag_WithNonAsciiCaseEquivalent_ReturnsFalse(string checkName, Func<string, bool> check, string testSubtag)
        {
            // act
            var testResult = check(testSubtag);

            // assert
            testResult.Should().BeFalse("{0} must not accept a non-ASCII letter", checkName);
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
                foreach (var testSubtag in Enum.GetNames(typeof(Iso3166Part1Alpha2Country)))
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
                var globalCodes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetGlobalCode()).Where(code => code != null).Cast<ushort>();
                var regionCodes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetRegionCode()).Where(code => code != null).Cast<ushort>();
                var subRegionCodes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetSubRegionCode()).Where(code => code != null).Cast<ushort>();
                var intermediateRegionCode = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetIntermediateRegionCode()).Where(code => code != null).Cast<ushort>();
                // 830 (Channel Islands) is not registered, so it is not a Region subtag (ref. RFC 5646 §2.2.4¶4E-F)
                _testCases = globalCodes.Union(regionCodes).Union(subRegionCodes).Union(intermediateRegionCode).Where(code => code != ChannelIslandsUnM49Code).Select(code => new object[] { $"{code:000}" }).AsEnumerable();
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

        #region Test: CheckRegionSubtag_WithChannelIslandsSubtag

        [Fact]
        public void CheckRegionSubtag_WithChannelIslandsSubtag_ReturnsFalse()
        {
            // act
            var testResult = Bcp47LanguageTag.CheckRegionSubtag("830");

            // assert
            testResult.Should().BeFalse("830 (Channel Islands) may be registered, but is not, and an unregistered UN M.49 area code must not be used (RFC 5646 §2.2.4¶4E-F)");
        }

        #endregion

        #region Test: CheckScriptSubtag_WithInvalidSubtag

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
                foreach (var testSubtag in Enum.GetNames(typeof(Iso15924Script)))
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

        #region Test: CheckVariantSubtag_WithMoreThanOneSubtag

        [Theory]
        [InlineData("1901-1901"), InlineData("1901-1996")]
        public void CheckVariantSubtag_WithMoreThanOneSubtag_ReturnsFalse(string testSubtag)
        {
            // act
            var testResult = Bcp47LanguageTag.CheckVariantSubtag(testSubtag);

            // assert
            testResult.Should().BeFalse("'{0}' is more than one subtag; repeated variants are rejected when the whole tag is checked", testSubtag);
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

        #region Test: ExtendedLanguageSubtags_WhenReturnedArrayIsChanged_DoesNotChangeLanguageTag

        [Fact]
        public void ExtendedLanguageSubtags_WhenReturnedArrayIsChanged_DoesNotChangeLanguageTag()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create("zh-yue-HK");
            var testChangedResult = testLanguageTag.ExtendedLanguageSubtags;
            testChangedResult[0] = "cmn";

            // act
            var testResult = testLanguageTag.ExtendedLanguageSubtags;

            // assert
            testResult.Should().NotBeSameAs(testChangedResult);
            testResult.Should().Equal("yue");
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

        #region Test: IsWellFormedLanguageTagString_WithTrailingNewline

        [Theory]
        [InlineData("en\n"), InlineData("en-US\n"), InlineData("x-private\n")]
        public void IsWellFormedLanguageTagString_WithTrailingNewline_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

            // assert
            testResult.Should().BeFalse("a language tag followed by a line feed is not well-formed");
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithNonAsciiCaseEquivalent

        [Theory]
        [InlineData("\u212Ao"), InlineData("\u017Fv"), InlineData("en-x-\u212A"), InlineData("en-x-\u017F"), InlineData("en-\u212AR")]
        public void IsWellFormedLanguageTagString_WithNonAsciiCaseEquivalent_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

            // assert
            testResult.Should().BeFalse("a language tag has only ASCII letters and digits (RFC 5646 §2.1)");
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithDuplicateSubtags

        [Theory]
        [MemberData(nameof(DuplicateSubtagLanguageTags))]
        public void IsWellFormedLanguageTagString_WithDuplicateSubtags_ReturnsFalse(string testLanguageTag, string controlLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);
            var controlResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(controlLanguageTag);

            // assert
            controlResult.Should().BeTrue("the language tag '{0}' has no repeated subtags", controlLanguageTag);
            testResult.Should().BeFalse("the language tag '{0}' repeats a variant subtag or extension singleton (RFC 5646 §2.2.5, §2.2.6)", testLanguageTag);
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithChannelIslandsRegionSubtag

        [Theory]
        [MemberData(nameof(ChannelIslandsLanguageTags))]
        public void IsWellFormedLanguageTagString_WithChannelIslandsRegionSubtag_ReturnsFalse(string testLanguageTag, string controlLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);
            var controlResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(controlLanguageTag);

            // assert
            controlResult.Should().BeTrue("the language tag '{0}' has a registered UN M.49 Region subtag", controlLanguageTag);
            testResult.Should().BeFalse("the Region subtag 830 of '{0}' is not registered (RFC 5646 §2.2.4¶4E-F)", testLanguageTag);
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithGrandfatheredLanguageTag

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void IsWellFormedLanguageTagString_WithGrandfatheredLanguageTag_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

            // assert
            testResult.Should().BeTrue("'{0}' is a grandfathered language tag, which is well-formed (RFC 5646 §2.1, §2.2.9)", testLanguageTag);
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithGrandfatheredPrefix

        [Theory]
        [MemberData(nameof(GrandfatheredPrefixLanguageTags))]
        public void IsWellFormedLanguageTagString_WithGrandfatheredPrefix_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

            // assert
            testResult.Should().BeFalse("'{0}' only starts with a grandfathered language tag, and is not otherwise well-formed", testLanguageTag);
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithReservedPrimaryLanguageSubtag

        [Theory]
        [MemberData(nameof(ReservedPrimaryLanguageSubtags))]
        public void IsWellFormedLanguageTagString_WithReservedPrimaryLanguageSubtag_ReturnsTrue(string testSubtag)
        {
            // arrange
            var testLanguageTags = new[] { testSubtag, $"{testSubtag}-Latn-US", $"{testSubtag}-x-test" };

            foreach (var testLanguageTag in testLanguageTags)
            {
                // act
                var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

                // assert
                testResult.Should().BeTrue("'{0}' has a reserved Primary Language subtag, which is well-formed (RFC 5646 §2.1, §2.2.1¶4)", testLanguageTag);
            }
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithNonAlphaPrimaryLanguageSubtag

        [Theory]
        [MemberData(nameof(NonAlphaPrimaryLanguageSubtags))]
        public void IsWellFormedLanguageTagString_WithNonAlphaPrimaryLanguageSubtag_ReturnsFalse(string testSubtag)
        {
            // arrange
            var testLanguageTags = new[] { testSubtag, $"{testSubtag}-US", $"{testSubtag}-x-test" };

            foreach (var testLanguageTag in testLanguageTags)
            {
                // act
                var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

                // assert
                testResult.Should().BeFalse("the Primary Language subtag of '{0}' contains a digit (RFC 5646 §2.1, §2.2.1)", testLanguageTag);
            }
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithMisplacedExtendedLanguageSubtags

        [Theory]
        [MemberData(nameof(MisplacedExtendedLanguageSubtagLanguageTags))]
        public void IsWellFormedLanguageTagString_WithMisplacedExtendedLanguageSubtags_ReturnsFalse(string testLanguageTag, string controlLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);
            var controlResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(controlLanguageTag);

            // assert
            controlResult.Should().BeTrue("the language tag '{0}' has Extended Language subtags after a two- or three-letter Primary Language subtag", controlLanguageTag);
            testResult.Should().BeFalse("the language tag '{0}' has Extended Language subtags after a Primary Language subtag of four to eight letters (RFC 5646 §2.1, §2.2.2)", testLanguageTag);
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

        #region Test: ExtensionSubtags_WhenReturnedArrayIsChanged_DoesNotChangeLanguageTag

        [Fact]
        public void ExtensionSubtags_WhenReturnedArrayIsChanged_DoesNotChangeLanguageTag()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create("he-IL-u-ca-hebrew");
            var testChangedResult = testLanguageTag.ExtensionSubtags;
            testChangedResult[0] = (Bcp47KeyedSubtag)"t-ca-hebrew";

            // act
            var testResult = testLanguageTag.ExtensionSubtags;

            // assert
            testResult.Should().NotBeSameAs(testChangedResult);
            testResult.Should().Equal((Bcp47KeyedSubtag)"u-ca-hebrew");
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

        #region Test: PrimaryLanguageSubtag_WhenReservedSubtag

        [Theory]
        [MemberData(nameof(ReservedPrimaryLanguageSubtags))]
        public void PrimaryLanguageSubtag_WhenReservedSubtag_ReturnsPrimaryLanguageSubtag(string expectedResult)
        {
            // arrange
            var testLanguageTag = $"{expectedResult}-Latn-US";
            var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

            // act
            var testResult = testSubject.PrimaryLanguageSubtag;

            // assert
            testResult.Should().Be(expectedResult, "'{0}' is the Primary Language subtag of the language tag {1}", expectedResult, testLanguageTag);
            testSubject.ScriptSubtag.Should().Be("Latn");
            testSubject.RegionSubtag.Should().Be("US");
        }

        #endregion

        #region Test: PrimaryLanguageSubtag_OnPrivateUseLanguageTag

        [Theory]
        [InlineData("x-private"), InlineData("X-PRIVATE"), InlineData("x-fr-CH")]
        public void PrimaryLanguageSubtag_OnPrivateUseLanguageTag_ReturnsEmptyString(string testLanguageTag)
        {
            // arrange
            var testSubjects = new[] { Bcp47LanguageTag.Create(testLanguageTag), Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry) };

            foreach (var testSubject in testSubjects)
            {
                // act
                var testResult = testSubject.PrimaryLanguageSubtag;

                // assert
                testResult.Should().BeEmpty("a private use language tag has no Primary Language subtag (RFC 5646 §2.2.7)");
                testSubject.PrivateUseSubtag.Should().NotBeNull();
            }
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
                _testCases = Enum.GetValues(typeof(Iso3166Part1Alpha2Country)).Cast<Iso3166Part1Alpha2Country>().Select(code => new object[] { $"en-{code}", code.ToString() }).AsEnumerable();
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
                    var globalM49Codes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetGlobalCode()).Where(code => code != null).Cast<ushort>().Distinct();
                    foreach (var globalM49Code in globalM49Codes)
                    {
                        yield return new object[] { $"en-{globalM49Code:000}", $"{globalM49Code:000}" };
                    }

                    var regionM49Codes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetRegionCode()).Where(code => code != null).Cast<ushort>().Distinct();
                    foreach (var regionM49Code in regionM49Codes)
                    {
                        yield return new object[] { $"en-{regionM49Code:000}", $"{regionM49Code:000}" };
                    }

                    var subRegionM49Codes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetSubRegionCode()).Where(code => code != null).Cast<ushort>().Distinct();
                    foreach (var subRegionM49Code in subRegionM49Codes)
                    {
                        yield return new object[] { $"en-{subRegionM49Code:000}", $"{subRegionM49Code:000}" };
                    }

                    var intermediateRegionM49Codes = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Select(code => code.GetIntermediateRegionCode()).Where(code => code != null).Cast<ushort>().Distinct();
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
                _testCases = Enum.GetValues(typeof(UnM49AreaByAlpha2CountryCode)).Cast<UnM49AreaByAlpha2CountryCode>().Cast<ushort>().Select(code => new object[] { $"en-{code:000}", code.ToString("000") }).AsEnumerable();
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
                _testCases = Enum.GetValues(typeof(Iso15924Script)).Cast<Iso15924Script>().Select(code => new object[] { $"en-{code}", code.ToString() }).AsEnumerable();
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

        #region Test: VariantSubtags_WhenReturnedArrayIsChanged_DoesNotChangeLanguageTag

        [Fact]
        public void VariantSubtags_WhenReturnedArrayIsChanged_DoesNotChangeLanguageTag()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create("sl-rozaj-biske-1994");
            var testChangedResult = testLanguageTag.VariantSubtags;
            testChangedResult[0] = "1901";

            // act
            var testResult = testLanguageTag.VariantSubtags;

            // assert
            testResult.Should().NotBeSameAs(testChangedResult);
            testResult.Should().Equal("rozaj", "biske", "1994");
        }

        #endregion

        #region Test: SubtagProperties_OnGrandfatheredLanguageTag

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void SubtagProperties_OnGrandfatheredLanguageTag_ReturnNoSubtags(string testLanguageTag)
        {
            // arrange
            var testLanguageTags = new[] { Bcp47LanguageTag.Create(testLanguageTag), Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry) };

            foreach (var languageTag in testLanguageTags)
            {
                // assert
                languageTag.PrimaryLanguageSubtag.Should().BeEmpty("a grandfathered language tag is interpreted as a whole (RFC 5646 §2.2.8)");
                languageTag.ExtendedLanguageSubtags.Should().BeEmpty();
                languageTag.ScriptSubtag.Should().BeNull();
                languageTag.RegionSubtag.Should().BeNull();
                languageTag.VariantSubtags.Should().BeEmpty();
                languageTag.ExtensionSubtags.Should().BeEmpty();
                languageTag.PrivateUseSubtag.Should().BeNull();
            }
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

        #region Test: ToIso3166Part1Alpha2_WithLanguageTagHavingUnM49RegionSubtag

        [Theory]
        [InlineData("es-419")]
        [InlineData("en-001")]
        public void ToIso3166Part1Alpha2_WithLanguageTagHavingUnM49RegionSubtag_ReturnsNull(string languageTag)
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(languageTag);

            // act
            var testResult = testLanguageTag.ToIso3166Part1Alpha2();

            // assert
            testResult.Should().BeNull();
        }

        #endregion

        #region Test: ToIso3166Part1Alpha2_WithLanguageTagHavingAlphaRegionSubtag

        [Theory]
        [InlineData("en-US")]
        [InlineData("en-us")]
        public void ToIso3166Part1Alpha2_WithLanguageTagHavingAlphaRegionSubtag_ReturnsIso3166Part1Code(string languageTag)
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(languageTag);

            // act
            var testResult = testLanguageTag.ToIso3166Part1Alpha2();

            // assert
            testResult.Should().Be(Iso3166Part1Alpha2Country.US);
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

        #region Test: ToIso639_WithLanguageTagHavingNoPrimaryLanguageSubtag

        [Theory]
        [InlineData("x-private"), InlineData("i-klingon"), InlineData("art-lojban")]
        public void ToIso639_WithLanguageTagHavingNoPrimaryLanguageSubtag_ReturnsNull(string testLanguageTag)
        {
            // arrange
            var testSubjects = new[] { Bcp47LanguageTag.Create(testLanguageTag), Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry) };

            foreach (var testSubject in testSubjects)
            {
                // assert
                testSubject.ToIso639Part1().Should().BeNull();
                testSubject.ToIso639Part2T().Should().BeNull();
                testSubject.ToIso639Part3().Should().BeNull();
                testSubject.ToIso639Part5().Should().BeNull();
            }
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

        #region Test: ExplicitConversion_WhenLanguageTagIsNull

        [Fact]
        public void ExplicitConversion_WhenLanguageTagIsNull_ThrowsInvalidCastException()
        {
            // act
            Action testAction = () => _ = (Bcp47LanguageTag)(string)null;

            // assert
            testAction.Should().Throw<InvalidCastException>("a null string is not a language tag").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagNull);
        }

        #endregion

        #region Test: ExplicitConversion_WhenLanguageTagIsInvalid

        [Theory]
        [InlineData(""), InlineData("NotAValidLanguageTag"), InlineData("en--US"), InlineData("en-US-US")]
        public void ExplicitConversion_WhenLanguageTagIsInvalid_ThrowsInvalidCastException(string testLanguageTag)
        {
            // act
            Action testAction = () => _ = (Bcp47LanguageTag)testLanguageTag;

            // assert
            testAction.Should().Throw<InvalidCastException>("'{0}' is not a well-formed language tag", testLanguageTag).WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: ExplicitConversion_WhenLanguageTagIsValid

        [Theory]
        [InlineData("en"), InlineData("zh-Hant-TW"), InlineData("i-klingon"), InlineData("x-private")]
        public void ExplicitConversion_WhenLanguageTagIsValid_ReturnsSameTagAsCreate(string testLanguageTag)
        {
            // act
            var testResult = (Bcp47LanguageTag)testLanguageTag;

            // assert
            testResult.Should().Be(Bcp47LanguageTag.Create(testLanguageTag), "the conversion applies the same rules as Create");
            ((string)testResult).Should().Be(testLanguageTag, "the conversion keeps the original string");
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenLanguageTagIsNull

        [Fact]
        public void Create_WithLanguageTagWhenLanguageTagIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(null);

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

        #region Test: Create_WithLanguageTagWhenLanguageTagHasTrailingNewline

        [Theory]
        [InlineData("en\n"), InlineData("en-US\n")]
        public void Create_WithLanguageTagWhenLanguageTagHasTrailingNewline_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("a language tag followed by a line feed is not well-formed").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndMatchTimeoutWhenLanguageTagHasTrailingNewline

        [Theory]
        [InlineData("en\n"), InlineData("en-US\n")]
        public void Create_WithLanguageTagAndMatchTimeoutWhenLanguageTagHasTrailingNewline_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag, TimeSpan.FromSeconds(10));

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("a language tag followed by a line feed is not well-formed").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasTrailingNewline

        [Theory]
        [InlineData("en\n"), InlineData("en-US\n")]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasTrailingNewline_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("a language tag followed by a line feed is not well-formed");
        }

        #endregion

        #region Test: Create_WithLanguageTagSubtagRegistryAndMatchTimeoutWhenLanguageTagHasTrailingNewline

        [Theory]
        [InlineData("en\n"), InlineData("en-US\n")]
        public void Create_WithLanguageTagSubtagRegistryAndMatchTimeoutWhenLanguageTagHasTrailingNewline_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry, TimeSpan.FromSeconds(10));

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("a language tag followed by a line feed is not well-formed");
        }

        #endregion

        #region Test: Create_WithLanguageTagAndMatchTimeoutUnderTurkishCulture

        [Theory]
        [InlineData("IT"), InlineData("it-CH"), InlineData("IT-ch"), InlineData("it")]
        public void Create_WithLanguageTagAndMatchTimeoutUnderTurkishCulture_ReturnsLanguageTag(string testLanguageTag)
        {
            // act
            // A match timeout no other test uses makes the language tag expression get built under the Turkish culture.
            var testResult = RunUnderCulture("tr-TR", () => Bcp47LanguageTag.Create(testLanguageTag, TimeSpan.FromMilliseconds(25_101)));

            // assert
            ((string)testResult).Should().Be(testLanguageTag, "case-insensitive matching of a language tag doesn't depend on the current culture (RFC 5646 §2.1.1)");
        }

        #endregion

        #region Test: Create_WithLanguageTagSubtagRegistryAndMatchTimeoutUnderTurkishCulture

        [Theory]
        [InlineData("IT"), InlineData("it-CH"), InlineData("IT-ch"), InlineData("it")]
        public void Create_WithLanguageTagSubtagRegistryAndMatchTimeoutUnderTurkishCulture_ReturnsLanguageTag(string testLanguageTag)
        {
            // act
            // A match timeout no other test uses makes the language tag expression get built under the Turkish culture.
            var testResult = RunUnderCulture("tr-TR", () => Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry, TimeSpan.FromMilliseconds(25_102)));

            // assert
            ((string)testResult).Should().Be(testLanguageTag, "case-insensitive matching of a language tag doesn't depend on the current culture (RFC 5646 §2.1.1)");
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenLanguageTagHasNonAsciiCaseEquivalent

        [Theory]
        [InlineData("\u212Ao"), InlineData("\u017Fv"), InlineData("en-x-\u212A"), InlineData("en-x-\u017F"), InlineData("en-\u212AR")]
        public void Create_WithLanguageTagWhenLanguageTagHasNonAsciiCaseEquivalent_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("a language tag has only ASCII letters and digits (RFC 5646 §2.1), so the Kelvin sign and long s aren't letters of one").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndMatchTimeoutWhenLanguageTagHasNonAsciiCaseEquivalent

        [Theory]
        [InlineData("\u212Ao"), InlineData("en-x-\u212A"), InlineData("en-x-\u017F")]
        public void Create_WithLanguageTagAndMatchTimeoutWhenLanguageTagHasNonAsciiCaseEquivalent_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag, TimeSpan.FromSeconds(10));

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("a language tag has only ASCII letters and digits (RFC 5646 §2.1)").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasNonAsciiCaseEquivalent

        [Theory]
        [InlineData("\u212Ao"), InlineData("en-x-\u212A"), InlineData("en-x-\u017F")]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasNonAsciiCaseEquivalent_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("a language tag has only ASCII letters and digits (RFC 5646 §2.1)");
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenPrimaryLanguageSubtagIsNonAlpha

        [Theory]
        [MemberData(nameof(NonAlphaPrimaryLanguageSubtags))]
        public void Create_WithLanguageTagWhenPrimaryLanguageSubtagIsNonAlpha_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("the Primary Language subtag of '{0}' contains a digit (RFC 5646 §2.1, §2.2.1)", testLanguageTag).WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenPrimaryLanguageSubtagIsReserved

        [Theory]
        [MemberData(nameof(ReservedPrimaryLanguageSubtags))]
        public void Create_WithLanguageTagWhenPrimaryLanguageSubtagIsReserved_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // act
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag);
            var testResult = (string)languageTag;

            // assert
            testResult.Should().Be(testLanguageTag, "'{0}' is a reserved Primary Language subtag, which is well-formed (RFC 5646 §2.1, §2.2.1¶4)", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenExtendedLanguageSubtagsAreMisplaced

        [Theory]
        [MemberData(nameof(MisplacedExtendedLanguageSubtagLanguageTags))]
        public void Create_WithLanguageTagWhenExtendedLanguageSubtagsAreMisplaced_ThrowsLanguageTagFormatException(string testLanguageTag, string controlLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag);
            Action controlAction = () => Bcp47LanguageTag.Create(controlLanguageTag);

            // assert
            controlAction.Should().NotThrow("the language tag '{0}' has Extended Language subtags after a two- or three-letter Primary Language subtag", controlLanguageTag);
            testAction.Should().Throw<LanguageTagFormatException>("the language tag '{0}' has Extended Language subtags after a Primary Language subtag of four to eight letters (RFC 5646 §2.1, §2.2.2)", testLanguageTag).WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenLanguageTagHasDuplicateSubtags

        [Theory]
        [MemberData(nameof(DuplicateSubtagLanguageTags))]
        public void Create_WithLanguageTagWhenLanguageTagHasDuplicateSubtags_ThrowsLanguageTagFormatException(string testLanguageTag, string controlLanguageTag)
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag);
            Action controlAction = () => _ = Bcp47LanguageTag.Create(controlLanguageTag);

            // assert
            controlAction.Should().NotThrow("the language tag '{0}' has no repeated subtags", controlLanguageTag);
            testAction.Should().Throw<LanguageTagFormatException>("a variant subtag or extension singleton is repeated (RFC 5646 §2.2.5, §2.2.6)").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenRegionSubtagIsChannelIslands

        [Theory]
        [MemberData(nameof(ChannelIslandsLanguageTags))]
        public void Create_WithLanguageTagWhenRegionSubtagIsChannelIslands_ThrowsLanguageTagFormatException(string testLanguageTag, string controlLanguageTag)
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag);
            Action controlAction = () => _ = Bcp47LanguageTag.Create(controlLanguageTag);

            // assert
            controlAction.Should().NotThrow("the language tag '{0}' has a registered UN M.49 Region subtag", controlLanguageTag);
            testAction.Should().Throw<LanguageTagFormatException>("the Region subtag 830 of '{0}' is not registered (RFC 5646 §2.2.4¶4E-F)", testLanguageTag).WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenRegionSubtagIsChannelIslands

        [Theory]
        [MemberData(nameof(ChannelIslandsLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenRegionSubtagIsChannelIslands_ThrowsLanguageTagFormatException(string testLanguageTag, string controlLanguageTag)
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry);
            Action controlAction = () => _ = Bcp47LanguageTag.Create(controlLanguageTag, _fixture.IanaSubtagRegistry);

            // assert
            controlAction.Should().NotThrow("the language tag '{0}' has a registered UN M.49 Region subtag", controlLanguageTag);
            testAction.Should().Throw<LanguageTagFormatException>("the Subtag Registry has no record for the Region subtag 830 of '{0}'", testLanguageTag).WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenChannelIslandsIsRegistered

        [Theory]
        [InlineData("en-830"), InlineData("fr-Latn-830"), InlineData("en-830-x-foo")]
        public void Create_WithLanguageTagAndSubtagRegistryWhenChannelIslandsIsRegistered_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = SubtagRegistry.SubtagRegistry.CreateFromContent(string.Join("\n", _fixture.IanaSubtagRegistry.ToString().TrimEnd(), "%%", "Type: region", "Subtag: 830", "Description: Channel Islands", "Added: 2026-10-19"));

            // act
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            languageTag.RegionSubtag.Should().Be("830", "a Subtag Registry may register 830 (Channel Islands) as a Region subtag (RFC 5646 §2.2.4¶4E)");
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenLanguageTagIsGrandfathered

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void Create_WithLanguageTagWhenLanguageTagIsGrandfathered_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // act
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag);
            var testResult = (string)languageTag;

            // assert
            testResult.Should().Be(testLanguageTag, "'{0}' is a grandfathered language tag (RFC 5646 §2.1, §2.2.8)", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenLanguageTagIsValid

        [Theory]
        [MemberData(nameof(Create_WithLanguageTagWhenLanguageTagIsValid_TestCaseGenerator.TestCases), MemberType = typeof(Create_WithLanguageTagWhenLanguageTagIsValid_TestCaseGenerator))]
        public void Create_WithLanguageTagWhenLanguageTagIsValid_ReturnsBcp47LanguageTag(string testLanguageTag, string expectedResult)
        {
            // act
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag);
            var testResult = (string)languageTag;

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
            Action testAction = () => _ = Bcp47LanguageTag.Create(null, Regex.InfiniteMatchTimeout);

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

        #region Test: Create_WithLanguageTagAndTimeoutWhenLanguageTagHasDuplicateSubtags

        [Theory]
        [MemberData(nameof(DuplicateSubtagLanguageTags))]
        public void Create_WithLanguageTagAndTimeoutWhenLanguageTagHasDuplicateSubtags_ThrowsLanguageTagFormatException(string testLanguageTag, string controlLanguageTag)
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, Regex.InfiniteMatchTimeout);
            Action controlAction = () => _ = Bcp47LanguageTag.Create(controlLanguageTag, Regex.InfiniteMatchTimeout);

            // assert
            controlAction.Should().NotThrow("the language tag '{0}' has no repeated subtags", controlLanguageTag);
            testAction.Should().Throw<LanguageTagFormatException>("a variant subtag or extension singleton is repeated (RFC 5646 §2.2.5, §2.2.6)").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndTimeoutWhenLanguageTagIsGrandfathered

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void Create_WithLanguageTagAndTimeoutWhenLanguageTagIsGrandfathered_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // act
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag, Regex.InfiniteMatchTimeout);
            var testResult = (string)languageTag;

            // assert
            testResult.Should().Be(testLanguageTag, "'{0}' is a grandfathered language tag (RFC 5646 §2.1, §2.2.8)", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndTimeoutWhenLanguageTagIsValid

        [Theory]
        [MemberData(nameof(Create_WithLanguageTagAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator.TestCases), MemberType = typeof(Create_WithLanguageTagAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator))]
        public void Create_WithLanguageTagAndTimeoutWhenLanguageTagIsValid_ReturnsBcp47LanguageTag(string testLanguageTag, TimeSpan testTimeout, string expectedResult)
        {
            // act
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag, testTimeout);
            var testResult = (string)languageTag;

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
            Action testAction = () => _ = Bcp47LanguageTag.Create(null, _fixture.IanaSubtagRegistry);

            // assert
            testAction.Should().Throw<ArgumentNullException>("the language tag must not be null").WithParameterName("languageTag");
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenSubtagRegistryIsNull

        [Fact]
        public void Create_WithLanguageTagAndSubtagRegistryWhenSubtagRegistryIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create("en", null);

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

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasDuplicateSubtags

        [Theory]
        [MemberData(nameof(DuplicateSubtagLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasDuplicateSubtags_ThrowsLanguageTagFormatException(string testLanguageTag, string controlLanguageTag)
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry);
            Action controlAction = () => _ = Bcp47LanguageTag.Create(controlLanguageTag, _fixture.IanaSubtagRegistry);

            // assert
            controlAction.Should().NotThrow("the language tag '{0}' has no repeated subtags", controlLanguageTag);
            testAction.Should().Throw<LanguageTagFormatException>("a variant subtag or extension singleton is repeated (RFC 5646 §2.2.5, §2.2.6)").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenSubtagIsInPrivateUseRange

        [Theory]
        [MemberData(nameof(PrivateUseRangeLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenSubtagIsInPrivateUseRange_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // act
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry);
            var testResult = (string)languageTag;

            // assert
            testResult.Should().Be(testLanguageTag, "the subtags of '{0}' are registered by private use range records (RFC 5646 §3.1.1)", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenSubtagIsOutsidePrivateUseRange

        [Theory]
        [MemberData(nameof(OutsidePrivateUseRangeLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenSubtagIsOutsidePrivateUseRange_ThrowsLanguageTagFormatException(string testLanguageTag, string controlLanguageTag)
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry);
            Action controlAction = () => _ = Bcp47LanguageTag.Create(controlLanguageTag, _fixture.IanaSubtagRegistry);

            // assert
            controlAction.Should().NotThrow("the language tag '{0}' ends a private use range record", controlLanguageTag);
            testAction.Should().Throw<LanguageTagFormatException>("the Subtag Registry has no record or range for a subtag of '{0}'", testLanguageTag).WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsGrandfathered

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsGrandfathered_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // act
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry);
            var testResult = (string)languageTag;

            // assert
            testResult.Should().Be(testLanguageTag, "'{0}' is a grandfathered language tag (RFC 5646 §2.1, §2.2.8)", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid

        [Theory]
        [MemberData(nameof(Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_TestCaseGenerator.TestCases), MemberType = typeof(Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_TestCaseGenerator))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_ReturnsBcp47LanguageTag(string testLanguageTag, string expectedResult)
        {
            // act
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry);
            var testResult = (string)languageTag;

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
            Action testAction = () => _ = Bcp47LanguageTag.Create(null, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout);

            // assert
            testAction.Should().Throw<ArgumentNullException>("the language tag must not be null").WithParameterName("languageTag");
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagRegistryIsNull

        [Fact]
        public void Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagRegistryIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create("en", null, Regex.InfiniteMatchTimeout);

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

        #region Test: Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagHasDuplicateSubtags

        [Theory]
        [MemberData(nameof(DuplicateSubtagLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagHasDuplicateSubtags_ThrowsLanguageTagFormatException(string testLanguageTag, string controlLanguageTag)
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout);
            Action controlAction = () => _ = Bcp47LanguageTag.Create(controlLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout);

            // assert
            controlAction.Should().NotThrow("the language tag '{0}' has no repeated subtags", controlLanguageTag);
            testAction.Should().Throw<LanguageTagFormatException>("a variant subtag or extension singleton is repeated (RFC 5646 §2.2.5, §2.2.6)").WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagIsInPrivateUseRange

        [Theory]
        [MemberData(nameof(PrivateUseRangeLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagIsInPrivateUseRange_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // act
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout);
            var testResult = (string)languageTag;

            // assert
            testResult.Should().Be(testLanguageTag, "the subtags of '{0}' are registered by private use range records (RFC 5646 §3.1.1)", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagIsOutsidePrivateUseRange

        [Theory]
        [MemberData(nameof(OutsidePrivateUseRangeLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagIsOutsidePrivateUseRange_ThrowsLanguageTagFormatException(string testLanguageTag, string controlLanguageTag)
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout);
            Action controlAction = () => _ = Bcp47LanguageTag.Create(controlLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout);

            // assert
            controlAction.Should().NotThrow("the language tag '{0}' ends a private use range record", controlLanguageTag);
            testAction.Should().Throw<LanguageTagFormatException>("the Subtag Registry has no record or range for a subtag of '{0}'", testLanguageTag).WithMessage(Bcp47LanguageTag.ErrorMessage.LanguageTagInvalidFormat);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsGrandfathered

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsGrandfathered_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // act
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout);
            var testResult = (string)languageTag;

            // assert
            testResult.Should().Be(testLanguageTag, "'{0}' is a grandfathered language tag (RFC 5646 §2.1, §2.2.8)", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid

        [Theory]
        [MemberData(nameof(Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator.TestCases), MemberType = typeof(Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid_TestCaseGenerator))]
        public void Create_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsValid_ReturnsBcp47LanguageTag(string testLanguageTag, TimeSpan testTimeout, string expectedResult)
        {
            // arrange
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry, testTimeout);
            var testResult = (string)languageTag;

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

        #region Test: GetHashCode_WhenTagsDifferOnlyInCase

        [Theory]
        [InlineData("en-US", "en-us"), InlineData("zh-Hant-TW", "ZH-HANT-TW"), InlineData("x-fr-CH", "X-FR-ch")]
        public void GetHashCode_WhenTagsDifferOnlyInCase_ReturnsEqualHashCodes(string firstLanguageTag, string secondLanguageTag)
        {
            // arrange
            var first = Bcp47LanguageTag.Create(firstLanguageTag);
            var second = Bcp47LanguageTag.Create(secondLanguageTag);

            // act
            var testResult = second.GetHashCode();

            // assert
            first.Should().Be(second, "language tags are compared case-insensitively");
            testResult.Should().Be(first.GetHashCode(), "'{0}' and '{1}' are equal, so their hash codes must be equal", firstLanguageTag, secondLanguageTag);
        }

        #endregion

        #region Test: GetHashCode_WhenTagsDifferOnlyInCaseInHashSet

        [Fact]
        public void GetHashCode_WhenTagsDifferOnlyInCaseInHashSet_DeduplicatesTags()
        {
            // arrange
            var testSet = new HashSet<Bcp47LanguageTag>();

            // act
            testSet.Add(Bcp47LanguageTag.Create("en-US"));
            var testResult = testSet.Add(Bcp47LanguageTag.Create("en-us"));

            // assert
            testResult.Should().BeFalse("'en-us' is equal to 'en-US', which the set already holds");
            testSet.Should().ContainSingle();
            testSet.Contains(Bcp47LanguageTag.Create("EN-us")).Should().BeTrue("'EN-us' is equal to 'en-US'");
        }

        #endregion

        #region Test: GetHashCode_WhenUninitialised

        [Fact]
        public void GetHashCode_WhenUninitialised_ReturnsZero()
        {
            // act
            var testResult = default(Bcp47LanguageTag).GetHashCode();

            // assert
            testResult.Should().Be(0, "an uninitialised language tag has no value to hash");
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

        #region Test: TryCreate_WithLanguageTagWhenLanguageTagHasDuplicateSubtags

        [Theory]
        [MemberData(nameof(DuplicateSubtagLanguageTags))]
        public void TryCreate_WithLanguageTagWhenLanguageTagHasDuplicateSubtags_ReturnsFalse(string testLanguageTag, string controlLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, out _);
            var controlResult = Bcp47LanguageTag.TryCreate(controlLanguageTag, out _);

            // assert
            controlResult.Should().BeTrue("the language tag '{0}' has no repeated subtags", controlLanguageTag);
            testResult.Should().BeFalse("the language tag '{0}' repeats a variant subtag or extension singleton (RFC 5646 §2.2.5, §2.2.6)", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagWhenLanguageTagIsGrandfathered

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void TryCreate_WithLanguageTagWhenLanguageTagIsGrandfathered_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, out var languageTag);

            // assert
            testResult.Should().BeTrue("'{0}' is a grandfathered language tag (RFC 5646 §2.1, §2.2.8)", testLanguageTag);
            ((string)languageTag).Should().Be(testLanguageTag);
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
            testResult.Should().BeTrue("language tag '{0}' is correctly formatted", (string)languageTag);
            ((string)languageTag).Should().Be(expectedResult, "language tag '{0}' is correctly formatted", (string)languageTag);
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

        #region Test: TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagHasDuplicateSubtags

        [Theory]
        [MemberData(nameof(DuplicateSubtagLanguageTags))]
        public void TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagHasDuplicateSubtags_ReturnsFalse(string testLanguageTag, string controlLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, Regex.InfiniteMatchTimeout, out _);
            var controlResult = Bcp47LanguageTag.TryCreate(controlLanguageTag, Regex.InfiniteMatchTimeout, out _);

            // assert
            controlResult.Should().BeTrue("the language tag '{0}' has no repeated subtags", controlLanguageTag);
            testResult.Should().BeFalse("the language tag '{0}' repeats a variant subtag or extension singleton (RFC 5646 §2.2.5, §2.2.6)", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagIsGrandfathered

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void TryCreate_WithLanguageTagAndTimeoutWhenLanguageTagIsGrandfathered_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, Regex.InfiniteMatchTimeout, out var languageTag);

            // assert
            testResult.Should().BeTrue("'{0}' is a grandfathered language tag (RFC 5646 §2.1, §2.2.8)", testLanguageTag);
            ((string)languageTag).Should().Be(testLanguageTag);
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
            ((string)languageTag).Should().Be(expectedResult, "the language tag '{0}' is correctly formatted", testLanguageTag);
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

        #region Test: TryCreate_WithLanguageTagAndTimeoutWhenMatchWouldTimeOut

        [Fact]
        public void TryCreate_WithLanguageTagAndTimeoutWhenMatchWouldTimeOut_ReturnsTrue()
        {
            // act
            Bcp47LanguageTag languageTag = default;
            var testResult = false;
            Action testAction = () => testResult = Bcp47LanguageTag.TryCreate(TimeoutLanguageTag, TimeSpan.FromTicks(1), out languageTag);

            // assert
            testAction.Should().NotThrow("a Try method reports failure through its return value");
            testResult.Should().BeTrue("the default rules are checked by a parser, which cannot time out, and the language tag is well-formed");
            ((string)languageTag).Should().Be(TimeoutLanguageTag);
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
            var testResult = Bcp47LanguageTag.TryCreate("en", null, out _);

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

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasDuplicateSubtags

        [Theory]
        [MemberData(nameof(DuplicateSubtagLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasDuplicateSubtags_ReturnsFalse(string testLanguageTag, string controlLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, out _);
            var controlResult = Bcp47LanguageTag.TryCreate(controlLanguageTag, _fixture.IanaSubtagRegistry, out _);

            // assert
            controlResult.Should().BeTrue("the language tag '{0}' has no repeated subtags", controlLanguageTag);
            testResult.Should().BeFalse("the language tag '{0}' repeats a variant subtag or extension singleton (RFC 5646 §2.2.5, §2.2.6)", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenSubtagIsInPrivateUseRange

        [Theory]
        [MemberData(nameof(PrivateUseRangeLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenSubtagIsInPrivateUseRange_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, out var languageTag);

            // assert
            testResult.Should().BeTrue("the subtags of '{0}' are registered by private use range records (RFC 5646 §3.1.1)", testLanguageTag);
            ((string)languageTag).Should().Be(testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenSubtagIsOutsidePrivateUseRange

        [Theory]
        [MemberData(nameof(OutsidePrivateUseRangeLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenSubtagIsOutsidePrivateUseRange_ReturnsFalse(string testLanguageTag, string controlLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, out _);
            var controlResult = Bcp47LanguageTag.TryCreate(controlLanguageTag, _fixture.IanaSubtagRegistry, out _);

            // assert
            controlResult.Should().BeTrue("the language tag '{0}' ends a private use range record", controlLanguageTag);
            testResult.Should().BeFalse("the Subtag Registry has no record or range for a subtag of '{0}'", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsGrandfathered

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsGrandfathered_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, out var languageTag);

            // assert
            testResult.Should().BeTrue("'{0}' is a grandfathered language tag (RFC 5646 §2.1, §2.2.8)", testLanguageTag);
            ((string)languageTag).Should().Be(testLanguageTag);
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
            ((string)languageTag).Should().Be(expectedResult, "the language tag '{0}' is correctly formatted", testLanguageTag);
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
            var testResult = Bcp47LanguageTag.TryCreate("en", null, Regex.InfiniteMatchTimeout, out _);

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

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagHasDuplicateSubtags

        [Theory]
        [MemberData(nameof(DuplicateSubtagLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagHasDuplicateSubtags_ReturnsFalse(string testLanguageTag, string controlLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout, out _);
            var controlResult = Bcp47LanguageTag.TryCreate(controlLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout, out _);

            // assert
            controlResult.Should().BeTrue("the language tag '{0}' has no repeated subtags", controlLanguageTag);
            testResult.Should().BeFalse("the language tag '{0}' repeats a variant subtag or extension singleton (RFC 5646 §2.2.5, §2.2.6)", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagIsInPrivateUseRange

        [Theory]
        [MemberData(nameof(PrivateUseRangeLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagIsInPrivateUseRange_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout, out var languageTag);

            // assert
            testResult.Should().BeTrue("the subtags of '{0}' are registered by private use range records (RFC 5646 §3.1.1)", testLanguageTag);
            ((string)languageTag).Should().Be(testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagIsOutsidePrivateUseRange

        [Theory]
        [MemberData(nameof(OutsidePrivateUseRangeLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenSubtagIsOutsidePrivateUseRange_ReturnsFalse(string testLanguageTag, string controlLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout, out _);
            var controlResult = Bcp47LanguageTag.TryCreate(controlLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout, out _);

            // assert
            controlResult.Should().BeTrue("the language tag '{0}' ends a private use range record", controlLanguageTag);
            testResult.Should().BeFalse("the Subtag Registry has no record or range for a subtag of '{0}'", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsGrandfathered

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenLanguageTagIsGrandfathered_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout, out var languageTag);

            // assert
            testResult.Should().BeTrue("'{0}' is a grandfathered language tag (RFC 5646 §2.1, §2.2.8)", testLanguageTag);
            ((string)languageTag).Should().Be(testLanguageTag);
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
            ((string)languageTag).Should().Be(expectedResult, "the language tag '{0}' is correctly formatted", testLanguageTag);
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

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenMatchTimesOut

        [Fact]
        public void TryCreate_WithLanguageTagAndSubtagRegistryAndTimeoutWhenMatchTimesOut_ReturnsFalse()
        {
            // act
            Bcp47LanguageTag languageTag = default;
            var testResult = true;
            Action testAction = () => testResult = Bcp47LanguageTag.TryCreate(TimeoutLanguageTag, _fixture.IanaSubtagRegistry, TimeSpan.FromTicks(1), out languageTag);

            // assert
            testAction.Should().NotThrow("a Try method reports failure through its return value");
            testResult.Should().BeFalse("matching the language tag took longer than the timeout");
            languageTag.Should().Be(default(Bcp47LanguageTag));
        }

        #endregion

        #region Test: CreateAndTryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValidInRegistry

        [Theory]
        [InlineData("i-klingon"), InlineData("my-BU")]
        public void CreateAndTryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValidInRegistry_AllOverloadsSucceed(string testLanguageTag)
        {
            // act
            var createResult = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry);
            var createWithTimeoutResult = Bcp47LanguageTag.Create(testLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout);
            var tryCreateResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, out var tryCreateLanguageTag);
            var tryCreateWithTimeoutResult = Bcp47LanguageTag.TryCreate(testLanguageTag, _fixture.IanaSubtagRegistry, Regex.InfiniteMatchTimeout, out var tryCreateWithTimeoutLanguageTag);

            // assert
            ((string)createResult).Should().Be(testLanguageTag);
            ((string)createWithTimeoutResult).Should().Be(testLanguageTag);
            tryCreateResult.Should().BeTrue("the Subtag Registry has a record for every subtag of '{0}'", testLanguageTag);
            ((string)tryCreateLanguageTag).Should().Be(testLanguageTag);
            tryCreateWithTimeoutResult.Should().BeTrue("the registry-based overloads must agree on '{0}'", testLanguageTag);
            ((string)tryCreateWithTimeoutLanguageTag).Should().Be(testLanguageTag);
        }

        #endregion
    }
}
