using System.Text;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// Builds iCalendar test input and readers over it.
/// </summary>
internal static class IcalTestData
{
    /// <summary>
    /// Character block sizes that force refills at many different points, plus the default.
    /// </summary>
    public static readonly int[] BufferSizes = [1, 2, 3, 4, 5, 7, 11, 16 * 1024];

    public static TheoryData<int> BufferSizeData()
    {
        var data = new TheoryData<int>();
        foreach (var bufferSize in BufferSizes)
        {
            data.Add(bufferSize);
        }

        return data;
    }

    /// <summary>
    /// Joins physical lines, each ending with CRLF.
    /// </summary>
    public static string Crlf(params string[] lines)
    {
        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            builder.Append(line).Append("\r\n");
        }

        return builder.ToString();
    }

    public static IcalContentLineReader CreateReader(string text, IcalOptions? options = null, int bufferSize = 16 * 1024)
    {
        return new IcalContentLineReader(new StringReader(text), options ?? new IcalOptions(), bufferSize);
    }

    public static IcalContentLineReader CreateStreamReader(byte[] data, IcalOptions? options = null, int streamBufferSize = 16 * 1024, int bufferSize = 16 * 1024)
    {
        return new IcalContentLineReader(new MemoryStream(data), options ?? new IcalOptions(), streamBufferSize, bufferSize);
    }

    public static List<IcalContentLine> ReadAll(IcalContentLineReader reader)
    {
        return reader.ReadLines().ToList();
    }
}
