using System.IO;
using System.Text;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// A string writer that reports no encoding, so a CSV writer writing to it can't tell its character set and writes US-ASCII.
    /// </summary>
    internal sealed class UnencodedStringWriter : StringWriter
    {
        public override Encoding Encoding => null;
    }
}
