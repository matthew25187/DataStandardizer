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
        private const string TwoLeaves = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\none\r\n--b\r\nContent-Type: text/html\r\n\r\ntwo\r\n--b--\r\n";
        private const int ShiftJisCodePage = 932;
        private const string MessageWithThreeOctets = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nxxx\r\n--b--\r\n";

        private static ImfOptions NoRequiredFields => new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true };

        [Fact]
        public void WriteWithLineFeeds_ReadWithLineFeeds_GivesTheSameBytes()
        {
            // arrange
            var subject = EntityTrees.ReadWithLineFeeds("From: a@b.test\nDate: Fri, 21 Nov 1997 09:55:06 -0600\n\nbody\n");

            // act
            var testResult = EntityTrees.WriteWithLineFeeds(subject);

            // assert
            testResult.Should().Be("From: a@b.test\nDate: Fri, 21 Nov 1997 09:55:06 -0600\n\nbody\n");
        }

        [Fact]
        public void Describe_MultipartMessage_GivesALineForEachEntity()
        {
            // arrange
            var subject = EntityTrees.Read(TwoLeaves, NoRequiredFields);

            // act
            var testResult = EntityTrees.Describe(subject);

            // assert
            testResult.Should().Equal("/ 0 multipart multipart/mixed", "1 1 leaf text/plain", "2 1 leaf text/html");
        }

        [Fact]
        public void At_KnownPath_GivesThatEntity()
        {
            // arrange
            var subject = EntityTrees.Read(TwoLeaves, NoRequiredFields);

            // act
            var testResult = EntityTrees.At(subject, "2");

            // assert
            testResult.ContentType.Subtype.Should().Be("html");
        }

        [Fact]
        public void At_UnknownPath_Throws()
        {
            // arrange
            var subject = EntityTrees.Read(TwoLeaves, NoRequiredFields);

            // act
            Action testAction = () => EntityTrees.At(subject, "3");

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void DescribeFields_OneField_GivesItsNameAndBody()
        {
            // arrange
            var subject = new ImfEntity { ContentId = ImfMessageId.Parse("<a@b.test>") };

            // act
            var testResult = EntityTrees.DescribeFields(subject);

            // assert
            testResult.Should().Equal("Content-ID:<a@b.test>");
        }

        [Fact]
        public void TypesOf_TwoEntities_LeavesOutTheParameters()
        {
            // arrange
            var subject = new[] { new ImfEntity(), new ImfEntity { ContentType = DataStandardizer.Communication.Mime.MimeMediaType.ApplicationOctetStream } };

            // act
            var testResult = EntityTrees.TypesOf(subject);

            // assert
            testResult.Should().Equal("text/plain", "application/octet-stream");
        }

        [Fact]
        public void MessageWithPartOfLength_SmallLength_HoldsThatManyOctets()
        {
            // arrange
            const int Length = 3;

            // act
            var testResult = EntityTrees.MessageWithPartOfLength(Length);

            // assert
            testResult.Should().Be(MessageWithThreeOctets);
        }

        [Fact]
        public void MessageNestedToDepth_Two_GivesThatTree()
        {
            // arrange
            var subject = EntityTrees.MessageNestedToDepth(2, new ImfOptions { AllowMissingMimeVersion = true });

            // act
            var testResult = EntityTrees.Describe(subject);

            // assert
            testResult.Should().Equal("/ 0 multipart multipart/mixed", "1 1 multipart multipart/mixed", "1.1 2 leaf text/plain");
        }

        [Fact]
        public void TryGetEncoding_UnknownName_IsFalse()
        {
            // arrange
            var subject = new CodePageCharsetResolver();

            // act
            var testResult = subject.TryGetEncoding(DataStandardizer.Communication.Mime.MimeCharset.Parse("x-no-such-charset"), out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryGetEncoding_CodePage_GivesItsEncoding()
        {
            // arrange
            var subject = new CodePageCharsetResolver();

            // act
            var testResult = subject.TryGetEncoding(DataStandardizer.Communication.Mime.MimeCharset.Parse("shift_jis"), out var encoding);

            // assert
            testResult.Should().BeTrue();
            encoding.CodePage.Should().Be(ShiftJisCodePage);
        }

        private const string Text = "A\r\n\xE9";
        private const int BufferLength = 6;

        [Theory]
        [InlineData("", 0)]
        [InlineData("abc", 3)]
        [InlineData("a\r\nbcd\r\nef\r\n", 3)]
        public void LongestLineLength_Output_GivesTheLongestWithoutItsLineBreak(string output, int expected)
        {
            // act
            var testResult = EncodedWordFields.LongestLineLength(output);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ReadDecodedValue_Section_GivesTheFirstFieldDecoded()
        {
            // act
            var testResult = EncodedWordFields.ReadDecodedValue("A: =?utf-8?Q?a?=\r\nB: c\r\n\r\n", new ImfOptions { AllowMissingMimeVersion = true });

            // assert
            testResult.Should().Be("a");
        }

        [Fact]
        public void ReadDecodedValue_NoFields_GivesAnEmptyString()
        {
            // act
            var testResult = EncodedWordFields.ReadDecodedValue("\r\n", new ImfOptions { AllowMissingMimeVersion = true });

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void ReadValue_Section_GivesTheFirstFieldUnfoldedAndNotDecoded()
        {
            // act
            var testResult = EncodedWordFields.ReadValue("A: =?utf-8?Q?a?=\r\n b\r\n");

            // assert
            testResult.Should().Be(" =?utf-8?Q?a?= b");
        }

        [Fact]
        public void ReadValue_NoFields_GivesAnEmptyString()
        {
            // act
            var testResult = EncodedWordFields.ReadValue("\r\n");

            // assert
            testResult.Should().BeEmpty();
        }

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
            using (var reader = new ImfEntityReader(stream, EntityEvents.MimeEntityOptions))
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
            using (var reader = new ImfEntityReader(stream, new ImfOptions { AllowMissingMimeVersion = true }, 1, false))
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
        public void CloseDelimiterAtTheEndWithPadding_PaddingOfTwo_GivesTheSpaces()
        {
            // act
            var testResult = LongMessages.CloseDelimiterAtTheEndWithPadding(2);

            // assert
            testResult.Should().Be("Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b--  ");
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
