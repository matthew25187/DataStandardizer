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
    /// clears them.  Clearing never changes how a line is read, so tests that clear the cache can run alongside the others.
    /// </remarks>
    public class CsvFileReaderTests
    {
        private const string OptionsParameterName = "options";

        /// <summary>
        /// Size, in bytes, of the buffer of the <see cref="StreamReader"/> that a reader creates over a stream
        /// (16 KiB).
        /// </summary>
        private const int StreamBufferSize = 16384;

        /// <summary>
        /// Code page of ISO 8859-1 (Latin-1), which every target framework has built in.
        /// </summary>
        private const int Latin1CodePage = 28591;

        /// <summary>
        /// Size, in bytes, of the buffer of a caller-supplied <see cref="StreamReader"/>; any size other than
        /// <see cref="StreamBufferSize"/>.
        /// </summary>
        private const int CallerReaderBufferSize = 256;

        private const string BadValueMessage = "Encountered invalid value at field index 1.";
        private const string FieldCountMessage = "Found 2 of 3 expected fields.";

        // Names the reader gives to the fields of a line that has no header line and no mapping.
        private const string PositionalFieldName1 = "Field 1";
        private const string PositionalFieldName2 = "Field 2";
        private const string PositionalFieldName3 = "Field 3";
        private const string PositionalFieldName4 = "Field 4";

        // The names the reader gives a field when another field already has its positional name.
        private const string SecondPositionalFieldName1 = "Field 1 (2)";
        private const string SecondPositionalFieldName2 = "Field 2 (2)";
        private const string SecondPositionalFieldName3 = "Field 3 (2)";
        private const string ThirdPositionalFieldName3 = "Field 3 (3)";

        // Field values of the lines in the test files.
        private const string HeaderValue1 = "One";
        private const string HeaderValue2 = "Two";
        private const string HeaderValue3 = "Three";
        private const string RecordValue1 = "1";
        private const string RecordValue2 = "2";
        private const string RecordValue3 = "3";
        private const string FirstName = "One";
        private const string FirstDescription = "First";
        private const string FirstLongDescription = "This is the first";
        private const string SecondName = "Two";
        private const string SecondDescription = "Second";
        private const string ThirdName = "Three";
        private const string ThirdDescription = "Third";
        private const string CafeLatin1Value = "Caf\x00E9";
        private const string NoelValue = "No\x00EBl";

        // Field names given by mappings and header lines.
        private const string IdFieldName = nameof(TestLine.Id);
        private const string NameFieldName = nameof(TestLine.Name);
        private const string DescriptionFieldName = nameof(TestLine.Description);
        private const string ExtraFieldName = "Extra";
        private const string ExtraFieldValue = "x";

        // Test files.
        private const string NumbersLine = "1,2,3";
        private const string TwoNumberLinesFile = "1,2,3\r\n4,5,6";
        private const string IgnoredHeaderFile = "ID,Name,Description\r\n1,One,This is the first";
        private const string ShortRecordAfterHeaderFile = "Field 1,Field 2,Field 3\r\n1,2";
        private const string ShortRecordAfterRecordFile = "1,2,3\r\n4,5";

        // A header line whose last name is the one the reader gives the third field of a record without a header name for it.
        private const string LongRecordAfterPositionalHeaderNameFile = "A,Field 3\r\n1,2,3";

        private const string EmbeddedQuoteLine = "One Two,Three\"Four,Five Six";

        // A record whose last field has text before its opening quote, which RFC 4180 section 2 paragraph 5 doesn't allow.
        private const string InvalidLastFieldLine = "One,x\"Two\"";
        private const string InvalidLastFieldMappedLine = "1,x\"One\"";
        private const string EmptyAndInvalidLastFieldLine = ",x\"Two\"";
        private const string InvalidConvertedFieldMappedLine = "x\"1\",One";

        // A record whose last field opens a quote that the file never closes, which the escaped rule of RFC 4180 section 2 doesn't allow.
        private const string UnterminatedQuoteFile = "One,\"Two\r\nThree,Four\r\n";

        // Records whose last field breaks the field rules of RFC 4180 section 2: an escaped field is only DQUOTE *(...) DQUOTE, so whitespace
        // can't come before or after it, and a non-escaped field is TEXTDATA, which holds no CR or LF.
        private const string WhiteSpaceBeforeOpeningQuoteLine = "One, \"Two\"";
        private const string WhiteSpaceAfterClosingQuoteLine = "One,\"Two\" ";
        private const string BareLineFeedLine = "One,Two\nThree";

        private const string DuplicateFieldNameMessage = "Found 1 duplicate field names.";
        private const string FieldNamesDataItem = "FieldNames";
        private const string DuplicateFieldName = "a";
        private const string DuplicateFieldNamesHeaderFile = "a,a\r\n1,2";
        private const string TwoShortRecordsFile = "1,2\r\n3,4";
        private const string InvalidLastFieldHeaderFile = "One,x\"Two\"\r\n1,2";
        private const string HeaderHandlerFailureMessage = "Header handler failed.";
        private const string FirstLongRecordLine = "1,One,This is the first";
        private const string TwoRecordsFile = "1,One,First\r\n2,Two,Second";
        private const string ThreeRecordsWithHeaderFile = "Id,Name,Description\r\n1,One,First\r\n2,Two,Second\r\n3,Three,Third";

        // Field names that differ from the property keys they are mapped to, and files that use them.
        private const string PersonIdFieldName = "person_id";
        private const string PersonNameFieldName = "person_name";
        private const string PersonKeyFieldName = "pid";
        private const int RenamedId = 5;
        private const string RenamedName = "bob";
        private const int SecondRenamedId = 6;
        private const string SecondRenamedName = "amy";
        private const string RenamedHeaderFile = "person_id,person_name\r\n5,bob";
        private const string RenamedRecordFile = "5,bob";
        private const string TwoRenamedRecordsWithHeaderFile = "person_id,person_name\r\n5,bob\r\n6,amy";
        private const string RenamedIdHeaderFile = "person_id\r\n5";

        // A file with a column named like the property key that another field is mapped to.
        private const string CompanyName = "Acme";
        private const string PropertyKeyColumnFile = "person_id,Name,person_name\r\n5,Acme,bob";

        // A file without the field that a required mapping is mapped to by name.
        private const string MissingPersonIdHeaderFile = "person_name\r\nbob";
        private const string MissingPersonIdMessage = "Found no field named 'person_id' for required property 'Id'.";

        // The value a constant mapping gives a field that the line it reads doesn't have.
        private const string AddedFieldValue = "Added";

        // A file with a column named like the property key of a required mapping, but not the field it's mapped to.
        private const string PropertyKeyIdHeaderFile = "Id,person_name\r\n5,bob";

        // Files whose header has none of the fields that a test's mappings are mapped to by name.
        private const string OneFieldHeaderFile = "x\r\na";
        private const string TwoFieldHeaderFile = "x,y\r\na,b";
        private const string MissingIndexedDescriptionMessage = "Found no field at index 2 for required property 'Description' in a record of 2 fields.";

        // Files whose fields are mapped by index to properties whose names the files don't use.
        private const string XFieldName = "x";
        private const string YFieldName = "y";
        private const string ZFieldName = "z";
        private const string IndexedFieldValue1 = "a";
        private const string IndexedFieldValue2 = "b";
        private const string IndexedFieldValue3 = "c";
        private const int IndexedId = 7;
        private const string IndexedIdHeaderFile = "x,y,z\r\na,b,7";
        private const string IndexedNameRecordFile = "a,b,c";
        private const string GapRecordLine = "1,One,Skipped,This is the first";

        // The position of the field that a mapping in a test is mapped to, past the last field of the line it reads.
        private const int PastLastFieldIndex = 3;
        private const string MissingIndexedIdMessage = "Found no field at index 3 for required property 'Id' in a record of 3 fields.";
        private const string IndexedIdNameTakenMessage =
            "Found no field at index 3 for property 'Id' in a record of 3 fields, and the record already has a field named 'Id'.";

        // A file whose header names a field like the property that a mapping maps to a position past the last field.
        private const string IndexedIdValue = "7";
        private const string IdNamedHeaderFile = "Id,y,z\r\n7,b,c";

        // A file with a record that has one field more than its header, whose extra field a mapping maps to by index.
        private const string LongRecordIdHeaderFile = "Id,Name\r\n1,One,9";
        private const string LongRecordFieldCountMessage = "Found 3 of 2 expected fields.";

        // A file whose last field holds 1F, which is 31 in hexadecimal.
        private const string HexIdHeaderFile = "x,y,z\r\na,b,1F";
        private const int HexIndexedId = 31;

        // NumbersLine and TwoNumberLinesFile in ASCII.
        private static readonly byte[] NumbersLineBytes = new byte[] { 0x31, 0x2C, 0x32, 0x2C, 0x33 };
        private static readonly byte[] TwoNumberLinesBytes = new byte[] { 0x31, 0x2C, 0x32, 0x2C, 0x33, 0x0D, 0x0A, 0x34, 0x2C, 0x35, 0x2C, 0x36 };

        [Fact]
        public void ReadLine_HeaderLineWithQuotedValues_ReturnsHeaderLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader("\"One\",\"Two\",\"Three\"\r\n1,2,3"), options))
            {

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().BeOfType<CsvFileHeaderLine>();
                testResult.Count.Should().Be(3);
                testResult[0].Should().Be(HeaderValue1);
                testResult[1].Should().Be(HeaderValue2);
                testResult[2].Should().Be(HeaderValue3);
            }
        }

        [Fact]
        public void ReadLine_HeaderLineWithUnquotedValues_ReturnsHeaderLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader("One,Two,Three\r\n1,2,3"), options))
            {

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().BeOfType<CsvFileHeaderLine>();
                testResult.Count.Should().Be(3);
                testResult[0].Should().Be(HeaderValue1);
                testResult[1].Should().Be(HeaderValue2);
                testResult[2].Should().Be(HeaderValue3);
            }
        }

        [Fact]
        public void ReadLine_HeaderLineIgnored_ReturnsRecordLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Ignore };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(IgnoredHeaderFile), options))
            {

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().BeOfType<CsvFileRecordLine>();
                testResult.Count.Should().Be(3);
                testResult[0].Should().Be(RecordValue1);
                testResult[1].Should().Be(FirstName);
                testResult[2].Should().Be(FirstLongDescription);
            }
        }

        [Fact]
        public void ReadLine_HeaderLineIgnoredAndUsingHeaderDelegate_ReturnsRecordLineWithCustomFieldNames()
        {
            // arrange
            const string fieldName1 = "identifier", fieldName2 = "person_name", fieldName3 = "person_description";

            var headerHandler = new CsvFileHeader(_ => new[] { fieldName1, fieldName2, fieldName3 });
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Ignore, HeaderHandler = headerHandler };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(IgnoredHeaderFile), options))
            {

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().BeOfType<CsvFileRecordLine>();
                testResult.Count.Should().Be(3);
                testResult[fieldName1].Should().Be(RecordValue1);
                testResult[fieldName2].Should().Be(FirstName);
                testResult[fieldName3].Should().Be(FirstLongDescription);
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithQuotedValues_ReturnsRecordLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader("\"1\",\"2\",\"3\""), options))
            {

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().BeOfType<CsvFileRecordLine>();
                testResult.Count.Should().Be(3);
                testResult[0].Should().Be(RecordValue1);
                testResult[1].Should().Be(RecordValue2);
                testResult[2].Should().Be(RecordValue3);
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithUnquotedValues_ReturnsRecordLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(NumbersLine), options))
            {

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().BeOfType<CsvFileRecordLine>();
                testResult.Count.Should().Be(3);
                testResult[0].Should().Be(RecordValue1);
                testResult[1].Should().Be(RecordValue2);
                testResult[2].Should().Be(RecordValue3);
            }
        }

        [Theory]
        [InlineData("1,2,3\n4,5,6", "\n"), InlineData("1,2,3\r4,5,6", "\r")]
        public void ReadLine_FileWithCustomLineBreaks_ReturnsRecordLine(string testFile, string lineBreak)
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, TerminatorLineBreak = lineBreak };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(testFile), options))
            {
                _ = csvReader.ReadLine();

                // act
                // Read the second line, to be sure that it was read correctly after the line break.
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().BeOfType<CsvFileRecordLine>();
                testResult.Count.Should().Be(3);
                testResult[0].Should().Be("4");
                testResult[1].Should().Be("5");
                testResult[2].Should().Be("6");
            }
        }

        [Theory]
        [InlineData("\n", "Three\nFour"), InlineData("\r", "Three\rFour")]
        public void ReadLine_CustomEmbeddedLineBreak_ReturnsRecordLineWithLineBreaksReplaced(string lineBreak, string expectedFieldValue)
        {
            // arrange
            var options = new CsvFileOptions { EmbeddedLineBreak = lineBreak };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader("One Two,\"Three\r\nFour\",Five Six"), options))
            {

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().BeOfType<CsvFileRecordLine>();
                testResult.Count.Should().Be(3);
                testResult[0].Should().Be("One Two");
                testResult[1].Should().Be(expectedFieldValue);
                testResult[2].Should().Be("Five Six");
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithFieldCountDifferentFromHeaderLine_CallsInconsistentFieldCountHandler()
        {
            // arrange
            var handlerCalled = false;
            var handler = new CsvFieldCount<CsvFileRecordLine>(_ => handlerCalled = true);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, InconsistentFieldCountHandler = handler };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(ShortRecordAfterHeaderFile), options))
            {
                _ = csvReader.ReadLine(); // the header line

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().BeNull();
                handlerCalled.Should().BeTrue();
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithFieldCountDifferentFromHeaderLine_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, InconsistentFieldCountHandler = null };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(ShortRecordAfterHeaderFile), options))
            {
                _ = csvReader.ReadLine(); // the header line

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(FieldCountMessage);
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithFieldCountDifferentFromPreviousRecordLine_CallsInconsistentFieldCountHandler()
        {
            // arrange
            var handlerCalled = false;
            var handler = new CsvFieldCount<CsvFileRecordLine>(_ => handlerCalled = true);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, InconsistentFieldCountHandler = handler };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(ShortRecordAfterRecordFile), options))
            {
                _ = csvReader.ReadLine(); // the first record line

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().BeNull();
                handlerCalled.Should().BeTrue();
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithFieldCountDifferentFromPreviousRecordLine_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, InconsistentFieldCountHandler = null };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(ShortRecordAfterRecordFile), options))
            {
                _ = csvReader.ReadLine(); // the first record line

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(FieldCountMessage);
            }
        }

        [Fact]
        public void ReadLine_LongerRecordLineWhosePositionalFieldNameIsAHeaderName_CallsInconsistentFieldCountHandler()
        {
            // arrange
            var handlerCalled = false;
            var handler = new CsvFieldCount<CsvFileRecordLine>(_ => handlerCalled = true);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, InconsistentFieldCountHandler = handler };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(LongRecordAfterPositionalHeaderNameFile), options))
            {
                _ = csvReader.ReadLine(); // the header line

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().BeNull();
                handlerCalled.Should().BeTrue();
            }
        }

        [Fact]
        public void ReadLine_LongerRecordLineWhosePositionalFieldNameIsAHeaderName_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, InconsistentFieldCountHandler = null };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(LongRecordAfterPositionalHeaderNameFile), options))
            {
                _ = csvReader.ReadLine(); // the header line

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(LongRecordFieldCountMessage);
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithUnquotedFieldValueHavingEmbeddedDoubleQuote_CallsBadValueHandler()
        {
            // arrange
            var handlerCalled = false;
            var handler = new CsvFieldBadValue<CsvFileRecordLine>(_ => handlerCalled = true);
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, BadValueHandler = handler };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(EmbeddedQuoteLine), options))
            {

                // act
                _ = csvReader.ReadLine();

                // assert
                handlerCalled.Should().BeTrue();
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithUnquotedFieldValueHavingEmbeddedDoubleQuote_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, BadValueHandler = null };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(EmbeddedQuoteLine), options))
            {

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(BadValueMessage);
            }
        }

        [Fact]
        public void ReadLine_UnterminatedQuotedField_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, BadValueHandler = null };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(UnterminatedQuoteFile), options))
            {

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(BadValueMessage);
            }
        }

        [Fact]
        public void ReadLine_UnterminatedQuotedFieldWithBadValueHandler_ReadsEmptyFieldValue()
        {
            // arrange
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.None,
                BadValueHandler = new CsvFieldBadValue<CsvFileRecordLine>(_ => { })
            };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(UnterminatedQuoteFile), options))
            {

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Values.Should().Equal(FirstName, string.Empty);
            }
        }

        [Theory]
        [InlineData(WhiteSpaceBeforeOpeningQuoteLine)]
        [InlineData(WhiteSpaceAfterClosingQuoteLine)]
        [InlineData(BareLineFeedLine)]
        public void ReadLine_FieldOutsideRfc4180Grammar_ThrowsCsvFileException(string line)
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, BadValueHandler = null };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(line), options))
            {

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(BadValueMessage);
            }
        }

        [Theory]
        [InlineData(WhiteSpaceBeforeOpeningQuoteLine)]
        [InlineData(WhiteSpaceAfterClosingQuoteLine)]
        [InlineData(BareLineFeedLine)]
        public void ReadLine_FieldOutsideRfc4180GrammarWithBadValueHandler_ReadsEmptyFieldValue(string line)
        {
            // arrange
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.None,
                BadValueHandler = new CsvFieldBadValue<CsvFileRecordLine>(_ => { })
            };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(line), options))
            {

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Values.Should().Equal(FirstName, string.Empty);
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithInvalidLastFieldValueAndBadValueHandler_ReadsEmptyFieldValue()
        {
            // arrange
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.None,
                BadValueHandler = new CsvFieldBadValue<CsvFileRecordLine>(_ => { })
            };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(InvalidLastFieldLine), options))
            {

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Values.Should().Equal(FirstName, string.Empty);
            }
        }

        [Fact]
        public void ReadLine_MappedLineWithInvalidLastFieldValueAndBadValueHandler_ReadsEmptyPropertyValue()
        {
            // arrange
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.None,
                BadValueHandler = new CsvFieldBadValue<AttributeMappedLine>(_ => { })
            };
            using (var csvReader = new CsvFileReader<AttributeMappedLine>(new StringReader(InvalidLastFieldMappedLine), options))
            {

                // act
                var testResult = csvReader.ReadLine() as AttributeMappedLine;

                // assert
                testResult.Should().NotBeNull();
                testResult.Id.Should().Be(1);
                testResult.Name.Should().BeEmpty();
            }
        }

        [Fact]
        public void ReadLine_MappedLineWithInvalidConvertedFieldValueAndBadValueHandler_CallsHandlerAgainWithEmptyValue()
        {
            // arrange
            // The field is read as empty, and Int32Converter rejects an empty value, so the handler is called a second time.
            var badValues = new List<KeyValuePair<string, int?>>();
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.None,
                BadValueHandler = new CsvFieldBadValue<AttributeMappedLine>(context => badValues.Add(new KeyValuePair<string, int?>(context.RawFieldValue, context.FieldIndex)))
            };
            using (var csvReader = new CsvFileReader<AttributeMappedLine>(new StringReader(InvalidConvertedFieldMappedLine), options))
            {

                // act
                _ = csvReader.ReadLine();

                // assert
                badValues.Should().Equal(new KeyValuePair<string, int?>("x", 0), new KeyValuePair<string, int?>(string.Empty, 0));
            }
        }

        [Fact]
        public void ReadLine_HeaderLineWithInvalidLastFieldNameAndBadValueHandler_ReadsEmptyFieldName()
        {
            // arrange
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.Use,
                BadValueHandler = new CsvFieldBadValue<CsvFileRecordLine>(_ => { })
            };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(InvalidLastFieldLine), options))
            {

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().BeOfType<CsvFileHeaderLine>();
                testResult.Keys.Should().Equal(FirstName, string.Empty);
            }
        }

        [Fact]
        public void ReadLine_HeaderLineWithEmptyAndInvalidLastFieldNamesAndBadValueHandler_ThrowsCsvFileException()
        {
            // arrange
            // Both field names are read as empty, so they are duplicates.
            var options = new CsvFileOptions
            {
                HeaderHandling = CsvFileHeaderHandling.Use,
                BadValueHandler = new CsvFieldBadValue<CsvFileRecordLine>(_ => { })
            };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(EmptyAndInvalidLastFieldLine), options))
            {

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(DuplicateFieldNameMessage)
                    .Which.Data[FieldNamesDataItem].Should().BeEquivalentTo(new[] { string.Empty });
            }
        }

        [Fact]
        public void ReadLine_AfterHeaderLineWithDuplicateFieldNames_ThrowsCsvFileExceptionAgain()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(DuplicateFieldNamesHeaderFile), options))
            {
                _ = Record.Exception(() => csvReader.ReadLine());

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(DuplicateFieldNameMessage)
                    .Which.Data[FieldNamesDataItem].Should().BeEquivalentTo(new[] { DuplicateFieldName });
            }
        }

        [Fact]
        public void ReadLine_AfterHeaderHandlerWithDuplicateFieldNames_ThrowsCsvFileExceptionAgain()
        {
            // arrange
            var headerHandler = new CsvFileHeader(_ => new[] { DuplicateFieldName, DuplicateFieldName });
            var options = new CsvFileOptions { HeaderHandler = headerHandler };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(TwoShortRecordsFile), options))
            {
                _ = Record.Exception(() => csvReader.ReadLine());

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(DuplicateFieldNameMessage)
                    .Which.Data[FieldNamesDataItem].Should().BeEquivalentTo(new[] { DuplicateFieldName });
            }
        }

        [Fact]
        public void ReadLine_AfterHeaderLineWithInvalidValue_ThrowsCsvFileExceptionAgain()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(InvalidLastFieldHeaderFile), options))
            {
                _ = Record.Exception(() => csvReader.ReadLine());

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(BadValueMessage);
            }
        }

        [Fact]
        public void ReadLine_AfterIgnoredHeaderLineWithInvalidValue_ThrowsCsvFileExceptionAgain()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Ignore };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(InvalidLastFieldHeaderFile), options))
            {
                _ = Record.Exception(() => csvReader.ReadLine());

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(BadValueMessage);
            }
        }

        [Fact]
        public void ReadLine_AfterHeaderHandlerThrows_ThrowsHeaderHandlerExceptionAgainWithoutCallingIt()
        {
            // arrange
            var headerHandlerCalls = new List<CsvFileHeaderLine>();
            var headerHandler = new CsvFileHeader(headerLine =>
            {
                headerHandlerCalls.Add(headerLine);
                throw new InvalidOperationException(HeaderHandlerFailureMessage);
            });
            var options = new CsvFileOptions { HeaderHandler = headerHandler };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(TwoShortRecordsFile), options))
            {
                _ = Record.Exception(() => csvReader.ReadLine());

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(HeaderHandlerFailureMessage);
                headerHandlerCalls.Should().HaveCount(1);
            }
        }

        [Fact]
        public void ReadLine_HeaderHandlerReturnsNull_ThrowsInvalidOperationException()
        {
            // arrange
            var headerHandler = new CsvFileHeader(_ => null);
            var options = new CsvFileOptions { HeaderHandler = headerHandler };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(TwoShortRecordsFile), options))
            {

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithFirstIndexMapper_ReadsCsvFieldsInOrderByMapping()
        {
            // arrange
            using (var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(FirstLongRecordLine)))
            {
                csvReader.RegisterMapper<FirstIndexLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as FirstIndexLine;

                // assert
                testResult.Should().NotBeNull();
                testResult.Id.Should().Be(1);
                testResult.Name.Should().Be(FirstName);
                testResult.Description.Should().Be(FirstLongDescription);
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithFirstIndexMapperThenFieldInsertedAtStart_PropertyReturnsMappedValue()
        {
            // arrange
            using (var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(FirstLongRecordLine)))
            {
                csvReader.RegisterMapper<FirstIndexLineMapper>();
                var testLine = (FirstIndexLine)csvReader.ReadLine();
                ((ICsvFileLine)testLine).Insert(0, ExtraFieldName, ExtraFieldValue);

                // act
                var testResult = testLine.Description;

                // assert
                testResult.Should().Be(FirstLongDescription);
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithFirstIndexMapperThenFieldInsertedAtStart_IntIndexerReturnsShiftedValue()
        {
            // arrange
            using (var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(FirstLongRecordLine)))
            {
                csvReader.RegisterMapper<FirstIndexLineMapper>();
                ICsvFileLine testLine = csvReader.ReadLine();
                testLine.Insert(0, ExtraFieldName, ExtraFieldValue);

                // act
                var testResult = testLine[1];

                // assert
                testResult.Should().Be(1);
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithFirstIndexMapperThenFirstFieldRemoved_PropertyReturnsMappedValue()
        {
            // arrange
            using (var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(FirstLongRecordLine)))
            {
                csvReader.RegisterMapper<FirstIndexLineMapper>();
                var testLine = (FirstIndexLine)csvReader.ReadLine();
                ((ICsvFileLine)testLine).RemoveAt(0);

                // act
                var testResult = testLine.Name;

                // assert
                testResult.Should().Be(FirstName);
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithFirstIndexMapperThenFirstFieldRemoved_IntIndexerReturnsShiftedValue()
        {
            // arrange
            using (var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(FirstLongRecordLine)))
            {
                csvReader.RegisterMapper<FirstIndexLineMapper>();
                ICsvFileLine testLine = csvReader.ReadLine();
                testLine.RemoveAt(0);

                // act
                var testResult = testLine[0];

                // assert
                testResult.Should().Be(FirstName);
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithLastIndexMapper_ReadsCsvFieldsInOrderByMapping()
        {
            // arrange
            using (var csvReader = new CsvFileReader<LastIndexLine>(new StringReader(FirstLongRecordLine)))
            {
                csvReader.RegisterMapper<LastIndexLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as LastIndexLine;

                // assert
                testResult.Should().NotBeNull();
                testResult.Id.Should().Be(1);
                testResult.Name.Should().Be(FirstName);
                testResult.Description.Should().Be(FirstLongDescription);
            }
        }

        [Fact]
        public void ReadLine_RecordLineWithGapIndexMapper_ReadsFieldAtEachMappedIndex()
        {
            // arrange
            using (var csvReader = new CsvFileReader<GapIndexLine>(new StringReader(GapRecordLine)))
            {
                csvReader.RegisterMapper<GapIndexLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as GapIndexLine;

                // assert
                testResult.Should().NotBeNull();
                testResult.Id.Should().Be(1);
                testResult.Name.Should().Be(FirstName);
                testResult.Description.Should().Be(FirstLongDescription);
            }
        }

        [Fact]
        public void RegisterMapper_SameMapperTwice_DoesNotThrow()
        {
            // arrange
            using (var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(string.Empty)))
            {
                csvReader.RegisterMapper<FirstIndexLineMapper>();

                // act
                Action testAction = () => csvReader.RegisterMapper<FirstIndexLineMapper>();

                // assert
                testAction.Should().NotThrow();
            }
        }

        [Fact]
        public void RegisterMapper_SameMapperTwice_KeepsOneMapperForRecordLineType()
        {
            // arrange
            using (var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(string.Empty)))
            {
                csvReader.RegisterMapper<FirstIndexLineMapper>();

                // act
                csvReader.RegisterMapper<FirstIndexLineMapper>();

                // assert
                csvReader.Context.Mappers.Should().ContainKey(typeof(FirstIndexLine))
                    .WhoseValue.Should().BeOfType<FirstIndexLineMapper>();
            }
        }

        [Fact]
        public void ReadLine_MapperRegisteredOnAnotherReader_ReadsFieldsWithoutMapping()
        {
            // arrange
            using (var otherReader = new CsvFileReader<InstanceScopedLine>(new StringReader(string.Empty)))
            using (var csvReader = new CsvFileReader<InstanceScopedLine>(new StringReader(FirstLongRecordLine)))
            {
                otherReader.RegisterMapper<InstanceScopedLineMapper>();

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().NotBeNull();
                testResult.Keys.Should().Equal(PositionalFieldName1, PositionalFieldName2, PositionalFieldName3);
            }
        }

        [Fact]
        public void Context_MapperRegisteredOnAnotherReader_HasNoMappers()
        {
            // arrange
            using (var otherReader = new CsvFileReader<InstanceScopedLine>(new StringReader(string.Empty)))
            using (var csvReader = new CsvFileReader<InstanceScopedLine>(new StringReader(string.Empty)))
            {
                otherReader.RegisterMapper<InstanceScopedLineMapper>();

                // act
                var testResult = csvReader.Context;

                // assert
                testResult.Mappers.Should().BeEmpty();
            }
        }

        [Fact]
        public void Context_MapperRegisteredAfterContextRead_ShowsMapper()
        {
            // arrange
            using (var csvReader = new CsvFileReader<InstanceScopedLine>(new StringReader(string.Empty)))
            {
                var testResult = csvReader.Context;

                // act
                csvReader.RegisterMapper<InstanceScopedLineMapper>();

                // assert
                testResult.Mappers.Should().ContainSingle()
                    .Which.Value.Should().BeOfType<InstanceScopedLineMapper>();
            }
        }

        [Fact]
        public void Context_MapperUnregistered_HasNoMappers()
        {
            // arrange
            using (var csvReader = new CsvFileReader<InstanceScopedLine>(new StringReader(string.Empty)))
            {
                csvReader.RegisterMapper<InstanceScopedLineMapper>();
                var testResult = csvReader.Context;

                // act
                csvReader.UnregisterMapper<InstanceScopedLineMapper>();

                // assert
                testResult.Mappers.Should().BeEmpty();
            }
        }

        [Fact]
        public void RegisterMapper_MapperOfAnotherTypeRegistered_ReplacesMapper()
        {
            // arrange
            using (var csvReader = new CsvFileReader<InstanceScopedLine>(new StringReader(string.Empty)))
            {
                csvReader.RegisterMapper<InstanceScopedLineMapper>();
                var testResult = csvReader.Context;

                // act
                csvReader.RegisterMapper<ReversedInstanceScopedLineMapper>();

                // assert
                testResult.Mappers.Should().ContainSingle()
                    .Which.Value.Should().BeOfType<ReversedInstanceScopedLineMapper>();
            }
        }

        [Fact]
        public void ReadLine_MapperOfAnotherTypeRegistered_UsesLaterMapper()
        {
            // arrange
            using (var csvReader = new CsvFileReader<InstanceScopedLine>(new StringReader(FirstLongRecordLine)))
            {
                csvReader.RegisterMapper<InstanceScopedLineMapper>();
                csvReader.RegisterMapper<ReversedInstanceScopedLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as InstanceScopedLine;

                // assert
                testResult.Should().NotBeNull();
                testResult.Name.Should().Be(FirstLongDescription);
                testResult.Description.Should().Be(FirstName);
            }
        }

        [Fact]
        public void UnregisterMapper_MapperOfAnotherTypeRegistered_KeepsRegisteredMapper()
        {
            // arrange
            using (var csvReader = new CsvFileReader<InstanceScopedLine>(new StringReader(string.Empty)))
            {
                csvReader.RegisterMapper<InstanceScopedLineMapper>();
                var testResult = csvReader.Context;

                // act
                csvReader.UnregisterMapper<ReversedInstanceScopedLineMapper>();

                // assert
                testResult.Mappers.Should().ContainSingle()
                    .Which.Value.Should().BeOfType<InstanceScopedLineMapper>();
            }
        }

        [Fact]
        public void ReadLine_AfterMapperCacheCleared_MapsFieldsByAttributes()
        {
            // arrange
            using (var otherReader = new CsvFileReader<AttributeMappedLine>(new StringReader(TwoRecordsFile)))
            {
                otherReader.ReadLine();
            }

            CsvMapperCache.Clear();

            using (var csvReader = new CsvFileReader<AttributeMappedLine>(new StringReader(TwoRecordsFile)))
            {
                // act
                var testResult = csvReader.ReadLine() as AttributeMappedLine;

                // assert
                testResult.Should().NotBeNull();
                testResult.Id.Should().Be(1);
                testResult.Name.Should().Be(FirstName);
            }
        }

        [Fact]
        public void ReadLine_MapperCacheClearedPartWayThroughFile_MapsLaterLinesByAttributes()
        {
            // arrange
            using (var csvReader = new CsvFileReader<AttributeMappedLine>(new StringReader(TwoRecordsFile)))
            {
                csvReader.ReadLine();
                CsvMapperCache.Clear();

                // act
                var testResult = csvReader.ReadLine() as AttributeMappedLine;

                // assert
                testResult.Should().NotBeNull();
                testResult.Id.Should().Be(2);
                testResult.Name.Should().Be(SecondName);
            }
        }

        [Fact]
        public void ReadLine_MapperRegisteredAfterFirstLine_UsesRegisteredMapper()
        {
            // arrange
            using (var csvReader = new CsvFileReader<LateMappedLine>(new StringReader(TwoRecordsFile)))
            {

                // act
                // The line before the mapper is registered and the line after it.
                var firstLine = csvReader.ReadLine() as LateMappedLine;
                csvReader.RegisterMapper<LateMappedLineMapper>();
                var testResult = csvReader.ReadLine() as LateMappedLine;

                // assert
                firstLine.Should().NotBeNull();
                firstLine.Id.Should().Be(0);
                testResult.Should().NotBeNull();
                testResult.Id.Should().Be(2);
                testResult.Name.Should().Be(SecondName);
            }
        }

        [Fact]
        public void ReadLine_MapperUnregisteredAfterFirstLine_StopsMapping()
        {
            // arrange
            using (var csvReader = new CsvFileReader<UnregisteredLine>(new StringReader(TwoRecordsFile)))
            {
                csvReader.RegisterMapper<UnregisteredLineMapper>();

                // act
                // The line before the mapper is unregistered and the line after it.
                var firstLine = csvReader.ReadLine() as UnregisteredLine;
                csvReader.UnregisterMapper<UnregisteredLineMapper>();
                var testResult = csvReader.ReadLine();

                // assert
                firstLine.Should().NotBeNull();
                firstLine.Id.Should().Be(1);
                testResult.Should().NotBeNull();
                testResult[PositionalFieldName1].Should().Be(RecordValue2);
            }
        }

        [Fact]
        public void ReadLine_UnmappedRecordLinesWithoutHeader_NamesFieldsByPosition()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(TwoRecordsFile), options))
            {
                _ = csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().BeEquivalentTo(new[] { PositionalFieldName1, PositionalFieldName2, PositionalFieldName3 }, config => config.WithStrictOrdering());
                testResult[PositionalFieldName3].Should().Be(SecondDescription);
            }
        }

        [Fact]
        public void ReadLine_UnmappedRecordLineWithHeader_NamesFieldsByHeader()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader("ID,Name,Description\r\n1,One,First"), options))
            {
                _ = csvReader.ReadLine(); // the header line

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().BeEquivalentTo(new[] { "ID", NameFieldName, DescriptionFieldName }, config => config.WithStrictOrdering());
                testResult[DescriptionFieldName].Should().Be(FirstDescription);
            }
        }

        [Fact]
        public void ReadLine_MappedRecordLineWithMoreFieldsThanMappings_NamesFieldsByMapperThenPosition()
        {
            // arrange
            using (var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader("1,One,First,Extra")))
            {
                csvReader.RegisterMapper<FirstIndexLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as FirstIndexLine;

                // assert
                ((ICsvFileLine)testResult).Keys.Should().BeEquivalentTo(new[] { IdFieldName, NameFieldName, DescriptionFieldName, PositionalFieldName4 }, config => config.WithStrictOrdering());
                testResult.Id.Should().Be(1);
                ((ICsvFileLine)testResult)[PositionalFieldName4].Should().Be(ExtraFieldName);
            }
        }

        [Fact]
        public void ReadLine_MappingNamedLikeALaterPositionalField_KeepsMappingNameAndRenamesLaterField()
        {
            // arrange
            using (var csvReader = new CsvFileReader<PositionalNameLine>(new StringReader(NumbersLine)))
            {
                csvReader.RegisterMapper<PositionalNameLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as PositionalNameLine;

                // assert
                ((ICsvFileLine)testResult).Keys.Should().Equal(PositionalFieldName2, SecondPositionalFieldName2, PositionalFieldName3);
                testResult.Name.Should().Be(RecordValue1);
            }
        }

        [Fact]
        public void ReadLine_MappingPastLastFieldNamedLikeAPositionalField_RenamesPositionalField()
        {
            // arrange
            using (var csvReader = new CsvFileReader<PastLastPositionalNameLine>(new StringReader(TwoShortRecordsFile)))
            {
                csvReader.RegisterMapper<PastLastPositionalNameLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as PastLastPositionalNameLine;

                // assert
                ((ICsvFileLine)testResult).Keys.Should().Equal(SecondPositionalFieldName1, IdFieldName);
                testResult.Name.Should().BeNull();
            }
        }

        [Fact]
        public void ReadLine_HeaderHandlerNamesTakingPositionalFieldNameAndItsSuffix_NamesFieldWithNextSuffix()
        {
            // arrange
            var headerHandler = new CsvFileHeader(_ => new[] { PositionalFieldName3, SecondPositionalFieldName3 });
            var options = new CsvFileOptions { HeaderHandler = headerHandler };
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(NumbersLine), options))
            {
                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().Equal(PositionalFieldName3, SecondPositionalFieldName3, ThirdPositionalFieldName3);
            }
        }

        [Fact]
        public void ReadLine_SeveralMappedRecordLines_MapsFirstLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(ThreeRecordsWithHeaderFile), options))
            {
                csvReader.RegisterMapper<FirstIndexLineMapper>();
                _ = csvReader.ReadLine(); // the header line

                // act
                var testResult = csvReader.ReadLine() as FirstIndexLine;

                // assert
                testResult.Id.Should().Be(1);
                testResult.Name.Should().Be(FirstName);
                testResult.Description.Should().Be(FirstDescription);
            }
        }

        [Fact]
        public void ReadLine_SeveralMappedRecordLines_MapsSecondLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(ThreeRecordsWithHeaderFile), options))
            {
                csvReader.RegisterMapper<FirstIndexLineMapper>();
                _ = csvReader.ReadLine(); // the header line
                _ = csvReader.ReadLine(); // the first record line

                // act
                var testResult = csvReader.ReadLine() as FirstIndexLine;

                // assert
                testResult.Id.Should().Be(2);
                testResult.Name.Should().Be(SecondName);
                testResult.Description.Should().Be(SecondDescription);
            }
        }

        [Fact]
        public void ReadLine_SeveralMappedRecordLines_MapsThirdLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(ThreeRecordsWithHeaderFile), options))
            {
                csvReader.RegisterMapper<FirstIndexLineMapper>();
                _ = csvReader.ReadLine(); // the header line
                _ = csvReader.ReadLine(); // the first record line
                _ = csvReader.ReadLine(); // the second record line

                // act
                var testResult = csvReader.ReadLine() as FirstIndexLine;

                // assert
                testResult.Id.Should().Be(3);
                testResult.Name.Should().Be(ThirdName);
                testResult.Description.Should().Be(ThirdDescription);
            }
        }

        [Fact]
        public void ReadLine_MapperWithFromStringConverter_ConvertsFieldValue()
        {
            // arrange
            // The converter reads the field as a hexadecimal number, so 1A is 26.
            const int expectedId = 26;
            using (var csvReader = new CsvFileReader<ConverterLine>(new StringReader("1A,One,First")))
            {
                csvReader.RegisterMapper<ConverterLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as ConverterLine;

                // assert
                testResult.Id.Should().Be(expectedId);
                testResult.Name.Should().Be(FirstName);
            }
        }

        [Fact]
        public void ReadLine_FromStringConverterThrows_PropagatesException()
        {
            // arrange
            using (var csvReader = new CsvFileReader<ConverterLine>(new StringReader("x,One,First")))
            {
                csvReader.RegisterMapper<ConverterLineMapper>();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<FormatException>();
            }
        }

        [Fact]
        public void ReadLine_MapperWithConstantValue_SetsConstantValue()
        {
            // arrange
            using (var csvReader = new CsvFileReader<ConstantLine>(new StringReader("1,One,First")))
            {
                csvReader.RegisterMapper<ConstantLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as ConstantLine;

                // assert
                testResult.Id.Should().Be(1);
                testResult.Name.Should().Be(ConstantLineMapper.ConstantName);
                testResult.Description.Should().Be(FirstDescription);
            }
        }

        [Fact]
        public void ReadLine_MapperWithVariableValueGenerators_SetsGeneratedValuesOnFirstLine()
        {
            // arrange
            using (var csvReader = new CsvFileReader<GeneratorLine>(new StringReader(TwoRecordsFile)))
            {
                csvReader.RegisterMapper<GeneratorLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as GeneratorLine;

                // assert
                testResult.Id.Should().Be(GeneratorLineMapper.GeneratedId);
                testResult.Name.Should().Be(GeneratorLineMapper.GeneratedName);
                testResult.Description.Should().Be(FirstDescription);
            }
        }

        [Fact]
        public void ReadLine_MapperWithVariableValueGenerators_SetsGeneratedValuesOnLaterLine()
        {
            // arrange
            using (var csvReader = new CsvFileReader<GeneratorLine>(new StringReader(TwoRecordsFile)))
            {
                csvReader.RegisterMapper<GeneratorLineMapper>();
                _ = csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as GeneratorLine;

                // assert
                testResult.Id.Should().Be(GeneratorLineMapper.GeneratedId);
                testResult.Name.Should().Be(GeneratorLineMapper.GeneratedName);
                testResult.Description.Should().Be(SecondDescription);
            }
        }

        [Fact]
        public void ReadLine_MappingAddsFieldToLine_LaterOptionalMappingSeesAddedField()
        {
            // arrange
            using (var csvReader = new CsvFileReader<AddedFieldLine>(new StringReader("1")))
            {
                csvReader.RegisterMapper<AddedFieldLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as AddedFieldLine;

                // assert
                testResult.Id.Should().Be(1);
                ((ICsvFileLine)testResult).Keys.Should().BeEquivalentTo(new[] { IdFieldName, ExtraFieldName }, config => config.WithStrictOrdering());
                testResult.ValidatedFieldNames.Should().Equal(ExtraFieldName);
            }
        }

        [Fact]
        public void ReadLine_AttributeMappingOnBaseLineType_PropertiesReturnConvertedFieldValues()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<InheritedAttributeLine>(new StringReader(RenamedHeaderFile), options))
            {
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as InheritedAttributeLine;

                // assert
                testResult.Id.Should().Be(RenamedId);
                testResult.Name.Should().Be(RenamedName);
            }
        }

        [Fact]
        public void ReadLine_AttributeMappingRenamesHeaderField_PropertiesReturnFieldValues()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RenamedAttributeLine>(new StringReader(RenamedHeaderFile), options))
            {
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as RenamedAttributeLine;

                // assert
                testResult.Id.Should().Be(RenamedId);
                testResult.Name.Should().Be(RenamedName);
            }
        }

        [Fact]
        public void ReadLine_BuilderMapperRenamesHeaderField_PropertiesReturnFieldValues()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RenamedBuilderLine>(new StringReader(RenamedHeaderFile), options))
            {
                csvReader.RegisterMapper<RenamedBuilderLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as RenamedBuilderLine;

                // assert
                testResult.Id.Should().Be(RenamedId);
                testResult.Name.Should().Be(RenamedName);
            }
        }

        [Fact]
        public void ReadLine_IndexMappingWithOtherHeaderNames_PropertyReturnsFieldAtIndex()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<IndexedIdLine>(new StringReader(IndexedIdHeaderFile), options))
            {
                csvReader.RegisterMapper<IndexedIdLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as IndexedIdLine;

                // assert
                testResult.Id.Should().Be(IndexedId);
            }
        }

        [Fact]
        public void ReadLine_IndexMappingWithOtherHeaderNames_KeepsOtherFieldValues()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<IndexedIdLine>(new StringReader(IndexedIdHeaderFile), options))
            {
                csvReader.RegisterMapper<IndexedIdLineMapper>();
                csvReader.ReadLine();

                // act
                ICsvFileLine testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().Equal(XFieldName, YFieldName, ZFieldName);
                testResult.Values.Should().Equal(IndexedFieldValue1, IndexedFieldValue2, IndexedId);
            }
        }

        [Fact]
        public void ReadLine_IndexMappingWithoutHeader_PropertyReturnsFieldAtIndex()
        {
            // arrange
            using (var csvReader = new CsvFileReader<IndexedNameLine>(new StringReader(IndexedNameRecordFile)))
            {
                csvReader.RegisterMapper<IndexedNameLineMapper>();

                // act
                var testResult = csvReader.ReadLine() as IndexedNameLine;

                // assert
                testResult.Name.Should().Be(IndexedFieldValue2);
            }
        }

        [Fact]
        public void ReadLine_IndexMappingWithoutHeader_NamesOnlyMappedField()
        {
            // arrange
            using (var csvReader = new CsvFileReader<IndexedNameLine>(new StringReader(IndexedNameRecordFile)))
            {
                csvReader.RegisterMapper<IndexedNameLineMapper>();

                // act
                ICsvFileLine testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().Equal(PositionalFieldName1, NameFieldName, PositionalFieldName3);
                testResult.Values.Should().Equal(IndexedFieldValue1, IndexedFieldValue2, IndexedFieldValue3);
            }
        }

        [Fact]
        public void ReadLine_IndexAttributesWithOtherHeaderNames_PropertiesReturnFieldValues()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<AttributeMappedLine>(new StringReader(RenamedHeaderFile), options))
            {
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as AttributeMappedLine;

                // assert
                testResult.Id.Should().Be(RenamedId);
                testResult.Name.Should().Be(RenamedName);
            }
        }

        [Fact]
        public void ReadLine_IndexAttributesWithOtherHeaderNames_AddsNoFields()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<AttributeMappedLine>(new StringReader(RenamedHeaderFile), options))
            {
                csvReader.ReadLine();

                // act
                ICsvFileLine testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().Equal(PersonIdFieldName, PersonNameFieldName);
                testResult.Values.Should().Equal(RenamedId, RenamedName);
            }
        }

        [Fact]
        public void ReadLine_OptionalIndexMappingPastLastField_SkipsMapping()
        {
            // arrange
            using (var csvReader = new CsvFileReader<PastLastFieldLine>(new StringReader(IndexedNameRecordFile)))
            {
                csvReader.RegisterMapper<PastLastFieldLineMapper>();

                // act
                ICsvFileLine testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().Equal(PositionalFieldName1, PositionalFieldName2, PositionalFieldName3);
                testResult.Values.Should().Equal(IndexedFieldValue1, IndexedFieldValue2, IndexedFieldValue3);
            }
        }

        [Fact]
        public void ReadLine_ConverterOnIndexMappingWithOtherHeaderNames_ConverterReadsFieldAtIndex()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<HexIndexedIdLine>(new StringReader(HexIdHeaderFile), options))
            {
                csvReader.RegisterMapper<HexIndexedIdLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as HexIndexedIdLine;

                // assert
                testResult.Id.Should().Be(HexIndexedId);
            }
        }

        [Fact]
        public void ReadLine_OptionalIndexMappingPastLastFieldWithFieldOfSameName_LeavesFieldUnchanged()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<OptionalPastLastIdLine>(new StringReader(IdNamedHeaderFile), options))
            {
                csvReader.RegisterMapper<OptionalPastLastIdLineMapper>();
                csvReader.ReadLine();

                // act
                ICsvFileLine testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().Equal(IdFieldName, YFieldName, ZFieldName);
                testResult.Values.Should().Equal(IndexedIdValue, IndexedFieldValue2, IndexedFieldValue3);
            }
        }

        [Fact]
        public void ReadLine_RequiredIndexMappingPastLastField_ThrowsCsvFileException()
        {
            // arrange
            using (var csvReader = new CsvFileReader<RequiredPastLastIdLine>(new StringReader(IndexedNameRecordFile)))
            {
                csvReader.RegisterMapper<RequiredPastLastIdLineMapper>();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(MissingIndexedIdMessage);
            }
        }

        [Fact]
        public void ReadLine_RequiredIndexMappingPastLastFieldWithFieldOfSameName_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RequiredPastLastIdLine>(new StringReader(IdNamedHeaderFile), options))
            {
                csvReader.RegisterMapper<RequiredPastLastIdLineMapper>();
                csvReader.ReadLine();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(MissingIndexedIdMessage);
            }
        }

        [Fact]
        public void ReadLine_RequiredNameMappingWithPropertyKeyColumn_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RenamedAttributeLine>(new StringReader(PropertyKeyIdHeaderFile), options))
            {
                csvReader.ReadLine();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(MissingPersonIdMessage);
            }
        }

        [Fact]
        public void ReadLine_MappingAddsFieldBeforeRequiredIndexMappingPastLastField_ReportsRecordFieldCount()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<AddedFieldPastLastLine>(new StringReader(TwoFieldHeaderFile), options))
            {
                csvReader.RegisterMapper<AddedFieldPastLastLineMapper>();
                csvReader.ReadLine();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(MissingIndexedDescriptionMessage);
            }
        }

        [Fact]
        public void ReadLine_GeneratedNameMappingMissingFromHeader_AddsFieldWithGeneratedValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<GeneratedMissingFieldLine>(new StringReader(OneFieldHeaderFile), options))
            {
                csvReader.RegisterMapper<GeneratedMissingFieldLineMapper>();
                csvReader.ReadLine();

                // act
                ICsvFileLine testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().Equal(XFieldName, ExtraFieldName);
                testResult.Values.Should().Equal(IndexedFieldValue1, AddedFieldValue);
            }
        }

        [Fact]
        public void ReadLine_RequiredNameMappingMissingFromHeader_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RenamedAttributeLine>(new StringReader(MissingPersonIdHeaderFile), options))
            {
                csvReader.ReadLine();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(MissingPersonIdMessage);
            }
        }

        [Fact]
        public void ReadLine_ConstantIndexMappingPastLastField_AddsFieldWithConstantValue()
        {
            // arrange
            using (var csvReader = new CsvFileReader<ConstantPastLastIdLine>(new StringReader(IndexedNameRecordFile)))
            {
                csvReader.RegisterMapper<ConstantPastLastIdLineMapper>();

                // act
                ICsvFileLine testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().Equal(PositionalFieldName1, PositionalFieldName2, PositionalFieldName3, IdFieldName);
                testResult.Values.Should().Equal(IndexedFieldValue1, IndexedFieldValue2, IndexedFieldValue3, IndexedId);
            }
        }

        [Fact]
        public void ReadLine_ConstantIndexMappingPastLastFieldWithFieldOfSameName_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<ConstantPastLastIdLine>(new StringReader(IdNamedHeaderFile), options))
            {
                csvReader.RegisterMapper<ConstantPastLastIdLineMapper>();
                csvReader.ReadLine();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(IndexedIdNameTakenMessage);
            }
        }

        [Fact]
        public void ReadLine_IndexMappingPastHeaderWithHeaderFieldName_ThrowsFieldCountMismatch()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<IndexedIdLine>(new StringReader(LongRecordIdHeaderFile), options))
            {
                csvReader.RegisterMapper<IndexedIdLineMapper>();
                csvReader.ReadLine();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<CsvFileException>().WithMessage(LongRecordFieldCountMessage);
            }
        }

        [Fact]
        public void ReadLine_AttributeMappingRenamesFieldWithoutHeader_PropertiesReturnFieldValues()
        {
            // arrange
            using (var csvReader = new CsvFileReader<RenamedAttributeLine>(new StringReader(RenamedRecordFile)))
            {

                // act
                var testResult = csvReader.ReadLine() as RenamedAttributeLine;

                // assert
                testResult.Id.Should().Be(RenamedId);
                testResult.Name.Should().Be(RenamedName);
            }
        }

        [Fact]
        public void ReadLine_MappingRenamesHeaderField_KeepsFieldNamesAsKeys()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RenamedAttributeLine>(new StringReader(RenamedHeaderFile), options))
            {
                csvReader.ReadLine();

                // act
                ICsvFileLine testResult = csvReader.ReadLine();

                // assert
                testResult.Keys.Should().BeEquivalentTo(new[] { PersonIdFieldName, PersonNameFieldName }, config => config.WithStrictOrdering());
            }
        }

        [Fact]
        public void ReadLine_LaterMappingFieldNamedLikeEarlierPropertyKey_KeepsEarlierPropertyValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<LegacyIdLine>(new StringReader(RenamedIdHeaderFile), options))
            {
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as LegacyIdLine;

                // assert
                testResult.Id.Should().Be(RenamedId);
                testResult.LegacyId.Should().BeNull();
            }
        }

        [Fact]
        public void ReadLine_HeaderHasColumnNamedLikePropertyKey_KeepsColumnValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RenamedAttributeLine>(new StringReader(PropertyKeyColumnFile), options))
            {
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as RenamedAttributeLine;

                // assert
                testResult.Name.Should().Be(RenamedName);
                ((ICsvFileLine)testResult)[NameFieldName].Should().Be(CompanyName);
            }
        }

        [Fact]
        public void ReadLine_TwoMappingsWithSameFieldName_BothPropertiesReturnFieldValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<SharedFieldLine>(new StringReader(RenamedIdHeaderFile), options))
            {
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as SharedFieldLine;

                // assert
                testResult.Id.Should().Be(RenamedId);
                testResult.Copy.Should().Be(RenamedId);
            }
        }

        [Fact]
        public void ReadLine_BuilderMapperRenamesFieldOfPropertyWithCsvFieldAttribute_PropertyReturnsFieldValue()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<KeyedBuilderLine>(new StringReader(RenamedIdHeaderFile), options))
            {
                csvReader.RegisterMapper<KeyedBuilderLineMapper>();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as KeyedBuilderLine;

                // assert
                testResult.Id.Should().Be(RenamedId);
            }
        }

        [Fact]
        public void ReadLine_SecondLineWithRenamedHeaderField_PropertiesReturnFieldValues()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using (var csvReader = new CsvFileReader<RenamedAttributeLine>(new StringReader(TwoRenamedRecordsWithHeaderFile), options))
            {
                csvReader.ReadLine();
                csvReader.ReadLine();

                // act
                var testResult = csvReader.ReadLine() as RenamedAttributeLine;

                // assert
                testResult.Id.Should().Be(SecondRenamedId);
                testResult.Name.Should().Be(SecondRenamedName);
            }
        }

        [Fact]
        public void Constructor_FilePathWithNullOptions_ThrowsArgumentNullException()
        {
            // arrange
            // The options are checked before the file is opened, so the file doesn't need to exist.
            const string testFilePath = "unused.csv";

            // act
            Action testAction = () => _ = new CsvFileReader<CsvFileRecordLine>(testFilePath, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(OptionsParameterName);
        }

        [Fact]
        public void ReadLine_StreamConstructor_ReadsStreamInBlocksOfStreamBufferSize()
        {
            // arrange
            using (var testStream = new RecordingStream(NumbersLineBytes))
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(testStream))
            {

                // act
                _ = csvReader.ReadLine();

                // assert
                testStream.ReadSizes.Should().NotBeEmpty().And.OnlyContain(size => size == StreamBufferSize);
            }
        }

        [Fact]
        public void ReadLine_StreamWithEncodingOptionConstructor_ReadsStreamInBlocksOfStreamBufferSize()
        {
            // arrange
            using (var testStream = new RecordingStream(NumbersLineBytes))
            {
                var options = new CsvFileOptions { Encoding = Encoding.GetEncoding(Latin1CodePage) };
                using (var csvReader = new CsvFileReader<CsvFileRecordLine>(testStream, options))
                {

                    // act
                    _ = csvReader.ReadLine();

                    // assert
                    testStream.ReadSizes.Should().NotBeEmpty().And.OnlyContain(size => size == StreamBufferSize);
                }
            }
        }

        [Fact]
        public void ReadLine_StreamWithEncodingOption_ReadsUsingEncoding()
        {
            // arrange
            // "Caf\x00E9" in ISO 8859-1.
            using (var testStream = new MemoryStream(new byte[] { 0x43, 0x61, 0x66, 0xE9 }))
            {
                var options = new CsvFileOptions { Encoding = Encoding.GetEncoding(Latin1CodePage) };
                using (var csvReader = new CsvFileReader<CsvFileRecordLine>(testStream, options))
                {

                    // act
                    var testResult = csvReader.ReadLine();

                    // assert
                    testResult.Should().NotBeNull();
                    testResult[0].Should().Be(CafeLatin1Value);
                }
            }
        }

        [Fact]
        public void ReadLine_CallerSuppliedReader_ReadsThroughCallerSuppliedReader()
        {
            // arrange
            using (var testStream = new RecordingStream(NumbersLineBytes))
            using (var streamReader = new StreamReader(testStream, Encoding.UTF8, true, CallerReaderBufferSize))
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(streamReader))
            {

                // act
                _ = csvReader.ReadLine();

                // assert
                testStream.ReadSizes.Should().NotBeEmpty().And.OnlyContain(size => size == CallerReaderBufferSize);
            }
        }

        [Fact]
        public void ReadLine_StreamWithByteOrderMark_DetectsEncoding()
        {
            // arrange
            // "Caf\x00E9,No\x00EBl" in UTF-16 little endian, after its byte order mark.
            byte[] testFileBytes = new byte[]
            {
                0xFF, 0xFE,
                0x43, 0x00, 0x61, 0x00, 0x66, 0x00, 0xE9, 0x00, 0x2C, 0x00,
                0x4E, 0x00, 0x6F, 0x00, 0xEB, 0x00, 0x6C, 0x00
            };
            using (var testFileStream = new MemoryStream(testFileBytes))
            using (var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream))
            {

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().NotBeNull();
                testResult[0].Should().Be(CafeLatin1Value);
                testResult[1].Should().Be(NoelValue);
            }
        }

        [Fact]
        public void ReadLine_AfterDisposeWithStream_ThrowsObjectDisposedException()
        {
            // arrange
            using (var testFileStream = new MemoryStream(TwoNumberLinesBytes))
            {
                var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream);
                csvReader.ReadLine();
                csvReader.Dispose();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Fact]
        public void ReadLine_AfterDisposeWithCallerSuppliedReader_ThrowsObjectDisposedException()
        {
            // arrange
            using (var stringReader = new StringReader(TwoNumberLinesFile))
            {
                var csvReader = new CsvFileReader<CsvFileRecordLine>(stringReader);
                csvReader.Dispose();

                // act
                Action testAction = () => csvReader.ReadLine();

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Fact]
        public void Dispose_CallerSuppliedReader_LeavesReaderOpen()
        {
            // arrange
            using (var stringReader = new StringReader(TwoNumberLinesFile))
            {
                var csvReader = new CsvFileReader<CsvFileRecordLine>(stringReader);

                // act
                csvReader.Dispose();

                // assert
                stringReader.ReadToEnd().Should().Be(TwoNumberLinesFile);
            }
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // arrange
            using (var testFileStream = new MemoryStream())
            {
                var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream);
                csvReader.Dispose();

                // act
                Action testAction = () => csvReader.Dispose();

                // assert
                testAction.Should().NotThrow();
            }
        }

        [Fact]
        public void Context_AfterDispose_ThrowsObjectDisposedException()
        {
            // arrange
            using (var testFileStream = new MemoryStream())
            {
                var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream);
                csvReader.Dispose();

                // act
                Action testAction = () => _ = csvReader.Context;

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Fact]
        public void RegisterMapper_AfterDispose_ThrowsObjectDisposedException()
        {
            // arrange
            using (var testFileStream = new MemoryStream())
            {
                var csvReader = new CsvFileReader<FirstIndexLine>(testFileStream);
                csvReader.Dispose();

                // act
                Action testAction = () => csvReader.RegisterMapper<FirstIndexLineMapper>();

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Fact]
        public void UnregisterMapper_AfterDispose_ThrowsObjectDisposedException()
        {
            // arrange
            using (var testFileStream = new MemoryStream())
            {
                var csvReader = new CsvFileReader<DisposedReaderLine>(testFileStream);
                csvReader.RegisterMapper<DisposedReaderLineMapper>();
                csvReader.Dispose();

                // act
                Action testAction = () => csvReader.UnregisterMapper<DisposedReaderLineMapper>();

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
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

        private sealed class FirstIndexLine : TestLine
        {
        }

        private sealed class LastIndexLine : TestLine
        {
        }

        private sealed class GapIndexLine : TestLine
        {
        }

        private sealed class LateMappedLine : TestLine
        {
        }

        private sealed class UnregisteredLine : TestLine
        {
        }

        private sealed class DisposedReaderLine : TestLine
        {
        }

        private sealed class ConverterLine : TestLine
        {
        }

        private sealed class ConstantLine : TestLine
        {
        }

        private sealed class GeneratorLine : TestLine
        {
        }

        /// <summary>
        /// A line whose Id <see cref="IndexedIdLineMapper"/> maps by index to the last of three fields.
        /// </summary>
        private sealed class IndexedIdLine : TestLine
        {
        }

        private sealed class IndexedIdLineMapper : CsvFileMapperBase<IndexedIdLine>
        {
            public IndexedIdLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(2)
                    .ConvertUsing(typeof(Int32Converter));
            }
        }

        /// <summary>
        /// A line whose Name <see cref="IndexedNameLineMapper"/> maps by index to the second of three fields.
        /// </summary>
        private sealed class IndexedNameLine : TestLine
        {
        }

        private sealed class IndexedNameLineMapper : CsvFileMapperBase<IndexedNameLine>
        {
            public IndexedNameLineMapper()
            {
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(1);
            }
        }

        /// <summary>
        /// A line whose Description <see cref="PastLastFieldLineMapper"/> maps, as optional, to a position past the last field of a three-field line.
        /// </summary>
        private sealed class PastLastFieldLine : TestLine
        {
        }

        /// <summary>
        /// A line whose Id <see cref="HexIndexedIdLineMapper"/> maps by index to the last of three fields, with a converter that reads the field
        /// by the name its context gives.
        /// </summary>
        private sealed class HexIndexedIdLine : TestLine
        {
        }

        private sealed class HexIndexedIdLineMapper : CsvFileMapperBase<HexIndexedIdLine>
        {
            public HexIndexedIdLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(2)
                    .ConvertUsing(new CsvFieldConvertFromString<HexIndexedIdLine, int>(context =>
                        int.Parse((string)((ICsvFileLine)context.Model)[context.FieldName], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)));
            }
        }

        /// <summary>
        /// A line whose Id <see cref="OptionalPastLastIdLineMapper"/> maps, as optional, to a position past the last field of a three-field line.
        /// </summary>
        private sealed class OptionalPastLastIdLine : TestLine
        {
        }

        private sealed class OptionalPastLastIdLineMapper : CsvFileMapperBase<OptionalPastLastIdLine>
        {
            public OptionalPastLastIdLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(PastLastFieldIndex)
                    .ConvertUsing(typeof(Int32Converter))
                    .IsOptional();
            }
        }

        /// <summary>
        /// A line whose Id <see cref="RequiredPastLastIdLineMapper"/> maps to a position past the last field of a three-field line.
        /// </summary>
        private sealed class RequiredPastLastIdLine : TestLine
        {
        }

        private sealed class RequiredPastLastIdLineMapper : CsvFileMapperBase<RequiredPastLastIdLine>
        {
            public RequiredPastLastIdLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(PastLastFieldIndex)
                    .ConvertUsing(typeof(Int32Converter));
            }
        }

        /// <summary>
        /// A line whose Id <see cref="ConstantPastLastIdLineMapper"/> maps, with a constant value, to a position past the last field of a
        /// three-field line.
        /// </summary>
        private sealed class ConstantPastLastIdLine : TestLine
        {
        }

        private sealed class ConstantPastLastIdLineMapper : CsvFileMapperBase<ConstantPastLastIdLine>
        {
            public ConstantPastLastIdLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(PastLastFieldIndex)
                    .HasConstantValue(IndexedId);
            }
        }

        /// <summary>
        /// A line whose Name <see cref="AddedFieldPastLastLineMapper"/> maps, with a constant value, to a field the line doesn't have, and whose
        /// Description it then maps to a position past the last field of a two-field line.
        /// </summary>
        private sealed class AddedFieldPastLastLine : TestLine
        {
        }

        private sealed class AddedFieldPastLastLineMapper : CsvFileMapperBase<AddedFieldPastLastLine>
        {
            public AddedFieldPastLastLineMapper()
            {
                this.Map().Property(x => x.Name).HasFieldName(ExtraFieldName).HasConstantValue(AddedFieldValue);
                this.Map().Property(x => x.Description).HasFieldIndex(2);
            }
        }

        /// <summary>
        /// A line whose Name <see cref="GeneratedMissingFieldLineMapper"/> maps, with a generated value, to a field the line doesn't have.
        /// </summary>
        private sealed class GeneratedMissingFieldLine : TestLine
        {
        }

        private sealed class GeneratedMissingFieldLineMapper : CsvFileMapperBase<GeneratedMissingFieldLine>
        {
            public GeneratedMissingFieldLineMapper()
            {
                this.Map().Property(x => x.Name).HasFieldName(ExtraFieldName).HasVariableValue(() => AddedFieldValue);
            }
        }

        private sealed class PastLastFieldLineMapper : CsvFileMapperBase<PastLastFieldLine>
        {
            public PastLastFieldLineMapper()
            {
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(PastLastFieldIndex)
                    .IsOptional();
            }
        }

        /// <summary>
        /// A line whose Name <see cref="PositionalNameLineMapper"/> maps to the first field, under the name the reader gives the second field of a
        /// line without a header line.
        /// </summary>
        private sealed class PositionalNameLine : TestLine
        {
        }

        private sealed class PositionalNameLineMapper : CsvFileMapperBase<PositionalNameLine>
        {
            public PositionalNameLineMapper()
            {
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(PositionalFieldName2);
            }
        }

        /// <summary>
        /// A line whose Id <see cref="PastLastPositionalNameLineMapper"/> maps by index to the second field, and whose optional Name it maps, at the
        /// third position, under the name the reader gives the first field of a line without a header line.
        /// </summary>
        private sealed class PastLastPositionalNameLine : TestLine
        {
        }

        private sealed class PastLastPositionalNameLineMapper : CsvFileMapperBase<PastLastPositionalNameLine>
        {
            public PastLastPositionalNameLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(1)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldName(PositionalFieldName1)
                    .IsOptional();
            }
        }

        /// <summary>
        /// A line whose properties are mapped, by attribute, to fields with other names, for <see cref="InheritedAttributeLine"/> to inherit.
        /// </summary>
        private class RenamedAttributeBaseLine : CsvFileRecordLine
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
        /// A line that inherits every mapped property from <see cref="RenamedAttributeBaseLine"/>.
        /// </summary>
        private sealed class InheritedAttributeLine : RenamedAttributeBaseLine
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
        /// A line with an optional mapping whose field name is the property key of the line's other mapping.
        /// </summary>
        private sealed class LegacyIdLine : CsvFileRecordLine
        {
            [CsvFieldMapping(PersonIdFieldName)]
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(IdFieldName, IsOptional = true)]
            public string LegacyId
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        /// <summary>
        /// A line with two properties mapped to the same field.
        /// </summary>
        private sealed class SharedFieldLine : CsvFileRecordLine
        {
            [CsvFieldMapping(PersonIdFieldName)]
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(PersonIdFieldName)]
            public int Copy
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
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

        private sealed class AddedFieldLine : TestLine
        {
            /// <summary>
            /// Names of the fields that the validator of the Description mapping was called for.  A field, not a
            /// property, so that it is not part of the line.
            /// </summary>
            public readonly List<string> ValidatedFieldNames = new List<string>();
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
                this.Map().Property(x => x.Name);
                this.Map().Property(x => x.Description);
            }
        }

        /// <summary>
        /// Maps the fields of an <see cref="InstanceScopedLine"/> in the opposite order to <see cref="InstanceScopedLineMapper"/>.
        /// </summary>
        private sealed class ReversedInstanceScopedLineMapper : CsvFileMapperBase<InstanceScopedLine>
        {
            public ReversedInstanceScopedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(1);
                this.Map()
                    .Property(x => x.Name)
                    .HasFieldIndex(2);
            }
        }

        private sealed class AttributeMappedLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0)]
            [TypeConverter(typeof(Int32Converter))]
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(1)]
            public string Name
            {
                get => GetPropertyValue<string>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class FirstIndexLineMapper : CsvFileMapperBase<FirstIndexLine>
        {
            public FirstIndexLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map().Property(x => x.Name);
                this.Map().Property(x => x.Description);
            }
        }

        private sealed class LastIndexLineMapper : CsvFileMapperBase<LastIndexLine>
        {
            public LastIndexLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map().Property(x => x.Name);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(2);
            }
        }

        private sealed class GapIndexLineMapper : CsvFileMapperBase<GapIndexLine>
        {
            public GapIndexLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map().Property(x => x.Name);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldIndex(3);
            }
        }

        private sealed class LateMappedLineMapper : CsvFileMapperBase<LateMappedLine>
        {
            public LateMappedLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map().Property(x => x.Name);
                this.Map().Property(x => x.Description);
            }
        }

        private sealed class UnregisteredLineMapper : CsvFileMapperBase<UnregisteredLine>
        {
            public UnregisteredLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map().Property(x => x.Name);
                this.Map().Property(x => x.Description);
            }
        }

        private sealed class DisposedReaderLineMapper : CsvFileMapperBase<DisposedReaderLine>
        {
            public DisposedReaderLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
            }
        }

        private sealed class ConverterLineMapper : CsvFileMapperBase<ConverterLine>
        {
            public ConverterLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(new CsvFieldConvertFromString<ConverterLine, int>(context =>
                        int.Parse((string)((ICsvFileLine)context.Model)[context.FieldName], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)));
                this.Map().Property(x => x.Name);
                this.Map().Property(x => x.Description);
            }
        }

        private sealed class ConstantLineMapper : CsvFileMapperBase<ConstantLine>
        {
            public const string ConstantName = "Constant";

            public ConstantLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map().Property(x => x.Name).HasConstantValue(ConstantName);
                this.Map().Property(x => x.Description);
            }
        }

        private sealed class GeneratorLineMapper : CsvFileMapperBase<GeneratorLine>
        {
            public const int GeneratedId = 42;
            public const string GeneratedName = "Generated";

            public GeneratorLineMapper()
            {
                this.Map().Property(x => x.Id).HasVariableValue(() => GeneratedId);
                this.Map().Property(x => x.Name).HasVariableValue(() => GeneratedName);
                this.Map().Property(x => x.Description);
            }
        }

        /// <summary>
        /// Maps Name to a field the line does not have, so storing its value adds the field, which the optional
        /// Description mapping then finds.
        /// </summary>
        private sealed class AddedFieldLineMapper : CsvFileMapperBase<AddedFieldLine>
        {
            public AddedFieldLineMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map().Property(x => x.Name).HasFieldName(ExtraFieldName).HasConstantValue(AddedFieldValue);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldName(ExtraFieldName)
                    .ConvertUsing(typeof(StringConverter))
                    .ValidateUsing(context =>
                    {
                        context.Model.ValidatedFieldNames.Add(context.FieldName);
                        return true;
                    })
                    .IsOptional();
            }
        }

        [Fact]
        public void Constructor_TextReaderWithNullOptions_ThrowsArgumentNullException()
        {
            // arrange
            using (var stringReader = new StringReader(string.Empty))
            {

                // act
                Action testAction = () => _ = new CsvFileReader<CsvFileRecordLine>(stringReader, null);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName(OptionsParameterName);
            }
        }
    }
}
