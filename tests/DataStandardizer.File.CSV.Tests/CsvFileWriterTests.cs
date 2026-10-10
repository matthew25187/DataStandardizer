using System.ComponentModel;
using System.Globalization;
using System.Text;
using FluentAssertions;

namespace DataStandardizer.File.CSV.Tests;

public class CsvFileWriterTests : IDisposable
{
    public void Dispose()
    {
        CacheRepositoryConfiguration.Reset();
    }

    [Fact]
    public void WriteLine_HeaderLineAndQuoteHandlingAlways_WritesHeaderLineWithAllValuesQuoted()
    {
        // arrange
        ICsvFileLine testLine = new CsvFileHeaderLine();
        testLine.Add("1", "1");
        testLine.Add("Two", "Two");

        var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Always };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

        const string expectedResult = "\"1\",\"Two\"\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_HeaderLineAndQuoteHandlingRequired_WritesHeaderLineWithRequiredValuesQuoted()
    {
        // arrange
        ICsvFileLine testLine = new CsvFileHeaderLine();
        testLine.Add("1", "One,Two");
        testLine.Add("2", $"Three{Environment.NewLine}Four");
        testLine.Add("3", "Five\"Six");
        testLine.Add("4", "Last");

        var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Required };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

        const string expectedResult = "\"One,Two\",\"Three\r\nFour\",\"Five\"\"Six\",Last\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_HeaderLineAndSuppressTrailingBlankFields_WritesRecordWithoutTrailingBlankFields()
    {
        // arrange
        ICsvFileLine testLine = new CsvFileHeaderLine();
        testLine.Add("1", "One");
        testLine.Add("2", string.Empty);

        var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

        const string expectedResult = "One\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("\n", "One,Two\n"), InlineData("\r", "One,Two\r")]
    public void WriteLine_HeaderLineAndCustomLineBreak_WritesHeaderLineTerminatedByLineBreak(string testLineBreak, string expectedResult)
    {
        // arrange
        ICsvFileLine testLine = new CsvFileHeaderLine();
        testLine.Add("One", "One");
        testLine.Add("Two", "Two");

        var options = new CsvFileOptions { TerminatorLineBreak = testLineBreak };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_HeaderLineAndCustomFieldDelimiter_WritesHeaderLineWithFieldsSeparatedByDelimiter()
    {
        // arrange
        ICsvFileLine testLine = new CsvFileHeaderLine();
        testLine.Add("One", "One");
        testLine.Add("Two", "Two");

        var options = new CsvFileOptions { FieldDelimiterCharacter = ';' };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

        const string expectedResult = "One;Two\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineAndQuoteHandlingAlways_WritesRecordLineWithAllValuesQuoted()
    {
        // arrange
        ICsvFileLine testLine = new CsvFileRecordLine();
        testLine.Add("One", "1");
        testLine.Add("Two", "Two");
        testLine.Add("Three", "Left\"Right");
        testLine.Add("Four", $"Left{Environment.NewLine}Right");
        testLine.Add("Five", "Left,Right");

        var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Always };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

        const string expectedResult = $"\"1\",\"Two\",\"Left\"\"Right\",\"Left\r\nRight\",\"Left,Right\"\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineAndQuoteHandlingAuto_WritesRecordLineWithStringValuesQuoted()
    {
        // arrange
        ICsvFileLine testLine = new CsvFileRecordLine();
        testLine.Add("One", "1");
        testLine.Add("Two", "Two");

        var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Auto };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

        const string expectedResult = "1,\"Two\"\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineAndQuoteHandlingRequired_WritesRecordLineWithRequiredValuesQuoted()
    {
        // arrange
        ICsvFileLine testLine = new CsvFileRecordLine();
        testLine.Add("One", "1");
        testLine.Add("Two", "Two");
        testLine.Add("Three", "Left\"Right");
        testLine.Add("Four", $"Left{Environment.NewLine}Right");
        testLine.Add("Five", "Left,Right");

        var options = new CsvFileOptions { QuoteHandling = CsvFieldQuoteHandling.Required };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

        const string expectedResult = "1,Two,\"Left\"\"Right\",\"Left\r\nRight\",\"Left,Right\"\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineWithDeserializedValueAndNoCulture_WritesRecordWithSerializedValueUsingInvariantCulture()
    {
        // arrange
        const int firstFieldValue = 1;
        ICsvFileLine testLine = new TestLine { Id = firstFieldValue };

        var options = new CsvFileOptions { Culture = null };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<TestLine>(stringWriter, options);

        csvWriter.RegisterMapper<TestLineMapperAllIndexed>();

        var expectedResult = firstFieldValue.ToString(CultureInfo.InvariantCulture) + ",,\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_ConcurrentWritersRegisteringSameMapper_DoNotThrow()
    {
        // arrange
        const int runCount = 5, writerCount = 32;

        for (var run = 0; run < runCount; run++)
        {
            // The race is probabilistic, so start each run from empty caches and repeat it.
            CacheRepositoryConfiguration.Reset();

            // act
            var act = () => Parallel.For(0, writerCount, _ =>
            {
                var buffer = new StringBuilder();
                using var stringWriter = new StringWriter(buffer);
                using var csvWriter = new CsvFileWriter<TestLine>(stringWriter);
                csvWriter.RegisterMapper<TestLineMapperAllIndexed>();

                csvWriter.WriteLine(new TestLine { Id = 1 });

                buffer.ToString().Should().Be("1,,\r\n");
            });

            // assert
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void WriteLine_RecordLineWithDeserializedValueAndCulture_WritesRecordWithSerializedValueUsingSpecifiedCulture()
    {
        // arrange
        const int firstFieldValue = 100;
        ICsvFileLine testLine = new TestLine { Id = firstFieldValue };

        var options = new CsvFileOptions { Culture = new CultureInfo("ru-RU") };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<TestLine>(stringWriter, options);
        csvWriter.RegisterMapper<TestLineMapperAllIndexed>();

        var expectedResult = firstFieldValue.ToString(options.Culture) + ",,\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineWithDeserializedValueNoCultureNoMapper_WritesRecordWithSerializedValueUsingInvariantCulture()
    {
        // arrange
        const int firstFieldValue = 47;
        const string secondFieldValue = "One", thirdFieldValue = "First";
        ICsvFileLine testLine = new TestLine { Id = firstFieldValue, Name = secondFieldValue, Description = thirdFieldValue};

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<TestLine>(stringWriter);

        var expectedResult = string.Join(",", firstFieldValue.ToString(CultureInfo.InvariantCulture), secondFieldValue, thirdFieldValue) + "\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineWithDeserializedValueCultureNoMapper_WritesRecordWithSerializedValueUsingSpecifiedCulture()
    {
        // arrange
        const int firstFieldValue = 49;
        const string secondFieldValue = "One", thirdFieldValue = "First";
        ICsvFileLine testLine = new TestLine { Id = firstFieldValue, Name = secondFieldValue, Description = thirdFieldValue};

        var options = new CsvFileOptions { Culture = new CultureInfo("he-IL") };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<TestLine>(stringWriter);

        var expectedResult = string.Join(",", firstFieldValue.ToString(options.Culture), secondFieldValue, thirdFieldValue) + "\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineAndSuppressTrailingBlankFields_WritesRecordWithoutTrailingBlankFields()
    {
        // arrange
        ICsvFileLine testLine = new CsvFileRecordLine();
        testLine.Add("One", "1");
        testLine.Add("Two", string.Empty);

        var options = new CsvFileOptions { SuppressTrailingBlankFields = true };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

        const string expectedResult = "1\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("\n"), InlineData("\r")]
    public void WriteLine_RecordLineAndCustomLineBreak_WritesRecordLineTerminatedByLineBreak(string testLineBreak)
    {
        // arrange
        ICsvFileLine testLine = new CsvFileRecordLine();
        testLine.Add("One", "1");
        testLine.Add("Two", "Two");

        var options = new CsvFileOptions { TerminatorLineBreak = testLineBreak };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

        string expectedResult = $"1,Two{testLineBreak}";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineAndCustomFieldDelimiter_WritesRecordLineWithFieldsSeparatedByDelimiter()
    {
        // arrange
        ICsvFileLine testLine = new CsvFileRecordLine();
        testLine.Add("One", "1");
        testLine.Add("Two", "Two");
        testLine.Add("Three", "3");

        var options = new CsvFileOptions { FieldDelimiterCharacter = ';' };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

        const string expectedResult = "1;Two;3\r\n";

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("\n", "1,\"Left\nRight\"\r\n"), InlineData("\r", "1,\"Left\rRight\"\r\n")]
    public void WriteLine_RecordLineAndEmbeddedLineBreak_WritesRecordLineWithNormalizedFieldValues(string testLineBreak, string expectedResult)
    {
        // arrange
        ICsvFileLine testLine = new CsvFileRecordLine();
        testLine.Add("One", "1");
        testLine.Add("Two", "Left\r\nRight");

        var options = new CsvFileOptions { EmbeddedLineBreak = testLineBreak };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter, options);

        // act
        csvWriter.WriteLine(testLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_HeaderLineAndRecordLines_WritesCsvFieldsInOrderByHeaderLine()
    {
        // arrange
        const string headerFieldName1 = "ID", headerFieldName2 = "Name", headerFieldName3 = "Description";

        ICsvFileLine testHeaderLine = new CsvFileHeaderLine();
        testHeaderLine.Add(headerFieldName1, headerFieldName1);
        testHeaderLine.Add(headerFieldName2, headerFieldName2);
        testHeaderLine.Add(headerFieldName3, headerFieldName3);

        const string record1FieldValue2 = "One", record1FieldValue3 = "This is the first record";
        ICsvFileLine testRecordLine1 = new CsvFileRecordLine();
        testRecordLine1.Add(headerFieldName2, record1FieldValue2);
        testRecordLine1.Add(headerFieldName3, record1FieldValue3);
        testRecordLine1.Add(headerFieldName1, "1");

        const string record2FieldValue2 = "Two", record2FieldValue3 = "This is the second record";
        ICsvFileLine testRecordLine2 = new CsvFileRecordLine();
        testRecordLine2.Add(headerFieldName3, record2FieldValue3);
        testRecordLine2.Add(headerFieldName1, "2");
        testRecordLine2.Add(headerFieldName2, record2FieldValue2);

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter);

        const string expectedResult = $"{headerFieldName1},{headerFieldName2},{headerFieldName3}\r\n1,{record1FieldValue2},{record1FieldValue3}\r\n2,{record2FieldValue2},{record2FieldValue3}\r\n";

        // act
        csvWriter.WriteLine(testHeaderLine);
        csvWriter.WriteLine(testRecordLine1);
        csvWriter.WriteLine(testRecordLine2);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLines_WritesCsvFieldsInOrderByFirstRecordLine()
    {
        // arrange
        const string fieldName1 = "ID", fieldName2 = "Name", fieldName3 = "Description";

        const string record1FieldValue2 = "One", record1FieldValue3 = "This is the first record";
        ICsvFileLine testRecordLine1 = new CsvFileRecordLine();
        testRecordLine1.Add(fieldName1, "1");
        testRecordLine1.Add(fieldName2, "One");
        testRecordLine1.Add(fieldName3, record1FieldValue3);

        const string record2FieldValue2 = "Two", record2FieldValue3 = "This is the second record";
        ICsvFileLine testRecordLine2 = new CsvFileRecordLine();
        testRecordLine2.Add(fieldName2, record2FieldValue2);
        testRecordLine2.Add(fieldName3, record2FieldValue3);
        testRecordLine2.Add(fieldName1, "2");

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<CsvFileRecordLine>(stringWriter);

        const string expectedResult = $"1,{record1FieldValue2},{record1FieldValue3}\r\n2,{record2FieldValue2},{record2FieldValue3}\r\n";

        // act
        csvWriter.WriteLine(testRecordLine1);
        csvWriter.WriteLine(testRecordLine2);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_HeaderLineAndRecordLineWithMapper_WritesCsvFieldsInOrderByMapping()
    {
        // arrange
        const string fieldName1 = nameof(TestLine.Id), fieldName2 = nameof(TestLine.Name), fieldName3 = nameof(TestLine.Description);
        ICsvFileLine testHeaderLine = new CsvFileHeaderLine();
        testHeaderLine.Add(fieldName1, fieldName1);
        testHeaderLine.Add(fieldName2, fieldName2);
        testHeaderLine.Add(fieldName3, fieldName3);

        const string recordLineFieldValue2 = "One", recordLineFieldValue3 = "This is the first record";
        ICsvFileLine testRecordLine = new TestLine
        {
            Description = recordLineFieldValue3,
            Name = recordLineFieldValue2,
            Id = 1
        };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<TestLine>(stringWriter);
        csvWriter.RegisterMapper<TestLineMapperAllIndexed>();

        const string expectedResult = $"{fieldName1},{fieldName2},{fieldName3}\r\n1,{recordLineFieldValue2},{recordLineFieldValue3}\r\n";

        // act
        csvWriter.WriteLine(testHeaderLine);
        csvWriter.WriteLine(testRecordLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineWithAllIndexedMapper_WritesCsvFieldsInOrderByMapping()
    {
        // arrange
        const string recordLineFieldValue2 = "One", recordLineFieldValue3 = "This is the first record";
        ICsvFileLine testRecordLine = new TestLine { Description = recordLineFieldValue3, Name = recordLineFieldValue2, Id = 1 };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<TestLine>(stringWriter);
        csvWriter.RegisterMapper<TestLineMapperAllIndexed>();

        const string expectedResult = $"1,{recordLineFieldValue2},{recordLineFieldValue3}\r\n";

        // act
        csvWriter.WriteLine(testRecordLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineWithFirstIndexMapper_WritesCsvFieldsInOrderByMapping()
    {
        // arrange
        const string recordLineFieldValue2 = "One", recordLineFieldValue3 = "This is the first record";
        ICsvFileLine testRecordLine = new TestLine { Description = recordLineFieldValue3, Name = recordLineFieldValue2, Id = 1 };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<TestLine>(stringWriter);
        csvWriter.RegisterMapper<TestLineMapperFirstIndexed>();

        const string expectedResult = $"1,{recordLineFieldValue2},{recordLineFieldValue3}\r\n";

        // act
        csvWriter.WriteLine(testRecordLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineWithLastIndexMapper_WritesCsvFieldsInOrderByMapping()
    {
        // arrange
        const string recordLineFieldValue2 = "One", recordLineFieldValue3 = "This is the first record";
        ICsvFileLine testRecordLine = new TestLine { Description = recordLineFieldValue3, Name = recordLineFieldValue2, Id = 1 };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<TestLine>(stringWriter);
        csvWriter.RegisterMapper<TestLineMapperLastIndexed>();

        const string expectedResult = $"1,{recordLineFieldValue2},{recordLineFieldValue3}\r\n";

        // act
        csvWriter.WriteLine(testRecordLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineWithGapInIndexMapping_WritesCsvFieldsInOrderByIndexAndNameMapping()
    {
        // arrange
        const int recordLineFieldValue1 = 1;
        const string recordLineFieldValue2 = "One", recordLineFieldValue3 = "This is the first record";
        ICsvFileLine testRecordLine = new TestLine { Name = recordLineFieldValue2, Description = recordLineFieldValue3, Id = recordLineFieldValue1 };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<TestLine>(stringWriter);
        csvWriter.RegisterMapper<TestLineMapperGapIndexed>();

        string expectedResult = $"{recordLineFieldValue1},{recordLineFieldValue2},{recordLineFieldValue3}\r\n";

        // act
        csvWriter.WriteLine(testRecordLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLineWithIncompleteFieldList_WritesAvailableCsvFieldsInOrderByMapping()
    {
        // arrange
        const string recordLineFieldValue3 = "This is the first record";
        var testRecordLine = new TestLine { Description = recordLineFieldValue3 };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<TestLine>(stringWriter);
        csvWriter.RegisterMapper<TestLineMapperAllIndexed>();

        const string expectedResult = $",,{recordLineFieldValue3}\r\n";

        // act
        csvWriter.WriteLine(testRecordLine);
        var testResult = buffer.ToString();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Fact]
    public void WriteLine_RecordLinesWithInconsistentFieldCountsAndFieldCountDelegate_CallsFieldCountDelegate()
    {
        // arrange
        var recordLine1 = new TestLine { Id = 1, Name = "One" };
        var recordLine2 = new TestLine { Id = 2, Name = "Two", Description = "Second" };

        bool handled = false;

        void HandleInconsistentFieldCount(CsvFieldContext<TestLine> context)
        {
            handled = true;
        }

        var options = new CsvFileOptions { InconsistentFieldCountHandler = new CsvFieldCount<TestLine>(HandleInconsistentFieldCount) };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<TestLine>(stringWriter, options);

        // act
        csvWriter.WriteLine(recordLine1);
        csvWriter.WriteLine(recordLine2);

        // assert
        handled.Should().BeTrue();
    }

    [Fact]
    public void WriteLine_RecordLinesWithInconsistentFieldCountsAndNoFieldCountDelegate_ThrowsCsvFileException()
    {
        // arrange
        var recordLine1 = new TestLine { Id = 1, Name = "One" };
        var recordLine2 = new TestLine { Id = 2, Name = "Two", Description = "Second" };

        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<TestLine>(stringWriter);

        // act
        csvWriter.WriteLine(recordLine1);
        Action testAction = () => csvWriter.WriteLine(recordLine2);

        // assert
        testAction.Should().Throw<CsvFileException>().WithMessage("Expected 2 fields; found 3 fields.");
    }

    [Fact]
    public void WriteLine_FilePathWithEncodingOption_WritesUsingEncoding()
    {
        // arrange
        var testFilePath = Path.GetTempFileName();
        try
        {
            ICsvFileLine testRecordLine = new CsvFileRecordLine();
            testRecordLine.Add("0", "Café");
            var options = new CsvFileOptions { Encoding = Encoding.Latin1 };

            // act
            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testFilePath, options))
            {
                csvWriter.WriteLine(testRecordLine);
            }

            var testResult = System.IO.File.ReadAllBytes(testFilePath);

            // assert
            testResult.Should().Equal(0x43, 0x61, 0x66, 0xE9, 0x0D, 0x0A);
        }
        finally
        {
            System.IO.File.Delete(testFilePath);
        }
    }

    [Fact]
    public void Dispose_WriterConstructedFromFilePathWithOptions_ClosesFile()
    {
        // arrange
        var testFilePath = Path.GetTempFileName();
        try
        {
            var options = new CsvFileOptions { Encoding = Encoding.UTF8 };

            using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testFilePath, options))
            {
                ICsvFileLine testRecordLine = new CsvFileRecordLine();
                testRecordLine.Add("0", "1");
                csvWriter.WriteLine(testRecordLine);
            }

            // act
            var testAction = () =>
            {
                using var exclusiveStream = new FileStream(testFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
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
    public void Dispose_WriterConstructedFromFilePathWithoutEncoding_ClosesFile()
    {
        // arrange
        var testFilePath = Path.GetTempFileName();
        try
        {
            using (new CsvFileWriter<CsvFileRecordLine>(testFilePath, new CsvFileOptions()))
            {
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
    public void Constructor_FilePathWithNullOptions_ThrowsArgumentNullException()
    {
        // arrange
        var testFilePath = Path.GetTempFileName();
        try
        {
            // act
            var testAction = () => new CsvFileWriter<CsvFileRecordLine>(testFilePath, null!);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }
        finally
        {
            System.IO.File.Delete(testFilePath);
        }
    }

    [Fact]
    public void Constructor_StreamWithNullOptions_ThrowsArgumentNullException()
    {
        // arrange
        using var stream = new MemoryStream();

        // act
        var testAction = () => new CsvFileWriter<CsvFileRecordLine>(stream, null!);

        // assert
        testAction.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    private class TestLine : CsvFileRecordLine
    {
        public int Id
        {
            get => GetPropertyValue<int>();
            init => SetPropertyValue(value);
        }

        public string? Name
        {
            get => GetPropertyValue<string?>();
            init => SetPropertyValue(value);
        }

        public string? Description
        {
            get => GetPropertyValue<string?>();
            init => SetPropertyValue(value);
        }
    }

    private class TestLineMapperAllIndexed : CsvFileMapperBase<TestLine>
    {
        public TestLineMapperAllIndexed()
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

    private class TestLineMapperFirstIndexed : CsvFileMapperBase<TestLine>
    {
        public TestLineMapperFirstIndexed()
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

    private class TestLineMapperLastIndexed : CsvFileMapperBase<TestLine>
    {
        public TestLineMapperLastIndexed()
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

    private class TestLineMapperGapIndexed : CsvFileMapperBase<TestLine>
    {
        public TestLineMapperGapIndexed()
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

    // ReSharper disable once ClassNeverInstantiated.Local
    private class CacheRepositoryConfiguration : CsvFileCacheRepositoryBase
    {
        public static void Reset()
        {
            ClearMapperCaches();
        }
    }
}