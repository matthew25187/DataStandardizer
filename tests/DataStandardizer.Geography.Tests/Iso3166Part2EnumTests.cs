using System.Collections;
using FluentAssertions;

namespace DataStandardizer.Geography.Tests;

public class Iso3166Part2EnumTests
{
    [Theory]
    [ClassData(typeof(GetNames_Iso3166Part1Alpha2Country_TestCaseFactory))]
    public void GetNames_Iso3166Part1Alpha2Country_ReturnsNamesOfSubdivisionFields(Iso3166Part1Alpha2Country testValue, string[] expectedResult)
    {
        // act
        var testResult = Iso3166Part2Enum.GetNames(testValue);

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    public class GetNames_Iso3166Part1Alpha2Country_TestCaseFactory : IEnumerable<object[]>
    {
        private readonly IEnumerable<object[]> _testCases;

        public GetNames_Iso3166Part1Alpha2Country_TestCaseFactory()
        {
            _testCases = new List<object[]>
                {
                    new object[]
                    {
                        Iso3166Part1Alpha2Country.AD,
                        new[]
                        {
                            nameof(Iso3166Part2Subdivision.AD._02), nameof(Iso3166Part2Subdivision.AD._03), nameof(Iso3166Part2Subdivision.AD._04), nameof(Iso3166Part2Subdivision.AD._05), nameof(Iso3166Part2Subdivision.AD._06),
                            nameof(Iso3166Part2Subdivision.AD._07), nameof(Iso3166Part2Subdivision.AD._08)
                        }
                    }
                }
                .AsEnumerable();
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

    [Theory]
    [ClassData(typeof(GetNames_Iso3166Part1Alpha3Country_TestCaseFactory))]
    public void GetNames_Iso3166Part1Alpha3Country_ReturnsNamesOfSubdivisionFields(Iso3166Part1Alpha3Country testValue, string[] expectedResult)
    {
        // act
        var testResult = Iso3166Part2Enum.GetNames(testValue);

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    public class GetNames_Iso3166Part1Alpha3Country_TestCaseFactory : IEnumerable<object[]>
    {
        private readonly IEnumerable<object[]> _testCases;

        public GetNames_Iso3166Part1Alpha3Country_TestCaseFactory()
        {
            _testCases = new List<object[]>
                {
                    new object[]
                    {
                        Iso3166Part1Alpha3Country.AUS,
                        new[]
                        {
                            nameof(Iso3166Part2Subdivision.AU._ACT), nameof(Iso3166Part2Subdivision.AU._NSW), nameof(Iso3166Part2Subdivision.AU._NT), nameof(Iso3166Part2Subdivision.AU._QLD), nameof(Iso3166Part2Subdivision.AU._SA),
                            nameof(Iso3166Part2Subdivision.AU._TAS), nameof(Iso3166Part2Subdivision.AU._VIC), nameof(Iso3166Part2Subdivision.AU._WA)
                        }
                    }
                }
                .AsEnumerable();
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

    [Theory]
    [ClassData(typeof(GetValues_Iso3166Part1Alpha2Country_TestCaseFactory))]
    public void GetValues_Iso3166Part1Alpha2Country_ReturnsValuesOfSubdivisionFields(Iso3166Part1Alpha2Country testValue, Iso3166Part2Subdivision[] expectedResult)
    {
        // act
        var testResult = Iso3166Part2Enum.GetValues(testValue);

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    public class GetValues_Iso3166Part1Alpha2Country_TestCaseFactory : IEnumerable<object[]>
    {
        private readonly IEnumerable<object[]> _testCases;

        public GetValues_Iso3166Part1Alpha2Country_TestCaseFactory()
        {
            _testCases = new List<object[]>
                {
                    new object[]
                    {
                        Iso3166Part1Alpha2Country.AD,
                        new[]
                        {
                            Iso3166Part2Subdivision.AD._02, Iso3166Part2Subdivision.AD._03, Iso3166Part2Subdivision.AD._04, Iso3166Part2Subdivision.AD._05, Iso3166Part2Subdivision.AD._06, Iso3166Part2Subdivision.AD._07,
                            Iso3166Part2Subdivision.AD._08
                        }
                    }
                }
                .AsEnumerable();
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

    [Theory]
    [ClassData(typeof(GetValues_Iso3166Part1Alpha3Country_TestCaseFactory))]
    public void GetValues_Iso3166Part1Alpha3Country_ReturnsValuesOfSubdivisionFields(Iso3166Part1Alpha3Country testValue, Iso3166Part2Subdivision[] expectedResult)
    {
        // act
        var testResult = Iso3166Part2Enum.GetValues(testValue);

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    public class GetValues_Iso3166Part1Alpha3Country_TestCaseFactory : IEnumerable<object[]>
    {
        private readonly IEnumerable<object[]> _testCases;

        public GetValues_Iso3166Part1Alpha3Country_TestCaseFactory()
        {
            _testCases = new List<object[]>
                {
                    new object[]
                    {
                        Iso3166Part1Alpha3Country.AUS,
                        new[]
                        {
                            Iso3166Part2Subdivision.AU._ACT, Iso3166Part2Subdivision.AU._NSW, Iso3166Part2Subdivision.AU._NT, Iso3166Part2Subdivision.AU._QLD, Iso3166Part2Subdivision.AU._SA, Iso3166Part2Subdivision.AU._TAS,
                            Iso3166Part2Subdivision.AU._VIC, Iso3166Part2Subdivision.AU._WA
                        }
                    }
                }
                .AsEnumerable();
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
}