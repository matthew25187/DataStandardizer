using System.ComponentModel;
using System.Globalization;
using System.Text;
using FluentAssertions;

namespace DataStandardizer.File.CSV.Tests
{
    /// <remarks>
    /// Mappers are registered process-wide, by record-line type.  Each test that registers or unregisters a mapper
    /// reads a record-line type of its own, so that no test sees another test's mapper.
    /// </remarks>
    public class CsvFileReaderTests
    {
        /// <summary>
        /// Size, in bytes, of the buffer of the <see cref="StreamReader"/> that a reader creates over a stream
        /// (16 KiB).
        /// </summary>
        private const int StreamBufferSize = 16384;

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

        // Test files.
        private const string NumbersLine = "1,2,3";
        private const string TwoNumberLinesFile = "1,2,3\r\n4,5,6";
        private const string IgnoredHeaderFile = "ID,Name,Description\r\n1,One,This is the first";
        private const string ShortRecordAfterHeaderFile = "Field 1,Field 2,Field 3\r\n1,2";
        private const string ShortRecordAfterRecordFile = "1,2,3\r\n4,5";
        private const string EmbeddedQuoteLine = "One Two,Three\"Four,Five Six";
        private const string FirstLongRecordLine = "1,One,This is the first";
        private const string TwoRecordsFile = "1,One,First\r\n2,Two,Second";
        private const string ThreeRecordsWithHeaderFile = "Id,Name,Description\r\n1,One,First\r\n2,Two,Second\r\n3,Three,Third";

        // NumbersLine and TwoNumberLinesFile in ASCII.
        private static readonly byte[] NumbersLineBytes = [0x31, 0x2C, 0x32, 0x2C, 0x33];
        private static readonly byte[] TwoNumberLinesBytes = [0x31, 0x2C, 0x32, 0x2C, 0x33, 0x0D, 0x0A, 0x34, 0x2C, 0x35, 0x2C, 0x36];

        [Fact]
        public void ReadLine_HeaderLineWithQuotedValues_ReturnsHeaderLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader("\"One\",\"Two\",\"Three\"\r\n1,2,3"), options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileHeaderLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be(HeaderValue1);
            testResult[1].Should().Be(HeaderValue2);
            testResult[2].Should().Be(HeaderValue3);
        }

