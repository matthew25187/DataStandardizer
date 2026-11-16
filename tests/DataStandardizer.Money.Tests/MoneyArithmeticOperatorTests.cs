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
        private const string RatioReason = "the ratio of the amounts is {0}";

        private const decimal LeftAmount = 5m;
        private const decimal RightAmount = 2m;
        private const int TestRoundingPrecision = 2;
        private const MidpointRounding TestRoundingMethod = MidpointRounding.AwayFromZero;

        // 1.005 rounded to two digits away from zero is 1.01.
        private const decimal UnroundedAmount = 1.005m;

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
            { "+", (left, right) => left + right, Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD), Money.Create(RightAmount) },
            { "-", (left, right) => left - right, Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD), Money.Create(RightAmount) },
            { "/", (left, right) => left / right, Money.Create(LeftAmount, Iso4217CurrencyCurrent.USD), Money.Create(RightAmount) },
            { "+", (left, right) => left + right, default(Money), Money.Create(RightAmount, Iso4217CurrencyCurrent.USD) },
            { "-", (left, right) => left - right, default(Money), Money.Create(RightAmount, Iso4217CurrencyCurrent.USD) },
            { "/", (left, right) => left / right, default(Money), Money.Create(RightAmount, Iso4217CurrencyCurrent.USD) },
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

        // The right operand contributes its rounded amount, 1.01.
        public static TheoryData<string, Func<Money, Money, Money>, decimal> OperandCurrenciesSameTestCases => new()
        {
            { "+", (left, right) => left + right, 6.01m },
            { "-", (left, right) => left - right, 3.99m },
        };

        [Fact]
        public void DivisionOperator_OperandCurrenciesSame_ReturnsRatioOfRoundedAmounts()
        {
            // arrange
            // 1.01 / 2 = 0.505.
            const decimal expectedResult = 0.505m;
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
            { "decimal + Money", money => 2m + money, 7m },
            { "long + Money", money => 2L + money, 7m },
            { "ulong + Money", money => 2UL + money, 7m },
            { "decimal - Money", money => 2m - money, -3m },
            { "long - Money", money => 2L - money, -3m },
            { "ulong - Money", money => 2UL - money, -3m },
            { "decimal * Money", money => 2m * money, 10m },
            { "long * Money", money => 2L * money, 10m },
            { "ulong * Money", money => 2UL * money, 10m },
            { "int * Money", money => 2 * money, 10m },
            { "Money + int", money => money + 2, 7m },
            { "Money + long", money => money + 2L, 7m },
            { "Money + ulong", money => money + 2UL, 7m },
            { "Money - int", money => money - 2, 3m },
            { "Money - long", money => money - 2L, 3m },
            { "Money - ulong", money => money - 2UL, 3m },
            { "Money * int", money => money * 2, 10m },
            { "Money * long", money => money * 2L, 10m },
            { "Money * ulong", money => money * 2UL, 10m },
            { "Money / int", money => money / 2, 2.5m },
            { "Money / long", money => money / 2L, 2.5m },
            { "Money / ulong", money => money / 2UL, 2.5m },
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
            { "decimal / Money", value => _ = 2m / (dynamic)value },
            { "long / Money", value => _ = 2L / (dynamic)value },
            { "ulong / Money", value => _ = 2UL / (dynamic)value },
        };
    }
}
