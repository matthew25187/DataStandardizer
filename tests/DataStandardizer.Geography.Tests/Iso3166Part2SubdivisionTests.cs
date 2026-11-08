using FluentAssertions;

namespace DataStandardizer.Geography.Tests;

public class Iso3166Part2SubdivisionTests
{
    #region Test: CompareTo

    [Theory]
    [MemberData(nameof(CompareTo_SmallerCode_TestCases))]
    public void CompareTo_SmallerCode_ReturnsNegative(Iso3166Part2Subdivision testValue, Iso3166Part2Subdivision otherValue)
    {
        // act
        var testResult = testValue.CompareTo(otherValue);

        // assert
        testResult.Should().BeNegative();
    }

    public static TheoryData<Iso3166Part2Subdivision, Iso3166Part2Subdivision> CompareTo_SmallerCode_TestCases => new()
    {
        { Iso3166Part2Subdivision.AU._NSW, Iso3166Part2Subdivision.AU._VIC },
        { Iso3166Part2Subdivision.AU._NSW, Iso3166Part2Subdivision.NZ._AUK },
    };

    [Theory]
    [MemberData(nameof(CompareTo_SameCode_TestCases))]
    public void CompareTo_SameCode_ReturnsZero(Iso3166Part2Subdivision testValue, Iso3166Part2Subdivision otherValue)
    {
        // act
        var testResult = testValue.CompareTo(otherValue);

        // assert
        testResult.Should().Be(0);
    }

    public static TheoryData<Iso3166Part2Subdivision, Iso3166Part2Subdivision> CompareTo_SameCode_TestCases => new()
    {
        { Iso3166Part2Subdivision.AU._NSW, Iso3166Part2Subdivision.AU._NSW },
        { Iso3166Part2Subdivision.NZ._AUK, Iso3166Part2Subdivision.NZ._AUK },
    };

    [Theory]
    [MemberData(nameof(CompareTo_LargerCode_TestCases))]
    public void CompareTo_LargerCode_ReturnsPositive(Iso3166Part2Subdivision testValue, Iso3166Part2Subdivision otherValue)
    {
        // act
        var testResult = testValue.CompareTo(otherValue);

        // assert
        testResult.Should().BePositive();
    }

    public static TheoryData<Iso3166Part2Subdivision, Iso3166Part2Subdivision> CompareTo_LargerCode_TestCases => new()
    {
        { Iso3166Part2Subdivision.AU._VIC, Iso3166Part2Subdivision.AU._NSW },
        { Iso3166Part2Subdivision.NZ._AUK, Iso3166Part2Subdivision.AU._NSW },
    };

    [Fact]
    public void CompareTo_DefaultInstance_ThrowsInvalidOperationException()
    {
        // arrange
        var testValue = default(Iso3166Part2Subdivision);

        // act
        Action testAction = () => testValue.CompareTo(Iso3166Part2Subdivision.AU._NSW);

        // assert
        testAction.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CompareTo_NullObject_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => Iso3166Part2Subdivision.AU._NSW.CompareTo(null);

        // assert
        testAction.Should().Throw<ArgumentNullException>().WithParameterName("obj");
    }

    [Fact]
    public void CompareTo_ObjectOfOtherType_ThrowsArgumentException()
    {
        // act
        Action testAction = () => Iso3166Part2Subdivision.AU._NSW.CompareTo("AU-NSW");

        // assert
        testAction.Should().Throw<ArgumentException>().WithParameterName("obj");
    }

    [Fact]
    public void CompareTo_DefaultInstanceObject_ThrowsArgumentException()
    {
        // arrange
        object otherValue = default(Iso3166Part2Subdivision);

        // act
        Action testAction = () => Iso3166Part2Subdivision.AU._NSW.CompareTo(otherValue);

        // assert
        testAction.Should().Throw<ArgumentException>().WithParameterName("obj");
    }

    [Fact]
    public void CompareTo_ListSort_OrdersSubdivisionsByCode()
    {
        // arrange
        var testList = new List<Iso3166Part2Subdivision>
        {
            Iso3166Part2Subdivision.NZ._AUK,
            Iso3166Part2Subdivision.AU._VIC,
            Iso3166Part2Subdivision.AU._ACT,
            Iso3166Part2Subdivision.AU._NSW,
        };

        // act
        testList.Sort();

        // assert
        testList.Should().Equal(
            Iso3166Part2Subdivision.AU._ACT,
            Iso3166Part2Subdivision.AU._NSW,
            Iso3166Part2Subdivision.AU._VIC,
            Iso3166Part2Subdivision.NZ._AUK);
    }

    #endregion
}
