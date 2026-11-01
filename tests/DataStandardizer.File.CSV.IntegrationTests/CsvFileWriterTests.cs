using System.ComponentModel;
using System.Text;
using FluentAssertions;

namespace DataStandardizer.File.CSV.IntegrationTests;

public class CsvFileWriterTests
{
    private const int WriterCount = 32;
    private const string FieldName = "0";

    [Fact]
    public void WriteLine_FilePathWithEncodingOption_WritesUsingEncoding()
    {
        // arrange
        using var testFile = TemporaryFile.Create();
        ICsvFileLine testRecordLine = new CsvFileRecordLine();
        testRecordLine.Add(FieldName, "Caf\x00E9");
        var options = new CsvFileOptions { Encoding = Encoding.Latin1 };
        var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testFile.Path, options);

        // act
        WriteLineAndDispose(csvWriter, testRecordLine);

        // assert
        // "Caf\x00E9" and CRLF in ISO 8859-1.
        System.IO.File.ReadAllBytes(testFile.Path).Should().Equal(0x43, 0x61, 0x66, 0xE9, 0x0D, 0x0A);
    }

    [Fact]
    public void WriteLine_FilePathWithoutEncodingOption_WritesUtf8WithoutByteOrderMark()
    {
        // arrange
        using var testFile = TemporaryFile.Create();
        ICsvFileLine testRecordLine = new CsvFileRecordLine();
        testRecordLine.Add(FieldName, "\x00E9");
        var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testFile.Path);

        // act
        WriteLineAndDispose(csvWriter, testRecordLine);

        // assert
        // "\x00E9" and CRLF in UTF-8, with no byte order mark.
        System.IO.File.ReadAllBytes(testFile.Path).Should().Equal(0xC3, 0xA9, 0x0D, 0x0A);
    }

    [Fact]
    public void Dispose_WriterConstructedFromFilePathWithOptions_ClosesFile()
    {
        // arrange
        using var testFile = TemporaryFile.Create();
        ICsvFileLine testRecordLine = new CsvFileRecordLine();
        testRecordLine.Add(FieldName, "1");
        var options = new CsvFileOptions { Encoding = Encoding.UTF8 };
        var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testFile.Path, options);
        csvWriter.WriteLine(testRecordLine);
        csvWriter.Dispose();

        // act
        Action testAction = () => OpenExclusively(testFile.Path);

        // assert
        testAction.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WriterConstructedFromFilePathWithoutEncoding_ClosesFile()
    {
        // arrange
        using var testFile = TemporaryFile.Create();
        var csvWriter = new CsvFileWriter<CsvFileRecordLine>(testFile.Path, new CsvFileOptions());
        csvWriter.Dispose();

        // act
        Action testAction = () => System.IO.File.Delete(testFile.Path);

        // assert
        testAction.Should().NotThrow();
    }

    [Fact]
    public void WriteLine_ConcurrentWritersRegisteringSameMapper_WriteEveryLine()
    {
        // arrange
        // No other test writes ConcurrentLine, so the writers race to register its mapper.
        const string expectedLine = "1,,\r\n";

        // act
        var testResult = ParallelRunner.Run(WriterCount, _ => WriteLine(new ConcurrentLine { Id = 1 }));

        // assert
        testResult.Should().HaveCount(WriterCount).And.OnlyContain(line => line == expectedLine);
    }

    private static string WriteLine(ConcurrentLine testLine)
    {
        var buffer = new StringBuilder();
        using var stringWriter = new StringWriter(buffer);
        using var csvWriter = new CsvFileWriter<ConcurrentLine>(stringWriter);
        csvWriter.RegisterMapper<ConcurrentLineMapper>();
        csvWriter.WriteLine(testLine);

        return buffer.ToString();
    }

    /// <summary>
    /// Writes a line and then disposes the writer, so that everything it wrote reaches its file.
    /// </summary>
    private static void WriteLineAndDispose(CsvFileWriter<CsvFileRecordLine> csvWriter, ICsvFileLine csvLine)
    {
        using (csvWriter)
        {
            csvWriter.WriteLine(csvLine);
        }
    }

    private static void OpenExclusively(string path)
    {
        using var exclusiveStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    private sealed class ConcurrentLine : CsvFileRecordLine
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

    private sealed class ConcurrentLineMapper : CsvFileMapperBase<ConcurrentLine>
    {
        public ConcurrentLineMapper()
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
}
