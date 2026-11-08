using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.LanguageTag.Tests
{
    /// <summary>
    /// A reader of a string that returns at most one character from each read, however many are asked for, so that a test can read text a character
    /// at a time, with the carriage return and the line feed of each CRLF in different reads.
    /// </summary>
    internal sealed class OneCharacterTextReader : TextReader
    {
        private readonly string _text;
        private int _position;

        public OneCharacterTextReader(string text)
        {
            _text = text;
        }

        public override int Peek()
        {
            return _position < _text.Length ? _text[_position] : -1;
        }

        public override int Read()
        {
            return _position < _text.Length ? _text[_position++] : -1;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (count == 0 || _position == _text.Length) return 0;

            buffer[index] = _text[_position++];
            return 1;
        }

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            return Task.FromResult(Read(buffer, index, count));
        }

#if NET
        public override int Read(Span<char> buffer)
        {
            if (buffer.Length == 0 || _position == _text.Length) return 0;

            buffer[0] = _text[_position++];
            return 1;
        }

        public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<int>(Read(buffer.Span));
        }
#endif
    }
}
