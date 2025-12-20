using System.Collections;
using FluentAssertions;

namespace DataStandardizer.Geography.Tests;

public class Iso3166Part2EnumTests
{
    [Fact]
    public void GetNames_InvalidIso3166Part1Alpha2Country_ReturnsEmptySet()
    {
        // act
        var testResult = Iso3166Part2Enum.GetNames(default(Iso3166Part1Alpha2Country));

        // assert
        testResult.Should().BeEmpty();
    }

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
    [InlineData(Iso3166Part1Alpha2Country.AI), InlineData(Iso3166Part1Alpha2Country.AQ), InlineData(Iso3166Part1Alpha2Country.AS), InlineData(Iso3166Part1Alpha2Country.AW), InlineData(Iso3166Part1Alpha2Country.AX),
     InlineData(Iso3166Part1Alpha2Country.BL), InlineData(Iso3166Part1Alpha2Country.BM), InlineData(Iso3166Part1Alpha2Country.BV), InlineData(Iso3166Part1Alpha2Country.CC), InlineData(Iso3166Part1Alpha2Country.CK),
     InlineData(Iso3166Part1Alpha2Country.CW), InlineData(Iso3166Part1Alpha2Country.CX), InlineData(Iso3166Part1Alpha2Country.EH), InlineData(Iso3166Part1Alpha2Country.FK), InlineData(Iso3166Part1Alpha2Country.FO),
     InlineData(Iso3166Part1Alpha2Country.GF), InlineData(Iso3166Part1Alpha2Country.GG), InlineData(Iso3166Part1Alpha2Country.GI), InlineData(Iso3166Part1Alpha2Country.GP), InlineData(Iso3166Part1Alpha2Country.GS),
     InlineData(Iso3166Part1Alpha2Country.GU), InlineData(Iso3166Part1Alpha2Country.HK), InlineData(Iso3166Part1Alpha2Country.HM), InlineData(Iso3166Part1Alpha2Country.IM), InlineData(Iso3166Part1Alpha2Country.IO),
     InlineData(Iso3166Part1Alpha2Country.JE), InlineData(Iso3166Part1Alpha2Country.KY), InlineData(Iso3166Part1Alpha2Country.MF), InlineData(Iso3166Part1Alpha2Country.MO), InlineData(Iso3166Part1Alpha2Country.MP),
     InlineData(Iso3166Part1Alpha2Country.MQ), InlineData(Iso3166Part1Alpha2Country.MS), InlineData(Iso3166Part1Alpha2Country.NC), InlineData(Iso3166Part1Alpha2Country.NF), InlineData(Iso3166Part1Alpha2Country.NU),
     InlineData(Iso3166Part1Alpha2Country.PF), InlineData(Iso3166Part1Alpha2Country.PM), InlineData(Iso3166Part1Alpha2Country.PN), InlineData(Iso3166Part1Alpha2Country.PR), InlineData(Iso3166Part1Alpha2Country.RE),
     InlineData(Iso3166Part1Alpha2Country.SJ), InlineData(Iso3166Part1Alpha2Country.SX), InlineData(Iso3166Part1Alpha2Country.TC), InlineData(Iso3166Part1Alpha2Country.TF), InlineData(Iso3166Part1Alpha2Country.TK),
     InlineData(Iso3166Part1Alpha2Country.VA), InlineData(Iso3166Part1Alpha2Country.VG), InlineData(Iso3166Part1Alpha2Country.VI), InlineData(Iso3166Part1Alpha2Country.YT)]
    public void GetNames_Iso3166Part1Alpha2CountryNoSubdivisions_ReturnsEmptySet(Iso3166Part1Alpha2Country testValue)
    {
        // act
        var testResult = Iso3166Part2Enum.GetNames(testValue);

        // assert
        testResult.Should().BeEmpty();
    }

