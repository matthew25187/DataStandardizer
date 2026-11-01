using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Money.Tests
{
    public class MoneyTests
    {
        private const string CurrencyNotSetReason = "the currency of the result is not set";
        private const string RoundingPrecisionNotSetReason = "the rounding precision of the result is not set";
        private const string RoundingMethodNotSetReason = "the rounding method of the result is not set";
        private const string ResultCurrencyReason = "the currency of the result is {0}";
        private const string ResultRoundingPrecisionReason = "the rounding precision of the result is {0}";
        private const string ResultRoundingMethodReason = "the rounding method of the result is {0}";
        private const string AdditionResultReason = "the result of the addition operation is {0}";
        private const string SubtractionResultReason = "the result of the subtraction operation is {0}";
        private const string MultiplicationResultReason = "the result of the multiplication operation is {0}";
        private const string DivisionResultReason = "the result of the division operation is {0}";

        private const string LeftLessThanRightReason = "the left operand {0} is less than the right operand {1}";
        private const string LeftEqualToRightReason = "the left operand {0} is equal to the right operand {1}";
        private const string LeftGreaterThanRightReason = "the left operand {0} is greater than the right operand {1}";
        private const string LeftNotEqualToRightReason = "the left operand {0} is not equal to the right operand {1}";

        private const string NoRoundingReason = "the money value has no rounding";
        private const string InputIsNullReason = "the input is null";
        private const string InputIsBlankReason = "the input is blank";
        private const string InputIsValidReason = "the input is a valid money value";
        private const string MoneyValueContainsAmountReason = "the money value contains the amount {0}";
        private const string InputContainsMoneyValueReason = "the input contains the money value {0}";
        private const string ArgumentIsNotMoneyValueReason = "the argument is not a money value";

        private const string NotAMoneyValue = "not a money value";
        private const decimal ObjectTestAmount = 1234m;

        // ISO 4217 List One gives the US Dollar two minor units.
        private const int UsDollarMinorUnits = 2;

        [Fact]
        public void AdditionOperator_MoneyValueWithAmount_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1234, testAdditionOperand = 13, expectedResult = 1247;
            var testValue = Money.Create(testAmount);

            // act
            var testResult = testValue + testAdditionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, AdditionResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.XXX, CurrencyNotSetReason);
            testResult.RoundingPrecision.Should().BeNull(RoundingPrecisionNotSetReason);
            testResult.RoundingMethod.Should().BeNull(RoundingMethodNotSetReason);
        }

        [Fact]
        public void AdditionOperator_MoneyValueWithAmountAndCurrency_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1357, testAdditionOperand = 24, expectedResult = 1381;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            var testResult = testValue + testAdditionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, AdditionResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, ResultCurrencyReason, testCurrency);
            testResult.RoundingPrecision.Should().BeNull(RoundingPrecisionNotSetReason);
            testResult.RoundingMethod.Should().BeNull(RoundingMethodNotSetReason);
        }

        [Fact]
        public void AdditionOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecision_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 2468, testAdditionOperand = 57, expectedResult = 2525;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 4;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision);

            // act
            var testResult = testValue + testAdditionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, AdditionResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, ResultCurrencyReason, testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, ResultRoundingPrecisionReason, testRoundingPrecision);
            testResult.RoundingMethod.Should().BeNull(RoundingMethodNotSetReason);
        }

        [Fact]
        public void AdditionOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecisionAndRoundingMethod_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 3579, testAdditionOperand = 68, expectedResult = 3647;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 3;
            const MidpointRounding testRoundingMethod = MidpointRounding.AwayFromZero;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision, testRoundingMethod);

            // act
            var testResult = testValue + testAdditionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, AdditionResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, ResultCurrencyReason, testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, ResultRoundingPrecisionReason, testRoundingPrecision);
            testResult.RoundingMethod.Should().Be(testRoundingMethod, ResultRoundingMethodReason, testRoundingMethod);
        }

        [Fact]
        public void SubtractionOperator_MoneyValueWithAmount_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1234, testSubtractionOperand = 12, expectedResult = 1222;
            var testValue = Money.Create(testAmount);

            // act
            var testResult = testValue - testSubtractionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, SubtractionResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.XXX, CurrencyNotSetReason);
            testResult.RoundingPrecision.Should().BeNull(RoundingPrecisionNotSetReason);
            testResult.RoundingMethod.Should().BeNull(RoundingMethodNotSetReason);
        }

        [Fact]
        public void SubtractionOperator_MoneyValueWithAmountAndCurrency_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 5678, testSubtractionOperand = 34, expectedResult = 5644;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            var testResult = testValue - testSubtractionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, SubtractionResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, ResultCurrencyReason, testCurrency);
            testResult.RoundingPrecision.Should().BeNull(RoundingPrecisionNotSetReason);
            testResult.RoundingMethod.Should().BeNull(RoundingMethodNotSetReason);
        }

        [Fact]
        public void SubtractionOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecision_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1357, testSubtractionOperand = 56, expectedResult = 1301;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 4;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision);

            // act
            var testResult = testValue - testSubtractionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, SubtractionResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, ResultCurrencyReason, testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, ResultRoundingPrecisionReason, testRoundingPrecision);
            testResult.RoundingMethod.Should().BeNull(RoundingMethodNotSetReason);
        }

        [Fact]
        public void SubtractionOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecisionAndRoundingMethod_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 2468, testSubtractionOperand = 78, expectedResult = 2390;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 3;
            const MidpointRounding testRoundingMethod = MidpointRounding.ToEven;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision, testRoundingMethod);

            // act
            var testResult = testValue - testSubtractionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, SubtractionResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, ResultCurrencyReason, testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, ResultRoundingPrecisionReason, testRoundingPrecision);
            testResult.RoundingMethod.Should().Be(testRoundingMethod, ResultRoundingMethodReason, testRoundingMethod);
        }

        [Fact]
        public void MultiplicationOperator_MoneyValueWithAmount_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1234, testMultiplicationOperand = 12, expectedResult = 14808;
            var testValue = Money.Create(testAmount);

            // act
            var testResult = testValue * testMultiplicationOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, MultiplicationResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.XXX, CurrencyNotSetReason);
            testResult.RoundingPrecision.Should().BeNull(RoundingPrecisionNotSetReason);
            testResult.RoundingMethod.Should().BeNull(RoundingMethodNotSetReason);
        }

        [Fact]
        public void MultiplicationOperator_MoneyValueWithAmountAndCurrency_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 5678, testMultiplicationOperand = 34, expectedResult = 193052;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            var testResult = testValue * testMultiplicationOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, MultiplicationResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, ResultCurrencyReason, testCurrency);
            testResult.RoundingPrecision.Should().BeNull(RoundingPrecisionNotSetReason);
            testResult.RoundingMethod.Should().BeNull(RoundingMethodNotSetReason);
        }

        [Fact]
        public void MultiplicationOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecision_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1357, testMultiplicationOperand = 56, expectedResult = 75992;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 4;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision);

            // act
            var testResult = testValue * testMultiplicationOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, MultiplicationResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, ResultCurrencyReason, testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, ResultRoundingPrecisionReason, testRoundingPrecision);
            testResult.RoundingMethod.Should().BeNull(RoundingMethodNotSetReason);
        }

        [Fact]
        public void MultiplicationOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecisionAndRoundingMethod_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 2468, testMultiplicationOperand = 78, expectedResult = 192504;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 3;
            const MidpointRounding testRoundingMethod = MidpointRounding.AwayFromZero;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision, testRoundingMethod);

            // act
            var testResult = testValue * testMultiplicationOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, MultiplicationResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, ResultCurrencyReason, testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, ResultRoundingPrecisionReason, testRoundingPrecision);
            testResult.RoundingMethod.Should().Be(testRoundingMethod, ResultRoundingMethodReason, testRoundingMethod);
        }

        [Fact]
        public void DivisionOperator_MoneyValueWithAmount_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 2709, testDivisionOperand = 63, expectedResult = 43;
            var testValue = Money.Create(testAmount);

            // act
            var testResult = testValue / testDivisionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, DivisionResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.XXX, CurrencyNotSetReason);
            testResult.RoundingPrecision.Should().BeNull(RoundingPrecisionNotSetReason);
            testResult.RoundingMethod.Should().BeNull(RoundingMethodNotSetReason);
        }

        [Fact]
        public void DivisionOperator_MoneyValueWithAmountAndCurrency_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1553, testDivisionOperand = 25, expectedResult = 62.12m;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            var testValue = Money.Create(testAmount, testCurrency);

            // act
            var testResult = testValue / testDivisionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, DivisionResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, ResultCurrencyReason, testCurrency);
            testResult.RoundingPrecision.Should().BeNull(RoundingPrecisionNotSetReason);
            testResult.RoundingMethod.Should().BeNull(RoundingMethodNotSetReason);
        }

        [Fact]
        public void DivisionOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecision_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 2542, testDivisionOperand = 25, expectedResult = 101.68m;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 4;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision);

            // act
            var testResult = testValue / testDivisionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, DivisionResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, ResultCurrencyReason, testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, ResultRoundingPrecisionReason, testRoundingPrecision);
            testResult.RoundingMethod.Should().BeNull(RoundingMethodNotSetReason);
        }

        [Fact]
        public void DivisionOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecisionAndRoundingMethod_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 8127, testDivisionOperand = 36, expectedResult = 225.75m;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 3;
            const MidpointRounding testRoundingMethod = MidpointRounding.AwayFromZero;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision, testRoundingMethod);

            // act
            var testResult = testValue / testDivisionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, DivisionResultReason, expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, ResultCurrencyReason, testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, ResultRoundingPrecisionReason, testRoundingPrecision);
            testResult.RoundingMethod.Should().Be(testRoundingMethod, ResultRoundingMethodReason, testRoundingMethod);
        }

        [Fact]
        public void LessThanOperator_OperandCurrenciesDifferent_ThrowsInvalidOperationException()
        {
            // arrange
            Money testValue1 = Money.Create(1535m, Iso4217CurrencyCurrent.USD), testValue2 = Money.Create(8127m, Iso4217CurrencyCurrent.GBP);

            // act
            Action testAction = () => _ = testValue1 < testValue2;

            // assert
            testAction.Should().Throw<InvalidOperationException>("comparing money values with different currencies is not supported");
        }

        [Fact]
        public void LessThanOperator_MoneyValueLessThanOperand_ReturnsTrue()
        {
            // arrange
            Money testValueLeft = Money.Create(13), testValueRight = Money.Create(57);

            // act
            var testResult = testValueLeft < testValueRight;

            // assert
            testResult.Should().BeTrue(LeftLessThanRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void LessThanOperator_MoneyValueEqualToOperand_ReturnsFalse()
        {
            // arrange
            const decimal testAmount = 45;
            Money testValueLeft = Money.Create(testAmount), testValueRight = Money.Create(testAmount);

            // act
            var testResult = testValueLeft < testValueRight;

            // assert
            testResult.Should().BeFalse(LeftEqualToRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void LessThanOperator_MoneyValueGreaterThanOperand_ReturnsFalse()
        {
            // arrange
            Money testValueLeft = Money.Create(36), testValueRight = Money.Create(14);

            // act
            var testResult = testValueLeft < testValueRight;

            // assert
            testResult.Should().BeFalse(LeftGreaterThanRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void LessThanOrEqualToOperator_OperandCurrenciesDifferent_ThrowsInvalidOperationException()
        {
            // arrange
            Money testValueLeft = Money.Create(235, Iso4217CurrencyCurrent.USD), testValueRight = Money.Create(165, Iso4217CurrencyCurrent.GBP);

            // act
            Action testAction = () => _ = testValueLeft <= testValueRight;

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void LessThanOrEqualToOperator_MoneyValueLessThanOperand_ReturnsTrue()
        {
            // arrange
            Money testValueLeft = Money.Create(134), testValueRight = Money.Create(7226);

            // act
            var testResult = testValueLeft <= testValueRight;

            // assert
            testResult.Should().BeTrue(LeftLessThanRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void LessThanOrEqualToOperator_MoneyValueEqualToOperand_ReturnsTrue()
        {
            // arrange
            const decimal testAmount = 142;
            Money testValueLeft = Money.Create(testAmount), testValueRight = Money.Create(testAmount);

            // act
            var testResult = testValueLeft <= testValueRight;

            // assert
            testResult.Should().BeTrue(LeftEqualToRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void LessThanOrEqualToOperator_MoneyValueGreaterThanOperand_ReturnsFalse()
        {
            // arrange
            Money testValueLeft = Money.Create(721), testValueRight = Money.Create(124);

            // act
            var testResult = testValueLeft <= testValueRight;

            // assert
            testResult.Should().BeFalse(LeftGreaterThanRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void GreaterThanOperator_OperandCurrenciesDifferent_ThrowsInvalidOperationException()
        {
            // arrange
            Money testValueLeft = Money.Create(1356, Iso4217CurrencyCurrent.USD), testValueRight = Money.Create(176, Iso4217CurrencyCurrent.GBP);

            // act
            Action testAction = () => _ = testValueLeft > testValueRight;

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GreaterThanOperator_MoneyValueLessThanOperand_ReturnsFalse()
        {
            // arrange
            Money testValueLeft = Money.Create(52), testValueRight = Money.Create(924);

            // act
            var testResult = testValueLeft > testValueRight;

            // assert
            testResult.Should().BeFalse(LeftLessThanRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void GreaterThanOperator_MoneyValueEqualToOperand_ReturnsFalse()
        {
            // arrange
            const decimal testAmount = 62;
            Money testValueLeft = Money.Create(testAmount), testValueRight = Money.Create(testAmount);

            // act
            var testResult = testValueLeft > testValueRight;

            // assert
            testResult.Should().BeFalse(LeftEqualToRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void GreaterThanOperator_MoneyValueGreaterThanOperand_ReturnsTrue()
        {
            // arrange
            Money testValueLeft = Money.Create(924), testValueRight = Money.Create(13);

            // act
            var testResult = testValueLeft > testValueRight;

            // assert
            testResult.Should().BeTrue(LeftGreaterThanRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void GreaterThanOrEqualToOperator_OperandCurrenciesDifferent_ThrowsInvalidOperationException()
        {
            // arrange
            Money testValueLeft = Money.Create(14, Iso4217CurrencyCurrent.USD), testValueRight = Money.Create(9124, Iso4217CurrencyCurrent.GBP);

            // act
            Action testAction = () => _ = testValueLeft >= testValueRight;

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GreaterThanOrEqualToOperator_MoneyValueLessThanOperand_ReturnsFalse()
        {
            // arrange
            Money testValueLeft = Money.Create(123), testValueRight = Money.Create(139);

            // act
            var testResult = testValueLeft >= testValueRight;

            // assert
            testResult.Should().BeFalse(LeftLessThanRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void GreaterThanOrEqualToOperator_MoneyValueEqualToOperand_ReturnsTrue()
        {
            // arrange
            const decimal testAmount = 198;
            Money testValueLeft = Money.Create(testAmount), testValueRight = Money.Create(testAmount);

            // act
            var testResult = testValueLeft >= testValueRight;

            // assert
            testResult.Should().BeTrue(LeftEqualToRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void GreaterThanOrEqualToOperator_MoneyValueGreaterThanOperand_ReturnsTrue()
        {
            // arrange
            Money testValueLeft = Money.Create(12789), testValueRight = Money.Create(1454);

            // act
            var testResult = testValueLeft >= testValueRight;

            // assert
            testResult.Should().BeTrue(LeftGreaterThanRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void EqualsOperator_OperandsAreEqual_ReturnsTrue()
        {
            // arrange
            const decimal testAmount = 2734;
            Money testValueLeft = Money.Create(testAmount), testValueRight = Money.Create(testAmount);

            // act
            var testResult = testValueLeft == testValueRight;

            // assert
            testResult.Should().BeTrue(LeftEqualToRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void EqualsOperator_OperandsNotEqual_ReturnsFalse()
        {
            // arrange
            Money testValueLeft = Money.Create(1235), testValueRight = Money.Create(856);

            // act
            var testResult = testValueLeft == testValueRight;

            // assert
            testResult.Should().BeFalse(LeftNotEqualToRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void NotEqualsOperator_OperandsAreEqual_ReturnsFalse()
        {
            // arrange
            const decimal testAmount = 1223;
            Money testValueLeft = Money.Create(testAmount), testValueRight = Money.Create(testAmount);

            // act
            var testResult = testValueLeft != testValueRight;

            // assert
            testResult.Should().BeFalse(LeftEqualToRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void NotEqualsOperator_OperandsNotEqual_ReturnsTrue()
        {
            // arrange
            Money testValueLeft = Money.Create(92), testValueRight = Money.Create(3);

            // act
            var testResult = testValueLeft != testValueRight;

            // assert
            testResult.Should().BeTrue(LeftNotEqualToRightReason, (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void CurrencyMinorUnits_MoneyValueWithCurrency_ReturnsMinorUnitsOfCurrency()
        {
            // arrange
            const Iso4217CurrencyCurrent currency = Iso4217CurrencyCurrent.USD;
            var testValue = Money.Create(10, currency);

            // act
            var testResult = testValue.CurrencyMinorUnits;

            // assert
            testResult.Should().Be(UsDollarMinorUnits, "currency {0} has {1} minor units", currency, UsDollarMinorUnits);
        }

        [Fact]
        public void CurrencyMinorUnits_MoneyValueNoCurrency_ReturnsNull()
        {
            // arrange
            const Iso4217CurrencyCurrent currency = Iso4217CurrencyCurrent.XTS;
            var testValue = Money.Create(20, currency);

            // act
            var testResult = testValue.CurrencyMinorUnits;

            // assert
            testResult.Should().BeNull("currency {0} has no minor units", currency);
        }

        [Fact]
        public void IsoCurrencyCode_MoneyValueWithCurrency_ReturnsCurrencyCode()
        {
            // arrange
            const Iso4217CurrencyCurrent currency = Iso4217CurrencyCurrent.USD;
            var testValue = Money.Create(30, currency);

            // act
            var testResult = testValue.IsoCurrencyCode;

            // assert
            testResult.Should().Be(currency, "{0} is the currency of the money value", currency);
        }

        [Fact]
        public void IsoCurrencyCode_MoneyValueNoCurrency_ReturnsNoCurrencyCode()
        {
            // arrange
            var testValue = Money.Create(40);

            // act
            var testResult = testValue.IsoCurrencyCode;

            // assert
            testResult.Should().Be(Iso4217CurrencyCurrent.XXX, "the money value has no currency");
        }

        [Fact]
        public void RoundingMethod_MoneyValueWithRounding_ReturnsRoundingMethod()
        {
            // arrange
            const int roundingPrecision = 2;
            const MidpointRounding roundingMethod = MidpointRounding.ToEven;
            var testValue = Money.Create(50, Iso4217CurrencyCurrent.USD, roundingPrecision, roundingMethod);

            // act
            var testResult = testValue.RoundingMethod;

            // assert
            testResult.Should().Be(roundingMethod, "the money value uses the {0} rounding method", roundingMethod);
        }

        [Fact]
        public void RoundingMethod_MoneyValueNoRounding_ReturnsNull()
        {
            // arrange
            var testValue = Money.Create(60, Iso4217CurrencyCurrent.USD);

            // act
            var testResult = testValue.RoundingMethod;

            // assert
            testResult.Should().BeNull(NoRoundingReason);
        }

        [Fact]
        public void RoundingPrecision_MoneyValueWithRounding_ReturnsRoundingPrecision()
        {
            // arrange
            const int roundingPrecision = 4;
            var testValue = Money.Create(70, Iso4217CurrencyCurrent.USD, roundingPrecision);

            // act
            var testResult = testValue.RoundingPrecision;

            // assert
            testResult.Should().Be(roundingPrecision, "{0} is the rounding precision of the money value", roundingPrecision);
        }

        [Fact]
        public void RoundingPrecision_MoneyValueNoRounding_ReturnsNull()
        {
            // arrange
            var testValue = Money.Create(80, Iso4217CurrencyCurrent.USD);

            // act
            var testResult = testValue.RoundingPrecision;

            // assert
            testResult.Should().BeNull(NoRoundingReason);
        }

        [Fact]
        public void CompareTo_ArgumentGreaterThanMoneyValue_ReturnsNegativeOne()
        {
            // arrange
            var testValue = Money.Create(24);

            // act
            var testResult = testValue.CompareTo(234);

            // assert
            testResult.Should().Be(-1, "the argument is greater than the money value");
        }

        [Fact]
        public void CompareTo_ArgumentEqualToMoneyValue_ReturnsZero()
        {
            // arrange
            const decimal testAmount = 46;
            var testValue = Money.Create(testAmount);

            // act
            var testResult = testValue.CompareTo(testAmount);

            // assert
            testResult.Should().Be(0, "the argument is equal to the money value");
        }

        [Fact]
        public void CompareTo_ArgumentLessThanMoneyValue_ReturnsOne()
        {
            // arrange
            var testValue = Money.Create(8245);

            // act
            var testResult = testValue.CompareTo(3245);

            // assert
            testResult.Should().Be(1, "the argument is less than the money value");
        }

        [Fact]
        public void Equals_ArgumentEqualToMoneyValue_ReturnsTrue()
        {
            // arrange
            const decimal testAmount = 23;
            var testValue = Money.Create(testAmount);

            // act
            var testResult = testValue.Equals(testAmount);

            // assert
            testResult.Should().BeTrue("the argument is equal to the money value");
        }

        [Fact]
        public void Equals_ArgumentNotEqualToMoneyValue_ReturnsFalse()
        {
            // arrange
            var testValue = Money.Create(36);

            // act
            var testResult = testValue.Equals(634);

            // assert
            testResult.Should().BeFalse("the argument is not equal to the money value");
        }

        [Fact]
        public void Parse_InputIsNull_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => _ = Money.Parse(null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>(InputIsNullReason);
        }

        [Theory]
        [MemberData(nameof(NegativeAmountTestCases))]
        public void Parse_InputIsNegativeAmount_ReturnsMoneyValue(string testInput, decimal expectedResult)
        {
            // arrange
            using var cultureScope = new CurrentCultureScope(CultureInfo.InvariantCulture);

            // act
            var testResult = Money.Parse(testInput);

            // assert
            ((decimal)testResult).Should().Be(expectedResult, MoneyValueContainsAmountReason, expectedResult);
        }

        [Theory]
        [MemberData(nameof(PositiveAmountTestCases))]
        public void Parse_InputIsPositiveAmount_ReturnsMoneyValue(string testInput, decimal expectedResult)
        {
            // arrange
            using var cultureScope = new CurrentCultureScope(CultureInfo.InvariantCulture);

            // act
            var testResult = Money.Parse(testInput);

            // assert
            ((decimal)testResult).Should().Be(expectedResult, MoneyValueContainsAmountReason, expectedResult);
        }

        [Fact]
        public void Parse_ProviderSetAndInputIsNull_ThrowsArgumentNullException()
        {
            // arrange
            IFormatProvider provider = CultureInfo.InvariantCulture;

            // act
            Action testAction = () => _ = Money.Parse(null!, provider);

            // assert
            testAction.Should().Throw<ArgumentNullException>(InputIsNullReason);
        }

        [Theory]
        [MemberData(nameof(NegativeAmountTestCases))]
        public void Parse_ProviderSetAndInputIsNegativeAmount_ReturnsMoneyValue(string testInput, decimal expectedResult)
        {
            // arrange
            IFormatProvider provider = CultureInfo.InvariantCulture;

            // act
            var testResult = Money.Parse(testInput, provider);

            // assert
            ((decimal)testResult).Should().Be(expectedResult, MoneyValueContainsAmountReason, expectedResult);
        }

        [Theory]
        [MemberData(nameof(PositiveAmountTestCases))]
        public void Parse_ProviderSetAndInputIsPositiveAmount_ReturnsMoneyValue(string testInput, decimal expectedResult)
        {
            // arrange
            IFormatProvider provider = CultureInfo.InvariantCulture;

            // act
            var testResult = Money.Parse(testInput, provider);

            // assert
            ((decimal)testResult).Should().Be(expectedResult, MoneyValueContainsAmountReason, expectedResult);
        }

        [Theory]
        [InlineData(null), InlineData("")]
        public void TryParse_InputIsBlank_ReturnsFalse(string? testValue)
        {
            // arrange
            using var cultureScope = new CurrentCultureScope(CultureInfo.InvariantCulture);

            // act
            var testResult = Money.TryParse(testValue, out _);

            // assert
            testResult.Should().BeFalse(InputIsBlankReason);
        }

        [Theory]
        [MemberData(nameof(NegativeAmountTestCases))]
        public void TryParse_InputIsNegativeAmount_ReturnsTrueAndOutputsMoneyValue(string testValue, decimal expectedResult)
        {
            // arrange
            using var cultureScope = new CurrentCultureScope(CultureInfo.InvariantCulture);

            // act
            var testResult = Money.TryParse(testValue, out var result);

            // assert
            testResult.Should().BeTrue(InputIsValidReason);
            ((decimal)result).Should().Be(expectedResult, InputContainsMoneyValueReason, expectedResult);
        }

        [Theory]
        [MemberData(nameof(PositiveAmountTestCases))]
        public void TryParse_InputIsPositiveAmount_ReturnsTrueAndOutputsMoneyValue(string testValue, decimal expectedResult)
        {
            // arrange
            using var cultureScope = new CurrentCultureScope(CultureInfo.InvariantCulture);

            // act
            var testResult = Money.TryParse(testValue, out var result);

            // assert
            testResult.Should().BeTrue(InputIsValidReason);
            ((decimal)result).Should().Be(expectedResult, InputContainsMoneyValueReason, expectedResult);
        }

        [Theory]
        [InlineData(null), InlineData("")]
        public void TryParse_ProviderSetAndInputIsBlank_ReturnsFalse(string? testValue)
        {
            // arrange
            IFormatProvider provider = CultureInfo.InvariantCulture;

            // act
            var testResult = Money.TryParse(testValue, provider, out _);

            // assert
            testResult.Should().BeFalse(InputIsBlankReason);
        }

        [Theory]
        [MemberData(nameof(NegativeAmountTestCases))]
        public void TryParse_ProviderSetAndInputIsNegativeAmount_ReturnsTrueAndOutputsMoneyValue(string testValue, decimal expectedResult)
        {
            // arrange
            IFormatProvider provider = CultureInfo.InvariantCulture;

            // act
            var testResult = Money.TryParse(testValue, provider, out var result);

            // assert
            testResult.Should().BeTrue(InputIsValidReason);
            ((decimal)result).Should().Be(expectedResult, InputContainsMoneyValueReason, expectedResult);
        }

        [Theory]
        [MemberData(nameof(PositiveAmountTestCases))]
        public void TryParse_ProviderSetAndInputIsPositiveAmount_ReturnsTrueAndOutputsMoneyValue(string testValue, decimal expectedResult)
        {
            // arrange
            IFormatProvider provider = CultureInfo.InvariantCulture;

            // act
            var testResult = Money.TryParse(testValue, provider, out var result);

            // assert
            testResult.Should().BeTrue(InputIsValidReason);
            ((decimal)result).Should().Be(expectedResult, InputContainsMoneyValueReason, expectedResult);
        }

        // Each placement of the currency code and of the sign or parentheses that a negative amount may take.
        public static TheoryData<string, decimal> NegativeAmountTestCases => new()
        {
            { "(USD12.34)", -12.34m },
            { "-USD23.45", -23.45m },
            { "USD-34.56", -34.56m },
            { "USD45.67-", -45.67m },
            { "(56.78USD)", -56.78m },
            { "-67.89USD", -67.89m },
            { "78.90-USD", -78.90m },
            { "89.01USD-", -89.01m },
            { "-90.12 USD", -90.12m },
            { "-USD 1.23", -1.23m },
            { "23.45 USD-", -23.45m },
            { "USD 34.56-", -34.56m },
            { "USD -45.67", -45.67m },
            { "56.78- USD", -56.78m },
            { "(USD 67.89)", -67.89m },
            { "(78.90 USD)", -78.90m },
            { "(USD12)", -12m },
            { "-USD23", -23m },
            { "USD-34", -34m },
            { "USD45-", -45m },
            { "(56USD)", -56m },
            { "-67USD", -67m },
            { "78-USD", -78m },
            { "89USD-", -89m },
            { "-90 USD", -90m },
            { "-USD 01", -1m },
            { "12 USD-", -12m },
            { "USD 23-", -23m },
            { "USD -34", -34m },
            { "45- USD", -45m },
            { "(USD 56)", -56m },
            { "(67 USD)", -67m },
        };

        // Each placement of the currency code that a positive amount may take.
        public static TheoryData<string, decimal> PositiveAmountTestCases => new()
        {
            { "USD12.34", 12.34m },
            { "23.45USD", 23.45m },
            { "USD 34.56", 34.56m },
            { "45.67 USD", 45.67m },
            { "USD78", 78m },
            { "89USD", 89m },
            { "USD 90", 90m },
            { "01 USD", 1m },
        };

        #region Test: CompareTo_ObjectArgument

        [Theory]
        [MemberData(nameof(CompareToObjectArgumentTestCases))]
        public void CompareTo_ObjectArgumentIsMoneyValue_ReturnsComparisonResult(decimal testAmount, int expectedResult)
        {
            // arrange
            var testSubject = Money.Create(ObjectTestAmount, Iso4217CurrencyCurrent.NZD);
            object testValue = Money.Create(testAmount, Iso4217CurrencyCurrent.NZD);

            // act
            var testResult = testSubject.CompareTo(testValue);

            // assert
            testResult.Should().Be(expectedResult, "the argument has the amount {0}", testAmount);
        }

        public static TheoryData<decimal, int> CompareToObjectArgumentTestCases => new()
        {
            { ObjectTestAmount, 0 },  // the values are equal
            { 12m, 1 },               // the argument is less than this instance
            { 12345m, -1 },           // the argument is greater than this instance
        };

        [Fact]
        public void CompareTo_ObjectArgumentIsNull_ReturnsPositive()
        {
            // arrange
            var testSubject = Money.Create(ObjectTestAmount, Iso4217CurrencyCurrent.NZD);

            // act
            var testResult = testSubject.CompareTo(null);

            // assert
            testResult.Should().BePositive("any instance compares greater than null");
        }

        [Fact]
        public void CompareTo_ObjectArgumentIsDifferentType_ThrowsArgumentException()
        {
            // arrange
            var testSubject = Money.Create(ObjectTestAmount, Iso4217CurrencyCurrent.NZD);

            // act
            Action testAction = () => testSubject.CompareTo(NotAMoneyValue);

            // assert
            testAction.Should().Throw<ArgumentException>(ArgumentIsNotMoneyValueReason);
        }

        #endregion

        #region Test: Equals_ObjectArgument

        [Fact]
        public void Equals_ObjectArgumentIsEquivalentMoneyValue_ReturnsTrue()
        {
            // arrange
            var testSubject = Money.Create(ObjectTestAmount, Iso4217CurrencyCurrent.NZD);
            object testValue = Money.Create(ObjectTestAmount, Iso4217CurrencyCurrent.NZD);

            // act
            var testResult = testSubject.Equals(testValue);

            // assert
            testResult.Should().BeTrue("the values have the same amount and currency");
        }

        [Theory]
        [MemberData(nameof(EqualsDifferentMoneyValueTestCases))]
        public void Equals_ObjectArgumentIsDifferentMoneyValue_ReturnsFalse(decimal testAmount, Iso4217CurrencyCurrent testCurrency)
        {
            // arrange
            var testSubject = Money.Create(ObjectTestAmount, Iso4217CurrencyCurrent.NZD);
            object testValue = Money.Create(testAmount, testCurrency);

            // act
            var testResult = testSubject.Equals(testValue);

            // assert
            testResult.Should().BeFalse("the values differ in amount or currency");
        }

        public static TheoryData<decimal, Iso4217CurrencyCurrent> EqualsDifferentMoneyValueTestCases => new()
        {
            { ObjectTestAmount, Iso4217CurrencyCurrent.AUD },  // the same amount in a different currency
            { 4321m, Iso4217CurrencyCurrent.NZD },             // a different amount in the same currency
        };

        [Fact]
        public void Equals_ObjectArgumentIsNotMoneyValue_ReturnsFalse()
        {
            // arrange
            var testSubject = Money.Create(ObjectTestAmount, Iso4217CurrencyCurrent.NZD);

            // act
            var testResult = testSubject.Equals(NotAMoneyValue);

            // assert
            testResult.Should().BeFalse(ArgumentIsNotMoneyValueReason);
        }

        #endregion
    }
}
