using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Builds vCard test input and readers over it.
    /// </summary>
    internal static class VcardTestData
    {
        /// <summary>
        /// The default size of a reader's character block.
        /// </summary>
        public const int DefaultBufferSize = 16 * 1024;

        /// <summary>
        /// Character block sizes that force refills at many different points, plus the default.
        /// </summary>
        public static readonly int[] BufferSizes = { 1, 2, 3, 4, 5, 7, 11, DefaultBufferSize };

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

        public static VcardContentLineReader CreateReader(string text, VcardOptions options = null, int bufferSize = DefaultBufferSize)
        {
            return new VcardContentLineReader(new StringReader(text), options ?? new VcardOptions { MaxLineLength = null }, bufferSize);
        }

        public static VcardContentLineReader CreateStreamReader(byte[] data, VcardOptions options = null, int streamBufferSize = DefaultBufferSize, int bufferSize = DefaultBufferSize)
        {
            return new VcardContentLineReader(new MemoryStream(data), options ?? new VcardOptions { MaxLineLength = null }, streamBufferSize, bufferSize);
        }

        public static List<VcardContentLine> ReadAll(VcardContentLineReader reader)
        {
            return reader.ReadLines().ToList();
        }

        /// <summary>
        /// Reads every line and gives the last.
        /// </summary>
        public static VcardContentLine ReadLast(VcardContentLineReader reader)
        {
            return reader.ReadLines().Last();
        }

        /// <summary>
        /// Reads every line through <see cref="VcardContentLineReader.ReadContentLine"/> and describes each through the view's own accessors.
        /// </summary>
        public static List<string> DescribeViews(VcardContentLineReader reader)
        {
            var lines = new List<string>();
            while (reader.ReadContentLine())
            {
                lines.Add(LineDescription.Of(reader.CurrentLine));
            }

            return lines;
        }

        /// <summary>
        /// Reads every line through <see cref="VcardContentLineReader.ReadContentLineAsync"/> and describes each through the view's own accessors.
        /// </summary>
        public static async Task<List<string>> DescribeViewsAsync(VcardContentLineReader reader)
        {
            var lines = new List<string>();
            while (await reader.ReadContentLineAsync())
            {
                lines.Add(LineDescription.Of(reader.CurrentLine));
            }

            return lines;
        }
    }

    /// <summary>
    /// Describes a line as text that a test can compare exactly: its line numbers, its group, its name, each parameter as <c>;NAME=value|value</c>, a
    /// colon and its value.  <c>1/2 g.N;P=a|b:v</c> is logical line 1, starting on physical line 2.
    /// </summary>
    internal static class LineDescription
    {
        public static string Of(VcardContentLine line)
        {
            var parameters = line.Parameters.Select(parameter => ";" + parameter.Name + "=" + string.Join("|", parameter.Values));
            var group = line.Group is null ? string.Empty : line.Group + ".";
            return line.LineNumber + "/" + line.PhysicalLineNumber + " " + group + line.Name + string.Concat(parameters) + ":" + line.Value;
        }

        public static string Of(VcardContentLineView view)
        {
            var builder = new StringBuilder();
            builder.Append(view.LineNumber).Append('/').Append(view.PhysicalLineNumber).Append(' ');
            if (view.Group.Length > 0)
            {
                builder.Append(view.Group.ToString()).Append('.');
            }

            builder.Append(view.Name.ToString());
            for (var parameterIndex = 0; parameterIndex < view.ParameterCount; parameterIndex++)
            {
                builder.Append(';').Append(view.GetParameterName(parameterIndex).ToString()).Append('=');
                for (var valueIndex = 0; valueIndex < view.GetParameterValueCount(parameterIndex); valueIndex++)
                {
                    builder.Append(valueIndex == 0 ? string.Empty : "|").Append(view.GetParameterValue(parameterIndex, valueIndex).ToString());
                }
            }

            return builder.Append(':').Append(view.RawValue.ToString()).ToString();
        }

        public static List<string> OfAll(IEnumerable<VcardContentLine> lines)
        {
            return lines.Select(Of).ToList();
        }
    }
}
