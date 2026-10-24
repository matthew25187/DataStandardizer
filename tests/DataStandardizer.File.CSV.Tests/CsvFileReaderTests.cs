using System.ComponentModel;
using System.Text;
using FluentAssertions;

namespace DataStandardizer.File.CSV.Tests
{
    public class CsvFileReaderTests : IDisposable
    {
        private const string CsvLineBreak = "\r\n";

        public void Dispose()
        {
            CacheRepositoryConfiguration.Reset();
        }

        [Fact]
        public void ReadLine_HeaderLineWithQuotedValues_ReturnsHeaderLine()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "\"One\"", "\"Two\"", "\"Three\"");
            AddTestFileLine(testFileLines, null, "1", "2", "3");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileHeaderLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be("One");
            testResult[1].Should().Be("Two");
            testResult[2].Should().Be("Three");
        }

        [Fact]
        public void ReadLine_HeaderLineWithUnquotedValues_ReturnsHeaderLine()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "One", "Two", "Three");
            AddTestFileLine(testFileLines, null, "1", "2", "3");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileHeaderLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be("One");
            testResult[1].Should().Be("Two");
            testResult[2].Should().Be("Three");
        }

        [Fact]
        public void ReadLine_HeaderLineIgnored_ReturnsRecordLine()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "ID", "Name", "Description");
            AddTestFileLine(testFileLines, null, "1", "One", "This is the first");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Ignore };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileRecordLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be("1");
            testResult[1].Should().Be("One");
            testResult[2].Should().Be("This is the first");
        }

        [Fact]
        public void ReadLine_HeaderLineIgnoredAndUsingHeaderDelegate_ReturnsRecordLineWithCustomFieldNames()
        {
            // arrange
            const string fieldName1 = "identifier", fieldName2 = "person_name", fieldName3 = "person_description";
            
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "ID", "Name", "Description");
            AddTestFileLine(testFileLines, null, "1", "One", "This is the first");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var headerHandler = new CsvFileHeader(_ => [fieldName1, fieldName2, fieldName3]);
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Ignore, HeaderHandler = headerHandler };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileRecordLine>();
            testResult!.Count.Should().Be(3);
            testResult[fieldName1].Should().Be("1");
            testResult[fieldName2].Should().Be("One");
            testResult[fieldName3].Should().Be("This is the first");
        }

        [Fact]
        public void ReadLine_RecordLineWithQuotedValues_ReturnsRecordLine()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "\"1\"", "\"2\"", "\"3\"");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileRecordLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be("1");
            testResult[1].Should().Be("2");
            testResult[2].Should().Be("3");
        }

        [Fact]
        public void ReadLine_RecordLineWithUnquotedValues_ReturnsRecordLine()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "1", "2", "3");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileRecordLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be("1");
            testResult[1].Should().Be("2");
            testResult[2].Should().Be("3");
        }

        [Theory]
        [InlineData("\n"), InlineData("\r")]
        public void ReadLine_FileWithCustomLineBreaks_ReturnsRecordLine(string lineBreak)
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "1", "2", "3");
            AddTestFileLine(testFileLines, null, "4", "5", "6");
            var testFile = string.Join(lineBreak, testFileLines);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, TerminatorLineBreak = lineBreak };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            _ = csvReader.ReadLine();
            var testResult = csvReader.ReadLine(); // check the 2nd line so we can be sure it was read correctly after the line break

            // assert
            testResult.Should().BeOfType<CsvFileRecordLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be("4");
            testResult[1].Should().Be("5");
            testResult[2].Should().Be("6");
        }

        [Theory]
        [InlineData("\n"), InlineData("\r")]
        public void ReadLine_CustomEmbeddedLineBreak_ReturnsRecordLineWithLineBreaksReplaced(string lineBreak)
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "One Two", "\"Three\r\nFour\"", "Five Six");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { EmbeddedLineBreak = lineBreak };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeOfType<CsvFileRecordLine>();
            testResult!.Count.Should().Be(3);
            testResult[0].Should().Be("One Two");
            testResult[1].Should().Be($"Three{lineBreak}Four");
            testResult[2].Should().Be("Five Six");
        }

        [Fact]
        public void ReadLine_RecordLineWithFieldCountDifferentFromHeaderLine_CallsInconsistentFieldCountHandler()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "Field 1", "Field 2", "Field 3");
            AddTestFileLine(testFileLines, null, "1", "2");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var handlerCalled = false;
            var handler = new CsvFieldCount<CsvFileRecordLine>(_ => handlerCalled = true);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, InconsistentFieldCountHandler = handler };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            _ = csvReader.ReadLine(); // discard the header line
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeNull();
            handlerCalled.Should().BeTrue();
        }

        [Fact]
        public void ReadLine_RecordLineWithFieldCountDifferentFromHeaderLine_ThrowsCsvFileException()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "Field 1", "Field 2", "Field 3");
            AddTestFileLine(testFileLines, null, "1", "2");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use, InconsistentFieldCountHandler = null };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            _ = csvReader.ReadLine(); // discard the header line
            Action testAction = () => csvReader.ReadLine();

            // assert
            testAction.Should().Throw<CsvFileException>().WithMessage("Found 2 of 3 expected fields.");
        }

        [Fact]
        public void ReadLine_RecordLineWithFieldCountDifferentFromPreviousRecordLine_CallsInconsistentFieldCountHandler()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "1", "2", "3");
            AddTestFileLine(testFileLines, null, "4", "5");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var handlerCalled = false;
            var handler = new CsvFieldCount<CsvFileRecordLine>(_ => handlerCalled = true);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, InconsistentFieldCountHandler = handler };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            _ = csvReader.ReadLine(); // discard the first record line
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().BeNull();
            handlerCalled.Should().BeTrue();
        }

        [Fact]
        public void ReadLine_RecordLineWithFieldCountDifferentFromPreviousRecordLine_ThrowsCsvFileException()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "1", "2", "3");
            AddTestFileLine(testFileLines, null, "4", "5");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, InconsistentFieldCountHandler = null };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            _ = csvReader.ReadLine(); // discard the first line
            Action testAction = () => _ = csvReader.ReadLine();

            // assert
            testAction.Should().Throw<CsvFileException>().WithMessage("Found 2 of 3 expected fields.");
        }

        [Fact]
        public void ReadLine_RecordLineWithUnquotedFieldValueHavingEmbeddedDoubleQuote_CallsBadValueHandler()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "One Two", "Three\"Four", "Five Six");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var handlerCalled = false;
            var handler = new CsvFieldBadValue<CsvFileRecordLine>(_ => handlerCalled = true);
            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, BadValueHandler = handler };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            handlerCalled.Should().BeTrue();
        }

        [Fact]
        public void ReadLine_RecordLineWithUnquotedFieldValueHavingEmbeddedDoubleQuote_ThrowsCsvFileException()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "One Two", "Three\"Four", "Five Six");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None, BadValueHandler = null };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            Action testAction = () => _ = csvReader.ReadLine();

            // assert
            testAction.Should().Throw<CsvFileException>().WithMessage("Encountered invalid value at field index 1.");
        }

        [Fact]
        public void ReadLine_RecordLineWithFirstIndexMapper_ReadsCsvFieldsInOrderByMapping()
        {
            // arrange
            const string recordLineFieldValue2 = "One", recordLineFieldValue3 = "This is the first record";
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines,null,"1",recordLineFieldValue2,recordLineFieldValue3);
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.RegisterMapper<TestLineFirstIndexMapper>();
            
            // act
            var testResult = csvReader.ReadLine() as TestLine;
            
            // assert
            testResult.Should().NotBeNull();
            testResult!.Id.Should().Be(1);
            testResult.Name.Should().Be(recordLineFieldValue2);
            testResult.Description.Should().Be(recordLineFieldValue3);
        }

        [Fact]
        public void ReadLine_RecordLineWithLastIndexMapper_ReadsCsvFieldsInOrderByMapping()
        {
            // arrange
            const string recordLineFieldValue2 = "One", recordLineFieldValue3 = "This is the first record";
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "1", recordLineFieldValue2, recordLineFieldValue3);
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.RegisterMapper<TestLineLastIndexMapper>();

            // act
            var testResult = csvReader.ReadLine() as TestLine;

            // assert
            testResult.Should().NotBeNull();
            testResult!.Id.Should().Be(1);
            testResult.Name.Should().Be(recordLineFieldValue2);
            testResult.Description.Should().Be(recordLineFieldValue3);
        }

        [Fact]
        public void ReadLine_RecordLineWithGapIndexMapper_ReadsCsvFieldsInOrderByMapping()
        {
            // arrange
            const string recordLineFieldValue2 = "One", recordLineFieldValue3 = "This is the first record";
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "1", recordLineFieldValue2, recordLineFieldValue3);
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.RegisterMapper<TestLineGapIndexMapper>();

            // act
            var testResult = csvReader.ReadLine() as TestLine;

            // assert
            testResult.Should().NotBeNull();
            testResult!.Id.Should().Be(1);
            testResult.Name.Should().Be(recordLineFieldValue2);
            testResult.Description.Should().Be(recordLineFieldValue3);
        }

        [Fact]
        public void ReadLine_ConcurrentReadersForSameDeclarativeRecordType_DoNotThrow()
        {
            // arrange
            var testFileBytes = Encoding.Default.GetBytes("1,One");
            const int runCount = 5, readerCount = 32;

            for (var run = 0; run < runCount; run++)
            {
                // The race is probabilistic, so start each run from empty caches and repeat it.
                CacheRepositoryConfiguration.Reset();

                // act
                var act = () => Parallel.For(0, readerCount, _ =>
                {
                    using var testFileStream = new MemoryStream(testFileBytes);
                    using var csvReader = new CsvFileReader<DeclarativeTestLine>(testFileStream);
                    var testResult = csvReader.ReadLine() as DeclarativeTestLine;

                    testResult.Should().NotBeNull();
                    testResult!.Id.Should().Be(1);
                    testResult.Name.Should().Be("One");
                });

                // assert
                act.Should().NotThrow();
            }
        }

        [Fact]
        public void RegisterMapper_SameMapperTwice_RegistersOneMapper()
        {
            // arrange
            using var testFileStream = new MemoryStream();
            using var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.RegisterMapper<TestLineFirstIndexMapper>();

            // act
            var act = () => csvReader.RegisterMapper<TestLineFirstIndexMapper>();

            // assert
            act.Should().NotThrow();
            csvReader.Context.Mappers.Should().ContainSingle()
                .Which.Key.Should().Be(typeof(TestLine));
        }

        [Fact]
        public void ReadLine_MapperRegisteredAfterFirstLine_UsesRegisteredMapper()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "1", "One", "First");
            AddTestFileLine(testFileLines, null, "2", "Two", "Second");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<TestLine>(testFileStream);
            var firstLine = csvReader.ReadLine() as TestLine;

            // act
            csvReader.RegisterMapper<TestLineFirstIndexMapper>();
            var testResult = csvReader.ReadLine() as TestLine;

            // assert
            firstLine.Should().NotBeNull();
            firstLine!.Id.Should().Be(0);
            testResult.Should().NotBeNull();
            testResult!.Id.Should().Be(2);
            testResult.Name.Should().Be("Two");
        }

        [Fact]
        public void ReadLine_MapperUnregisteredAfterFirstLine_StopsMapping()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "1", "One", "First");
            AddTestFileLine(testFileLines, null, "2", "Two", "Second");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.RegisterMapper<TestLineFirstIndexMapper>();
            var firstLine = csvReader.ReadLine() as TestLine;

            // act
            csvReader.UnregisterMapper<TestLineFirstIndexMapper>();
            var testResult = csvReader.ReadLine();

            // assert
            firstLine.Should().NotBeNull();
            firstLine!.Id.Should().Be(1);
            testResult.Should().NotBeNull();
            testResult!.Contains("Field 1").Should().BeTrue();
            testResult["Field 1"].Should().Be("2");
        }

        [Fact]
        public void ReadLine_UnmappedRecordLinesWithoutHeader_NamesFieldsByPosition()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "1", "One", "First");
            AddTestFileLine(testFileLines, null, "2", "Two", "Second");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.None };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            var testResults = new[] { csvReader.ReadLine(), csvReader.ReadLine() };

            // assert
            testResults[0]!.Keys.Cast<string>().Should().Equal("Field 1", "Field 2", "Field 3");
            testResults[1]!.Keys.Cast<string>().Should().Equal("Field 1", "Field 2", "Field 3");
            testResults[1]!["Field 3"].Should().Be("Second");
        }

        [Fact]
        public void ReadLine_UnmappedRecordLineWithHeader_NamesFieldsByHeader()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "ID", "Name", "Description");
            AddTestFileLine(testFileLines, null, "1", "One", "First");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // act
            _ = csvReader.ReadLine(); // discard the header line
            var testResult = csvReader.ReadLine();

            // assert
            testResult!.Keys.Cast<string>().Should().Equal("ID", "Name", "Description");
            testResult["Description"].Should().Be("First");
        }

        [Fact]
        public void ReadLine_MappedRecordLineWithMoreFieldsThanMappings_NamesFieldsByMapperThenPosition()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "1", "One", "First", "Extra");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.RegisterMapper<TestLineFirstIndexMapper>();

            // act
            var testResult = csvReader.ReadLine() as TestLine;

            // assert
            ((ICsvFileLine)testResult!).Keys.Cast<string>().Should().Equal("Id", "Name", "Description", "Field 4");
            testResult.Id.Should().Be(1);
            ((ICsvFileLine)testResult)["Field 4"].Should().Be("Extra");
        }

        [Fact]
        public void ReadLine_SeveralMappedRecordLines_MapsEachLine()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "Id", "Name", "Description");
            AddTestFileLine(testFileLines, null, "1", "One", "First");
            AddTestFileLine(testFileLines, null, "2", "Two", "Second");
            AddTestFileLine(testFileLines, null, "3", "Three", "Third");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<TestLine>(testFileStream, options);
            csvReader.RegisterMapper<TestLineFirstIndexMapper>();

            // act
            _ = csvReader.ReadLine(); // discard the header line
            var testResults = new[] { csvReader.ReadLine() as TestLine, csvReader.ReadLine() as TestLine, csvReader.ReadLine() as TestLine };

            // assert
            testResults.Select(line => line!.Id).Should().Equal(1, 2, 3);
            testResults.Select(line => line!.Name).Should().Equal("One", "Two", "Three");
            testResults.Select(line => line!.Description).Should().Equal("First", "Second", "Third");
        }

        [Fact]
        public void ReadLine_MapperWithFromStringConverter_ConvertsFieldValue()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "1", "One", "First");
            AddTestFileLine(testFileLines, null, "2", "Two", "Second");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.RegisterMapper<TestLineConverterMapper>();

            // act
            var testResults = new[] { csvReader.ReadLine() as TestLine, csvReader.ReadLine() as TestLine };

            // assert
            testResults.Select(line => line!.Id).Should().Equal(10, 20);
            testResults.Select(line => line!.Name).Should().Equal("One", "Two");
        }

        [Fact]
        public void ReadLine_FromStringConverterThrows_PropagatesException()
        {
            // arrange
            var testFileBytes = Encoding.Default.GetBytes("x,One,First");
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.RegisterMapper<TestLineConverterMapper>();

            // act
            var testAction = () => csvReader.ReadLine();

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void ReadLine_MapperWithConstantValue_SetsConstantValue()
        {
            // arrange
            var testFileBytes = Encoding.Default.GetBytes("1,One,First");
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.RegisterMapper<TestLineConstantMapper>();

            // act
            var testResult = csvReader.ReadLine() as TestLine;

            // assert
            testResult!.Id.Should().Be(1);
            testResult.Name.Should().Be("Constant");
            testResult.Description.Should().Be("First");
        }

        [Fact]
        public void ReadLine_MapperWithVariableValueGenerators_SetsGeneratedValues()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "1", "One", "First");
            AddTestFileLine(testFileLines, null, "2", "Two", "Second");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var testFileBytes = Encoding.Default.GetBytes(testFile);
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.RegisterMapper<TestLineGeneratorMapper>();

            // act
            var testResults = new[] { csvReader.ReadLine() as TestLine, csvReader.ReadLine() as TestLine };

            // assert
            testResults.Select(line => line!.Id).Should().Equal(42, 42);
            testResults.Select(line => line!.Name).Should().Equal("Generated", "Generated");
            testResults.Select(line => line!.Description).Should().Equal("First", "Second");
        }

        [Fact]
        public void ReadLine_MappingAddsFieldToLine_LaterOptionalMappingSeesAddedField()
        {
            // arrange
            var validatedFieldNames = new List<string?>();
            TestLineAddedFieldMapper.Validated = context => validatedFieldNames.Add(context.FieldName);

            var testFileBytes = Encoding.Default.GetBytes("1");
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.RegisterMapper<TestLineAddedFieldMapper>();

            // act
            var testResult = csvReader.ReadLine() as TestLine;

            // assert
            testResult!.Id.Should().Be(1);
            ((ICsvFileLine)testResult).Keys.Cast<string>().Should().Equal("Id", "Extra");
            validatedFieldNames.Should().Equal("Extra");
        }

        private class DeclarativeTestLine : CsvFileRecordLine
        {
            [CsvFieldMapping(0)]
            [TypeConverter(typeof(Int32Converter))]
            public int Id
            {
                get => GetPropertyValue<int>();
                set => SetPropertyValue(value);
            }

            [CsvFieldMapping(1)]
            public string? Name
            {
                get => GetPropertyValue<string?>();
                set => SetPropertyValue(value);
            }
        }

        private class TestLine : CsvFileRecordLine
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

        private class TestLineFirstIndexMapper : CsvFileMapperBase<TestLine>
        {
            public TestLineFirstIndexMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map().Property(x => x.Name);
                this.Map().Property(x => x.Description);
            }
        }

        private class TestLineLastIndexMapper : CsvFileMapperBase<TestLine>
        {
            public TestLineLastIndexMapper()
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

        private class TestLineGapIndexMapper : CsvFileMapperBase<TestLine>
        {
            public TestLineGapIndexMapper()
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

        private class TestLineConverterMapper : CsvFileMapperBase<TestLine>
        {
            public TestLineConverterMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(new CsvFieldConvertFromString<TestLine, int>(context => int.Parse((string)((ICsvFileLine)context.Model!)[context.FieldName!]!) * 10));
                this.Map().Property(x => x.Name);
                this.Map().Property(x => x.Description);
            }
        }

        private class TestLineConstantMapper : CsvFileMapperBase<TestLine>
        {
            public TestLineConstantMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map().Property(x => x.Name).HasConstantValue("Constant");
                this.Map().Property(x => x.Description);
            }
        }

        private class TestLineGeneratorMapper : CsvFileMapperBase<TestLine>
        {
            public TestLineGeneratorMapper()
            {
                this.Map().Property(x => x.Id).HasVariableValue(() => 42);
                this.Map().Property(x => x.Name).HasVariableValue(() => "Generated");
                this.Map().Property(x => x.Description);
            }
        }

        /// <summary>
        /// Maps Name to a field the line does not have, so storing its value adds the field, which the optional Description mapping then finds.
        /// </summary>
        private class TestLineAddedFieldMapper : CsvFileMapperBase<TestLine>
        {
            public static Action<CsvFieldContext<TestLine>> Validated = _ => { };

            public TestLineAddedFieldMapper()
            {
                this.Map()
                    .Property(x => x.Id)
                    .HasFieldIndex(0)
                    .ConvertUsing(typeof(Int32Converter));
                this.Map().Property(x => x.Name).HasFieldName("Extra");
                this.Map()
                    .Property(x => x.Description)
                    .HasFieldName("Extra")
                    .ConvertUsing(typeof(StringConverter))
                    .ValidateUsing(context =>
                    {
                        Validated(context);
                        return true;
                    })
                    .IsOptional();
            }
        }

        [Fact]
        public void Dispose_ReaderConstructedFromFilePath_ClosesFile()
        {
            // arrange
            var testFilePath = Path.GetTempFileName();
            try
            {
                System.IO.File.WriteAllText(testFilePath, "1,2,3");

                using (var csvReader = new CsvFileReader<CsvFileRecordLine>(testFilePath))
                {
                    csvReader.ReadLine();
                }

                // act
                var testAction = () =>
                {
                    using var exclusiveStream = new FileStream(testFilePath, FileMode.Open, FileAccess.Read, FileShare.None);
                };

                // assert
                testAction.Should().NotThrow();
            }
            finally
            {
                System.IO.File.Delete(testFilePath);
            }
        }

        [Fact]
        public void Dispose_ReaderConstructedFromFilePathWithOptions_ClosesFile()
        {
            // arrange
            var testFilePath = Path.GetTempFileName();
            try
            {
                System.IO.File.WriteAllText(testFilePath, "1,2,3");
                var options = new CsvFileOptions { Encoding = Encoding.UTF8 };

                using (var csvReader = new CsvFileReader<CsvFileRecordLine>(testFilePath, options))
                {
                    csvReader.ReadLine();
                }

                // act
                var testAction = () => System.IO.File.Delete(testFilePath);

                // assert
                testAction.Should().NotThrow();
            }
            finally
            {
                System.IO.File.Delete(testFilePath);
            }
        }

        [Fact]
        public void ReadLine_FilePathWithEncodingOption_ReadsUsingEncoding()
        {
            // arrange
            var testFilePath = Path.GetTempFileName();
            try
            {
                System.IO.File.WriteAllText(testFilePath, "Café,Crème,Noël", Encoding.Latin1);
                var options = new CsvFileOptions { Encoding = Encoding.Latin1 };

                using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFilePath, options);

                // act
                var testResult = csvReader.ReadLine();

                // assert
                testResult.Should().NotBeNull();
                testResult![0].Should().Be("Café");
                testResult[1].Should().Be("Crème");
                testResult[2].Should().Be("Noël");
            }
            finally
            {
                System.IO.File.Delete(testFilePath);
            }
        }

        [Fact]
        public void Constructor_FilePathWithNullOptions_ThrowsArgumentNullException()
        {
            // arrange
            var testFilePath = Path.GetTempFileName();
            try
            {
                // act
                var testAction = () => new CsvFileReader<CsvFileRecordLine>(testFilePath, null!);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("options");
            }
            finally
            {
                System.IO.File.Delete(testFilePath);
            }
        }

        [Fact]
        public void Constructor_Stream_UsesStreamBufferSize()
        {
            // arrange
            using var testFileStream = new MemoryStream(Encoding.UTF8.GetBytes("1,2,3"));

            // act
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream);

            // assert
            var streamReader = StreamBufferInspector.GetUnderlyingReader(csvReader).Should().BeOfType<StreamReader>().Subject;
            StreamBufferInspector.GetBufferSize(streamReader).Should().Be(CsvFileReader<CsvFileRecordLine>.StreamBufferSize);
            streamReader.CurrentEncoding.Should().Be(Encoding.UTF8);
        }

        [Fact]
        public void Constructor_StreamWithEncodingOption_UsesStreamBufferSize()
        {
            // arrange
            using var testFileStream = new MemoryStream(Encoding.Latin1.GetBytes("1,2,3"));
            var options = new CsvFileOptions { Encoding = Encoding.Latin1 };

            // act
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream, options);

            // assert
            var streamReader = StreamBufferInspector.GetUnderlyingReader(csvReader).Should().BeOfType<StreamReader>().Subject;
            StreamBufferInspector.GetBufferSize(streamReader).Should().Be(CsvFileReader<CsvFileRecordLine>.StreamBufferSize);
            streamReader.CurrentEncoding.Should().Be(Encoding.Latin1);
        }

        [Fact]
        public void Constructor_FilePath_UsesStreamBufferSize()
        {
            // arrange
            var testFilePath = Path.GetTempFileName();
            try
            {
                // act
                using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFilePath);

                // assert
                var streamReader = StreamBufferInspector.GetUnderlyingReader(csvReader).Should().BeOfType<StreamReader>().Subject;
                StreamBufferInspector.GetBufferSize(streamReader).Should().Be(CsvFileReader<CsvFileRecordLine>.StreamBufferSize);
                var fileStream = streamReader.BaseStream.Should().BeOfType<FileStream>().Subject;
                StreamBufferInspector.GetBufferSize(fileStream).Should().Be(CsvFileReader<CsvFileRecordLine>.StreamBufferSize);
            }
            finally
            {
                System.IO.File.Delete(testFilePath);
            }
        }

        [Fact]
        public void Constructor_FilePathWithEncodingOption_UsesStreamBufferSize()
        {
            // arrange
            var testFilePath = Path.GetTempFileName();
            try
            {
                var options = new CsvFileOptions { Encoding = Encoding.Latin1 };

                // act
                using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFilePath, options);

                // assert
                var streamReader = StreamBufferInspector.GetUnderlyingReader(csvReader).Should().BeOfType<StreamReader>().Subject;
                StreamBufferInspector.GetBufferSize(streamReader).Should().Be(CsvFileReader<CsvFileRecordLine>.StreamBufferSize);
                var fileStream = streamReader.BaseStream.Should().BeOfType<FileStream>().Subject;
                StreamBufferInspector.GetBufferSize(fileStream).Should().Be(CsvFileReader<CsvFileRecordLine>.StreamBufferSize);
                streamReader.CurrentEncoding.Should().Be(Encoding.Latin1);
            }
            finally
            {
                System.IO.File.Delete(testFilePath);
            }
        }

        [Fact]
        public void Constructor_TextReader_UsesCallerSuppliedReader()
        {
            // arrange
            using var streamReader = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes("1,2,3")), Encoding.UTF8, true, 256);

            // act
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(streamReader);

            // assert
            StreamBufferInspector.GetUnderlyingReader(csvReader).Should().BeSameAs(streamReader);
            StreamBufferInspector.GetBufferSize(streamReader).Should().Be(256);
        }

        [Fact]
        public void ReadLine_StreamWithByteOrderMark_DetectsEncoding()
        {
            // arrange
            var testFileBytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("Café,Noël")).ToArray();
            using var testFileStream = new MemoryStream(testFileBytes);
            using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream);

            // act
            var testResult = csvReader.ReadLine();

            // assert
            testResult.Should().NotBeNull();
            testResult![0].Should().Be("Café");
            testResult[1].Should().Be("Noël");
        }

        [Fact]
        public void ReadLine_AfterDisposeWithStream_ThrowsObjectDisposedException()
        {
            // arrange
            using var testFileStream = new MemoryStream(Encoding.Default.GetBytes("1,2,3\r\n4,5,6"));
            var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream);
            csvReader.ReadLine();
            csvReader.Dispose();

            // act
            var testAction = () => csvReader.ReadLine();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void ReadLine_AfterDisposeWithCallerSuppliedReader_ThrowsObjectDisposedException()
        {
            // arrange
            using var stringReader = new StringReader("1,2,3\r\n4,5,6");
            var csvReader = new CsvFileReader<CsvFileRecordLine>(stringReader);
            csvReader.Dispose();

            // act
            var testAction = () => csvReader.ReadLine();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
            stringReader.ReadToEnd().Should().Be("1,2,3\r\n4,5,6");
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // arrange
            using var testFileStream = new MemoryStream();
            var csvReader = new CsvFileReader<CsvFileRecordLine>(testFileStream);
            csvReader.Dispose();

            // act
            var testAction = () => csvReader.Dispose();

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
            var testAction = () => csvReader.Context;

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void RegisterMapper_AfterDispose_ThrowsObjectDisposedException()
        {
            // arrange
            using var testFileStream = new MemoryStream();
            var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.Dispose();

            // act
            var testAction = () => csvReader.RegisterMapper<TestLineFirstIndexMapper>();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void UnregisterMapper_AfterDispose_ThrowsObjectDisposedException()
        {
            // arrange
            using var testFileStream = new MemoryStream();
            var csvReader = new CsvFileReader<TestLine>(testFileStream);
            csvReader.RegisterMapper<TestLineFirstIndexMapper>();
            csvReader.Dispose();

            // act
            var testAction = () => csvReader.UnregisterMapper<TestLineFirstIndexMapper>();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        private void AddTestFileLine(List<string> testFileLines, string? fieldDelimiter, params string[] testValues)
        {
            var line = string.Join(fieldDelimiter ?? ",", testValues);
            testFileLines.Add(line);
        }

        private class CacheRepositoryConfiguration : CsvFileIoBase<CsvFileRecordLine>
        {
            public static void Reset()
            {
                ClearMapperCaches();
            }
        }
    }
}