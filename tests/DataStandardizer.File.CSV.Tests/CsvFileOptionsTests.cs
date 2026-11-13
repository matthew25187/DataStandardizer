using System;
using System.Collections.Generic;
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
        private const string DefaultOptionsText = "CsvFileOptions { BadValueHandler = , Culture = , EmbeddedLineBreak = , Encoding = , ExtendedCharacters = None, FieldDelimiterCharacter = ,, HeaderHandler = , HeaderHandling = None, InconsistentFieldCountHandler = , QuoteHandling = Required, SuppressTrailingBlankFields = False, TerminatorLineBreak = \r\n }";

        /// <summary>
        /// What <see cref="CsvFileOptions.ToString"/> returns for options with a value other than the default for each property whose value
        /// prints the same on every runtime.  The invariant culture prints as an empty name.
        /// </summary>
        private const string NonDefaultOptionsText = "CsvFileOptions { BadValueHandler = , Culture = , EmbeddedLineBreak = \n, Encoding = , ExtendedCharacters = Control, NonAscii, FieldDelimiterCharacter = ;, HeaderHandler = , HeaderHandling = Use, InconsistentFieldCountHandler = , QuoteHandling = Always, SuppressTrailingBlankFields = True, TerminatorLineBreak = \n }";

        /// <summary>
        /// The number of public properties that the hand-written <see cref="CsvFileOptions.Equals(CsvFileOptions)"/>, <c>GetHashCode</c> and
        /// <c>ToString</c> of the .NET Standard builds list.  A property added to the type has to be added to those members too, and here.
        /// </summary>
        private const int ComparedPropertyCount = 12;

        private static readonly Action FirstHandler = () => { };
        private static readonly Action SecondHandler = () => { };

        public static TheoryData<CsvFileOptions, CsvFileOptions> OptionsDifferingInOneProperty => new TheoryData<CsvFileOptions, CsvFileOptions>
        {
            { new CsvFileOptions { BadValueHandler = FirstHandler }, new CsvFileOptions { BadValueHandler = SecondHandler } },
            { new CsvFileOptions { Culture = CultureInfo.InvariantCulture }, new CsvFileOptions() },
            { new CsvFileOptions { EmbeddedLineBreak = LineFeed }, new CsvFileOptions() },
            { new CsvFileOptions { Encoding = Encoding.UTF8 }, new CsvFileOptions() },
            { new CsvFileOptions { ExtendedCharacters = CsvExtendedCharacters.Control }, new CsvFileOptions() },
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

        [Theory]
        [MemberData(nameof(OptionsDifferingInOneProperty))]
        public void EqualityOperator_OptionsDifferingInOneProperty_ReturnsFalse(CsvFileOptions options, CsvFileOptions other)
        {
            // act
            var testResult = options == other;

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void InequalityOperator_OptionsWithSameValues_ReturnsFalse()
        {
            // arrange
            var options = CreateNonDefaultOptions();
            var other = CreateNonDefaultOptions();

            // act
            var testResult = options != other;

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_NullLeftOperand_ReturnsFalse()
        {
            // arrange
            CsvFileOptions left = null;
            var right = new CsvFileOptions();

            // act
            var testResult = left == right;

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_BothOperandsNull_ReturnsTrue()
        {
            // arrange
            CsvFileOptions left = null;
            CsvFileOptions right = null;

            // act
            var testResult = left == right;

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_ObjectOfAnotherType_ReturnsFalse()
        {
            // arrange
            var options = new CsvFileOptions();
            object other = LineFeed;

            // act
            var testResult = options.Equals(other);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_OptionsWithSameValues_HashSetKeepsOne()
        {
            // arrange
            var options = CreateNonDefaultOptions();
            var other = CreateNonDefaultOptions();

            // act
            var testResult = new HashSet<CsvFileOptions> { options, other };

            // assert
            testResult.Should().HaveCount(1);
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

        [Fact]
        public void ToString_NonDefaultOptions_ReturnsEveryPropertyValue()
        {
            // arrange
            var options = new CsvFileOptions
            {
                Culture = CultureInfo.InvariantCulture,
                EmbeddedLineBreak = LineFeed,
                ExtendedCharacters = CsvExtendedCharacters.Control | CsvExtendedCharacters.NonAscii,
                FieldDelimiterCharacter = Semicolon,
                HeaderHandling = CsvFileHeaderHandling.Use,
                QuoteHandling = CsvFieldQuoteHandling.Always,
                SuppressTrailingBlankFields = true,
                TerminatorLineBreak = LineFeed,
            };

            // act
            var testResult = options.ToString();

            // assert
            testResult.Should().Be(NonDefaultOptionsText);
        }

        [Fact]
        public void GetProperties_CsvFileOptions_ReturnsOnlyThePropertiesEqualityCompares()
        {
            // act
            var testResult = typeof(CsvFileOptions).GetProperties();

            // assert
            testResult.Should().HaveCount(ComparedPropertyCount);
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
            ExtendedCharacters = CsvExtendedCharacters.Control | CsvExtendedCharacters.NonAscii,
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
