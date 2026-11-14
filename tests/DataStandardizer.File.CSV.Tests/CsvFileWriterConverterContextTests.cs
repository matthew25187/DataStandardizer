using System.IO;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// Tests of the context that <see cref="CsvFileWriter{TRecordLine}"/> gives a <see cref="CsvFieldConvertToString{TModel}"/> delegate.
    /// </summary>
    public class CsvFileWriterConverterContextTests
    {
        private const string OtherFieldName = "Other";
        private const string AmountFieldName = "Amount";
        private const string TotalFieldName = "Total";
        private const string OtherValue = "x";
        private const int AmountValue = 7;

        /// <summary>
        /// The text that <see cref="ContextRecordingLine.RecordContext"/> gives for the field it converts.
        /// </summary>
        private const string ConvertedText = "converted";

        /// <summary>
        /// The position of the <c>Amount</c> field in a line whose fields are <c>Other,Amount</c>.
        /// </summary>
        private const int AmountFieldPosition = 1;

        [Fact]
        public void WriteLine_ToStringConverterOnNamedFieldAfterAnotherField_GivesFieldPosition()
        {
            // arrange
            var testLine = new ContextRecordingLine { Other = OtherValue, Amount = AmountValue };
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvWriter = new CsvFileWriter<ContextRecordingLine>(new StringWriter(), options))
            {
                csvWriter.RegisterMapper<NamedAmountSecondMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                testLine.WrittenContext.FieldIndex.Should().Be(AmountFieldPosition);
            }
        }

        [Fact]
        public void WriteLine_ToStringConverterOnNamedFieldAfterAnotherField_GivesFieldName()
        {
            // arrange
            var testLine = new ContextRecordingLine { Other = OtherValue, Amount = AmountValue };
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvWriter = new CsvFileWriter<ContextRecordingLine>(new StringWriter(), options))
            {
                csvWriter.RegisterMapper<NamedAmountSecondMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                testLine.WrittenContext.FieldName.Should().Be(AmountFieldName);
            }
        }

        [Fact]
        public void WriteLine_ToStringConverterOnNamedFieldSecondOnHeaderLine_GivesHeaderLinePosition()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(OtherFieldName, OtherFieldName);
            headerLine.Add(AmountFieldName, AmountFieldName);
            var testLine = new ContextRecordingLine { Other = OtherValue, Amount = AmountValue };
            using (var csvWriter = new CsvFileWriter<ContextRecordingLine>(new StringWriter()))
            {
                csvWriter.RegisterMapper<NamedAmountFirstMapper>();
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(testLine);

                // assert
                testLine.WrittenContext.FieldIndex.Should().Be(AmountFieldPosition);
            }
        }

        [Fact]
        public void WriteLine_ToStringConverterOnIndexedFieldHeaderLineNamesDifferently_GivesHeaderLineFieldName()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(OtherFieldName, OtherFieldName);
            headerLine.Add(TotalFieldName, TotalFieldName);
            var testLine = new ContextRecordingLine { Other = OtherValue, Amount = AmountValue };
            using (var csvWriter = new CsvFileWriter<ContextRecordingLine>(new StringWriter()))
            {
                csvWriter.RegisterMapper<IndexedAmountSharingTotalMapper>();
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(testLine);

                // assert
                testLine.WrittenContext.FieldName.Should().Be(TotalFieldName);
            }
        }

        [Fact]
        public void WriteLine_ToStringConverterOnIndexedFieldHeaderLineNamesDifferently_GivesFieldPosition()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(OtherFieldName, OtherFieldName);
            headerLine.Add(TotalFieldName, TotalFieldName);
            var testLine = new ContextRecordingLine { Other = OtherValue, Amount = AmountValue };
            using (var csvWriter = new CsvFileWriter<ContextRecordingLine>(new StringWriter()))
            {
                csvWriter.RegisterMapper<IndexedAmountSharingTotalMapper>();
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(testLine);

                // assert
                testLine.WrittenContext.FieldIndex.Should().Be(AmountFieldPosition);
            }
        }

        [Fact]
        public void WriteLine_ToStringConverterOnConstantValueMappedByIndex_GivesNoFieldPosition()
        {
            // arrange
            var testLine = new ContextRecordingLine { Other = OtherValue };
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvWriter = new CsvFileWriter<ContextRecordingLine>(new StringWriter(), options))
            {
                csvWriter.RegisterMapper<ConstantAmountMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                testLine.WrittenContext.FieldIndex.Should().BeNull();
            }
        }

        [Fact]
        public void WriteLine_ToStringConverterOnNamedFieldAfterAnotherField_WritesConvertedText()
        {
            // arrange
            var testLine = new ContextRecordingLine { Other = OtherValue, Amount = AmountValue };
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            var stringWriter = new StringWriter();
            using (var csvWriter = new CsvFileWriter<ContextRecordingLine>(stringWriter, options))
            {
                csvWriter.RegisterMapper<NamedAmountSecondMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                stringWriter.ToString().Should().Be("x,converted\r\n");
            }
        }

        /// <summary>
        /// A line that keeps the context its converter was last given.
        /// </summary>
        public sealed class ContextRecordingLine : CsvFileRecordLine
        {
            public string Other
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public int Amount
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            /// <summary>
            /// Gets or sets a value that the tests leave unset, so a field it shares is given <see cref="Amount"/>.
            /// </summary>
            public int Total
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            /// <summary>
            /// Gets the context last given to <see cref="RecordContext"/>.  It isn't a field of the line.
            /// </summary>
            public CsvFieldContext<ContextRecordingLine> WrittenContext { get; private set; }

            /// <summary>
            /// Keeps the context of the field being written, on the line it belongs to, and gives <see cref="ConvertedText"/> as the field's text.
            /// </summary>
            internal static string RecordContext(CsvFieldContext<ContextRecordingLine> context)
            {
                context.Model.WrittenContext = context;
                return ConvertedText;
            }
        }

        /// <summary>
        /// Maps <c>Other</c> and then <c>Amount</c> by name, with a converter on <c>Amount</c>.
        /// </summary>
        public sealed class NamedAmountSecondMapper : CsvFileMapperBase<ContextRecordingLine>
        {
            public NamedAmountSecondMapper()
            {
                this.Map().Property(x => x.Other).HasFieldName(OtherFieldName);
                this.Map().Property(x => x.Amount).HasFieldName(AmountFieldName)
                    .ConvertUsing(new CsvFieldConvertToString<ContextRecordingLine>(ContextRecordingLine.RecordContext));
            }
        }

        /// <summary>
        /// Maps <c>Amount</c> and then <c>Other</c> by name, with a converter on <c>Amount</c>, so without a header line <c>Amount</c> is first.
        /// </summary>
        public sealed class NamedAmountFirstMapper : CsvFileMapperBase<ContextRecordingLine>
        {
            public NamedAmountFirstMapper()
            {
                this.Map().Property(x => x.Amount).HasFieldName(AmountFieldName)
                    .ConvertUsing(new CsvFieldConvertToString<ContextRecordingLine>(ContextRecordingLine.RecordContext));
                this.Map().Property(x => x.Other).HasFieldName(OtherFieldName);
            }
        }

        /// <summary>
        /// Maps <c>Other</c> by name, and <c>Amount</c> and <c>Total</c> both to index 1, so their field names are their property names, with a
        /// converter on <c>Amount</c>.  A header line with
        /// a <c>Total</c> field names the field at index 1 <c>Total</c>.
        /// </summary>
        public sealed class IndexedAmountSharingTotalMapper : CsvFileMapperBase<ContextRecordingLine>
        {
            public IndexedAmountSharingTotalMapper()
            {
                this.Map().Property(x => x.Other).HasFieldName(OtherFieldName);
                this.Map().Property(x => x.Amount).HasFieldIndex(AmountFieldPosition)
                    .ConvertUsing(new CsvFieldConvertToString<ContextRecordingLine>(ContextRecordingLine.RecordContext));
                this.Map().Property(x => x.Total).HasFieldIndex(AmountFieldPosition);
            }
        }

        /// <summary>
        /// Maps <c>Amount</c> to index 0 with a constant value and a converter, and <c>Other</c> by name.  The mapping builder doesn't offer a
        /// constant with a converter, so the constant is set on the mapping directly.
        /// </summary>
        public sealed class ConstantAmountMapper : CsvFileMapperBase<ContextRecordingLine>
        {
            public ConstantAmountMapper()
            {
                this.Map().Property(x => x.Amount).HasFieldIndex(0)
                    .ConvertUsing(new CsvFieldConvertToString<ContextRecordingLine>(ContextRecordingLine.RecordContext));
                this.Map().Property(x => x.Other).HasFieldName(OtherFieldName);
                ((ICsvFileMapper)this)[nameof(ContextRecordingLine.Amount)].ConstantValue = AmountValue;
            }
        }
    }
}
