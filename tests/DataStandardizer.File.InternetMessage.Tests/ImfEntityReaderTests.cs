using System;
using System.IO;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfEntityReaderTests
    {
        private const string SinglePartMessage = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nA:1\r\n\r\nx\r\n--b--";
        private const string NoCloseDelimiter = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx";
        private const string MissingBoundary = "Content-Type: multipart/mixed\r\n\r\n";
        private const string InvalidBase64Body = "Content-Transfer-Encoding: base64\r\n\r\nQUI";
        private const string OneField = "A:1\r\n";
        private const string FieldInvalid = ":\r\nA:1\r\n";

        // ref. RFC 2046 §5.1.1: a line of 998 octets is the longest allowed by RFC 5322 §2.1.1, so with the three of "--b" this is the longest padding.
        private const int LongestDelimiterPadding = 995;

        // ref. RFC 5322 §2.1.1: likewise with the five of "--b--".
        private const int LongestCloseDelimiterPadding = 993;

        private const int SmallBlock = 3;

        // Enough levels to overflow the stack, if each level took a stack frame.
        private const int VeryDeep = 20000;
        private const int ManyCandidates = 100000;

        // The size of the block that a reader reads into by default.
        private const int DefaultBlockLength = 16 * 1024;
        private const int LargeBodyLength = 1024 * 1024;

        private const string UnknownCharsetContentType = "Content-Type: text/plain; title*=x-no-such-charset''a\r\n\r\n";

        public static TheoryData<string, string[]> ValidMessages => EntityMessages.Valid;

        public static TheoryData<string, string> InvalidMessages => new TheoryData<string, string>
        {
            // ref. RFC 2046 §5.1.1: a multipart entity needs a boundary, of 1 to 70 of bchars, not ending in a space.
            { MissingBoundary, "" },
            { "Content-Type: multipart/mixed; boundary=\"\"\r\n\r\n", "" },
            { "Content-Type: multipart/mixed; boundary=\"b \"\r\n\r\n", "" },
            { "Content-Type: multipart/mixed; boundary=\"b*\"\r\n\r\n", "" },
            { "Content-Type: multipart/mixed; boundary=12345678901234567890123456789012345678901234567890123456789012345678901\r\n\r\n", "" },

            // ref. RFC 2046 §5.1.1: the close delimiter is required.
            { NoCloseDelimiter, "1" },
            { "Content-Type: multipart/mixed; boundary=b\r\n\r\nnot a delimiter\r\n", "" },
            { "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b", "" },
            { "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b", "1" },
            { "Content-Type: multipart/mixed; boundary=b\r\n", "" },
            { "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nA:1\r\n", "1" },
            { "Content-Type: multipart/mixed; boundary=a\r\n\r\n--a\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--a--", "1.1" },

            // ref. RFC 2046 §5.1.1: at least one part.
            { "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b--", "" },

            // ref. RFC 2046 §5.1.1: body-part := MIME-part-headers [CRLF *OCTET], and the delimiter has a CRLF of its own, so the fields need an empty line.
            { "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nA:1\r\n--b--", "1" },
            { "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n--b--", "1" },

            // ref. RFC 2045 §3: at most one of each.
            { "Content-Type: text/plain\r\nContent-Type: text/plain\r\n\r\n", "" },
            { "Content-Transfer-Encoding: 7bit\r\nContent-Transfer-Encoding: 7bit\r\n\r\n", "" },

            // ref. RFC 2045 §5.1 and §6.1: syntax of the fields.
            { "Content-Type:\r\n\r\n", "" },
            { "Content-Type: text\r\n\r\n", "" },
            { "Content-Type: text/plain (open\r\n\r\n", "" },
            { "Content-Type: text/plain; a=\"open\r\n\r\n", "" },
            { "Content-Type: multipart(c)ed/mixed; boundary=b\r\n\r\n", "" },
            { "Content-Transfer-Encoding:\r\n\r\n", "" },
            { "Content-Transfer-Encoding: a b\r\n\r\n", "" },
            { "Content-Transfer-Encoding: (open\r\n\r\n", "" },

            // ref. RFC 2045 §6.4: a composite entity has only the identity encodings.
            { "Content-Type: multipart/mixed; boundary=b\r\nContent-Transfer-Encoding: base64\r\n\r\n", "" },
            { "Content-Type: multipart/mixed; boundary=b\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\n", "" },
            { "Content-Type: multipart/mixed; boundary=b\r\nContent-Transfer-Encoding: x-other\r\n\r\n", "" },
            { "Content-Type: message/rfc822\r\nContent-Transfer-Encoding: base64\r\n\r\n", "" },
            { "Content-Type: message/rfc822\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\n", "" },
            { "Content-Type: message/x-other\r\nContent-Transfer-Encoding: base64\r\n\r\n", "" },

            // ref. RFC 2046 §5.2.2 and §5.2.3: these must be 7bit.
            { "Content-Type: message/partial; id=x; number=1\r\nContent-Transfer-Encoding: 8bit\r\n\r\n", "" },
            { "Content-Type: message/partial; id=x; number=1\r\nContent-Transfer-Encoding: binary\r\n\r\n", "" },
            { "Content-Type: message/external-body; access-type=x\r\nContent-Transfer-Encoding: 8bit\r\n\r\n", "" },

            // A field of a part, reported with the part's path.
            { "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n: no name\r\n\r\nx\r\n--b--", "1" },

            // A field of an embedded message, reported with the same path as the part that holds it.
            { "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Type: message/rfc822\r\n\r\nno colon\r\n\r\n--b--", "1" },
        };

        public static TheoryData<string, string> Decoded => new TheoryData<string, string>
        {
            // ref. RFC 2045 §6.7 rule 3: trailing white space is deleted, and a soft line break is removed.
            { "Content-Transfer-Encoding: quoted-printable\r\n\r\ncaf=C3=A9 =\r\nau lait  \r\n", "caf\xC3\xA9 au lait\r\n" },
            { "Content-Transfer-Encoding: QUOTED-PRINTABLE\r\n\r\na=3Db", "a=b" },

            // ref. RFC 2045 §6.8: base64 with line breaks in it.
            { "Content-Transfer-Encoding: base64\r\n\r\nSGVsbG8s\r\nIHdvcmxk\r\n", "Hello, world" },
            { "Content-Transfer-Encoding: base64\r\n\r\nQUI=", "AB" },

            // The line break before the delimiter does not reach the decoder, and the codec's state ends with the part.
            {
                "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\nx=3D  \r\n--b\r\nContent-Transfer-Encoding: base64\r\n\r\nQUI=\r\n--b--",
                "x=AB"
            },

            // The other encodings are left as they are.
            { "Content-Transfer-Encoding: 7bit\r\n\r\na=3Db", "a=3Db" },
            { "Content-Transfer-Encoding: 8bit\r\n\r\na=3Db", "a=3Db" },
            { "Content-Transfer-Encoding: binary\r\n\r\na=3Db", "a=3Db" },
            { "\r\na=3Db", "a=3Db" },

            // ref. RFC 6532 §3.7: a message/global that can't be read as a nested entity is decoded, as a body.
            { "Content-Type: message/global\r\nContent-Transfer-Encoding: base64\r\n\r\nQTox", "A:1" },
        };

        // A line one octet too long to be a delimiter line, which stays in the body.  Its padding is made here, as it is too long to write out.
        public static TheoryData<string, string[]> DelimiterLinesTooLong => new TheoryData<string, string[]>
        {
            {
                LongMessages.DelimiterLineWithPadding(LongestDelimiterPadding + 1),
                new[]
                {
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "type 1 text/plain", "body 1 [x\r\n--b" + new string(' ', LongestDelimiterPadding + 1) + "\r\n\r\ny]", "end 1",
                    "end /"
                }
            },
        };

        public static TheoryData<string> InvalidEncodedBodies => new TheoryData<string>
        {
            // ref. RFC 2045 §6.8: ends inside a quantum.
            InvalidBase64Body,
            "Content-Transfer-Encoding: base64\r\n\r\nQUI=A",

            // ref. RFC 2045 §6.7 rule 1: upper-case hexadecimal digits only, and a soft line break needs its line break.
            "Content-Transfer-Encoding: quoted-printable\r\n\r\n=3d",
            "Content-Transfer-Encoding: quoted-printable\r\n\r\n=",
        };

        [Theory]
        [MemberData(nameof(ValidMessages))]
        public void ReadEvent_ValidMessage_GivesTheEvents(string message, string[] expected)
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(message))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal(expected);
            }
        }

        [Theory]
        [MemberData(nameof(ValidMessages))]
        public void ReadEvent_OneByteAtATime_GivesTheSameEvents(string message, string[] expected)
        {
            // arrange
            var stream = new BlockStream(Octets.Of(message), BlockStreamMode.SynchronousOnly, 1, 1);
            using (var reader = new ImfEntityReader(stream, EntityEvents.MimeEntityOptions, 1, false))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal(expected);
            }
        }

        [Theory]
        [ClassData(typeof(SplitMessages))]
        public void ReadEvent_SplitAcrossRefills_GivesTheSameEvents(int firstReadLength, int bufferSize)
        {
            // arrange
            var stream = new BlockStream(SplitMessages.Message, BlockStreamMode.SynchronousOnly, firstReadLength);
            using (var reader = new ImfEntityReader(stream, EntityEvents.AnyOctetsOptions, bufferSize, false))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal(SplitMessages.ExpectedEvents);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidMessages))]
        public void ReadEvent_InvalidMessage_ThrowsWithTheEntityPath(string message, string expectedPath)
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(message))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>()
                    .Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be(expectedPath);
            }
        }

        [Fact]
        public void ReadEvent_AfterAnError_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(MissingBoundary))
            {
                EntityEvents.ReadUntilRejected(reader);

                // act
                Action testAction = () => reader.ReadEvent();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadEvent_AfterAHeaderFieldError_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(FieldInvalid))
            {
                EntityEvents.ReadUntilRejected(reader);

                // act
                Action testAction = () => reader.ReadEvent();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadEvent_AtTheEndOfTheMessage_KeepsReturningNull()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(OneField))
            {
                EntityEvents.ReadToEnd(reader);

                // act
                var testResult = reader.ReadEvent();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void ReadEvent_NoCloseDelimiterAtTheEndOfTheData_AcceptedWithTheOption()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(NoCloseDelimiter, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, AllowMissingBoundaryTerminator = true }))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal("start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "end /");
            }
        }

        [Fact]
        public void ReadEvent_NoCloseDelimiterBeforeAnOuterDelimiter_AcceptedWithTheOption()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=a\r\n\r\n--a\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--a\r\n\r\ny\r\n--a--";
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, AllowMissingBoundaryTerminator = true }))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal(
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "field 1 Content-Type", "type 1 multipart/mixed",
                    "start 1.1", "type 1.1 text/plain", "body 1.1 [x]", "end 1.1",
                    "end 1",
                    "start 2", "type 2 text/plain", "body 2 [y]", "end 2",
                    "end /");
            }
        }

        [Fact]
        public void ReadEvent_NoDelimiterAtAll_WithTheOptionIsAllPreamble()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=b\r\n\r\ntext\r\n";
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, AllowMissingBoundaryTerminator = true }))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal("start /", "field / Content-Type", "type / multipart/mixed", "preamble / [text\r\n]", "end /");
            }
        }

        [Fact]
        public void ReadEvent_PartHeaderSectionEndsWithTheData_WithTheOptionHasNoBody()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nA:1\r\n";
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, AllowMissingBoundaryTerminator = true }))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal("start /", "field / Content-Type", "type / multipart/mixed", "start 1", "field 1 A", "type 1 text/plain", "end 1", "end /");
            }
        }

        [Fact]
        public void ReadEvent_PartFieldCutOffByTheEndOfTheData_RejectedEvenWithTheOption()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nA:1";
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, AllowMissingBoundaryTerminator = true }))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadEvent_CloseDelimiterWithNoPart_RejectedEvenWithTheOption()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b--";
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, AllowMissingBoundaryTerminator = true }))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadEvent_BareLineFeedBeforeABoundary_IsNotADelimiter()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\n--b\r\ny\r\n--b--";
            using (var reader = EntityEvents.CreateReader(message, EntityEvents.AnyOctetsOptions))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal("start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x\n--b\r\ny]", "end 1", "end /");
            }
        }

        [Fact]
        public void ReadEvent_BareLineFeedAfterABoundary_IsNotADelimiter()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b\ny\r\n--b--\nz\r\n--b--";
            using (var reader = EntityEvents.CreateReader(message, EntityEvents.AnyOctetsOptions))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal("start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x\r\n--b\ny\r\n--b--\nz]", "end 1", "end /");
            }
        }

        [Fact]
        public void ReadEvent_BareLineFeeds_AcceptedWithTheOption()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=b\n\n--b\n\nx\n--b\r\n\r\ny\r\n--b--\nz\n";
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, AcceptBareLineFeed = true }))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal(
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "type 1 text/plain", "body 1 [x]", "end 1",
                    "start 2", "type 2 text/plain", "body 2 [y]", "end 2",
                    "epilogue / [z\n]", "end /");
            }
        }

        [Fact]
        public void ReadEvent_QuotedPrintableWithBareLineFeeds_AcceptedWithTheOption()
        {
            // arrange
            const string message = "Content-Transfer-Encoding: quoted-printable\n\na=\nb\n";
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, AcceptBareLineFeed = true, DecodeTransferEncoding = true }))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal("start /", "field / Content-Transfer-Encoding", "type / text/plain", "body / [ab\n]", "end /");
            }
        }

        [Fact]
        public void ReadEvent_LineFeedIsLineBreak_ReadsLineFeedsAsTheContainerFormatsDo()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=b\n\n--b\n\nx\r\n--b\r\ny\n--b--\nz\n";
            using (var reader = new ImfEntityReader(new MemoryStream(Octets.Of(message)), new ImfOptions { AllowMissingMimeVersion = true }, SmallBlock, true))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal(
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "type 1 text/plain", "body 1 [x\r\n--b\r\ny]", "end 1",
                    "epilogue / [z\n]", "end /");
            }
        }

        [Fact]
        public void ReadEvent_LongestDelimiterLine_IsADelimiter()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.DelimiterLineWithPadding(LongestDelimiterPadding)))
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

        [Theory]
        [MemberData(nameof(DelimiterLinesTooLong))]
        public void ReadEvent_DelimiterLineTooLong_IsNotADelimiter(string message, string[] expected)
        {
            // arrange
            using (var reader = new ImfEntityReader(new MemoryStream(Octets.Of(message)), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, AllowLongLines = true }, SmallBlock, false))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal(expected);
            }
        }

        [Fact]
        public void ReadEvent_LongestCloseDelimiterLineAtTheEndOfTheData_IsADelimiter()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.CloseDelimiterAtTheEndWithPadding(LongestCloseDelimiterPadding)))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal("start /", "field / Content-Type", "type / multipart/mixed", "start 1", "type 1 text/plain", "body 1 [x]", "end 1", "end /");
            }
        }

        [Fact]
        public void ReadEvent_CloseDelimiterLineTooLongAtTheEndOfTheData_IsNotADelimiter()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.CloseDelimiterAtTheEndWithPadding(LongestCloseDelimiterPadding + 1)))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadEvent_ManyLinesThatStartLikeADelimiter_ReadsThemAsContent()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.ManyCandidateLines(ManyCandidates)))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.EntitiesRead.Should().Be(2);
            }
        }

        [Fact]
        public void ReadEvent_NestingAtTheLimit_IsRead()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.EmbeddedMessages(3), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxNestingDepth = 3 }))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.EntitiesRead.Should().Be(4);
            }
        }

        [Fact]
        public void ReadEvent_NestingPastTheLimit_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.EmbeddedMessages(4), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxNestingDepth = 3 }))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadEvent_MultipartNestingAtTheLimit_IsRead()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.NestedMultiparts(3), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxNestingDepth = 3 }))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.EntitiesRead.Should().Be(4);
            }
        }

        [Fact]
        public void ReadEvent_MultipartNestingPastTheLimit_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.NestedMultiparts(4), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxNestingDepth = 3 }))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadEvent_DefaultNestingLimit_StopsDeepNesting()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.EmbeddedMessages(ImfOptions.DefaultMaxNestingDepth + 1)))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadEvent_VeryDeepNestingWithALimitToMatch_DoesNotOverflowTheStack()
        {
            // arrange
            var options = new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxNestingDepth = VeryDeep, MaxPartCount = VeryDeep };
            using (var reader = EntityEvents.CreateReader(LongMessages.EmbeddedMessages(VeryDeep), options))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.EntitiesRead.Should().Be(VeryDeep + 1);
            }
        }

        [Fact]
        public void ReadEvent_PartsAtTheLimit_AreRead()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.Parts(3), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxPartCount = 3 }))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.EntitiesRead.Should().Be(4);
            }
        }

        [Fact]
        public void ReadEvent_PartsPastTheLimit_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.Parts(4), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxPartCount = 3 }))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadEvent_EmbeddedMessagePastThePartLimit_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.EmbeddedMessages(2), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxPartCount = 1 }))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadEvent_HeaderLimitsApplyToEachSection_NotToTheWholeMessage()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nA:1\r\n\r\nx\r\n--b\r\nB:2\r\n\r\ny\r\n--b--";
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxHeaderFieldCount = 1, MaxHeaderSectionLength = 45 }))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.EntitiesRead.Should().Be(3);
            }
        }

        [Fact]
        public void ReadEvent_HeaderFieldLimitPassedInAPart_Throws()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nA:1\r\nB:2\r\n\r\nx\r\n--b--";
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxHeaderFieldCount = 1 }))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Theory]
        [MemberData(nameof(Decoded))]
        public void ReadEvent_DecodingOn_GivesTheDecodedBodies(string message, string expected)
        {
            // arrange
            var options = new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, DecodeTransferEncoding = true };
            using (var reader = EntityEvents.CreateReader(message, options))
            {
                // act
                var testResult = DecodedBodies.Read(reader);

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [MemberData(nameof(Decoded))]
        public void ReadEvent_DecodingOnOneByteAtATime_GivesTheSameBodies(string message, string expected)
        {
            // arrange
            var options = new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, DecodeTransferEncoding = true };
            var stream = new BlockStream(Octets.Of(message), BlockStreamMode.SynchronousOnly, 1, 1);
            using (var reader = new ImfEntityReader(stream, options, 1, false))
            {
                // act
                var testResult = DecodedBodies.Read(reader);

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidEncodedBodies))]
        public void ReadEvent_DecodingOnWithInvalidData_Throws(string message)
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, DecodeTransferEncoding = true }))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Theory]
        [MemberData(nameof(InvalidEncodedBodies))]
        public void ReadEvent_DecodingOffWithInvalidData_DoesNotThrow(string message)
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(message))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().NotThrow();
            }
        }

        [Fact]
        public void ReadEvent_DecodingOnInSeveralParts_GivesEachPartItsOwnDecodedBody()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\nx=3D\r\n--b\r\nContent-Transfer-Encoding: base64\r\n\r\nQUI=\r\n--b--";
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, DecodeTransferEncoding = true }))
            {
                // act
                var testResult = EntityEvents.ReadOutline(reader);

                // assert
                testResult.Should().Equal(
                    "start /", "field / Content-Type", "type / multipart/mixed",
                    "start 1", "field 1 Content-Transfer-Encoding", "type 1 text/plain", "body 1 [x=]", "end 1",
                    "start 2", "field 2 Content-Transfer-Encoding", "type 2 text/plain", "body 2 [AB]", "end 2",
                    "end /");
            }
        }

        [Fact]
        public void ReadEvent_InvalidDataBeforeMoreLines_ReportsTheByteAndLineOfTheProblem()
        {
            // arrange
            // ref. RFC 2045 §6.8: nothing of the alphabet can follow padding.  The header section takes 37 bytes and 2 lines, and "QUI=\r\n" 6 bytes and 1 line.
            const string message = "Content-Transfer-Encoding: base64\r\n\r\nQUI=\r\nA\r\nB\r\n";
            const long expectedBytePosition = 43;
            const long expectedLineNumber = 4;
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, DecodeTransferEncoding = true }))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                var exception = testAction.Should().Throw<ImfFileException>().Which;
                exception.Data[ImfFileException.DataItemName.BytePosition].Should().Be(expectedBytePosition);
                exception.Data[ImfFileException.DataItemName.LineNumber].Should().Be(expectedLineNumber);
            }
        }

        [Fact]
        public void ReadEvent_RawBytesThatGiveNoOutputAtTheEnd_AreCoveredByALastChunk()
        {
            // arrange
            // The 4 bytes of "QUJD" start at 37.  One byte at a time, the line break after them gives no output, so the last chunk covers it.
            var stream = new BlockStream(Octets.Of("Content-Transfer-Encoding: base64\r\n\r\nQUJD\r\n"), BlockStreamMode.SynchronousOnly, 1, 1);
            using (var reader = new ImfEntityReader(stream, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, DecodeTransferEncoding = true }, 1, false))
            {
                // act
                var testResult = EntityEvents.ReadBodyChunkRanges(reader);

                // assert
                testResult.Should().Equal("37 4", "41 2");
            }
        }

        [Fact]
        public void ReadEvent_DecodingOff_GivesRawBytesAndNotDecoded()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader("Content-Transfer-Encoding: base64\r\n\r\nQUI="))
            {
                // act
                var testResult = EntityEvents.ReadUntil(reader, ImfEntityEventKind.BodyChunk).Value;

                // assert
                Octets.ToText(testResult.Data).Should().Be("QUI=");
                testResult.IsDecoded.Should().BeFalse();
                testResult.TransferEncoding.Should().Be(MimeTransferEncoding.Base64);
            }
        }

        [Fact]
        public void ReadEvent_DecodingOn_MarksTheChunkAsDecoded()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader("Content-Transfer-Encoding: base64\r\n\r\nQUJD", new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, DecodeTransferEncoding = true }))
            {
                // act
                var testResult = EntityEvents.ReadUntil(reader, ImfEntityEventKind.BodyChunk).Value;

                // assert
                Octets.ToText(testResult.Data).Should().Be("ABC");
                testResult.IsDecoded.Should().BeTrue();
            }
        }

        [Fact]
        public void ReadEvent_UnknownTransferEncodingWithDecodingOn_LeavesTheBodyWithADiagnostic()
        {
            // arrange
            const string message = "Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nContent-Transfer-Encoding: x-other\r\n\r\nQUI=\r\n--b--";
            using (var reader = EntityEvents.CreateReader(message, new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, DecodeTransferEncoding = true }))
            {
                // act
                var testResult = DecodedBodies.Read(reader);

                // assert
                testResult.Should().Be("QUI=");
                reader.Diagnostics.Should().ContainSingle();
                reader.Diagnostics[0].Kind.Should().Be(ImfDiagnosticKind.UnknownTransferEncoding);
                reader.Diagnostics[0].EntityPath.Should().Be("1");
                reader.Diagnostics[0].BytePosition.Should().Be(161);
                reader.Diagnostics[0].Message.Should().Contain("x-other");
            }
        }

        [Fact]
        public void ReadEvent_UnknownTransferEncodingWithDecodingOff_HasNoDiagnostic()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader("Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\nContent-Transfer-Encoding: x-other\r\n\r\nQUI="))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.Diagnostics.Should().BeEmpty();
            }
        }

        [Fact]
        public void Diagnostics_Always_CannotBeChangedByACast()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(OneField))
            {
                // act
                var testResult = reader.Diagnostics;

                // assert
                testResult.Should().NotBeAssignableTo<System.Collections.Generic.List<ImfDiagnostic>>();
            }
        }

        [Fact]
        public void ReadEvent_DefaultMediaType_HasTheUsAsciiCharset()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(OneField))
            {
                // act
                var testResult = EntityEvents.ReadUntil(reader, ImfEntityEventKind.HeaderEnd).Value.ContentType;

                // assert
                // ref. RFC 2045 §5.2
                testResult.ToString().Should().Be("text/plain;charset=us-ascii");
            }
        }

        [Fact]
        public void ReadEvent_NoTransferEncoding_Is7bit()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(OneField))
            {
                // act
                var testResult = EntityEvents.ReadUntil(reader, ImfEntityEventKind.HeaderEnd).Value.TransferEncoding;

                // assert
                // ref. RFC 2045 §6.1
                testResult.Should().Be(MimeTransferEncoding.SevenBit);
            }
        }

        [Fact]
        public void ReadEvent_ExtendedParameterWithUnknownCharset_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(UnknownCharsetContentType))
            {
                // act
                Action testAction = () => EntityEvents.ReadToEnd(reader);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void ReadEvent_ExtendedParameterWithCharsetTheResolverKnows_IsDecoded()
        {
            // arrange
            var options = new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, CharsetResolver = new FixedCharsetResolver(Encoding.UTF8) };
            using (var reader = EntityEvents.CreateReader(UnknownCharsetContentType, options))
            {
                // act
                var testResult = EntityEvents.ReadUntil(reader, ImfEntityEventKind.HeaderEnd).Value.ContentType;

                // assert
                testResult.TryGetParameter("title", out var title).Should().BeTrue();
                title.Value.Should().Be("a");
            }
        }

        [Fact]
        public void ReadEvent_LargeBodyInAMultipartEntity_NeverHoldsMoreThanABlock()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.PartWithBody(LargeBodyLength)))
            {
                // act
                var testResult = DecodedBodies.ReadLongestChunk(reader);

                // assert
                testResult.Should().BeLessOrEqualTo(DefaultBlockLength);
            }
        }

        [Fact]
        public void ReadEvent_LargeBase64BodyDecoded_NeverHoldsMoreThanABlock()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(LongMessages.Base64Body(LargeBodyLength), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, DecodeTransferEncoding = true }))
            {
                // act
                var testResult = DecodedBodies.ReadLongestChunk(reader);

                // assert
                testResult.Should().BeLessOrEqualTo(DefaultBlockLength);
            }
        }

        [Fact]
        public void ReadEvent_Message_GivesTheRawByteRangeOfEachEntity()
        {
            // act
            var testResult = EntityEvents.ReadEntityRanges(EntityMessages.Rfc2046SimpleBoundaryExample);

            // assert
            testResult.Should().Equal(
                "1 [\r\nThis is implicitly typed plain US-ASCII text.\r\nIt does NOT end with a linebreak.]",
                "2 [Content-type: text/plain; charset=us-ascii\r\n\r\nThis is explicitly typed plain US-ASCII text.\r\nIt DOES end with a linebreak.\r\n]",
                "/ [" + EntityMessages.Rfc2046SimpleBoundaryExample + "]");
        }

        [Fact]
        public void ReadEvent_PartWithNoBody_RangeStopsBeforeTheLineBreakOfTheDelimiter()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nA:1\r\n\r\n--b\r\n\r\n--b--";

            // act
            var testResult = EntityEvents.ReadEntityRanges(message);

            // assert
            testResult.Should().Equal("1 [A:1\r\n]", "2 []", "/ [" + message + "]");
        }

        [Fact]
        public void ReadEvent_NestedEntities_GivesTheirRanges()
        {
            // arrange
            const string message = "Content-Type: multipart/mixed; boundary=a\r\n\r\n--a\r\nContent-Type: message/rfc822\r\n\r\nB:2\r\n\r\nx\r\n--a--\r\nend";

            // act
            var testResult = EntityEvents.ReadEntityRanges(message);

            // assert
            testResult.Should().Equal(
                "1 [B:2\r\n\r\nx]",
                "1 [Content-Type: message/rfc822\r\n\r\nB:2\r\n\r\nx]",
                "/ [" + message + "]");
        }

        [Fact]
        public void ReadEvent_Message_GivesTheOffsetAndLengthOfEachEvent()
        {
            // arrange
            // Worked out by hand: the header section ends at 45, "--b\r\n" at 50, "A:1\r\n" and the empty line at 57, "x" at 58, "\r\n--b--\r\n" at 67,
            // and "e" at 68.
            const string message = "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nA:1\r\n\r\nx\r\n--b--\r\ne";

            // act
            var testResult = EntityEvents.ReadEventRanges(message);

            // assert
            testResult.Should().Equal(
                "EntityStart 0 0",
                "HeaderField 0 43",
                "HeaderEnd 0 45",
                "EntityStart 50 0",
                "HeaderField 50 5",
                "HeaderEnd 50 7",
                "BodyChunk 57 1",
                "EntityEnd 50 8",
                "Epilogue 67 1",
                "EntityEnd 0 68");
        }

        [Fact]
        public void ReadEvent_Event_GivesThePathAndDepthOfItsEntity()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(SinglePartMessage))
            {
                // act
                var testResult = EntityEvents.ReadUntil(reader, ImfEntityEventKind.BodyChunk).Value;

                // assert
                testResult.EntityPath.Should().Be("1");
                testResult.Depth.Should().Be(1);
            }
        }

        [Fact]
        public void ReadEvent_NonDataEvent_HasNoData()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(SinglePartMessage))
            {
                // act
                var testResult = reader.ReadEvent().Value;

                // assert
                testResult.DataMemory.Length.Should().Be(0);
            }
        }

        [Fact]
        public void Field_HeaderFieldEvent_GivesTheField()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader("A: 1\r\n 2\r\n\r\n"))
            {
                // act
                var testResult = EntityEvents.ReadUntil(reader, ImfEntityEventKind.HeaderField).Value.Field;

                // assert
                FieldSnapshot.Of(testResult).Should().BeEquivalentTo(new FieldSnapshot("A: 1\r\n 2\r\n", "A", " 1 2", " 1 2", 0, 1));
            }
        }

        [Fact]
        public void Field_OtherEvent_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(OneField))
            {
                var entityEvent = reader.ReadEvent().Value;

                // act
                Action testAction = () => _ = entityEvent.Field;

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void Field_AfterTheNextRead_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader("A:1\r\nB:2\r\n"))
            {
                var field = EntityEvents.ReadUntil(reader, ImfEntityEventKind.HeaderField).Value.Field;
                reader.ReadEvent();

                // act
                Action testAction = () => field.GetName();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void Kind_AfterTheNextRead_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(OneField))
            {
                var entityEvent = reader.ReadEvent().Value;
                reader.ReadEvent();

                // act
                Action testAction = () => _ = entityEvent.Kind;

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void Kind_AfterDispose_Throws()
        {
            // arrange
            var reader = EntityEvents.CreateReader(OneField);
            var entityEvent = reader.ReadEvent().Value;
            reader.Dispose();

            // act
            Action testAction = () => _ = entityEvent.Kind;

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Kind_DefaultView_Throws()
        {
            // arrange
            var entityEvent = default(ImfEntityEvent);

            // act
            Action testAction = () => _ = entityEvent.Kind;

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ContentType_Utf8InQuotedParameter_GivesTheValue()
        {
            // arrange
            // ref. RFC 6532 §3.2
            using (var reader = EntityEvents.CreateReader("Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\nContent-Type: a/b; c=\"\xC3\xA9\"\r\n\r\n"))
            {
                // act
                var testResult = EntityEvents.ReadUntil(reader, ImfEntityEventKind.HeaderEnd).Value.ContentType;

                // assert
                testResult.Parameters[0].Value.Should().Be("\x00E9");
            }
        }

        [Fact]
        public void ContentType_BeforeTheHeaderEnds_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(OneField))
            {
                var entityEvent = reader.ReadEvent().Value;

                // act
                Action testAction = () => _ = entityEvent.ContentType;

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void TransferEncoding_BeforeTheHeaderEnds_Throws()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(OneField))
            {
                var entityEvent = reader.ReadEvent().Value;

                // act
                Action testAction = () => _ = entityEvent.TransferEncoding;

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void BytePosition_AtTheEnd_IsTheLengthOfTheMessage()
        {
            // arrange
            using (var reader = EntityEvents.CreateReader(SinglePartMessage))
            {
                // act
                EntityEvents.ReadToEnd(reader);

                // assert
                reader.BytePosition.Should().Be(SinglePartMessage.Length);
            }
        }

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // act
            Action testAction = () => new ImfEntityReader((Stream)null, EntityEvents.MimeEntityOptions);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullOptions_Throws()
        {
            // act
            Action testAction = () => new ImfEntityReader(new MemoryStream(), null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WriteOnlyStream_Throws()
        {
            // act
            Action testAction = () => new ImfEntityReader(new WriteOnlyStream(), EntityEvents.MimeEntityOptions);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NestingLimitBelowOne_Throws()
        {
            // act
            Action testAction = () => new ImfEntityReader(new MemoryStream(), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxNestingDepth = 0 });

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_PartLimitBelowOne_Throws()
        {
            // act
            Action testAction = () => new ImfEntityReader(new MemoryStream(), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxPartCount = 0 });

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NoCharsetResolver_Throws()
        {
            // act
            Action testAction = () => new ImfEntityReader(new MemoryStream(), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, CharsetResolver = null });

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_HeaderLimitBelowOne_Throws()
        {
            // act
            Action testAction = () => new ImfEntityReader(new MemoryStream(), new ImfOptions { AllowMissingMimeVersion = true, AllowMissingRequiredFields = true, MaxHeaderFieldCount = 0 });

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ReadEvent_AfterDispose_Throws()
        {
            // arrange
            var reader = EntityEvents.CreateReader(OneField);
            reader.Dispose();

            // act
            Action testAction = () => reader.ReadEvent();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void BytePosition_AfterDispose_Throws()
        {
            // arrange
            var reader = EntityEvents.CreateReader(OneField);
            reader.Dispose();

            // act
            Action testAction = () => _ = reader.BytePosition;

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void EntitiesRead_AfterDispose_Throws()
        {
            // arrange
            var reader = EntityEvents.CreateReader(OneField);
            reader.Dispose();

            // act
            Action testAction = () => _ = reader.EntitiesRead;

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Diagnostics_AfterDispose_Throws()
        {
            // arrange
            var reader = EntityEvents.CreateReader(OneField);
            reader.Dispose();

            // act
            Action testAction = () => _ = reader.Diagnostics;

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // arrange
            var reader = EntityEvents.CreateReader(OneField);
            reader.Dispose();

            // act
            Action testAction = () => reader.Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void Dispose_CallerSuppliedStream_LeavesItOpen()
        {
            // arrange
            var stream = new MemoryStream(Octets.Of(OneField));
            var reader = new ImfEntityReader(stream, EntityEvents.MimeEntityOptions);

            // act
            reader.Dispose();

            // assert
            stream.CanRead.Should().BeTrue();
        }
    }
}
