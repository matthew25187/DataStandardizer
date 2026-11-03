using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of the helpers that hold logic of their own.
    /// </summary>
    public class TestHelperTests
    {
        private const string Text = "A\r\n\xE9";
        private const int BufferLength = 6;

        [Fact]
        public void Of_TextOfOneByteCharacters_GivesThoseBytes()
        {
            // act
            var testResult = Octets.Of(Text);

            // assert
            testResult.Should().Equal(0x41, 0x0D, 0x0A, 0xE9);
        }

        [Fact]
        public void ToText_Bytes_GivesOneCharacterForEach()
        {
            // arrange
            byte[] bytes = { 0x41, 0x0D, 0x0A, 0xE9 };

            // act
            var testResult = Octets.ToText(bytes);

            // assert
            testResult.Should().Be(Text);
        }

        [Fact]
        public void Read_BlockStream_GivesTheFirstReadLengthThenTheReadLength()
        {
            // arrange
            var stream = new BlockStream(new byte[BufferLength], BlockStreamMode.SynchronousOnly, 1, 2);
            var buffer = new byte[BufferLength];

            // act
            var testResult = new[]
            {
                stream.Read(buffer, 0, BufferLength),
                stream.Read(buffer, 0, BufferLength),
                stream.Read(buffer, 0, BufferLength),
                stream.Read(buffer, 0, BufferLength),
                stream.Read(buffer, 0, BufferLength),
            };

            // assert
            testResult.Should().Equal(1, 2, 2, 1, 0);
        }

        [Fact]
        public async Task ReadAsync_AsynchronousBlockStream_GivesTheFirstReadLength()
        {
            // arrange
            var stream = new BlockStream(new byte[BufferLength], BlockStreamMode.AsynchronousOnly, 1);
            var buffer = new byte[BufferLength];

            // act
            var testResult = await stream.ReadAsync(buffer, 0, BufferLength);

            // assert
            testResult.Should().Be(1);
        }

        [Fact]
        public void Read_AsynchronousBlockStream_Throws()
        {
            // arrange
            var stream = new BlockStream(new byte[1], BlockStreamMode.AsynchronousOnly);

            // act
            Action testAction = () => _ = stream.Read(new byte[1], 0, 1);

            // assert
            testAction.Should().Throw<InvalidOperationException>().WithMessage(StreamMessages.SynchronousRead);
        }

        [Fact]
        public void ReadOutcomes_FieldThatFails_GoesOnToTheNext()
        {
            // arrange
            using (var reader = new ImfHeaderReader(new MemoryStream(Octets.Of(":\r\nA:1\r\n"))))
            {
                // act
                var testResult = HeaderSections.ReadOutcomes(reader);

                // assert
                testResult.Should().Equal(HeaderSections.Rejected, "A");
            }
        }
    }
}
