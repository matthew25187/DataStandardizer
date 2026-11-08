using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.Language.Tests;

/// <summary>
/// Tests of <see cref="IComparable.CompareTo(object)"/> on the ISO 639 string enumerations. Codes of every part of
/// ISO 639 are lower-case ASCII letters, so they order by their ordinal (alphabetical) value.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Iso639CompareToTests
{
    // IComparable.CompareTo(object): every instance compares greater than null. The codes return 1, as Money does.
    private const int GreaterThanNullResult = 1;

    #region Test: CompareTo_SmallerCode

    [Theory]
    [MemberData(nameof(CompareTo_SmallerCode_TestCaseGenerator.TestCases), MemberType = typeof(CompareTo_SmallerCode_TestCaseGenerator))]
    public void CompareTo_SmallerCode_ReturnsNegative(IComparable testSubject, object testOther)
    {
        // act
        var testResult = testSubject.CompareTo(testOther);

        // assert
        testResult.Should().BeNegative();
    }

    private class CompareTo_SmallerCode_TestCaseGenerator
    {
        public static TheoryData<IComparable, object> TestCases => new()
        {
            { Iso639Part1Language.aa, Iso639Part1Language.ab },
            { Iso639Part2BLanguage.aar, Iso639Part2BLanguage.abk },
            { Iso639Part2TLanguage.aar, Iso639Part2TLanguage.abk },
            { Iso639Part3Language.aaa, Iso639Part3Language.aab },
            { Iso639Part5LanguageFamily.aav, Iso639Part5LanguageFamily.afa },
        };
    }

    #endregion

    #region Test: CompareTo_SameCode

    [Theory]
    [MemberData(nameof(CompareTo_SameCode_TestCaseGenerator.TestCases), MemberType = typeof(CompareTo_SameCode_TestCaseGenerator))]
    public void CompareTo_SameCode_ReturnsZero(IComparable testSubject, object testOther)
    {
        // act
        var testResult = testSubject.CompareTo(testOther);

        // assert
        testResult.Should().Be(0);
    }

    private class CompareTo_SameCode_TestCaseGenerator
    {
        public static TheoryData<IComparable, object> TestCases => new()
        {
            { Iso639Part1Language.aa, Iso639Part1Language.aa },
            { Iso639Part2BLanguage.aar, Iso639Part2BLanguage.aar },
            { Iso639Part2TLanguage.aar, Iso639Part2TLanguage.aar },
            { Iso639Part3Language.aaa, Iso639Part3Language.aaa },
            { Iso639Part5LanguageFamily.aav, Iso639Part5LanguageFamily.aav },
        };
    }

    #endregion

    #region Test: CompareTo_LargerCode

    [Theory]
    [MemberData(nameof(CompareTo_LargerCode_TestCaseGenerator.TestCases), MemberType = typeof(CompareTo_LargerCode_TestCaseGenerator))]
    public void CompareTo_LargerCode_ReturnsPositive(IComparable testSubject, object testOther)
    {
        // act
        var testResult = testSubject.CompareTo(testOther);

        // assert
        testResult.Should().BePositive();
    }

    private class CompareTo_LargerCode_TestCaseGenerator
    {
        public static TheoryData<IComparable, object> TestCases => new()
        {
            { Iso639Part1Language.ab, Iso639Part1Language.aa },
            { Iso639Part2BLanguage.abk, Iso639Part2BLanguage.aar },
            { Iso639Part2TLanguage.abk, Iso639Part2TLanguage.aar },
            { Iso639Part3Language.aab, Iso639Part3Language.aaa },
            { Iso639Part5LanguageFamily.afa, Iso639Part5LanguageFamily.aav },
        };
    }

    #endregion

    #region Test: CompareTo_OnDefaultInstance

    [Theory]
    [MemberData(nameof(CompareTo_OnDefaultInstance_TestCaseGenerator.TestCases), MemberType = typeof(CompareTo_OnDefaultInstance_TestCaseGenerator))]
    public void CompareTo_OnDefaultInstance_ThrowsInvalidOperationException(IComparable testSubject, object testOther)
    {
        // act
        Action testAction = () => testSubject.CompareTo(testOther);

        // assert
        testAction.Should().ThrowExactly<InvalidOperationException>();
    }

    private class CompareTo_OnDefaultInstance_TestCaseGenerator
    {
        public static TheoryData<IComparable, object> TestCases => new()
        {
            { default(Iso639Part1Language), Iso639Part1Language.aa },
            { default(Iso639Part2BLanguage), Iso639Part2BLanguage.aar },
            { default(Iso639Part2TLanguage), Iso639Part2TLanguage.aar },
            { default(Iso639Part3Language), Iso639Part3Language.aaa },
            { default(Iso639Part5LanguageFamily), Iso639Part5LanguageFamily.aav },
        };
    }

    #endregion

    #region Test: CompareTo_OnDefaultInstanceWithNullArgument

    [Theory]
    [MemberData(nameof(CompareTo_OnDefaultInstanceWithNullArgument_TestCaseGenerator.TestCases), MemberType = typeof(CompareTo_OnDefaultInstanceWithNullArgument_TestCaseGenerator))]
    public void CompareTo_OnDefaultInstanceWithNullArgument_ThrowsInvalidOperationException(IComparable testSubject)
    {
        // act
        Action testAction = () => testSubject.CompareTo(null);

        // assert
        testAction.Should().ThrowExactly<InvalidOperationException>();
    }

    private class CompareTo_OnDefaultInstanceWithNullArgument_TestCaseGenerator
    {
        public static TheoryData<IComparable> TestCases => new()
        {
            default(Iso639Part1Language),
            default(Iso639Part2BLanguage),
            default(Iso639Part2TLanguage),
            default(Iso639Part3Language),
            default(Iso639Part5LanguageFamily),
        };
    }

    #endregion

    #region Test: CompareTo_DefaultInstanceArgument

    [Theory]
    [MemberData(nameof(CompareTo_DefaultInstanceArgument_TestCaseGenerator.TestCases), MemberType = typeof(CompareTo_DefaultInstanceArgument_TestCaseGenerator))]
    public void CompareTo_DefaultInstanceArgument_ThrowsArgumentException(IComparable testSubject, object testOther)
    {
        // act
        Action testAction = () => testSubject.CompareTo(testOther);

        // assert
        testAction.Should().ThrowExactly<ArgumentException>();
    }

    private class CompareTo_DefaultInstanceArgument_TestCaseGenerator
    {
        public static TheoryData<IComparable, object> TestCases => new()
        {
            { Iso639Part1Language.aa, default(Iso639Part1Language) },
            { Iso639Part2BLanguage.aar, default(Iso639Part2BLanguage) },
            { Iso639Part2TLanguage.aar, default(Iso639Part2TLanguage) },
            { Iso639Part3Language.aaa, default(Iso639Part3Language) },
            { Iso639Part5LanguageFamily.aav, default(Iso639Part5LanguageFamily) },
        };
    }

    #endregion

    #region Test: CompareTo_NullArgument

    [Theory]
    [MemberData(nameof(CompareTo_NullArgument_TestCaseGenerator.TestCases), MemberType = typeof(CompareTo_NullArgument_TestCaseGenerator))]
    public void CompareTo_NullArgument_ReturnsOne(IComparable testSubject)
    {
        // act
        var testResult = testSubject.CompareTo(null);

        // assert
        testResult.Should().Be(GreaterThanNullResult);
    }

    private class CompareTo_NullArgument_TestCaseGenerator
    {
        public static TheoryData<IComparable> TestCases => new()
        {
            Iso639Part1Language.aa,
            Iso639Part2BLanguage.aar,
            Iso639Part2TLanguage.aar,
            Iso639Part3Language.aaa,
            Iso639Part5LanguageFamily.aav,
        };
    }

    #endregion

    #region Test: CompareTo_ArgumentOfAnotherType

    [Theory]
    [MemberData(nameof(CompareTo_ArgumentOfAnotherType_TestCaseGenerator.TestCases), MemberType = typeof(CompareTo_ArgumentOfAnotherType_TestCaseGenerator))]
    public void CompareTo_ArgumentOfAnotherType_ThrowsArgumentException(IComparable testSubject, object testOther)
    {
        // act
        Action testAction = () => testSubject.CompareTo(testOther);

        // assert
        testAction.Should().ThrowExactly<ArgumentException>();
    }

    private class CompareTo_ArgumentOfAnotherType_TestCaseGenerator
    {
        // Each subject is compared with the code of the same letters, as a string or as a code of another part of
        // ISO 639.
        public static TheoryData<IComparable, object> TestCases => new()
        {
            { Iso639Part1Language.aa, "aa" },
            { Iso639Part2BLanguage.aar, Iso639Part2TLanguage.aar },
            { Iso639Part2TLanguage.aar, Iso639Part2BLanguage.aar },
            { Iso639Part3Language.aar, Iso639Part2TLanguage.aar },
            { Iso639Part5LanguageFamily.aav, "aav" },
        };
    }

    #endregion

    #region Test: Sort_OnList

    [Fact]
    public void Sort_OnIso639Part1LanguageList_OrdersByCode()
    {
        // arrange
        var testList = new List<Iso639Part1Language> { Iso639Part1Language.af, Iso639Part1Language.aa, Iso639Part1Language.ab };

        // act
        testList.Sort();

        // assert
        testList.Should().Equal(Iso639Part1Language.aa, Iso639Part1Language.ab, Iso639Part1Language.af);
    }

    [Fact]
    public void Sort_OnIso639Part2BLanguageList_OrdersByCode()
    {
        // arrange
        var testList = new List<Iso639Part2BLanguage> { Iso639Part2BLanguage.ace, Iso639Part2BLanguage.aar, Iso639Part2BLanguage.abk };

        // act
        testList.Sort();

        // assert
        testList.Should().Equal(Iso639Part2BLanguage.aar, Iso639Part2BLanguage.abk, Iso639Part2BLanguage.ace);
    }

    [Fact]
    public void Sort_OnIso639Part2TLanguageList_OrdersByCode()
    {
        // arrange
        var testList = new List<Iso639Part2TLanguage> { Iso639Part2TLanguage.ace, Iso639Part2TLanguage.aar, Iso639Part2TLanguage.abk };

        // act
        testList.Sort();

        // assert
        testList.Should().Equal(Iso639Part2TLanguage.aar, Iso639Part2TLanguage.abk, Iso639Part2TLanguage.ace);
    }

    [Fact]
    public void Sort_OnIso639Part3LanguageList_OrdersByCode()
    {
        // arrange
        var testList = new List<Iso639Part3Language> { Iso639Part3Language.aac, Iso639Part3Language.aaa, Iso639Part3Language.aab };

        // act
        testList.Sort();

        // assert
        testList.Should().Equal(Iso639Part3Language.aaa, Iso639Part3Language.aab, Iso639Part3Language.aac);
    }

    [Fact]
    public void Sort_OnIso639Part5LanguageFamilyList_OrdersByCode()
    {
        // arrange
        var testList = new List<Iso639Part5LanguageFamily> { Iso639Part5LanguageFamily.alg, Iso639Part5LanguageFamily.aav, Iso639Part5LanguageFamily.afa };

        // act
        testList.Sort();

        // assert
        testList.Should().Equal(Iso639Part5LanguageFamily.aav, Iso639Part5LanguageFamily.afa, Iso639Part5LanguageFamily.alg);
    }

    #endregion
}
