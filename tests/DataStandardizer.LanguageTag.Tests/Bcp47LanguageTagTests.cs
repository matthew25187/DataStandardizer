using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using DataStandardizer.Core;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Bcp47LanguageTagTests
    {
        #region Declarations

        /// <summary>
        /// The empty string, which is not a subtag or a language tag of any kind (RFC 5646 §2.1).
        /// </summary>
        private const string EmptyString = "";

        /// <summary>
        /// English: the ISO 639-1 code as a Primary Language subtag (RFC 5646 §2.2.1).
        /// </summary>
        private const string EnglishLanguageTag = "en";

        /// <summary>
        /// Italian: the ISO 639-1 code as a Primary Language subtag (RFC 5646 §2.2.1).
        /// </summary>
        private const string ItalianLanguageTag = "it";

        /// <summary>
        /// Spanish as used in Latin America and the Caribbean, an example from RFC 5646 Appendix A.
        /// </summary>
        private const string LatinAmericanSpanish = "es-419";

        /// <summary>
        /// Serbian written in Cyrillic script (RFC 5646 §2.2.3).
        /// </summary>
        private const string SerbianCyrillic = "sr-Cyrl";

        /// <summary>
        /// Min Nan Chinese written in traditional Han characters, as spoken in Taiwan.
        /// </summary>
        private const string MinNanTraditionalTaiwan = "nan-Hant-TW";

        /// <summary>
        /// Cantonese written in traditional Han characters, as spoken in Hong Kong.
        /// </summary>
        private const string CantoneseTraditionalHongKong = "yue-Hant-HK";

        /// <summary>
        /// Romansh in its Sursilvan variety (RFC 5646 §2.2.5).
        /// </summary>
        private const string RomanshSursilvan = "rm-sursilv";

        /// <summary>
        /// Swiss German as used in the canton of Zurich, with a Unicode locale extension (RFC 5646 §2.2.6; RFC 6067).
        /// </summary>
        private const string SwissGermanZurich = "gsw-u-sd-chzh";

        /// <summary>
        /// A language tag followed by a line feed, which is not part of any subtag (RFC 5646 §2.1).
        /// </summary>
        private const string EnglishWithLineFeed = "en\n";

        /// <summary>
        /// A language tag followed by a line feed, which is not part of any subtag (RFC 5646 §2.1).
        /// </summary>
        private const string EnglishUnitedStatesWithLineFeed = "en-US\n";

        /// <summary>
        /// A private use tag followed by a line feed, which is not part of any subtag (RFC 5646 §2.1).
        /// </summary>
        private const string PrivateUseWithLineFeed = "x-private\n";

        /// <summary>
        /// The ISO 639-1 code ko with the Kelvin sign U+212A for k: a case equivalent of an ASCII letter that is not one (RFC 5646 §2.1).
        /// </summary>
        private const string KelvinSignO = "\x212Ao";

        /// <summary>
        /// The ISO 639-1 code sv with the long s U+017F for s: a case equivalent of an ASCII letter that is not one (RFC 5646 §2.1).
        /// </summary>
        private const string LongSV = "\x017Fv";

        /// <summary>
        /// A Private Use subtag that is only the Kelvin sign U+212A, which is not an ASCII letter (RFC 5646 §2.1).
        /// </summary>
        private const string EnglishPrivateUseKelvinSign = "en-x-\x212A";

        /// <summary>
        /// A Private Use subtag that is only the long s U+017F, which is not an ASCII letter (RFC 5646 §2.1).
        /// </summary>
        private const string EnglishPrivateUseLongS = "en-x-\x017F";

        /// <summary>
        /// The Region subtag KR with the Kelvin sign U+212A for K, which is not an ASCII letter (RFC 5646 §2.1).
        /// </summary>
        private const string EnglishKelvinSignRegion = "en-\x212AR";

        /// <summary>
        /// A Primary Language subtag of five letters, which is well-formed (RFC 5646 §2.2.1¶5) but is not a code of any part of ISO 639.
        /// </summary>
        private const string UnlistedPrimaryLanguageSubtag = "zzzzz";

        /// <summary>
        /// A string that is not a language tag: it is one subtag of more than eight letters (RFC 5646 §2.1).
        /// </summary>
        private const string NotALanguageTag = "NotAValidLanguageTag";

        /// <summary>
        /// A tag with an empty subtag between two others, which no production of RFC 5646 §2.1 allows.
        /// </summary>
        private const string EnglishWithEmptySubtag = "en--US";

        /// <summary>
        /// A tag with the first Script subtag reserved for private use (RFC 5646 §2.2.3¶3).
        /// </summary>
        private const string EnglishFirstPrivateUseScript = "en-Qaaa";

        /// <summary>
        /// A tag with the last Script subtag of the first run reserved for private use (RFC 5646 §2.2.3¶3).
        /// </summary>
        private const string EnglishPrivateUseScriptQaaz = "en-Qaaz";

        /// <summary>
        /// A tag with the first Script subtag of the second run reserved for private use (RFC 5646 §2.2.3¶3).
        /// </summary>
        private const string EnglishPrivateUseScriptQaba = "en-Qaba";

        /// <summary>
        /// A tag with the last Script subtag reserved for private use (RFC 5646 §2.2.3¶3).
        /// </summary>
        private const string EnglishLastPrivateUseScript = "en-Qabx";

        /// <summary>
        /// A tag with the first Region subtag of the QM..QZ range reserved for private use (RFC 5646 §2.2.4¶3).
        /// </summary>
        private const string EnglishFirstPrivateUseRegionQ = "en-QM";

        /// <summary>
        /// A private use sequence of one digit (RFC 5646 §2.1).
        /// </summary>
        private const string PrivateUseOneDigit = "x-1";

        /// <summary>
        /// A private use sequence of one letter (RFC 5646 §2.1).
        /// </summary>
        private const string PrivateUseOneLetter = "x-a";

        /// <summary>
        /// A private use sequence of nine digits, one more than RFC 5646 §2.1 allows.
        /// </summary>
        private const string PrivateUseNineDigits = "x-123456789";

        /// <summary>
        /// A private use sequence of nine letters, one more than RFC 5646 §2.1 allows.
        /// </summary>
        private const string PrivateUseNineLetters = "x-abcdefghi";

        /// <summary>
        /// One letter, which is a singleton and not a subtag of any other kind (RFC 5646 §2.1).
        /// </summary>
        private const string OneLetter = "a";

        /// <summary>
        /// One digit, which is a singleton and not a subtag of any other kind (RFC 5646 §2.1).
        /// </summary>
        private const string OneDigit = "1";

        /// <summary>
        /// Two letters.
        /// </summary>
        private const string TwoLetters = "ab";

        /// <summary>
        /// Three digits.
        /// </summary>
        private const string ThreeDigits = "123";

        /// <summary>
        /// Four letters, which is a reserved Primary Language subtag (RFC 5646 §2.2.1¶4).
        /// </summary>
        private const string FourLetters = "abcd";

        /// <summary>
        /// An Extended Language subtag (RFC 5646 §2.2.2).
        /// </summary>
        private const string FirstExtendedLanguageSubtag = "aaa";

        /// <summary>
        /// An Extended Language subtag (RFC 5646 §2.2.2).
        /// </summary>
        private const string SecondExtendedLanguageSubtag = "bbb";

        /// <summary>
        /// An Extended Language subtag (RFC 5646 §2.2.2).
        /// </summary>
        private const string ThirdExtendedLanguageSubtag = "ccc";

        /// <summary>
        /// Cantonese, as an Extended Language subtag of Chinese (RFC 5646 §2.2.2).
        /// </summary>
        private const string CantoneseExtendedLanguageSubtag = "yue";

        /// <summary>
        /// Mandarin, as an Extended Language subtag of Chinese (RFC 5646 §2.2.2).
        /// </summary>
        private const string MandarinExtendedLanguageSubtag = "cmn";

        /// <summary>
        /// The variant subtag for the Resian dialect of Slovenian (RFC 5646 §2.2.5, Appendix A).
        /// </summary>
        private const string RozajVariantSubtag = "rozaj";

        /// <summary>
        /// The variant subtag for the San Giorgio / Bila dialect of Resian (RFC 5646 §2.2.5, Appendix A).
        /// </summary>
        private const string BiskeVariantSubtag = "biske";

        /// <summary>
        /// The variant subtag for the standardized Resian orthography of 1994 (RFC 5646 §2.2.5).
        /// </summary>
        private const string NineteenNinetyFourVariantSubtag = "1994";

        /// <summary>
        /// The variant subtag for the traditional German orthography (RFC 5646 §2.2.5, Appendix A).
        /// </summary>
        private const string NineteenOhOneVariantSubtag = "1901";

        /// <summary>
        /// An extension with the singleton t (RFC 5646 §2.2.6).
        /// </summary>
        private const string TransformedExtension = "t-ca-hebrew";

        /// <summary>
        /// The tag zh-Hant-TW in upper case, which is the same tag (RFC 5646 §2.1.1).
        /// </summary>
        private const string TraditionalChineseTaiwanUpperCase = "ZH-HANT-TW";

        /// <summary>
        /// The tag x-fr-CH in other cases, which is the same tag (RFC 5646 §2.1.1).
        /// </summary>
        private const string PrivateUseFrenchSwitzerlandMixedCase = "X-FR-ch";

        /// <summary>
        /// Burmese as used in Burma, with a Region subtag that the IANA registry has deprecated but still holds (RFC 5646 §3.1.6).
        /// </summary>
        private const string BurmeseBurma = "my-BU";

        /// <summary>
        /// A tag that ends with an empty subtag, which no production of RFC 5646 §2.1 allows.
        /// </summary>
        private const string EnglishWithTrailingHyphen = "en-";

        /// <summary>
        /// A tag that ends with an empty subtag, which no production of RFC 5646 §2.1 allows.
        /// </summary>
        private const string EnglishUnitedStatesWithTrailingHyphen = "en-US-";

        /// <summary>
        /// A Private Use subtag (RFC 5646 §2.2.7), to follow another subtag.
        /// </summary>
        private const string PrivateUseSuffix = "-x-test";

        /// <summary>
        /// A Script and a Region subtag (RFC 5646 §2.2.3, §2.2.4), to follow a Primary Language subtag.
        /// </summary>
        private const string LatinUnitedStatesSuffix = "-Latn-US";

        /// <summary>
        /// Catalan in its Valencian variety (RFC 5646 §2.2.5).
        /// </summary>
        private const string CatalanValencian = "ca-valencia";

        /// <summary>
        /// A tag with two extensions, each with its own singleton (RFC 5646 §2.2.6).
        /// </summary>
        private const string EnglishWithTwoExtensions = "en-a-bbb-b-ccc";

        /// <summary>
        /// A tag with the Region subtag 830 (Channel Islands), which may be registered but is not (RFC 5646 §2.2.4¶4E-F).
        /// </summary>
        private const string EnglishChannelIslands = "en-830";

        /// <summary>
        /// A tag with the Region subtag 830 (Channel Islands) after a Script subtag (RFC 5646 §2.2.4¶4E-F).
        /// </summary>
        private const string FrenchLatinChannelIslands = "fr-Latn-830";

        /// <summary>
        /// A tag with the Region subtag 830 (Channel Islands) before a Private Use subtag (RFC 5646 §2.2.4¶4E-F).
        /// </summary>
        private const string EnglishChannelIslandsPrivateUse = "en-830-x-foo";

        /// <summary>
        /// The UN M49 code for the Channel Islands, which RFC 5646 §2.2.4¶4E allows to be registered as a Region subtag.
        /// </summary>
        private const string ChannelIslandsRegionSubtag = "830";

        /// <summary>
        /// A Unicode locale extension with the calendar key (RFC 5646 §2.2.6; RFC 6067).
        /// </summary>
        private const string HebrewCalendarExtension = "u-ca-hebrew";

        /// <summary>
        /// Hebrew as used in Israel, with the Hebrew calendar (RFC 5646 §2.2.6; RFC 6067).
        /// </summary>
        private const string HebrewIsraelHebrewCalendar = "he-IL-u-ca-hebrew";

        /// <summary>
        /// Cantonese, as an Extended Language subtag of Chinese, as used in Hong Kong, an example from RFC 5646 Appendix A.
        /// </summary>
        private const string CantoneseHongKong = "zh-yue-HK";

        /// <summary>
        /// Resian, San Giorgio / Bila dialect, in the orthography of 1994: three variant subtags (RFC 5646 §2.2.5, Appendix A).
        /// </summary>
        private const string ResianSanGiorgio1994 = "sl-rozaj-biske-1994";

        /// <summary>
        /// A tag that is only a Private Use subtag (RFC 5646 §2.2.7).
        /// </summary>
        private const string PrivateUseOnly = "x-private";

        /// <summary>
        /// A tag that is only a Private Use subtag (RFC 5646 §2.2.7), whose sequences look like a language and a region.
        /// </summary>
        private const string PrivateUseFrenchSwitzerland = "x-fr-CH";

        /// <summary>
        /// A tag that is only a Private Use subtag (RFC 5646 §2.2.7), in which x appears again as a sequence.
        /// </summary>
        private const string PrivateUseFrenchSwitzerlandWithSecondSequence = "x-fr-CH-x-valid-private-use-subtag";

        /// <summary>
        /// The irregular grandfathered tag for Klingon (RFC 5646 §2.1, §2.2.8).
        /// </summary>
        private const string KlingonGrandfathered = "i-klingon";

        /// <summary>
        /// The regular grandfathered tag for Lojban (RFC 5646 §2.1, §2.2.8).
        /// </summary>
        private const string LojbanGrandfathered = "art-lojban";

        /// <summary>
        /// Chinese written in traditional script as used in Taiwan (RFC 5646 §2.2.3, §2.2.4).
        /// </summary>
        private const string TraditionalChineseTaiwan = "zh-Hant-TW";

        /// <summary>
        /// English as used in the United States, an example from RFC 5646 Appendix A.
        /// </summary>
        private const string EnglishUnitedStates = "en-US";

        /// <summary>
        /// The tag en-US in lower case, which is the same tag (RFC 5646 §2.1.1).
        /// </summary>
        private const string EnglishUnitedStatesLowerCase = "en-us";

        /// <summary>
        /// The tag it in upper case, which is the same tag (RFC 5646 §2.1.1); under the tr-TR culture, I does not lower-case to i.
        /// </summary>
        private const string ItalianUpperCase = "IT";

        /// <summary>
        /// Italian as used in Switzerland.
        /// </summary>
        private const string ItalianSwitzerland = "it-CH";

        /// <summary>
        /// The tag it-CH in other cases, which is the same tag (RFC 5646 §2.1.1).
        /// </summary>
        private const string ItalianSwitzerlandMixedCase = "IT-ch";

        /// <summary>
        /// The name of the Turkish culture, whose case mapping of I and i differs from the invariant one.
        /// </summary>
        private const string TurkishCultureName = "tr-TR";

        /// <summary>
        /// A string that is not a language tag: one subtag of seven letters, which is not a subtag of any kind on its own (RFC 5646 §2.1).
        /// </summary>
        private const string InvalidLanguageTag = "invalid";

        /// <summary>
        /// The name of the parameter that takes a language tag.
        /// </summary>
        private const string LanguageTagParameterName = "languageTag";

        /// <summary>
        /// The message of the exception thrown for a string that is not a language tag.
        /// </summary>
        private const string InvalidFormatMessage = "The language tag is not in a valid format.";

        private static readonly char[] TestCharacters;
        private static readonly char[] TestDigits;
        private static readonly char[] TestLetters;
        private static readonly string[] _validLanguageTags;

        #endregion

        static Bcp47LanguageTagTests()
        {
            // The ASCII digits and lower-case letters, from which subtags are made (RFC 5646 §2.1).
            TestDigits = "0123456789".ToCharArray();
            TestLetters = "abcdefghijklmnopqrstuvwxyz".ToCharArray();
            TestCharacters = TestDigits.Concat(TestLetters).ToArray();

            _validLanguageTags = new[]
            {
                "af", "am", "ar", "arn", "ary", "as", "az", "ba", "be", "bg", "bn", "bo", "br", "bs", "ca", "ckb", "co", "cs", "cy", "da", "de", "dsb", "dv", "el", EnglishLanguageTag, "es", "et", "eu", "fa", "fi", "fil", "fo", "fr", "fy", "ga", "gd", "gil", "gl",
                "gsw", "gu", "ha", "he", "hi", "hr", "hsb", "hu", "hy", "id", "ig", "ii", "is", ItalianLanguageTag, "iu", "ja", "ka", "kk", "kl", "km", "kn", "ko", "kok", "ku", "ky", "lb", "lo", "lt", "lv", "mi", "mk", "ml", "mn", "moh", "mr", "ms", "mt", "my", "nb",
                "ne", "nl", "nn", "no", "oc", "or", "pap", "pa", "pl", "prs", "ps", "pt", "quc", "qu", "rm", "ro", "ru", "rw", "sa", "sah", "se", "si", "sk", "sl", "sma", "smj", "smn", "sms", "sq", "sr", "st", "sv", "sw", "syc", "ta", "te", "tg", "th",
                "tk", "tn", "tr", "tt", "tzm", "ug", "uk", "ur", "uz", "vi", "wo", "xh", "yo", "zh", "zu", LatinAmericanSpanish, RomanshSursilvan, SerbianCyrillic, MinNanTraditionalTaiwan, CantoneseTraditionalHongKong, SwissGermanZurich
            };
        }

        /// <summary>
        /// Each way to create a language tag from a string: by the default rules, and against the IANA registry fixture.
        /// </summary>
        private static readonly Dictionary<string, Func<string, Bcp47LanguageTag>> LanguageTagFactories = new Dictionary<string, Func<string, Bcp47LanguageTag>>
        {
            { "Create(string)", languageTag => Bcp47LanguageTag.Create(languageTag) },
            { "Create(string, SubtagRegistry)", languageTag => Bcp47LanguageTag.Create(languageTag, IanaSubtagRegistryResource.Shared) }
        };

        /// <summary>
        /// Each language tag, created in each way that <see cref="LanguageTagFactories"/> has.
        /// </summary>
        private static TheoryData<string, Bcp47LanguageTag> CreateLanguageTagCases(IEnumerable<string> languageTags)
        {
            var data = new TheoryData<string, Bcp47LanguageTag>();
            foreach (var factory in LanguageTagFactories)
            foreach (var languageTag in languageTags)
            {
                data.Add(factory.Key + ": " + languageTag, factory.Value(languageTag));
            }

            return data;
        }

        private static TheoryData<string> FirstOfEach(IEnumerable<(string LanguageTag, string ControlLanguageTag)> pairs)
        {
            var data = new TheoryData<string>();
            foreach (var pair in pairs)
            {
                data.Add(pair.LanguageTag);
            }

            return data;
        }

        private static TheoryData<string> ControlOfEach(params IEnumerable<(string LanguageTag, string ControlLanguageTag)>[] pairLists)
        {
            var data = new TheoryData<string>();
            foreach (var pairs in pairLists)
            foreach (var pair in pairs)
            {
                data.Add(pair.ControlLanguageTag);
            }

            return data;
        }

        /// <summary>
        /// Subtag checks, each paired with a subtag it accepts followed by a line feed. A trailing line feed isn't part of any subtag
        /// (RFC 5646 §2.1), so every check must reject it.
        /// </summary>
        public static TheoryData<string, Func<string, bool>, string> TrailingNewlineSubtags => new TheoryData<string, Func<string, bool>, string>()
        {
            { nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), Bcp47LanguageTag.CheckPrimaryLanguageSubtag, EnglishWithLineFeed },
            { nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), Bcp47LanguageTag.CheckExtendedLanguageSubtag, "yue\n" },
            { nameof(Bcp47LanguageTag.CheckScriptSubtag), Bcp47LanguageTag.CheckScriptSubtag, "Latn\n" },
            { nameof(Bcp47LanguageTag.CheckRegionSubtag), Bcp47LanguageTag.CheckRegionSubtag, "US\n" },
            { nameof(Bcp47LanguageTag.CheckVariantSubtag), Bcp47LanguageTag.CheckVariantSubtag, "sursilv\n" },
            { nameof(Bcp47LanguageTag.CheckExtensionSubtag), Bcp47LanguageTag.CheckExtensionSubtag, "u-sd-chzh\n" },
            { nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), Bcp47LanguageTag.CheckPrivateUseSubtag, PrivateUseWithLineFeed }
        };

        /// <summary>
        /// Subtag checks, each paired with a subtag it accepts with one ASCII letter replaced by a non-ASCII case equivalent: the
        /// Kelvin sign U+212A for K or the long s U+017F for S. Subtags have only ASCII letters and digits (RFC 5646 §2.1), so every
        /// check must reject it.
        /// </summary>
        public static TheoryData<string, Func<string, bool>, string> NonAsciiCaseEquivalentSubtags => new TheoryData<string, Func<string, bool>, string>()
        {
            { nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), Bcp47LanguageTag.CheckPrimaryLanguageSubtag, KelvinSignO },
            { nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), Bcp47LanguageTag.CheckPrimaryLanguageSubtag, LongSV },
            { nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), Bcp47LanguageTag.CheckExtendedLanguageSubtag, "\x017Fgn" },
            { nameof(Bcp47LanguageTag.CheckScriptSubtag), Bcp47LanguageTag.CheckScriptSubtag, "\x212Ana" },
            { nameof(Bcp47LanguageTag.CheckRegionSubtag), Bcp47LanguageTag.CheckRegionSubtag, "\x212AR" },
            { nameof(Bcp47LanguageTag.CheckVariantSubtag), Bcp47LanguageTag.CheckVariantSubtag, "\x017Fursilv" },
            { nameof(Bcp47LanguageTag.CheckExtensionSubtag), Bcp47LanguageTag.CheckExtensionSubtag, "u-\x017Fd-chzh" },
            { nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), Bcp47LanguageTag.CheckPrivateUseSubtag, "x-\x212A" }
        };

        /// <summary>
        /// Static checks, each paired with the name of its parameter, which the <see cref="ArgumentNullException"/> it throws for
        /// <c>null</c> must report.
        /// </summary>
        public static TheoryData<string, Func<string, bool>, string> NullArgumentChecks => new TheoryData<string, Func<string, bool>, string>()
        {
            { nameof(Bcp47LanguageTag.CheckPrimaryLanguageSubtag), Bcp47LanguageTag.CheckPrimaryLanguageSubtag, "primaryLanguageSubtag" },
            { nameof(Bcp47LanguageTag.CheckExtendedLanguageSubtag), Bcp47LanguageTag.CheckExtendedLanguageSubtag, "extendedLanguageSubtag" },
            { nameof(Bcp47LanguageTag.CheckScriptSubtag), Bcp47LanguageTag.CheckScriptSubtag, "scriptSubtag" },
            { nameof(Bcp47LanguageTag.CheckRegionSubtag), Bcp47LanguageTag.CheckRegionSubtag, "regionSubtag" },
            { nameof(Bcp47LanguageTag.CheckVariantSubtag), Bcp47LanguageTag.CheckVariantSubtag, "variantSubtag" },
            { nameof(Bcp47LanguageTag.CheckExtensionSubtag), Bcp47LanguageTag.CheckExtensionSubtag, "extensionSubtag" },
            { nameof(Bcp47LanguageTag.CheckPrivateUseSubtag), Bcp47LanguageTag.CheckPrivateUseSubtag, "privateUseSubtag" },
            { nameof(Bcp47LanguageTag.IsWellFormedLanguageTagString), Bcp47LanguageTag.IsWellFormedLanguageTagString, "languageTagString" }
        };

        /// <summary>
        /// Language tags that repeat a variant subtag (RFC 5646 §2.2.5¶5) or an extension singleton (§2.2.6¶3), each paired with
        /// the same tag without the repetition. The comparison is case-insensitive (§2.1.1). Private use subtags may repeat.
        /// </summary>
        private static readonly (string LanguageTag, string ControlLanguageTag)[] DuplicateSubtagPairs =
        {
            ("ca-valencia-valencia", CatalanValencian),
            ("ca-valencia-VALENCIA", CatalanValencian),
            ("en-1901-1901", "en-1901"),
            ("sl-rozaj-biske-rozaj", "sl-rozaj-biske"),
            ("en-a-bbb-a-ccc", EnglishWithTwoExtensions),
            ("en-a-bbb-A-ccc", EnglishWithTwoExtensions),
            ("en-a-bbb-b-ccc-a-ddd", "en-a-bbb-b-ccc-c-ddd"),
            ("en-a-bbb-a-ccc-x-a-a", "en-a-bbb-x-a-a"),
        };

        public static TheoryData<string> DuplicateSubtagLanguageTags => FirstOfEach(DuplicateSubtagPairs);

        /// <summary>
        /// The 26 grandfathered language tags (RFC 5646 §2.1, §2.2.8): 17 irregular, then 9 regular, plus irregular and regular tags in other cases (§2.1.1).
        /// </summary>
        public static TheoryData<string> GrandfatheredLanguageTags => new TheoryData<string>()
        {
            "en-GB-oed", "i-ami", "i-bnn", "i-default", "i-enochian", "i-hak", KlingonGrandfathered, "i-lux", "i-mingo", "i-navajo", "i-pwn", "i-tao", "i-tay", "i-tsu",
            "sgn-BE-FR", "sgn-BE-NL", "sgn-CH-DE",
            LojbanGrandfathered, "cel-gaulish", "no-bok", "no-nyn", "zh-guoyu", "zh-hakka", "zh-min", "zh-min-nan", "zh-xiang",
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
            FourLetters, "ABCD", "Wxyz",
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
        private static readonly (string LanguageTag, string ControlLanguageTag)[] MisplacedExtendedLanguageSubtagPairs =
        {
            ("abcde-aaa", "zh-aaa"),
            ("abcd-yue", "zh-yue"),
            ("klingon-aaa-bbb", "tlh-aaa-bbb"),
            ("abcdefgh-cmn-Hans-CN", "zh-cmn-Hans-CN"),
        };

        public static TheoryData<string> MisplacedExtendedLanguageSubtagLanguageTags => FirstOfEach(MisplacedExtendedLanguageSubtagPairs);

        /// <summary>
        /// The UN M.49 code for the Channel Islands, which RFC 5646 §2.2.4¶4E allows to be registered as a Region subtag, but which is not registered.
        /// </summary>
        private const ushort ChannelIslandsUnM49Code = 830;

        /// <summary>
        /// Language tags with the unregistered Region subtag <c>830</c> (RFC 5646 §2.2.4¶4E-F), each with a control tag that has a registered UN M.49 Region subtag in its place.
        /// </summary>
        private static readonly (string LanguageTag, string ControlLanguageTag)[] ChannelIslandsPairs =
        {
            (EnglishChannelIslands, "en-154"),
            (FrenchLatinChannelIslands, "fr-Latn-154"),
            (EnglishChannelIslandsPrivateUse, "en-154-x-foo"),
        };

        public static TheoryData<string> ChannelIslandsLanguageTags => FirstOfEach(ChannelIslandsPairs);

        /// <summary>
        /// Language tags whose subtags are registered only through the private use range records <c>qaa..qtz</c>, <c>Qaaa..Qabx</c>, <c>QM..QZ</c> and <c>XA..XZ</c> (RFC 5646 §3.1.1, §3.1.4),
        /// at each end of a range and inside it, in various cases (§2.1.1).
        /// </summary>
        public static TheoryData<string> PrivateUseRangeLanguageTags => new TheoryData<string>()
        {
            "qaa", "qtz", "qbm", "QTZ", EnglishFirstPrivateUseScript, EnglishPrivateUseScriptQaaz, EnglishPrivateUseScriptQaba, EnglishLastPrivateUseScript, EnglishFirstPrivateUseRegionQ, "en-QS", "en-QZ", "en-XA", "en-XZ", "qaa-Qabx-XZ",
        };

        /// <summary>
        /// Language tags with an unregistered Script or Region subtag just past the end of a private use range record, each with a control tag that has the end of the range in its place.
        /// </summary>
        private static readonly (string LanguageTag, string ControlLanguageTag)[] OutsidePrivateUseRangePairs =
        {
            ("en-Qaby", EnglishLastPrivateUseScript),
            ("en-QL", EnglishFirstPrivateUseRegionQ),
        };

        public static TheoryData<string> OutsidePrivateUseRangeLanguageTags => FirstOfEach(OutsidePrivateUseRangePairs);

        /// <summary>
        /// The control tags of <see cref="DuplicateSubtagPairs"/>, <see cref="MisplacedExtendedLanguageSubtagPairs"/> and <see cref="ChannelIslandsPairs"/>:
        /// tags that are well-formed by the default rules, and differ from a tag that isn't only in what makes that one not well-formed.
        /// </summary>
        public static TheoryData<string> ControlLanguageTags => ControlOfEach(DuplicateSubtagPairs, MisplacedExtendedLanguageSubtagPairs, ChannelIslandsPairs);

        /// <summary>
        /// The control tags of <see cref="DuplicateSubtagPairs"/>, <see cref="ChannelIslandsPairs"/> and <see cref="OutsidePrivateUseRangePairs"/>: tags that are
        /// valid against the IANA registry fixture, and differ from a tag that isn't only in what makes that one not valid.
        /// </summary>
        public static TheoryData<string> RegistryControlLanguageTags => ControlOfEach(DuplicateSubtagPairs, ChannelIslandsPairs, OutsidePrivateUseRangePairs);

        /// <summary>
        /// Language tags with a Script subtag reserved for private use, <c>Qaaa</c> to <c>Qaaz</c> and then <c>Qaba</c> to <c>Qabx</c> (RFC 5646 §2.2.3¶3), at each end of
        /// both runs and in various cases (§2.1.1).
        /// </summary>
        public static TheoryData<string> PrivateUseScriptLanguageTags => new TheoryData<string>()
        {
            EnglishFirstPrivateUseScript, "en-Qaax", "en-Qaay", EnglishPrivateUseScriptQaaz, "en-QAAZ", "en-qaay", EnglishPrivateUseScriptQaba, EnglishLastPrivateUseScript, "qaa-Qaaz-XZ",
        };

        #region Test: CheckExtendedLanguageSubtag_WithInvalidSubtag

        [Theory]
        [InlineData(OneLetter), InlineData(TwoLetters), InlineData(FourLetters), InlineData(ThreeDigits), InlineData("a2c")]
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
        [InlineData(TwoLetters), InlineData("12"), InlineData(PrivateUseOneDigit), InlineData(PrivateUseOneLetter), InlineData(PrivateUseNineDigits), InlineData(PrivateUseNineLetters)]
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
        [InlineData("x"), InlineData("x-"), InlineData(PrivateUseNineDigits), InlineData(PrivateUseNineLetters)]
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
        [InlineData(OneLetter), InlineData(ThirdExtendedLanguageSubtag), InlineData(OneDigit), InlineData("22"), InlineData("4444")]
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

        #region Test: CheckSubtag_WithNull

        [Theory]
        [MemberData(nameof(NullArgumentChecks))]
        public void CheckSubtag_WithNull_ThrowsArgumentNullException(string checkName, Func<string, bool> check, string expectedParameterName)
        {
            // act
            Action testAction = () => check(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>("{0} must reject null", checkName).WithParameterName(expectedParameterName);
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
            var testResult = Bcp47LanguageTag.CheckRegionSubtag(ChannelIslandsRegionSubtag);

            // assert
            testResult.Should().BeFalse("830 (Channel Islands) may be registered, but is not, and an unregistered UN M.49 area code must not be used (RFC 5646 §2.2.4¶4E-F)");
        }

        #endregion

        #region Test: CheckScriptSubtag_WithInvalidSubtag

        [Theory]
        [InlineData(OneDigit), InlineData(OneLetter), InlineData("a2cd"), InlineData("Qaby"), InlineData("Qabz"), InlineData("Qaca")]
        public void CheckScriptSubtag_WithInvalidSubtag_ReturnsFalse(string testSubtag)
        {
            // act
            var testResult = Bcp47LanguageTag.CheckScriptSubtag(testSubtag);

            // assert
            testResult.Should().BeFalse("the subtag {0} is not a valid Script subtag", testSubtag);
        }

        #endregion

        #region Test: CheckScriptSubtag_WithPrivateUseSubtag

        [Theory]
        [InlineData("Qaaa"), InlineData("Qaax"), InlineData("Qaay"), InlineData("Qaaz"), InlineData("QAAZ"), InlineData("qaay"), InlineData("Qaba"), InlineData("Qabx")]
        public void CheckScriptSubtag_WithPrivateUseSubtag_ReturnsTrue(string testSubtag)
        {
            // act
            var testResult = Bcp47LanguageTag.CheckScriptSubtag(testSubtag);

            // assert
            testResult.Should().BeTrue("the subtag {0} is reserved for private use, which is a valid Script subtag (RFC 5646 §2.2.3¶3)", testSubtag);
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
        [InlineData(OneLetter), InlineData(OneDigit), InlineData("1bc"), InlineData(FourLetters), InlineData(ThreeDigits), InlineData("123456789"), InlineData("abcdefghi")]
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
            var testLanguageTag = Bcp47LanguageTag.Create(EnglishLanguageTag);

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
                yield return new object[] { "en-aaa", new[] { FirstExtendedLanguageSubtag } };
                yield return new object[] { "en-aaa-bbb", new[] { FirstExtendedLanguageSubtag, SecondExtendedLanguageSubtag } };
                yield return new object[] { "en-aaa-bbb-ccc", new[] { FirstExtendedLanguageSubtag, SecondExtendedLanguageSubtag, ThirdExtendedLanguageSubtag } };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        #endregion

        #region Test: ExtendedLanguageSubtags_OnRepeatedReads

        [Fact]
        public void ExtendedLanguageSubtags_OnRepeatedReads_ReturnsSameCollection()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(CantoneseHongKong);
            var testFirstResult = testLanguageTag.ExtendedLanguageSubtags;

            // act
            var testResult = testLanguageTag.ExtendedLanguageSubtags;

            // assert
            testResult.Should().BeSameAs(testFirstResult, "the decomposition is made once and shared, not copied per read");
            testResult.Should().Equal(CantoneseExtendedLanguageSubtag);
        }

        #endregion

        #region Test: ExtendedLanguageSubtags_WhenCollectionIsChanged

        [Fact]
        public void ExtendedLanguageSubtags_WhenCollectionIsChanged_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = Bcp47LanguageTag.Create(CantoneseHongKong).ExtendedLanguageSubtags;

            // act
            Action testAction = () => ((IList<string>)testSubject)[0] = MandarinExtendedLanguageSubtag;

            // assert
            testAction.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void ExtendedLanguageSubtags_OnValidLanguageTag_IsNotAnArray()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(CantoneseHongKong);

            // act
            var testResult = testLanguageTag.ExtendedLanguageSubtags;

            // assert
            testResult.Should().NotBeAssignableTo<string[]>("a cast must not reach the array behind the collection");
        }

        [Fact]
        public void ExtendedLanguageSubtags_AfterRejectedChange_AreUnchanged()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(CantoneseHongKong);
            var testSubject = testLanguageTag.ExtendedLanguageSubtags;
            Record.Exception(() => ((IList<string>)testSubject)[0] = MandarinExtendedLanguageSubtag);

            // act
            var testResult = testLanguageTag.ExtendedLanguageSubtags;

            // assert
            testResult.Should().Equal(CantoneseExtendedLanguageSubtag);
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
            var testLanguageTag = Bcp47LanguageTag.Create(EnglishLanguageTag);

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
        [InlineData(EmptyString), InlineData("zz"), InlineData("NotALanguageTag")]
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
        [InlineData(EnglishWithLineFeed), InlineData(EnglishUnitedStatesWithLineFeed), InlineData(PrivateUseWithLineFeed)]
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
        [InlineData(KelvinSignO), InlineData(LongSV), InlineData(EnglishPrivateUseKelvinSign), InlineData(EnglishPrivateUseLongS), InlineData(EnglishKelvinSignRegion)]
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
        public void IsWellFormedLanguageTagString_WithDuplicateSubtags_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

            // assert
            testResult.Should().BeFalse("the language tag '{0}' repeats a variant subtag or extension singleton (RFC 5646 §2.2.5, §2.2.6)", testLanguageTag);
        }

        [Theory]
        [MemberData(nameof(ControlLanguageTags))]
        public void IsWellFormedLanguageTagString_WithControlLanguageTag_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

            // assert
            testResult.Should().BeTrue("the language tag '{0}' has no repeated subtags, Extended Language subtags only after a two- or three-letter Primary Language subtag, and only registered Region subtags", testLanguageTag);
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithChannelIslandsRegionSubtag

        [Theory]
        [MemberData(nameof(ChannelIslandsLanguageTags))]
        public void IsWellFormedLanguageTagString_WithChannelIslandsRegionSubtag_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

            // assert
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

        /// <summary>
        /// Each of <see cref="ReservedPrimaryLanguageSubtags"/> alone, followed by a Script and a Region subtag, and followed by a Private Use subtag.
        /// </summary>
        public static TheoryData<string> ReservedPrimaryLanguageSubtagLanguageTags => CreateLanguageTagsStartingWith(ReservedPrimaryLanguageSubtags, LatinUnitedStatesSuffix);

        /// <summary>
        /// Each of <see cref="NonAlphaPrimaryLanguageSubtags"/> alone, followed by a Region subtag, and followed by a Private Use subtag.
        /// </summary>
        public static TheoryData<string> NonAlphaPrimaryLanguageSubtagLanguageTags => CreateLanguageTagsStartingWith(NonAlphaPrimaryLanguageSubtags, "-US");

        private static TheoryData<string> CreateLanguageTagsStartingWith(TheoryData<string> primaryLanguageSubtags, string otherSubtags)
        {
            var data = new TheoryData<string>();
            foreach (var primaryLanguageSubtag in primaryLanguageSubtags)
            {
                data.Add(primaryLanguageSubtag);
                data.Add(primaryLanguageSubtag + otherSubtags);
                data.Add(primaryLanguageSubtag + PrivateUseSuffix);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(ReservedPrimaryLanguageSubtagLanguageTags))]
        public void IsWellFormedLanguageTagString_WithReservedPrimaryLanguageSubtag_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

            // assert
            testResult.Should().BeTrue("'{0}' has a reserved Primary Language subtag, which is well-formed (RFC 5646 §2.1, §2.2.1¶4)", testLanguageTag);
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithPrivateUseScriptSubtag

        [Theory]
        [MemberData(nameof(PrivateUseScriptLanguageTags))]
        public void IsWellFormedLanguageTagString_WithPrivateUseScriptSubtag_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

            // assert
            testResult.Should().BeTrue("'{0}' has a Script subtag reserved for private use, which is well-formed (RFC 5646 §2.2.3¶3)", testLanguageTag);
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithNonAlphaPrimaryLanguageSubtag

        [Theory]
        [MemberData(nameof(NonAlphaPrimaryLanguageSubtagLanguageTags))]
        public void IsWellFormedLanguageTagString_WithNonAlphaPrimaryLanguageSubtag_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

            // assert
            testResult.Should().BeFalse("the Primary Language subtag of '{0}' contains a digit (RFC 5646 §2.1, §2.2.1)", testLanguageTag);
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithMisplacedExtendedLanguageSubtags

        [Theory]
        [MemberData(nameof(MisplacedExtendedLanguageSubtagLanguageTags))]
        public void IsWellFormedLanguageTagString_WithMisplacedExtendedLanguageSubtags_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

            // assert
            testResult.Should().BeFalse("the language tag '{0}' has Extended Language subtags after a Primary Language subtag of four to eight letters (RFC 5646 §2.1, §2.2.2)", testLanguageTag);
        }

        #endregion

        #region Test: IsWellFormedLanguageTagString_WithValidLanguageTag

        [Theory]
        [InlineData(EnglishLanguageTag, "English"),
         InlineData(LatinAmericanSpanish, "Latin American Spanish"),
         InlineData(RomanshSursilvan, "Romansh Sursilvan"),
         InlineData(SerbianCyrillic, "Serbian written in Cyrillic script"),
         InlineData(MinNanTraditionalTaiwan, "Min Nan Chinese using traditional Han characters, as spoken in Taiwan"),
         InlineData(CantoneseTraditionalHongKong, "Cantonese using traditional Han characters, as spoken in Hong Kong"),
         InlineData(SwissGermanZurich, "Zürich German")]
        public void IsWellFormedLanguageTagString_WithValidLanguageTag_ReturnsTrue(string testLanguageTag, string testLanguageTagDescription)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormedLanguageTagString(testLanguageTag);

            // assert
            testResult.Should().BeTrue("{0} is the language tag for {1}", testLanguageTag, testLanguageTagDescription);
        }

        #endregion

        #region Test: ExtensionSubtags_OnRepeatedReads

        [Fact]
        public void ExtensionSubtags_OnRepeatedReads_ReturnsSameCollection()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(HebrewIsraelHebrewCalendar);
            var testFirstResult = testLanguageTag.ExtensionSubtags;

            // act
            var testResult = testLanguageTag.ExtensionSubtags;

            // assert
            testResult.Should().BeSameAs(testFirstResult, "the decomposition is made once and shared, not copied per read");
            testResult.Should().Equal((Bcp47KeyedSubtag)HebrewCalendarExtension);
        }

        #endregion

        #region Test: ExtensionSubtags_WhenCollectionIsChanged

        [Fact]
        public void ExtensionSubtags_WhenCollectionIsChanged_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = Bcp47LanguageTag.Create(HebrewIsraelHebrewCalendar).ExtensionSubtags;

            // act
            Action testAction = () => ((IList<Bcp47KeyedSubtag>)testSubject)[0] = (Bcp47KeyedSubtag)TransformedExtension;

            // assert
            testAction.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void ExtensionSubtags_OnValidLanguageTag_IsNotAnArray()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(HebrewIsraelHebrewCalendar);

            // act
            var testResult = testLanguageTag.ExtensionSubtags;

            // assert
            testResult.Should().NotBeAssignableTo<Bcp47KeyedSubtag[]>("a cast must not reach the array behind the collection");
        }

        [Fact]
        public void ExtensionSubtags_AfterRejectedChange_AreUnchanged()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(HebrewIsraelHebrewCalendar);
            var testSubject = testLanguageTag.ExtensionSubtags;
            Record.Exception(() => ((IList<Bcp47KeyedSubtag>)testSubject)[0] = (Bcp47KeyedSubtag)TransformedExtension);

            // act
            var testResult = testLanguageTag.ExtensionSubtags;

            // assert
            testResult.Should().Equal((Bcp47KeyedSubtag)HebrewCalendarExtension);
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
                        yield return new object[] { primaryLanguageSubtag + PrivateUseSuffix, primaryLanguageSubtag };
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

        public static TheoryData<string, LanguageTagSubtags> ReservedPrimaryLanguageSubtagsWithScriptAndRegion
        {
            get
            {
                var data = new TheoryData<string, LanguageTagSubtags>();
                foreach (var primaryLanguageSubtag in ReservedPrimaryLanguageSubtags)
                {
                    data.Add(primaryLanguageSubtag + LatinUnitedStatesSuffix, new LanguageTagSubtags(primaryLanguageSubtag, new string[0], "Latn", "US", new string[0], new Bcp47KeyedSubtag[0], null));
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(ReservedPrimaryLanguageSubtagsWithScriptAndRegion), DisableDiscoveryEnumeration = true)]
        public void Subtags_WhenPrimaryLanguageSubtagIsReserved_ReturnsEverySubtag(string testLanguageTag, LanguageTagSubtags expectedResult)
        {
            // arrange
            var testSubject = Bcp47LanguageTag.Create(testLanguageTag);

            // act
            var testResult = LanguageTagSubtags.Of(testSubject);

            // assert
            testResult.Should().BeEquivalentTo(expectedResult, "a reserved Primary Language subtag is decomposed like any other (RFC 5646 §2.2.1¶4)");
        }

        #endregion

        #region Test: PrimaryLanguageSubtag_OnPrivateUseLanguageTag

        public static TheoryData<string, Bcp47LanguageTag> PrivateUseLanguageTagCases => CreateLanguageTagCases(new[] { PrivateUseOnly, "X-PRIVATE", PrivateUseFrenchSwitzerland });

        [Theory]
        [MemberData(nameof(PrivateUseLanguageTagCases), DisableDiscoveryEnumeration = true)]
        public void PrimaryLanguageSubtag_OnPrivateUseLanguageTag_ReturnsEmptyString(string testCase, Bcp47LanguageTag testSubject)
        {
            // act
            var testResult = testSubject.PrimaryLanguageSubtag;

            // assert
            testResult.Should().BeEmpty("a private use language tag ({0}) has no Primary Language subtag (RFC 5646 §2.2.7)", testCase);
        }

        [Theory]
        [MemberData(nameof(PrivateUseLanguageTagCases), DisableDiscoveryEnumeration = true)]
        public void PrivateUseSubtag_OnPrivateUseLanguageTag_ReturnsSubtag(string testCase, Bcp47LanguageTag testSubject)
        {
            // act
            var testResult = testSubject.PrivateUseSubtag;

            // assert
            testResult.Should().NotBeNull("a private use language tag ({0}) is its Private Use subtag (RFC 5646 §2.2.7)", testCase);
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
            const string languageTag = EnglishLanguageTag;
            var testLanguageTag = Bcp47LanguageTag.Create(languageTag);

            // act
            var testResult = testLanguageTag.PrivateUseSubtag;

            // assert
            testResult.Should().BeNull("the language tag {0} does not contain a Private Use subtag", languageTag);
        }

        #endregion

        #region Test: PrivateUseSubtag_OnValidLanguageTagWithPrivateUseSubtag

        [Theory]
        [InlineData("en-x-a", PrivateUseOneLetter),
         InlineData("en-x-bb", "x-bb"),
         InlineData("en-x-ccc", "x-ccc"),
         InlineData("en-x-dddd", "x-dddd"),
         InlineData("en-x-eeeee", "x-eeeee"),
         InlineData("en-x-ffffff", "x-ffffff"),
         InlineData("en-x-ggggggg", "x-ggggggg"),
         InlineData("en-x-hhhhhhhh", "x-hhhhhhhh"),
         InlineData("en-x-1", PrivateUseOneDigit),
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
            var testLanguageTag = Bcp47LanguageTag.Create(EnglishLanguageTag);

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
            testAction.Should().Throw<LanguageTagFormatException>("{0} is not a valid Region subtag", testCode).WithMessage(InvalidFormatMessage);
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
            var testLanguageTag = Bcp47LanguageTag.Create(EnglishLanguageTag);

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
            var testLanguageTag = Bcp47LanguageTag.Create(EnglishLanguageTag);

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
            var testLanguageTag = Bcp47LanguageTag.Create(RomanshSursilvan);

            // act
            var testResult = testLanguageTag.VariantSubtags;

            // assert
            testResult.Should().Equal("sursilv");
        }

        #endregion

        #region Test: VariantSubtags_OnRepeatedReads

        [Fact]
        public void VariantSubtags_OnRepeatedReads_ReturnsSameCollection()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(ResianSanGiorgio1994);
            var testFirstResult = testLanguageTag.VariantSubtags;

            // act
            var testResult = testLanguageTag.VariantSubtags;

            // assert
            testResult.Should().BeSameAs(testFirstResult, "the decomposition is made once and shared, not copied per read");
            testResult.Should().Equal(RozajVariantSubtag, BiskeVariantSubtag, NineteenNinetyFourVariantSubtag);
        }

        #endregion

        #region Test: VariantSubtags_WhenCollectionIsChanged

        [Fact]
        public void VariantSubtags_WhenCollectionIsChanged_ThrowsNotSupportedException()
        {
            // arrange
            var testSubject = Bcp47LanguageTag.Create(ResianSanGiorgio1994).VariantSubtags;

            // act
            Action testAction = () => ((IList<string>)testSubject)[0] = NineteenOhOneVariantSubtag;

            // assert
            testAction.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void VariantSubtags_OnValidLanguageTag_IsNotAnArray()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(ResianSanGiorgio1994);

            // act
            var testResult = testLanguageTag.VariantSubtags;

            // assert
            testResult.Should().NotBeAssignableTo<string[]>("a cast must not reach the array behind the collection");
        }

        [Fact]
        public void VariantSubtags_AfterRejectedChange_AreUnchanged()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(ResianSanGiorgio1994);
            var testSubject = testLanguageTag.VariantSubtags;
            Record.Exception(() => ((IList<string>)testSubject)[0] = NineteenOhOneVariantSubtag);

            // act
            var testResult = testLanguageTag.VariantSubtags;

            // assert
            testResult.Should().Equal(RozajVariantSubtag, BiskeVariantSubtag, NineteenNinetyFourVariantSubtag);
        }

        #endregion

        #region Test: SubtagProperties_OnGrandfatheredLanguageTag

        public static TheoryData<string, Bcp47LanguageTag> GrandfatheredLanguageTagCases
        {
            get
            {
                return CreateLanguageTagCases(GrandfatheredLanguageTags);
            }
        }

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTagCases), DisableDiscoveryEnumeration = true)]
        public void SubtagProperties_OnGrandfatheredLanguageTag_ReturnNoSubtags(string testCase, Bcp47LanguageTag testSubject)
        {
            // act
            var testResult = LanguageTagSubtags.Of(testSubject);

            // assert
            testResult.Should().BeEquivalentTo(LanguageTagSubtags.None, "a grandfathered language tag ({0}) is interpreted as a whole (RFC 5646 §2.2.8)", testCase);
        }

        #endregion

        #region Test: ToIso15924_WithLanguageTagHavingNoScriptSubtag

        [Fact]
        public void ToIso15924_WithLanguageTagHavingNoScriptSubtag_ReturnsNull()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(EnglishLanguageTag);

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
            var testLanguageTag = Bcp47LanguageTag.Create(SerbianCyrillic);

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
            var testLanguageTag = Bcp47LanguageTag.Create(EnglishLanguageTag);

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
            var testLanguageTag = Bcp47LanguageTag.Create(CantoneseTraditionalHongKong);

            // act
            var testResult = testLanguageTag.ToIso3166Part1Alpha2();

            // assert
            testResult.Should().Be(Iso3166Part1Alpha2Country.HK);
        }

        #endregion

        #region Test: ToIso3166Part1Alpha2_WithLanguageTagHavingUnM49RegionSubtag

        [Theory]
        [InlineData(LatinAmericanSpanish)]
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
        [InlineData(EnglishUnitedStates)]
        [InlineData(EnglishUnitedStatesLowerCase)]
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
            var testLanguageTag = Bcp47LanguageTag.Create(UnlistedPrimaryLanguageSubtag);

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
            var testLanguageTag = Bcp47LanguageTag.Create(LatinAmericanSpanish);

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
            var testLanguageTag = Bcp47LanguageTag.Create(UnlistedPrimaryLanguageSubtag);

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
            var testLanguageTag = Bcp47LanguageTag.Create(UnlistedPrimaryLanguageSubtag);

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
            var testLanguageTag = Bcp47LanguageTag.Create(MinNanTraditionalTaiwan);

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
            var testLanguageTag = Bcp47LanguageTag.Create(UnlistedPrimaryLanguageSubtag);

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

        /// <summary>
        /// Each conversion to an ISO 639 code.
        /// </summary>
        private static readonly Dictionary<string, Func<Bcp47LanguageTag, object>> Iso639Conversions = new Dictionary<string, Func<Bcp47LanguageTag, object>>
        {
            { nameof(Bcp47LanguageTag.ToIso639Part1), languageTag => languageTag.ToIso639Part1() },
            { nameof(Bcp47LanguageTag.ToIso639Part2T), languageTag => languageTag.ToIso639Part2T() },
            { nameof(Bcp47LanguageTag.ToIso639Part3), languageTag => languageTag.ToIso639Part3() },
            { nameof(Bcp47LanguageTag.ToIso639Part5), languageTag => languageTag.ToIso639Part5() }
        };

        /// <summary>
        /// Each conversion to an ISO 639 code, with each language tag that has no Primary Language subtag, created in each way.
        /// </summary>
        public static TheoryData<string, Func<Bcp47LanguageTag, object>, Bcp47LanguageTag> NoPrimaryLanguageSubtagConversions
        {
            get
            {
                var data = new TheoryData<string, Func<Bcp47LanguageTag, object>, Bcp47LanguageTag>();
                foreach (var conversion in Iso639Conversions)
                foreach (var row in CreateLanguageTagCases(new[] { PrivateUseOnly, KlingonGrandfathered, LojbanGrandfathered }))
                {
                    data.Add(conversion.Key + " of " + row[0], conversion.Value, (Bcp47LanguageTag)row[1]);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(NoPrimaryLanguageSubtagConversions), DisableDiscoveryEnumeration = true)]
        public void ToIso639_WithLanguageTagHavingNoPrimaryLanguageSubtag_ReturnsNull(string testCase, Func<Bcp47LanguageTag, object> convert, Bcp47LanguageTag testSubject)
        {
            // act
            var testResult = convert(testSubject);

            // assert
            testResult.Should().BeNull("{0} has no Primary Language subtag to convert", testCase);
        }

        #endregion

        #region Test: ToUnM49_WithLanguageTagHavingNoRegionSubtag

        [Fact]
        public void ToUnM49_WithLanguageTagHavingNoRegionSubtag_ReturnsNull()
        {
            // arrange
            var testLanguageTag = Bcp47LanguageTag.Create(EnglishLanguageTag);

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
            var testLanguageTag = Bcp47LanguageTag.Create(LatinAmericanSpanish);

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
            testAction.Should().Throw<InvalidCastException>("a null string is not a language tag").WithMessage("The language tag must not be null.");
        }

        #endregion

        #region Test: ExplicitConversion_WhenLanguageTagIsInvalid

        [Theory]
        [InlineData(EmptyString), InlineData(NotALanguageTag), InlineData(EnglishWithEmptySubtag), InlineData("en-US-US")]
        public void ExplicitConversion_WhenLanguageTagIsInvalid_ThrowsInvalidCastException(string testLanguageTag)
        {
            // act
            Action testAction = () => _ = (Bcp47LanguageTag)testLanguageTag;

            // assert
            testAction.Should().Throw<InvalidCastException>("'{0}' is not a well-formed language tag", testLanguageTag).WithMessage(InvalidFormatMessage);
        }

        #endregion

        #region Test: ExplicitConversion_WhenLanguageTagIsValid

        [Theory]
        [InlineData(EnglishLanguageTag), InlineData(TraditionalChineseTaiwan), InlineData(KlingonGrandfathered), InlineData(PrivateUseOnly)]
        public void ExplicitConversion_WhenLanguageTagIsValid_KeepsTheString(string testLanguageTag)
        {
            // act
            string testResult = (Bcp47LanguageTag)testLanguageTag;

            // assert
            testResult.Should().Be(testLanguageTag, "the conversion keeps the original string");
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenLanguageTagIsNull

        [Fact]
        public void Create_WithLanguageTagWhenLanguageTagIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>("the language tag must not be null").WithParameterName(LanguageTagParameterName);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenLanguageTagIsInvalid

        [Theory]
        [InlineData(EmptyString), InlineData(NotALanguageTag)]
        public void Create_WithLanguageTagWhenLanguageTagIsInvalid_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("the language tag was not in a recognised format").WithMessage(InvalidFormatMessage);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenLanguageTagHasTrailingNewline

        [Theory]
        [InlineData(EnglishWithLineFeed), InlineData(EnglishUnitedStatesWithLineFeed)]
        public void Create_WithLanguageTagWhenLanguageTagHasTrailingNewline_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("a language tag followed by a line feed is not well-formed").WithMessage(InvalidFormatMessage);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasTrailingNewline

        [Theory]
        [InlineData(EnglishWithLineFeed), InlineData(EnglishUnitedStatesWithLineFeed)]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasTrailingNewline_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag, IanaSubtagRegistryResource.Shared);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("a language tag followed by a line feed is not well-formed");
        }

        #endregion

        #region Test: Create_WithLanguageTagUnderTurkishCulture

        [Theory]
        [InlineData(ItalianUpperCase), InlineData(ItalianSwitzerland), InlineData(ItalianSwitzerlandMixedCase), InlineData(ItalianLanguageTag)]
        public void Create_WithLanguageTagUnderTurkishCulture_ReturnsLanguageTag(string testLanguageTag)
        {
            // arrange
            using (new CultureScope(TurkishCultureName))
            {
                // act
                string testResult = Bcp47LanguageTag.Create(testLanguageTag);

                // assert
                testResult.Should().Be(testLanguageTag, "case-insensitive matching of a language tag doesn't depend on the current culture (RFC 5646 §2.1.1)");
            }
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryUnderTurkishCulture

        [Theory]
        [InlineData(ItalianUpperCase), InlineData(ItalianSwitzerland), InlineData(ItalianSwitzerlandMixedCase), InlineData(ItalianLanguageTag)]
        public void Create_WithLanguageTagAndSubtagRegistryUnderTurkishCulture_ReturnsLanguageTag(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;
            using (new CultureScope(TurkishCultureName))
            {
                // act
                string testResult = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

                // assert
                testResult.Should().Be(testLanguageTag, "case-insensitive matching of a language tag doesn't depend on the current culture (RFC 5646 §2.1.1)");
            }
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenLanguageTagHasNonAsciiCaseEquivalent

        [Theory]
        [InlineData(KelvinSignO), InlineData(LongSV), InlineData(EnglishPrivateUseKelvinSign), InlineData(EnglishPrivateUseLongS), InlineData(EnglishKelvinSignRegion)]
        public void Create_WithLanguageTagWhenLanguageTagHasNonAsciiCaseEquivalent_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("a language tag has only ASCII letters and digits (RFC 5646 §2.1), so the Kelvin sign and long s aren't letters of one").WithMessage(InvalidFormatMessage);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasNonAsciiCaseEquivalent

        [Theory]
        [InlineData(KelvinSignO), InlineData(EnglishPrivateUseKelvinSign), InlineData(EnglishPrivateUseLongS)]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasNonAsciiCaseEquivalent_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag, IanaSubtagRegistryResource.Shared);

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
            testAction.Should().Throw<LanguageTagFormatException>("the Primary Language subtag of '{0}' contains a digit (RFC 5646 §2.1, §2.2.1)", testLanguageTag).WithMessage(InvalidFormatMessage);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenPrimaryLanguageSubtagIsReserved

        [Theory]
        [MemberData(nameof(ReservedPrimaryLanguageSubtags))]
        public void Create_WithLanguageTagWhenPrimaryLanguageSubtagIsReserved_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testResult.Should().Be(testLanguageTag, "'{0}' is a reserved Primary Language subtag, which is well-formed (RFC 5646 §2.1, §2.2.1¶4)", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenScriptSubtagIsPrivateUse

        [Theory]
        [MemberData(nameof(PrivateUseScriptLanguageTags))]
        public void Create_WithLanguageTagWhenScriptSubtagIsPrivateUse_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testResult.Should().Be(testLanguageTag, "'{0}' has a Script subtag reserved for private use, which is well-formed (RFC 5646 §2.2.3¶3)", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenExtendedLanguageSubtagsAreMisplaced

        [Theory]
        [MemberData(nameof(MisplacedExtendedLanguageSubtagLanguageTags))]
        public void Create_WithLanguageTagWhenExtendedLanguageSubtagsAreMisplaced_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("the language tag '{0}' has Extended Language subtags after a Primary Language subtag of four to eight letters (RFC 5646 §2.1, §2.2.2)", testLanguageTag).WithMessage(InvalidFormatMessage);
        }

        [Theory]
        [MemberData(nameof(ControlLanguageTags))]
        public void Create_WithControlLanguageTag_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testResult.Should().Be(testLanguageTag, "the language tag '{0}' is well-formed", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenLanguageTagHasDuplicateSubtags

        [Theory]
        [MemberData(nameof(DuplicateSubtagLanguageTags))]
        public void Create_WithLanguageTagWhenLanguageTagHasDuplicateSubtags_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("a variant subtag or extension singleton is repeated (RFC 5646 §2.2.5, §2.2.6)").WithMessage(InvalidFormatMessage);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenRegionSubtagIsChannelIslands

        [Theory]
        [MemberData(nameof(ChannelIslandsLanguageTags))]
        public void Create_WithLanguageTagWhenRegionSubtagIsChannelIslands_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("the Region subtag 830 of '{0}' is not registered (RFC 5646 §2.2.4¶4E-F)", testLanguageTag).WithMessage(InvalidFormatMessage);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenRegionSubtagIsChannelIslands

        [Theory]
        [MemberData(nameof(ChannelIslandsLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenRegionSubtagIsChannelIslands_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("the Subtag Registry has no record for the Region subtag 830 of '{0}'", testLanguageTag).WithMessage(InvalidFormatMessage);
        }

        [Theory]
        [MemberData(nameof(RegistryControlLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsControl_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            testResult.Should().Be(testLanguageTag, "the Subtag Registry has a record for every subtag of '{0}', which repeats none", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenChannelIslandsIsRegistered

        /// <summary>
        /// A Subtag Registry that registers 830 (Channel Islands) as a Region subtag, which RFC 5646 §2.2.4¶4E allows, with the other subtags of
        /// <see cref="ChannelIslandsLanguageTags"/>.
        /// </summary>
        private const string ChannelIslandsSubtagRegistryContent =
            "File-Date: 2026-10-19\n" +
            "%%\nType: language\nSubtag: en\nDescription: English\nAdded: 2005-10-16\n" +
            "%%\nType: language\nSubtag: fr\nDescription: French\nAdded: 2005-10-16\n" +
            "%%\nType: script\nSubtag: Latn\nDescription: Latin\nAdded: 2005-10-16\n" +
            "%%\nType: region\nSubtag: 830\nDescription: Channel Islands\nAdded: 2026-10-19";

        [Theory]
        [MemberData(nameof(ChannelIslandsLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenChannelIslandsIsRegistered_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = SubtagRegistry.SubtagRegistry.CreateFromContent(ChannelIslandsSubtagRegistryContent);
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // act
            var testResult = languageTag.RegionSubtag;

            // assert
            testResult.Should().Be(ChannelIslandsRegionSubtag, "a Subtag Registry may register 830 (Channel Islands) as a Region subtag (RFC 5646 §2.2.4¶4E)");
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenLanguageTagIsGrandfathered

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void Create_WithLanguageTagWhenLanguageTagIsGrandfathered_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testResult.Should().Be(testLanguageTag, "'{0}' is a grandfathered language tag (RFC 5646 §2.1, §2.2.8)", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagWhenLanguageTagIsValid

        /// <summary>
        /// Well-formed language tags, among them two that are only a Private Use subtag (RFC 5646 §2.2.7), one of which has a second private use sequence.
        /// </summary>
        public static TheoryData<string> ValidLanguageTags
        {
            get
            {
                var data = new TheoryData<string>(_validLanguageTags);
                data.Add(PrivateUseFrenchSwitzerland);
                data.Add(PrivateUseFrenchSwitzerlandWithSecondSequence);
                return data;
            }
        }

        [Theory]
        [MemberData(nameof(ValidLanguageTags))]
        public void Create_WithLanguageTagWhenLanguageTagIsValid_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testResult.Should().Be(testLanguageTag, "'{0}' is a correctly formatted language tag", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsNull

        [Fact]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(null, subtagRegistry);

            // assert
            testAction.Should().Throw<ArgumentNullException>("the language tag must not be null").WithParameterName(LanguageTagParameterName);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenSubtagRegistryIsNull

        [Fact]
        public void Create_WithLanguageTagAndSubtagRegistryWhenSubtagRegistryIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(EnglishLanguageTag, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>("the subtag registry must not be null").WithParameterName("subtagRegistry");
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsInvalid

        [Theory]
        [InlineData(EmptyString), InlineData(InvalidLanguageTag)]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsInvalid_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("the language tag was not in a recognised format").WithMessage(InvalidFormatMessage);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasDuplicateSubtags

        [Theory]
        [MemberData(nameof(DuplicateSubtagLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasDuplicateSubtags_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("a variant subtag or extension singleton is repeated (RFC 5646 §2.2.5, §2.2.6)").WithMessage(InvalidFormatMessage);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenSubtagIsInPrivateUseRange

        [Theory]
        [MemberData(nameof(PrivateUseRangeLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenSubtagIsInPrivateUseRange_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            testResult.Should().Be(testLanguageTag, "the subtags of '{0}' are registered by private use range records (RFC 5646 §3.1.1)", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenSubtagIsOutsidePrivateUseRange

        [Theory]
        [MemberData(nameof(OutsidePrivateUseRangeLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenSubtagIsOutsidePrivateUseRange_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("the Subtag Registry has no record or range for a subtag of '{0}'", testLanguageTag).WithMessage(InvalidFormatMessage);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsGrandfathered

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsGrandfathered_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            testResult.Should().Be(testLanguageTag, "'{0}' is a grandfathered language tag (RFC 5646 §2.1, §2.2.8)", testLanguageTag);
        }

        #endregion

        #region Test: Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid

        [Theory]
        [MemberData(nameof(ValidLanguageTags))]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            testResult.Should().Be(testLanguageTag, "the language tag '{0}' is correctly formatted", testLanguageTag);
        }

        #endregion

        #region Test: GetHashCode_WhenTagsDifferOnlyInCase

        [Theory]
        [InlineData(EnglishUnitedStates, EnglishUnitedStatesLowerCase), InlineData(TraditionalChineseTaiwan, TraditionalChineseTaiwanUpperCase), InlineData(PrivateUseFrenchSwitzerland, PrivateUseFrenchSwitzerlandMixedCase)]
        public void GetHashCode_WhenTagsDifferOnlyInCase_ReturnsEqualHashCodes(string firstLanguageTag, string secondLanguageTag)
        {
            // arrange
            var expectedResult = Bcp47LanguageTag.Create(firstLanguageTag).GetHashCode();
            var second = Bcp47LanguageTag.Create(secondLanguageTag);

            // act
            var testResult = second.GetHashCode();

            // assert
            testResult.Should().Be(expectedResult, "'{0}' and '{1}' are equal, so their hash codes must be equal", firstLanguageTag, secondLanguageTag);
        }

        [Theory]
        [InlineData(EnglishUnitedStates, EnglishUnitedStatesLowerCase), InlineData(TraditionalChineseTaiwan, TraditionalChineseTaiwanUpperCase), InlineData(PrivateUseFrenchSwitzerland, PrivateUseFrenchSwitzerlandMixedCase)]
        public void Equals_WhenTagsDifferOnlyInCase_ReturnsTrue(string firstLanguageTag, string secondLanguageTag)
        {
            // arrange
            var first = Bcp47LanguageTag.Create(firstLanguageTag);
            var second = Bcp47LanguageTag.Create(secondLanguageTag);

            // act
            var testResult = first.Equals(second);

            // assert
            testResult.Should().BeTrue("language tags are compared case-insensitively (RFC 5646 §2.1.1)");
        }

        #endregion

        #region Test: GetHashCode_WhenTagsDifferOnlyInCaseInHashSet

        [Fact]
        public void GetHashCode_WhenTagsDifferOnlyInCaseInHashSet_DeduplicatesTags()
        {
            // arrange
            var testSet = new HashSet<Bcp47LanguageTag> { Bcp47LanguageTag.Create(EnglishUnitedStates) };
            var testLanguageTag = Bcp47LanguageTag.Create(EnglishUnitedStatesLowerCase);

            // act
            var testResult = testSet.Add(testLanguageTag);

            // assert
            testResult.Should().BeFalse("'en-us' is equal to 'en-US', which the set already holds");
            testSet.Should().ContainSingle();
        }

        [Fact]
        public void GetHashCode_WhenTagsDifferOnlyInCaseInHashSet_FindsTag()
        {
            // arrange
            var testSet = new HashSet<Bcp47LanguageTag> { Bcp47LanguageTag.Create(EnglishUnitedStates) };
            var testLanguageTag = Bcp47LanguageTag.Create("EN-us");

            // act
            var testResult = testSet.Contains(testLanguageTag);

            // assert
            testResult.Should().BeTrue("'EN-us' is equal to 'en-US'");
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
            var testResult = Bcp47LanguageTag.TryCreate((string)null, out _);

            // assert
            testResult.Should().BeFalse("the language tag must not be null");
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagWhenLanguageTagIsInvalid

        [Theory]
        [InlineData(EmptyString), InlineData(NotALanguageTag)]
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
        public void TryCreate_WithLanguageTagWhenLanguageTagHasDuplicateSubtags_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, out _);

            // assert
            testResult.Should().BeFalse("the language tag '{0}' repeats a variant subtag or extension singleton (RFC 5646 §2.2.5, §2.2.6)", testLanguageTag);
        }

        [Theory]
        [MemberData(nameof(ControlLanguageTags))]
        public void TryCreate_WithControlLanguageTag_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, out _);

            // assert
            testResult.Should().BeTrue("the language tag '{0}' is well-formed", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagWhenScriptSubtagIsPrivateUse

        [Theory]
        [MemberData(nameof(PrivateUseScriptLanguageTags))]
        public void TryCreate_WithLanguageTagWhenScriptSubtagIsPrivateUse_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = (Bcp47LanguageTag.TryCreate(testLanguageTag, out var languageTag), (string)languageTag);

            // assert
            testResult.Should().Be((true, testLanguageTag), "'{0}' has a Script subtag reserved for private use, which is well-formed (RFC 5646 §2.2.3¶3)", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagWhenLanguageTagIsGrandfathered

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void TryCreate_WithLanguageTagWhenLanguageTagIsGrandfathered_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = (Bcp47LanguageTag.TryCreate(testLanguageTag, out var languageTag), (string)languageTag);

            // assert
            testResult.Should().Be((true, testLanguageTag), "'{0}' is a grandfathered language tag (RFC 5646 §2.1, §2.2.8)", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagWhenLanguageTagIsValid

        [Theory]
        [MemberData(nameof(ValidLanguageTags))]
        public void TryCreate_WithLanguageTagWhenLanguageTagIsValid_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = (Bcp47LanguageTag.TryCreate(testLanguageTag, out var languageTag), (string)languageTag);

            // assert
            testResult.Should().Be((true, testLanguageTag), "language tag '{0}' is correctly formatted", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsNull

        [Fact]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsNull_ReturnsFalse()
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = Bcp47LanguageTag.TryCreate((string)null, subtagRegistry, out _);

            // assert
            testResult.Should().BeFalse("the language tag must not be null");
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenSubtagRegistryIsNull

        [Fact]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenSubtagRegistryIsNull_ReturnsFalse()
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(EnglishLanguageTag, null, out _);

            // assert
            testResult.Should().BeFalse("the subtag registry must not be null");
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsInvalid

        [Theory]
        [InlineData(EmptyString), InlineData(InvalidLanguageTag)]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsInvalid_ReturnsFalse(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, subtagRegistry, out _);

            // assert
            testResult.Should().BeFalse("the language tag '{0}' is not correctly formatted", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasDuplicateSubtags

        [Theory]
        [MemberData(nameof(DuplicateSubtagLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagHasDuplicateSubtags_ReturnsFalse(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, subtagRegistry, out _);

            // assert
            testResult.Should().BeFalse("the language tag '{0}' repeats a variant subtag or extension singleton (RFC 5646 §2.2.5, §2.2.6)", testLanguageTag);
        }

        [Theory]
        [MemberData(nameof(RegistryControlLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsControl_ReturnsTrue(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, subtagRegistry, out _);

            // assert
            testResult.Should().BeTrue("the Subtag Registry has a record for every subtag of '{0}', which repeats none", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenSubtagIsInPrivateUseRange

        [Theory]
        [MemberData(nameof(PrivateUseRangeLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenSubtagIsInPrivateUseRange_ReturnsTrue(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = (Bcp47LanguageTag.TryCreate(testLanguageTag, subtagRegistry, out var languageTag), (string)languageTag);

            // assert
            testResult.Should().Be((true, testLanguageTag), "the subtags of '{0}' are registered by private use range records (RFC 5646 §3.1.1)", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenSubtagIsOutsidePrivateUseRange

        [Theory]
        [MemberData(nameof(OutsidePrivateUseRangeLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenSubtagIsOutsidePrivateUseRange_ReturnsFalse(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, subtagRegistry, out _);

            // assert
            testResult.Should().BeFalse("the Subtag Registry has no record or range for a subtag of '{0}'", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsGrandfathered

        [Theory]
        [MemberData(nameof(GrandfatheredLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsGrandfathered_ReturnsTrue(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = (Bcp47LanguageTag.TryCreate(testLanguageTag, subtagRegistry, out var languageTag), (string)languageTag);

            // assert
            testResult.Should().Be((true, testLanguageTag), "'{0}' is a grandfathered language tag (RFC 5646 §2.1, §2.2.8)", testLanguageTag);
        }

        #endregion

        #region Test: TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid

        [Theory]
        [MemberData(nameof(ValidLanguageTags))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValid_ReturnsTrue(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = (Bcp47LanguageTag.TryCreate(testLanguageTag, subtagRegistry, out var languageTag), (string)languageTag);

            // assert
            testResult.Should().Be((true, testLanguageTag), "the language tag '{0}' is correctly formatted", testLanguageTag);
        }

        #endregion

        #region Test: CreateAndTryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValidInRegistry

        [Theory]
        [InlineData(KlingonGrandfathered), InlineData(BurmeseBurma)]
        public void Create_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValidInRegistry_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            testResult.Should().Be(testLanguageTag, "the Subtag Registry has a record for every subtag of '{0}'", testLanguageTag);
        }

        [Theory]
        [InlineData(KlingonGrandfathered), InlineData(BurmeseBurma)]
        public void TryCreate_WithLanguageTagAndSubtagRegistryWhenLanguageTagIsValidInRegistry_ReturnsTrue(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = (Bcp47LanguageTag.TryCreate(testLanguageTag, subtagRegistry, out var languageTag), (string)languageTag);

            // assert
            testResult.Should().Be((true, testLanguageTag), "the Subtag Registry has a record for every subtag of '{0}'", testLanguageTag);
        }

        #endregion

        #region Test: CreateAndTryCreate_WithLanguageTagAndSparseSubtagRegistryWhenLanguageTagHasEmptySubtag

        /// <summary>
        /// A Subtag Registry with one language and one region record, and no grandfathered, script or variant records.
        /// </summary>
        private const string SparseSubtagRegistryContent =
            "File-Date: 2026-10-21\n" +
            "%%\nType: language\nSubtag: en\nDescription: English\nAdded: 2005-10-16\n" +
            "%%\nType: region\nSubtag: US\nDescription: United States\nAdded: 2005-10-16";

        [Theory]
        [InlineData(EmptyString), InlineData(EnglishWithTrailingHyphen), InlineData(EnglishWithEmptySubtag), InlineData(EnglishUnitedStatesWithTrailingHyphen)]
        public void Create_WithLanguageTagAndSparseSubtagRegistryWhenLanguageTagHasEmptySubtag_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = SubtagRegistry.SubtagRegistry.CreateFromContent(SparseSubtagRegistryContent);

            // act
            Action testAction = () => _ = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("an empty subtag is not a subtag of any kind, even when the Subtag Registry has no records of that kind (RFC 5646 §2.1)")
                .WithMessage(InvalidFormatMessage);
        }

        [Theory]
        [InlineData(EmptyString), InlineData(EnglishWithTrailingHyphen), InlineData(EnglishWithEmptySubtag), InlineData(EnglishUnitedStatesWithTrailingHyphen)]
        public void TryCreate_WithLanguageTagAndSparseSubtagRegistryWhenLanguageTagHasEmptySubtag_ReturnsFalse(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = SubtagRegistry.SubtagRegistry.CreateFromContent(SparseSubtagRegistryContent);

            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, subtagRegistry, out _);

            // assert
            testResult.Should().BeFalse("'{0}' has an empty subtag", testLanguageTag);
        }

        [Theory]
        [InlineData(EnglishLanguageTag), InlineData(EnglishUnitedStates)]
        public void Create_WithLanguageTagAndSparseSubtagRegistryWhenEverySubtagIsRegistered_ReturnsBcp47LanguageTag(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = SubtagRegistry.SubtagRegistry.CreateFromContent(SparseSubtagRegistryContent);

            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            testResult.Should().Be(testLanguageTag, "the Subtag Registry has a record for every subtag of '{0}'", testLanguageTag);
        }

        #endregion
    }
}
