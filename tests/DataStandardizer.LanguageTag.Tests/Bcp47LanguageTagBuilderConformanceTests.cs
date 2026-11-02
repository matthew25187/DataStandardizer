using System;
using System.Diagnostics.CodeAnalysis;
using DataStandardizer.Language;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    /// <summary>
    /// Tests of what the builder checks: the string steps check shape, and <c>Build</c> applies the registry, the chosen conformance, or well-formedness (#505).
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Bcp47LanguageTagBuilderConformanceTests
    {
        #region Declarations

        /// <summary>
        /// A Primary Language subtag with the shape of RFC 5646 §2.1 that is not an ISO 639 code or in the IANA registry.
        /// </summary>
        private const string UnlistedPrimaryLanguage = "zz";

        /// <summary>
        /// A Script subtag with the shape of RFC 5646 §2.1 that is not an ISO 15924 code or in the IANA registry.
        /// </summary>
        private const string UnlistedScript = "Abcd";

        /// <summary>
        /// The language tag built from <see cref="UnlistedPrimaryLanguage"/> and <see cref="UnlistedScript"/>.
        /// </summary>
        private const string UnlistedLanguageTag = "zz-Abcd";

        /// <summary>
        /// The Region subtag 830 (Channel Islands), which has the shape of a region (RFC 5646 §2.1) and may be registered but is not (§2.2.4¶4E-F).
        /// </summary>
        private const string ChannelIslandsRegion = "830";

        /// <summary>
        /// English as used in the United States, an example from RFC 5646 Appendix A.
        /// </summary>
        private const string EnglishUnitedStates = "en-US";

        private const string EnglishPrimaryLanguage = "en";
        private const string UnitedStatesRegion = "US";

        /// <summary>
        /// The ISO 639-2 code for English, which only the grammar accepts as a Primary Language subtag, because English has the ISO 639-1 code <c>en</c> (RFC 5646 §2.2.1).
        /// </summary>
        private const string EnglishThreeLetterCode = "eng";

        /// <summary>
        /// A registry that has only the subtags that no ISO list has.
        /// </summary>
        private const string RegistryOfUnlistedSubtags =
            "File-Date: 2026-11-02\n%%\nType: language\nSubtag: zz\nDescription: Test language\nAdded: 2026-11-02\n%%\nType: script\nSubtag: Abcd\nDescription: Test script\nAdded: 2026-11-02\n";

        /// <summary>
        /// A value that <see cref="LanguageTagConformance"/> doesn't define.
        /// </summary>
        private const LanguageTagConformance UndefinedConformance = (LanguageTagConformance)99;

        private const string ConformanceParameterName = "conformance";
        private const string NotInIsoCodeListsMessage = "The language tag is not well-formed, or is not in the ISO code lists.";
        private const string NotValidMessage = "The language tag is not valid against the subtag registry.";

        #endregion

        #region Test: no conformance chosen

        [Fact]
        public void Build_WithUnlistedStringSubtags_ProducesWellFormedLanguageTag()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(UnlistedPrimaryLanguage).UsingScriptSubtag(UnlistedScript);

            // act
            var languageTag = builder.Build();
            var testResult = ((string)languageTag, languageTag.Conformance);

            // assert
            testResult.Should().Be((UnlistedLanguageTag, LanguageTagConformance.WellFormed), "the string steps check shape, and with nothing chosen Build checks the grammar");
        }

        [Fact]
        public void Build_WithChannelIslandsRegionString_ProducesWellFormedLanguageTag()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(EnglishPrimaryLanguage).UsingRegionSubtag(ChannelIslandsRegion);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be("en-830", "830 has the shape region = 3DIGIT (RFC 5646 §2.1)");
        }

        [Fact]
        public void Build_WithIso639Part2TCodeOfLanguageWithPart1Code_ProducesWellFormedLanguageTag()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(Iso639Part2TLanguage.eng);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(EnglishThreeLetterCode, "only the ISO code lists look a code up");
        }

        #endregion

        #region Test: WithConformance

        [Fact]
        public void Build_WithIsoCodeListsAndUnlistedStringSubtags_ThrowsLanguageTagFormatException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().WithConformance(LanguageTagConformance.IsoCodeLists).UsingPrimaryLanguageSubtag(UnlistedPrimaryLanguage).UsingScriptSubtag(UnlistedScript);

            // act
            Action testAction = () => builder.Build();

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("neither subtag is in the ISO code lists").WithMessage(NotInIsoCodeListsMessage);
        }

        [Fact]
        public void Build_WithIsoCodeListsAndChannelIslandsRegionString_ThrowsLanguageTagFormatException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().WithConformance(LanguageTagConformance.IsoCodeLists).UsingPrimaryLanguageSubtag(EnglishPrimaryLanguage).UsingRegionSubtag(ChannelIslandsRegion);

            // act
            Action testAction = () => builder.Build();

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("830 may be registered but is not (RFC 5646 §2.2.4¶4E-F)").WithMessage(NotInIsoCodeListsMessage);
        }

        [Fact]
        public void Build_WithIsoCodeListsAndIso639Part2TCodeOfLanguageWithPart1Code_ThrowsLanguageTagFormatException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().WithConformance(LanguageTagConformance.IsoCodeLists).UsingPrimaryLanguageSubtag(Iso639Part2TLanguage.eng);

            // act
            Action testAction = () => builder.Build();

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("only the ISO 639-1 code of English is a Primary Language subtag (RFC 5646 §2.2.1)").WithMessage(NotInIsoCodeListsMessage);
        }

        [Fact]
        public void Build_WithIsoCodeListsAndUnlistedLanguageTag_ThrowsLanguageTagFormatException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().WithConformance(LanguageTagConformance.IsoCodeLists).UsingLanguageTag(UnlistedLanguageTag);

            // act
            Action testAction = () => builder.Build();

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("the whole-tag step applies the chosen conformance too").WithMessage(NotInIsoCodeListsMessage);
        }

        [Fact]
        public void Build_WithIsoCodeListsAndListedSubtags_ReportsIsoCodeLists()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().WithConformance(LanguageTagConformance.IsoCodeLists).UsingPrimaryLanguageSubtag(EnglishPrimaryLanguage).UsingRegionSubtag(UnitedStatesRegion);

            // act
            var languageTag = builder.Build();
            var testResult = ((string)languageTag, languageTag.Conformance);

            // assert
            testResult.Should().Be((EnglishUnitedStates, LanguageTagConformance.IsoCodeLists));
        }

        [Fact]
        public void WithConformance_WithValid_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.WithConformance(LanguageTagConformance.Valid);

            // assert
            testAction.Should().ThrowExactly<ArgumentException>("validity needs a registry").WithParameterName(ConformanceParameterName);
        }

        [Fact]
        public void WithConformance_WithUndefinedValue_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.WithConformance(UndefinedConformance);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName(ConformanceParameterName);
        }

        #endregion

        #region Test: WithLanguageSubtagRegistry

        [Fact]
        public void Build_WithRegistryAndSubtagsOnlyTheRegistryHas_ReportsValid()
        {
            // arrange
            var subtagRegistry = SubtagRegistry.SubtagRegistry.CreateFromContent(RegistryOfUnlistedSubtags);
            var builder = new Bcp47LanguageTagBuilder().WithLanguageSubtagRegistry(subtagRegistry).UsingPrimaryLanguageSubtag(UnlistedPrimaryLanguage).UsingScriptSubtag(UnlistedScript);

            // act
            var languageTag = builder.Build();
            var testResult = ((string)languageTag, languageTag.Conformance);

            // assert
            testResult.Should().Be((UnlistedLanguageTag, LanguageTagConformance.Valid), "the string steps must not reject a subtag that is in the caller's registry");
        }

        [Fact]
        public void Build_WithRegistryAndSubtagsItDoesNotHave_ThrowsLanguageTagFormatException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().WithLanguageSubtagRegistry(IanaSubtagRegistryResource.Shared).UsingPrimaryLanguageSubtag(UnlistedPrimaryLanguage).UsingScriptSubtag(UnlistedScript);

            // act
            Action testAction = () => builder.Build();

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("the shape is right, but Build checks validity against the registry").WithMessage(NotValidMessage);
        }

        #endregion

        #region Test: the last of the two steps applies

        [Fact]
        public void Build_WithConformanceAfterRegistry_AppliesTheConformance()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder();
            builder.WithLanguageSubtagRegistry(IanaSubtagRegistryResource.Shared);
            var next = builder.WithConformance(LanguageTagConformance.WellFormed).UsingLanguageTag(UnlistedLanguageTag);

            // act
            var testResult = next.Build().Conformance;

            // assert
            testResult.Should().Be(LanguageTagConformance.WellFormed);
        }

        [Fact]
        public void Build_WithRegistryAfterConformance_AppliesTheRegistry()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder();
            builder.WithConformance(LanguageTagConformance.IsoCodeLists);
            var next = builder.WithLanguageSubtagRegistry(IanaSubtagRegistryResource.Shared).UsingLanguageTag(EnglishUnitedStates);

            // act
            var testResult = next.Build().Conformance;

            // assert
            testResult.Should().Be(LanguageTagConformance.Valid);
        }

        #endregion
    }
}
