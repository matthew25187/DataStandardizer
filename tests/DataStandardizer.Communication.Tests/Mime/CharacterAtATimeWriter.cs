using System.IO;
using System.Text;

namespace DataStandardizer.Communication.Tests.Mime
{
    /// <summary>
    /// A writer that passes text to another writer one character at a time.
    /// </summary>
    /// <remarks>
    /// It overrides only <see cref="Write(char)"/>, so <see cref="TextWriter"/> writes a string through it one character at a time. Wrapped
    /// around a <see cref="StreamWriter"/> whose <see cref="StreamWriter.AutoFlush"/> is on, that gives the encoder each character in its own
    /// call, and flushes the encoder only when the writer is disposed.
    /// </remarks>
    internal sealed class CharacterAtATimeWriter : TextWriter
    {
        private readonly TextWriter _writer;

        public CharacterAtATimeWriter(TextWriter writer)
        {
            _writer = writer;
        }

        public override Encoding Encoding => _writer.Encoding;

        public override void Write(char value) => _writer.Write(value);

        protected override void Dispose(bool disposing)
        {
            _writer.Dispose();
            base.Dispose(disposing);
        }
    }
}