        [Fact]
        public void ReadLine_HeaderLineWithUnquotedValues_ReturnsHeaderLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader("One,Two,Three\r\n1,2,3"), options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileHeaderLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be(HeaderValue1);
            testResult[1].Should().Be(HeaderValue2);
            testResult[2].Should().Be(HeaderValue3);
        }

        [Fact]
        public void ReadLine_HeaderLineIgnored_ReturnsRecordLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Ignore };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(IgnoredHeaderFile), options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileRecordLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be(RecordValue1);
            testResult[1].Should().Be(FirstName);
            testResult[2].Should().Be(FirstLongDescription);
        }

        [Fact]
        public void ReadLine_HeaderLineIgnoredAndUsingHeaderDelegate_ReturnsRecordLineWithCustomFieldNames()
        {
            // arrange
            const string fieldName1 = "identifier", fieldName2 = "person_name", fieldName3 = "person_description";

            var headerHandler = new CsvFileHeader(_ => [fieldName1, fieldName2, fieldName3]);
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Ignore, HeaderHandler = headerHandler };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(IgnoredHeaderFile), options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileRecordLine>();
            testResult!.Count.Should().Be(3);
            testResult[fieldName1].Should().Be(RecordValue1);
            testResult[fieldName2].Should().Be(FirstName);
            testResult[fieldName3].Should().Be(FirstLongDescription);
        }

        [Fact]
        public void ReadLine_RecordLineWithQuotedValues_ReturnsRecordLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader("\"1\",\"2\",\"3\""), options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileRecordLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be(RecordValue1);
            testResult[1].Should().Be(RecordValue2);
            testResult[2].Should().Be(RecordValue3);
        }

        [Fact]
        public void ReadLine_RecordLineWithUnquotedValues_ReturnsRecordLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(NumbersLine), options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileRecordLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be(RecordValue1);
            testResult[1].Should().Be(RecordValue2);
            testResult[2].Should().Be(RecordValue3);
        }

        [Theory]
        [InlineData("1,2,3\n4,5,6", "\n"), InlineData("1,2,3\r4,5,6", "\r")]
        public void ReadLine_FileWithCustomLineBreaks_ReturnsRecordLine(string testFile, string lineBreak)
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, TerminatorLineBreak = lineBreak };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(testFile), options);
            _ = csvReader.ReadLine();

            // act
            // Read the second line, to be sure that it was read correctly after the line break.
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileRecordLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be("4");
            testResult[1].Should().Be("5");
            testResult[2].Should().Be("6");
        }

        [Theory]
        [InlineData("\n", "Three\nFour"), InlineData("\r", "Three\rFour")]
        public void ReadLine_CustomEmbeddedLineBreak_ReturnsRecordLineWithLineBreaksReplaced(string lineBreak, string expectedFieldValue)
        {
            // arrange
            var options = new CsvFileOptions { EmbeddedLineBreak = lineBreak };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader("One Two,\"Three\r\nFour\",Five Six"), options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileRecordLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be("One Two");
            testResult[1].Should().Be(expectedFieldValue);
            testResult[2].Should().Be("Five Six");
        }

        [Fact]
        public void ReadLine_RecordLineWithFieldCountDifferentFromHeaderLine_CallsInconsistentFieldCountHandler()
        {
            // arrange
            var handlerCalled = false;
            var handler = new CsvFieldCount<CsvFileRecordLine>(_ => handlerCalled = true);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, InconsistentFieldCountHandler = handler };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(ShortRecordAfterHeaderFile), options);
            _ = csvReader.ReadLine(); // the header line

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeNull();
            handlerCalled.Should().BeTrue();
        }

        [Fact]
        public void ReadLine_RecordLineWithFieldCountDifferentFromHeaderLine_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, InconsistentFieldCountHandler = null };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(ShortRecordAfterHeaderFile), options);
            _ = csvReader.ReadLine(); // the header line

            // act
            Action testAction = () => csvReader.ReadLine();

            // assert
            testAction.Should().Throw<CsvFileException>().WithMessage(FieldCountMessage);
        }

        [Fact]
        public void ReadLine_RecordLineWithFieldCountDifferentFromPreviousRecordLine_CallsInconsistentFieldCountHandler()
        {
            // arrange
            var handlerCalled = false;
            var handler = new CsvFieldCount<CsvFileRecordLine>(_ => handlerCalled = true);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, InconsistentFieldCountHandler = handler };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(ShortRecordAfterRecordFile), options);
            _ = csvReader.ReadLine(); // the first record line

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeNull();
            handlerCalled.Should().BeTrue();
        }

        [Fact]
        public void ReadLine_RecordLineWithFieldCountDifferentFromPreviousRecordLine_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, InconsistentFieldCountHandler = null };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(ShortRecordAfterRecordFile), options);
            _ = csvReader.ReadLine(); // the first record line

            // act
            Action testAction = () => csvReader.ReadLine();

            // assert
            testAction.Should().Throw<CsvFileException>().WithMessage(FieldCountMessage);
        }

        [Fact]
        public void ReadLine_RecordLineWithUnquotedFieldValueHavingEmbeddedDoubleQuote_CallsBadValueHandler()
        {
            // arrange
            var handlerCalled = false;
            var handler = new CsvFieldBadValue<CsvFileRecordLine>(_ => handlerCalled = true);
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, BadValueHandler = handler };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(EmbeddedQuoteLine), options);

            // act
            _ = csvReader.ReadLine();

            // assert
            handlerCalled.Should().BeTrue();
        }

        [Fact]
        public void ReadLine_RecordLineWithUnquotedFieldValueHavingEmbeddedDoubleQuote_ThrowsCsvFileException()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, BadValueHandler = null };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(EmbeddedQuoteLine), options);

            // act
            Action testAction = () => csvReader.ReadLine();

            // assert
            testAction.Should().Throw<CsvFileException>().WithMessage(BadValueMessage);
        }

        [Fact]
        public void ReadLine_RecordLineWithFirstIndexMapper_ReadsCsvFieldsInOrderByMapping()
        {
            // arrange
            using var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(FirstLongRecordLine));
            csvReader.RegisterMapper<FirstIndexLineMapper>();

            // act
            var testResult = csvReader.ReadLine() as FirstIndexLine;

            // assert
            testResult.Should().NotBeNull();
            testResult!.Id.Should().Be(1);
            testResult.Name.Should().Be(FirstName);
            testResult.Description.Should().Be(FirstLongDescription);
        }

        [Fact]
        public void ReadLine_RecordLineWithLastIndexMapper_ReadsCsvFieldsInOrderByMapping()
        {
            // arrange
            using var csvReader = new CsvFileReader<LastIndexLine>(new StringReader(FirstLongRecordLine));
            csvReader.RegisterMapper<LastIndexLineMapper>();

            // act
            var testResult = csvReader.ReadLine() as LastIndexLine;

            // assert
            testResult.Should().NotBeNull();
            testResult!.Id.Should().Be(1);
            testResult.Name.Should().Be(FirstName);
            testResult.Description.Should().Be(FirstLongDescription);
        }

        [Fact]
        public void ReadLine_RecordLineWithGapIndexMapper_ReadsCsvFieldsInOrderByMapping()
        {
            // arrange
            using var csvReader = new CsvFileReader<GapIndexLine>(new StringReader(FirstLongRecordLine));
            csvReader.RegisterMapper<GapIndexLineMapper>();

            // act
            var testResult = csvReader.ReadLine() as GapIndexLine;

            // assert
            testResult.Should().NotBeNull();
            testResult!.Id.Should().Be(1);
            testResult.Name.Should().Be(FirstName);
            testResult.Description.Should().Be(FirstLongDescription);
        }

        [Fact]
        public void RegisterMapper_SameMapperTwice_DoesNotThrow()
        {
            // arrange
            using var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(string.Empty));
            csvReader.RegisterMapper<FirstIndexLineMapper>();

            // act
            Action testAction = () => csvReader.RegisterMapper<FirstIndexLineMapper>();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void RegisterMapper_SameMapperTwice_KeepsOneMapperForRecordLineType()
        {
            // arrange
            using var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(string.Empty));
            csvReader.RegisterMapper<FirstIndexLineMapper>();

            // act
            csvReader.RegisterMapper<FirstIndexLineMapper>();

            // assert
            csvReader.Context.Mappers.Should().ContainKey(typeof(FirstIndexLine))
                .WhoseValue.Should().BeOfType<FirstIndexLineMapper>();
        }

        [Fact]
        public void ReadLine_MapperRegisteredAfterFirstLine_UsesRegisteredMapper()
        {
            // arrange
            using var csvReader = new CsvFileReader<LateMappedLine>(new StringReader(TwoRecordsFile));

            // act
            // The line before the mapper is registered and the line after it.
            var firstLine = csvReader.ReadLine() as LateMappedLine;
            csvReader.RegisterMapper<LateMappedLineMapper>();
            var testResult = csvReader.ReadLine() as LateMappedLine;

            // assert
            firstLine.Should().NotBeNull();
            firstLine!.Id.Should().Be(0);
            testResult.Should().NotBeNull();
            testResult!.Id.Should().Be(2);
            testResult.Name.Should().Be(SecondName);
        }

        [Fact]
        public void ReadLine_MapperUnregisteredAfterFirstLine_StopsMapping()
        {
            // arrange
            using var csvReader = new CsvFileReader<UnregisteredLine>(new StringReader(TwoRecordsFile));
            csvReader.RegisterMapper<UnregisteredLineMapper>();

            // act
            // The line before the mapper is unregistered and the line after it.
            var firstLine = csvReader.ReadLine() as UnregisteredLine;
            csvReader.UnregisterMapper<UnregisteredLineMapper>();
            var testResult = csvReader.ReadLine();

            // assert
            firstLine.Should().NotBeNull();
            firstLine!.Id.Should().Be(1);
            testResult.Should().NotBeNull();
            testResult![PositionalFieldName1].Should().Be(RecordValue2);
        }

        [Fact]
        public void ReadLine_UnmappedRecordLinesWithoutHeader_NamesFieldsByPosition()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(TwoRecordsFile), options);
            _ = csvReader.ReadLine();

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult!.Keys.Should().BeEquivalentTo(new[] { PositionalFieldName1, PositionalFieldName2, PositionalFieldName3 }, options => options.WithStrictOrdering());
            testResult[PositionalFieldName3].Should().Be(SecondDescription);
        }

        [Fact]
        public void ReadLine_UnmappedRecordLineWithHeader_NamesFieldsByHeader()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader("ID,Name,Description\r\n1,One,First"), options);
            _ = csvReader.ReadLine(); // the header line

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult!.Keys.Should().BeEquivalentTo(new[] { "ID", NameFieldName, DescriptionFieldName }, options => options.WithStrictOrdering());
            testResult[DescriptionFieldName].Should().Be(FirstDescription);
        }

        [Fact]
        public void ReadLine_MappedRecordLineWithMoreFieldsThanMappings_NamesFieldsByMapperThenPosition()
        {
            // arrange
            using var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader("1,One,First,Extra"));
            csvReader.RegisterMapper<FirstIndexLineMapper>();

            // act
            var testResult = csvReader.ReadLine() as FirstIndexLine;

            // assert
            ((ICsvFileLine)testResult!).Keys.Should().BeEquivalentTo(new[] { IdFieldName, NameFieldName, DescriptionFieldName, PositionalFieldName4 }, options => options.WithStrictOrdering());
            testResult.Id.Should().Be(1);
            ((ICsvFileLine)testResult)[PositionalFieldName4].Should().Be(ExtraFieldName);
        }

        [Fact]
        public void ReadLine_SeveralMappedRecordLines_MapsFirstLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(ThreeRecordsWithHeaderFile), options);
            csvReader.RegisterMapper<FirstIndexLineMapper>();
            _ = csvReader.ReadLine(); // the header line

            // act
            var testResult = csvReader.ReadLine() as FirstIndexLine;

            // assert
            testResult!.Id.Should().Be(1);
            testResult.Name.Should().Be(FirstName);
            testResult.Description.Should().Be(FirstDescription);
        }

        [Fact]
        public void ReadLine_SeveralMappedRecordLines_MapsSecondLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(ThreeRecordsWithHeaderFile), options);
            csvReader.RegisterMapper<FirstIndexLineMapper>();
            _ = csvReader.ReadLine(); // the header line
            _ = csvReader.ReadLine(); // the first record line

            // act
            var testResult = csvReader.ReadLine() as FirstIndexLine;

            // assert
            testResult!.Id.Should().Be(2);
            testResult.Name.Should().Be(SecondName);
            testResult.Description.Should().Be(SecondDescription);
        }

        [Fact]
        public void ReadLine_SeveralMappedRecordLines_MapsThirdLine()
        {
            // arrange
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };
            using var csvReader = new CsvFileReader<FirstIndexLine>(new StringReader(ThreeRecordsWithHeaderFile), options);
            csvReader.RegisterMapper<FirstIndexLineMapper>();
            _ = csvReader.ReadLine(); // the header line
            _ = csvReader.ReadLine(); // the first record line
            _ = csvReader.ReadLine(); // the second record line

            // act
            var testResult = csvReader.ReadLine() as FirstIndexLine;

            // assert
            testResult!.Id.Should().Be(3);
            testResult.Name.Should().Be(ThirdName);
            testResult.Description.Should().Be(ThirdDescription);
        }

        [Fact]
        public void ReadLine_MapperWithFromStringConverter_ConvertsFieldValue()
        {
            // arrange
            // The converter reads the field as a hexadecimal number, so 1A is 26.
            const int expectedId = 26;
            using var csvReader = new CsvFileReader<ConverterLine>(new StringReader("1A,One,First"));
            csvReader.RegisterMapper<ConverterLineMapper>();

            // act
            var testResult = csvReader.ReadLine() as ConverterLine;

            // assert
            testResult!.Id.Should().Be(expectedId);
            testResult.Name.Should().Be(FirstName);
        }

        [Fact]
        public void ReadLine_FromStringConverterThrows_PropagatesException()
        {
            // arrange
            using var csvReader = new CsvFileReader<ConverterLine>(new StringReader("x,One,First"));
            csvReader.RegisterMapper<ConverterLineMapper>();

            // act
            Action testAction = () => csvReader.ReadLine();

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void ReadLine_MapperWithConstantValue_SetsConstantValue()
        {
            // arrange
            using var csvReader = new CsvFileReader<ConstantLine>(new StringReader("1,One,First"));
            csvReader.RegisterMapper<ConstantLineMapper>();

            // act
            var testResult = csvReader.ReadLine() as ConstantLine;

            // assert
            testResult!.Id.Should().Be(1);
            testResult.Name.Should().Be(ConstantLineMapper.ConstantName);
            testResult.Description.Should().Be(FirstDescription);
        }

        [Fact]
        public void ReadLine_MapperWithVariableValueGenerators_SetsGeneratedValuesOnFirstLine()
        {
            // arrange
            using var csvReader = new CsvFileReader<GeneratorLine>(new StringReader(TwoRecordsFile));
            csvReader.RegisterMapper<GeneratorLineMapper>();

            // act
            var testResult = csvReader.ReadLine() as GeneratorLine;

            // assert
            testResult!.Id.Should().Be(GeneratorLineMapper.GeneratedId);
            testResult.Name.Should().Be(GeneratorLineMapper.GeneratedName);
            testResult.Description.Should().Be(FirstDescription);
        }

        [Fact]
        public void ReadLine_MapperWithVariableValueGenerators_SetsGeneratedValuesOnLaterLine()
        {
            // arrange
            using var csvReader = new CsvFileReader<GeneratorLine>(new StringReader(TwoRecordsFile));
            csvReader.RegisterMapper<GeneratorLineMapper>();
            _ = csvReader.ReadLine();

            // act
            var testResult = csvReader.ReadLine() as GeneratorLine;

            // assert
            testResult!.Id.Should().Be(GeneratorLineMapper.GeneratedId);
            testResult.Name.Should().Be(GeneratorLineMapper.GeneratedName);
            testResult.Description.Should().Be(SecondDescription);
        }

        [Fact]
        public void ReadLine_MappingAddsFieldToLine_LaterOptionalMappingSeesAddedField()
        {
            // arrange
            using var csvReader = new CsvFileReader<AddedFieldLine>(new StringReader("1"));
            csvReader.RegisterMapper<AddedFieldLineMapper>();

            // act
            var testResult = csvReader.ReadLine() as AddedFieldLine;

            // assert
            testResult!.Id.Should().Be(1);
            ((ICsvFileLine)testResult).Keys.Should().BeEquivalentTo(new[] { IdFieldName, ExtraFieldName }, options => options.WithStrictOrdering());
            testResult.ValidatedFieldNames.Should().Equal(ExtraFieldName);
        }

        [Fact]
        public void Constructor_FilePathWithNullOptions_ThrowsArgumentNullException()
        {
            // arrange
            // The options are checked before the file is opened, so the file doesn't need to exist.
            const string testFilePath = "unused.csv";

            // act
            Action testAction = () => _ = new CsvFileReader<CsvFileRecordLine>(testFilePath, null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Fact]
        public void ReadLine_StreamConstructor_ReadsStreamInBlocksOfStreamBufferSize()
        {
            // arrange
            using var testStream = new RecordingStream(NumbersLineBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testStream);

            // act
            _ = csvReader.ReadLine();

            // assert
            testStream.ReadSizes.Should().NotBeEmpty().And.OnlyContain(size => size == StreamBufferSize);
        }

        [Fact]
        public void ReadLine_StreamWithEncodingOptionConstructor_ReadsStreamInBlocksOfStreamBufferSize()
        {
            // arrange
            using var testStream = new RecordingStream(NumbersLineBytes);
            var options = new CsvFileOptions { Encoding = Encoding.Latin1 };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testStream, options);

            // act
            _ = csvReader.ReadLine();

            // assert
            testStream.ReadSizes.Should().NotBeEmpty().And.OnlyContain(size => size == StreamBufferSize);
        }

        [Fact]
        public void ReadLine_StreamWithEncodingOption_ReadsUsingEncoding()
        {
            // arrange
            // "Caf\x00E9" in ISO 8859-1.
            using var testStream = new MemoryStream([0x43, 0x61, 0x66, 0xE9]);
            var options = new CsvFileOptions { Encoding = Encoding.Latin1 };
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testStream, options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().NotBeNull();
            testResult![0].Should().Be(CafeLatin1Value);
        }

        [Fact]
        public void ReadLine_CallerSuppliedReader_ReadsThroughCallerSuppliedReader()
        {
            // arrange
            using var testStream = new RecordingStream(NumbersLineBytes);
            using var streamReader = new StreamReader(testStream, Encoding.UTF8, true, CallerReaderBufferSize);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(streamReader);

            // act
            _ = csvReader.ReadLine();

            // assert
            testStream.ReadSizes.Should().NotBeEmpty().And.OnlyContain(size => size == CallerReaderBufferSize);
        }

        [Fact]
        public void ReadLine_StreamWithByteOrderMark_DetectsEncoding()
        {
            // arrange
            // "Caf\x00E9,No\x00EBl" in UTF-16 little endian, after its byte order mark.
            byte[] testFileBytes =
            [
                0xFF, 0xFE,
                0x43, 0x00, 0x61, 0x00, 0x66, 0x00, 0xE9, 0x00, 0x2C, 0x00,
                0x4E, 0x00, 0x6F, 0x00, 0xEB, 0x00, 0x6C, 0x00
            ];
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().NotBeNull();
            testResult![0].Should().Be(CafeLatin1Value);
            testResult[1].Should().Be(NoelValue);
        }

        [Fact]
        public void ReadLine_AfterDisposeWithStream_ThrowsObjectDisposedException()
        {
            // arrange
            using var testFileStream = new MemoryStream(TwoNumberLinesBytes);
            var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream);
            csvReader.ReadLine();
            csvReader.Dispose();

            // act
            Action testAction = () => csvReader.ReadLine();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void ReadLine_AfterDisposeWithCallerSuppliedReader_ThrowsObjectDisposedException()
        {
            // arrange
            using var stringReader = new StringReader(TwoNumberLinesFile);
            var csvReader = new CsvFileReader<CsvFileRecordLine>(stringReader);
            csvReader.Dispose();

            // act
            Action testAction = () => csvReader.ReadLine();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_CallerSuppliedReader_LeavesReaderOpen()
        {
            // arrange
            using var stringReader = new StringReader(TwoNumberLinesFile);
            var csvReader = new CsvFileReader<CsvFileRecordLine>(stringReader);

            // act
            csvReader.Dispose();

            // assert
            stringReader.ReadToEnd().Should().Be(TwoNumberLinesFile);
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // arrange
            using var testFileStream = new MemoryStream();
            var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream);
            csvReader.Dispose();

            // act
            Action testAction = () => csvReader.Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void Context_AfterDispose_ThrowsObjectDisposedException()
        {
            // arrange
            using var testFileStream = new MemoryStream();
            var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream);
            csvReader.Dispose();

            // act
            Action testAction = () => _ = csvReader.Context;

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void RegisterMapper_AfterDispose_ThrowsObjectDisposedException()
        {
            // arrange
            using var testFileStream = new MemoryStream();
            var csvReader = new CsvFileReader<FirstIndexLine>(testFileStream);
            csvReader.Dispose();

            // act
            Action testAction = () => csvReader.RegisterMapper<FirstIndexLineMapper>();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void UnregisterMapper_AfterDispose_ThrowsObjectDisposedException()
        {
            // arrange
            using var testFileStream = new MemoryStream();
            var csvReader = new CsvFileReader<DisposedReaderLine>(testFileStream);
            csvReader.RegisterMapper<DisposedReaderLineMapper>();
            csvReader.Dispose();

            // act
            Action testAction = () => csvReader.UnregisterMapper<DisposedReaderLineMapper>();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        private abstract class TestLine : CsvFileRecordLine
        {
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            public string? Name
            {
                get => GetPropertyValue<string?>();
                set => SetPropertyValue(value);
            }

            public string? Description
            {
                get => GetPropertyValue<string?>();
                set => SetPropertyValue(value);
            }
        }

        private sealed class FirstIndexLine : TestLine;

        private sealed class LastIndexLine : TestLine;

        private sealed class GapIndexLine : TestLine;

        private sealed class LateMappedLine : TestLine;

        private sealed class UnregisteredLine : TestLine;

        private sealed class DisposedReaderLine : TestLine;

        private sealed class ConverterLine : TestLine;

        private sealed class ConstantLine : TestLine;

        private sealed class GeneratorLine : TestLine;

        private sealed class AddedFieldLine : TestLine
        {
            /// <summary>
            /// Names of the fields that the validator of the Description mapping was called for.  A field, not a
            /// property, so that it is not part of the line.
            /// </summary>
            public readonly List<string?> ValidatedFieldNames = new();
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
                    .HasFieldIndex(10);
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
                        int.Parse((string)((ICsvFileLine)context.Model!)[context.FieldName!]!, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)));
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
                this.Map().Property(x => x.Name).HasFieldName(ExtraFieldName);
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldName(ExtraFieldName)
                    .ConvertUsing(typeof(StringConverter))
                    .ValidateUsing(context =>
                    {
                        context.Model!.ValidatedFieldNames.Add(context.FieldName);
                        return true;
                    })
                    .IsOptional();
            }
        }
    }
}
