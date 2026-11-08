using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    /// <summary>
    /// Tests of the test helper <see cref="OneCharacterTextReader"/>, which the tests of the registry reader rely on to split each CRLF across reads.
    /// </summary>
    public class OneCharacterTextReaderTests
    {
        /// <summary>
        /// A CRLF, whose carriage return and line feed the reader returns from different reads.
        /// </summary>
        private const string CrLfText = "\r\n";

        private const char CarriageReturn = '\r';
        private const char LineFeed = '\n';

        /// <summary>
        /// The number of characters each read asks for, many more than the text has.
        /// </summary>
        private const int ReadLength = 4096;

        /// <summary>
        /// Each way to read characters into a buffer, asking for the whole buffer.
        /// </summary>
        public static TheoryData<string, Func<TextReader, char[], Task<int>>> BufferReads => new TheoryData<string, Func<TextReader, char[], Task<int>>>
        {
            { "Read(char[], int, int)", (reader, buffer) => Task.FromResult(reader.Read(buffer, 0, buffer.Length)) },
            { "ReadAsync(char[], int, int)", (reader, buffer) => reader.ReadAsync(buffer, 0, buffer.Length) },
#if NET
            { "Read(Span<char>)", (reader, buffer) => Task.FromResult(reader.Read(buffer.AsSpan())) },
            { "ReadAsync(Memory<char>)", (reader, buffer) => reader.ReadAsync(buffer.AsMemory()).AsTask() },
#endif
        };

        [Theory]
        [MemberData(nameof(BufferReads), DisableDiscoveryEnumeration = true)]
        public async Task Read_WhenMoreCharactersAreAskedFor_ReturnsOneCharacterPerReadThenNone(string readName, Func<TextReader, char[], Task<int>> read)
        {
            // arrange
            var reader = new OneCharacterTextReader(CrLfText);
            var firstBuffer = new char[ReadLength];
            var secondBuffer = new char[ReadLength];
            var thirdBuffer = new char[ReadLength];

            // act
            var testResult = (First: await read(reader, firstBuffer), Second: await read(reader, secondBuffer), Third: await read(reader, thirdBuffer));

            // assert
            testResult.First.Should().Be(1, "{0} returns one character, however many are asked for", readName);
            firstBuffer[0].Should().Be(CarriageReturn);
            testResult.Second.Should().Be(1, "{0} returns the next character from the next read", readName);
            secondBuffer[0].Should().Be(LineFeed);
            testResult.Third.Should().Be(0, "{0} returns no characters at the end of the text", readName);
        }
    }
}
