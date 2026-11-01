using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Builds iCalendar test input and readers over it.
    /// </summary>
    internal static class IcalTestData
    {
        /// <summary>
        /// Character block sizes that force refills at many different points, plus the default.
        /// </summary>
        public static readonly int[] BufferSizes = new[] { 1, 2, 3, 4, 5, 7, 11, 16 * 1024 };

        /// <summary>
        /// Gets the ISO-8859-1 encoding, which .NET 5 and later give as <c>Encoding.Latin1</c>.
        /// </summary>
        public static Encoding Latin1 =>
#if NET5_0_OR_GREATER
            Encoding.Latin1;
#else
            Encoding.GetEncoding(28591);
#endif

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
        /// Joins groups of lines into one array.
        /// </summary>
        public static string[] Lines(params string[][] groups)
        {
            return groups.SelectMany(group => group).ToArray();
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

        public static IcalContentLineReader CreateReader(string text, IcalOptions options = null, int bufferSize = 16 * 1024)
        {
            return new IcalContentLineReader(new StringReader(text), options ?? new IcalOptions(), bufferSize);
        }

        public static IcalContentLineReader CreateStreamReader(byte[] data, IcalOptions options = null, int streamBufferSize = 16 * 1024, int bufferSize = 16 * 1024)
        {
            return new IcalContentLineReader(new MemoryStream(data), options ?? new IcalOptions(), streamBufferSize, bufferSize);
        }

        /// <summary>
        /// Gets each item that <see cref="IcalValue.EnumerateTextList"/> gives for a value.
        /// </summary>
        public static List<string> TextListItems(string value, IcalValueFormat format)
        {
            var items = new List<string>();
            foreach (var item in IcalValue.EnumerateTextList(value.AsSpan(), format))
            {
                items.Add(item.ToString());
            }

            return items;
        }

        public static List<IcalContentLine> ReadAll(IcalContentLineReader reader)
        {
            return reader.ReadLines().ToList();
        }
    }
}
