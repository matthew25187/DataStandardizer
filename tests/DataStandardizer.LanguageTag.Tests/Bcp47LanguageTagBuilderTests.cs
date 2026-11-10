using System;
using System.Linq;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    public class Bcp47LanguageTagBuilderTests
    {
        #region Declarations

        private const string DefaultPrimaryLanguageSubtag = "en";

        /// <summary>
        /// A value that is not a member of any of the code lists.
        /// </summary>
        private const string UndefinedCode = "test";

        /// <summary>
        /// A string that is not a subtag of any kind: it is longer than eight characters (RFC 5646 §2.1).
        /// </summary>
        private const string InvalidSubtag = "invalid";

        private const string ValidExtendedLanguageSubtag = "abc";
        private const string OtherValidExtendedLanguageSubtag = "def";

        // Variant subtags registered for Slovenian dialects (RFC 5646 §2.2.5; IANA Language Subtag Registry), from the RFC 5646 Appendix A example sl-rozaj-biske.
        private const string RozajVariantSubtag = "rozaj";
        private const string BiskeVariantSubtag = "biske";

        /// <summary>
        /// The variant subtag registered for Valencian (RFC 5646 §2.2.5; IANA Language Subtag Registry).
        /// </summary>
        private const string ValenciaVariantSubtag = "valencia";

        // Extensions with the singletons a and b (RFC 5646 §2.2.6), from the RFC 5646 Appendix A example en-a-myext-b-another.
        private const string ExtensionA = "a-bbb";
        private const string ExtensionB = "b-ccc";

        /// <summary>
        /// A Unicode locale extension with the calendar key (RFC 5646 §2.2.6; RFC 6067).
        /// </summary>
        private const string HebrewCalendarExtension = "u-ca-hebrew";

        private const string PrimaryLanguageSubtagParameterName = "primaryLanguageSubtag";
        private const string FirstExtendedLanguageSubtagParameterName = "firstExtendedLanguageSubtag";
        private const string SecondExtendedLanguageSubtagParameterName = "secondExtendedLanguageSubtag";
        private const string ThirdExtendedLanguageSubtagParameterName = "thirdExtendedLanguageSubtag";
        private const string ScriptSubtagParameterName = "scriptSubtag";
        private const string RegionSubtagParameterName = "regionSubtag";
        private const string VariantSubtagParameterName = "variantSubtag";
        private const string VariantSubtagsParameterName = "variantSubtags";
        private const string ExtensionSubtagParameterName = "extensionSubtag";
        private const string ExtensionSubtagsParameterName = "extensionSubtags";
        private const string PrivateUseSubtagParameterName = "privateUseSubtag";

        // The messages of the exceptions the builder throws. Each ends with a wildcard for the parameter name that ArgumentException appends to it.
        private const string Iso639Part1CodeUndefinedMessage = "The ISO 639-1 code is undefined.*";
        private const string Iso639Part2TCodeUndefinedMessage = "The ISO 639-2T code is undefined.*";
        private const string Iso639Part3CodeUndefinedMessage = "The ISO 639-3 code is undefined.*";
        private const string Iso639Part5CodeUndefinedMessage = "The ISO 639-5 code is undefined.*";
        private const string UnM49CodeZeroUndefinedMessage = "The UN M49 code '000' is undefined.*";
        private const string InvalidExtendedLanguageSubtagMessage = "'invalid' is not a valid Extended Language subtag.*";
        private const string InvalidVariantSubtagMessage = "'invalidvariant' is not a valid Variant subtag.*";
        private const string InvalidExtensionSubtagMessage = "'invalid' is not a valid Extension subtag.*";

        private const string InvalidVariantSubtag = "invalidvariant";

        #endregion

        #region Test: UsingLanguageTag_WhenLanguageTagIsNull

        [Fact]
        public void UsingLanguageTag_WhenLanguageTagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingLanguageTag(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the language tag must not be null")
                .WithParameterName("languageTag");
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part1

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part1_ThrowsArgumentException()
        {
            // arrange
            var testValue = new Iso639Part1Language();
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be initialised")
                .WithMessage(Iso639Part1CodeUndefinedMessage)
                .WithParameterName(PrimaryLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part1

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part1_ThrowsArgumentException()
        {
            // arrange
            var testValue = (Iso639Part1Language)UndefinedCode;
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso639Part1Language))
                .WithMessage("The ISO 639-1 code 'test' is undefined.*")
                .WithParameterName(PrimaryLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part2T

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part2T_ThrowsArgumentException()
        {
            // arrange
            var testValue = new Iso639Part2TLanguage();
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be initialised")
                .WithMessage(Iso639Part2TCodeUndefinedMessage)
                .WithParameterName(PrimaryLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part2T

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part2T_ThrowsArgumentException()
        {
            // arrange
            var testValue = (Iso639Part2TLanguage)UndefinedCode;
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso639Part2TLanguage))
                .WithMessage("The ISO 639-2T code 'test' is undefined.*")
                .WithParameterName(PrimaryLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part3

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part3_ThrowsArgumentException()
        {
            // arrange
            var testValue = new Iso639Part3Language();
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be initialised")
                .WithMessage(Iso639Part3CodeUndefinedMessage)
                .WithParameterName(PrimaryLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part3

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part3_ThrowsArgumentException()
        {
            // arrange
            var testValue = (Iso639Part3Language)UndefinedCode;
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso639Part3Language))
                .WithMessage("The ISO 639-3 code 'test' is undefined.*")
                .WithParameterName(PrimaryLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part5

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part5_ThrowsArgumentException()
        {
            // arrange
            var testValue = new Iso639Part5LanguageFamily();
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be initialised")
                .WithMessage(Iso639Part5CodeUndefinedMessage)
                .WithParameterName(PrimaryLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part5

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part5_ThrowsArgumentException()
        {
            // arrange
            var testValue = (Iso639Part5LanguageFamily)UndefinedCode;
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso639Part5LanguageFamily))
                .WithMessage("The ISO 639-5 code 'test' is undefined.*")
                .WithParameterName(PrimaryLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsInvalid

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => _ = builder.UsingPrimaryLanguageSubtag("invalidprimarysubtag");

            // assert
            testAction.Should()
                .Throw<ArgumentException>("a Primary Language subtag has at most eight letters (RFC 5646 §2.1)")
                .WithMessage("'invalidprimarysubtag' is not a valid Primary Language subtag.*")
                .WithParameterName(PrimaryLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNonAlpha

        [Theory]
        [InlineData("12345", "'12345' is not a valid Primary Language subtag.*"),
         InlineData("abcd1", "'abcd1' is not a valid Primary Language subtag.*"),
         InlineData("a1b2c3d4", "'a1b2c3d4' is not a valid Primary Language subtag.*")]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNonAlpha_ThrowsArgumentException(string testSubject, string expectedMessage)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => _ = builder.UsingPrimaryLanguageSubtag(testSubject);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' contains a digit, and a Primary Language subtag is letters only (RFC 5646 §2.1, §2.2.1)", testSubject)
                .WithMessage(expectedMessage)
                .WithParameterName(PrimaryLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsReserved

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsReserved_ProducesBcp47LanguageTag()
        {
            // arrange
            // A four-letter Primary Language subtag is reserved for future use (RFC 5646 §2.2.1¶4).
            const string testSubject = "abcd";
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(testSubject);

            // act
            var testResult = builder.Build();

            // assert
            testResult.PrimaryLanguageSubtag.Should().Be(testSubject, "a four-letter Primary Language subtag is reserved, and well-formed (RFC 5646 §2.1, §2.2.1¶4)");
        }

        #endregion

        #region Test: Build_WhenExtendedLanguageSubtagsFollowRegisteredPrimaryLanguageSubtag

        [Fact]
        public void Build_WhenExtendedLanguageSubtagsFollowRegisteredPrimaryLanguageSubtag_ThrowsLanguageTagFormatException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag("abcde").UsingExtendedLanguageSubtags("aaa");

            // act
            Action testAction = () => _ = builder.Build();

            // assert
            testAction.Should().Throw<LanguageTagFormatException>("only a two- or three-letter Primary Language subtag may be followed by Extended Language subtags (RFC 5646 §2.1, §2.2.2)");
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNull

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => _ = builder.UsingPrimaryLanguageSubtag(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(PrimaryLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingExtendedLanguageSubtags_WithOneExtendedLanguageSubtagAndExtendedLanguageSubtagIsInvalid

        [Fact]
        public void UsingExtendedLanguageSubtags_WithOneExtendedLanguageSubtagAndExtendedLanguageSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtendedLanguageSubtags(InvalidSubtag);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("an Extended Language subtag has three letters (RFC 5646 §2.1)")
                .WithMessage(InvalidExtendedLanguageSubtagMessage)
                .WithParameterName(FirstExtendedLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingExtendedLanguageSubtags_WithOneExtendedLanguageSubtagAndExtendedLanguageSubtagIsNull

        [Fact]
        public void UsingExtendedLanguageSubtags_WithOneExtendedLanguageSubtagAndExtendedLanguageSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtendedLanguageSubtags(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(FirstExtendedLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingExtendedLanguageSubtags_WithTwoExtendedLanguageSubtagsAndExtendedLanguageSubtagIsNull

        [Theory]
        [InlineData(null, null, FirstExtendedLanguageSubtagParameterName),
         InlineData(null, "bbb", FirstExtendedLanguageSubtagParameterName),
         InlineData("aaa", null, SecondExtendedLanguageSubtagParameterName)]
        public void UsingExtendedLanguageSubtags_WithTwoExtendedLanguageSubtagsAndExtendedLanguageSubtagIsNull_ThrowsArgumentNullException(string firstExtendedLanguageSubtag, string secondExtendedLanguageSubtag, string parameterName)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtendedLanguageSubtags(firstExtendedLanguageSubtag, secondExtendedLanguageSubtag);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(parameterName);
        }

        #endregion

        #region Test: UsingExtendedLanguageSubtags_WithTwoExtendedLanguageSubtagsAndExtendedLanguageSubtagIsInvalid

        [Fact]
        public void UsingExtendedLanguageSubtags_WithTwoExtendedLanguageSubtagsAndExtendedLanguageSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtendedLanguageSubtags(ValidExtendedLanguageSubtag, InvalidSubtag);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("an Extended Language subtag has three letters (RFC 5646 §2.1)")
                .WithMessage(InvalidExtendedLanguageSubtagMessage)
                .WithParameterName(SecondExtendedLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingExtendedLanguageSubtags_WithThreeExtendedLanguageSubtagsAndExtendedLanguageSubtagIsNull

        [Theory]
        [InlineData(null, null, null, FirstExtendedLanguageSubtagParameterName),
         InlineData(null, ValidExtendedLanguageSubtag, null, FirstExtendedLanguageSubtagParameterName),
         InlineData(null, null, ValidExtendedLanguageSubtag, FirstExtendedLanguageSubtagParameterName),
         InlineData(null, ValidExtendedLanguageSubtag, OtherValidExtendedLanguageSubtag, FirstExtendedLanguageSubtagParameterName),
         InlineData(ValidExtendedLanguageSubtag, null, null, SecondExtendedLanguageSubtagParameterName),
         InlineData(ValidExtendedLanguageSubtag, OtherValidExtendedLanguageSubtag, null, ThirdExtendedLanguageSubtagParameterName),
         InlineData(ValidExtendedLanguageSubtag, null, OtherValidExtendedLanguageSubtag, SecondExtendedLanguageSubtagParameterName)]
        public void UsingExtendedLanguageSubtags_WithThreeExtendedLanguageSubtagsAndExtendedLanguageSubtagIsNull_ThrowsArgumentNullException(string firstExtendedLanguageSubtag, string secondExtendedLanguageSubtag, string thirdExtendedLanguageSubtag,
            string parameterName)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtendedLanguageSubtags(firstExtendedLanguageSubtag, secondExtendedLanguageSubtag, thirdExtendedLanguageSubtag);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(parameterName);
        }

        #endregion

        #region Test: UsingExtendedLanguageSubtags_WithThreeExtendedLanguageSubtagsAndExtendedLanguageSubtagIsInvalid

        [Fact]
        public void UsingExtendedLanguageSubtags_WithThreeExtendedLanguageSubtagsAndExtendedLanguageSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtendedLanguageSubtags(ValidExtendedLanguageSubtag, OtherValidExtendedLanguageSubtag, InvalidSubtag);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("an Extended Language subtag has three letters (RFC 5646 §2.1)")
                .WithMessage(InvalidExtendedLanguageSubtagMessage)
                .WithParameterName(ThirdExtendedLanguageSubtagParameterName);
        }

        #endregion

        #region Test: UsingScriptSubtag_WhenScriptSubtagIsNotDefinedMemberOfIso15924

        [Fact]
        public void UsingScriptSubtag_WhenScriptSubtagIsNotDefinedMemberOfIso15924_ThrowsArgumentException()
        {
            // arrange
            var testValue = (Iso15924Script)0;
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => builder.UsingScriptSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso15924Script))
                .WithMessage("The ISO 15924 code '0' is undefined.*")
                .WithParameterName(ScriptSubtagParameterName);
        }

        #endregion

        #region Test: UsingScriptSubtag_WhenScriptSubtagIsNull

        [Fact]
        public void UsingScriptSubtag_WhenScriptSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => builder.UsingScriptSubtag(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(ScriptSubtagParameterName);
        }

        #endregion

        #region Test: UsingScriptSubtag_WhenScriptSubtagIsInvalid

        [Fact]
        public void UsingScriptSubtag_WhenScriptSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingScriptSubtag(InvalidSubtag);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("a Script subtag has four letters (RFC 5646 §2.1)")
                .WithMessage("'invalid' is not a valid Script subtag.*")
                .WithParameterName(ScriptSubtagParameterName);
        }

        #endregion

        #region Test: UsingRegionSubtag_WhenRegionSubtagIsNotDefinedMemberOfIso3166Part1Alpha2

        [Fact]
        public void UsingRegionSubtag_WhenRegionSubtagIsNotDefinedMemberOfIso3166Part1Alpha2_ThrowsArgumentException()
        {
            // arrange
            var testValue = (Iso3166Part1Alpha2Country)0;
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => builder.UsingRegionSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso3166Part1Alpha2Country))
                .WithMessage("The ISO 3166-1 Alpha-2 code '0' is undefined.*")
                .WithParameterName(RegionSubtagParameterName);
        }

        #endregion

        #region Test: UsingRegionSubtag_WhenRegionSubtagIsNotDefinedMemberOfUnM49Area

        [Fact]
        public void UsingRegionSubtag_WhenRegionSubtagIsNotDefinedMemberOfUnM49Area_ThrowsArgumentException()
        {
            // arrange
            var testValue = (UnM49Area)0;
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => builder.UsingRegionSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be a member of {0}", nameof(UnM49Area))
                .WithMessage(UnM49CodeZeroUndefinedMessage)
                .WithParameterName(RegionSubtagParameterName);
        }

        #endregion

        #region Test: UsingRegionSubtag_WhenRegionSubtagIsUnM49CountryOrArea

        /// <summary>
        /// Every UN M49 country or area code, which BCP 47 does not allow as a Region subtag (RFC 5646 §2.2.4¶4B), with the message the builder
        /// gives for it.
        /// </summary>
        public static TheoryData<UnM49Area, string> UnM49CountryOrAreaCodes
        {
            get
            {
                var data = new TheoryData<UnM49Area, string>();
                foreach (var testValue in Enum.GetValues(typeof(UnM49Area)).Cast<UnM49Area>().Where(code => code.GetLevel() == UnM49AreaLevel.CountryOrArea))
                {
                    data.Add(testValue, "'" + ((ushort)testValue).ToString("000") + "' is not a valid Region subtag.*");
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(UnM49CountryOrAreaCodes))]
        public void UsingRegionSubtag_WhenRegionSubtagIsUnM49CountryOrArea_ThrowsArgumentException(UnM49Area testValue, string expectedMessage)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => builder.UsingRegionSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("BCP 47 does not allow a UN M49 country or area code as a Region subtag")
                .WithMessage(expectedMessage)
                .WithParameterName(RegionSubtagParameterName);
        }

        #endregion

        #region Test: UsingRegionSubtag_WhenRegionSubtagIsNull

        [Fact]
        public void UsingRegionSubtag_WhenRegionSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => builder.UsingRegionSubtag(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(RegionSubtagParameterName);
        }

        #endregion

        #region Test: UsingRegionSubtag_WhenRegionSubtagIsInvalid

        [Fact]
        public void UsingRegionSubtag_WhenRegionSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingRegionSubtag(InvalidSubtag);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("a Region subtag has two letters or three digits (RFC 5646 §2.1)")
                .WithMessage("'invalid' is not a valid Region subtag.*")
                .WithParameterName(RegionSubtagParameterName);
        }

        #endregion

        #region Test: UsingVariantSubtags_WhenVariantSubtagIsNull

        [Fact]
        public void UsingVariantSubtags_WhenVariantSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => builder.UsingVariantSubtags(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(VariantSubtagParameterName);
        }

        #endregion

        #region Test: UsingVariantSubtags_WhenVariantSubtagIsInvalid

        [Fact]
        public void UsingVariantSubtags_WhenVariantSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingVariantSubtags(InvalidVariantSubtag);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("a Variant subtag has at most eight characters (RFC 5646 §2.1)")
                .WithMessage(InvalidVariantSubtagMessage)
                .WithParameterName(VariantSubtagParameterName);
        }

        #endregion

        #region Test: UsingVariantSubtags_WhenVariantSubtagIsRepeated

        [Theory]
        [InlineData(ValenciaVariantSubtag, new[] { ValenciaVariantSubtag }, "The Variant subtag 'valencia' appears more than once.*"),
         InlineData(ValenciaVariantSubtag, new[] { "VALENCIA" }, "The Variant subtag 'VALENCIA' appears more than once.*"),
         InlineData(RozajVariantSubtag, new[] { BiskeVariantSubtag, RozajVariantSubtag }, "The Variant subtag 'rozaj' appears more than once.*")]
        public void UsingVariantSubtags_WhenVariantSubtagIsRepeated_ThrowsArgumentException(string testVariantSubtag, string[] testVariantSubtags, string expectedMessage)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingVariantSubtags(testVariantSubtag, testVariantSubtags);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("each variant subtag must appear at most once (RFC 5646 §2.2.5)")
                .WithMessage(expectedMessage)
                .WithParameterName(VariantSubtagsParameterName);
        }

        #endregion

        #region Test: UsingExtensionSubtags_WhenExtensionSubtagIsNull

        [Fact]
        public void UsingExtensionSubtags_WhenExtensionSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => builder.UsingExtensionSubtags(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(ExtensionSubtagParameterName);
        }

        #endregion

        #region Test: UsingExtensionSubtags_WhenExtensionSubtagIsInvalid

        [Fact]
        public void UsingExtensionSubtags_WhenExtensionSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtensionSubtags(InvalidSubtag);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("an Extension subtag starts with a singleton (RFC 5646 §2.1)")
                .WithMessage(InvalidExtensionSubtagMessage)
                .WithParameterName(ExtensionSubtagParameterName);
        }

        #endregion

        #region Test: UsingExtensionSubtags_WhenSingletonIsRepeated

        [Theory]
        [InlineData(ExtensionA, new[] { "a-ccc" }, "The Extension singleton 'a' appears more than once.*"),
         InlineData(ExtensionA, new[] { "A-ccc" }, "The Extension singleton 'A' appears more than once.*"),
         InlineData(ExtensionA, new[] { ExtensionB, "a-ddd" }, "The Extension singleton 'a' appears more than once.*")]
        public void UsingExtensionSubtags_WhenSingletonIsRepeated_ThrowsArgumentException(string testExtensionSubtag, string[] testExtensionSubtags, string expectedMessage)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtensionSubtags(testExtensionSubtag, testExtensionSubtags);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("each extension singleton must appear at most once (RFC 5646 §2.2.6)")
                .WithMessage(expectedMessage)
                .WithParameterName(ExtensionSubtagsParameterName);
        }

        #endregion

        #region Test: UsingVariantSubtags_WhenLaterVariantSubtagIsNull

        [Fact]
        public void UsingVariantSubtags_WhenLaterVariantSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testVariantSubtags = new[] { BiskeVariantSubtag, null };

            // act
            Action testAction = () => _ = builder.UsingVariantSubtags(RozajVariantSubtag, testVariantSubtags);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(VariantSubtagsParameterName);
        }

        #endregion

        #region Test: UsingVariantSubtags_WhenLaterVariantSubtagIsInvalid

        [Fact]
        public void UsingVariantSubtags_WhenLaterVariantSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testVariantSubtags = new[] { BiskeVariantSubtag, InvalidVariantSubtag };

            // act
            Action testAction = () => _ = builder.UsingVariantSubtags(RozajVariantSubtag, testVariantSubtags);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("a Variant subtag has at most eight characters (RFC 5646 §2.1)")
                .WithMessage(InvalidVariantSubtagMessage)
                .WithParameterName(VariantSubtagsParameterName);
        }

        #endregion

        #region Test: UsingExtensionSubtags_WhenLaterExtensionSubtagIsNull

        [Fact]
        public void UsingExtensionSubtags_WhenLaterExtensionSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testExtensionSubtags = new[] { ExtensionB, null };

            // act
            Action testAction = () => _ = builder.UsingExtensionSubtags(ExtensionA, testExtensionSubtags);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(ExtensionSubtagsParameterName);
        }

        #endregion

        #region Test: UsingExtensionSubtags_WhenLaterExtensionSubtagIsInvalid

        [Fact]
        public void UsingExtensionSubtags_WhenLaterExtensionSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testExtensionSubtags = new[] { ExtensionB, InvalidSubtag };

            // act
            Action testAction = () => _ = builder.UsingExtensionSubtags(ExtensionA, testExtensionSubtags);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("an Extension subtag starts with a singleton (RFC 5646 §2.1)")
                .WithMessage(InvalidExtensionSubtagMessage)
                .WithParameterName(ExtensionSubtagsParameterName);
        }

        #endregion

#if NET9_0_OR_GREATER
        #region Test: UsingVariantSubtags_FromSpan

        [Fact]
        public void UsingVariantSubtags_FromSpan_ProducesLanguageTagWithEveryVariantSubtag()
        {
            // arrange
            var testVariantSubtags = new[] { BiskeVariantSubtag, "1994" };
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag("sl").UsingVariantSubtags(RozajVariantSubtag, new ReadOnlySpan<string>(testVariantSubtags));

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be("sl-rozaj-biske-1994", "the span overload adds the same subtags as the array overload");
        }

        #endregion

        #region Test: UsingVariantSubtags_FromSpanWhenLaterVariantSubtagIsNull

        [Fact]
        public void UsingVariantSubtags_FromSpanWhenLaterVariantSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testVariantSubtags = new[] { BiskeVariantSubtag, null };

            // act
            Action testAction = () => _ = builder.UsingVariantSubtags(RozajVariantSubtag, new ReadOnlySpan<string>(testVariantSubtags));

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(VariantSubtagsParameterName);
        }

        #endregion

        #region Test: UsingVariantSubtags_FromSpanWhenVariantSubtagIsRepeated

        [Fact]
        public void UsingVariantSubtags_FromSpanWhenVariantSubtagIsRepeated_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testVariantSubtags = new[] { BiskeVariantSubtag, "ROZAJ" };

            // act
            Action testAction = () => _ = builder.UsingVariantSubtags(RozajVariantSubtag, new ReadOnlySpan<string>(testVariantSubtags));

            // assert
            testAction.Should()
                .Throw<ArgumentException>("each variant subtag must appear at most once (RFC 5646 §2.2.5)")
                .WithMessage("The Variant subtag 'ROZAJ' appears more than once.*")
                .WithParameterName(VariantSubtagsParameterName);
        }

        #endregion

        #region Test: UsingVariantSubtags_FromSpanByDefaultImplementation

        [Fact]
        public void UsingVariantSubtags_FromSpanByDefaultImplementation_CallsArrayOverload()
        {
            // arrange
            var step = new ArrayOnlyStep();
            IBcp47LanguageTagBuilderStepUsingVariantSubtags testSubject = step;

            // act
            testSubject.UsingVariantSubtags(RozajVariantSubtag, new ReadOnlySpan<string>(new[] { BiskeVariantSubtag, "1994" }));

            // assert
            step.ReceivedFirstSubtag.Should().Be(RozajVariantSubtag);
            step.ReceivedSubtags.Should().Equal(BiskeVariantSubtag, "1994");
        }

        #endregion

        #region Test: UsingExtensionSubtags_FromSpan

        [Fact]
        public void UsingExtensionSubtags_FromSpan_ProducesLanguageTagWithEveryExtensionSubtag()
        {
            // arrange
            var testExtensionSubtags = new[] { HebrewCalendarExtension };
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag("he").UsingExtensionSubtags(ExtensionA, new ReadOnlySpan<string>(testExtensionSubtags));

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be("he-a-bbb-u-ca-hebrew", "the span overload adds the same subtags as the array overload");
        }

        #endregion

        #region Test: UsingExtensionSubtags_FromSpanWhenLaterExtensionSubtagIsNull

        [Fact]
        public void UsingExtensionSubtags_FromSpanWhenLaterExtensionSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testExtensionSubtags = new[] { ExtensionB, null };

            // act
            Action testAction = () => _ = builder.UsingExtensionSubtags(ExtensionA, new ReadOnlySpan<string>(testExtensionSubtags));

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(ExtensionSubtagsParameterName);
        }

        #endregion

        #region Test: UsingExtensionSubtags_FromSpanWhenSingletonIsRepeated

        [Fact]
        public void UsingExtensionSubtags_FromSpanWhenSingletonIsRepeated_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testExtensionSubtags = new[] { ExtensionB, "A-ddd" };

            // act
            Action testAction = () => _ = builder.UsingExtensionSubtags(ExtensionA, new ReadOnlySpan<string>(testExtensionSubtags));

            // assert
            testAction.Should()
                .Throw<ArgumentException>("each extension singleton must appear at most once (RFC 5646 §2.2.6)")
                .WithMessage("The Extension singleton 'A' appears more than once.*")
                .WithParameterName(ExtensionSubtagsParameterName);
        }

        #endregion

        #region Test: UsingExtensionSubtags_FromSpanByDefaultImplementation

        [Fact]
        public void UsingExtensionSubtags_FromSpanByDefaultImplementation_CallsArrayOverload()
        {
            // arrange
            var step = new ArrayOnlyStep();
            IBcp47LanguageTagBuilderStepUsingExtensionSubtags testSubject = step;

            // act
            testSubject.UsingExtensionSubtags(ExtensionA, new ReadOnlySpan<string>(new[] { HebrewCalendarExtension }));

            // assert
            step.ReceivedFirstSubtag.Should().Be(ExtensionA);
            step.ReceivedSubtags.Should().Equal(HebrewCalendarExtension);
        }

        #endregion

        /// <summary>
        /// Implements only the array overloads, so that the span overloads use their default implementations.
        /// </summary>
        private sealed class ArrayOnlyStep : IBcp47LanguageTagBuilderStepUsingVariantSubtags, IBcp47LanguageTagBuilderStepUsingExtensionSubtags
        {
            public string ReceivedFirstSubtag { get; private set; }

            public string[] ReceivedSubtags { get; private set; }

            public IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext UsingVariantSubtags(string variantSubtag, params string[] variantSubtags)
            {
                ReceivedFirstSubtag = variantSubtag;
                ReceivedSubtags = variantSubtags;
                return null;
            }

            public IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext UsingExtensionSubtags(string extensionSubtag, params string[] extensionSubtags)
            {
                ReceivedFirstSubtag = extensionSubtag;
                ReceivedSubtags = extensionSubtags;
                return null;
            }
        }
#endif

        #region Test: UsingPrivateUseSubtag_WhenPrivateUseSubtagIsNull

        [Fact]
        public void UsingPrivateUseSubtag_WhenPrivateUseSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => builder.UsingPrivateUseSubtag(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName(PrivateUseSubtagParameterName);
        }

        #endregion

        #region Test: UsingPrivateUseSubtag_WhenPrivateUseSubtagIsInvalid

        [Fact]
        public void UsingPrivateUseSubtag_WhenPrivateUseSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingPrivateUseSubtag("invalidprivateuse");

            // assert
            testAction.Should()
                .Throw<ArgumentException>("a Private Use subtag starts with the singleton x (RFC 5646 §2.1)")
                .WithMessage("'invalidprivateuse' is not a valid Private Use subtag.*")
                .WithParameterName(PrivateUseSubtagParameterName);
        }

        #endregion

        #region Test: WithLanguageSubtagRegistry_WhenSubtagRegistryIsNull

        [Fact]
        public void WithLanguageSubtagRegistry_WhenSubtagRegistryIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => _ = builder.WithLanguageSubtagRegistry(null);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag registry must not be null")
                .WithParameterName("subtagRegistry");
        }

        #endregion
    }
}
