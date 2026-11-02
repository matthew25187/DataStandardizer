#if NETCOREAPP3_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.IntegrationTests
{
    /// <summary>
    /// Checks that validating a span, and failing to create a tag from one, allocate nothing once the code lists are built (#229).
    /// </summary>
    /// <remarks>
    /// These measure what the runtime allocates, which depends on the JIT and the target framework rather than only on the package, so they are
    /// integration tests. They run on the frameworks that have <see cref="GC.GetAllocatedBytesForCurrentThread"/>.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Bcp47LanguageTagAllocationTests
    {
        private delegate bool SpanCheck(ReadOnlySpan<char> subtag);

        /// <summary>
        /// Strings that are well-formed language tags (RFC 5646 §2.1).
        /// </summary>
        private static readonly string[] ValidLanguageTags =
        {
            "en", "es-419", "zh-Hant-TW", "zh-yue-HK", "sr-Latn-RS", "de-CH-1901", "sl-rozaj-biske-1994", "hy-Latn-IT-arevela", "gsw-u-sd-chzh",
            "he-IL-u-ca-hebrew-tz-jeruslm", "en-US-x-twain", "x-private", "i-klingon", "zh-min-nan", "EN-us"
        };

        /// <summary>
        /// Strings that are not well-formed language tags (RFC 5646 §2.1).
        /// </summary>
        private static readonly string[] InvalidLanguageTags =
        {
            "", "e", "en-", "-en", "en--US", "abcdefghi", "en-US-US", "zh-Hant-Hans", "de-CH-123456789", "en-US-a", "en-x-", "x-abcdefghi"
        };

        /// <summary>
        /// Strings that are well-formed but repeat a subtag (RFC 5646 §2.2.5¶5, §2.2.6¶3), or use a subtag in no list, so the ISO code lists opt-in rejects them.
        /// </summary>
        private static readonly string[] NotInIsoCodeLists =
        {
            "de-1901-1901", "en-a-bbb-a-ccc", "zz", "en-Abcd", "en-830", "eng"
        };

        /// <summary>
        /// Subtags that between them are valid and invalid for every check.
        /// </summary>
        private static readonly string[] Subtags =
        {
            "", "-", "a", "x", "en", "EN", "zh", "qaa", "yue", "YUE", "abc", "Hant", "hant", "Zzzz", "TW", "tw", "419", "999", "AA", "CQ",
            "1994", "rozaj", "1abc", "abcdefghi", "u-ca-hebrew", "u-ca-hebrew-tz-jeruslm", "u-a", "u", "x-private", "X-a-b", "x-", "x-abcdefghi",
            "u-ca-x-a", "en\n", "\x0130"
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
        /// One call for each span validation: of each valid tag, of each invalid tag by <see cref="Bcp47LanguageTag.TryCreate(ReadOnlySpan{char}, out Bcp47LanguageTag)"/>,
        /// and of each subtag by each check.
        /// </summary>
        public static TheoryData<string, Action> SpanValidations
        {
            get
            {
                var data = new TheoryData<string, Action>();
                foreach (var languageTag in ValidLanguageTags)
                {
                    data.Add("IsWellFormed(\"" + languageTag + "\")", () => Bcp47LanguageTag.IsWellFormed(languageTag.AsSpan()));
                }

                foreach (var languageTag in NotInIsoCodeLists)
                {
                    data.Add("TryCreate(\"" + languageTag + "\", IsoCodeLists)", () => Bcp47LanguageTag.TryCreate(languageTag.AsSpan(), LanguageTagConformance.IsoCodeLists, out _));
                    data.Add("IsValid(\"" + languageTag + "\", IsoCodeLists)", () => Bcp47LanguageTag.IsValid(languageTag.AsSpan(), LanguageTagConformance.IsoCodeLists));
                }

                foreach (var languageTag in InvalidLanguageTags)
                {
                    data.Add("TryCreate(\"" + languageTag + "\")", () => Bcp47LanguageTag.TryCreate(languageTag.AsSpan(), out _));
                }

                foreach (var check in SpanChecks)
                foreach (var subtag in Subtags)
                {
                    var spanCheck = check.Value;
                    data.Add(check.Key + "(\"" + subtag + "\")", () => spanCheck(subtag.AsSpan()));
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(SpanValidations), DisableDiscoveryEnumeration = true)]
        public void SpanValidation_WithWarmCaches_DoesNotAllocate(string validation, Action validate)
        {
            // act
            var testResult = AllocationMeter.MeasureAllocatedBytes(validate);

            // assert
            testResult.Should().Be(0, "{0} must not allocate", validation);
        }
    }
}
#endif
