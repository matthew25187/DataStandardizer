using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    /// <summary>
    /// Tests of the conformance classes of RFC 5646 §2.2.9: what well-formed, valid and the ISO code lists opt-in each accept (#502, #503).
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Bcp47LanguageTagConformanceTests
    {
        #region Declarations

        /// <summary>
        /// A two-letter Primary Language subtag that the ABNF allows (RFC 5646 §2.1) but that is not an ISO 639-1 code or in the registry.
        /// </summary>
        private const string UnassignedPrimaryLanguage = "zz";

        /// <summary>
        /// A four-letter Script subtag that the ABNF allows (RFC 5646 §2.1) but that is not an ISO 15924 code or in the registry.
        /// </summary>
        private const string UnassignedScript = "en-Abcd";

        /// <summary>
        /// The Region subtag 830 (Channel Islands), which has the shape of a region (RFC 5646 §2.1) and may be registered but is not (§2.2.4¶4E-F).
        /// </summary>
        private const string ChannelIslandsRegion = "en-830";

        /// <summary>
        /// A private use tag of one letter (RFC 5646 §2.1, §2.2.7).
        /// </summary>
        private const string PrivateUseOneLetter = "x-a";

        /// <summary>
        /// The irregular grandfathered tag for Klingon (RFC 5646 §2.1, §2.2.8).
        /// </summary>
        private const string KlingonGrandfathered = "i-klingon";

        /// <summary>
        /// A repeated variant subtag, which matches the ABNF (RFC 5646 §2.1) and is not valid (§2.2.9).
        /// </summary>
        private const string RepeatedVariant = "de-1901-1901";

        /// <summary>
        /// A repeated extension singleton, which matches the ABNF (RFC 5646 §2.1) and is not valid (§2.2.9).
        /// </summary>
        private const string RepeatedSingleton = "en-a-bb-a-cc";

        /// <summary>
        /// A singleton followed by a one-character subtag, where <c>extension = singleton 1*("-" (2*8alphanum))</c> requires two to eight (RFC 5646 §2.1).
        /// </summary>
        private const string SingletonWithOneCharacterSubtag = "en-a-x-a-y";

        /// <summary>
        /// English as used in the United States, an example from RFC 5646 Appendix A, whose subtags are all registered.
        /// </summary>
        private const string EnglishUnitedStates = "en-US";

        /// <summary>
        /// Not a language tag under any class: an empty subtag (RFC 5646 §2.1).
        /// </summary>
        private const string EmptySubtag = "en--US";

        /// <summary>
        /// The ISO 639-2 and ISO 639-3 code for English, which has the ISO 639-1 code <c>en</c>, so only <c>en</c> is defined (RFC 5646 §2.2.1).
        /// </summary>
        private const string EnglishThreeLetterCode = "eng";

        /// <summary>
        /// The ISO 639-2/T and ISO 639-3 code for German, which has the ISO 639-1 code <c>de</c> (RFC 5646 §2.2.1).
        /// </summary>
        private const string GermanThreeLetterCode = "deu";

        /// <summary>
        /// The ISO 639-2/T and ISO 639-3 code for French, which has the ISO 639-1 code <c>fr</c> (RFC 5646 §2.2.1).
        /// </summary>
        private const string FrenchThreeLetterCode = "fra";

        /// <summary>
        /// The ISO 639-3 code for Serbo-Croatian. The registry defines <c>sh</c> for this language and not this code, so the ISO code lists opt-in rejects it.
        /// </summary>
        private const string SerboCroatianThreeLetterCode = "hbs";

        /// <summary>
        /// The ISO 639-2 code for Asturian, which has no ISO 639-1 code (RFC 5646 §2.2.1¶2A).
        /// </summary>
        private const string AsturianThreeLetterCode = "ast";

        /// <summary>
        /// The ISO 639-2 code for Hawaiian, which has no ISO 639-1 code (RFC 5646 §2.2.1¶2A).
        /// </summary>
        private const string HawaiianThreeLetterCode = "haw";

        /// <summary>
        /// The ISO 639-2 code for multiple languages (RFC 5646 §4.1).
        /// </summary>
        private const string MultipleLanguagesCode = "mul";

        /// <summary>
        /// The ISO 639-2 code for undetermined languages (RFC 5646 §4.1).
        /// </summary>
        private const string UndeterminedLanguageCode = "und";

        /// <summary>
        /// The first code reserved for local use (RFC 5646 §2.2.1¶3).
        /// </summary>
        private const string FirstLocalUseCode = "qaa";

        /// <summary>
        /// The ISO 639-3 code for Cantonese, which has no ISO 639-1 code (RFC 5646 §2.2.1¶2B).
        /// </summary>
        private const string CantoneseThreeLetterCode = "yue";

        /// <summary>
        /// The ISO 639-5 code for the Indo-European languages (RFC 5646 §2.2.1¶2C).
        /// </summary>
        private const string IndoEuropeanFamilyCode = "ine";

        /// <summary>
        /// The ISO 15924 code for an uncoded script, which RFC 5646 §2.2.3 takes from that list, so the ISO code lists accept it.
        /// </summary>
        private const string UncodedScript = "en-Zzzz";

        /// <summary>
        /// A value that <see cref="LanguageTagConformance"/> doesn't define.
        /// </summary>
        private const LanguageTagConformance UndefinedConformance = (LanguageTagConformance)99;

        private const string NotWellFormedMessage = "The language tag is not well-formed.";
        private const string NotValidMessage = "The language tag is not valid against the subtag registry.";
        private const string NotInIsoCodeListsMessage = "The language tag is not well-formed, or is not in the ISO code lists.";
        private const string NeedsRegistryMessagePattern = "Validity needs a subtag registry. Use the overload that takes a SubtagRegistry.*";
        private const string LanguageTagNotInitialisedMessage = "The Bcp47LanguageTag instance is not initialised.";
        private const string KeyedSubtagNotInitialisedMessage = "The Bcp47KeyedSubtag instance is not initialised.";
        private const string ConformanceParameterName = "conformance";
        private const string LanguageTagParameterName = "languageTag";
        private const string SubtagRegistryParameterName = "subtagRegistry";

        #endregion

        #region Test cases

        /// <summary>
        /// Tags that match the ABNF but that nothing has assigned, or that repeat a subtag, or that §2.1 lists: each is well-formed (RFC 5646 §2.2.9).
        /// </summary>
        public static TheoryData<string> WellFormedLanguageTags => new TheoryData<string>
        {
            UnassignedPrimaryLanguage, UnassignedScript, ChannelIslandsRegion, PrivateUseOneLetter, KlingonGrandfathered, RepeatedVariant, RepeatedSingleton, EnglishUnitedStates
        };

        /// <summary>
        /// Tags that are well-formed but not valid against the registry fixture: unregistered subtags, or a repeated variant or singleton (RFC 5646 §2.2.9).
        /// </summary>
        public static TheoryData<string> NotValidLanguageTags => new TheoryData<string>
        {
            UnassignedPrimaryLanguage, UnassignedScript, ChannelIslandsRegion, RepeatedVariant, RepeatedSingleton
        };

        /// <summary>
        /// Tags that are valid against the registry fixture (RFC 5646 §2.2.9).
        /// </summary>
        public static TheoryData<string> ValidLanguageTags => new TheoryData<string>
        {
            EnglishUnitedStates, KlingonGrandfathered, PrivateUseOneLetter
        };

        /// <summary>
        /// Tags that the ISO code lists opt-in rejects although they are well-formed.
        /// </summary>
        public static TheoryData<string> NotInIsoCodeListsLanguageTags => new TheoryData<string>
        {
            UnassignedPrimaryLanguage, UnassignedScript, ChannelIslandsRegion, RepeatedVariant, RepeatedSingleton, EnglishThreeLetterCode, GermanThreeLetterCode, FrenchThreeLetterCode, SerboCroatianThreeLetterCode
        };

        /// <summary>
        /// Three-letter codes of languages that have an ISO 639-1 code, which only the grammar accepts (RFC 5646 §2.2.1).
        /// </summary>
        public static TheoryData<string> ThreeLetterCodesWithPart1Equivalent => new TheoryData<string>
        {
            EnglishThreeLetterCode, GermanThreeLetterCode, FrenchThreeLetterCode, SerboCroatianThreeLetterCode
        };

        /// <summary>
        /// Tags that the ISO code lists opt-in accepts.
        /// </summary>
        public static TheoryData<string> InIsoCodeListsLanguageTags => new TheoryData<string>
        {
            EnglishUnitedStates, KlingonGrandfathered, PrivateUseOneLetter, AsturianThreeLetterCode, HawaiianThreeLetterCode, MultipleLanguagesCode, UndeterminedLanguageCode,
            FirstLocalUseCode, CantoneseThreeLetterCode, IndoEuropeanFamilyCode, UncodedScript
        };

        /// <summary>
        /// Each way to create a language tag, with the conformance it must report.
        /// </summary>
        public static TheoryData<string, Func<Bcp47LanguageTag>, LanguageTagConformance> CreationPaths => new TheoryData<string, Func<Bcp47LanguageTag>, LanguageTagConformance>
        {
            { "Create(string)", () => Bcp47LanguageTag.Create(EnglishUnitedStates), LanguageTagConformance.WellFormed },
            { "TryCreate(string)", () => TryCreateFromString(EnglishUnitedStates), LanguageTagConformance.WellFormed },
            { "TryCreate(span)", () => TryCreateFromSpan(EnglishUnitedStates), LanguageTagConformance.WellFormed },
            { "explicit operator", () => (Bcp47LanguageTag)EnglishUnitedStates, LanguageTagConformance.WellFormed },
            { "Create(string, WellFormed)", () => Bcp47LanguageTag.Create(EnglishUnitedStates, LanguageTagConformance.WellFormed), LanguageTagConformance.WellFormed },
            { "TryCreate(string, WellFormed)", () => TryCreateFromString(EnglishUnitedStates, LanguageTagConformance.WellFormed), LanguageTagConformance.WellFormed },
            { "TryCreate(span, WellFormed)", () => TryCreateFromSpan(EnglishUnitedStates, LanguageTagConformance.WellFormed), LanguageTagConformance.WellFormed },
            { "Create(string, IsoCodeLists)", () => Bcp47LanguageTag.Create(EnglishUnitedStates, LanguageTagConformance.IsoCodeLists), LanguageTagConformance.IsoCodeLists },
            { "TryCreate(string, IsoCodeLists)", () => TryCreateFromString(EnglishUnitedStates, LanguageTagConformance.IsoCodeLists), LanguageTagConformance.IsoCodeLists },
            { "TryCreate(span, IsoCodeLists)", () => TryCreateFromSpan(EnglishUnitedStates, LanguageTagConformance.IsoCodeLists), LanguageTagConformance.IsoCodeLists },
            { "Create(string, SubtagRegistry)", () => Bcp47LanguageTag.Create(EnglishUnitedStates, IanaSubtagRegistryResource.Shared), LanguageTagConformance.Valid },
            { "TryCreate(string, SubtagRegistry)", () => TryCreateFromString(EnglishUnitedStates, IanaSubtagRegistryResource.Shared), LanguageTagConformance.Valid },
            { "TryCreate(span, SubtagRegistry)", () => TryCreateFromSpan(EnglishUnitedStates, IanaSubtagRegistryResource.Shared), LanguageTagConformance.Valid },
#if NET7_0_OR_GREATER
            { "Parse(string)", () => Bcp47LanguageTag.Parse(EnglishUnitedStates, null), LanguageTagConformance.WellFormed },
            { "Parse(span)", () => Bcp47LanguageTag.Parse(EnglishUnitedStates.AsSpan(), null), LanguageTagConformance.WellFormed },
#endif
        };

        /// <summary>
        /// Each overload that takes a conformance, called with one.
        /// </summary>
        private static TheoryData<string, Action> ConformanceCalls(LanguageTagConformance conformance) => new TheoryData<string, Action>
        {
            { "Create(string)", () => Bcp47LanguageTag.Create(EnglishUnitedStates, conformance) },
            { "TryCreate(string)", () => Bcp47LanguageTag.TryCreate(EnglishUnitedStates, conformance, out _) },
            { "TryCreate(null string)", () => Bcp47LanguageTag.TryCreate((string)null, conformance, out _) },
            { "TryCreate(span)", () => Bcp47LanguageTag.TryCreate(EnglishUnitedStates.AsSpan(), conformance, out _) },
            { "IsValid(string)", () => Bcp47LanguageTag.IsValid(EnglishUnitedStates, conformance) },
            { "IsValid(span)", () => Bcp47LanguageTag.IsValid(EnglishUnitedStates.AsSpan(), conformance) },
            { "IsValid on an instance", () => Bcp47LanguageTag.Create(EnglishUnitedStates).IsValid(conformance) }
        };

        public static TheoryData<string, Action> CallsWithValidConformance => ConformanceCalls(LanguageTagConformance.Valid);

        public static TheoryData<string, Action> CallsWithUndefinedConformance => ConformanceCalls(UndefinedConformance);

        /// <summary>
        /// Each member of an uninitialised language tag that describes the tag and that this issue added.
        /// </summary>
        public static TheoryData<string, Action> UninitialisedLanguageTagMembers => new TheoryData<string, Action>
        {
            { "default: Conformance", () => _ = default(Bcp47LanguageTag).Conformance },
            { "new: Conformance", () => _ = new Bcp47LanguageTag().Conformance },
            { "default: IsValid", () => default(Bcp47LanguageTag).IsValid(IanaSubtagRegistryResource.Shared) },
            { "new: IsValid", () => new Bcp47LanguageTag().IsValid(IanaSubtagRegistryResource.Shared) },
            { "default: IsValid(conformance)", () => default(Bcp47LanguageTag).IsValid(LanguageTagConformance.IsoCodeLists) },
            { "new: IsValid(conformance)", () => new Bcp47LanguageTag().IsValid(LanguageTagConformance.IsoCodeLists) }
        };

        /// <summary>
        /// Each member of an uninitialised keyed subtag that describes the subtag.
        /// </summary>
        public static TheoryData<string, Action> UninitialisedKeyedSubtagMembers => new TheoryData<string, Action>
        {
            { "Singleton", () => _ = default(Bcp47KeyedSubtag).Singleton },
            { "Subtags", () => _ = default(Bcp47KeyedSubtag).Subtags }
        };

        private static Bcp47LanguageTag TryCreateFromString(string languageTag)
        {
            Bcp47LanguageTag.TryCreate(languageTag, out var result);
            return result;
        }

        private static Bcp47LanguageTag TryCreateFromString(string languageTag, LanguageTagConformance conformance)
        {
            Bcp47LanguageTag.TryCreate(languageTag, conformance, out var result);
            return result;
        }

        private static Bcp47LanguageTag TryCreateFromString(string languageTag, SubtagRegistry.SubtagRegistry subtagRegistry)
        {
            Bcp47LanguageTag.TryCreate(languageTag, subtagRegistry, out var result);
            return result;
        }

        private static Bcp47LanguageTag TryCreateFromSpan(string languageTag)
        {
            Bcp47LanguageTag.TryCreate(languageTag.AsSpan(), out var result);
            return result;
        }

        private static Bcp47LanguageTag TryCreateFromSpan(string languageTag, LanguageTagConformance conformance)
        {
            Bcp47LanguageTag.TryCreate(languageTag.AsSpan(), conformance, out var result);
            return result;
        }

        private static Bcp47LanguageTag TryCreateFromSpan(string languageTag, SubtagRegistry.SubtagRegistry subtagRegistry)
        {
            Bcp47LanguageTag.TryCreate(languageTag.AsSpan(), subtagRegistry, out var result);
            return result;
        }

        #endregion

        #region Test: well-formed

        [Theory]
        [MemberData(nameof(WellFormedLanguageTags))]
        public void Create_WithWellFormedLanguageTag_KeepsTheString(string testLanguageTag)
        {
            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag);

            // assert
            testResult.Should().Be(testLanguageTag, "creation without a registry checks the ABNF only (RFC 5646 §2.2.9)");
        }

        [Theory]
        [MemberData(nameof(WellFormedLanguageTags))]
        public void TryCreate_WithWellFormedLanguageTag_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, out _);

            // assert
            testResult.Should().BeTrue("creation without a registry checks the ABNF only (RFC 5646 §2.2.9)");
        }

        [Theory]
        [MemberData(nameof(WellFormedLanguageTags))]
        public void IsWellFormed_WithWellFormedLanguageTag_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormed(testLanguageTag);

            // assert
            testResult.Should().BeTrue("the language tag matches the ABNF (RFC 5646 §2.2.9)");
        }

        [Theory]
        [MemberData(nameof(WellFormedLanguageTags))]
        public void IsWellFormed_WithWellFormedSpan_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormed(testLanguageTag.AsSpan());

            // assert
            testResult.Should().BeTrue("the language tag matches the ABNF (RFC 5646 §2.2.9)");
        }

        [Theory]
        [InlineData(SingletonWithOneCharacterSubtag), InlineData(EmptySubtag)]
        public void IsWellFormed_WithLanguageTagOutsideTheAbnf_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormed(testLanguageTag);

            // assert
            testResult.Should().BeFalse("the language tag does not match the ABNF (RFC 5646 §2.1)");
        }

        [Fact]
        public void Create_WithSingletonFollowedByOneCharacterSubtag_ThrowsLanguageTagFormatException()
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(SingletonWithOneCharacterSubtag);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("an extension needs subtags of two to eight characters (RFC 5646 §2.1)").WithMessage(NotWellFormedMessage);
        }

        [Fact]
        public void Create_WithWellFormedConformanceAndLanguageTagOutsideTheAbnf_ThrowsLanguageTagFormatException()
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(EmptySubtag, LanguageTagConformance.WellFormed);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>().WithMessage(NotWellFormedMessage);
        }

        #endregion

        #region Test: valid

        [Theory]
        [MemberData(nameof(ValidLanguageTags))]
        public void IsValid_WithRegistryAndValidLanguageTag_ReturnsTrue(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = Bcp47LanguageTag.IsValid(testLanguageTag, subtagRegistry);

            // assert
            testResult.Should().BeTrue("the language tag satisfies RFC 5646 §2.2.9");
        }

        [Theory]
        [MemberData(nameof(NotValidLanguageTags))]
        public void IsValid_WithRegistryAndLanguageTagThatIsNotValid_ReturnsFalse(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = Bcp47LanguageTag.IsValid(testLanguageTag, subtagRegistry);

            // assert
            testResult.Should().BeFalse("the language tag has an unregistered subtag or repeats a variant or singleton (RFC 5646 §2.2.9)");
        }

        [Theory]
        [MemberData(nameof(ValidLanguageTags))]
        public void IsValid_WithRegistryAndValidSpan_ReturnsTrue(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = Bcp47LanguageTag.IsValid(testLanguageTag.AsSpan(), subtagRegistry);

            // assert
            testResult.Should().BeTrue("the language tag satisfies RFC 5646 §2.2.9");
        }

        [Theory]
        [MemberData(nameof(NotValidLanguageTags))]
        public void IsValid_WithRegistryAndSpanThatIsNotValid_ReturnsFalse(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = Bcp47LanguageTag.IsValid(testLanguageTag.AsSpan(), subtagRegistry);

            // assert
            testResult.Should().BeFalse("the language tag has an unregistered subtag or repeats a variant or singleton (RFC 5646 §2.2.9)");
        }

        [Theory]
        [MemberData(nameof(ValidLanguageTags))]
        public void IsValid_OnInstanceWithRegistryAndValidLanguageTag_ReturnsTrue(string testLanguageTag)
        {
            // arrange
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag);
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = languageTag.IsValid(subtagRegistry);

            // assert
            testResult.Should().BeTrue("the language tag satisfies RFC 5646 §2.2.9");
        }

        [Theory]
        [MemberData(nameof(NotValidLanguageTags))]
        public void IsValid_OnInstanceWithRegistryAndLanguageTagThatIsNotValid_ReturnsFalse(string testLanguageTag)
        {
            // arrange
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag);
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = languageTag.IsValid(subtagRegistry);

            // assert
            testResult.Should().BeFalse("the language tag has an unregistered subtag or repeats a variant or singleton (RFC 5646 §2.2.9)");
        }

        [Fact]
        public void IsValid_OnInstance_LeavesConformanceUnchanged()
        {
            // arrange
            var languageTag = Bcp47LanguageTag.Create(EnglishUnitedStates);
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var isValid = languageTag.IsValid(subtagRegistry);
            var testResult = (isValid, languageTag.Conformance);

            // assert
            testResult.Should().Be((true, LanguageTagConformance.WellFormed), "the struct is read-only, so a caller creates the tag with the registry to carry Valid");
        }

        [Theory]
        [MemberData(nameof(NotValidLanguageTags))]
        public void Create_WithRegistryAndLanguageTagThatIsNotValid_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag, subtagRegistry);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>().WithMessage(NotValidMessage);
        }

        [Fact]
        public void IsValid_WithNullLanguageTag_ThrowsArgumentNullException()
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            Action testAction = () => Bcp47LanguageTag.IsValid((string)null, subtagRegistry);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(LanguageTagParameterName);
        }

        [Fact]
        public void IsValid_WithStringAndNullRegistry_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => Bcp47LanguageTag.IsValid(EnglishUnitedStates, (SubtagRegistry.SubtagRegistry)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(SubtagRegistryParameterName);
        }

        [Fact]
        public void IsValid_WithSpanAndNullRegistry_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => Bcp47LanguageTag.IsValid(EnglishUnitedStates.AsSpan(), (SubtagRegistry.SubtagRegistry)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(SubtagRegistryParameterName);
        }

        [Fact]
        public void IsValid_OnInstanceWithNullRegistry_ThrowsArgumentNullException()
        {
            // arrange
            var languageTag = Bcp47LanguageTag.Create(EnglishUnitedStates);

            // act
            Action testAction = () => languageTag.IsValid((SubtagRegistry.SubtagRegistry)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(SubtagRegistryParameterName);
        }

        #endregion

        #region Test: ISO code lists

        [Theory]
        [MemberData(nameof(InIsoCodeListsLanguageTags))]
        public void Create_WithIsoCodeListsAndListedLanguageTag_KeepsTheString(string testLanguageTag)
        {
            // act
            string testResult = Bcp47LanguageTag.Create(testLanguageTag, LanguageTagConformance.IsoCodeLists);

            // assert
            testResult.Should().Be(testLanguageTag);
        }

        [Theory]
        [MemberData(nameof(NotInIsoCodeListsLanguageTags))]
        public void Create_WithIsoCodeListsAndUnlistedLanguageTag_ThrowsLanguageTagFormatException(string testLanguageTag)
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(testLanguageTag, LanguageTagConformance.IsoCodeLists);

            // assert
            testAction.Should().Throw<LanguageTagFormatException>().WithMessage(NotInIsoCodeListsMessage);
        }

        [Theory]
        [MemberData(nameof(NotInIsoCodeListsLanguageTags))]
        public void TryCreate_WithIsoCodeListsAndUnlistedLanguageTag_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag, LanguageTagConformance.IsoCodeLists, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(NotInIsoCodeListsLanguageTags))]
        public void TryCreate_WithIsoCodeListsAndUnlistedSpan_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.TryCreate(testLanguageTag.AsSpan(), LanguageTagConformance.IsoCodeLists, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(InIsoCodeListsLanguageTags))]
        public void IsValid_WithIsoCodeListsAndListedLanguageTag_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsValid(testLanguageTag, LanguageTagConformance.IsoCodeLists);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(NotInIsoCodeListsLanguageTags))]
        public void IsValid_WithIsoCodeListsAndUnlistedLanguageTag_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsValid(testLanguageTag, LanguageTagConformance.IsoCodeLists);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(InIsoCodeListsLanguageTags))]
        public void IsValid_WithIsoCodeListsAndListedSpan_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsValid(testLanguageTag.AsSpan(), LanguageTagConformance.IsoCodeLists);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(NotInIsoCodeListsLanguageTags))]
        public void IsValid_WithIsoCodeListsAndUnlistedSpan_ReturnsFalse(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsValid(testLanguageTag.AsSpan(), LanguageTagConformance.IsoCodeLists);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(InIsoCodeListsLanguageTags))]
        public void IsValid_OnInstanceWithIsoCodeListsAndListedLanguageTag_ReturnsTrue(string testLanguageTag)
        {
            // arrange
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag);

            // act
            var testResult = languageTag.IsValid(LanguageTagConformance.IsoCodeLists);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(NotInIsoCodeListsLanguageTags))]
        public void IsValid_OnInstanceWithIsoCodeListsAndUnlistedLanguageTag_ReturnsFalse(string testLanguageTag)
        {
            // arrange
            var languageTag = Bcp47LanguageTag.Create(testLanguageTag);

            // act
            var testResult = languageTag.IsValid(LanguageTagConformance.IsoCodeLists);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void IsValid_WithConformanceAndNullLanguageTag_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => Bcp47LanguageTag.IsValid((string)null, LanguageTagConformance.IsoCodeLists);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(LanguageTagParameterName);
        }

        [Fact]
        public void IsValid_WithWellFormedConformanceAndRepeatedVariant_ReturnsTrue()
        {
            // act
            var testResult = Bcp47LanguageTag.IsValid(RepeatedVariant, LanguageTagConformance.WellFormed);

            // assert
            testResult.Should().BeTrue("a well-formed language tag may repeat a variant subtag (RFC 5646 §2.2.9)");
        }

        [Fact]
        public void IsValid_OnInstance_WithIsoCodeListsLeavesConformanceUnchanged()
        {
            // arrange
            var languageTag = Bcp47LanguageTag.Create(EnglishUnitedStates);

            // act
            var isValid = languageTag.IsValid(LanguageTagConformance.IsoCodeLists);
            var testResult = (isValid, languageTag.Conformance);

            // assert
            testResult.Should().Be((true, LanguageTagConformance.WellFormed), "the struct is read-only");
        }

        [Theory]
        [MemberData(nameof(ThreeLetterCodesWithPart1Equivalent))]
        public void IsWellFormed_WithThreeLetterCodeOfLanguageWithPart1Code_ReturnsTrue(string testLanguageTag)
        {
            // act
            var testResult = Bcp47LanguageTag.IsWellFormed(testLanguageTag);

            // assert
            testResult.Should().BeTrue("the grammar has language = 2*3ALPHA, and only the ISO code lists look a code up (RFC 5646 §2.1)");
        }

        [Theory]
        [MemberData(nameof(ThreeLetterCodesWithPart1Equivalent))]
        public void IsValid_WithRegistryAndThreeLetterCodeOfLanguageWithPart1Code_ReturnsFalse(string testLanguageTag)
        {
            // arrange
            var subtagRegistry = IanaSubtagRegistryResource.Shared;

            // act
            var testResult = Bcp47LanguageTag.IsValid(testLanguageTag, subtagRegistry);

            // assert
            testResult.Should().BeFalse("only the ISO 639-1 code is defined in the registry (RFC 5646 §2.2.1)");
        }

        #endregion

        #region Test: choice of conformance

        [Theory]
        [MemberData(nameof(CallsWithValidConformance), DisableDiscoveryEnumeration = true)]
        public void Create_WithValidConformance_ThrowsArgumentException(string overload, Action call)
        {
            // act
            Action testAction = call;

            // assert
            testAction.Should().ThrowExactly<ArgumentException>("{0} has no registry to check validity against", overload).WithMessage(NeedsRegistryMessagePattern).WithParameterName(ConformanceParameterName);
        }

        [Theory]
        [MemberData(nameof(CallsWithUndefinedConformance), DisableDiscoveryEnumeration = true)]
        public void Create_WithUndefinedConformance_ThrowsArgumentOutOfRangeException(string overload, Action call)
        {
            // act
            Action testAction = call;

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>("{0} must not guess what to check", overload).WithParameterName(ConformanceParameterName);
        }

        [Fact]
        public void Create_WithConformanceAndNullLanguageTag_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => Bcp47LanguageTag.Create(null, LanguageTagConformance.WellFormed);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(LanguageTagParameterName);
        }

        [Fact]
        public void TryCreate_WithConformanceAndNullLanguageTag_ReturnsFalseAndDefault()
        {
            // act
            var testResult = (Bcp47LanguageTag.TryCreate((string)null, LanguageTagConformance.WellFormed, out var languageTag), languageTag);

            // assert
            testResult.Should().Be((false, default(Bcp47LanguageTag)));
        }

        [Theory]
        [MemberData(nameof(CreationPaths), DisableDiscoveryEnumeration = true)]
        public void Conformance_ForEachCreationPath_IsWhatCreationChecked(string creationPath, Func<Bcp47LanguageTag> create, LanguageTagConformance expectedResult)
        {
            // arrange
            var languageTag = create();

            // act
            var testResult = languageTag.Conformance;

            // assert
            testResult.Should().Be(expectedResult, "{0} checks that class", creationPath);
        }

        #endregion

        #region Test: uninitialised instances

        [Theory]
        [MemberData(nameof(UninitialisedLanguageTagMembers), DisableDiscoveryEnumeration = true)]
        public void Member_OnUninitialisedLanguageTag_ThrowsInvalidOperationException(string member, Action read)
        {
            // act
            Action testAction = read;

            // assert
            testAction.Should().Throw<InvalidOperationException>("{0} describes a tag that doesn't exist", member).WithMessage(LanguageTagNotInitialisedMessage);
        }

        [Theory]
        [MemberData(nameof(UninitialisedKeyedSubtagMembers), DisableDiscoveryEnumeration = true)]
        public void Member_OnUninitialisedKeyedSubtag_ThrowsInvalidOperationExceptionWithSameSpelling(string member, Action read)
        {
            // act
            Action testAction = read;

            // assert
            testAction.Should().Throw<InvalidOperationException>("{0} describes a subtag that doesn't exist", member).WithMessage(KeyedSubtagNotInitialisedMessage);
        }

        #endregion
    }
}
