using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// Tests that <see cref="CsvFieldContext{TModel}"/> compares and hashes by value on every target: the <c>net10.0</c> build is a record,
    /// and the .NET Standard builds, which the <c>net48</c> tests run against, are a class that has to behave the same way.
    /// </summary>
    /// <remarks>
    /// A reader creates a new context for each value it reports to the bad-value handler, so a line with two properties whose validators
    /// reject the same field reports two contexts with the same values.
    /// </remarks>
    public class CsvFieldContextTests
    {
        private const string AmountFieldName = "Amount";
        private const string OtherFieldName = "Other";

        /// <summary>
        /// A file with a header line and one record line.
        /// </summary>
        private const string TwoFieldFile = "Other,Amount\r\nx,abc\r\n";

        /// <summary>
        /// What <see cref="CsvFieldContext{TModel}.ToString"/> returns for the context of the <c>Amount</c> field of <see cref="TwoFieldFile"/>.
        /// </summary>
        private const string AmountContextText = "CsvFieldContext { FieldIndex = 1, FieldName = Amount, HeaderLine = DataStandardizer.File.CSV.CsvFileHeaderLine, Model = DataStandardizer.File.CSV.Tests.CsvFieldContextTests+RejectedFieldLine, Options = CsvFileOptions { BadValueHandler = DataStandardizer.File.CSV.CsvFieldBadValue`1[DataStandardizer.File.CSV.Tests.CsvFieldContextTests+RejectedFieldLine], Culture = , EmbeddedLineBreak = , Encoding = , FieldDelimiterCharacter = ,, HeaderHandler = , HeaderHandling = Use, InconsistentFieldCountHandler = , QuoteHandling = Required, SuppressTrailingBlankFields = False, TerminatorLineBreak = \r\n }, RawFieldValue = abc }";

        /// <summary>
        /// The number of public properties that the hand-written <c>Equals</c>, <c>GetHashCode</c> and <c>ToString</c> of the .NET Standard
        /// builds list.  A property added to the type has to be added to those members too, and here.
        /// </summary>
        private const int ComparedPropertyCount = 6;

        [Fact]
        public void Equals_ContextsForSameField_ReturnsTrue()
        {
            // arrange
            var contexts = ReadRejectedFieldContexts<SameFieldRejectingLineMapper>();

            // act
            var testResult = contexts[0].Equals(contexts[1]);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_ContextsForDifferentFields_ReturnsFalse()
        {
            // arrange
            var contexts = ReadRejectedFieldContexts<DifferentFieldRejectingLineMapper>();

            // act
            var testResult = contexts[0].Equals(contexts[1]);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_ContextsForSameField_ReturnsTrue()
        {
            // arrange
            var contexts = ReadRejectedFieldContexts<SameFieldRejectingLineMapper>();

            // act
            var testResult = contexts[0] == contexts[1];

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void InequalityOperator_ContextsForDifferentFields_ReturnsTrue()
        {
            // arrange
            var contexts = ReadRejectedFieldContexts<DifferentFieldRejectingLineMapper>();

            // act
            var testResult = contexts[0] != contexts[1];

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void EqualityOperator_ContextsForDifferentFields_ReturnsFalse()
        {
            // arrange
            var contexts = ReadRejectedFieldContexts<DifferentFieldRejectingLineMapper>();

            // act
            var testResult = contexts[0] == contexts[1];

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void InequalityOperator_ContextsForSameField_ReturnsFalse()
        {
            // arrange
            var contexts = ReadRejectedFieldContexts<SameFieldRejectingLineMapper>();

            // act
            var testResult = contexts[0] != contexts[1];

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_NullLeftOperand_ReturnsFalse()
        {
            // arrange
            var contexts = ReadRejectedFieldContexts<SameFieldRejectingLineMapper>();
            CsvFieldContext<RejectedFieldLine> left = null;

            // act
            var testResult = left == contexts[0];

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            // arrange
            var contexts = ReadRejectedFieldContexts<SameFieldRejectingLineMapper>();

            // act
            var testResult = contexts[0].Equals(null);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Equals_ContextForSameFieldAsObject_ReturnsTrue()
        {
            // arrange
            var contexts = ReadRejectedFieldContexts<SameFieldRejectingLineMapper>();
            object other = contexts[1];

            // act
            var testResult = contexts[0].Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_ObjectOfAnotherType_ReturnsFalse()
        {
            // arrange
            var contexts = ReadRejectedFieldContexts<SameFieldRejectingLineMapper>();
            object other = AmountFieldName;

            // act
            var testResult = contexts[0].Equals(other);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_ContextsForSameField_HashSetKeepsOne()
        {
            // arrange
            var contexts = ReadRejectedFieldContexts<SameFieldRejectingLineMapper>();

            // act
            var testResult = new HashSet<CsvFieldContext<RejectedFieldLine>>(contexts);

            // assert
            testResult.Should().HaveCount(1);
        }

        [Fact]
        public void GetProperties_CsvFieldContext_ReturnsOnlyThePropertiesEqualityCompares()
        {
            // act
            var testResult = typeof(CsvFieldContext<RejectedFieldLine>).GetProperties();

            // assert
            testResult.Should().HaveCount(ComparedPropertyCount);
        }

        [Fact]
        public void ToString_ContextForField_ReturnsTypeNameAndEveryProperty()
        {
            // arrange
            var contexts = ReadRejectedFieldContexts<SameFieldRejectingLineMapper>();

            // act
            var testResult = contexts[0].ToString();

            // assert
            testResult.Should().Be(AmountContextText);
        }

        /// <summary>
        /// Reads the record line of <see cref="TwoFieldFile"/> with a mapper whose validators reject its fields, and returns the contexts the
        /// bad-value handler is given, in the order it is given them.
        /// </summary>
        private static List<CsvFieldContext<RejectedFieldLine>> ReadRejectedFieldContexts<TMapper>()
            where TMapper : CsvFileMapperBase<RejectedFieldLine>, new()
        {
            var contexts = new List<CsvFieldContext<RejectedFieldLine>>();
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.Use,
                BadValueHandler = new CsvFieldBadValue<RejectedFieldLine>(contexts.Add)
            };
            using (var csvReader = new CsvFileReader<RejectedFieldLine>(new StringReader(TwoFieldFile), options))
            {
                csvReader.RegisterMapper<TMapper>();
                _ = csvReader.ReadLine();
                _ = csvReader.ReadLine();
            }

            return contexts;
        }

        /// <summary>
        /// A line with two string properties.
        /// </summary>
        private sealed class RejectedFieldLine : CsvFileRecordLine
        {
            public string First
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public string Second
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Maps both properties to the <c>Amount</c> field and rejects it for each.
        /// </summary>
        private sealed class SameFieldRejectingLineMapper : CsvFileMapperBase<RejectedFieldLine>
        {
            public SameFieldRejectingLineMapper()
            {
                this.Map()
                    .Property(x => x.First)
                    .HasFieldName(AmountFieldName)
                    .ConvertUsing(typeof(StringConverter))
                    .ValidateUsing(_ => false);
                this.Map()
                    .Property(x => x.Second)
                    .HasFieldName(AmountFieldName)
                    .ConvertUsing(typeof(StringConverter))
                    .ValidateUsing(_ => false);
            }
        }

        /// <summary>
        /// Maps the first property to the <c>Other</c> field and the second to the <c>Amount</c> field, and rejects both.
        /// </summary>
        private sealed class DifferentFieldRejectingLineMapper : CsvFileMapperBase<RejectedFieldLine>
        {
            public DifferentFieldRejectingLineMapper()
            {
                this.Map()
                    .Property(x => x.First)
                    .HasFieldName(OtherFieldName)
                    .ConvertUsing(typeof(StringConverter))
                    .ValidateUsing(_ => false);
                this.Map()
                    .Property(x => x.Second)
                    .HasFieldName(AmountFieldName)
                    .ConvertUsing(typeof(StringConverter))
                    .ValidateUsing(_ => false);
            }
        }
    }
}
