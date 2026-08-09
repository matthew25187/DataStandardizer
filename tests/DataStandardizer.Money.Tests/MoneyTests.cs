using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Money.Tests
{
    public class MoneyTests
    {
        [Fact]
        public void AdditionOperator_MoneyValueWithAmount_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1234, testAdditionOperand = 13;
            var testValue = Money.Create(testAmount);

            var expectedResult = testAmount + testAdditionOperand;

            // act
            var testResult = testValue + testAdditionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the addition operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.XXX, "the currency of the result is not set");
            testResult.RoundingPrecision.Should().BeNull("the rounding precision of the result is not set");
            testResult.RoundingMethod.Should().BeNull("the rounding method of the result is not set");
        }

        [Fact]
        public void AdditionOperator_MoneyValueWithAmountAndCurrency_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1357, testAdditionOperand = 24;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            var testValue = Money.Create(testAmount, testCurrency);

            var expectedResult = testAmount + testAdditionOperand;

            // act
            var testResult = testValue + testAdditionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the addition operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, "the currency of the result is {0}", testCurrency);
            testResult.RoundingPrecision.Should().BeNull("the rounding precision of the result is not set");
            testResult.RoundingMethod.Should().BeNull("the rounding method of the result is not set");
        }

        [Fact]
        public void AdditionOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecision_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 2468, testAdditionOperand = 57;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 4;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision);

            var expectedResult = testAmount + testAdditionOperand;

            // act
            var testResult = testValue + testAdditionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the addition operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, "the currency of the result is {0}", testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, "the rounding precision of the result is {0}", testRoundingPrecision);
            testResult.RoundingMethod.Should().BeNull("the rounding method of the result is not set");
        }

        [Fact]
        public void AdditionOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecisionAndRoundingMethod_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 3579, testAdditionOperand = 68;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 3;
            const MidpointRounding testRoundingMethod = MidpointRounding.AwayFromZero;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision, testRoundingMethod);

            var expectedResult = testAmount + testAdditionOperand;

            // act
            var testResult = testValue + testAdditionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the addition operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, "the currency of the result is {0}", testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, "the rounding precision of the result is {0}", testRoundingPrecision);
            testResult.RoundingMethod.Should().Be(testRoundingMethod, "the rounding method of the result is {0}", testRoundingMethod);
        }

        [Fact]
        public void SubtractionOperator_MoneyValueWithAmount_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1234, testSubtractionOperand = 12;
            var testValue = Money.Create(testAmount);

            var expectedResult = testAmount - testSubtractionOperand;

            // act
            var testResult = testValue - testSubtractionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the subtraction operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.XXX, "the currency of the result is not set");
            testResult.RoundingPrecision.Should().BeNull("the rounding precision of the result is not set");
            testResult.RoundingMethod.Should().BeNull("the rounding method of the result is not set");
        }

        [Fact]
        public void SubtractionOperator_MoneyValueWithAmountAndCurrency_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 5678, testSubtractionOperand = 34;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            var testValue = Money.Create(testAmount, testCurrency);

            var expectedResult = testAmount - testSubtractionOperand;

            // act
            var testResult = testValue - testSubtractionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the subtraction operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, "the currency of the result is {0}", testCurrency);
            testResult.RoundingPrecision.Should().BeNull("the rounding precision of the result is not set");
            testResult.RoundingMethod.Should().BeNull("the rounding method of the result is not set");
        }

        [Fact]
        public void SubtractionOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecision_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1357, testSubtractionOperand = 56;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 4;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision);

            var expectedResult = testAmount - testSubtractionOperand;

            // act
            var testResult = testValue - testSubtractionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the subtraction operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, "the currency of the result is {0}", testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, "the rounding precision of the result is {0}", testRoundingPrecision);
            testResult.RoundingMethod.Should().BeNull("the rounding method of the result is not set");
        }

        [Fact]
        public void SubtractionOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecisionAndRoundingMethod_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 2468, testSubtractionOperand = 78;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 3;
            const MidpointRounding testRoundingMethod = MidpointRounding.ToEven;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision, testRoundingMethod);

            var expectedResult = testAmount - testSubtractionOperand;

            // act
            var testResult = testValue - testSubtractionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the subtraction operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, "the currency of the result is {0}", testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, "the rounding precision of the result is {0}", testRoundingPrecision);
            testResult.RoundingMethod.Should().Be(testRoundingMethod, "the rounding method of the result is {0}", testRoundingMethod);
        }

        [Fact]
        public void MultiplicationOperator_MoneyValueWithAmount_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1234, testMultiplicationOperand = 12;
            var testValue = Money.Create(testAmount);

            var expectedResult = testAmount * testMultiplicationOperand;

            // act
            var testResult = testValue * testMultiplicationOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the multiplication operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.XXX, "the currency of the result is not set");
            testResult.RoundingPrecision.Should().BeNull("the rounding precision of the result is not set");
            testResult.RoundingMethod.Should().BeNull("the rounding method of the result is not set");
        }

        [Fact]
        public void MultiplicationOperator_MoneyValueWithAmountAndCurrency_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 5678, testMultiplicationOperand = 34;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            var testValue = Money.Create(testAmount, testCurrency);

            var expectedResult = testAmount * testMultiplicationOperand;

            // act
            var testResult = testValue * testMultiplicationOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the multiplication operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, "the currency of the result is {0}", testCurrency);
            testResult.RoundingPrecision.Should().BeNull("the rounding precision of the result is not set");
            testResult.RoundingMethod.Should().BeNull("the rounding method of the result is not set");
        }

        [Fact]
        public void MultiplicationOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecision_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1357, testMultiplicationOperand = 56;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 4;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision);

            var expectedResult = testAmount * testMultiplicationOperand;

            // act
            var testResult = testValue * testMultiplicationOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the multiplication operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, "the currency of the result is {0}", testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, "the rounding precision of the result is {0}", testRoundingPrecision);
            testResult.RoundingMethod.Should().BeNull("the rounding method of the result is not set");
        }

        [Fact]
        public void MultiplicationOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecisionAndRoundingMethod_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 2468, testMultiplicationOperand = 78;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 3;
            const MidpointRounding testRoundingMethod = MidpointRounding.AwayFromZero;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision, testRoundingMethod);

            var expectedResult = testAmount * testMultiplicationOperand;

            // act
            var testResult = testValue * testMultiplicationOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the multiplication operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, "the currency of the result is {0}", testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, "the rounding precision of the result is {0}", testRoundingPrecision);
            testResult.RoundingMethod.Should().Be(testRoundingMethod, "the rounding method of the result is {0}", testRoundingMethod);
        }

        [Fact]
        public void DivisionOperator_MoneyValueWithAmount_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 2677, testDivisionOperand = 63;
            var testValue = Money.Create(testAmount);

            var expectedResult = testAmount / testDivisionOperand;

            // act
            var testResult = testValue / testDivisionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the division operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.XXX, "the currency of the result is not set");
            testResult.RoundingPrecision.Should().BeNull("the rounding precision of the result is not set");
            testResult.RoundingMethod.Should().BeNull("the rounding method of the result is not set");
        }

        [Fact]
        public void DivisionOperator_MoneyValueWithAmountAndCurrency_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 1553, testDivisionOperand = 25;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            var testValue = Money.Create(testAmount, testCurrency);

            var expectedResult = testAmount / testDivisionOperand;

            // act
            var testResult = testValue / testDivisionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the division operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, "the currency of the result is {0}", testCurrency);
            testResult.RoundingPrecision.Should().BeNull("the rounding precision of the result is not set");
            testResult.RoundingMethod.Should().BeNull("the rounding method of the result is not set");
        }

        [Fact]
        public void DivisionOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecision_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 2542, testDivisionOperand = 25;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 4;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision);

            var expectedResult = testAmount / testDivisionOperand;

            // act
            var testResult = testValue / testDivisionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the division operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, "the currency of the result is {0}", testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, "the rounding precision of the result is {0}", testRoundingPrecision);
            testResult.RoundingMethod.Should().BeNull("the rounding method of the result is not set");
        }

        [Fact]
        public void DivisionOperator_MoneyValueWithAmountAndCurrencyAndRoundingPrecisionAndRoundingMethod_ReturnsMoneyValueResult()
        {
            // arrange
            const decimal testAmount = 8127, testDivisionOperand = 36;
            const Iso4217CurrencyCurrent testCurrency = Iso4217CurrencyCurrent.USD;
            const int testRoundingPrecision = 3;
            const MidpointRounding testRoundingMethod = MidpointRounding.AwayFromZero;
            var testValue = Money.Create(testAmount, testCurrency, testRoundingPrecision, testRoundingMethod);

            var expectedResult = testAmount / testDivisionOperand;

            // act
            var testResult = testValue / testDivisionOperand;

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the result of the division operation is {0}", expectedResult);
            testResult.IsoCurrencyCode.Should().Be(testCurrency, "the currency of the result is {0}", testCurrency);
            testResult.RoundingPrecision.Should().Be(testRoundingPrecision, "the rounding precision of the result is {0}", testRoundingPrecision);
            testResult.RoundingMethod.Should().Be(testRoundingMethod, "the rounding method of the result is {0}", testRoundingMethod);
        }

        [Fact]
        public void LessThanOperator_OperandCurrenciesDifferent_ThrowsInvalidOperationException()
        {
            // arrange
            Money testValue1 = Money.Create(1535m, Iso4217CurrencyCurrent.USD), testValue2 = Money.Create(8127m, Iso4217CurrencyCurrent.GBP);

            // act
            Func<bool> testAction = () => testValue1 < testValue2;

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
            testResult.Should().BeTrue("the left operand {0} is less than the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void LessThanOperator_MoneyValueEqualToOperand_ReturnsFalse()
        {
            // arrange
            Money testValueLeft = Money.Create(45), testValueRight = Money.Create(45);

            // act
            var testResult = testValueLeft < testValueRight;

            // assert
            testResult.Should().BeFalse("the left operand {0} is equal to the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void LessThanOperator_MoneyValueGreaterThanOperand_ReturnsFalse()
        {
            // arrange
            Money testValueLeft = Money.Create(36), testValueRight = Money.Create(14);

            // act
            var testResult = testValueLeft < testValueRight;

            // assert
            testResult.Should().BeFalse("the left operand {0} is greater than the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void LessThanOrEqualToOperator_OperandCurrenciesDifferent_ThrowsInvalidOperationException()
        {
            // arrange
            Money testValueLeft = Money.Create(235, Iso4217CurrencyCurrent.USD), testValueRight = Money.Create(165, Iso4217CurrencyCurrent.GBP);

            // act
            Func<bool> testAction = () => testValueLeft <= testValueRight;

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
            testResult.Should().BeTrue("the left operand {0} is less than the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void LessThanOrEqualToOperator_MoneyValueEqualToOperand_ReturnsTrue()
        {
            // arrange
            Money testValueLeft = Money.Create(142), testValueRight = Money.Create(142);

            // act
            var testResult = testValueLeft <= testValueRight;

            // assert
            testResult.Should().BeTrue("the left operand {0} is equal to the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void LessThanOrEqualToOperator_MoneyValueGreaterThanOperand_ReturnsFalse()
        {
            // arrange
            Money testValueLeft = Money.Create(721), testValueRight = Money.Create(124);

            // act
            var testResult = testValueLeft <= testValueRight;

            // assert
            testResult.Should().BeFalse("the left operand {0} is greater than the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void GreaterThanOperator_OperandCurrenciesDifferent_ThrowsInvalidOperationException()
        {
            // arrange
            Money testValueLeft = Money.Create(1356, Iso4217CurrencyCurrent.USD), testValueRight = Money.Create(176, Iso4217CurrencyCurrent.GBP);

            // act
            Func<bool> testAction = () => testValueLeft > testValueRight;

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
            testResult.Should().BeFalse("the left operand {0} is less than the right operand {0}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void GreaterThanOperator_MoneyValueEqualToOperand_ReturnsFalse()
        {
            // arrange
            Money testValueLeft = Money.Create(62), testValueRight = Money.Create(62);

            // act
            var testResult = testValueLeft > testValueRight;

            // assert
            testResult.Should().BeFalse("the left operand {0} is equal to the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void GreaterThanOperator_MoneyValueGreaterThanOperand_ReturnsTrue()
        {
            // arrange
            Money testValueLeft = Money.Create(924), testValueRight = Money.Create(13);

            // act
            var testResult = testValueLeft > testValueRight;

            // assert
            testResult.Should().BeTrue("the left operand {0} is greater than the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void GreaterThanOrEqualToOperator_OperandCurrenciesDifferent_ThrowsInvalidOperationException()
        {
            // arrange
            Money testValueLeft = Money.Create(14, Iso4217CurrencyCurrent.USD), testValueRight = Money.Create(9124, Iso4217CurrencyCurrent.GBP);

            // act
            Func<bool> testAction = () => testValueLeft >= testValueRight;

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
            testResult.Should().BeFalse("the left operand {0} is less than the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void GreaterThanOrEqualToOperator_MoneyValueEqualToOperand_ReturnsTrue()
        {
            // arrange
            Money testValueLeft = Money.Create(198), testValueRight = Money.Create(198);

            // act
            var testResult = testValueLeft >= testValueRight;

            // assert
            testResult.Should().BeTrue("the left operand {0} is equal to the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void GreaterThanOrEqualToOperator_MoneyValueGreaterThanOperand_ReturnsTrue()
        {
            // arrange
            Money testValueLeft = Money.Create(12789), testValueRight = Money.Create(1454);

            // act
            var testResult = testValueLeft >= testValueRight;

            // assert
            testResult.Should().BeTrue("the left operand {0} is greater than the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void EqualsOperator_OperandsAreEqual_ReturnsTrue()
        {
            // arrange
            Money testValueLeft = Money.Create(2734), testValueRight = Money.Create(2734);

            // act
            var testResult = testValueLeft == testValueRight;

            // assert
            testResult.Should().BeTrue("the lft operand {0} is equal to the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void EqualsOperator_OperandsNotEqual_ReturnsFalse()
        {
            // arrange
            Money testValueLeft = Money.Create(1235), testValueRight = Money.Create(856);

            // act
            var testResult = testValueLeft == testValueRight;

            // assert
            testResult.Should().BeFalse("the left operand {1} is not equal to the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void NotEqualsOperator_OperandsAreEqual_ReturnsFalse()
        {
            // arrange
            Money testValueLeft = Money.Create(1223), testValueRight = Money.Create(1223);

            // act
            var testResult = testValueLeft != testValueRight;

            // assert
            testResult.Should().BeFalse("the left operand {0} is not equal to the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
        }

        [Fact]
        public void NotEqualsOperator_OperandsNotEqual_ReturnsTrue()
        {
            // arrange
            Money testValueLeft = Money.Create(092), testValueRight = Money.Create(3);

            // act
            var testResult = testValueLeft != testValueRight;

            // assert
            testResult.Should().BeTrue("the left operand {0} is not equal to the right operand {1}", (decimal)testValueLeft, (decimal)testValueRight);
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
            testResult.Should().Be(2, "currency {0} has 2 minor units", currency);
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
            testResult.Should().BeNull("currency {0} has no minor units");
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
            const MidpointRounding roundingMethod = MidpointRounding.ToEven;
            var testValue = Money.Create(50, Iso4217CurrencyCurrent.USD, 2, roundingMethod);

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
            testResult.Should().BeNull("the money value has no rounding");
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
            testResult.Should().BeNull("the money value has no rounding");
        }

        [Fact]
        public void CompareTo_ArgumentGreaterThanMoneyValue_ReturnsNegativeOne()
        {
            // arrange
            var testValue = Money.Create(24);

            // act
            var testResult = testValue.CompareTo(234);

            // assert
            testResult.Should().Be(-1, "the argument is less than the money value");
        }

        [Fact]
        public void CompareTo_ArgumentEqualToMoneyValue_ReturnsZero()
        {
            // arrange
            var testValue = Money.Create(46);

            // act
            var testResult = testValue.CompareTo(46);

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
            testResult.Should().Be(1, "the argument is greater than the money value");
        }

        [Fact]
        public void Equals_ArgumentEqualToMoneyValue_ReturnsTrue()
        {
            // arrange
            var testValue = Money.Create(23);

            // act
            var testResult = testValue.Equals(23);

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
            testAction.Should().Throw<ArgumentNullException>("the input is null");
        }

        [Theory]
        [InlineData("(USD12.34)", -12.34),
         InlineData("-USD23.45", -23.45),
         InlineData("USD-34.56", -34.56),
         InlineData("USD45.67-", -45.67),
         InlineData("(56.78USD)", -56.78),
         InlineData("-67.89USD", -67.89),
         InlineData("78.90-USD", -78.90),
         InlineData("89.01USD-", -89.01),
         InlineData("-90.12 USD", -90.12),
         InlineData("-USD 1.23", -1.23),
         InlineData("23.45 USD-", -23.45),
         InlineData("USD 34.56-", -34.56),
         InlineData("USD -45.67", -45.67),
         InlineData("56.78- USD", -56.78),
         InlineData("(USD 67.89)", -67.89),
         InlineData("(78.90 USD)", -78.90),
         InlineData("(USD12)", -12),
         InlineData("-USD23", -23),
         InlineData("USD-34", -34),
         InlineData("USD45-", -45),
         InlineData("(56USD)", -56),
         InlineData("-67USD", -67),
         InlineData("78-USD", -78),
         InlineData("89USD-", -89),
         InlineData("-90 USD", -90),
         InlineData("-USD 01", -1),
         InlineData("12 USD-", -12),
         InlineData("USD 23-", -23),
         InlineData("USD -34", -34),
         InlineData("45- USD", -45),
         InlineData("(USD 56)", -56),
         InlineData("(67 USD)", -67)]
        public void Parse_InputIsNegativeAmount_ReturnsMoneyValue(string testInput, decimal expectedResult)
        {
            // act
            var testResult = Money.Parse(testInput);

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the money value contains the amount {0}", expectedResult);
        }

        [Theory]
        [InlineData("USD12.34", 12.34),
         InlineData("23.45USD", 23.45),
         InlineData("USD 34.56", 34.56),
         InlineData("45.67 USD", 45.67),
         InlineData("USD78", 78),
         InlineData("89USD", 89),
         InlineData("USD 90", 90),
         InlineData("01 USD", 1)]
        public void Parse_InputIsPositiveAmount_ReturnsMoneyValue(string testInput, decimal expectedResult)
        {
            // act
            var testResult = Money.Parse(testInput);

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the money value contains the amount {0}", expectedResult);
        }

        [Fact]
        public void Parse_ProviderSetAndInputIsNull_ThrowsArgumentNullException()
        {
            // arrange
            IFormatProvider provider = CultureInfo.CurrentCulture;

            // act
            Action testAction = () => _ = Money.Parse(null!, provider);

            // assert
            testAction.Should().Throw<ArgumentNullException>("the input is null");
        }

        [Theory]
        [InlineData("(USD12.34)", -12.34),
         InlineData("-USD23.45", -23.45),
         InlineData("USD-34.56", -34.56),
         InlineData("USD45.67-", -45.67),
         InlineData("(56.78USD)", -56.78),
         InlineData("-67.89USD", -67.89),
         InlineData("78.90-USD", -78.90),
         InlineData("89.01USD-", -89.01),
         InlineData("-90.12 USD", -90.12),
         InlineData("-USD 1.23", -1.23),
         InlineData("23.45 USD-", -23.45),
         InlineData("USD 34.56-", -34.56),
         InlineData("USD -45.67", -45.67),
         InlineData("56.78- USD", -56.78),
         InlineData("(USD 67.89)", -67.89),
         InlineData("(78.90 USD)", -78.90),
         InlineData("(USD12)", -12),
         InlineData("-USD23", -23),
         InlineData("USD-34", -34),
         InlineData("USD45-", -45),
         InlineData("(56USD)", -56),
         InlineData("-67USD", -67),
         InlineData("78-USD", -78),
         InlineData("89USD-", -89),
         InlineData("-90 USD", -90),
         InlineData("-USD 01", -1),
         InlineData("12 USD-", -12),
         InlineData("USD 23-", -23),
         InlineData("USD -34", -34),
         InlineData("45- USD", -45),
         InlineData("(USD 56)", -56),
         InlineData("(67 USD)", -67)]
        public void Parse_ProviderSetAndInputIsNegativeAmount_ReturnsMoneyValue(string testInput, decimal expectedResult)
        {
            // arrange
            IFormatProvider provider = CultureInfo.CurrentCulture;

            // act
            var testResult = Money.Parse(testInput, provider);

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the money value contains the amount {0}", expectedResult);
        }

        [Theory]
        [InlineData("USD12.34", 12.34),
         InlineData("23.45USD", 23.45),
         InlineData("USD 34.56", 34.56),
         InlineData("45.67 USD", 45.67),
         InlineData("USD78", 78),
         InlineData("89USD", 89),
         InlineData("USD 90", 90),
         InlineData("01 USD", 1)]
        public void Parse_ProviderSetAndInputIsPositiveAmount_ReturnsMoneyValue(string testInput, decimal expectedResult)
        {
            // arrange
            IFormatProvider provider = CultureInfo.CurrentCulture;

            // act
            var testResult = Money.Parse(testInput, provider);

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the money value contains the amount {0}", expectedResult);
        }

        [Theory]
        [InlineData(null), InlineData("")]
        public void TryParse_InputIsBlank_ReturnsFalse(string? testValue)
        {
            // act
            var testResult = Money.TryParse(testValue, out _);

            // assert
            testResult.Should().BeFalse("the input is blank");
        }

        [Theory]
        [InlineData("(USD12.34)", -12.34),
         InlineData("-USD23.45", -23.45),
         InlineData("USD-34.56", -34.56),
         InlineData("USD45.67-", -45.67),
         InlineData("(56.78USD)", -56.78),
         InlineData("-67.89USD", -67.89),
         InlineData("78.90-USD", -78.90),
         InlineData("89.01USD-", -89.01),
         InlineData("-90.12 USD", -90.12),
         InlineData("-USD 1.23", -1.23),
         InlineData("23.45 USD-", -23.45),
         InlineData("USD 34.56-", -34.56),
         InlineData("USD -45.67", -45.67),
         InlineData("56.78- USD", -56.78),
         InlineData("(USD 67.89)", -67.89),
         InlineData("(78.90 USD)", -78.90),
         InlineData("(USD12)", -12),
         InlineData("-USD23", -23),
         InlineData("USD-34", -34),
         InlineData("USD45-", -45),
         InlineData("(56USD)", -56),
         InlineData("-67USD", -67),
         InlineData("78-USD", -78),
         InlineData("89USD-", -89),
         InlineData("-90 USD", -90),
         InlineData("-USD 01", -1),
         InlineData("12 USD-", -12),
         InlineData("USD 23-", -23),
         InlineData("USD -34", -34),
         InlineData("45- USD", -45),
         InlineData("(USD 56)", -56),
         InlineData("(67 USD)", -67)]
        public void TryParse_InputIsNegativeAmount_ReturnsTrueAndOutputsMoneyValue(string testValue, decimal expectedResult)
        {
            // act
            var testResult = Money.TryParse(testValue, out var result);

            // assert
            testResult.Should().BeTrue("the input is a valid money value");
            ((decimal)result).Should().Be(expectedResult, "the input contains the money value {0}", expectedResult);
        }

        [Theory]
        [InlineData("USD12.34", 12.34),
         InlineData("23.45USD", 23.45),
         InlineData("USD 34.56", 34.56),
         InlineData("45.67 USD", 45.67),
         InlineData("USD78", 78),
         InlineData("89USD", 89),
         InlineData("USD 90", 90),
         InlineData("01 USD", 1)]
        public void TryParse_InputIsPositiveAmount_ReturnsTrueAndOutputsMoneyValue(string testValue, decimal expectedResult)
        {
            // act
            var testResult = Money.TryParse(testValue, out var result);

            // assert
            testResult.Should().BeTrue("the input is a valid money value");
            ((decimal)result).Should().Be(expectedResult, "the input contains the money value {0}", expectedResult);
        }

        [Theory]
        [InlineData(null), InlineData("")]
        public void TryParse_ProviderSetAndInputIsBlank_ReturnsFalse(string? testValue)
        {
            // arrange
            IFormatProvider provider = CultureInfo.CurrentCulture;

            // act
            var testResult = Money.TryParse(testValue, provider, out _);

            // assert
            testResult.Should().BeFalse("the input is blank");
        }

        [Theory]
        [InlineData("(USD12.34)", -12.34),
         InlineData("-USD23.45", -23.45),
         InlineData("USD-34.56", -34.56),
         InlineData("USD45.67-", -45.67),
         InlineData("(56.78USD)", -56.78),
         InlineData("-67.89USD", -67.89),
         InlineData("78.90-USD", -78.90),
         InlineData("89.01USD-", -89.01),
         InlineData("-90.12 USD", -90.12),
         InlineData("-USD 1.23", -1.23),
         InlineData("23.45 USD-", -23.45),
         InlineData("USD 34.56-", -34.56),
         InlineData("USD -45.67", -45.67),
         InlineData("56.78- USD", -56.78),
         InlineData("(USD 67.89)", -67.89),
         InlineData("(78.90 USD)", -78.90),
         InlineData("(USD12)", -12),
         InlineData("-USD23", -23),
         InlineData("USD-34", -34),
         InlineData("USD45-", -45),
         InlineData("(56USD)", -56),
         InlineData("-67USD", -67),
         InlineData("78-USD", -78),
         InlineData("89USD-", -89),
         InlineData("-90 USD", -90),
         InlineData("-USD 01", -1),
         InlineData("12 USD-", -12),
         InlineData("USD 23-", -23),
         InlineData("USD -34", -34),
         InlineData("45- USD", -45),
         InlineData("(USD 56)", -56),
         InlineData("(67 USD)", -67)]
        public void TryParse_ProviderSetAndInputIsNegativeAmount_ReturnsTrueAndOutputsMoneyValue(string testValue, decimal expectedResult)
        {
            // arrange
            IFormatProvider provider = CultureInfo.CurrentCulture;

            // act
            var testResult = Money.TryParse(testValue, provider, out var result);

            // assert
            testResult.Should().BeTrue("the input is a valid money value");
            ((decimal)result).Should().Be(expectedResult, "the input contains the money value {0}", expectedResult);
        }

        [Theory]
        [InlineData("USD12.34", 12.34),
         InlineData("23.45USD", 23.45),
         InlineData("USD 34.56", 34.56),
         InlineData("45.67 USD", 45.67),
         InlineData("USD78", 78),
         InlineData("89USD", 89),
         InlineData("USD 90", 90),
         InlineData("01 USD", 1)]
        public void TryParse_ProviderSetAndInputIsPositiveAmount_ReturnsTrueAndOutputsMoneyValue(string testValue, decimal expectedResult)
        {
            // arrange
            IFormatProvider provider = CultureInfo.CurrentCulture;

            // act
            var testResult = Money.TryParse(testValue, provider, out var result);

            // assert
            testResult.Should().BeTrue("the input is a valid money value");
            ((decimal)result).Should().Be(expectedResult, "the input contains the money value {0}", expectedResult);
        }

        #region Test: Parse_ProviderSetAndInputIsNegativeAmount

        [Theory]
        [InlineData("-USD23.45", -23.45)]
        [InlineData("(USD23.45)", -23.45)]
        [InlineData("-90.12 USD", -90.12)]
        [InlineData("USD23.45-", -23.45)]
        public void Parse_ProviderSetAndInputIsNegativeAmount_ReturnsNegativeMoneyValue(string testValue, decimal expectedResult)
        {
            // arrange
            IFormatProvider provider = CultureInfo.CurrentCulture;

            // act
            var testResult = Money.Parse(testValue, provider);

            // assert
            ((decimal)testResult).Should().Be(expectedResult, "the input contains the negative money value {0}", expectedResult);
        }

        [Theory]
        [InlineData("-USD23.45", -23.45)]
        [InlineData("(USD23.45)", -23.45)]
        [InlineData("-90.12 USD", -90.12)]
        public void Parse_ProviderSetAndInputIsNegativeAmount_MatchesParameterlessOverload(string testValue, decimal expectedResult)
        {
            // act
            var withProvider = Money.Parse(testValue, CultureInfo.CurrentCulture);
            var withoutProvider = Money.Parse(testValue);

            // assert
            ((decimal)withProvider).Should().Be(expectedResult, "the input contains the negative money value {0}", expectedResult);
            ((decimal)withProvider).Should().Be((decimal)withoutProvider, "both overloads must agree on the sign of the result");
        }

        #endregion

        #region Test: CompareTo_ObjectArgument

        [Fact]
        public void CompareTo_ObjectArgumentIsMoneyValue_ReturnsComparisonResult()
        {
            // arrange
            var testSubject = Money.Create(1234m, Iso4217CurrencyCurrent.NZD);
            object testValue = Money.Create(1234m, Iso4217CurrencyCurrent.NZD);
            object lesserValue = Money.Create(12m, Iso4217CurrencyCurrent.NZD);
            object greaterValue = Money.Create(12345m, Iso4217CurrencyCurrent.NZD);

            // act & assert
            testSubject.CompareTo(testValue).Should().Be(0, "the values are equal");
            testSubject.CompareTo(lesserValue).Should().BePositive("the argument is less than this instance");
            testSubject.CompareTo(greaterValue).Should().BeNegative("the argument is greater than this instance");
        }

        [Fact]
        public void CompareTo_ObjectArgumentIsNull_ReturnsPositive()
        {
            // arrange
            var testSubject = Money.Create(1234m, Iso4217CurrencyCurrent.NZD);

            // act
            var testResult = testSubject.CompareTo(null);

            // assert
            testResult.Should().BePositive("any instance compares greater than null");
        }

        [Fact]
        public void CompareTo_ObjectArgumentIsDifferentType_ThrowsArgumentException()
        {
            // arrange
            var testSubject = Money.Create(1234m, Iso4217CurrencyCurrent.NZD);

            // act
            var testAction = () => testSubject.CompareTo("not a money value");

            // assert
            testAction.Should().Throw<ArgumentException>("the argument is not a money value");
        }

        #endregion

        #region Test: Equals_ObjectArgument

        [Fact]
        public void Equals_ObjectArgumentIsEquivalentMoneyValue_ReturnsTrue()
        {
            // arrange
            var testSubject = Money.Create(1234m, Iso4217CurrencyCurrent.NZD);
            object testValue = Money.Create(1234m, Iso4217CurrencyCurrent.NZD);

            // act
            var testResult = testSubject.Equals(testValue);

            // assert
            testResult.Should().BeTrue("the values have the same amount and currency");
        }

        [Theory]
        [InlineData(1234, Iso4217CurrencyCurrent.AUD)]
        [InlineData(4321, Iso4217CurrencyCurrent.NZD)]
        public void Equals_ObjectArgumentIsDifferentMoneyValue_ReturnsFalse(decimal testAmount, Iso4217CurrencyCurrent testCurrency)
        {
            // arrange
            var testSubject = Money.Create(1234m, Iso4217CurrencyCurrent.NZD);
            object testValue = Money.Create(testAmount, testCurrency);

            // act
            var testResult = testSubject.Equals(testValue);

            // assert
            testResult.Should().BeFalse("the values differ in amount or currency");
        }

        [Fact]
        public void Equals_ObjectArgumentIsNotMoneyValue_ReturnsFalse()
        {
            // arrange
            var testSubject = Money.Create(1234m, Iso4217CurrencyCurrent.NZD);

            // act
            var testResult = testSubject.Equals("not a money value");

            // assert
            testResult.Should().BeFalse("the argument is not a money value");
        }

        #endregion
    }
}