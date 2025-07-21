using DataStandardizer.ISO15924;
using DataStandardizer.ISO3166;
using DataStandardizer.ISO639;
using DataStandardizer.UNM49;
using FluentAssertions;

namespace DataStandardizer.BCP47.Tests;

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
        Action testAction = () => builder.UsingLanguageTag(null!);

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
        var testValue = new Iso639Part1();
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
        var testValue = (Iso639Part1)"test";
        var builder = new Bcp47LanguageTagBuilder();

        // act
        Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

        // assert
        testAction.Should()
            .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso639Part1))
            .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*", "ISO 639-1", testValue))
            .WithParameterName("primaryLanguageSubtag");
    }

    #endregion

    #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part2T

    [Fact]
    public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part2T_ThrowsArgumentException()
    {
        // arrange
        var testValue = new Iso639Part2T();
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
        var testValue = (Iso639Part2T)"test";
        var builder = new Bcp47LanguageTagBuilder();

        // act
        Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

        // assert
        testAction.Should()
            .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso639Part2T))
            .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*", "ISO 639-2T", testValue))
            .WithParameterName("primaryLanguageSubtag");
    }

    #endregion

    #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part3

    [Fact]
    public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part3_ThrowsArgumentException()
    {
        // arrange
        var testValue = new Iso639Part3();
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
        var testValue = new Iso639Part3("test");
        var builder = new Bcp47LanguageTagBuilder();

        // act
        Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

        // assert
        testAction.Should()
            .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso639Part3))
            .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*", "ISO 639-3", testValue))
            .WithParameterName("primaryLanguageSubtag");
    }

    #endregion

    #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part5

    [Fact]
    public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNotInitialisedInstanceOfIso639Part5_ThrowsArgumentException()
    {
        // arrange
        var testValue = new Iso639Part5();
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
        var testValue = (Iso639Part5)"test";
        var builder = new Bcp47LanguageTagBuilder();

        // act
        Action testAction = () => builder.UsingPrimaryLanguageSubtag(testValue);

        // assert
        testAction.Should()
            .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso639Part5))
            .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*", "ISO 639-5", testValue))
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

    #region Test: UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNull

    [Fact]
    public void UsingPrimaryLanguageSubtag_WhenPrimaryLanguageSubtagIsNull_ThrowsArgumentNullException()
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder();

        // act
        Action testAction = () => _ = builder.UsingPrimaryLanguageSubtag(null!);

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
        Action testAction = () => _ = builder.UsingExtendedLanguageSubtags(null!);

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
    public void UsingExtendedLanguageSubtags_WithTwoExtendedLanguageSubtagsAndExtendedLanguageSubtagIsNull_ThrowsArgumentNullException(string? firstExtendedLanguageSubtag, string? secondExtendedLanguageSubtag, string parameterName)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        Action testAction = () => _ = builder.UsingExtendedLanguageSubtags(firstExtendedLanguageSubtag!, secondExtendedLanguageSubtag!);

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
    public void UsingExtendedLanguageSubtags_WithThreeExtendedLanguageSubtagsAndExtendedLanguageSubtagIsNull_ThrowsArgumentNullException(string? firstExtendedLanguageSubtag, string? secondExtendedLanguageSubtag, string? thirdExtendedLanguageSubtag,
        string parameterName)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        Action testAction = () => _ = builder.UsingExtendedLanguageSubtags(firstExtendedLanguageSubtag!, secondExtendedLanguageSubtag!, thirdExtendedLanguageSubtag!);

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
        var testValue = (Iso15924)0;
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        Action testAction = () => builder.UsingScriptSubtag(testValue);

        // assert
        testAction.Should()
            .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso15924))
            .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*", "ISO 15924", testValue))
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
        Action testAction = () => builder.UsingScriptSubtag(null!);

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
        var testValue = (Iso3166Part1Alpha2)0;
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        Action testAction = () => builder.UsingRegionSubtag(testValue);

        // assert
        testAction.Should()
            .Throw<ArgumentException>("the code must be a member of {0}", nameof(Iso3166Part1Alpha2))
            .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*", "ISO 3166-1 Alpha-2", testValue))
            .WithParameterName("regionSubtag");
    }

    #endregion

    #region Test: UsingRegionSubtag_WhenRegionSubtagIsNotDefinedMemberOfUnM49ByAlpha2Code

    [Fact]
    public void UsingRegionSubtag_WhenRegionSubtagIsNotDefinedMemberOfUnM49ByAlpha2Code_ThrowsArgumentException()
    {
        // arrange
        var testValue = (UnM49ByAlpha2Code)0;
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        Action testAction = () => builder.UsingRegionSubtag(testValue);

        // assert
        testAction.Should()
            .Throw<ArgumentException>("the code must be a member of {0}", nameof(UnM49ByAlpha2Code))
            .WithMessage(string.Format(Bcp47LanguageTagBuilder.ErrorMessage.SpecifiedCodeUndefinedTemplate + "*", "UN M49", ((ushort)testValue).ToString("000")))
            .WithParameterName("regionSubtag");
    }

    #endregion

    #region Test: UsingRegionSubtag_WhenRegionSubtagIsNotDefinedMemberOfUnM49ByAlpha3Code

    [Fact]
    public void UsingRegionSubtag_WhenRegionSubtagIsNotDefinedMemberOfUnM49ByAlpha3Code_ThrowsArgumentException()
    {
        // arrange
        var testValue = (UnM49ByAlpha3Code)0;
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

    #region Test: UsingRegionSubtag_WhenRegionSubtagIsNull

    [Fact]
    public void UsingRegionSubtag_WhenRegionSubtagIsNull_ThrowsArgumentNullException()
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        Action testAction = () => builder.UsingRegionSubtag(null!);

        // assert
        testAction.Should()
            .Throw<ArgumentNullException>("the subtag must not be null")
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
        Action testAction = () => builder.UsingVariantSubtags(null!);

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

    #region Test: UsingExtensionSubtags_WhenExtensionSubtagIsNull

    [Fact]
    public void UsingExtensionSubtags_WhenExtensionSubtagIsNull_ThrowsArgumentNullException()
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        Action testAction = () => builder.UsingExtensionSubtags(null!);

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

    #region Test: UsingPrivateUseSubtag_WhenPrivateUseSubtagIsNull

    [Fact]
    public void UsingPrivateUseSubtag_WhenPrivateUseSubtagIsNull_ThrowsArgumentNullException()
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        Action testAction = () => builder.UsingPrivateUseSubtag(null!);

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
        Action testAction = () => _ = builder.WithLanguageSubtagRegistry(null!);

        // assert
        testAction.Should()
            .Throw<ArgumentNullException>("the subtag registry must not be null")
            .WithParameterName("subtagRegistry");
    }

    #endregion
}