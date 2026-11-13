using System;
using System.Globalization;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// Tests that <see cref="CsvFileOptions"/> compares, hashes and prints by value on every target: the <c>net10.0</c> build is a record, and
    /// the .NET Standard builds, which the <c>net48</c> tests run against, are a class that has to behave the same way.
    /// </summary>
    public class CsvFileOptionsTests
    {
        private const char Semicolon = ';';
        private const string LineFeed = "\n";

        /// <summary>
        /// What <see cref="CsvFileOptions.ToString"/> returns for options with every property at its default.
        /// </summary>
        private const string DefaultOptionsText = "CsvFileOptions { BadValueHandler = , Culture = , EmbeddedLineBreak = , Encoding = , FieldDelimiterCharacter = ,, HeaderHandler = , HeaderHandling = None, InconsistentFieldCountHandler = , QuoteHandling = Required, SuppressTrailingBlankFields = False, TerminatorLineBreak = \r\n }";

        private static readonly Action FirstHandler = () => { };
        private static readonly Action SecondHandler = () => { };

        public static TheoryData<CsvFileOptions, CsvFileOptions> OptionsDifferingInOneProperty => new TheoryData<CsvFileOptions, CsvFileOptions>
        {
            { new CsvFileOptions { BadValueHandler = FirstHandler }, new CsvFileOptions { BadValueHandler = SecondHandler } },
            { new CsvFileOptions { Culture = CultureInfo.InvariantCulture }, new CsvFileOptions() },
            { new CsvFileOptions { EmbeddedLineBreak = LineFeed }, new CsvFileOptions() },
            { new CsvFileOptions { Encoding = Encoding.UTF8 }, new CsvFileOptions() },
            { new CsvFileOptions { FieldDelimiterCharacter = Semicolon }, new CsvFileOptions() },
            { new CsvFileOptions { HeaderHandler = FirstHandler }, new CsvFileOptions() },
            { new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use }, new CsvFileOptions() },
            { new CsvFileOptions { InconsistentFieldCountHandler = FirstHandler }, new CsvFileOptions() },
            { new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Always }, new CsvFileOptions() },
            { new CsvFileOptions { SuppressTrailingBlankFields = true }, new CsvFileOptions() },
            { new CsvFileOptions { TerminatorLineBreak = LineFeed }, new CsvFileOptions() },
        };

        [Fact]
        public void Equals_OptionsWithSameValues_ReturnsTrue()
        {
            // arrange
            var options = CreateNonDefaultOptions();
            var other = CreateNonDefaultOptions();

            // act
            var testResult = options.Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_OptionsWithSameValuesAsObject_ReturnsTrue()
        {
            // arrange
            var options = CreateNonDefaultOptions();
            object other = CreateNonDefaultOptions();

            // act
            var testResult = options.Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(OptionsDifferingInOneProperty))]
        public void Equals_OptionsDifferingInOneProperty_ReturnsFalse(CsvFileOptions options, CsvFileOptions other)
        {
            // act
            var testResult = options.Equals(other);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            // arrange
            var options = new CsvFileOptions();

            // act
            var testResult = options.Equals(null);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_OptionsWithSameValues_ReturnsTrue()
        {
            // arrange
            var options = CreateNonDefaultOptions();
            var other = CreateNonDefaultOptions();

            // act
            var testResult = options == other;

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(OptionsDifferingInOneProperty))]
        public void InequalityOperator_OptionsDifferingInOneProperty_ReturnsTrue(CsvFileOptions options, CsvFileOptions other)
        {
            // act
            var testResult = options != other;

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_OptionsWithSameValues_ReturnsSameHashCode()
        {
            // arrange
            var options = CreateNonDefaultOptions();
            var other = CreateNonDefaultOptions();

            // act
            var testResult = new[] { options.GetHashCode(), other.GetHashCode() };

            // assert
            testResult[0].Should().Be(testResult[1]);
        }

        [Fact]
        public void ToString_DefaultOptions_ReturnsTypeNameAndEveryProperty()
        {
            // arrange
            var options = new CsvFileOptions();

            // act
            var testResult = options.ToString();

            // assert
            testResult.Should().Be(DefaultOptionsText);
        }

        /// <summary>
        /// Creates options with a value other than the default for every property, a new object on each call.
        /// </summary>
        private static CsvFileOptions CreateNonDefaultOptions() => new CsvFileOptions
        {
            BadValueHandler = FirstHandler,
            Culture = CultureInfo.InvariantCulture,
            EmbeddedLineBreak = LineFeed,
            Encoding = Encoding.UTF8,
            FieldDelimiterCharacter = Semicolon,
            HeaderHandler = SecondHandler,
            HeaderHandling = CsvFileHeaderHandling.Use,
            InconsistentFieldCountHandler = FirstHandler,
            QuoteHandling = CsvFieldQuoteHandling.Always,
            SuppressTrailingBlankFields = true,
            TerminatorLineBreak = LineFeed,
        };
    }
}
