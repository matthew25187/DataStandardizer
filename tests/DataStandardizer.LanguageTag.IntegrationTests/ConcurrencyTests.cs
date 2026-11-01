using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DataStandardizer.LanguageTag.SubtagRegistry;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.IntegrationTests
{
    /// <summary>
    /// Tests that a Subtag Registry, and the language tags and records read from one, give every thread the same results when several threads use
    /// them at once.
    /// </summary>
    /// <remarks>
    /// What these exercise depends on how the thread scheduler interleaves the threads, so they are integration tests. Each starts from a new
    /// copy of the registry, so that the threads are the first to use it while its index and its records' lists are still being built.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class ConcurrencyTests
    {
        private const int ThreadCount = 8;

        /// <summary>
        /// The variant subtag for the standardized Resian orthography of 1994 (RFC 5646 §2.2.5), a record with several Prefix fields.
        /// </summary>
        private const string NineteenNinetyFourVariantSubtag = "1994";

        /// <summary>
        /// Language tags each with whether it is valid against the IANA registry fixture.
        /// </summary>
        public static TheoryData<string, bool> LanguageTagsWithValidity => new TheoryData<string, bool>
        {
            { "en", true },
            { "es-419", true },
            { "sr-Cyrl", true },
            { "yue-Hant-HK", true },
            { "gsw-u-sd-chzh", true },
            { "i-klingon", true },
            { "zh-min-nan", true },
            { "qaa-Qaaa-QM-x-southern", true },
            { "sl-rozaj-biske-1994", true },
            { "de-CH-1901-u-co-phonebk", true },
            { "x-private", true },
            { "", false },
            { "en-", false },
            { "invalid", false },
            { "en-US-US", false },
            { "de-1901-1901", false },
            { "en-a-bb-a-cc", false }
        };

        /// <summary>
        /// Language tags that are valid against the IANA registry fixture, each with its Primary Language subtag (RFC 5646 §2.2.1).
        /// </summary>
        public static TheoryData<string, string> LanguageTagsWithPrimaryLanguageSubtag => new TheoryData<string, string>
        {
            { "en", "en" },
            { "yue-Hant-HK", "yue" },
            { "sl-rozaj-biske-1994", "sl" },
            { "i-klingon", null },
            { "x-private", null }
        };

        [Theory]
        [MemberData(nameof(LanguageTagsWithValidity))]
        public void TryCreate_WithLanguageTagAndSubtagRegistryFromSeveralThreads_AllThreadsGetTheSameResult(string testLanguageTag, bool expectedResult)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.CreateSubtagRegistry();

            // act
            var testResult = ConcurrentRunner.Run(ThreadCount, () => Bcp47LanguageTag.TryCreate(testLanguageTag, subtagRegistry, out _));

            // assert
            testResult.Should().OnlyContain(outcome => outcome.Exception == null && outcome.Value == expectedResult,
                "checking '{0}' against a shared Subtag Registry is thread-safe", testLanguageTag);
        }

        [Theory]
        [MemberData(nameof(LanguageTagsWithPrimaryLanguageSubtag))]
        public void Create_WithLanguageTagAndSubtagRegistryFromSeveralThreads_AllThreadsGetTheSamePrimaryLanguageSubtag(string testLanguageTag, string expectedResult)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.CreateSubtagRegistry();

            // act
            var testResult = ConcurrentRunner.Run(ThreadCount, () => Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry).PrimaryLanguageSubtag);

            // assert
            testResult.Should().OnlyContain(outcome => outcome.Exception == null && outcome.Value == expectedResult,
                "creating '{0}' against a shared Subtag Registry is thread-safe", testLanguageTag);
        }

        /// <summary>
        /// Each list of a record that the record builds the first time it is read, with what the IANA registry fixture has for the variant subtag 1994.
        /// </summary>
        public static TheoryData<string, Func<SubtagRegistrySubtagRecord, IReadOnlyList<string>>, string[]> RecordLists =>
            new TheoryData<string, Func<SubtagRegistrySubtagRecord, IReadOnlyList<string>>, string[]>
            {
                { nameof(SubtagRegistrySubtagRecord.Description), record => record.Description, new[] { "Standardized Resian orthography" } },
                { nameof(SubtagRegistrySubtagRecord.Prefix), record => record.Prefix, new[] { "sl-rozaj", "sl-rozaj-biske", "sl-rozaj-njiva", "sl-rozaj-osojs", "sl-rozaj-solba" } },
                { nameof(SubtagRegistrySubtagRecord.Comments), record => record.Comments, new[] { "For standardized Resian an orthography was published in 1994." } }
            };

        [Theory]
        [MemberData(nameof(RecordLists), DisableDiscoveryEnumeration = true)]
        public void TagRecordLists_WhenReadFromSeveralThreads_ReturnSameValues(string listName, Func<SubtagRegistrySubtagRecord, IReadOnlyList<string>> list, string[] expectedResult)
        {
            // arrange
            IanaSubtagRegistryResource.CreateSubtagRegistry().TryGetSubtag(SubtagRegistryRecordType.Variant, NineteenNinetyFourVariantSubtag, out var record);

            // act
            var testResult = ConcurrentRunner.Run(ThreadCount, () => list(record));

            // assert
            testResult.Should().OnlyContain(outcome => outcome.Exception == null && outcome.Value.SequenceEqual(expectedResult),
                "reading the {0} list of a record is thread-safe", listName);
        }
    }
}
