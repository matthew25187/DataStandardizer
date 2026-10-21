using System;
using System.Collections.Generic;
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
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.UnspecifiedCodeUndefinedTemplate + "*", "ISO 639-1"))
                .WithParameterName("primaryLanguageSubtag");
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part1

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part1_ThrowsArgumentException()
        {
            // arrange
            var testValue = (Iso639Part1Language)"test";
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso639Part1Language))
                .WithMessage(string.Format((string)(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*"), "ISO 639-1", testValue))
                .WithParameterName("primaryLanguageSubtag");
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
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.UnspecifiedCodeUndefinedTemplate + "*", "ISO 639-2T"))
                .WithParameterName("primaryLanguageSubtag");
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part2T

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part2T_ThrowsArgumentException()
        {
            // arrange
            var testValue = (Iso639Part2TLanguage)"test";
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso639Part2TLanguage))
                .WithMessage(string.Format((string)(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*"), "ISO 639-2T", testValue))
                .WithParameterName("primaryLanguageSubtag");
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
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.UnspecifiedCodeUndefinedTemplate + "*", "ISO 639-3"))
                .WithParameterName("primaryLanguageSubtag");
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part3

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part3_ThrowsArgumentException()
        {
            // arrange
            var testValue = (Iso639Part3Language)"test";
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso639Part3Language))
                .WithMessage(string.Format((string)(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*"), "ISO 639-3", testValue))
                .WithParameterName("primaryLanguageSubtag");
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
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.UnspecifiedCodeUndefinedTemplate + "*", "ISO 639-5"))
                .WithParameterName("primaryLanguageSubtag");
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part5

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotDefinedMemberOfIso639Part5_ThrowsArgumentException()
        {
            // arrange
            var testValue = (Iso639Part5LanguageFamily)"test";
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso639Part5LanguageFamily))
                .WithMessage(string.Format((string)(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*"), "ISO 639-5", testValue))
                .WithParameterName("primaryLanguageSubtag");
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsInvalid

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            const string testSubject = "invalidprimarysubtag";
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => _ = builder.UsingPrimaryLanguageSubtag(testSubject);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' is not a valid subtag", testSubject)
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Primary Language subtag") + "*")
                .WithParameterName("primaryLanguageSubtag");
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNonAlpha

        [Theory]
        [InlineData("12345"), InlineData("abcd1"), InlineData("a1b2c3d4")]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNonAlpha_ThrowsArgumentException(string testSubject)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder();

            // act
            Action testAction = () => _ = builder.UsingPrimaryLanguageSubtag(testSubject);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' contains a digit, and a Primary Language subtag is letters only (RFC 5646 §2.1, §2.2.1)", testSubject)
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Primary Language subtag") + "*")
                .WithParameterName("primaryLanguageSubtag");
        }

        #endregion

        #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsReserved

        [Fact]
        public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsReserved_ProducesBcp47LanguageTag()
        {
            // arrange
            const string testSubject = "abcd";
            var builder = new Bcp47LanguageTagBuilder();

            // act
            var testResult = builder.UsingPrimaryLanguageSubtag(testSubject).Build();

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
                .WithParameterName("primaryLanguageSubtag");
        }

        #endregion

        #region Test: UsingExtendedLanguageSubtags_WithOneExtendedLanguageSubtagAndExtendedLanguageSubtagIsInvalid

        [Fact]
        public void UsingExtendedLanguageSubtags_WithOneExtendedLanguageSubtagAndExtendedLanguageSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            const string testSubject = "invalid";
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtendedLanguageSubtags(testSubject);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' is not a valid subtag", testSubject)
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Extended Language subtag") + "*")
                .WithParameterName("firstExtendedLanguageSubtag");
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
                .WithParameterName("firstExtendedLanguageSubtag");
        }

        #endregion

        #region Test: UsingExtendedLanguageSubtags_WithTwoExtendedLanguageSubtagsAndExtendedLanguageSubtagIsNull

        [Theory]
        [InlineData(null, null, "firstExtendedLanguageSubtag"), 
         InlineData(null, "bbb", "firstExtendedLanguageSubtag"), 
         InlineData("aaa", null, "secondExtendedLanguageSubtag")]
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
            const string testSubject = "invalid";
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtendedLanguageSubtags("abc", testSubject);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' is not a valid subtag", testSubject)
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Extended Language subtag") + "*")
                .WithParameterName("secondExtendedLanguageSubtag");
        }

        #endregion

        #region Test: UsingExtendedLanguageSubtags_WithThreeExtendedLanguageSubtagsAndExtendedLanguageSubtagIsNull

        [Theory]
        [InlineData(null, null, null, "firstExtendedLanguageSubtag"),
         InlineData(null, "abc", null, "firstExtendedLanguageSubtag"),
         InlineData(null, null, "abc", "firstExtendedLanguageSubtag"),
         InlineData(null, "abc", "def", "firstExtendedLanguageSubtag"),
         InlineData("abc", null, null, "secondExtendedLanguageSubtag"),
         InlineData("abc", "def", null, "thirdExtendedLanguageSubtag"),
         InlineData("abc", null, "def", "secondExtendedLanguageSubtag")]
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
            const string testSubject = "invalid";
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtendedLanguageSubtags("abc", "def", testSubject);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' is not a valid subtag", testSubject)
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Extended Language subtag") + "*")
                .WithParameterName("thirdExtendedLanguageSubtag");
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
                .WithMessage(string.Format((string)(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*"), "ISO 15924", testValue))
                .WithParameterName("scriptSubtag");
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
                .WithParameterName("scriptSubtag");
        }

        #endregion

        #region Test: UsingScriptSubtag_WhenScriptSubtagIsInvalid

        [Fact]
        public void UsingScriptSubtag_WhenScriptSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            const string testSubject = "invalid";
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingScriptSubtag(testSubject);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' is not a valid subtag", testSubject)
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Script subtag") + "*")
                .WithParameterName("scriptSubtag");
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
                .WithMessage(string.Format((string)(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*"), "ISO 3166-1 Alpha-2", testValue))
                .WithParameterName("regionSubtag");
        }

        #endregion

#pragma warning disable CS0618 // The UN M49 country-code overloads are tested until they are removed.
        #region Test: UsingRegionSubtag_WhenRegionSubtagIsNotDefinedMemberOfUnM49ByAlpha2Code

        [Fact]
        public void UsingRegionSubtag_WhenRegionSubtagIsNotDefinedMemberOfUnM49ByAlpha2Code_ThrowsArgumentException()
        {
            // arrange
            var testValue = (UnM49AreaByAlpha2CountryCode)0;
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => builder.UsingRegionSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("the code must be a member of {0}", nameof(UnM49AreaByAlpha2CountryCode))
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*", "UN M49", ((ushort)testValue).ToString("000")))
                .WithParameterName("regionSubtag");
        }

        #endregion

        #region Test: UsingRegionSubtag_WhenRegionSubtagIsNotDefinedMemberOfUnM49ByAlpha3Code

        [Fact]
        public void UsingRegionSubtag_WhenRegionSubtagIsNotDefinedMemberOfUnM49ByAlpha3Code_ThrowsArgumentException()
        {
            // arrange
            var testValue = (UnM49AreaByAlpha3CountryCode)0;
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => builder.UsingRegionSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>()
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*", "UN M49", ((ushort)testValue).ToString("000")))
                .WithParameterName("regionSubtag");
        }

        #endregion

#pragma warning restore CS0618

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
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*", "UN M49", ((ushort)testValue).ToString("000")))
                .WithParameterName("regionSubtag");
        }

        #endregion

        #region Test: UsingRegionSubtag_WhenRegionSubtagIsUnM49CountryOrArea

        [Theory]
        [MemberData(nameof(UsingRegionSubtag_WhenRegionSubtagIsUnM49CountryOrArea_TestCaseGenerator.TestCases), MemberType = typeof(UsingRegionSubtag_WhenRegionSubtagIsUnM49CountryOrArea_TestCaseGenerator))]
        public void UsingRegionSubtag_WhenRegionSubtagIsUnM49CountryOrArea_ThrowsArgumentException(UnM49Area testValue)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => builder.UsingRegionSubtag(testValue);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("BCP 47 does not allow a UN M49 country or area code as a Region subtag")
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, ((ushort)testValue).ToString("000"), "Region subtag") + "*")
                .WithParameterName("regionSubtag");
        }

        private class UsingRegionSubtag_WhenRegionSubtagIsUnM49CountryOrArea_TestCaseGenerator
        {
            public static IEnumerable<object[]> TestCases
            {
                get
                {
                    foreach (var testValue in Enum.GetValues(typeof(UnM49Area)).Cast<UnM49Area>().Where(code => code.GetLevel() == UnM49AreaLevel.CountryOrArea))
                    {
                        yield return new object[] { testValue };
                    }
                }
            }
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
                .WithParameterName("regionSubtag");
        }

        #endregion

        #region Test: UsingRegionSubtag_WhenRegionSubtagIsChannelIslands

        [Fact]
        public void UsingRegionSubtag_WhenRegionSubtagIsChannelIslands_ThrowsArgumentException()
        {
            // arrange
            const string testSubject = "830";
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingRegionSubtag(testSubject);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("830 (Channel Islands) is not a registered Region subtag (RFC 5646 §2.2.4¶4E-F)")
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Region subtag") + "*")
                .WithParameterName("regionSubtag");
        }

        #endregion

        #region Test: UsingRegionSubtag_WhenRegionSubtagIsInvalid

        [Fact]
        public void UsingRegionSubtag_WhenRegionSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            const string testSubject = "invalid";
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingRegionSubtag(testSubject);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' is not a valid subtag", testSubject)
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Region subtag") + "*")
                .WithParameterName("regionSubtag");
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
                .WithParameterName("variantSubtag");
        }

        #endregion

        #region Test: UsingVariantSubtags_WhenVariantSubtagIsInvalid

        [Fact]
        public void UsingVariantSubtags_WhenVariantSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            const string testSubject = "invalidvariant";
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingVariantSubtags(testSubject);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' is not a valid subtag", testSubject)
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Variant subtag") + "*")
                .WithParameterName("variantSubtag");
        }

        #endregion

        #region Test: UsingVariantSubtags_WhenVariantSubtagIsRepeated

        [Theory]
        [InlineData("valencia", new[] { "valencia" }, "valencia"),
         InlineData("valencia", new[] { "VALENCIA" }, "VALENCIA"),
         InlineData("rozaj", new[] { "biske", "rozaj" }, "rozaj")]
        public void UsingVariantSubtags_WhenVariantSubtagIsRepeated_ThrowsArgumentException(string testVariantSubtag, string[] testVariantSubtags, string expectedRepeatedSubtag)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingVariantSubtags(testVariantSubtag, testVariantSubtags);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("each variant subtag must appear at most once (RFC 5646 §2.2.5)")
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagRepeatedTemplate, "Variant subtag", expectedRepeatedSubtag) + "*")
                .WithParameterName("variantSubtags");
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
                .WithParameterName("extensionSubtag");
        }

        #endregion

        #region Test: UsingExtensionSubtags_WhenExtensionSubtagIsInvalid

        [Fact]
        public void UsingExtensionSubtags_WhenExtensionSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            const string testSubject = "invalid";
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtensionSubtags(testSubject);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' is not a valid subtag")
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Extension subtag") + "*")
                .WithParameterName("extensionSubtag");
        }

        #endregion

        #region Test: UsingExtensionSubtags_WhenSingletonIsRepeated

        [Theory]
        [InlineData("a-bbb", new[] { "a-ccc" }, "a"),
         InlineData("a-bbb", new[] { "A-ccc" }, "A"),
         InlineData("a-bbb", new[] { "b-ccc", "a-ddd" }, "a")]
        public void UsingExtensionSubtags_WhenSingletonIsRepeated_ThrowsArgumentException(string testExtensionSubtag, string[] testExtensionSubtags, string expectedRepeatedSingleton)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingExtensionSubtags(testExtensionSubtag, testExtensionSubtags);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("each extension singleton must appear at most once (RFC 5646 §2.2.6)")
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagRepeatedTemplate, "Extension singleton", expectedRepeatedSingleton) + "*")
                .WithParameterName("extensionSubtags");
        }

        #endregion

        #region Test: UsingVariantSubtags_WhenLaterVariantSubtagIsNull

        [Fact]
        public void UsingVariantSubtags_WhenLaterVariantSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testVariantSubtags = new[] { "biske", null };

            // act
            Action testAction = () => _ = builder.UsingVariantSubtags("rozaj", testVariantSubtags);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName("variantSubtags");
        }

        #endregion

        #region Test: UsingVariantSubtags_WhenLaterVariantSubtagIsInvalid

        [Fact]
        public void UsingVariantSubtags_WhenLaterVariantSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            const string testSubject = "invalidvariant";
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testVariantSubtags = new[] { "biske", testSubject };

            // act
            Action testAction = () => _ = builder.UsingVariantSubtags("rozaj", testVariantSubtags);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' is not a valid subtag", testSubject)
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Variant subtag") + "*")
                .WithParameterName("variantSubtags");
        }

        #endregion

        #region Test: UsingExtensionSubtags_WhenLaterExtensionSubtagIsNull

        [Fact]
        public void UsingExtensionSubtags_WhenLaterExtensionSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testExtensionSubtags = new[] { "b-ccc", null };

            // act
            Action testAction = () => _ = builder.UsingExtensionSubtags("a-bbb", testExtensionSubtags);

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName("extensionSubtags");
        }

        #endregion

        #region Test: UsingExtensionSubtags_WhenLaterExtensionSubtagIsInvalid

        [Fact]
        public void UsingExtensionSubtags_WhenLaterExtensionSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            const string testSubject = "invalid";
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testExtensionSubtags = new[] { "b-ccc", testSubject };

            // act
            Action testAction = () => _ = builder.UsingExtensionSubtags("a-bbb", testExtensionSubtags);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' is not a valid subtag", testSubject)
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Extension subtag") + "*")
                .WithParameterName("extensionSubtags");
        }

        #endregion

