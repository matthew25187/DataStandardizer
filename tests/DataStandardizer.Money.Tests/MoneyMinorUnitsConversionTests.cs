using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Money.Tests
{
    public class MoneyMinorUnitsConversionTests
    {
        private const string MinorUnitsResultReason = "the amount in minor units is {0}";
        private const string AmountResultReason = "the conversion gives the amount itself, {0}";
        private const string NoMinorUnitsReason = "the currency has no minor units";
        private const string FractionalMinorUnitsReason = "the amount is not a whole number of minor units";
        private const string OutsideRangeReason = "the amount in minor units is outside the range of the target type";
        private const string UnsignedConversionReason = "a monetary value does not convert to an unsigned integer";

        // ISO 4217 List One gives the US Dollar two minor units, so 1.005 USD is not a whole number of cents.
        private const decimal FractionalUsDollarAmount = 1.005m;
        private const decimal NegativeFractionalUsDollarAmount = -1.005m;

        // An amount in US Dollars that converts to 1234 cents, and whose decimal value is 12.34.
        private const decimal UsDollarAmount = 12.34m;
        private const decimal UsDollarAmountWithTrailingZeros = 12.3400m;
        private const decimal NegativeUsDollarAmount = -12.34m;
        private const double UsDollarAmountAsDouble = 12.34d;
        private const int UsDollarAmountInCents = 1234;
        private const int NegativeUsDollarAmountInCents = -1234;

        // An amount in US Dollars midway between two cents, which rounding to two places resolves.
        private const decimal MidpointUsDollarAmount = 12.345m;

        // An amount in US Dollars midway between two whole dollars, which the rounding method resolves.
        private const decimal MidpointWholeUsDollarAmount = 2.5m;

        // MidpointWholeUsDollarAmount rounded to no places, away from zero.
        private const int RoundedWholeUsDollarAmount = 3;

        // ISO 4217 List One gives the Yen no minor units, so an amount in Yen is its own amount in minor units.
        private const decimal YenAmount = 500m;
        private const int YenAmountInMinorUnits = 500;

        // ISO 4217 List One gives the Bahraini Dinar three minor units (fils).
        private const decimal BahrainiDinarAmount = 1.234m;
        private const int BahrainiDinarAmountInFils = 1234;

        // ISO 4217 List One gives the Unidad Previsional four minor units.
        private const decimal UnidadPrevisionalAmount = 1.2345m;
        private const int UnidadPrevisionalAmountInMinorUnits = 12345;

        private const decimal ZeroAmount = 0m;
        private const int ZeroMinorUnits = 0;

        [Theory]
        [MemberData(nameof(Int64ConversionTestCases))]
        public void ExplicitInt64Operator_AmountIsWholeNumberOfMinorUnits_ReturnsMinorUnits(decimal testAmount, Iso4217CurrencyCurrent testCurrency, long expectedResult)
        {
            // arrange
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            var testResult = (long)testValue;

            // assert
            testResult.Should().Be(expectedResult, MinorUnitsResultReason, expectedResult);
        }

        public static TheoryData<decimal, Iso4217CurrencyCurrent, long> Int64ConversionTestCases => new()
        {
            // ISO 4217 List One: JPY has 0 minor-unit digits, USD 2, BHD 3 and UYW 4.
            { YenAmount, Iso4217CurrencyCurrent.JPY, YenAmountInMinorUnits },
            { UsDollarAmount, Iso4217CurrencyCurrent.USD, UsDollarAmountInCents },
            { BahrainiDinarAmount, Iso4217CurrencyCurrent.BHD, BahrainiDinarAmountInFils },
            { UnidadPrevisionalAmount, Iso4217CurrencyCurrent.UYW, UnidadPrevisionalAmountInMinorUnits },
            { NegativeUsDollarAmount, Iso4217CurrencyCurrent.USD, NegativeUsDollarAmountInCents },
            { -500m, Iso4217CurrencyCurrent.JPY, -500L },
            { ZeroAmount, Iso4217CurrencyCurrent.USD, ZeroMinorUnits },
            { ZeroAmount, Iso4217CurrencyCurrent.JPY, ZeroMinorUnits },
            // Trailing zeros give the amount a larger scale than the currency's minor units.
            { UsDollarAmountWithTrailingZeros, Iso4217CurrencyCurrent.USD, UsDollarAmountInCents },
            { 500.000m, Iso4217CurrencyCurrent.JPY, 500L },
            { 1.2340m, Iso4217CurrencyCurrent.BHD, 1234L },
            // An amount with fewer decimal places than the currency's minor units.
            { 12m, Iso4217CurrencyCurrent.USD, 1200L },
            { 1.2m, Iso4217CurrencyCurrent.UYW, 12000L },
            // The exact boundaries of the range of long.
            { 92233720368547758.07m, Iso4217CurrencyCurrent.USD, long.MaxValue },
            { -92233720368547758.08m, Iso4217CurrencyCurrent.USD, long.MinValue },
            { 9223372036854775807m, Iso4217CurrencyCurrent.JPY, long.MaxValue },
            { -9223372036854775808m, Iso4217CurrencyCurrent.JPY, long.MinValue },
            { 922337203685477.5807m, Iso4217CurrencyCurrent.UYW, long.MaxValue },
            { -922337203685477.5808m, Iso4217CurrencyCurrent.UYW, long.MinValue },
        };

        [Theory]
        [MemberData(nameof(Int32ConversionTestCases))]
        public void ExplicitInt32Operator_AmountIsWholeNumberOfMinorUnits_ReturnsMinorUnits(decimal testAmount, Iso4217CurrencyCurrent testCurrency, int expectedResult)
        {
            // arrange
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            var testResult = (int)testValue;

            // assert
            testResult.Should().Be(expectedResult, MinorUnitsResultReason, expectedResult);
        }

        public static TheoryData<decimal, Iso4217CurrencyCurrent, int> Int32ConversionTestCases => new()
        {
            { YenAmount, Iso4217CurrencyCurrent.JPY, YenAmountInMinorUnits },
            { UsDollarAmount, Iso4217CurrencyCurrent.USD, UsDollarAmountInCents },
            { BahrainiDinarAmount, Iso4217CurrencyCurrent.BHD, BahrainiDinarAmountInFils },
            { UnidadPrevisionalAmount, Iso4217CurrencyCurrent.UYW, UnidadPrevisionalAmountInMinorUnits },
            { NegativeUsDollarAmount, Iso4217CurrencyCurrent.USD, NegativeUsDollarAmountInCents },
            { ZeroAmount, Iso4217CurrencyCurrent.USD, ZeroMinorUnits },
            { UsDollarAmountWithTrailingZeros, Iso4217CurrencyCurrent.USD, UsDollarAmountInCents },
            // The exact boundaries of the range of int.
            { 21474836.47m, Iso4217CurrencyCurrent.USD, int.MaxValue },
            { -21474836.48m, Iso4217CurrencyCurrent.USD, int.MinValue },
            { 2147483647m, Iso4217CurrencyCurrent.JPY, int.MaxValue },
            { -2147483648m, Iso4217CurrencyCurrent.JPY, int.MinValue },
        };

        [Theory]
        [MemberData(nameof(Int16ConversionTestCases))]
        public void ExplicitInt16Operator_AmountIsWholeNumberOfMinorUnits_ReturnsMinorUnits(decimal testAmount, Iso4217CurrencyCurrent testCurrency, short expectedResult)
        {
            // arrange
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            var testResult = (short)testValue;

            // assert
            testResult.Should().Be(expectedResult, MinorUnitsResultReason, expectedResult);
        }

        public static TheoryData<decimal, Iso4217CurrencyCurrent, short> Int16ConversionTestCases => new()
        {
            { YenAmount, Iso4217CurrencyCurrent.JPY, YenAmountInMinorUnits },
            { UsDollarAmount, Iso4217CurrencyCurrent.USD, UsDollarAmountInCents },
            { BahrainiDinarAmount, Iso4217CurrencyCurrent.BHD, BahrainiDinarAmountInFils },
            { UnidadPrevisionalAmount, Iso4217CurrencyCurrent.UYW, UnidadPrevisionalAmountInMinorUnits },
            { NegativeUsDollarAmount, Iso4217CurrencyCurrent.USD, NegativeUsDollarAmountInCents },
            { ZeroAmount, Iso4217CurrencyCurrent.USD, ZeroMinorUnits },
            { UsDollarAmountWithTrailingZeros, Iso4217CurrencyCurrent.USD, UsDollarAmountInCents },
            // The exact boundaries of the range of short.
            { 327.67m, Iso4217CurrencyCurrent.USD, short.MaxValue },
            { -327.68m, Iso4217CurrencyCurrent.USD, short.MinValue },
            { 32767m, Iso4217CurrencyCurrent.JPY, short.MaxValue },
            { -32768m, Iso4217CurrencyCurrent.JPY, short.MinValue },
        };

        [Theory]
        [MemberData(nameof(SByteConversionTestCases))]
        public void ExplicitSByteOperator_AmountIsWholeNumberOfMinorUnits_ReturnsMinorUnits(decimal testAmount, Iso4217CurrencyCurrent testCurrency, sbyte expectedResult)
        {
            // arrange
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            var testResult = (sbyte)testValue;

            // assert
            testResult.Should().Be(expectedResult, MinorUnitsResultReason, expectedResult);
        }

        public static TheoryData<decimal, Iso4217CurrencyCurrent, sbyte> SByteConversionTestCases => new()
        {
            { 100m, Iso4217CurrencyCurrent.JPY, 100 },
            { 1.23m, Iso4217CurrencyCurrent.USD, 123 },
            { 0.123m, Iso4217CurrencyCurrent.BHD, 123 },
            { 0.0123m, Iso4217CurrencyCurrent.UYW, 123 },
            { -1.23m, Iso4217CurrencyCurrent.USD, -123 },
            { ZeroAmount, Iso4217CurrencyCurrent.USD, ZeroMinorUnits },
            { 1.2300m, Iso4217CurrencyCurrent.USD, 123 },
            // The exact boundaries of the range of sbyte.
            { 1.27m, Iso4217CurrencyCurrent.USD, sbyte.MaxValue },
            { -1.28m, Iso4217CurrencyCurrent.USD, sbyte.MinValue },
            { 127m, Iso4217CurrencyCurrent.JPY, sbyte.MaxValue },
            { -128m, Iso4217CurrencyCurrent.JPY, sbyte.MinValue },
        };

        [Theory]
        [MemberData(nameof(RoundingPrecisionTestCases))]
        public void ExplicitInt64Operator_AmountMadeWholeNumberByRoundingPrecision_ReturnsRoundedMinorUnits(decimal testAmount, Iso4217CurrencyCurrent testCurrency, int testRoundingPrecision, long expectedResult)
        {
            // arrange
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision);

            // act
            var testResult = (long)testValue;

            // assert
            testResult.Should().Be(expectedResult, MinorUnitsResultReason, expectedResult);
        }

        public static TheoryData<decimal, Iso4217CurrencyCurrent, int, long> RoundingPrecisionTestCases => new()
        {
            // Without a rounding method, the amount is rounded to the nearest even digit.
            { FractionalUsDollarAmount, Iso4217CurrencyCurrent.USD, 2, 100L },
            { MidpointUsDollarAmount, Iso4217CurrencyCurrent.USD, 2, 1234L },
            { 499.6m, Iso4217CurrencyCurrent.JPY, 0, 500L },
            { 1.23456m, Iso4217CurrencyCurrent.BHD, 3, 1235L },
            // A precision smaller than the currency's minor units.
            { UsDollarAmount, Iso4217CurrencyCurrent.USD, 1, 1230L },
        };

        [Theory]
        [MemberData(nameof(RoundingMethodTestCases))]
        public void ExplicitInt64Operator_AmountMadeWholeNumberByRoundingMethod_ReturnsRoundedMinorUnits(decimal testAmount, Iso4217CurrencyCurrent testCurrency, int testRoundingPrecision, MidpointRounding testRoundingMethod, long expectedResult)
        {
            // arrange
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision, testRoundingMethod);

            // act
            var testResult = (long)testValue;

            // assert
            testResult.Should().Be(expectedResult, MinorUnitsResultReason, expectedResult);
        }

        public static TheoryData<decimal, Iso4217CurrencyCurrent, int, MidpointRounding, long> RoundingMethodTestCases => new()
        {
            { FractionalUsDollarAmount, Iso4217CurrencyCurrent.USD, 2, MidpointRounding.AwayFromZero, 101L },
            { FractionalUsDollarAmount, Iso4217CurrencyCurrent.USD, 2, MidpointRounding.ToEven, 100L },
            { NegativeFractionalUsDollarAmount, Iso4217CurrencyCurrent.USD, 2, MidpointRounding.AwayFromZero, -101L },
        };

        [Fact]
        public void ExplicitInt64Operator_RoundingPrecisionBeyondMinorUnits_ThrowsInvalidCastException()
        {
            // arrange
            // A precision of 3 keeps the third decimal place of an amount in US Dollars, which have 2 minor units.
            var testValue = Money.Create(FractionalUsDollarAmount, Iso4217CurrencyCurrent.USD, 3);

            // act
            Action testAction = () => _ = (long)testValue;

            // assert
            testAction.Should().Throw<InvalidCastException>(FractionalMinorUnitsReason);
        }

        [Theory]
        [MemberData(nameof(FractionalAmountTestCases))]
        public void ExplicitInt64Operator_AmountNotWholeNumberOfMinorUnits_ThrowsInvalidCastException(decimal testAmount, Iso4217CurrencyCurrent testCurrency)
        {
            // arrange
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            Action testAction = () => _ = (long)testValue;

            // assert
            testAction.Should().Throw<InvalidCastException>(FractionalMinorUnitsReason);
        }

        public static TheoryData<decimal, Iso4217CurrencyCurrent> FractionalAmountTestCases => new()
        {
            { 0.5m, Iso4217CurrencyCurrent.JPY },
            { FractionalUsDollarAmount, Iso4217CurrencyCurrent.USD },
            { NegativeFractionalUsDollarAmount, Iso4217CurrencyCurrent.USD },
            { 0.001m, Iso4217CurrencyCurrent.USD },
            { 1.2345m, Iso4217CurrencyCurrent.BHD },
            { 1.23456m, Iso4217CurrencyCurrent.UYW },
            { 0.0000000000000000000000000001m, Iso4217CurrencyCurrent.USD },
        };

        [Theory]
        [MemberData(nameof(SignedConversionTestCases))]
        public void ExplicitSignedOperator_AmountNotWholeNumberOfMinorUnits_ThrowsInvalidCastException(Action<Money> testConversion)
        {
            // arrange
            var testValue = Money.Create(FractionalUsDollarAmount, Iso4217CurrencyCurrent.USD);

            // act
            Action testAction = () => testConversion(testValue);

            // assert
            testAction.Should().Throw<InvalidCastException>(FractionalMinorUnitsReason);
        }

        [Theory]
        [MemberData(nameof(SignedConversionTestCases))]
        public void ExplicitSignedOperator_DefaultValue_ThrowsInvalidCastException(Action<Money> testConversion)
        {
            // arrange
            var testValue = default(Money);

            // act
            Action testAction = () => testConversion(testValue);

            // assert
            testAction.Should().Throw<InvalidCastException>(NoMinorUnitsReason);
        }

        [Theory]
        [MemberData(nameof(SignedConversionTestCases))]
        public void ExplicitSignedOperator_NoCurrency_ThrowsInvalidCastException(Action<Money> testConversion)
        {
            // arrange
            var testValue = Money.Create(1m);

            // act
            Action testAction = () => testConversion(testValue);

            // assert
            testAction.Should().Throw<InvalidCastException>(NoMinorUnitsReason);
        }

        [Theory]
        [MemberData(nameof(SignedConversionTestCases))]
        public void ExplicitSignedOperator_TestingCurrency_ThrowsInvalidCastException(Action<Money> testConversion)
        {
            // arrange
            var testValue = Money.Create(1m, Iso4217CurrencyCurrent.XTS);

            // act
            Action testAction = () => testConversion(testValue);

            // assert
            testAction.Should().Throw<InvalidCastException>(NoMinorUnitsReason);
        }

        public static TheoryData<Action<Money>> SignedConversionTestCases => new()
        {
            value => _ = (long)value,
            value => _ = (int)value,
            value => _ = (short)value,
            value => _ = (sbyte)value,
        };

        [Theory]
        [MemberData(nameof(Int64OutsideRangeTestCases))]
        public void ExplicitInt64Operator_AmountOutsideRange_ThrowsOverflowException(decimal testAmount, Iso4217CurrencyCurrent testCurrency)
        {
            // arrange
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            Action testAction = () => _ = (long)testValue;

            // assert
            testAction.Should().Throw<OverflowException>(OutsideRangeReason);
        }

        public static TheoryData<decimal, Iso4217CurrencyCurrent> Int64OutsideRangeTestCases => new()
        {
            { 92233720368547758.08m, Iso4217CurrencyCurrent.USD },
            { -92233720368547758.09m, Iso4217CurrencyCurrent.USD },
            { 9223372036854775808m, Iso4217CurrencyCurrent.JPY },
            { -9223372036854775809m, Iso4217CurrencyCurrent.JPY },
            { 922337203685477.5808m, Iso4217CurrencyCurrent.UYW },
            { -922337203685477.5809m, Iso4217CurrencyCurrent.UYW },
            { decimal.MaxValue, Iso4217CurrencyCurrent.USD },
            { decimal.MinValue, Iso4217CurrencyCurrent.USD },
        };

        [Theory]
        [MemberData(nameof(Int32OutsideRangeTestCases))]
        public void ExplicitInt32Operator_AmountOutsideRange_ThrowsOverflowException(decimal testAmount, Iso4217CurrencyCurrent testCurrency)
        {
            // arrange
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            Action testAction = () => _ = (int)testValue;

            // assert
            testAction.Should().Throw<OverflowException>(OutsideRangeReason);
        }

        public static TheoryData<decimal, Iso4217CurrencyCurrent> Int32OutsideRangeTestCases => new()
        {
            { 21474836.48m, Iso4217CurrencyCurrent.USD },
            { -21474836.49m, Iso4217CurrencyCurrent.USD },
            { 2147483648m, Iso4217CurrencyCurrent.JPY },
            { -2147483649m, Iso4217CurrencyCurrent.JPY },
        };

        [Theory]
        [MemberData(nameof(Int16OutsideRangeTestCases))]
        public void ExplicitInt16Operator_AmountOutsideRange_ThrowsOverflowException(decimal testAmount, Iso4217CurrencyCurrent testCurrency)
        {
            // arrange
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            Action testAction = () => _ = (short)testValue;

            // assert
            testAction.Should().Throw<OverflowException>(OutsideRangeReason);
        }

        public static TheoryData<decimal, Iso4217CurrencyCurrent> Int16OutsideRangeTestCases => new()
        {
            { 327.68m, Iso4217CurrencyCurrent.USD },
            { -327.69m, Iso4217CurrencyCurrent.USD },
            { 32768m, Iso4217CurrencyCurrent.JPY },
            { -32769m, Iso4217CurrencyCurrent.JPY },
        };

        [Theory]
        [MemberData(nameof(SByteOutsideRangeTestCases))]
        public void ExplicitSByteOperator_AmountOutsideRange_ThrowsOverflowException(decimal testAmount, Iso4217CurrencyCurrent testCurrency)
        {
            // arrange
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            Action testAction = () => _ = (sbyte)testValue;

            // assert
            testAction.Should().Throw<OverflowException>(OutsideRangeReason);
        }

        public static TheoryData<decimal, Iso4217CurrencyCurrent> SByteOutsideRangeTestCases => new()
        {
            { 1.28m, Iso4217CurrencyCurrent.USD },
            { -1.29m, Iso4217CurrencyCurrent.USD },
            { 128m, Iso4217CurrencyCurrent.JPY },
            { -129m, Iso4217CurrencyCurrent.JPY },
        };

        [Theory]
        [MemberData(nameof(FractionalOutsideRangeTestCases))]
        public void ExplicitSignedOperator_AmountFractionalAndOutsideRange_ThrowsInvalidCastException(Action<Money> testConversion, decimal testAmount, Iso4217CurrencyCurrent testCurrency)
        {
            // arrange
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            Action testAction = () => testConversion(testValue);

            // assert
            testAction.Should().Throw<InvalidCastException>(FractionalMinorUnitsReason);
        }

        public static TheoryData<Action<Money>, decimal, Iso4217CurrencyCurrent> FractionalOutsideRangeTestCases => new()
        {
            // Each amount lies between the target's boundary and the next whole number of minor units beyond it.
            { value => _ = (long)value, 92233720368547758.075m, Iso4217CurrencyCurrent.USD },
            { value => _ = (long)value, -92233720368547758.085m, Iso4217CurrencyCurrent.USD },
            { value => _ = (long)value, 9223372036854775807.5m, Iso4217CurrencyCurrent.JPY },
            { value => _ = (int)value, 21474836.475m, Iso4217CurrencyCurrent.USD },
            { value => _ = (int)value, -21474836.485m, Iso4217CurrencyCurrent.USD },
            { value => _ = (short)value, 327.675m, Iso4217CurrencyCurrent.USD },
            { value => _ = (short)value, -327.685m, Iso4217CurrencyCurrent.USD },
            { value => _ = (sbyte)value, 1.275m, Iso4217CurrencyCurrent.USD },
            { value => _ = (sbyte)value, -1.285m, Iso4217CurrencyCurrent.USD },
            { value => _ = (sbyte)value, 127.5m, Iso4217CurrencyCurrent.JPY },
            // Far outside every range.
            { value => _ = (sbyte)value, decimal.MaxValue / 1000m, Iso4217CurrencyCurrent.USD },
        };

        [Fact]
        public void ExplicitNativeIntegerConversion_AmountIsWholeNumberOfMinorUnits_ReturnsMinorUnits()
        {
            // arrange
            // C# resolves (nint)money through the explicit conversion to int.
            const nint expectedResult = UsDollarAmountInCents;
            var testValue = Money.Create(UsDollarAmount, Iso4217CurrencyCurrent.USD);

            // act
            var testResult = (nint)testValue;

            // assert
            testResult.Should().Be(expectedResult, MinorUnitsResultReason, expectedResult);
        }

        [Theory]
        [MemberData(nameof(UnsignedConversionTestCases))]
        public void ExplicitUnsignedOperator_ReachedDynamically_ThrowsInvalidCastException(Action<object> testConversion)
        {
            // arrange
            // C# rejects (uint)money at compile time, so the operator is reached through the runtime binder.
            object testValue = Money.Create(1m, Iso4217CurrencyCurrent.USD);

            // act
            Action testAction = () => testConversion(testValue);

            // assert
            testAction.Should().Throw<InvalidCastException>(UnsignedConversionReason);
        }

        public static TheoryData<Action<object>> UnsignedConversionTestCases => new()
        {
            value => _ = (ulong)(dynamic)value,
            value => _ = (uint)(dynamic)value,
            value => _ = (ushort)(dynamic)value,
            value => _ = (byte)(dynamic)value,
        };

        [Fact]
        public void ExplicitDoubleOperator_MoneyValue_ReturnsAmount()
        {
            // arrange
            const double expectedResult = UsDollarAmountAsDouble;
            var testValue = Money.Create(UsDollarAmount, Iso4217CurrencyCurrent.USD);

            // act
            var testResult = (double)testValue;

            // assert
            testResult.Should().Be(expectedResult, AmountResultReason, expectedResult);
        }

        [Fact]
        public void ExplicitDoubleOperator_MoneyValueWithRoundingPrecision_ReturnsRoundedAmount()
        {
            // arrange
            const double expectedResult = UsDollarAmountAsDouble;
            var testValue = Money.Create(MidpointUsDollarAmount, Iso4217CurrencyCurrent.USD, 2);

            // act
            var testResult = (double)testValue;

            // assert
            testResult.Should().Be(expectedResult, AmountResultReason, expectedResult);
        }

        [Fact]
        public void ExplicitSingleOperator_MoneyValue_ReturnsAmount()
        {
            // arrange
            const float expectedResult = 12.34f;
            var testValue = Money.Create(UsDollarAmount, Iso4217CurrencyCurrent.USD);

            // act
            var testResult = (float)testValue;

            // assert
            testResult.Should().Be(expectedResult, AmountResultReason, expectedResult);
        }

        [Fact]
        public void ExplicitDoubleOperator_NoCurrency_ReturnsAmount()
        {
            // arrange
            const double expectedResult = UsDollarAmountAsDouble;
            var testValue = Money.Create(UsDollarAmount);

            // act
            var testResult = (double)testValue;

            // assert
            testResult.Should().Be(expectedResult, AmountResultReason, expectedResult);
        }

        [Fact]
        public void ConvertibleToInt64_MoneyValue_ReturnsAmountRoundedToInteger()
        {
            // arrange
            // IConvertible.ToInt64 converts the decimal amount, 12.34, which rounds to 12.
            const long expectedResult = 12L;
            IConvertible testValue = Money.Create(UsDollarAmount, Iso4217CurrencyCurrent.USD);

            // act
            var testResult = testValue.ToInt64(CultureInfo.InvariantCulture);

            // assert
            testResult.Should().Be(expectedResult, AmountResultReason, expectedResult);
        }

        [Fact]
        public void ConvertibleToDecimal_MoneyValueWithRoundingPrecision_ReturnsRoundedAmount()
        {
            // arrange
            // 12.345 rounded to two places, with ties to even, is 12.34.
            const decimal expectedResult = UsDollarAmount;
            IConvertible testValue = Money.Create(MidpointUsDollarAmount, Iso4217CurrencyCurrent.USD, 2);

            // act
            var testResult = testValue.ToDecimal(CultureInfo.InvariantCulture);

            // assert
            testResult.Should().Be(expectedResult, AmountResultReason, expectedResult);
        }

        [Theory]
        [MemberData(nameof(ConvertibleRoundedAmountTestCases), DisableDiscoveryEnumeration = true)]
        public void ConvertibleMember_MoneyValueWithRoundingMethod_ConvertsRoundedAmount(Func<IConvertible, object> testConversion, object expectedResult)
        {
            // arrange
            // 2.5 rounded to no places, away from zero, is 3. Before rounding, an integer conversion gives 2,
            // since it rounds ties to even.
            IConvertible testValue = Money.Create(MidpointWholeUsDollarAmount, Iso4217CurrencyCurrent.USD, 0, MidpointRounding.AwayFromZero);

            // act
            var testResult = testConversion(testValue);

            // assert
            testResult.Should().Be(expectedResult, AmountResultReason, expectedResult);
        }

        public static TheoryData<Func<IConvertible, object>, object> ConvertibleRoundedAmountTestCases => new()
        {
            { value => value.ToByte(CultureInfo.InvariantCulture), (byte)RoundedWholeUsDollarAmount },
            { value => value.ToDecimal(CultureInfo.InvariantCulture), (decimal)RoundedWholeUsDollarAmount },
            { value => value.ToDouble(CultureInfo.InvariantCulture), (double)RoundedWholeUsDollarAmount },
            { value => value.ToInt16(CultureInfo.InvariantCulture), (short)RoundedWholeUsDollarAmount },
            { value => value.ToInt32(CultureInfo.InvariantCulture), RoundedWholeUsDollarAmount },
            { value => value.ToInt64(CultureInfo.InvariantCulture), (long)RoundedWholeUsDollarAmount },
            { value => value.ToSByte(CultureInfo.InvariantCulture), (sbyte)RoundedWholeUsDollarAmount },
            { value => value.ToSingle(CultureInfo.InvariantCulture), (float)RoundedWholeUsDollarAmount },
            { value => value.ToUInt16(CultureInfo.InvariantCulture), (ushort)RoundedWholeUsDollarAmount },
            { value => value.ToUInt32(CultureInfo.InvariantCulture), (uint)RoundedWholeUsDollarAmount },
            { value => value.ToUInt64(CultureInfo.InvariantCulture), (ulong)RoundedWholeUsDollarAmount },
            { value => value.ToType(typeof(int), CultureInfo.InvariantCulture), RoundedWholeUsDollarAmount },
            { value => Convert.ToInt32((object)value, CultureInfo.InvariantCulture), RoundedWholeUsDollarAmount }
        };

        [Fact]
        public void ImplicitDecimalOperator_MoneyValue_ReturnsAmount()
        {
            // arrange
            const decimal expectedResult = UsDollarAmount;
            var testValue = Money.Create(UsDollarAmount, Iso4217CurrencyCurrent.USD);

            // act
            decimal testResult = testValue;

            // assert
            testResult.Should().Be(expectedResult, AmountResultReason, expectedResult);
        }
    }
}
