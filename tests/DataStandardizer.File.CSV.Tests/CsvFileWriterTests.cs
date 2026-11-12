using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{

    /// <remarks>
    /// Mappers built from attributes are cached process-wide, by record-line type, and <see cref="CsvMapperCache.Clear"/>
    /// clears them.  Clearing never changes how a line is written, so tests that clear the cache can run alongside the others.
    /// </remarks>
    public class CsvFileWriterTests
    {
        /// <summary>
        /// Size, in characters, of the buffer of the <see cref="StreamWriter"/> that a writer creates over a stream
        /// (16 Ki characters).
        /// </summary>
        private const int StreamBufferSize = 16384;

        /// <summary>
        /// Code page of ISO 8859-1 (Latin-1), which every target framework has built in.
        /// </summary>
        private const int Latin1CodePage = 28591;

        // A field value as long as the stream buffer, so that writing it and a line break fills the buffer once.
        private static readonly string StreamBufferLengthFieldValue = new string('a', StreamBufferSize);

        // A negative sign that no culture uses, so that a value formatted with the culture under test can be told apart
        // from one formatted with any other culture.
        private const string TestNegativeSign = "~";

        // Field names of the lines written.
        private const string FieldName1 = "One";
        private const string FieldName2 = "Two";
        private const string FieldName3 = "Three";
        private const string FieldName4 = "Four";
        private const string FieldName5 = "Five";
        private const string IdFieldName = "ID";
        private const string NameFieldName = "Name";
        private const string DescriptionFieldName = "Description";

        // Field values of the lines written.
        private const string NumberValue = "1";
        private const string TextValue = "Two";
        private const string FirstName = "One";
        private const string FirstDescription = "First";
        private const string SecondName = "Two";
        private const string SecondDescription = "Second";
        private const string ThirdName = "Three";
        private const string ThirdDescription = "Third";
        private const string FirstLongDescription = "This is the first record";
        private const string SecondLongDescription = "This is the second record";
        private const string ValueWithQuote = "Left\"Right";
        private const string ValueWithCrLf = "Left\r\nRight";
        private const string ValueWithDelimiter = "Left,Right";
        private const string FirstAttributeValue = "1st";
        private const string SecondAttributeValue = "2nd";
        private const string HtmlLineBreak = "<br>";
        private const string FirstId = "1";
        private const string SecondId = "2";
        private const string InvariantDecimalSeparator = ".";

        // Field names of header lines, and of record lines that are given only by position.
        private const string HeaderFieldName1 = "1";
        private const string HeaderFieldName2 = "2";
        private const string HeaderFieldName3 = "3";
        private const string HeaderFieldName4 = "4";
        private const string ZeroFieldName = "0";

        // Lines written.
        private const string SingleNumberLine = "1\r\n";
        private const string FirstRecordLine = "1,One\r\n";
        private const string FirstAndThirdRecordLines = "1,One\r\n3,Three\r\n";

        // Field names that differ from the property keys they are mapped to, the values written to them, and the line
        // that results.
        private const string PersonIdFieldName = "person_id";
        private const string PersonNameFieldName = "person_name";
        private const int RenamedId = 5;
        private const string RenamedName = "bob";
        private const string RenamedRecordLine = "5,bob\r\n";

        // Values held under the field names of a line that also holds values under the property keys, and the line that
        // results.
        private const int FieldNameId = 9;
        private const string FieldNameName = "amy";
        private const string FieldNameRecordLine = "9,amy\r\n";

        // A property key given by CsvFieldAttribute, and a value that replaces RenamedId on a line read from a file.
        private const string PersonKeyFieldName = "pid";
        private const int ChangedId = 6;
        private const string ChangedRecordLine = "6,bob\r\n";
        private const string RenamedIdLine = "5\r\n";
        private const string RenamedNameLine = "bob\r\n";

        /// <summary>
        /// The text the converters of <see cref="DelegateConvertedNameLineMapper"/> and <see cref="FixedStringConverter"/> write for every value.
        /// </summary>
        private const string ConvertedName = "CONVERTED";

        private const string ConvertedNameRecordLine = "5,CONVERTED\r\n";
        private const string ConvertedRecordLine = "#5,bob\r\n";

        // The record line that is read and then written.
        private const string RenamedRecordFile = "5,bob";
        private const string FirstLongRecordLine = "1,One,This is the first record\r\n";

        private const string OptionsParameterName = "options";

        // Keys of the CsvFileException data items that describe a record line with a field the first record line doesn't have, and the field
        // count of the first record line of the tests that check them.
        private const string FieldNameDataItem = "FieldName";
        private const string ExpectedFieldCountDataItem = "ExpectedFieldCount";
        private const int FirstLineFieldCount = 2;

        [Fact]
        public void WriteLine_HeaderLineAndQuoteHandlingAlways_WritesHeaderLineWithAllValuesQuoted()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(NumberValue, NumberValue);
            testLine.Add(TextValue, TextValue);

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Always };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("\"1\",\"Two\"\r\n");
            }
        }

        [Fact]
        public void WriteLine_HeaderLineAndQuoteHandlingRequired_WritesHeaderLineWithRequiredValuesQuoted()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(HeaderFieldName1, "One,Two");
            testLine.Add(HeaderFieldName2, "Three\r\nFour");
            testLine.Add(HeaderFieldName3, "Five\"Six");
            testLine.Add(HeaderFieldName4, "Last");

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Required };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("\"One,Two\",\"Three\r\nFour\",\"Five\"\"Six\",Last\r\n");
            }
        }

        [Fact]
        public void WriteLine_HeaderLineAndSuppressTrailingBlankFields_WritesRecordWithoutTrailingBlankFields()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(HeaderFieldName1, FieldName1);
            testLine.Add(HeaderFieldName2, string.Empty);

            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("One\r\n");
            }
        }

        [Theory]
        [InlineData("\n", "One,Two\n"), InlineData("\r", "One,Two\r")]
        public void WriteLine_HeaderLineAndCustomLineBreak_WritesHeaderLineTerminatedByLineBreak(string testLineBreak, string expectedResult)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(FieldName1, FieldName1);
            testLine.Add(FieldName2, FieldName2);

            var options = new CsvFileOptions { TerminatorLineBreak = testLineBreak };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expectedResult);
            }
        }

        [Fact]
        public void WriteLine_HeaderLineAndCustomFieldDelimiter_WritesHeaderLineWithFieldsSeparatedByDelimiter()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(FieldName1, FieldName1);
            testLine.Add(FieldName2, FieldName2);

            var options = new CsvFileOptions { FieldDelimiterCharacter = ';' };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("One;Two\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineAndQuoteHandlingAlways_WritesRecordLineWithAllValuesQuoted()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, TextValue);
            testLine.Add(FieldName3, ValueWithQuote);
            testLine.Add(FieldName4, ValueWithCrLf);
            testLine.Add(FieldName5, ValueWithDelimiter);

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Always };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("\"1\",\"Two\",\"Left\"\"Right\",\"Left\r\nRight\",\"Left,Right\"\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineAndQuoteHandlingAuto_WritesRecordLineWithStringValuesQuoted()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, TextValue);

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Auto };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("1,\"Two\"\r\n");
            }
        }

        [Theory]
        [InlineData("1,000", "\"1,000\"\r\n")]
        [InlineData("1\r\n", "\"1\r\n\"\r\n")]
        public void WriteLine_QuoteHandlingAutoAndNumberWithCharacterRequiringQuotes_WritesValueQuoted(string fieldValue, string expectedLine)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, fieldValue);

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Auto };

            var buffer = new StringBuilder();
            using (new CurrentCultureScope(CultureInfo.InvariantCulture))
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expectedLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAndQuoteHandlingRequired_WritesRecordLineWithRequiredValuesQuoted()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, TextValue);
            testLine.Add(FieldName3, ValueWithQuote);
            testLine.Add(FieldName4, ValueWithCrLf);
            testLine.Add(FieldName5, ValueWithDelimiter);

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Required };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("1,Two,\"Left\"\"Right\",\"Left\r\nRight\",\"Left,Right\"\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithDeserializedValueAndNoCulture_WritesRecordWithSerializedValueUsingInvariantCulture()
        {
            // arrange
            ICsvFileLine testLine = new AllIndexedLine { Id = -1 };

            var options = new CsvFileOptions { Culture = null };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter, options))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("-1,,\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithDeserializedValueAndCulture_WritesRecordWithSerializedValueUsingSpecifiedCulture()
        {
            // arrange
            ICsvFileLine testLine = new AllIndexedLine { Id = -100 };

            var options = new CsvFileOptions { Culture = CreateCulture(TestNegativeSign, InvariantDecimalSeparator) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter, options))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("~100,,\r\n");
            }
        }

        [Fact]
        public void WriteLine_MapperRegisteredOnAnotherWriter_WritesFieldsInLineOrder()
        {
            // arrange
            ICsvFileLine testLine = new InstanceScopedLine { Id = 1, Name = FirstName };

            var buffer = new StringBuilder();
            using (var otherWriter = new CsvFileWriter<InstanceScopedLine>(new StringWriter()))
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<InstanceScopedLine>(stringWriter))
            {
                otherWriter.RegisterMapper<InstanceScopedLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(FirstRecordLine);
            }
        }

        [Fact]
        public void WriteLine_AfterMapperCacheCleared_WritesFieldsInAttributeOrder()
        {
            // arrange
            using (var otherWriter = new CsvFileWriter<ClearedCacheLine>(new StringWriter()))
            {
                otherWriter.WriteLine(new ClearedCacheLine { First = FirstAttributeValue, Second = SecondAttributeValue });
            }

            CsvMapperCache.Clear();

            ICsvFileLine testLine = new ClearedCacheLine { First = FirstAttributeValue, Second = SecondAttributeValue };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ClearedCacheLine>(stringWriter))
            {
                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("2nd,1st\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithDeserializedValueNoCultureNoMapper_WritesRecordWithSerializedValueUsingInvariantCulture()
        {
            // arrange
            ICsvFileLine testLine = new UnmappedLine { Id = -47, Name = FirstName, Description = FirstDescription };

            // A current culture with another negative sign, which the output must not use.
            var buffer = new StringBuilder();
            using (new CurrentCultureScope(CreateCulture(TestNegativeSign, InvariantDecimalSeparator)))
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("-47,One,First\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithDeserializedValueCultureNoMapper_WritesRecordWithSerializedValueUsingSpecifiedCulture()
        {
            // arrange
            ICsvFileLine testLine = new UnmappedLine { Id = -49, Name = FirstName, Description = FirstDescription };

            var options = new CsvFileOptions { Culture = CreateCulture(TestNegativeSign, InvariantDecimalSeparator) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("~49,One,First\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineAndSuppressTrailingBlankFields_WritesRecordWithoutTrailingBlankFields()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, string.Empty);

            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(SingleNumberLine);
            }
        }

        /// <summary>
        /// Record lines whose fields are in a different order from the header line One,Two,Three, or lack one of its fields, and the record
        /// line that results when they are written after it with <see cref="ICsvFileOptions.SuppressTrailingBlankFields"/> set.
        /// </summary>
        public static TheoryData<CsvFileRecordLine, string> RecordLinesInAnotherOrderThanTheHeaderLine
        {
            get
            {
                var lastFieldFirst = new CsvFileRecordLine();
                ((ICsvFileLine)lastFieldFirst).Add(FieldName3, "z");
                ((ICsvFileLine)lastFieldFirst).Add(FieldName1, string.Empty);
                ((ICsvFileLine)lastFieldFirst).Add(FieldName2, string.Empty);

                var middleFieldMissing = new CsvFileRecordLine();
                ((ICsvFileLine)middleFieldMissing).Add(FieldName1, "x");
                ((ICsvFileLine)middleFieldMissing).Add(FieldName3, string.Empty);

                var allFieldsBlank = new CsvFileRecordLine();
                ((ICsvFileLine)allFieldsBlank).Add(FieldName2, string.Empty);
                ((ICsvFileLine)allFieldsBlank).Add(FieldName1, string.Empty);
                ((ICsvFileLine)allFieldsBlank).Add(FieldName3, string.Empty);

                return new TheoryData<CsvFileRecordLine, string>
                {
                    { lastFieldFirst, ",,z\r\n" },
                    { middleFieldMissing, "x\r\n" },
                    { allFieldsBlank, ",,\r\n" }
                };
            }
        }

        [Theory]
        [MemberData(nameof(RecordLinesInAnotherOrderThanTheHeaderLine))]
        public void WriteLine_RecordLineInAnotherOrderThanHeaderLineAndSuppressTrailingBlankFields_TrimsTrailingBlankFieldsInHeaderLineOrder(
            CsvFileRecordLine testLine, string expectedResult)
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(HeaderFieldName1, FieldName1);
            headerLine.Add(HeaderFieldName2, FieldName2);
            headerLine.Add(HeaderFieldName3, FieldName3);

            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                csvWriter.WriteLine(headerLine);
                buffer.Clear();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expectedResult);
            }
        }

        [Fact]
        public void WriteLine_RecordLineInAnotherOrderThanFirstRecordLineAndSuppressTrailingBlankFields_TrimsTrailingBlankFieldsInFirstRecordLineOrder()
        {
            // arrange
            ICsvFileLine firstLine = new CsvFileRecordLine();
            firstLine.Add(FieldName1, NumberValue);
            firstLine.Add(FieldName2, TextValue);
            firstLine.Add(FieldName3, ThirdName);

            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName3, "z");
            testLine.Add(FieldName1, string.Empty);
            testLine.Add(FieldName2, string.Empty);

            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                csvWriter.WriteLine(firstLine);
                buffer.Clear();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(",,z\r\n");
            }
        }

        [Fact]
        public void WriteLine_TrailingMappedFieldConvertedToEmptyTextAndSuppressTrailingBlankFields_LeavesTheFieldOut()
        {
            // arrange
            var testLine = new RenamedBuilderLine { Id = RenamedId, Name = RenamedName };

            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter, options))
            {
                csvWriter.RegisterMapper<EmptyIdConverterLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedNameLine);
            }
        }

        [Fact]
        public void WriteLine_TrailingMappedFieldWithConstantValueAndSuppressTrailingBlankFields_WritesTheConstantValue()
        {
            // arrange
            var testLine = new RenamedBuilderLine { Id = RenamedId, Name = string.Empty };

            var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter, options))
            {
                csvWriter.RegisterMapper<ConstantNameLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_ToStringConverterOnStringProperty_WritesConvertedValue()
        {
            // arrange
            var testLine = new RenamedBuilderLine { Id = RenamedId, Name = RenamedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<DelegateConvertedNameLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ConvertedNameRecordLine);
            }
        }

        [Fact]
        public void WriteLine_TypeConverterOnStringProperty_WritesConvertedValue()
        {
            // arrange
            var testLine = new RenamedBuilderLine { Id = RenamedId, Name = RenamedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<TypeConvertedNameLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ConvertedNameRecordLine);
            }
        }

        [Fact]
        public void WriteLine_TypeConverterNotToStringOnStringProperty_WritesValue()
        {
            // arrange
            var testLine = new RenamedBuilderLine { Id = RenamedId, Name = RenamedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<FromStringOnlyConvertedNameLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_ConstantStringWithTypeConverter_WritesConstantValue()
        {
            // arrange
            var testLine = new ConstantConvertedNameLine { Name = string.Empty };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ConstantConvertedNameLine>(stringWriter))
            {
                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedNameLine);
            }
        }

        [Theory]
        [InlineData("\n", "1,Two\n"), InlineData("\r", "1,Two\r")]
        public void WriteLine_RecordLineAndCustomLineBreak_WritesRecordLineTerminatedByLineBreak(string testLineBreak, string expectedResult)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, TextValue);

            var options = new CsvFileOptions { TerminatorLineBreak = testLineBreak };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expectedResult);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAndCustomFieldDelimiter_WritesRecordLineWithFieldsSeparatedByDelimiter()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, TextValue);
            testLine.Add(FieldName3, "3");

            var options = new CsvFileOptions { FieldDelimiterCharacter = ';' };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("1;Two;3\r\n");
            }
        }

        [Theory]
        [InlineData("\n", "1,\"Left\nRight\"\r\n"), InlineData("\r", "1,\"Left\rRight\"\r\n")]
        public void WriteLine_RecordLineAndEmbeddedLineBreak_WritesRecordLineWithNormalizedFieldValues(string testLineBreak, string expectedResult)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, ValueWithCrLf);

            var options = new CsvFileOptions { EmbeddedLineBreak = testLineBreak };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expectedResult);
            }
        }

        [Fact]
        public void WriteLine_HeaderLineAndRecordLines_WritesCsvFieldsInOrderByHeaderLine()
        {
            // arrange
            ICsvFileLine testHeaderLine = new CsvFileHeaderLine();
            testHeaderLine.Add(IdFieldName, IdFieldName);
            testHeaderLine.Add(NameFieldName, NameFieldName);
            testHeaderLine.Add(DescriptionFieldName, DescriptionFieldName);

            ICsvFileLine testRecordLine1 = new CsvFileRecordLine();
            testRecordLine1.Add(NameFieldName, FirstName);
            testRecordLine1.Add(DescriptionFieldName, FirstLongDescription);
            testRecordLine1.Add(IdFieldName, FirstId);

            ICsvFileLine testRecordLine2 = new CsvFileRecordLine();
            testRecordLine2.Add(DescriptionFieldName, SecondLongDescription);
            testRecordLine2.Add(IdFieldName, SecondId);
            testRecordLine2.Add(NameFieldName, SecondName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(testHeaderLine);
                csvWriter.WriteLine(testRecordLine1);

                // act
                csvWriter.WriteLine(testRecordLine2);

                // assert
                buffer.ToString().Should().Be("ID,Name,Description\r\n1,One,This is the first record\r\n2,Two,This is the second record\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLines_WritesCsvFieldsInOrderByFirstRecordLine()
        {
            // arrange
            ICsvFileLine testRecordLine1 = new CsvFileRecordLine();
            testRecordLine1.Add(IdFieldName, FirstId);
            testRecordLine1.Add(NameFieldName, FirstName);
            testRecordLine1.Add(DescriptionFieldName, FirstLongDescription);

            ICsvFileLine testRecordLine2 = new CsvFileRecordLine();
            testRecordLine2.Add(NameFieldName, SecondName);
            testRecordLine2.Add(DescriptionFieldName, SecondLongDescription);
            testRecordLine2.Add(IdFieldName, SecondId);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(testRecordLine1);

                // act
                csvWriter.WriteLine(testRecordLine2);

                // assert
                buffer.ToString().Should().Be("1,One,This is the first record\r\n2,Two,This is the second record\r\n");
            }
        }

        [Fact]
        public void WriteLine_HeaderLineAndRecordLineWithMapper_WritesCsvFieldsInOrderByMapping()
        {
            // arrange
            ICsvFileLine testHeaderLine = new CsvFileHeaderLine();
            testHeaderLine.Add(nameof(TestLine.Id), nameof(TestLine.Id));
            testHeaderLine.Add(nameof(TestLine.Name), nameof(TestLine.Name));
            testHeaderLine.Add(nameof(TestLine.Description), nameof(TestLine.Description));

            ICsvFileLine testRecordLine = new AllIndexedLine { Description = FirstLongDescription, Name = FirstName, Id = 1 };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();
                csvWriter.WriteLine(testHeaderLine);

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be("Id,Name,Description\r\n1,One,This is the first record\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithAllIndexedMapper_WritesCsvFieldsInOrderByMapping()
        {
            // arrange
            ICsvFileLine testRecordLine = new AllIndexedLine { Description = FirstLongDescription, Name = FirstName, Id = 1 };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(FirstLongRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithAllIndexedMapperAndFieldsInsertedOutOfOrder_WritesCsvFieldsInOrderByMapping()
        {
            // arrange
            ICsvFileLine testRecordLine = new AllIndexedLine();
            testRecordLine.Add(nameof(TestLine.Description), FirstLongDescription);
            testRecordLine.Insert(0, nameof(TestLine.Name), FirstName);
            testRecordLine.Insert(1, nameof(TestLine.Id), 1);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(FirstLongRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithAllIndexedMapperAndFieldRemovedAtPosition_WritesEmptyValueForRemovedField()
        {
            // arrange
            // The initializer adds the fields in the order Description, Name, Id, so position 1 is Name.
            ICsvFileLine testRecordLine = new AllIndexedLine { Description = FirstLongDescription, Name = FirstName, Id = 1 };
            testRecordLine.RemoveAt(1);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be("1,,This is the first record\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithFirstIndexMapper_WritesCsvFieldsInOrderByMapping()
        {
            // arrange
            ICsvFileLine testRecordLine = new FirstIndexedLine { Description = FirstLongDescription, Name = FirstName, Id = 1 };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<FirstIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<FirstIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(FirstLongRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithLastIndexMapper_WritesCsvFieldsInOrderByMapping()
        {
            // arrange
            ICsvFileLine testRecordLine = new LastIndexedLine { Description = FirstLongDescription, Name = FirstName, Id = 1 };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<LastIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<LastIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(FirstLongRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithGapInIndexMapping_WritesCsvFieldsInOrderByIndexAndNameMapping()
        {
            // arrange
            ICsvFileLine testRecordLine = new GapIndexedLine { Name = FirstName, Description = FirstLongDescription, Id = 1 };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<GapIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<GapIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(FirstLongRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithIncompleteFieldList_WritesAvailableCsvFieldsInOrderByMapping()
        {
            // arrange
            var testRecordLine = new AllIndexedLine { Description = FirstLongDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(",,This is the first record\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLinesWithInconsistentFieldCountsAndFieldCountDelegate_CallsFieldCountDelegate()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Name = SecondName, Description = SecondDescription };

            var handled = false;
            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<UnmappedLine>(_ => handled = true) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);

                // assert
                handled.Should().BeTrue();
            }
        }

        [Fact]
        public void WriteLine_RecordLineHandledByFieldCountDelegate_WritesNothing()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Name = SecondName, Description = SecondDescription };

            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<UnmappedLine>(_ => { }) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);

                // assert
                buffer.ToString().Should().Be(FirstRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAfterLineHandledByFieldCountDelegate_WritesFieldsOfLinesWritten()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Name = SecondName, Description = SecondDescription };
            var recordLine3 = new UnmappedLine { Id = 3, Name = ThirdName };

            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<UnmappedLine>(_ => { }) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);
                csvWriter.WriteLine(recordLine3);

                // assert
                buffer.ToString().Should().Be(FirstAndThirdRecordLines);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAfterLineWithInconsistentFieldCountAndNoFieldCountDelegate_WritesFieldsOfLinesWritten()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Name = SecondName, Description = SecondDescription };
            var recordLine3 = new UnmappedLine { Id = 3, Name = ThirdName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                _ = Record.Exception(() => csvWriter.WriteLine(recordLine2));
                csvWriter.WriteLine(recordLine3);

                // assert
                buffer.ToString().Should().Be(FirstAndThirdRecordLines);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAfterLineWhoseValueFailsToFormat_WritesFieldsOfLinesWritten()
        {
            // arrange
            // Without a header line, the fields written are those of the record lines.  The first line isn't written, so the second line's
            // fields are the only ones.
            ICsvFileLine recordLine1 = new CsvFileRecordLine();
            recordLine1.Add(FieldName1, NumberValue);
            recordLine1.Add(FieldName2, TextValue);
            recordLine1.Add(FieldName3, new UnformattableValue());

            ICsvFileLine recordLine2 = new CsvFileRecordLine();
            recordLine2.Add(FieldName1, NumberValue);
            recordLine2.Add(FieldName2, TextValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                // act
                _ = Record.Exception(() => csvWriter.WriteLine(recordLine1));
                csvWriter.WriteLine(recordLine2);

                // assert
                buffer.ToString().Should().Be("1,Two\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLinesWithInconsistentFieldCountsAndNoFieldCountDelegate_ThrowsCsvFileException()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Name = SecondName, Description = SecondDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                Action testAction = () => csvWriter.WriteLine(recordLine2);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage("Expected 2 fields; found 3 fields.");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithSameFieldCountAndAnotherFieldNameAndNoFieldCountDelegate_ThrowsCsvFileException()
        {
            // arrange
            // Without a header line, the first line sets the fields written, so the second line's Description field isn't one of them.
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Description = SecondDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                Action testAction = () => csvWriter.WriteLine(recordLine2);

                // assert
                var exception = testAction.Should().Throw<CsvFileException>()
                    .WithMessage("Field 'Description' is not one of the 2 fields of the first record line.").Which;
                exception.Data[FieldNameDataItem].Should().Be(nameof(UnmappedLine.Description));
                exception.Data[ExpectedFieldCountDataItem].Should().Be(FirstLineFieldCount);
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithFewerFieldsAndAnotherFieldName_ThrowsCsvFileException()
        {
            // arrange
            // The second line has a field that the first line doesn't, as well as fewer fields, so its field count is reported.
            ICsvFileLine recordLine1 = new CsvFileRecordLine();
            recordLine1.Add(FieldName1, NumberValue);
            recordLine1.Add(FieldName2, TextValue);
            recordLine1.Add(FieldName3, TextValue);

            ICsvFileLine recordLine2 = new CsvFileRecordLine();
            recordLine2.Add(FieldName1, NumberValue);
            recordLine2.Add(FieldName4, TextValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                Action testAction = () => csvWriter.WriteLine(recordLine2);

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage("Expected 3 fields; found 2 fields.");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithSameFieldCountAndAnotherFieldNameAndFieldCountDelegate_CallsFieldCountDelegate()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Description = SecondDescription };

            var handled = false;
            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<UnmappedLine>(_ => handled = true) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);

                // assert
                handled.Should().BeTrue();
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithSameFieldCountAndAnotherFieldNameHandledByFieldCountDelegate_WritesNothing()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Description = SecondDescription };

            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<UnmappedLine>(_ => { }) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);

                // assert
                buffer.ToString().Should().Be(FirstRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAfterLineWithAnotherFieldNameHandledByFieldCountDelegate_WritesFieldsOfFirstLine()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2, Description = SecondDescription };
            var recordLine3 = new UnmappedLine { Id = 3, Name = ThirdName };

            var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<UnmappedLine>(_ => { }) };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter, options))
            {
                csvWriter.WriteLine(recordLine1);
                csvWriter.WriteLine(recordLine2);

                // act
                csvWriter.WriteLine(recordLine3);

                // assert
                buffer.ToString().Should().Be(FirstAndThirdRecordLines);
            }
        }

        [Fact]
        public void WriteLine_RecordLineLackingAFieldOfFirstLine_WritesBlankValueForField()
        {
            // arrange
            var recordLine1 = new UnmappedLine { Id = 1, Name = FirstName };
            var recordLine2 = new UnmappedLine { Id = 2 };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<UnmappedLine>(stringWriter))
            {
                csvWriter.WriteLine(recordLine1);

                // act
                csvWriter.WriteLine(recordLine2);

                // assert
                buffer.ToString().Should().Be("1,One\r\n2,\r\n");
            }
        }

        [Fact]
        public void Constructor_FilePathWithNullOptions_ThrowsArgumentNullException()
        {
            // arrange
            // The options are checked before the file is created, so no file is created.
            const string testFilePath = "unused.csv";

            // act
            Action testAction = () => _ = new CsvFileWriter<CsvFileRecordLine>(testFilePath, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(OptionsParameterName);
        }

        [Fact]
        public void WriteLine_StreamConstructor_WritesStreamInBlocksOfStreamBufferSize()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(FieldName1, StreamBufferLengthFieldValue);

            using (var testStream = new RecordingStream())
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testStream))
            {

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                // The buffer is full once the field value is in it, so it is written, and the line break is left in the buffer.
                testStream.WriteSizes.Should().Equal(StreamBufferSize);
            }
        }

        [Fact]
        public void WriteLine_StreamWithEncodingOptionConstructor_WritesStreamInBlocksOfStreamBufferSize()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(FieldName1, StreamBufferLengthFieldValue);

            var options = new CsvFileOptions { Encoding = Encoding.GetEncoding(Latin1CodePage) };
            using (var testStream = new RecordingStream())
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testStream, options))
            {

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                // The buffer is full once the field value is in it, so it is written, and the line break is left in the buffer.
                testStream.WriteSizes.Should().Equal(StreamBufferSize);
            }
        }

        [Fact]
        public void WriteLine_StreamWithEncodingOption_WritesUsingEncoding()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, "Caf\x00E9");

            var options = new CsvFileOptions { Encoding = Encoding.GetEncoding(Latin1CodePage) };
            using (var testStream = new MemoryStream())
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testStream, options);

                // act
                WriteLineAndDispose(csvWriter, testRecordLine);

                // assert
                // "Caf\x00E9" and CRLF in ISO 8859-1.
                testStream.ToArray().Should().Equal(0x43, 0x61, 0x66, 0xE9, 0x0D, 0x0A);
            }
        }

        [Fact]
        public void WriteLine_CallerSuppliedWriter_WritesToCallerSuppliedWriter()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(FieldName1, NumberValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter))
            {

                // act
                csvWriter.WriteLine(testRecordLine);

                // assert
                buffer.ToString().Should().Be(SingleNumberLine);
            }
        }

        [Fact]
        public void WriteLine_StreamWithoutEncodingOption_WritesUtf8WithoutByteOrderMark()
        {
            // arrange
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add(ZeroFieldName, "\x00E9");

            using (var testStream = new MemoryStream())
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testStream);

                // act
                WriteLineAndDispose(csvWriter, testRecordLine);

                // assert
                // "\x00E9" and CRLF in UTF-8, with no byte order mark.
                testStream.ToArray().Should().Equal(0xC3, 0xA9, 0x0D, 0x0A);
            }
        }

        [Fact]
        public void Constructor_StreamWithNullOptions_ThrowsArgumentNullException()
        {
            // arrange
            using (var stream = new MemoryStream())
            {

                // act
                Action testAction = () => _ = new CsvFileWriter<CsvFileRecordLine>(stream, null);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName(OptionsParameterName);
            }
        }

        [Fact]
        public void WriteLine_AfterDisposeWithStream_ThrowsObjectDisposedException()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(ZeroFieldName, NumberValue);

            using (var stream = new MemoryStream())
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stream);
                csvWriter.Dispose();

                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Fact]
        public void WriteLine_AfterDisposeWithCallerSuppliedWriter_ThrowsObjectDisposedException()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(ZeroFieldName, NumberValue);

            using (var stringWriter = new StringWriter())
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter);
                csvWriter.Dispose();

                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Fact]
        public void WriteLine_AfterDisposeWithCallerSuppliedWriter_WritesNothing()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(ZeroFieldName, NumberValue);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter);
                csvWriter.Dispose();

                // act
                _ = Record.Exception(() => csvWriter.WriteLine(testLine));

                // assert
                buffer.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // arrange
            using (var stream = new MemoryStream())
            {
                var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stream);
                csvWriter.Dispose();

                // act
                Action testAction = () => csvWriter.Dispose();

                // assert
                testAction.Should().NotThrow();
            }
        }

        [Fact]
        public void RegisterMapper_AfterDispose_ThrowsObjectDisposedException()
        {
            // arrange
            using (var stream = new MemoryStream())
            {
                var csvWriter = new CsvFileWriter<AllIndexedLine>(stream);
                csvWriter.Dispose();

                // act
                Action testAction = () => csvWriter.RegisterMapper<AllIndexedLineMapper>();

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Theory]
        [InlineData("\"", null, "1,\"\"\"\"\r\n")]
        [InlineData(",", null, "1,\",\"\r\n")]
        [InlineData("Say \"hi\" \"\"", null, "1,\"Say \"\"hi\"\" \"\"\"\"\"\r\n")]
        [InlineData("Left\rRight", null, "1,\"Left\rRight\"\r\n")]
        [InlineData("Left\nRight", null, "1,\"Left\nRight\"\r\n")]
        [InlineData(ValueWithCrLf, null, "1,\"Left\r\nRight\"\r\n")]
        [InlineData("Plain", null, "1,Plain\r\n")]
        [InlineData("Left\rRight", HtmlLineBreak, "1,\"Left<br>Right\"\r\n")]
        [InlineData("Left\nRight", HtmlLineBreak, "1,\"Left<br>Right\"\r\n")]
        [InlineData(ValueWithCrLf, HtmlLineBreak, "1,\"Left<br>Right\"\r\n")]
        [InlineData("Left<br>Right", HtmlLineBreak, "1,\"Left<br>Right\"\r\n")]
        [InlineData("bar", HtmlLineBreak, "1,bar\r\n")]
        [InlineData("Left\nRight", "\n", "1,\"Left\nRight\"\r\n")]
        public void WriteLine_RecordLineAndQuoteHandlingRequired_QuotesOnlyValuesRequiringQuotes(string testFieldValue, string testEmbeddedLineBreak, string expectedResult)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, testFieldValue);

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Required, EmbeddedLineBreak = testEmbeddedLineBreak };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expectedResult);
            }
        }

        [Fact]
        public void WriteLine_RecordLineAndQuoteHandlingRequiredWithEmptyEmbeddedLineBreak_WritesAllValuesQuoted()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberValue);
            testLine.Add(FieldName2, "Plain");

            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Required, EmbeddedLineBreak = string.Empty };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                // Every value contains the empty line break sequence, so every value is quoted.
                buffer.ToString().Should().Be("\"1\",\"Plain\"\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithFormattableValueAndNonInvariantCulture_WritesValueFormattedWithCulture()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add("Amount", 1234.5m);
            testLine.Add(NameFieldName, FirstName);

            var options = new CsvFileOptions { Culture = CreateCulture("-", ",") };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                // The culture's decimal separator is the field delimiter, so the value is quoted.
                buffer.ToString().Should().Be("\"1234,5\",One\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLinesWithToStringConverter_CallsConverterOncePerField()
        {
            // arrange
            var testLine1 = new IdConverterLine { Id = 1, Name = FirstName, Description = FirstDescription };
            var testLine2 = new IdConverterLine { Id = 2, Name = SecondName, Description = SecondDescription };
            var testLine3 = new IdConverterLine { Id = 3, Name = ThirdName, Description = ThirdDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<IdConverterLine>(stringWriter))
            {
                csvWriter.RegisterMapper<IdConverterLineMapper>();
                csvWriter.WriteLine(testLine1);
                csvWriter.WriteLine(testLine2);

                // act
                csvWriter.WriteLine(testLine3);

                // assert
                buffer.ToString().Should().Be("#1,One,First\r\n#2,Two,Second\r\n#3,Three,Third\r\n");
                testLine1.ConvertedIds.Should().Equal(1);
                testLine2.ConvertedIds.Should().Equal(2);
                testLine3.ConvertedIds.Should().Equal(3);
            }
        }

        [Fact]
        public void WriteLine_AttributeMappingRenamesField_WritesPropertyValues()
        {
            // arrange
            var testLine = new RenamedAttributeLine { Id = RenamedId, Name = RenamedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedAttributeLine>(stringWriter))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_BuilderMapperRenamesField_WritesPropertyValues()
        {
            // arrange
            var testLine = new RenamedBuilderLine { Id = RenamedId, Name = RenamedName };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<RenamedBuilderLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RenamedMappingAndLineWithFieldNamesOnly_WritesFieldValues()
        {
            // arrange
            var testLine = new RenamedAttributeLine();
            testLine.Add(PersonIdFieldName, RenamedId);
            testLine.Add(PersonNameFieldName, RenamedName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedAttributeLine>(stringWriter))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_RenamedMappingAndLineWithPropertyKeysAndFieldNames_WritesFieldNameValues()
        {
            // arrange
            var testLine = new RenamedAttributeLine { Id = RenamedId, Name = RenamedName };
            testLine.Add(PersonIdFieldName, FieldNameId);
            testLine.Add(PersonNameFieldName, FieldNameName);

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedAttributeLine>(stringWriter))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(FieldNameRecordLine);
            }
        }

        [Fact]
        public void WriteLine_LineReadWithRenamingMapperAndNoWriterMapper_WritesFieldValues()
        {
            // arrange
            RenamedBuilderLine testLine;
            using (var csvReader = new CsvFileReader<RenamedBuilderLine>(new StringReader(RenamedRecordFile)))
            {
                csvReader.RegisterMapper<RenamedBuilderLineMapper>();
                testLine = (RenamedBuilderLine)csvReader.ReadLine();
            }

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_LineReadWithRenamingMapperAndPropertyChanged_WritesChangedValue()
        {
            // arrange
            RenamedBuilderLine testLine;
            using (var csvReader = new CsvFileReader<RenamedBuilderLine>(new StringReader(RenamedRecordFile)))
            {
                csvReader.RegisterMapper<RenamedBuilderLineMapper>();
                testLine = (RenamedBuilderLine)csvReader.ReadLine();
            }

            testLine.Id = ChangedId;

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<RenamedBuilderLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ChangedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_LineReadWithRenamingMapperAndConverterReadingContextField_WritesConvertedValue()
        {
            // arrange
            RenamedBuilderLine testLine;
            using (var csvReader = new CsvFileReader<RenamedBuilderLine>(new StringReader(RenamedRecordFile)))
            {
                csvReader.RegisterMapper<FieldReadingConverterLineMapper>();
                testLine = (RenamedBuilderLine)csvReader.ReadLine();
            }

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<RenamedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<FieldReadingConverterLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(ConvertedRecordLine);
            }
        }

        [Fact]
        public void WriteLine_BuilderMapperRenamesFieldOfPropertyWithCsvFieldAttribute_WritesPropertyValue()
        {
            // arrange
            var testLine = new KeyedBuilderLine { Id = RenamedId };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<KeyedBuilderLine>(stringWriter))
            {
                csvWriter.RegisterMapper<KeyedBuilderLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(RenamedIdLine);
            }
        }

        [Fact]
        public void WriteLine_MapperRegisteredAfterFirstLine_WritesLaterLinesInOrderByMapping()
        {
            // arrange
            var testLine1 = new ReversedLine { Id = 1, Name = FirstName, Description = FirstDescription };
            var testLine2 = new ReversedLine { Id = 2, Name = SecondName, Description = SecondDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ReversedLine>(stringWriter))
            {
                csvWriter.WriteLine(testLine1);
                csvWriter.RegisterMapper<ReversedLineMapper>();

                // act
                csvWriter.WriteLine(testLine2);

                // assert
                buffer.ToString().Should().Be("1,One,First\r\nSecond,Two,2\r\n");
            }
        }

        [Fact]
        public void WriteLine_ConverterThrowsPartWayThroughLine_ThrowsConverterException()
        {
            // arrange
            var testLine = new ThrowingConverterLine { Id = 1, Name = FirstName, Description = FirstDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ThrowingConverterLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ThrowingConverterLineMapper>();

                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(ThrowingConverterLineMapper.ConverterMessage);
            }
        }

        [Fact]
        public void WriteLine_ConverterThrowsPartWayThroughLine_WritesNothingForLine()
        {
            // arrange
            var testLine = new ThrowingConverterLine { Id = 1, Name = FirstName, Description = FirstDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ThrowingConverterLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ThrowingConverterLineMapper>();

                // act
                _ = Record.Exception(() => csvWriter.WriteLine(testLine));

                // assert
                // The first field converts, but the line is written only once every field has converted.
                buffer.ToString().Should().BeEmpty();
            }
        }

        /// <summary>
        /// Lines that a <see cref="CsvFileWriter{TRecordLine}"/> of <see cref="AllIndexedLine"/> can't write: a plain record line, and a record line of a sibling type.
        /// </summary>
        public static TheoryData<ICsvFileLine> LinesOfAnotherRecordType()
        {
            ICsvFileLine plainLine = new CsvFileRecordLine();
            plainLine.Add(FieldName1, TextValue);

            return new TheoryData<ICsvFileLine>
            {
                plainLine,
                new UnmappedLine { Id = 1, Name = FirstName, Description = FirstDescription }
            };
        }

        [Theory]
        [MemberData(nameof(LinesOfAnotherRecordType))]
        public void WriteLine_LineOfAnotherRecordType_ThrowsArgumentException(ICsvFileLine testLine)
        {
            // arrange
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                // act
                Action testAction = () => csvWriter.WriteLine(testLine);

                // assert
                testAction.Should().Throw<ArgumentException>().WithParameterName("csvLine");
            }
        }

        [Theory]
        [MemberData(nameof(LinesOfAnotherRecordType))]
        public void WriteLine_LineOfAnotherRecordType_WritesNothing(ICsvFileLine testLine)
        {
            // arrange
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                // act
                _ = Record.Exception(() => csvWriter.WriteLine(testLine));

                // assert
                buffer.ToString().Should().BeEmpty();
            }
        }

        [Fact]
        public void WriteLine_NullLine_ThrowsArgumentNullException()
        {
            // arrange
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<AllIndexedLine>(stringWriter))
            {
                // act
                Action testAction = () => csvWriter.WriteLine(null);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("csvLine");
            }
        }

        [Fact]
        public void WriteLine_RecordLineWithConstantAndVariableValueMappings_WritesMappedValues()
        {
            // arrange
            var testLine = new ConstantAndVariableLine { Id = 1, Name = FirstName, Description = FirstDescription };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<ConstantAndVariableLine>(stringWriter))
            {
                csvWriter.RegisterMapper<ConstantAndVariableLineMapper>();

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("42,Constant,First\r\n");
            }
        }

        [Fact]
        public void WriteLine_RecordLinesOfDifferentTypesWithoutRegisteredMapper_WritesEachLineWithItsOwnMapping()
        {
            // arrange
            ICsvFileLine testLine1 = new MixedLine();
            testLine1.Add(nameof(AttributeMappedLine.First), FirstAttributeValue);
            testLine1.Add(nameof(AttributeMappedLine.Second), SecondAttributeValue);

            var testLine2 = new AttributeMappedLine { First = FirstAttributeValue, Second = SecondAttributeValue };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<MixedLine>(stringWriter))
            {
                csvWriter.WriteLine(testLine1);

                // act
                csvWriter.WriteLine(testLine2);

                // assert
                buffer.ToString().Should().Be("1st,2nd\r\n2nd,1st\r\n");
            }
        }

        [Fact]
        public void WriteLine_AttributeMappingOnBaseLineType_WritesFieldsInMappedOrder()
        {
            // arrange
            ICsvFileLine testLine = new InheritedAttributeMappedLine { First = FirstAttributeValue, Second = SecondAttributeValue };

            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<InheritedAttributeMappedLine>(stringWriter))
            {
                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("2nd,1st\r\n");
            }
        }

        /// <summary>
        /// Creates a culture that formats numbers with the given negative sign and decimal separator, and is otherwise the
        /// invariant culture, so that the result doesn't depend on the culture data of the machine.
        /// </summary>
        private static CultureInfo CreateCulture(string negativeSign, string decimalSeparator)
        {
            var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NegativeSign = negativeSign;
            culture.NumberFormat.NumberDecimalSeparator = decimalSeparator;

            return culture;
        }

        /// <summary>
        /// Writes a line and then disposes the writer, so that everything it wrote reaches its stream.
        /// </summary>
        private static void WriteLineAndDispose(CsvFileWriter<CsvFileRecordLine> csvWriter, ICsvFileLine csvLine)
        {
            using (csvWriter)
            {
                csvWriter.WriteLine(csvLine);
            }
        }

        /// <summary>
        /// The record-line type of a writer that writes lines of several types.
        /// </summary>
        private class MixedLine : CsvFileRecordLine
        {
        }

        /// <summary>
        /// A line whose properties are mapped, by attribute, to fields in the opposite order, for <see cref="InheritedAttributeMappedLine"/> to inherit.
        /// </summary>
        private class ReversedAttributeBaseLine : CsvFileRecordLine
        {
            [CsvFieldMapping(1)]
            public string First
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(0)]
            public string Second
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line that inherits every mapped property from <see cref="ReversedAttributeBaseLine"/>.
        /// </summary>
        private sealed class InheritedAttributeMappedLine : ReversedAttributeBaseLine
        {
        }

        private sealed class AttributeMappedLine : MixedLine
        {
            [CsvFieldMapping(1)]
            public string First
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(0)]
            public string Second
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private abstract class TestLine : CsvFileRecordLine
        {
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            public string Description
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line that no mapper is registered for.
        /// </summary>
        private sealed class UnmappedLine : TestLine
        {
        }

        private sealed class AllIndexedLine : TestLine
        {
        }

        private sealed class FirstIndexedLine : TestLine
        {
        }

        private sealed class LastIndexedLine : TestLine
        {
        }

        private sealed class GapIndexedLine : TestLine
        {
        }

        private sealed class ReversedLine : TestLine
        {
        }

        private sealed class ThrowingConverterLine : TestLine
        {
        }

        /// <summary>
        /// A field value that can't be formatted as text.
        /// </summary>
        private sealed class UnformattableValue
        {
            public override string ToString() => throw new InvalidOperationException();
        }

        private sealed class ConstantAndVariableLine : TestLine
        {
        }

        /// <summary>
        /// A line whose properties are mapped, by attribute, to fields with other names.
        /// </summary>
        private sealed class RenamedAttributeLine : CsvFileRecordLine
        {
            [CsvFieldMapping(PersonIdFieldName)]
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(PersonNameFieldName)]
            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line whose properties <see cref="RenamedBuilderLineMapper"/> maps to fields with other names.
        /// </summary>
        private sealed class RenamedBuilderLine : CsvFileRecordLine
        {
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class RenamedBuilderLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public RenamedBuilderLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
                this.Map().Property(x => x.Name).HasFieldName(PersonNameFieldName);
            }
        }

        /// <summary>
        /// Maps Id to a field with another name, with a converter that reads the value from the line by the field name the
        /// context gives, and prefixes it with <c>#</c>.
        /// </summary>
        private sealed class FieldReadingConverterLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public FieldReadingConverterLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldName(PersonIdFieldName)
                    .ConvertUsing(new CsvFieldConvertToString<RenamedBuilderLine>(context => "#" + ((ICsvFileLine)context.Model)[context.FieldName]));
                this.Map().Property(x => x.Name).HasFieldName(PersonNameFieldName);
            }
        }

        /// <summary>
        /// Maps Name and then Id to fields with other names, with a converter that writes empty text for Id.
        /// </summary>
        private sealed class EmptyIdConverterLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public EmptyIdConverterLineMapper()
            {
                this.Map().Property(x => x.Name).HasFieldName(PersonNameFieldName);
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldName(PersonIdFieldName)
                    .ConvertUsing(new CsvFieldConvertToString<RenamedBuilderLine>(context => string.Empty));
            }
        }

        /// <summary>
        /// Maps Id and Name to fields with other names, with a converter that writes <see cref="ConvertedName"/> for Name.
        /// </summary>
        private sealed class DelegateConvertedNameLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public DelegateConvertedNameLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(PersonNameFieldName)
                    .ConvertUsing(new CsvFieldConvertToString<RenamedBuilderLine>(_ => ConvertedName));
            }
        }

        /// <summary>
        /// Maps Id and Name to fields with other names, with <see cref="FixedStringConverter"/> as the type converter of Name.
        /// </summary>
        private sealed class TypeConvertedNameLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public TypeConvertedNameLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(PersonNameFieldName)
                    .ConvertUsing(typeof(FixedStringConverter));
            }
        }

        /// <summary>
        /// Maps Id and Name to fields with other names, with <see cref="FromStringOnlyConverter"/> as the type converter of Name.
        /// </summary>
        private sealed class FromStringOnlyConvertedNameLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public FromStringOnlyConvertedNameLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(PersonNameFieldName)
                    .ConvertUsing(typeof(FromStringOnlyConverter));
            }
        }

        /// <summary>
        /// A line whose Name has <see cref="RenamedName"/> as its constant value, and <see cref="FixedStringConverter"/> as its type converter.
        /// </summary>
        private sealed class ConstantConvertedNameLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0, ConstantValue = RenamedName)]
            [TypeConverter(typeof(FixedStringConverter))]
            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// Converts every string to <see cref="ConvertedName"/>, and doesn't convert to a string.
        /// </summary>
        private sealed class FromStringOnlyConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => false;

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => ConvertedName;
        }

        /// <summary>
        /// Converts every value to <see cref="ConvertedName"/>.
        /// </summary>
        private sealed class FixedStringConverter : TypeConverter
        {
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => ConvertedName;
        }

        /// <summary>
        /// Maps Id and Name to fields with other names, with <see cref="RenamedName"/> as the constant value of Name.
        /// </summary>
        private sealed class ConstantNameLineMapper : CsvFileMapperBase<RenamedBuilderLine>
        {
            public ConstantNameLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(PersonNameFieldName)
                    .HasConstantValue(RenamedName);
            }
        }

        /// <summary>
        /// A line whose property has a key from <see cref="CsvFieldAttribute"/>, which <see cref="KeyedBuilderLineMapper"/>
        /// maps to a field with another name.
        /// </summary>
        private sealed class KeyedBuilderLine : CsvFileRecordLine
        {
            [CsvField(PersonKeyFieldName)]
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class KeyedBuilderLineMapper : CsvFileMapperBase<KeyedBuilderLine>
        {
            public KeyedBuilderLineMapper()
            {
                this.Map().Property(x => x.Id).HasFieldName(PersonIdFieldName);
            }
        }

        private sealed class IdConverterLine : TestLine
        {
            /// <summary>
            /// The IDs that the converter of the Id mapping was called for.  A field, not a property, so that it is not
            /// part of the line.
            /// </summary>
            public readonly List<int> ConvertedIds = new List<int>();
        }

        private sealed class InstanceScopedLine : TestLine
        {
        }

        private sealed class InstanceScopedLineMapper : CsvFileMapperBase<InstanceScopedLine>
        {
            public InstanceScopedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(1);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        private sealed class ClearedCacheLine : CsvFileRecordLine
        {
            [CsvFieldMapping(1)]
            public string First
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(0)]
            public string Second
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class AllIndexedLineMapper : CsvFileMapperBase<AllIndexedLine>
        {
            public AllIndexedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(1);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        private sealed class FirstIndexedLineMapper : CsvFileMapperBase<FirstIndexedLine>
        {
            public FirstIndexedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name);
                this.Map()
                    .Property(x => x.Description);
            }
        }

        private sealed class LastIndexedLineMapper : CsvFileMapperBase<LastIndexedLine>
        {
            public LastIndexedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        private sealed class GapIndexedLineMapper : CsvFileMapperBase<GapIndexedLine>
        {
            public GapIndexedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(10);
            }
        }

        private sealed class IdConverterLineMapper : CsvFileMapperBase<IdConverterLine>
        {
            public IdConverterLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(context =>
                    {
                        context.Model.ConvertedIds.Add(context.Model.Id);
                        return context.Model.Id.ToString("'#'0", CultureInfo.InvariantCulture);
                    });
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(1);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        private sealed class ReversedLineMapper : CsvFileMapperBase<ReversedLine>
        {
            public ReversedLineMapper()
            {
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(0);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(1);
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(2)
                    .ConvertUsing(typeof(Int32Converter));
            }
        }

        /// <summary>
        /// Maps Name first, then Id with a converter that always throws, so the line fails part of the way through.
        /// </summary>
        private sealed class ThrowingConverterLineMapper : CsvFileMapperBase<ThrowingConverterLine>
        {
            public const string ConverterMessage = "The converter failed.";

            public ThrowingConverterLineMapper()
            {
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(0);
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(1)
                    .ConvertUsing(new CsvFieldConvertToString<ThrowingConverterLine>(_ => throw new InvalidOperationException(ConverterMessage)));
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        private sealed class ConstantAndVariableLineMapper : CsvFileMapperBase<ConstantAndVariableLine>
        {
            public ConstantAndVariableLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .HasVariableValue(() => 42);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(1)
                    .HasConstantValue("Constant");
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        [Fact]
        public void WriteLine_HeaderNameWithLineBreakAndEmbeddedLineBreakSet_WritesTheNameAsTheReaderWillGiveItBack()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileHeaderLine();
            testLine.Add(HeaderFieldName1, "One\r\nTwo");
            var options = new CsvFileOptions { EmbeddedLineBreak = "<br>" };
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("\"One<br>Two\"\r\n");
            }
        }

        [Fact]
        public void WriteLine_HeaderNameWithLineBreakAndEmbeddedLineBreakSet_StillOrdersRecordFieldsByTheName()
        {
            // arrange
            ICsvFileLine headerLine = new CsvFileHeaderLine();
            headerLine.Add(HeaderFieldName1, "One\r\nTwo");
            ICsvFileLine recordLine = new CsvFileRecordLine();
            recordLine.Add("One\r\nTwo", TextValue);
            var options = new CsvFileOptions { EmbeddedLineBreak = "<br>" };
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {
                csvWriter.WriteLine(headerLine);

                // act
                csvWriter.WriteLine(recordLine);

                // assert
                buffer.ToString().Should().EndWith(TextValue + "\r\n");
            }
        }

        public static TheoryData<string, string, string> ValuesHoldingPartOfTheTerminator => new TheoryData<string, string, string>
        {
            { "|", "a|b", "\"a|b\"|" },
            { "||", "a||b", "\"a||b\"||" },
            { "||", "x|", "\"x|\"||" },
            { "||", "|x", "\"|x\"||" }
        };

        [Theory]
        [MemberData(nameof(ValuesHoldingPartOfTheTerminator))]
        public void WriteLine_RecordValueHoldingPartOfACustomTerminator_QuotesTheValue(string terminator, string value, string expected)
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, value);
            var options = new CsvFileOptions { TerminatorLineBreak = terminator };
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(expected);
            }
        }

        // "1.5.5" is a number where the period groups digits, as in German, and not where it is the decimal separator, as in the invariant culture.
        private const string NumberOnlyWhereThePeriodGroups = "1.5.5";

        [Fact]
        public void WriteLine_AutoQuotingWithCultureOption_DecidesWhichValuesAreNumbersWithThatCulture()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberOnlyWhereThePeriodGroups);
            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Auto, Culture = new CultureInfo("de-DE") };
            var buffer = new StringBuilder();
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be(NumberOnlyWhereThePeriodGroups + "\r\n");
            }
        }

        [Fact]
        public void WriteLine_AutoQuotingWithNoCultureOption_IgnoresTheCurrentCulture()
        {
            // arrange
            ICsvFileLine testLine = new CsvFileRecordLine();
            testLine.Add(FieldName1, NumberOnlyWhereThePeriodGroups);
            var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Auto };
            var buffer = new StringBuilder();
            using (new CurrentCultureScope(new CultureInfo("de-DE")))
            using (var stringWriter = new StringWriter(buffer))
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options))
            {

                // act
                csvWriter.WriteLine(testLine);

                // assert
                buffer.ToString().Should().Be("\"" + NumberOnlyWhereThePeriodGroups + "\"\r\n");
            }
        }

        [Fact]
        public void Constructor_TextWriterWithNullOptions_ThrowsArgumentNullException()
        {
            // arrange
            using (var stringWriter = new StringWriter())
            {

                // act
                Action testAction = () => _ = new CsvFileWriter<CsvFileRecordLine>(stringWriter, null);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName(OptionsParameterName);
            }
        }
    }
}