#if NET9_0_OR_GREATER
        #region Test: UsingVariantSubtags_FromSpan

        [Fact]
        public void UsingVariantSubtags_FromSpan_ProducesSameLanguageTagAsFromArray()
        {
            // arrange
            var testVariantSubtags = new[] { "biske", "1994" };
            var expectedResult = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag("sl").UsingVariantSubtags("rozaj", testVariantSubtags).Build();
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag("sl");

            // act
            var testResult = builder.UsingVariantSubtags("rozaj", new ReadOnlySpan<string>(testVariantSubtags)).Build();

            // assert
            testResult.ToString().Should().Be(expectedResult.ToString()).And.Be("sl-rozaj-biske-1994");
        }

        #endregion

        #region Test: UsingVariantSubtags_FromSpanWhenLaterVariantSubtagIsNull

        [Fact]
        public void UsingVariantSubtags_FromSpanWhenLaterVariantSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testVariantSubtags = new[] { "biske", null };

            // act
            Action testAction = () => _ = builder.UsingVariantSubtags("rozaj", new ReadOnlySpan<string>(testVariantSubtags));

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName("variantSubtags");
        }

        #endregion

        #region Test: UsingVariantSubtags_FromSpanWhenVariantSubtagIsRepeated

        [Fact]
        public void UsingVariantSubtags_FromSpanWhenVariantSubtagIsRepeated_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testVariantSubtags = new[] { "biske", "ROZAJ" };

            // act
            Action testAction = () => _ = builder.UsingVariantSubtags("rozaj", new ReadOnlySpan<string>(testVariantSubtags));

            // assert
            testAction.Should()
                .Throw<ArgumentException>("each variant subtag must appear at most once (RFC 5646 §2.2.5)")
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagRepeatedTemplate, "Variant subtag", "ROZAJ") + "*")
                .WithParameterName("variantSubtags");
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
            testSubject.UsingVariantSubtags("rozaj", new ReadOnlySpan<string>(new[] { "biske", "1994" }));

            // assert
            step.ReceivedFirstSubtag.Should().Be("rozaj");
            step.ReceivedSubtags.Should().Equal("biske", "1994");
        }

        #endregion

        #region Test: UsingExtensionSubtags_FromSpan

        [Fact]
        public void UsingExtensionSubtags_FromSpan_ProducesSameLanguageTagAsFromArray()
        {
            // arrange
            var testExtensionSubtags = new[] { "u-ca-hebrew" };
            var expectedResult = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag("he").UsingExtensionSubtags("a-bbb", testExtensionSubtags).Build();
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag("he");

            // act
            var testResult = builder.UsingExtensionSubtags("a-bbb", new ReadOnlySpan<string>(testExtensionSubtags)).Build();

            // assert
            testResult.ToString().Should().Be(expectedResult.ToString()).And.Be("he-a-bbb-u-ca-hebrew");
        }

        #endregion

        #region Test: UsingExtensionSubtags_FromSpanWhenLaterExtensionSubtagIsNull

        [Fact]
        public void UsingExtensionSubtags_FromSpanWhenLaterExtensionSubtagIsNull_ThrowsArgumentNullException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testExtensionSubtags = new[] { "b-ccc", null };

            // act
            Action testAction = () => _ = builder.UsingExtensionSubtags("a-bbb", new ReadOnlySpan<string>(testExtensionSubtags));

            // assert
            testAction.Should()
                .Throw<ArgumentNullException>("the subtag must not be null")
                .WithParameterName("extensionSubtags");
        }

        #endregion

        #region Test: UsingExtensionSubtags_FromSpanWhenSingletonIsRepeated

        [Fact]
        public void UsingExtensionSubtags_FromSpanWhenSingletonIsRepeated_ThrowsArgumentException()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);
            var testExtensionSubtags = new[] { "b-ccc", "A-ddd" };

            // act
            Action testAction = () => _ = builder.UsingExtensionSubtags("a-bbb", new ReadOnlySpan<string>(testExtensionSubtags));

            // assert
            testAction.Should()
                .Throw<ArgumentException>("each extension singleton must appear at most once (RFC 5646 §2.2.6)")
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagRepeatedTemplate, "Extension singleton", "A") + "*")
                .WithParameterName("extensionSubtags");
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
            testSubject.UsingExtensionSubtags("a-bbb", new ReadOnlySpan<string>(new[] { "u-ca-hebrew" }));

            // assert
            step.ReceivedFirstSubtag.Should().Be("a-bbb");
            step.ReceivedSubtags.Should().Equal("u-ca-hebrew");
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
                .WithParameterName("privateUseSubtag");
        }

        #endregion

        #region Test: UsingPrivateUseSubtag_WhenPrivateUseSubtagIsInvalid

        [Fact]
        public void UsingPrivateUseSubtag_WhenPrivateUseSubtagIsInvalid_ThrowsArgumentException()
        {
            // arrange
            const string testSubject = "invalidprivateuse";
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

            // act
            Action testAction = () => _ = builder.UsingPrivateUseSubtag(testSubject);

            // assert
            testAction.Should()
                .Throw<ArgumentException>("'{0}' is not a valid subtag", testSubject)
                .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SubtagInvalidTemplate, testSubject, "Private Use subtag") + "*")
                .WithParameterName("privateUseSubtag");
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