    [Fact]
    public void GetNames_InvalidIso3166Part1Alpha3Country_ReturnsEmptySet()
    {
        // act
        var testResult = Iso3166Part2Enum.GetNames(default(Iso3166Part1Alpha3Country));
        
        // assert
        testResult.Should().BeEmpty();
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
    [InlineData(Iso3166Part1Alpha3Country.AIA), InlineData(Iso3166Part1Alpha3Country.ATA), InlineData(Iso3166Part1Alpha3Country.ASM), InlineData(Iso3166Part1Alpha3Country.ABW), InlineData(Iso3166Part1Alpha3Country.ALA),
     InlineData(Iso3166Part1Alpha3Country.BLM), InlineData(Iso3166Part1Alpha3Country.BMU), InlineData(Iso3166Part1Alpha3Country.BVT), InlineData(Iso3166Part1Alpha3Country.CCK), InlineData(Iso3166Part1Alpha3Country.COK),
     InlineData(Iso3166Part1Alpha3Country.CUW), InlineData(Iso3166Part1Alpha3Country.CXR), InlineData(Iso3166Part1Alpha3Country.ESH), InlineData(Iso3166Part1Alpha3Country.FLK), InlineData(Iso3166Part1Alpha3Country.FRO),
     InlineData(Iso3166Part1Alpha3Country.GUF), InlineData(Iso3166Part1Alpha3Country.GGY), InlineData(Iso3166Part1Alpha3Country.GIB), InlineData(Iso3166Part1Alpha3Country.GLP), InlineData(Iso3166Part1Alpha3Country.SGS),
     InlineData(Iso3166Part1Alpha3Country.GUM), InlineData(Iso3166Part1Alpha3Country.HKG), InlineData(Iso3166Part1Alpha3Country.HMD), InlineData(Iso3166Part1Alpha3Country.IMN), InlineData(Iso3166Part1Alpha3Country.IOT),
     InlineData(Iso3166Part1Alpha3Country.JEY), InlineData(Iso3166Part1Alpha3Country.CYM), InlineData(Iso3166Part1Alpha3Country.MAF), InlineData(Iso3166Part1Alpha3Country.MAC), InlineData(Iso3166Part1Alpha3Country.MNP),
     InlineData(Iso3166Part1Alpha3Country.MTQ), InlineData(Iso3166Part1Alpha3Country.MSR), InlineData(Iso3166Part1Alpha3Country.NCL), InlineData(Iso3166Part1Alpha3Country.NFK), InlineData(Iso3166Part1Alpha3Country.NIU),
     InlineData(Iso3166Part1Alpha3Country.PYF), InlineData(Iso3166Part1Alpha3Country.SPM), InlineData(Iso3166Part1Alpha3Country.PCN), InlineData(Iso3166Part1Alpha3Country.PRI), InlineData(Iso3166Part1Alpha3Country.REU),
     InlineData(Iso3166Part1Alpha3Country.SJM), InlineData(Iso3166Part1Alpha3Country.SXM), InlineData(Iso3166Part1Alpha3Country.TCA), InlineData(Iso3166Part1Alpha3Country.ATF), InlineData(Iso3166Part1Alpha3Country.TKL),
     InlineData(Iso3166Part1Alpha3Country.VAT), InlineData(Iso3166Part1Alpha3Country.VGB), InlineData(Iso3166Part1Alpha3Country.VIR), InlineData(Iso3166Part1Alpha3Country.MYT)]
    public void GetNames_Iso3166Part1Alpha3CountryNoSubdivisions_ReturnsEmptySet(Iso3166Part1Alpha3Country testValue)
    {
        // act
        var testResult = Iso3166Part2Enum.GetNames(testValue);

        // assert
        testResult.Should().BeEmpty();
    }

    [Fact]
    public void GetValues_InvalidIso3166Part1Alpha2Country_ReturnsEmptySet()
    {
        // act
        var testResult = Iso3166Part2Enum.GetValues(default(Iso3166Part1Alpha2Country));

        // assert
        testResult.Should().BeEmpty();
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
    [InlineData(Iso3166Part1Alpha2Country.AI), InlineData(Iso3166Part1Alpha2Country.AQ), InlineData(Iso3166Part1Alpha2Country.AS), InlineData(Iso3166Part1Alpha2Country.AW), InlineData(Iso3166Part1Alpha2Country.AX),
     InlineData(Iso3166Part1Alpha2Country.BL), InlineData(Iso3166Part1Alpha2Country.BM), InlineData(Iso3166Part1Alpha2Country.BV), InlineData(Iso3166Part1Alpha2Country.CC), InlineData(Iso3166Part1Alpha2Country.CK),
     InlineData(Iso3166Part1Alpha2Country.CW), InlineData(Iso3166Part1Alpha2Country.CX), InlineData(Iso3166Part1Alpha2Country.EH), InlineData(Iso3166Part1Alpha2Country.FK), InlineData(Iso3166Part1Alpha2Country.FO),
     InlineData(Iso3166Part1Alpha2Country.GF), InlineData(Iso3166Part1Alpha2Country.GG), InlineData(Iso3166Part1Alpha2Country.GI), InlineData(Iso3166Part1Alpha2Country.GP), InlineData(Iso3166Part1Alpha2Country.GS),
     InlineData(Iso3166Part1Alpha2Country.GU), InlineData(Iso3166Part1Alpha2Country.HK), InlineData(Iso3166Part1Alpha2Country.HM), InlineData(Iso3166Part1Alpha2Country.IM), InlineData(Iso3166Part1Alpha2Country.IO),
     InlineData(Iso3166Part1Alpha2Country.JE), InlineData(Iso3166Part1Alpha2Country.KY), InlineData(Iso3166Part1Alpha2Country.MF), InlineData(Iso3166Part1Alpha2Country.MO), InlineData(Iso3166Part1Alpha2Country.MP),
     InlineData(Iso3166Part1Alpha2Country.MQ), InlineData(Iso3166Part1Alpha2Country.MS), InlineData(Iso3166Part1Alpha2Country.NC), InlineData(Iso3166Part1Alpha2Country.NF), InlineData(Iso3166Part1Alpha2Country.NU),
     InlineData(Iso3166Part1Alpha2Country.PF), InlineData(Iso3166Part1Alpha2Country.PM), InlineData(Iso3166Part1Alpha2Country.PN), InlineData(Iso3166Part1Alpha2Country.PR), InlineData(Iso3166Part1Alpha2Country.RE),
     InlineData(Iso3166Part1Alpha2Country.SJ), InlineData(Iso3166Part1Alpha2Country.SX), InlineData(Iso3166Part1Alpha2Country.TC), InlineData(Iso3166Part1Alpha2Country.TF), InlineData(Iso3166Part1Alpha2Country.TK),
     InlineData(Iso3166Part1Alpha2Country.VA), InlineData(Iso3166Part1Alpha2Country.VG), InlineData(Iso3166Part1Alpha2Country.VI), InlineData(Iso3166Part1Alpha2Country.YT)]
    public void GetValues_Iso3166Part1Alpha2CountryNoSubdivisions_ReturnsEmptySet(Iso3166Part1Alpha2Country testValue)
    {
        // act
        var testResult = Iso3166Part2Enum.GetValues(testValue);

        // assert
        testResult.Should().BeEmpty();
    }

    [Fact]
    public void GetValues_InvalidIso3166Part1Alpha3Country_ReturnsEmptySet()
    {
        // act
        var testResult = Iso3166Part2Enum.GetValues(default(Iso3166Part1Alpha3Country));

        // assert
        testResult.Should().BeEmpty();
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

    [Theory]
    [InlineData(Iso3166Part1Alpha3Country.AIA), InlineData(Iso3166Part1Alpha3Country.ATA), InlineData(Iso3166Part1Alpha3Country.ASM), InlineData(Iso3166Part1Alpha3Country.ABW), InlineData(Iso3166Part1Alpha3Country.ALA),
     InlineData(Iso3166Part1Alpha3Country.BLM), InlineData(Iso3166Part1Alpha3Country.BMU), InlineData(Iso3166Part1Alpha3Country.BVT), InlineData(Iso3166Part1Alpha3Country.CCK), InlineData(Iso3166Part1Alpha3Country.COK),
     InlineData(Iso3166Part1Alpha3Country.CUW), InlineData(Iso3166Part1Alpha3Country.CXR), InlineData(Iso3166Part1Alpha3Country.ESH), InlineData(Iso3166Part1Alpha3Country.FLK), InlineData(Iso3166Part1Alpha3Country.FRO),
     InlineData(Iso3166Part1Alpha3Country.GUF), InlineData(Iso3166Part1Alpha3Country.GGY), InlineData(Iso3166Part1Alpha3Country.GIB), InlineData(Iso3166Part1Alpha3Country.GLP), InlineData(Iso3166Part1Alpha3Country.SGS),
     InlineData(Iso3166Part1Alpha3Country.GUM), InlineData(Iso3166Part1Alpha3Country.HKG), InlineData(Iso3166Part1Alpha3Country.HMD), InlineData(Iso3166Part1Alpha3Country.IMN), InlineData(Iso3166Part1Alpha3Country.IOT),
     InlineData(Iso3166Part1Alpha3Country.JEY), InlineData(Iso3166Part1Alpha3Country.CYM), InlineData(Iso3166Part1Alpha3Country.MAF), InlineData(Iso3166Part1Alpha3Country.MAC), InlineData(Iso3166Part1Alpha3Country.MNP),
     InlineData(Iso3166Part1Alpha3Country.MTQ), InlineData(Iso3166Part1Alpha3Country.MSR), InlineData(Iso3166Part1Alpha3Country.NCL), InlineData(Iso3166Part1Alpha3Country.NFK), InlineData(Iso3166Part1Alpha3Country.NIU),
     InlineData(Iso3166Part1Alpha3Country.PYF), InlineData(Iso3166Part1Alpha3Country.SPM), InlineData(Iso3166Part1Alpha3Country.PCN), InlineData(Iso3166Part1Alpha3Country.PRI), InlineData(Iso3166Part1Alpha3Country.REU),
     InlineData(Iso3166Part1Alpha3Country.SJM), InlineData(Iso3166Part1Alpha3Country.SXM), InlineData(Iso3166Part1Alpha3Country.TCA), InlineData(Iso3166Part1Alpha3Country.ATF), InlineData(Iso3166Part1Alpha3Country.TKL),
     InlineData(Iso3166Part1Alpha3Country.VAT), InlineData(Iso3166Part1Alpha3Country.VGB), InlineData(Iso3166Part1Alpha3Country.VIR), InlineData(Iso3166Part1Alpha3Country.MYT)]
    public void GetValues_Iso3166Part1Alpha3CountryNoSubdivisions_ReturnsEmptySet(Iso3166Part1Alpha3Country testValue)
    {
        // act
        var testResult = Iso3166Part2Enum.GetValues(testValue);

        // assert
        testResult.Should().BeEmpty();
    }
}