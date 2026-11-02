using System.Globalization;
using SharpFuzz;

namespace DataStandardizer.File.CSV.Fuzz;

internal static class Program
{
    private static readonly CsvFileHeaderHandling[] HeaderHandlings = [CsvFileHeaderHandling.None, CsvFileHeaderHandling.Use, CsvFileHeaderHandling.Ignore];
    private static readonly CsvFieldQuoteHandling[] QuoteHandlings = [CsvFieldQuoteHandling.Always, CsvFieldQuoteHandling.Auto, CsvFieldQuoteHandling.Required];

    private static void Main()
    {
        Fuzzer.LibFuzzer.Run(Run);
    }

    /// <summary>
    /// Reads the input with every combination of the options that change how a line is read, both as raw fields and mapped to properties.
    /// </summary>
    private static void Run(ReadOnlySpan<byte> input)
    {
        var data = input.ToArray();

        foreach (var headerHandling in HeaderHandlings)
        {
            foreach (var quoteHandling in QuoteHandlings)
            {
                var options = new CsvFileOptions
                {
                    Culture = CultureInfo.InvariantCulture,
                    HeaderHandling = headerHandling,
                    QuoteHandling = quoteHandling
                };

                ReadRawFields(data, options);
                ReadMappedFields(data, options);
            }
        }
    }

    private static void ReadRawFields(byte[] data, CsvFileOptions options)
    {
        using var stream = new MemoryStream(data);
        using var reader = new CsvFileReader<CsvFileRecordLine>(stream, options);

        ReadToEnd(reader);
    }

    private static void ReadMappedFields(byte[] data, CsvFileOptions options)
    {
        using var stream = new MemoryStream(data);
        using var reader = new CsvFileReader<FuzzRecordLine>(stream, options);
        reader.RegisterMapper<FuzzRecordLineMapper>();

        ReadToEnd(reader);
    }

    private static void ReadToEnd<TRecordLine>(CsvFileReader<TRecordLine> reader) where TRecordLine : CsvFileRecordLine, new()
    {
        try
        {
            while (reader.ReadLine() is not null)
            {
            }
        }
        catch (CsvFileException)
        {
            // CsvFileReader.ReadLine documents this for data that it can't read.  Its other documented exceptions are for a disposed reader and an
            // empty TerminatorLineBreak, neither of which happens here, so they count as findings.
        }
    }
}
