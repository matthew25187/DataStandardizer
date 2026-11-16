using FluentAssertions;

namespace DataStandardizer.Money.Tests
{
    public class MoneyArithmeticOperatorTests
    {
        private const string DifferentCurrenciesReason = "monetary values in different currencies can't be combined by {0}";
        private const string NotAnAmountOfMoneyReason = "the result of {0} is not an amount of money";
        private const string ResultAmountReason = "the amount of the result of {0} is {1}";
        private const string ResultCurrencyReason = "the result of {0} keeps the currency {1} of the monetary operand";
        private const string ResultRoundingPrecisionReason = "the result of {0} keeps the rounding precision {1} of the monetary operand";
        private const string ResultRoundingMethodReason = "the result of {0} keeps the rounding method {1} of the monetary operand";
        private const string RatioReason = "the ratio of the amounts before rounding is {0}";
        private const string SelfDifferenceReason = "a monetary value minus itself is zero";

        private const decimal LeftAmount = 5m;
        private const decimal RightAmount = 2m;
        private const int TestRoundingPrecision = 2;
        private const MidpointRounding TestRoundingMethod = MidpointRounding.AwayFromZero;

        // 1.005 rounded to two digits away from zero is 1.01.
        private const decimal UnroundedAmount = 1.005m;

        // The number each row combines with a monetary value of LeftAmount (5), in each operand type.
        private const decimal DecimalOperand = 2m;
        private const long Int64Operand = 2L;
        private const ulong UInt64Operand = 2UL;
        private const int Int32Operand = 2;

        // The results of combining 5 and 2.
        private const decimal Sum = 7m;
        private const decimal NumberMinusMoney = -3m;
        private const decimal MoneyMinusNumber = 3m;
        private const decimal Product = 10m;
        private const decimal Quotient = 2.5m;
        private const decimal Remainder = 1m;

        [Theory]
        [MemberData(nameof(OperandCurrenciesDifferentTestCases))]
        public void ArithmeticOperator_OperandCurrenciesDifferent_ThrowsInvalidOperationException(string testOperation, Func<Money, Money, object> testOperator, Money testLeft, Money testRight)
        {
            // arrange

            // act
            Action testAction = () => testOperator(testLeft, testRight);

            // assert
            testAction.Should().Throw<InvalidOperationException>(DifferentCurrenciesReason, testOperation);
        }

