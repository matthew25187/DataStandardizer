using System.IO;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// A reader that is neither a <see cref="StreamReader"/> nor a <see cref="StringReader"/>, so a CSV reader can't tell its encoding and reads
    /// it as US-ASCII.  It passes each call to the reader it wraps.
    /// </summary>
    internal sealed class UnencodedTextReader : TextReader
    {
        private readonly TextReader _reader;

        public UnencodedTextReader(TextReader reader)
        {
            _reader = reader;
        }

        public override int Peek() => _reader.Peek();

        public override int Read() => _reader.Read();

        public override int Read(char[] buffer, int index, int count) => _reader.Read(buffer, index, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
