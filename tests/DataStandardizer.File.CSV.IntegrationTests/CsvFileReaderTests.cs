using System.ComponentModel;
using System.Text;
using FluentAssertions;

namespace DataStandardizer.File.CSV.IntegrationTests;

public class CsvFileReaderTests
{
    private const int ReaderCount = 32;
    private const string CafeValue = "Caf\x00E9";

    // "1,2,3" in ASCII.
    private static readonly byte[] NumbersFileContent = [0x31, 0x2C, 0x32, 0x2C, 0x33];

    // The number of characters after the closing quote that made the reader allocate about 765 MiB when it recorded a copy of the value so far for each.
    private const int CharactersAfterClosingQuote = 20_000;

    // Many times more than the reader allocates for that input now, and far less than it allocated then.
    private const long AllocationLimit = 8L * 1024 * 1024;

    // A quoted value, then characters that RFC 4180 §2 doesn't allow after the closing quote.
    public static TheoryData<string> QuotedValuesFollowedByManyCharacters()
    {
        return new TheoryData<string> { "\"a\"" + new string('b', CharactersAfterClosingQuote) };
    }

    [Theory]
    [MemberData(nameof(QuotedValuesFollowedByManyCharacters))]
    public void ReadLine_ManyCharactersAfterClosingQuote_ThrowsCsvFileException(string input)
    {
        // arrange
        using var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(input));

        // act
        Action testAction = () => csvReader.ReadLine();

        // assert
        testAction.Should().Throw<CsvFileException>();
    }

    [Theory]
    [MemberData(nameof(QuotedValuesFollowedByManyCharacters))]
    public void ReadLine_ManyCharactersAfterClosingQuote_DoesNotAllocateMuchMemory(string input)
    {
        // arrange
        Func<Action> prepare = () =>
        {
            var csvReader = new CsvFileReader<CsvFileRecordLine>(new StringReader(input));
            return () => ReaderActions.ReadLineIgnoringFailure(csvReader);
        };

        // act
        var testResult = AllocationMeter.MeasureAllocatedBytes(prepare);

        // assert
        testResult.Should().BeLessThan(AllocationLimit);
    }

    [Fact]
    public void Dispose_ReaderConstructedFromFilePath_ClosesFile()
    {
        // arrange
        using var testFile = TemporaryFile.Create(NumbersFileContent);
        var csvReader = new CsvFileReader<CsvFileRecordLine>(testFile.Path);
        csvReader.ReadLine();
        csvReader.Dispose();

        // act
        Action testAction = () => OpenExclusively(testFile.Path);

        // assert
        testAction.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ReaderConstructedFromFilePathWithOptions_ClosesFile()
    {
        // arrange
        using var testFile = TemporaryFile.Create(NumbersFileContent);
        var options = new CsvFileOptions { Encoding = Encoding.UTF8 };
        var csvReader = new CsvFileReader<CsvFileRecordLine>(testFile.Path, options);
        csvReader.ReadLine();
        csvReader.Dispose();

        // act
        Action testAction = () => OpenExclusively(testFile.Path);

        // assert
        testAction.Should().NotThrow();
    }

    [Fact]
    public void ReadLine_FilePathWithEncodingOption_ReadsUsingEncoding()
    {
        // arrange
        // "Caf\x00E9,Cr\x00E8me,No\x00EBl" in ISO 8859-1.
        using var testFile = TemporaryFile.Create([0x43, 0x61, 0x66, 0xE9, 0x2C, 0x43, 0x72, 0xE8, 0x6D, 0x65, 0x2C, 0x4E, 0x6F, 0xEB, 0x6C]);
        var options = new CsvFileOptions { Encoding = Encoding.Latin1 };
        using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFile.Path, options);

        // act
        var testResult = csvReader.ReadLine();

        // assert
        testResult.Should().NotBeNull();
        testResult![0].Should().Be(CafeValue);
        testResult[1].Should().Be("Cr\x00E8me");
        testResult[2].Should().Be("No\x00EBl");
    }

    [Fact]
    public void ReadLine_FilePathWithoutEncodingOption_ReadsUsingUtf8()
    {
        // arrange
        // "Caf\x00E9" in UTF-8, with no byte order mark.
        using var testFile = TemporaryFile.Create([0x43, 0x61, 0x66, 0xC3, 0xA9]);
        using var csvReader = new CsvFileReader<CsvFileRecordLine>(testFile.Path);

        // act
        var testResult = csvReader.ReadLine();

        // assert
        testResult.Should().NotBeNull();
        testResult![0].Should().Be(CafeValue);
    }

    [Fact]
    public void ReadLine_ConcurrentReadersForSameDeclarativeRecordType_ReadEveryLine()
    {
        // arrange
        // No other test reads ConcurrentDeclarativeLine, so the readers start with no mapper cached for it, and race to
        // build and cache it.
        byte[] testFileBytes = [0x31, 0x2C, 0x4F, 0x6E, 0x65]; // "1,One" in ASCII

        // act
        var testResult = ParallelRunner.Run(ReaderCount, _ => ReadFirstLine(testFileBytes));

        // assert
        testResult.Should().HaveCount(ReaderCount)
            .And.OnlyContain(line => line != null && line.Id == 1 && line.Name == "One");
    }

    private static ConcurrentDeclarativeLine? ReadFirstLine(byte[] testFileBytes)
    {
        using var testFileStream = new MemoryStream(testFileBytes);
        using var csvReader = new CsvFileReader<ConcurrentDeclarativeLine>(testFileStream);

        return csvReader.ReadLine() as ConcurrentDeclarativeLine;
    }

    private static void OpenExclusively(string path)
    {
        using var exclusiveStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
    }

    private sealed class ConcurrentDeclarativeLine : CsvFileRecordLine
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

    [Fact]
    public void Constructor_FilePathWithHandlerOfAnotherType_ThrowsArgumentExceptionBeforeOpeningFile()
    {
        // arrange
        // The file is deleted, so a reader that opened it before checking the options would throw FileNotFoundException instead.
        using var testFile = TemporaryFile.Create();
        System.IO.File.Delete(testFile.Path);
        var options = new CsvFileOptions { BadValueHandler = new CsvFieldBadValue<CsvFileRecordLine>(context => { }) };

        // act
        Action testAction = () => _ = new CsvFileReader<HandlerLine>(testFile.Path, options);

        // assert
        testAction.Should().Throw<ArgumentException>().WithParameterName("options");
    }

    private sealed class HandlerLine : CsvFileRecordLine
    {
    }
}
