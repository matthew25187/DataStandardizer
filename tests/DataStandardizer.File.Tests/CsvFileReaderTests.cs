using System.ComponentModel;
using System.Text;
using DataStandardizer.File.Csv;
using FluentAssertions;

namespace DataStandardizer.File.Tests
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

            var options = new CsvFileOptions { HasHeaderLine = true };

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

            var options = new CsvFileOptions { HasHeaderLine = true };

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
        public void ReadLine_RecordLineWithQuotedValues_ReturnsRecordLine()
        {
            // arrange
            var testFileLines = new List<string>();
            AddTestFileLine(testFileLines, null, "\"1\"", "\"2\"", "\"3\"");
            var testFile = string.Join(CsvLineBreak, testFileLines);

            var options = new CsvFileOptions { HasHeaderLine = false };

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

            var options = new CsvFileOptions { HasHeaderLine = false };

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

            var options = new CsvFileOptions { HasHeaderLine = false, TerminatorLineBreak = lineBreak };

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
            AddTestFileLine(testFileLines, null, "One Two", $"\"Three{Environment.NewLine}Four\"", "Five Six");
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

            var options = new CsvFileOptions { HasHeaderLine = true, InconsistentFieldCountHandler = handler };

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

            var options = new CsvFileOptions { HasHeaderLine = true, InconsistentFieldCountHandler = null };

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

            var options = new CsvFileOptions { HasHeaderLine = false, InconsistentFieldCountHandler = handler };

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

            var options = new CsvFileOptions { HasHeaderLine = false, InconsistentFieldCountHandler = null };

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
            var options = new CsvFileOptions { HasHeaderLine = false, BadValueHandler = handler };

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

            var options = new CsvFileOptions { HasHeaderLine = false, BadValueHandler = null };

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

        private void AddTestFileLine(List<string> testFileLines, string? fieldDelimiter, params string[] testValues)
        {
            var line = string.Join(fieldDelimiter ?? ",", testValues);
            testFileLines.Add(line);
        }

        private class CacheRepositoryConfiguration : CsvFileIoBase<CsvFileRecordLine>
        {
            public static void Reset()
            {
                ImperativeMapperCache.Clear();
                DeclarativeMapperCache.Clear();
            }
        }
    }
}