        public static TheoryData<string, Func<Money, Money, object>, Money, Money> OperandCurrenciesDifferentTestCases => new()
        {
            { "+", (left, right) => left + right, Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD), Money.Create(RightAmount, Iso4217CurrencyCurrent.EUR) },
            { "-", (left, right) => left - right, Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD), Money.Create(RightAmount, Iso4217CurrencyCurrent.EUR) },
            { "/", (left, right) => left / right, Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD), Money.Create(RightAmount, Iso4217CurrencyCurrent.EUR) },
            { "%", (left, right) => left % right, Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD), Money.Create(RightAmount, Iso4217CurrencyCurrent.EUR) },
            { "+", (left, right) => left + right, Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD), Money.Create(RightAmount) },
            { "-", (left, right) => left - right, Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD), Money.Create(RightAmount) },
            { "/", (left, right) => left / right, Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD), Money.Create(RightAmount) },
            { "%", (left, right) => left % right, Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD), Money.Create(RightAmount) },
            { "+", (left, right) => left + right, default(Money), Money.Create(RightAmount, Iso4217CurrencyCurrent.USD) },
            { "-", (left, right) => left - right, default(Money), Money.Create(RightAmount, Iso4217CurrencyCurrent.USD) },
            { "/", (left, right) => left / right, default(Money), Money.Create(RightAmount, Iso4217CurrencyCurrent.USD) },
            { "%", (left, right) => left % right, default(Money), Money.Create(RightAmount, Iso4217CurrencyCurrent.USD) },
        };

        [Theory]
        [MemberData(nameof(OperandCurrenciesSameTestCases))]
        public void ArithmeticOperator_OperandCurrenciesSame_ReturnsResultInLeftOperandCurrencyAndRounding(string testOperation, Func<Money, Money, Money> testOperator, decimal expectedAmount)
        {
            // arrange
            var testLeft = Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD, TestRoundingPrecision, TestRoundingMethod);
            var testRight = Money.Create(UnroundedAmount, Iso4217CurrencyCurrent.USD, TestRoundingPrecision, TestRoundingMethod);

            // act
            var testResult = testOperator(testLeft, testRight);

            // assert
            ((decimal)testResult).Should().Be(expectedAmount, ResultAmountReason, testOperation, expectedAmount);
            testResult.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.USD, ResultCurrencyReason, testOperation, Iso4217CurrencyCurrent.USD);
            testResult.RoundingPrecision.Should().Be(TestRoundingPrecision, ResultRoundingPrecisionReason, testOperation, TestRoundingPrecision);
            testResult.RoundingMethod.Should().Be(TestRoundingMethod, ResultRoundingMethodReason, testOperation, TestRoundingMethod);
        }

        // The amounts are combined before rounding, then the result is rounded to two digits away from zero:
        // 5 + 1.005 = 6.005 → 6.01, 5 - 1.005 = 3.995 → 4.00, and 5 % 1.005 = 0.98 (4 × 1.005 = 4.02).
        public static TheoryData<string, Func<Money, Money, Money>, decimal> OperandCurrenciesSameTestCases => new()
        {
            { "+", (left, right) => left + right, 6.01m },
            { "-", (left, right) => left - right, 4.00m },
            { "%", (left, right) => left % right, 0.98m },
        };

        [Fact]
        public void SubtractionOperator_ValueWithRoundingMinusItself_ReturnsZero()
        {
            // arrange
            var testValue = Money.Create(UnroundedAmount, Iso4217CurrencyCurrent.USD, TestRoundingPrecision, TestRoundingMethod);

            // act
            var testResult = testValue - testValue;

            // assert
            ((decimal)testResult).Should().Be(decimal.Zero, SelfDifferenceReason);
        }

        [Fact]
        public void DivisionOperator_OperandCurrenciesSame_ReturnsRatioOfAmountsBeforeRounding()
        {
            // arrange
            // 1.005 / 2 = 0.5025.
            const decimal expectedResult = 0.5025m;
            var testLeft = Money.Create(UnroundedAmount, Iso4217CurrencyCurrent.USD, TestRoundingPrecision, TestRoundingMethod);
            var testRight = Money.Create(RightAmount, Iso4217CurrencyCurrent.USD);

            // act
            decimal testResult = testLeft / testRight;

            // assert
            testResult.Should().Be(expectedResult, RatioReason, expectedResult);
        }

        [Theory]
        [MemberData(nameof(NumberOperandTestCases))]
        public void ArithmeticOperator_NumberOperand_ReturnsResultInMoneyOperandCurrencyAndRounding(string testOperation, Func<Money, Money> testOperator, decimal expectedAmount)
        {
            // arrange
            var testValue = Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD, TestRoundingPrecision, TestRoundingMethod);

            // act
            var testResult = testOperator(testValue);

            // assert
            ((decimal)testResult).Should().Be(expectedAmount, ResultAmountReason, testOperation, expectedAmount);
            testResult.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.USD, ResultCurrencyReason, testOperation, Iso4217CurrencyCurrent.USD);
            testResult.RoundingPrecision.Should().Be(TestRoundingPrecision, ResultRoundingPrecisionReason, testOperation, TestRoundingPrecision);
            testResult.RoundingMethod.Should().Be(TestRoundingMethod, ResultRoundingMethodReason, testOperation, TestRoundingMethod);
        }

        public static TheoryData<string, Func<Money, Money>, decimal> NumberOperandTestCases => new()
        {
            { "decimal + Money", money => DecimalOperand + money, Sum },
            { "long + Money", money => Int64Operand + money, Sum },
            { "ulong + Money", money => UInt64Operand + money, Sum },
            { "decimal - Money", money => DecimalOperand - money, NumberMinusMoney },
            { "long - Money", money => Int64Operand - money, NumberMinusMoney },
            { "ulong - Money", money => UInt64Operand - money, NumberMinusMoney },
            { "decimal * Money", money => DecimalOperand * money, Product },
            { "long * Money", money => Int64Operand * money, Product },
            { "ulong * Money", money => UInt64Operand * money, Product },
            { "int * Money", money => Int32Operand * money, Product },
            { "Money + int", money => money + Int32Operand, Sum },
            { "Money + long", money => money + Int64Operand, Sum },
            { "Money + ulong", money => money + UInt64Operand, Sum },
            { "Money - int", money => money - Int32Operand, MoneyMinusNumber },
            { "Money - long", money => money - Int64Operand, MoneyMinusNumber },
            { "Money - ulong", money => money - UInt64Operand, MoneyMinusNumber },
            { "Money * int", money => money * Int32Operand, Product },
            { "Money * long", money => money * Int64Operand, Product },
            { "Money * ulong", money => money * UInt64Operand, Product },
            { "Money / int", money => money / Int32Operand, Quotient },
            { "Money / long", money => money / Int64Operand, Quotient },
            { "Money / ulong", money => money / UInt64Operand, Quotient },
            { "Money % decimal", money => money % DecimalOperand, Remainder },
            { "Money % int", money => money % Int32Operand, Remainder },
            { "Money % long", money => money % Int64Operand, Remainder },
            { "Money % ulong", money => money % UInt64Operand, Remainder },
        };

        [Theory]
        [MemberData(nameof(UnaryOperatorTestCases))]
        public void UnaryOperator_MoneyOperand_ReturnsResultInMoneyOperandCurrencyAndRounding(string testOperation, Func<Money, Money> testOperator, decimal expectedAmount)
        {
            // arrange
            var testValue = Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD, TestRoundingPrecision, TestRoundingMethod);

            // act
            var testResult = testOperator(testValue);

            // assert
            ((decimal)testResult).Should().Be(expectedAmount, ResultAmountReason, testOperation, expectedAmount);
            testResult.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.USD, ResultCurrencyReason, testOperation, Iso4217CurrencyCurrent.USD);
            testResult.RoundingPrecision.Should().Be(TestRoundingPrecision, ResultRoundingPrecisionReason, testOperation, TestRoundingPrecision);
            testResult.RoundingMethod.Should().Be(TestRoundingMethod, ResultRoundingMethodReason, testOperation, TestRoundingMethod);
        }

        public static TheoryData<string, Func<Money, Money>, decimal> UnaryOperatorTestCases => new()
        {
            { "+Money", money => +money, LeftAmount },
            { "-Money", money => -money, -5m },
            { "++Money", money => ++money, 6m },
            { "--Money", money => --money, 4m },
        };

        [Theory]
        [MemberData(nameof(ObsoleteOperatorTestCases))]
        public void ObsoleteArithmeticOperator_ReachedDynamically_ThrowsInvalidOperationException(string testOperation, Action<object> testOperator)
        {
            // arrange
            // C# rejects these operators at compile time, so they are reached through the runtime binder.
            object testValue = Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD);

            // act
            Action testAction = () => testOperator(testValue);

            // assert
            testAction.Should().Throw<InvalidOperationException>(NotAnAmountOfMoneyReason, testOperation);
        }

        public static TheoryData<string, Action<object>> ObsoleteOperatorTestCases => new()
        {
            { "Money * Money", value => _ = (dynamic)value * (dynamic)value },
            { "decimal / Money", value => _ = DecimalOperand / (dynamic)value },
            { "long / Money", value => _ = Int64Operand / (dynamic)value },
            { "ulong / Money", value => _ = UInt64Operand / (dynamic)value },
            { "decimal % Money", value => _ = DecimalOperand % (dynamic)value },
            { "long % Money", value => _ = Int64Operand % (dynamic)value },
            { "ulong % Money", value => _ = UInt64Operand % (dynamic)value },
        };
    }
}
