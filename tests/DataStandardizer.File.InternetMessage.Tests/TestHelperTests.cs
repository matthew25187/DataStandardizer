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
        public void ReadOutline_DataInSeveralChunks_MergesThem()
        {
            // arrange
            var stream = new BlockStream(Octets.Of("A:1\r\n\r\nxyz"), BlockStreamMode.SynchronousOnly, 1, 1);
            using (var reader = new ImfEntityReader(stream))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal("start /", "field / A", "type / text/plain", "body / [xyz]", "end /");
            }
        }

        [Fact]
        public void ReadOutline_SameBodyKindInTwoEntities_KeepsThemApart()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader("Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b\r\n\r\ny\r\n--b--"))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal(
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "type 1 text/plain", "body 1 [x]", "end 1",
                    "start 2", "type 2 text/plain", "body 2 [y]", "end 2",
                    "end /");
            }
        }

        [Fact]
        public void ReadEntityRanges_Message_GivesThePathAndBytesOfEachEntity()
        {
            // act
            var testResult = EntityEvents.ReadEntityRanges("A:1\r\n");

            // assert
            testResult.Should().Equal("/ [A:1\r\n]");
        }

        [Fact]
        public void ReadEventRanges_Message_GivesTheKindOffsetAndLengthOfEachEvent()
        {
            // act
            var testResult = EntityEvents.ReadEventRanges("A:1\r\n");

            // assert
            testResult.Should().Equal("EntityStart 0 0", "HeaderField 0 5", "HeaderEnd 0 5", "EntityEnd 0 5");
        }

        [Fact]
        public void ReadBodyChunkRanges_Message_GivesTheOffsetAndLengthOfEachChunk()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader("A:1\r\n\r\nxyz"))
            {
                // act
                var testResult = EntityEvents.ReadBodyChunkRanges(reader);

                // assert
                testResult.Should().Equal("7 3");
            }
        }

        [Fact]
        public void ReadUntil_KindNotInTheMessage_GivesNull()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader("A:1\r\n"))
            {
                // act
                var testResult = EntityEvents.ReadUntil(reader, ImfEntityEventKind.BodyChunk);

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void ReadUntilRejected_MessageWithAnError_SwallowsIt()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(":\r\n"))
            {
                // act
                Action testAction = () => EntityEvents.ReadUntilRejected(reader);

                // assert
                testAction.Should().NotThrow();
            }
        }

        [Fact]
        public void Read_MessageWithTwoParts_JoinsTheirBodies()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader("Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b\r\n\r\ny\r\n--b--"))
            {
                // act
                var testResult = DecodedBodies.Read(reader);

                // assert
                testResult.Should().Be("xy");
            }
        }

        [Fact]
        public void ReadLongestChunk_ChunksOfDifferentLengths_GivesTheLongest()
        {
            // arrange
            var stream = new BlockStream(Octets.Of("\r\nxyz"), BlockStreamMode.SynchronousOnly, 3, 2);
            using (var reader = new ImfEntityReader(stream, new ImfOptions(), 1, false))
            {
                // act
                var testResult = DecodedBodies.ReadLongestChunk(reader);

                // assert
                testResult.Should().Be(2);
            }
        }

        [Fact]
        public void EmbeddedMessages_DepthOfTwo_GivesTwoHeaders()
        {
            // act
            var testResult = LongMessages.EmbeddedMessages(2);

            // assert
            testResult.Should().Be("Content-Type: message/rfc822\r\n\r\nContent-Type: message/rfc822\r\n\r\nA:1\r\n");
        }

        [Fact]
        public void NestedMultiparts_DepthOfTwo_GivesEachBoundaryThePreviousAsAPrefix()
        {
            // act
            var testResult = LongMessages.NestedMultiparts(2);

            // assert
            testResult.Should().Be("Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Type: multipart/mixed; boundary=bb\r\n\r\n--bb\r\n\r\nx\r\n--bb--\r\n--b--");
        }

        [Fact]
        public void Parts_CountOfTwo_GivesTwoParts()
        {
            // act
            var testResult = LongMessages.Parts(2);

            // assert
            testResult.Should().Be("Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b\r\n\r\nx\r\n--b--");
        }

        [Fact]
        public void DelimiterLineWithPadding_PaddingOfTwo_GivesTheSpaces()
        {
            // act
            var testResult = LongMessages.DelimiterLineWithPadding(2);

            // assert
            testResult.Should().Be("Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b  \r\n\r\ny\r\n--b--");
        }

        [Fact]
        public void ManyCandidateLines_CountOfTwo_GivesTwoLines()
        {
            // act
            var testResult = LongMessages.ManyCandidateLines(2);

            // assert
            testResult.Should().Be("Content-Type: multipart/mixed; boundary=aaaa\r\n\r\n--aaaa\r\n\r\n--aaab\r\n--aaab\r\n\r\n--aaaa--");
        }

        [Fact]
        public void Base64Body_ShortLength_GivesOneLine()
        {
            // act
            var testResult = LongMessages.Base64Body(1);

            // assert
            testResult.Should().Be("Content-Transfer-Encoding: base64\r\n\r\nAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\r\n");
        }

        [Fact]
        public void PartWithBody_ShortLength_GivesOneLineInAPart()
        {
            // act
            var testResult = LongMessages.PartWithBody(1);

            // assert
            testResult.Should().Be("Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\r\n\r\n--b--");
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
