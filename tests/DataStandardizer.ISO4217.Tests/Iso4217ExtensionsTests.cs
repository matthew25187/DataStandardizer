using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.ISO4217.Tests
{
    public class Iso4217ExtensionsTests
    {
        #region Test: GetCurrencyName_OnIso4217CurrentCurrencyCode

        [Theory]
        [InlineData(Iso4217Current.INR, "Indian Rupee")]
        [InlineData(Iso4217Current.NOK, "Norwegian Krone")]
        [InlineData(Iso4217Current.USD, "US Dollar")]
        public void GetCurrencyName_OnIso4217CurrentCurrencyCode_ReturnsCurrencyName(Iso4217Current testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetCurrencyName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: GetCurrencyName_OnIso4217HistoricCurrencyCode

        [Theory]
        [InlineData(Iso4217Historic.BEF, "Belgian Franc")]
        [InlineData(Iso4217Historic.ZWC, "Rhodesian Dollar")]
        public void GetCurrencyName_OnIso4217HistoricCurrencyCode_ReturnsCurrencyName(Iso4217Historic testCode, string? expectedResult)
        {
            // act
            var testResult = testCode.GetCurrencyName();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: GetMinorUnits_OnIso4217CurrentCurrencyCode

        [Theory]
        [MemberData(nameof(GetMinorUnits_OnIso4217CurrentCurrencyCode_TestCaseGenerator.TestCases), MemberType = typeof(GetMinorUnits_OnIso4217CurrentCurrencyCode_TestCaseGenerator))]
        public void GetMinorUnits_OnIso4217CurrentCurrencyCode_ReturnsMinorDigits(Iso4217Current testCode, byte? expectedResult)
        {
            // act
            var testResult = testCode.GetMinorUnits();

            // assert
            testResult.Should().Be(expectedResult);
        }

        private class GetMinorUnits_OnIso4217CurrentCurrencyCode_TestCaseGenerator
        {
            public static IEnumerable<object?[]> TestCases
            {
                get
                {
                    yield return new object[] { Iso4217Current.BIF, (byte)0 };
                    yield return new object[] { Iso4217Current.USD, (byte)2 };
                    yield return new object[] { Iso4217Current.BHD, (byte)3 };
                    yield return new object[] { Iso4217Current.UYW, (byte)4 };
                    yield return new object?[] { Iso4217Current.XTS, null };
                }
            }
        }

        #endregion

        #region Test: IsFundCode_OnIso4217CurrentCurrencyCode

        [Theory]
        [InlineData(Iso4217Current.GBP, false)]
        [InlineData(Iso4217Current.CLF, true)]
        public void IsFundCode_OnIso4217CurrentCurrencyCode_ReturnsFundsFlag(Iso4217Current testCode, bool expectedResult)
        {
            // act
            var testResult = testCode.IsFundCode();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: IsNationalCurrency_OnIso4217CurrentCurrencyCode

        [Theory]
        [InlineData(Iso4217Current.EUR, true)]
        [InlineData(Iso4217Current.AUD, true)]
        [InlineData(Iso4217Current.XAU, false)]
        [InlineData(Iso4217Current.XTS, false)]
        public void IsNationalCurrency_OnIso4217CurrentCurrencyCode_ReturnsNationalCurrencyFlag(Iso4217Current testCode, bool expectedResult)
        {
            // act
            var testResult = testCode.IsNationalCurrency();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: IsSupranationalCurrency_OnIso4217CurrentCurrencyCode

        [Theory]
        [InlineData(Iso4217Current.TWD, false)]
        [InlineData(Iso4217Current.NZD, false)]
        [InlineData(Iso4217Current.ZAR, false)]
        [InlineData(Iso4217Current.XAF, true)]
        [InlineData(Iso4217Current.XCD, true)]
        [InlineData(Iso4217Current.XCG, true)]
        [InlineData(Iso4217Current.XOF, true)]
        [InlineData(Iso4217Current.XPF,true)]
        public void IsSupranationalCurrency_OnIso4217CurrentCurrencyCode_ReturnsSupranationalCurrencyFlag(Iso4217Current testCode, bool expectedResult)
        {
            // act
            var testResult = testCode.IsSupranationalCurrency();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion
    }
}