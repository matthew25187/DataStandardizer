using System;
using System.IO;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="ImfEntityWriter"/> and <see cref="ImfEntitySource"/>.
    /// </summary>
    public class ImfEntityWriterTests
    {
        // ref. RFC 2231 §3: section numbers start at 0.
        private const string MalformedContentType = "text/plain; c*1=d";
        private const string MalformedContentTypeField = "Content-Type: text/plain; c*1=d\r\n";

        private const string Header = "MIME-Version: 1.0\r\nContent-Type: text/plain\r\n";
        private const string OneByteBody = "x";
        private const int BlockSizeOfOne = 1;

        // The most the writer hands to the stream at once when it encodes a body, and an input block whose base64 is longer than that.
        private const int EncodedBlockSize = 16 * 1024;
        private const int LargeBlockSize = 64 * 1024;
        private const string FixedBoundaryType = "multipart/mixed;boundary=given";

        // ref. RFC 5322 §2.1.1
        private const int MaxLineLength = 998;

        private static readonly MimeMediaType MultipartWithGivenBoundary = MimeMediaType.Parse(FixedBoundaryType);
        private static readonly MimeMediaType TextUtf8 = MimeMediaType.TextPlain.WithParameter("charset", "utf-8");
        private static readonly MimeMediaType MessagePartial = MimeMediaType.Parse("message/partial;id=a;number=1");
        private static readonly MimeMediaType MessageGlobal = MimeMediaType.Parse("message/global");

        private static readonly string LongestLine = new string('a', MaxLineLength);
        private static readonly string LineOverLongest = new string('a', MaxLineLength + 1);
        private static readonly byte[] AllByteValues = BuildAllByteValues();

        // 2,500 literal bytes and 500 encoded ones come to 4,000 characters of quoted-printable, as do 3,000 bytes of base64.  The line breaks settle it: 53 soft
        // ones of 3 characters each, against 52 of 2.
        private static readonly string TieBeforeLineBreaks = string.Concat(System.Linq.Enumerable.Repeat("aaaaa\x00FF", 500));
        private static readonly byte[] BodyLargerThanTheEncodedBlock = new byte[LargeBlockSize];

        // ref. RFC 2045 §6.7 rule 5: a line of 76 characters, of which the last is the "=" of a soft line break.
        private static readonly string EncodedStartOfLineOverLongest = new string('a', 75) + "=\r\n";

        private static readonly string ExpectedLongLineMessage = Header + "Content-Transfer-Encoding: quoted-printable\r\n\r\n" + EncodedLongLine();

        public static TheoryData<string, byte[], string> ChosenEncodings => new TheoryData<string, byte[], string>
        {
            // ref. RFC 2045 §2.7
            { "ASCII text", Octets.Of("Hello\r\nworld\r\n"), Header + "\r\nHello\r\nworld\r\n" },
            { "empty", new byte[0], Header + "\r\n" },
            { "longest line", Octets.Of(LongestLine), Header + "\r\n" + LongestLine },

            // A few bytes above 127 are cheaper in quoted-printable.
            { "few non-ASCII", Octets.Of("caf\x00C3\x00A9\r\n"), Header + "Content-Transfer-Encoding: quoted-printable\r\n\r\ncaf=C3=A9\r\n" },

            // ref. RFC 2045 §6.7 rule 5
            { "line too long", Octets.Of(LineOverLongest), ExpectedLongLineMessage },

            // A bare CR or LF can't be 7bit, and is encoded so that it decodes to the same byte.
            { "bare LF", Octets.Of("abcdef\ng"), Header + "Content-Transfer-Encoding: quoted-printable\r\n\r\nabcdef=0Ag" },
            { "bare CR", Octets.Of("abcdef\rg"), Header + "Content-Transfer-Encoding: quoted-printable\r\n\r\nabcdef=0Dg" },
            { "CR at the end", Octets.Of("abcd\r"), Header + "Content-Transfer-Encoding: quoted-printable\r\n\r\nabcd=0D" },
            { "NUL", Octets.Of("abcdef\x00"), Header + "Content-Transfer-Encoding: quoted-printable\r\n\r\nabcdef=00" },

            // Most bytes above 127, so base64 is smaller.
            { "binary", Octets.Of("\x00FF\x00FE\x00FD"), Header + "Content-Transfer-Encoding: base64\r\n\r\n//79" },
        };

        public static TheoryData<byte[], ImfOptions, string> AllowedEncodings => new TheoryData<byte[], ImfOptions, string>
        {
            // ref. RFC 2045 §2.8: 8bit fits bytes above 127, but not a NUL, a bare line feed or a long line.
            { Octets.Of("caf\x00C3\x00A9"), new ImfOptions { AllowEightBitTransferEncoding = true }, Header + "Content-Transfer-Encoding: 8bit\r\n\r\ncaf\x00C3\x00A9" },
            { Octets.Of("abcdef\x00"), new ImfOptions { AllowEightBitTransferEncoding = true }, Header + "Content-Transfer-Encoding: quoted-printable\r\n\r\nabcdef=00" },
            { Octets.Of("abcdef\ng"), new ImfOptions { AllowEightBitTransferEncoding = true }, Header + "Content-Transfer-Encoding: quoted-printable\r\n\r\nabcdef=0Ag" },

            // ASCII stays 7bit whatever is allowed.
            { Octets.Of("abc"), new ImfOptions { AllowEightBitTransferEncoding = true, AllowBinaryTransferEncoding = true }, Header + "\r\nabc" },

            // ref. RFC 2045 §2.9: binary fits anything.
            { Octets.Of("a\x00\nb"), new ImfOptions { AllowBinaryTransferEncoding = true }, Header + "Content-Transfer-Encoding: binary\r\n\r\na\x00\nb" },
            { Octets.Of("a\x00FF"), new ImfOptions { AllowBinaryTransferEncoding = true }, Header + "Content-Transfer-Encoding: binary\r\n\r\na\x00FF" },
            { Octets.Of("a\x00FF"), new ImfOptions { AllowEightBitTransferEncoding = true, AllowBinaryTransferEncoding = true }, Header + "Content-Transfer-Encoding: 8bit\r\n\r\na\x00FF" },
        };

        public static TheoryData<MimeTransferEncoding, byte[], string> OverriddenEncodings => new TheoryData<MimeTransferEncoding, byte[], string>
        {
            { MimeTransferEncoding.SevenBit, Octets.Of("abc"), Header + "Content-Transfer-Encoding: 7bit\r\n\r\nabc" },
            { MimeTransferEncoding.EightBit, Octets.Of("abc"), Header + "Content-Transfer-Encoding: 8bit\r\n\r\nabc" },
            { MimeTransferEncoding.EightBit, Octets.Of("\x00FF"), Header + "Content-Transfer-Encoding: 8bit\r\n\r\n\x00FF" },
            { MimeTransferEncoding.Binary, Octets.Of("\x00\n"), Header + "Content-Transfer-Encoding: binary\r\n\r\n\x00\n" },
            { MimeTransferEncoding.QuotedPrintable, Octets.Of("a=b"), Header + "Content-Transfer-Encoding: quoted-printable\r\n\r\na=3Db" },
            { MimeTransferEncoding.Base64, Octets.Of("abc"), Header + "Content-Transfer-Encoding: base64\r\n\r\nYWJj" },
        };

        public static TheoryData<MimeMediaType, MimeTransferEncoding, byte[]> RefusedEncodings => new TheoryData<MimeMediaType, MimeTransferEncoding, byte[]>
        {
            // ref. RFC 2045 §2.7 and §2.8
            { MimeMediaType.TextPlain, MimeTransferEncoding.SevenBit, Octets.Of("\x00FF") },
            { MimeMediaType.TextPlain, MimeTransferEncoding.SevenBit, Octets.Of("a\nb") },
            { MimeMediaType.TextPlain, MimeTransferEncoding.SevenBit, Octets.Of(LineOverLongest) },
            { MimeMediaType.TextPlain, MimeTransferEncoding.EightBit, Octets.Of("\x00") },

            // ref. RFC 2045 §6.3: the writer can't apply an extension mechanism.
            { MimeMediaType.TextPlain, MimeTransferEncoding.Parse("x-rot13"), Octets.Of("a") },

            // ref. RFC 2045 §6.4
            { MimeMediaType.MessageRfc822, MimeTransferEncoding.Base64, Octets.Of("A: 1\r\n") },
            { MimeMediaType.MessageRfc822, MimeTransferEncoding.QuotedPrintable, Octets.Of("A: 1\r\n") },
            { MultipartWithGivenBoundary, MimeTransferEncoding.Base64, Octets.Of("--given--") },
        };

        public static TheoryData<string, string> RoundTrips => new TheoryData<string, string>
        {
            { "text 7bit", "Hello\r\nworld\r\n" },
            { "no line break at the end", "Hello" },
            { "quoted-printable text", "caf\x00C3\x00A9 \r\nend \t\r\n" },
            { "quoted-printable with bare line feeds", "caf\x00C3\x00A9\nend\r" },
            { "quoted-printable with an equals sign", "a=b=c\x00C3" },
            { "base64", "\x00FF\x00FE\x00FD\x00FC\x00FB" },
            { "binary over a line", new string('\x00E9', 200) },
            { "line over the longest", LineOverLongestText },
        };

        public static TheoryData<string> ValidMessages
        {
            get
            {
                var messages = new TheoryData<string>();
                foreach (var row in EntityMessages.Valid)
                {
                    messages.Add((string)row[0]);
                }

                return messages;
            }
        }

        public static TheoryData<Action<ImfEntityWriter>> DisposeGuardedMembers => new TheoryData<Action<ImfEntityWriter>>
        {
            writer => _ = writer.BytePosition,
            writer => _ = writer.EntitiesWritten,
            writer => _ = writer.FieldsWritten,
            writer => writer.Flush(),
            writer => writer.WriteMessage(ImfEntitySource.FromRaw(new byte[0])),
        };

        private static string LineOverLongestText => new string('a', MaxLineLength + 1);

        [Theory]
        [MemberData(nameof(ChosenEncodings))]
        public void WriteMessage_ContentAndNoEncodingGiven_ChoosesTheLightestThatFits(string description, byte[] content, string expected)
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, content);

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(expected, description);
        }

        [Theory]
        [MemberData(nameof(AllowedEncodings))]
        public void WriteMessage_EncodingAllowedByOptions_UsesItWhereItFits(byte[] content, ImfOptions options, string expected)
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, content);

            // act
            var testResult = MessageWrites.Write(source, options);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(OverriddenEncodings))]
        public void WriteMessage_TransferEncodingGiven_UsesIt(MimeTransferEncoding encoding, byte[] content, string expected)
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, content);
            source.TransferEncoding = encoding;

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(RefusedEncodings))]
        public void WriteMessage_TransferEncodingThatDoesNotFit_ThrowsAndWritesNothing(MimeMediaType contentType, MimeTransferEncoding encoding, byte[] content)
        {
            // arrange
            var source = ImfEntitySource.FromContent(contentType, content);
            source.TransferEncoding = encoding;
            var stream = new MemoryStream();
            using (var writer = MessageWrites.CreateWriter(stream))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
                stream.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_TextWithCrLfInQuotedPrintable_WritesItsLineBreaksAsLineBreaks()
        {
            // arrange
            // ref. RFC 2045 §6.7 rule 4
            var source = ImfEntitySource.FromContent(TextUtf8, Octets.Of("caf\x00C3\x00A9 abcdef\r\nb"));

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be("MIME-Version: 1.0\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\ncaf=C3=A9 abcdef\r\nb");
        }

        [Fact]
        public void WriteMessage_NonTextInQuotedPrintable_EncodesItsLineBreaks()
        {
            // arrange
            // ref. RFC 2045 §6.7 rule 4
            var source = ImfEntitySource.FromContent(MimeMediaType.ApplicationOctetStream, Octets.Of("abcdefgh\r\nij\x00C3"));

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be("MIME-Version: 1.0\r\nContent-Type: application/octet-stream\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\nabcdefgh=0D=0Aij=C3");
        }

        [Fact]
        public void WriteMessage_SizesTiedBeforeTheirLineBreaks_ChoosesTheSmallerWithThem()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(TieBeforeLineBreaks));

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Contain("Content-Transfer-Encoding: base64\r\n");
        }

        [Fact]
        public void WriteMessage_InputBlockLargerThanTheEncodedBlock_ReportsEachBlockOfOutput()
        {
            // arrange
            var progress = new RecordingProgress();
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, BodyLargerThanTheEncodedBlock);
            source.TransferEncoding = MimeTransferEncoding.Base64;
            using (var writer = new ImfEntityWriter(new MemoryStream(), new ImfOptions(), LargeBlockSize, false, MessageWrites.NextBoundary()))
            {
                // act
                writer.WriteMessage(source, progress);
            }

            // assert
            progress.LargestStep.Should().BeLessOrEqualTo(EncodedBlockSize);
        }

        [Fact]
        public void WriteMessage_AllByteValues_ChoosesBase64()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.ApplicationOctetStream, AllByteValues);

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Contain("Content-Transfer-Encoding: base64\r\n");
        }

        [Theory]
        [MemberData(nameof(RoundTrips))]
        public void ReadEvent_MessageThatWasWritten_DecodesToTheContent(string description, string content)
        {
            // arrange
            var written = MessageWrites.Write(ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(content)));

            // act
            var testResult = MessageWrites.ReadDecodedBody(written);

            // assert
            testResult.Should().Be(content, description);
        }

        [Theory]
        [MemberData(nameof(RoundTrips))]
        public void ReadEvent_PartThatWasWritten_DecodesToTheContent(string description, string content)
        {
            // arrange
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(content));
            var written = MessageWrites.Write(ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { part }));

            // act
            var testResult = MessageWrites.ReadDecodedBody(written);

            // assert
            testResult.Should().Be(content, description);
        }

        [Theory]
        [MemberData(nameof(RoundTrips))]
        public void ReadEvent_PartWrittenWithBinaryAllowed_DecodesToTheContent(string description, string content)
        {
            // arrange
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(content));
            var options = new ImfOptions { AllowBinaryTransferEncoding = true };
            var written = MessageWrites.Write(ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { part }), options);

            // act
            var testResult = MessageWrites.ReadDecodedBody(written);

            // assert
            testResult.Should().Be(content, description);
        }

        public static TheoryData<MimeTransferEncoding> AllEncodings => new TheoryData<MimeTransferEncoding>
        {
            MimeTransferEncoding.SevenBit,
            MimeTransferEncoding.EightBit,
            MimeTransferEncoding.Binary,
            MimeTransferEncoding.QuotedPrintable,
            MimeTransferEncoding.Base64,
        };

        [Theory]
        [MemberData(nameof(AllEncodings))]
        public void ReadEvent_PartWrittenInAnEncoding_DecodesToTheContent(MimeTransferEncoding encoding)
        {
            // arrange
            const string content = "Hello, = world \r\nend\r\n";
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(content));
            part.TransferEncoding = encoding;
            var written = MessageWrites.Write(ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { part }));

            // act
            var testResult = MessageWrites.ReadDecodedBody(written);

            // assert
            testResult.Should().Be(content);
        }

        [Theory]
        [MemberData(nameof(RoundTrips))]
        public void WriteMessage_BlocksOfOneByte_GivesTheSameBytes(string description, string content)
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(content));
            var expected = MessageWrites.Write(source);

            // act
            var testResult = MessageWrites.Write(source, new ImfOptions(), BlockSizeOfOne, false, MessageWrites.NextBoundary());

            // assert
            testResult.Should().Be(expected, description);
        }

        [Fact]
        public void WriteMessage_ContentInStream_GivesTheSameBytesAsInMemory()
        {
            // arrange
            var bytes = Octets.Of("caf\x00C3\x00A9\r\n");
            var expected = MessageWrites.Write(ImfEntitySource.FromContent(MimeMediaType.TextPlain, bytes));
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new MemoryStream(bytes));

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteMessage_StreamPartWayThrough_WritesFromItsPosition()
        {
            // arrange
            var stream = new MemoryStream(Octets.Of("skipped body"));
            stream.Position = 8;
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, stream);

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(Header + "\r\nbody");
        }

        [Fact]
        public void WriteMessage_ForwardOnlyStreamInBase64_EncodesIt()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new ForwardOnlyStream(Octets.Of("abc")));
            source.TransferEncoding = MimeTransferEncoding.Base64;

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(Header + "Content-Transfer-Encoding: base64\r\n\r\nYWJj");
        }

        [Fact]
        public void WriteMessage_ForwardOnlyTextInQuotedPrintable_WritesBareLineFeedsAsLineBreaks()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new ForwardOnlyStream(Octets.Of("a\nb")));
            source.TransferEncoding = MimeTransferEncoding.QuotedPrintable;

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(Header + "Content-Transfer-Encoding: quoted-printable\r\n\r\na\r\nb");
        }

        [Fact]
        public void WriteMessage_ForwardOnlyStreamThatWouldNeedALook_Throws()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new ForwardOnlyStream(Octets.Of("abc")));
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_ForwardOnlyQuotedPrintableInsideAGivenBoundaryWithoutTheSafeText_Throws()
        {
            // arrange
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new ForwardOnlyStream(Octets.Of("abc")));
            part.TransferEncoding = MimeTransferEncoding.QuotedPrintable;
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { part });
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_Fields_WritesThemInOrderThenTheGeneratedOnes()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(OneByteBody))
                .AddField("Subject", "Hi", ImfHeaderFieldKind.Unstructured)
                .AddRawField(Octets.Of("X-Raw :  1\r\n 2\r\n"));

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be("Subject: Hi\r\nX-Raw :  1\r\n 2\r\nMIME-Version: 1.0\r\nContent-Type: text/plain\r\n\r\nx");
        }

        [Fact]
        public void WriteMessage_MimeVersionAdded_DoesNotAddAnother()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(OneByteBody))
                .AddField("mime-version", "1.0 (x)", ImfHeaderFieldKind.Structured);

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be("mime-version: 1.0 (x)\r\nContent-Type: text/plain\r\n\r\nx");
        }

        [Fact]
        public void WriteMessage_ContentTypeAddedThatAgrees_DoesNotAddAnother()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(OneByteBody))
                .AddField("Content-Type", " text/plain (a comment)", ImfHeaderFieldKind.Structured);

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be("Content-Type: text/plain (a comment)\r\nMIME-Version: 1.0\r\n\r\nx");
        }

        [Fact]
        public void WriteMessage_TransferEncodingFieldAdded_AsksForThatEncoding()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("abc"))
                .AddRawField(Octets.Of("Content-Transfer-Encoding: base64\r\n"));

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be("Content-Transfer-Encoding: base64\r\n" + Header + "\r\nYWJj");
        }

        public static TheoryData<Func<ImfEntitySource>> SourcesWithConflictingFields => new TheoryData<Func<ImfEntitySource>>
        {
            () => ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]).AddField("Content-Type", "text/html", ImfHeaderFieldKind.Structured),
            () => ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]).AddField("Content-Type", "not a type", ImfHeaderFieldKind.Structured),
            () => ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0])
                .AddField("Content-Type", "text/plain", ImfHeaderFieldKind.Structured).AddField("content-type", "text/plain", ImfHeaderFieldKind.Structured),
            () => ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0])
                .AddField("Content-Transfer-Encoding", "7bit", ImfHeaderFieldKind.Structured).AddField("Content-Transfer-Encoding", "7bit", ImfHeaderFieldKind.Structured),
            () => ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]).AddField("Content-Transfer-Encoding", "not valid", ImfHeaderFieldKind.Structured),
            () => WithTransferEncoding(MimeTransferEncoding.Base64).AddField("Content-Transfer-Encoding", "7bit", ImfHeaderFieldKind.Structured),
            () => ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { ImfEntitySource.FromRaw(new byte[0]) })
                .AddField("Content-Type", "multipart/mixed", ImfHeaderFieldKind.Structured),
            () => ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]).AddField("A B", "1", ImfHeaderFieldKind.Unstructured),
            () => ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]).AddField("A", "caf\x00E9", ImfHeaderFieldKind.Unstructured),
            () => ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]).AddRawField(Octets.Of("A: 1\r\nB: 2\r\n")),
        };

        [Theory]
        [MemberData(nameof(SourcesWithConflictingFields))]
        public void WriteMessage_FieldThatIsNotAllowed_ThrowsAndWritesNothing(Func<ImfEntitySource> build)
        {
            // arrange
            var source = build();
            var stream = new MemoryStream();
            using (var writer = MessageWrites.CreateWriter(stream))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
                stream.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_FieldInAPartNotAllowed_NamesThePart()
        {
            // arrange
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]).AddField("A B", "1", ImfHeaderFieldKind.Unstructured);
            var source = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { ImfEntitySource.FromRaw(new byte[0]), part });
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be("2");
            }
        }

        [Fact]
        public void WriteMessage_NonAsciiFieldWithUtf8Encoding_WritesItAndLabelsAnEnclosingEntity8bit()
        {
            // arrange
            // ref. RFC 2045 §6.4, RFC 6532: a part with UTF-8 in its header is not 7bit data.
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(OneByteBody)).AddField("A", "\x00E9", ImfHeaderFieldKind.Unstructured);
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { part });

            // act
            var testResult = MessageWrites.Write(source, new ImfOptions { HeaderEncoding = ImfHeaderEncoding.Utf8 });

            // assert
            testResult.Should().Be(
                "MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=given\r\nContent-Transfer-Encoding: 8bit\r\n\r\n" +
                "--given\r\nA: \x00C3\x00A9\r\nContent-Type: text/plain\r\n\r\nx\r\n--given--");
        }

        [Fact]
        public void WriteMessage_Multipart_WritesTheDelimitersPreambleAndEpilogue()
        {
            // arrange
            // ref. RFC 2046 §5.1.1
            var source = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[]
            {
                ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("one")),
                ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("two\r\n")),
            });
            source.Preamble = Octets.Of("before");
            source.Epilogue = Octets.Of("after");

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(
                "MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=\"=_1\"\r\n\r\n" +
                "before\r\n" +
                "--=_1\r\nContent-Type: text/plain\r\n\r\none\r\n" +
                "--=_1\r\nContent-Type: text/plain\r\n\r\ntwo\r\n\r\n" +
                "--=_1--\r\nafter");
        }

        [Fact]
        public void WriteMessage_MultipartWithNoPreambleOrEpilogue_EndsAtTheCloseDelimiter()
        {
            // arrange
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { ImfEntitySource.FromRaw(new byte[0]) });

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be("MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=given\r\n\r\n--given\r\n\r\n--given--");
        }

        [Fact]
        public void WriteMessage_EmptyEpilogue_WritesItsLineBreak()
        {
            // arrange
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { ImfEntitySource.FromRaw(new byte[0]) });
            source.Epilogue = new byte[0];

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be("MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=given\r\n\r\n--given\r\n\r\n--given--\r\n");
        }

        [Fact]
        public void WriteMessage_NestedMultipart_UsesADifferentBoundaryForEach()
        {
            // arrange
            var inner = ImfEntitySource.FromParts(MimeMediaType.MultipartAlternative, new[] { ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(OneByteBody)) });
            var source = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { inner });

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(
                "MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=\"=_1\"\r\n\r\n" +
                "--=_1\r\nContent-Type: multipart/alternative; boundary=\"=_2\"\r\n\r\n" +
                "--=_2\r\nContent-Type: text/plain\r\n\r\nx\r\n--=_2--\r\n--=_1--");
        }

        [Fact]
        public void WriteMessage_EmbeddedMessage_WritesItsHeaderAndBody()
        {
            // arrange
            // ref. RFC 2046 §5.2.1
            var inner = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(OneByteBody)).AddField("Subject", "Inner", ImfHeaderFieldKind.Unstructured);
            var source = ImfEntitySource.FromMessage(inner);

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(
                "MIME-Version: 1.0\r\nContent-Type: message/rfc822\r\n\r\n" +
                "Subject: Inner\r\nMIME-Version: 1.0\r\nContent-Type: text/plain\r\n\r\nx");
        }

        [Fact]
        public void WriteMessage_EmbeddedMessageWithAnEightBitBody_LabelsTheEntity8bit()
        {
            // arrange
            // ref. RFC 2045 §6.4
            var options = new ImfOptions { AllowEightBitTransferEncoding = true };
            var inner = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("\x00FF"));
            var source = ImfEntitySource.FromMessage(inner);

            // act
            var testResult = MessageWrites.Write(source, options);

            // assert
            testResult.Should().StartWith("MIME-Version: 1.0\r\nContent-Type: message/rfc822\r\nContent-Transfer-Encoding: 8bit\r\n\r\n");
        }

        [Fact]
        public void WriteMessage_MultipartWithABinaryPart_LabelsItBinary()
        {
            // arrange
            // ref. RFC 2045 §6.4
            var options = new ImfOptions { AllowBinaryTransferEncoding = true };
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("\x00")) });

            // act
            var testResult = MessageWrites.Write(source, options);

            // assert
            testResult.Should().StartWith("MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=given\r\nContent-Transfer-Encoding: binary\r\n\r\n");
        }

        [Fact]
        public void WriteMessage_MultipartLabelTooNarrowForItsParts_Throws()
        {
            // arrange
            var options = new ImfOptions { AllowEightBitTransferEncoding = true };
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("\x00FF")) });
            source.TransferEncoding = MimeTransferEncoding.SevenBit;
            using (var writer = new ImfEntityWriter(new MemoryStream(), options))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_MultipartWithABase64Label_Throws()
        {
            // arrange
            // ref. RFC 2045 §6.4
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { ImfEntitySource.FromRaw(new byte[0]) });
            source.TransferEncoding = MimeTransferEncoding.Base64;
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_MessagePartialThatIsNotSevenBit_Throws()
        {
            // arrange
            // ref. RFC 2046 §5.2.2
            var options = new ImfOptions { AllowEightBitTransferEncoding = true };
            var source = ImfEntitySource.FromContent(MessagePartial, Octets.Of("\x00FF"));
            using (var writer = new ImfEntityWriter(new MemoryStream(), options))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_MessageContentThatNeedsEncoding_Throws()
        {
            // arrange
            // ref. RFC 2045 §6.4
            var source = ImfEntitySource.FromContent(MimeMediaType.MessageRfc822, Octets.Of("\x00FF"));
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_MessageGlobalContentThatNeedsEncoding_EncodesIt()
        {
            // arrange
            // ref. RFC 6532 §3.7
            var source = ImfEntitySource.FromContent(MessageGlobal, Octets.Of("\x00FF\x00FE\x00FD"));

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be("MIME-Version: 1.0\r\nContent-Type: message/global\r\nContent-Transfer-Encoding: base64\r\n\r\n//79");
        }

        [Fact]
        public void WriteMessage_BinaryPreambleNotAllowed_Throws()
        {
            // arrange
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { ImfEntitySource.FromRaw(new byte[0]) });
            source.Preamble = Octets.Of("\x00");
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_EightBitEpilogueNotAllowed_Throws()
        {
            // arrange
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { ImfEntitySource.FromRaw(new byte[0]) });
            source.Epilogue = Octets.Of("\x00FF");
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_ContentThatHoldsTheGeneratedBoundary_PicksAnother()
        {
            // arrange
            // ref. RFC 2046 §5.1.1: the first boundary appears at the start of a line of the part.
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("a\r\n--=_1\r\nb"));
            var source = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { part });

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(
                "MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=\"=_2\"\r\n\r\n" +
                "--=_2\r\nContent-Type: text/plain\r\n\r\na\r\n--=_1\r\nb\r\n--=_2--");
        }

        public static TheoryData<string> CollidingContent => new TheoryData<string>
        {
            // ref. RFC 2046 §5.1.1: the boundary must not appear as the prefix of any line.
            "--given",
            "--given--",
            "--givenmore",
            "a\r\n--given\r\nb",
            "a\n--given",
            "a\r\n--given",
        };

        [Theory]
        [MemberData(nameof(CollidingContent))]
        public void WriteMessage_ContentThatHoldsAGivenBoundary_ThrowsAndWritesNothing(string content)
        {
            // arrange
            var options = new ImfOptions { AllowBinaryTransferEncoding = true };
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(content));
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { part });
            var stream = new MemoryStream();
            using (var writer = new ImfEntityWriter(stream, options))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
                stream.Length.Should().Be(0);
            }
        }

        [Theory]
        [MemberData(nameof(CollidingContent))]
        public void WriteMessage_BlocksOfOneByteAndAGivenBoundary_StillFindsTheCollision(string content)
        {
            // arrange
            var options = new ImfOptions { AllowBinaryTransferEncoding = true };
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new MemoryStream(Octets.Of(content)));
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { part });
            using (var writer = new ImfEntityWriter(new MemoryStream(), options, BlockSizeOfOne, false, MessageWrites.NextBoundary()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        public static TheoryData<string> ContentThatDoesNotCollide => new TheoryData<string>
        {
            // Only the start of a line counts.
            "a--given",
            " --given",
            "-given",
            "--give",
        };

        [Theory]
        [MemberData(nameof(ContentThatDoesNotCollide))]
        public void WriteMessage_ContentThatDoesNotHoldAGivenBoundary_WritesIt(string content)
        {
            // arrange
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(content));
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { part });
            var written = MessageWrites.Write(source);

            // act
            var testResult = MessageWrites.ReadDecodedBody(written);

            // assert
            testResult.Should().Be(content);
        }

        [Fact]
        public void WriteMessage_EncodedContentWhoseBytesHoldTheBoundary_DoesNotCollide()
        {
            // arrange
            // Base64 output has no "-", so the bytes don't matter.
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("--given"));
            part.TransferEncoding = MimeTransferEncoding.Base64;
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { part });

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Contain("LS1naXZlbg==");
        }

        [Fact]
        public void WriteMessage_QuotedPrintableOutputThatHoldsAGivenBoundary_Throws()
        {
            // arrange
            // ref. RFC 2045 §6.7: the output is the same as the bytes here, so the line starts with the delimiter.
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("--given"));
            part.TransferEncoding = MimeTransferEncoding.QuotedPrintable;
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { part });
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_QuotedPrintableWithTheGeneratedBoundaryInItsBytes_KeepsTheBoundary()
        {
            // arrange
            // ref. RFC 2045 §6.7 rule 1: the "=" of the delimiter is written as "=3D", so no line of the output can start with it.
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("--=_1"));
            part.TransferEncoding = MimeTransferEncoding.QuotedPrintable;
            var source = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { part });

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(
                "MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=\"=_1\"\r\n\r\n" +
                "--=_1\r\nContent-Type: text/plain\r\nContent-Transfer-Encoding: quoted-printable\r\n\r\n--=3D_1\r\n--=_1--");
        }

        [Fact]
        public void WriteMessage_RawPartThatHoldsTheEnclosingBoundary_PicksAnother()
        {
            // arrange
            var part = ImfEntitySource.FromRaw(Octets.Of("\r\n--=_1\r\n"));
            var source = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { part });

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Contain("boundary=\"=_2\"");
        }

        [Fact]
        public void WriteMessage_PreambleThatHoldsTheGivenBoundary_Throws()
        {
            // arrange
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { ImfEntitySource.FromRaw(new byte[0]) });
            source.Preamble = Octets.Of("--given");
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_EpilogueThatHoldsItsOwnBoundary_WritesIt()
        {
            // arrange
            // ref. RFC 2046 §5.1.1: the epilogue comes after the close delimiter, so its own boundary means nothing there.
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { ImfEntitySource.FromRaw(new byte[0]) });
            source.Epilogue = Octets.Of("--given");

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().EndWith("--given--\r\n--given");
        }

        [Fact]
        public void WriteMessage_InnerEpilogueThatHoldsTheOuterBoundary_Throws()
        {
            // arrange
            var inner = ImfEntitySource.FromParts(MimeMediaType.Parse("multipart/mixed;boundary=inner"), new[] { ImfEntitySource.FromRaw(new byte[0]) });
            inner.Epilogue = Octets.Of("--given");
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { inner });
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_FieldOfAPartThatLooksLikeTheBoundary_Throws()
        {
            // arrange
            // ref. RFC 5322 §3.6.8: "--given" is a valid field name, but a line starting with it can't be in the part (RFC 2046 §5.1.1).
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]).AddField("--given", "1", ImfHeaderFieldKind.Unstructured);
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { part });
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_EmbeddedContentThatHoldsAnOuterBoundary_Throws()
        {
            // arrange
            var inner = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("--given"));
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { ImfEntitySource.FromMessage(inner) });
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        public static TheoryData<string> InvalidBoundaries => new TheoryData<string>
        {
            // ref. RFC 2046 §5.1.1: boundary := 0*69<bchars> bcharsnospace
            new string('a', 71),
            "ends in a space ",
            "a*b",
        };

        [Theory]
        [MemberData(nameof(InvalidBoundaries))]
        public void WriteMessage_GivenBoundaryNotValid_Throws(string boundary)
        {
            // arrange
            var contentType = MimeMediaType.MultipartMixed.WithParameter("boundary", boundary);
            var source = ImfEntitySource.FromParts(contentType, new[] { ImfEntitySource.FromRaw(new byte[0]) });
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_LongestGivenBoundary_WritesIt()
        {
            // arrange
            var boundary = new string('a', 70);
            var contentType = MimeMediaType.MultipartMixed.WithParameter("boundary", boundary);
            var source = ImfEntitySource.FromParts(contentType, new[] { ImfEntitySource.FromRaw(new byte[0]) });

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().EndWith(boundary + "--");
        }

        [Fact]
        public void WriteMessage_NestedGivenBoundariesWhereOneIsAPrefix_Throws()
        {
            // arrange
            // ref. RFC 2046 §5.1.1: a line that delimits one would start with the other.
            var inner = ImfEntitySource.FromParts(MimeMediaType.Parse("multipart/mixed;boundary=given2"), new[] { ImfEntitySource.FromRaw(new byte[0]) });
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { inner });
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_SiblingsWithTheSameGivenBoundary_WritesThem()
        {
            // arrange
            // ref. RFC 2046 §5.1.1: only an enclosing boundary matters.
            var inner = MimeMediaType.Parse("multipart/mixed;boundary=inner");
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[]
            {
                ImfEntitySource.FromParts(inner, new[] { ImfEntitySource.FromRaw(new byte[0]) }),
                ImfEntitySource.FromParts(inner, new[] { ImfEntitySource.FromRaw(new byte[0]) }),
            });
            var written = MessageWrites.Write(source);

            // act
            var testResult = MessageWrites.ReadOutline(written);

            // assert
            testResult.Should().Equal(
                "start /", "field / MIME-Version", "field / Content-Type", "type / multipart/mixed",
                "start 1", "field 1 Content-Type", "type 1 multipart/mixed", "start 1.1", "type 1.1 text/plain", "end 1.1", "end 1",
                "start 2", "field 2 Content-Type", "type 2 multipart/mixed", "start 2.1", "type 2.1 text/plain", "end 2.1", "end 2",
                "end /");
        }

        [Fact]
        public void WriteMessage_GeneratedBoundaryIsAPrefixOfAGivenInnerOne_PicksAnother()
        {
            // arrange
            var inner = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed.WithParameter("boundary", "=_12"), new[] { ImfEntitySource.FromRaw(new byte[0]) });
            var source = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { inner });

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().StartWith("MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=\"=_2\"\r\n");
        }

        [Fact]
        public void WriteMessage_GeneratedBoundaryIsAPrefixOfASibling_KeepsIt()
        {
            // arrange
            // ref. RFC 2046 §5.1.1: only an enclosing boundary matters.  Every generated boundary starts with "=_".
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[]
            {
                ImfEntitySource.FromParts(MimeMediaType.MultipartMixed.WithParameter("boundary", "=_"), new[] { ImfEntitySource.FromRaw(new byte[0]) }),
                ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { ImfEntitySource.FromRaw(new byte[0]) }),
            });

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Contain("boundary=\"=_1\"");
        }

        [Fact]
        public void WriteMessage_GeneratorThatKeepsColliding_ThrowsAfterTryingAgain()
        {
            // arrange
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("--same"));
            var source = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { part });
            var stream = new MemoryStream();
            using (var writer = new ImfEntityWriter(stream, new ImfOptions(), BlockSizeOfOne, false, MessageWrites.SameBoundary("same")))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
                stream.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_DefaultBoundaries_CanBeReadBack()
        {
            // arrange
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(OneByteBody));
            var source = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { part });
            var stream = new MemoryStream();
            using (var writer = new ImfEntityWriter(stream))
            {
                writer.WriteMessage(source);
            }

            // act
            var testResult = MessageWrites.ReadOutline(Octets.ToText(stream.ToArray()));

            // assert
            testResult.Should().Equal(
                "start /", "field / MIME-Version", "field / Content-Type", "type / multipart/mixed",
                "start 1", "field 1 Content-Type", "type 1 text/plain", "body 1 [x]", "end 1", "end /");
        }

        [Theory]
        [MemberData(nameof(ValidMessages))]
        public void WriteMessage_RawMessage_CopiesTheBytes(string message)
        {
            // arrange
            var source = ImfEntitySource.FromRaw(Octets.Of(message));

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(message);
        }

        [Fact]
        public void WriteMessage_RawForwardOnlyStream_CopiesTheBytes()
        {
            // arrange
            var source = ImfEntitySource.FromRaw(new ForwardOnlyStream(Octets.Of(EntityMessages.Rfc2046SimpleBoundaryExample)));

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(EntityMessages.Rfc2046SimpleBoundaryExample);
        }

        [Fact]
        public void WriteMessage_RawForwardOnlyStreamInAPart_Throws()
        {
            // arrange
            var part = ImfEntitySource.FromRaw(new ForwardOnlyStream(new byte[0]));
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { part });
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        public static TheoryData<string> MessagesToRewrite => new TheoryData<string>
        {
            SignedMessages.MultipartSigned,
            EntityMessages.Rfc2046SimpleBoundaryExample,
            "MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\nA:1\r\n\r\n\r\n--b\r\n\r\nx\r\n--b--",
        };

        [Theory]
        [MemberData(nameof(MessagesToRewrite))]
        public void WriteMessage_MultipartFromWhatWasRead_GivesTheSameBytes(string message)
        {
            // arrange
            var source = MessageWrites.BuildFromRead(message);

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(message);
        }

        [Fact]
        public void WriteMessage_SignedPartUnchangedAndAnotherChanged_KeepsTheSignedBytes()
        {
            // arrange
            // ref. RFC 1847 §2.1: the signature covers the first part of the multipart/signed entity, byte for byte.
            var signed = ImfEntitySource.FromRaw(Octets.Of(SignedMessages.MultipartSigned));
            var changed = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("changed"));
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { changed, signed });

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Contain(SignedMessages.MultipartSigned);
        }

        public static TheoryData<string> AppendixAMessages => new TheoryData<string>
        {
            SignedMessages.Rfc5322AppendixA11,
            SignedMessages.Rfc5322AppendixA12,
            SignedMessages.Rfc5322AppendixA13,
            SignedMessages.Rfc5322AppendixA51,
            SignedMessages.Rfc5322AppendixA6,
        };

        [Theory]
        [MemberData(nameof(AppendixAMessages))]
        public void WriteFields_Rfc5322AppendixAMessage_GivesTheSameBytes(string message)
        {
            // act
            // These are not MIME messages, so their fields go through a header writer, and their bodies straight to the stream.
            var testResult = MessageWrites.CopyWithHeaderWriter(message);

            // assert
            testResult.Should().Be(message);
        }

        [Theory]
        [MemberData(nameof(AppendixAMessages))]
        public void WriteMessage_Rfc5322AppendixAMessageAsRawBytes_GivesTheSameBytes(string message)
        {
            // arrange
            var source = ImfEntitySource.FromRaw(Octets.Of(message));

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(message);
        }

        [Fact]
        public void WriteMessage_MultipartSignedAsRawBytes_KeepsItsDelimiterPadding()
        {
            // arrange
            var source = ImfEntitySource.FromRaw(Octets.Of(SignedMessages.MultipartSignedWithPadding));

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(SignedMessages.MultipartSignedWithPadding);
        }

        [Fact]
        public void WriteMessage_LineFeedSetting_WritesLineFeedsThroughout()
        {
            // arrange
            // ref. #412 decision 14
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("a\nb")).AddField("A", "1", ImfHeaderFieldKind.Unstructured);
            var source = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { part });

            // act
            var testResult = MessageWrites.Write(source, new ImfOptions(), BlockSizeOfOne, true, MessageWrites.NextBoundary());

            // assert
            testResult.Should().Be(
                "MIME-Version: 1.0\nContent-Type: multipart/mixed; boundary=\"=_1\"\n\n" +
                "--=_1\nA: 1\nContent-Type: text/plain\n\na\nb\n--=_1--");
        }

        [Fact]
        public void WriteMessage_LineFeedSettingAndEncodedBody_HasNoCarriageReturns()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(LineOverLongest));

            // act
            var testResult = MessageWrites.Write(source, new ImfOptions(), BlockSizeOfOne, true, MessageWrites.NextBoundary());

            // assert
            testResult.Should().NotContain("\r");
        }

        [Fact]
        public void ReadEvent_MessageWrittenWithTheLineFeedSetting_DecodesToTheContent()
        {
            // arrange
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(LineOverLongest));
            var written = MessageWrites.Write(ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[] { part }), new ImfOptions(), BlockSizeOfOne, true, MessageWrites.NextBoundary());
            var options = new ImfOptions { DecodeTransferEncoding = true };

            // act
            var testResult = MessageWrites.ReadContainerDecodedBody(written, options);

            // assert
            testResult.Should().Be(LineOverLongest);
        }

        [Fact]
        public void WriteMessage_NestedPastTheLimit_Throws()
        {
            // arrange
            var options = new ImfOptions { MaxNestingDepth = 1 };
            var inner = ImfEntitySource.FromMessage(ImfEntitySource.FromRaw(new byte[0]));
            var source = ImfEntitySource.FromMessage(inner);
            using (var writer = MessageWrites.CreateWriter(new MemoryStream(), options))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_MorePartsThanTheLimit_Throws()
        {
            // arrange
            var options = new ImfOptions { MaxPartCount = 1 };
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { ImfEntitySource.FromRaw(new byte[0]), ImfEntitySource.FromRaw(new byte[0]) });
            using (var writer = MessageWrites.CreateWriter(new MemoryStream(), options))
            {
                // act
                Action testAction = () => writer.WriteMessage(source);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Fact]
        public void WriteMessage_TwoMessages_WritesThemOneAfterAnother()
        {
            // arrange
            var stream = new MemoryStream();
            using (var writer = MessageWrites.CreateWriter(stream))
            {
                writer.WriteMessage(ImfEntitySource.FromRaw(Octets.Of("A")));

                // act
                writer.WriteMessage(ImfEntitySource.FromRaw(Octets.Of("B")));
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be("AB");
        }

        [Fact]
        public void WriteMessage_AfterARefusal_CanContinue()
        {
            // arrange
            var stream = new MemoryStream();
            var refused = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]).AddField("A B", "1", ImfHeaderFieldKind.Unstructured);
            using (var writer = MessageWrites.CreateWriter(stream))
            {
                MessageWrites.IgnoreFailure(() => writer.WriteMessage(refused));

                // act
                writer.WriteMessage(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)));
            }

            // assert
            Octets.ToText(stream.ToArray()).Should().Be(OneByteBody);
        }

        [Fact]
        public void WriteMessage_AfterAStreamFailure_ThrowsThatTheWriterCannotContinue()
        {
            // arrange
            using (var writer = MessageWrites.CreateWriter(new FailingWriteStream()))
            {
                MessageWrites.IgnoreFailure(() => writer.WriteMessage(ImfEntitySource.FromRaw(Octets.Of(OneByteBody))));

                // act
                Action testAction = () => writer.WriteMessage(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)));

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_Progress_ReportsTheCountsOfTheOutput()
        {
            // arrange
            var progress = new RecordingProgress();
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(OneByteBody));
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { part });
            var stream = new MemoryStream();
            using (var writer = MessageWrites.CreateWriter(stream))
            {
                // act
                writer.WriteMessage(source, progress);
            }

            // assert
            // MIME-Version and two Content-Type fields; the part and the message.
            progress.Last.Should().Be(new ImfWriteProgress(3, 2, stream.Length));
        }

        [Fact]
        public void EntitiesWritten_AfterAMessageWithAPart_CountsBoth()
        {
            // arrange
            var source = ImfEntitySource.FromParts(MultipartWithGivenBoundary, new[] { ImfEntitySource.FromRaw(new byte[0]) });
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                writer.WriteMessage(source);

                // act
                var testResult = writer.EntitiesWritten;

                // assert
                testResult.Should().Be(2);
            }
        }

        [Fact]
        public void FieldsWritten_AfterAMessage_CountsTheFieldsAdded()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]);
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                writer.WriteMessage(source);

                // act
                var testResult = writer.FieldsWritten;

                // assert
                testResult.Should().Be(2);
            }
        }

        [Fact]
        public void BytePosition_AfterAMessage_CountsItsBytes()
        {
            // arrange
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                writer.WriteMessage(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)));

                // act
                var testResult = writer.BytePosition;

                // assert
                testResult.Should().Be(1);
            }
        }

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // act
            Action testAction = () => new ImfEntityWriter((Stream)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("messageStream");
        }

        [Fact]
        public void Constructor_NullOptions_Throws()
        {
            // act
            Action testAction = () => new ImfEntityWriter(new MemoryStream(), null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Fact]
        public void Constructor_StreamThatCannotBeWritten_Throws()
        {
            // act
            Action testAction = () => new ImfEntityWriter(new ReadOnlyMemoryStream());

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("messageStream");
        }

        [Fact]
        public void Constructor_NestingLimitBelowOne_Throws()
        {
            // act
            Action testAction = () => new ImfEntityWriter(new MemoryStream(), new ImfOptions { MaxNestingDepth = 0 });

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("options");
        }

        [Fact]
        public void Constructor_PartLimitBelowOne_Throws()
        {
            // act
            Action testAction = () => new ImfEntityWriter(new MemoryStream(), new ImfOptions { MaxPartCount = 0 });

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("options");
        }

        [Fact]
        public void WriteMessage_NullMessage_Throws()
        {
            // arrange
            using (var writer = MessageWrites.CreateWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => writer.WriteMessage(null);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("message");
            }
        }

        [Theory]
        [MemberData(nameof(DisposeGuardedMembers))]
        public void Member_DisposedWriter_Throws(Action<ImfEntityWriter> member)
        {
            // arrange
            var writer = MessageWrites.CreateWriter(new MemoryStream());
            writer.Dispose();

            // act
            Action testAction = () => member(writer);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_Twice_DoesNotThrow()
        {
            // arrange
            var writer = MessageWrites.CreateWriter(new MemoryStream());
            writer.Dispose();

            // act
            Action testAction = () => writer.Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void Dispose_CallerStream_LeavesItOpen()
        {
            // arrange
            var stream = new MemoryStream();
            var writer = MessageWrites.CreateWriter(stream);

            // act
            writer.Dispose();

            // assert
            stream.CanWrite.Should().BeTrue();
        }

        public static TheoryData<Action> InvalidSourceCalls => new TheoryData<Action>
        {
            () => ImfEntitySource.FromContent(default(MimeMediaType), new byte[0]),
            () => ImfEntitySource.FromContent(default(MimeMediaType), new MemoryStream()),
            () => ImfEntitySource.FromContent(MimeMediaType.TextPlain, new WriteOnlyStream()),
            () => ImfEntitySource.FromParts(default(MimeMediaType), new ImfEntitySource[0]),
            () => ImfEntitySource.FromParts(MimeMediaType.TextPlain, new[] { ImfEntitySource.FromRaw(new byte[0]) }),
            () => ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new ImfEntitySource[0]),
            () => ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new ImfEntitySource[] { null }),
            () => ImfEntitySource.FromMessage(MimeMediaType.TextPlain, ImfEntitySource.FromRaw(new byte[0])),
            () => ImfEntitySource.FromMessage(MessagePartial, ImfEntitySource.FromRaw(new byte[0])),
            () => ImfEntitySource.FromRaw(new WriteOnlyStream()),
        };

        [Theory]
        [MemberData(nameof(InvalidSourceCalls))]
        public void EntitySource_InvalidArguments_Throw(Action call)
        {
            // act
            Action testAction = call;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        public static TheoryData<Action<ImfEntitySource>> CallsNotAllowedOnARawEntity => new TheoryData<Action<ImfEntitySource>>
        {
            source => source.AddField("A", "1", ImfHeaderFieldKind.Unstructured),
            source => source.AddField(default(ImfHeaderFieldView)),
            source => source.AddRawField(Octets.Of("A: 1\r\n")),
            source => source.TransferEncoding = MimeTransferEncoding.Base64,
            source => source.Preamble = new byte[0],
            source => source.Epilogue = new byte[0],
        };

        [Theory]
        [MemberData(nameof(CallsNotAllowedOnARawEntity))]
        public void EntitySource_CallNotAllowedOnARawEntity_Throws(Action<ImfEntitySource> call)
        {
            // arrange
            var source = ImfEntitySource.FromRaw(new byte[0]);

            // act
            Action testAction = () => call(source);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AddField_ViewFromAReader_CopiesTheBytes()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]);
            using (var reader = HeaderSections.CreateReader("A :  1\r\n 2\r\n"))
            {
                source.AddField(reader.ReadField().Value);
            }

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be("A :  1\r\n 2\r\nMIME-Version: 1.0\r\nContent-Type: text/plain\r\n\r\n");
        }

        [Fact]
        public void AddField_NullName_Throws()
        {
            // arrange
            var source = ImfEntitySource.FromRaw(new byte[0]);

            // act
            Action testAction = () => source.AddField(null, "1", ImfHeaderFieldKind.Unstructured);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("name");
        }

        [Fact]
        public void AddField_NullValue_Throws()
        {
            // arrange
            var source = ImfEntitySource.FromRaw(new byte[0]);

            // act
            Action testAction = () => source.AddField("A", null, ImfHeaderFieldKind.Unstructured);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("value");
        }

        [Fact]
        public void AddField_KindNotAMember_Throws()
        {
            // arrange
            var source = ImfEntitySource.FromRaw(new byte[0]);

            // act
            Action testAction = () => source.AddField("A", "1", (ImfHeaderFieldKind)2);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("kind");
        }

        [Fact]
        public void AddField_LoneSurrogate_Throws()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]);

            // act
            Action testAction = () => source.AddField("A", "\xD800", ImfHeaderFieldKind.Unstructured);

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        private static string EncodedLongLine()
        {
            // 999 characters at 75 to a line and a soft line break, then what is left, 24.
            var encoded = new System.Text.StringBuilder();
            for (var line = 0; line < 13; line++)
            {
                encoded.Append(EncodedStartOfLineOverLongest);
            }

            encoded.Append(new string('a', 24));
            return encoded.ToString();
        }

        private static ImfEntitySource WithTransferEncoding(MimeTransferEncoding encoding)
        {
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[0]);
            source.TransferEncoding = encoding;
            return source;
        }

        private static byte[] BuildAllByteValues()
        {
            var bytes = new byte[256];
            for (var index = 0; index < bytes.Length; index++)
            {
                bytes[index] = (byte)index;
            }

            return bytes;
        }

        [Fact]
        public void Write_AddedContentTypeWithMalformedParameter_IsRejected()
        {
            // arrange
            var options = new ImfOptions { AllowMalformedParameters = true };
            var source = ImfEntitySource.FromContent(ImfMimeFields.ParseContentType(MalformedContentType, options), new byte[0])
                .AddRawField(Octets.Of(MalformedContentTypeField));

            // act
            Action testAction = () => MessageWrites.Write(source);

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void Write_AddedContentTypeWithMalformedParameterAndAllowMalformedParameters_CopiesIt()
        {
            // arrange
            var options = new ImfOptions { AllowMalformedParameters = true };
            var source = ImfEntitySource.FromContent(ImfMimeFields.ParseContentType(MalformedContentType, options), new byte[0])
                .AddRawField(Octets.Of(MalformedContentTypeField));

            // act
            var testResult = MessageWrites.Write(source, options);

            // assert
            testResult.Should().Contain(MalformedContentTypeField);
        }
    }
}
