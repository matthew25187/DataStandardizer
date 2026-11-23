using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="MboxWriter"/>.
    /// </summary>
    public class MboxWriterTests
    {
        private const string SeparatorLine = "From a@b.test Thu Jan  1 00:00:00 2026";
        private const string Separator = SeparatorLine + "\n";
        private const string Header = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\n";
        private const string CrLfHeader = "Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom: a@b.test\r\n";
        private const string Message = Header + "\none\n";
        private const string Eml = CrLfHeader + "\r\none\r\n";

        // A body with lines that each variant treats differently, of 43 octets as it stands, and 44 with the quote that mboxo adds to its first line.
        private const string QuotableBody = "From here\n>From there\n>>From far\n\nnot From\n";

        // ref. PNG (ISO/IEC 15948) §5.2: the PNG signature, which holds CRLF, LF and control octets, followed by more of each, as content that is not text.
        private static readonly byte[] AttachmentWithLineBreaks = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01, 0x0A, 0x0D, 0x0A, 0xFF };

        private const string BinaryEml = "Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\nContent-Transfer-Encoding: binary\r\n\r\nx\r\n";

        private const string CrLfSeparator = SeparatorLine + "\r\n";
        private const string BinaryInCrLfFile = CrLfSeparator + BinaryEml + "\r\n";
        private const string BinaryInLineFeedFile = Separator + Header + "MIME-Version: 1.0\nContent-Transfer-Encoding: binary\n\nx\n\n";
        private const string EmlInCrLfFile = CrLfSeparator + Eml + "\r\n";
        private const string MessageInFile = Separator + Message + "\n";
        private const string HeaderOnlyInFile = Separator + Header + "\n";
        private const string BuiltTextPart = "hi";
        private const string TextMediaType = "text/plain";
        private const string SevenBitText = "x";
        private const string EightBitText = "café";
        private const string MaxPartCountMessagePattern = "*ImfOptions.MaxPartCount*";

        // A folded field, with the line breaks of a mailbox and of a standalone message (RFC 5322 §2.1), and messages that each hold the one from the other.
        private const string BuiltMessageWithAddedSubject = CrLfHeader + CrLfSubject + "MIME-Version: 1.0\r\nContent-Type: text/plain; charset=UTF-8\r\n\r\n" + BuiltTextPart;
        private const string SourceWithAddedSubject = CrLfSubject + "MIME-Version: 1.0\r\nContent-Type: text/plain\r\n\r\n" + SevenBitText;
        private const string LineFeedSubject = "Subject: in\n ner\n";
        private const string CrLfSubject = "Subject: in\r\n ner\r\n";
        private const string SubjectInFile = Separator + LineFeedSubject + Header + "\none\n\n";
        private const string SubjectEml = CrLfSubject + CrLfHeader + "\r\none\r\n";
        private const string EmlWithAddedSubject = CrLfHeader + CrLfSubject + "\r\none\r\n";
        private const string FileWithAddedSubject = Separator + Header + LineFeedSubject + "\none\n\n";
        private const string NewMessageWithAddedSubject = CrLfHeader + CrLfSubject + "\r\n";
        private const int FirstFieldIndex = 0;

        // A message that forwards a two-part multipart: its text part, the part that holds the message, that message's multipart body, and the two parts of that body.
        private const int ForwardingEntityCount = 5;
        private const int OneFewerThanForwardingEntityCount = 4;
        private const string MultipartInFile = Separator + Header + "MIME-Version: 1.0\nContent-Type: multipart/mixed; boundary=b\n\n--b\n\nx\n--b--\n\n";
        private const string AttachmentFileName = "a.png";
        private const string AttachmentMediaType = "image/png";
        private const int AttachmentPartIndex = 1;

        // The attachment of a message built with no text part, which is the first part.
        private const int OnlyAttachmentPartIndex = 0;
        private const string BuiltDate = "Thu, 1 Jan 2026 00:00:00 +0000";
        private const string BuiltFrom = "a@b.test";

        // ref. RFC 3501 §6.4.5: the parts of a multipart message body are numbered from 1.
        private const string FirstPartPath = "1";
        private const string SecondPartPath = "2";

        // ref. RFC 3501 §6.4.5: the second part of a message held by the second part of a multipart message, and of one that is the body of a message.
        private const string HeldSecondPartPath = "2.2";
        private const string BodyMessageSecondPartPath = "1.2";
        private const string DataFileName = "a.bin";
        private const string DataMediaType = "application/octet-stream";

        // Content of seven-bit octets whose LF is data, as it isn't text (RFC 2046 §4.1.1), so 7bit would cover it in a file of LF if the line breaks didn't change.
        private static readonly byte[] SevenBitDataWithLineFeed = { 0x61, 0x0A, 0x62 };

        // Content of seven-bit octets whose CRLF is data, as it isn't text (RFC 2046 §4.1.1), so 7bit would cover it if the line breaks didn't change.
        private static readonly byte[] SevenBitDataWithCrLf = { 0x61, 0x0D, 0x0A, 0x62 };
        private const string SecondMessageInFile = Separator + Header + "\ntwo\n\n";
        private const string TwoDistinctMessagesInFile = MessageInFile + SecondMessageInFile;
        private const string ExtensionEml = CrLfHeader + "MIME-Version: 1.0\r\nContent-Transfer-Encoding: x-private\r\n\r\nx\r\n";
        private const string ExtensionInFile = Separator + Header + "MIME-Version: 1.0\nContent-Transfer-Encoding: x-private\n\nx\n\n";

        private const string MboxoBody = ">From here\n>From there\n>>From far\n\nnot From\n";
        private const string MboxrdFile = Separator + Header + "\n>From here\n>>From there\n>>>From far\n\nnot From\n\n";
        private const string MboxoFile = Separator + Header + "\n" + MboxoBody + "\n";
        private const string MboxclFile = Separator + MboxclMessage + "\n";
        private const string Mboxcl2File = Separator + Mboxcl2Message + "\n";
        private const string MboxrdMessage = Header + "\n" + QuotableBody;
        private const string MboxoMessage = Header + "\n" + MboxoBody;
        private const string MboxclMessage = Header + "Content-Length: 44\n\n" + MboxoBody;
        private const string Mboxcl2Message = Header + "Content-Length: 43\n\n" + QuotableBody;

        private const string QuotedAndEmptyLineFile = Separator + Header + "\n>From here\n\nx\n\n";
        private const string RightLengthFile = Separator + "Content-Length: 4\n" + Header + "\none\n\n";
        private const string FoldedLengthEml = "Content-Length:\r\n 99\r\n" + CrLfHeader + "\r\none\r\n";
        private const string TwoLengthsEml = "Content-Length: 4\r\nContent-Length: 4\r\n" + CrLfHeader + "\r\none\r\n";
        private const string WrongLengthEml = "Content-Length: 99\r\n" + CrLfHeader + "\r\none\r\n";
        private const string OneLengthFile = Separator + Header + "Content-Length: 4\n\none\n\n";
        private const string WrongLengthKeptFile = Separator + "Content-Length: 99\n" + Header + "\none\n\n";
        private const string ZeroLengthFile = Separator + Header + "Content-Length: 0\n\n\n";
        private const string ObsoleteFromEml = "Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom : a@b.test\r\n\r\none\r\n";
        private const string ObsoleteFromMessage = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom : a@b.test\n\none\n";
        private const string LastLineEml = CrLfHeader + "\r\nlast";
        private const string LastLineFile = Separator + Header + "\nlast\n\n";
        private const string LastLineLengthMessage = Header + "Content-Length: 4\n\nlast";
        private const string CrLfLengthMessage = CrLfHeader + "Content-Length: 5\r\n\r\none\r\n";
        private const string CrLfLengthFile = CrLfSeparator + CrLfLengthMessage + "\r\n";
        private const string ThunderbirdFile = "From - Thu Jan  1 00:00:00 2026\n" + Message + "\n";
        private const string NoRequiredFieldsEml = "Subject: x\r\n\r\ny\r\n";
        private const string BuiltText = "hello\nFrom here\n";
        private const string ContentLengthName = "Content-Length";

        // The length of a file of one message: 39 of the separator line, 57 of the message and 1 of the empty line.
        private const long MessageFileLength = 97;

        // 39 of the separator line, 52 of the two fields, 18 of the Content-Length field, 1 of the empty line, 4 of the body and 1 of the empty line that ends it.
        private const long CountedFileLength = 115;
        private const long CountedFieldCount = 3;

        // 56 of the header and its empty line, and 50 lines of 998 octets and a CRLF.
        private const long LargeEmlLength = 50056;
        private const long TwoFieldsWritten = 2;
        private const long BlockSize = 16 * 1024;
        private const int LargeLineLength = 998;
        private const int LargeLineCount = 50;
        private const long TwoMessagesFileLength = 194;
        private const int TwoWritten = 2;
        private const long FirstIndex = 0;

        private const string EightBitEml = CrLfHeader + "MIME-Version: 1.0\r\nContent-Transfer-Encoding: 8bit\r\n\r\ncaf\u00E9\r\n";
        private const string EightBitInFile = Separator + Header + "MIME-Version: 1.0\nContent-Transfer-Encoding: 8bit\n\ncaf\u00E9\n\n";
        private const string CrLfLineInFile = Separator + Header + "\none\r\n\n";

        private static readonly string LargeInFile = Separator + Header + "\n" + string.Concat(Enumerable.Repeat(new string('x', LargeLineLength) + "\n", LargeLineCount)) + "\n";

        private static readonly ImfOptions AllowEightBit = new ImfOptions { AllowEightBitMboxContent = true };
        private static readonly ImfOptions AllowCrLf = new ImfOptions { AllowMboxCarriageReturnLineFeed = true };
        private static readonly ImfOptions AllowMissingFields = new ImfOptions { AllowMissingRequiredFields = true };
        private static readonly ImfOptions AllowSeparator = new ImfOptions { AllowInvalidMboxSeparator = true };

        private static readonly DateTimeOffset FirstOfJanuary = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly MboxEnvelope Envelope = new MboxEnvelope(new ImfMailbox("a", "b.test"), FirstOfJanuary);

        // ref. RFC 5322 §2.1.1: a line is at most 998 octets, of which "From " takes 5, so a quote takes this one to 999.
        private const int LongestFromLinePadding = 993;
        private const int ShorterFromLinePadding = 992;
        private const int LongLineLength = 999;

        // The padding that takes a line of "From : ", 7 octets, and "@b.test", 7, to 998.
        private const int LongestFromFieldPadding = 984;
        private static readonly string LongestFromLine = "From " + new string('x', LongestFromLinePadding);
        private static readonly string ShorterFromLine = "From " + new string('x', ShorterFromLinePadding);
        private static readonly string LongestFromLineEml = CrLfHeader + "\r\n" + LongestFromLine + "\r\n";
        private static readonly string ShorterFromLineEml = CrLfHeader + "\r\n" + ShorterFromLine + "\r\n";
        private static readonly string LongestFromLineBinaryEml = CrLfHeader + "MIME-Version: 1.0\r\nContent-Transfer-Encoding: binary\r\n\r\n" + LongestFromLine + "\r\n";

        private static readonly string LongestFromLineBesideBinaryEml = CrLfHeader + "MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=b\r\nContent-Transfer-Encoding: binary\r\n\r\n--b\r\nContent-Transfer-Encoding: binary\r\n\r\nx\r\n--b\r\n\r\n" + LongestFromLine + "\r\n--b--\r\n";
        private static readonly string LongestFromFieldEml = "Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom : " + new string('x', LongestFromFieldPadding) + "@b.test\r\n\r\none\r\n";

        private static readonly string LongLineThenLongestFromLineEml = CrLfHeader + "MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\n" + new string('y', LongLineLength) + "\r\n--b\r\n\r\n" + LongestFromLine + "\r\n--b--\r\n";
        private static readonly string LongHeaderLineEml = CrLfHeader + "X-Long: " + new string('y', LongLineLength) + "\r\n\r\none\r\n";

        // A multipart body whose part holds a long line, or a line that a quote takes past the limit, with no close delimiter, which only AllowMissingBoundaryTerminator accepts (RFC 2046 §5.1.1),
        // and a part with the line and the delimiter.
        private const string MultipartEmlStart = CrLfHeader + "MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\n";
        private const string NoCloseDelimiterMessagePattern = "*no close delimiter*";
        private static readonly string LongLineWithNoCloseDelimiterEml = MultipartEmlStart + new string('y', LongLineLength) + "\r\n";
        private static readonly string LongestFromLineWithNoCloseDelimiterEml = MultipartEmlStart + LongestFromLine + "\r\n";
        private static readonly string LongestFromLineInPartEml = MultipartEmlStart + LongestFromLine + "\r\n--b--\r\n";
        private static readonly ImfOptions AllowNoCloseDelimiterAndLongLines = new ImfOptions { AllowMissingBoundaryTerminator = true, AllowLongLines = true };
        private const string ShortLineWithNoCloseDelimiterEml = MultipartEmlStart + "short\r\n";
        private const string TopLevelPath = "";

        // The same multipart body with no close delimiter, whose part holds a long line, a line that a quote takes past the limit, or neither.
        public static TheoryData<string> NoCloseDelimiterMessages => new TheoryData<string>
        {
            LongLineWithNoCloseDelimiterEml,
            LongestFromLineWithNoCloseDelimiterEml,
            ShortLineWithNoCloseDelimiterEml,
        };

        // The same messages, and what a writer that accepts both records for each: the delimiter for the multipart body, and a long line or a quoted line for its part.
        public static TheoryData<string, ImfDiagnosticKind[], string[]> NoCloseDelimiterMessagesWithOption => new TheoryData<string, ImfDiagnosticKind[], string[]>
        {
            { LongLineWithNoCloseDelimiterEml, new[] { ImfDiagnosticKind.InvalidBodyStructure, ImfDiagnosticKind.LongBodyLine }, new[] { TopLevelPath, FirstPartPath } },
            { LongestFromLineWithNoCloseDelimiterEml, new[] { ImfDiagnosticKind.InvalidBodyStructure, ImfDiagnosticKind.LongBodyLine }, new[] { TopLevelPath, FirstPartPath } },
            { ShortLineWithNoCloseDelimiterEml, new[] { ImfDiagnosticKind.InvalidBodyStructure }, new[] { TopLevelPath } },
        };

        public static TheoryData<MboxVariant> QuotingVariants => new TheoryData<MboxVariant> { MboxVariant.Mboxrd, MboxVariant.Mboxo, MboxVariant.Mboxcl };

        [Theory]
        [MemberData(nameof(QuotingVariants))]
        public void WriteMessage_QuoteTakesALinePastTheLimit_ThrowsByDefaultAndWritesNothing(MboxVariant variant)
        {
            // arrange
            var message = Mailboxes.ReadMessage(LongestFromLineEml);
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output, variant, new ImfOptions()))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.LongBodyLine);
                output.Length.Should().Be(0);
            }
        }

        [Theory]
        [MemberData(nameof(QuotingVariants))]
        public void WriteMessage_QuoteTakesALinePastTheLimitWithOption_RecordsADiagnosticAndWritesIt(MboxVariant variant)
        {
            // arrange
            var message = Mailboxes.ReadMessage(LongestFromLineEml);
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output, variant, new ImfOptions { AllowLongLines = true }))
            {
                // act
                subject.WriteMessage(Envelope, message);

                // assert
                subject.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.LongBodyLine);
                Octets.ToText(output.ToArray()).Should().Contain(">" + LongestFromLine + "\n");
            }
        }

        [Fact]
        public void WriteMessage_QuoteTakesALineBesideABinaryEntityPastTheLimit_Throws()
        {
            // arrange
            var message = Mailboxes.ReadMessage(LongestFromLineBesideBinaryEml);
            using (var subject = new MboxWriter(new MemoryStream(), MboxVariant.Mboxrd, new ImfOptions(), MboxLineBreak.CarriageReturnLineFeed))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.LongBodyLine);
            }
        }

        [Fact]
        public void WriteMessage_QuoteTakesALineOfTheHeaderPastTheLimit_Throws()
        {
            // arrange
            var message = Mailboxes.ReadMessage(LongestFromFieldEml);
            using (var subject = new MboxWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.LongBodyLine);
            }
        }

        [Fact]
        public void WriteMessage_LongLineInOneBodyAndAQuoteInAnotherWithOption_RecordsEach()
        {
            // arrange
            var options = new ImfOptions { AllowLongLines = true };
            var message = Mailboxes.ReadMessage(LongLineThenLongestFromLineEml, options);
            using (var subject = new MboxWriter(new MemoryStream(), MboxVariant.Mboxrd, options))
            {
                // act
                subject.WriteMessage(Envelope, message);

                // assert
                subject.Diagnostics.Select(diagnostic => diagnostic.EntityPath).Should().Equal("1", "2");
            }
        }

        [Fact]
        public void WriteMessage_LongLineOfAHeaderThatWasRead_Throws()
        {
            // arrange
            var message = Mailboxes.ReadMessage(LongHeaderLineEml, new ImfOptions { AllowLongLines = true });
            using (var subject = new MboxWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                testAction.Should().Throw<ImfFileException>();
            }
        }

        [Theory]
        [MemberData(nameof(NoCloseDelimiterMessages))]
        public void WriteMessage_UnchangedMultipartWithNoCloseDelimiter_ThrowsForTheDelimiterWhateverItsLines(string eml)
        {
            // arrange
            var message = Mailboxes.ReadMessage(eml, AllowNoCloseDelimiterAndLongLines);
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output, MboxVariant.Mboxrd, new ImfOptions { AllowLongLines = true }))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                var exception = testAction.Should().Throw<ImfFileException>().WithMessage(NoCloseDelimiterMessagePattern).Which;
                exception.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidBodyStructure);
                exception.Data[ImfFileException.DataItemName.EntityPath].Should().Be(TopLevelPath);
                output.Length.Should().Be(0);
            }
        }

        [Theory]
        [MemberData(nameof(NoCloseDelimiterMessagesWithOption))]
        public void WriteMessage_UnchangedMultipartWithNoCloseDelimiterAllowed_RecordsTheDelimiterWhateverItsLines(string eml, ImfDiagnosticKind[] expectedKinds, string[] expectedPaths)
        {
            // arrange
            var message = Mailboxes.ReadMessage(eml, AllowNoCloseDelimiterAndLongLines);
            using (var subject = new MboxWriter(new MemoryStream(), MboxVariant.Mboxrd, AllowNoCloseDelimiterAndLongLines))
            {
                // act
                subject.WriteMessage(Envelope, message);

                // assert
                subject.MessagesWritten.Should().Be(1);
                subject.Diagnostics.Select(diagnostic => diagnostic.Kind).Should().Equal(expectedKinds);
                subject.Diagnostics.Select(diagnostic => diagnostic.EntityPath).Should().Equal(expectedPaths);
            }
        }

        [Fact]
        public void WriteMessage_QuoteTakesALineOfAPartPastTheLimit_ThrowsWithItsEntityPath()
        {
            // arrange
            var message = Mailboxes.ReadMessage(LongestFromLineInPartEml);
            using (var subject = new MboxWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                var exception = testAction.Should().Throw<ImfFileException>().Which;
                exception.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.LongBodyLine);
                exception.Data[ImfFileException.DataItemName.EntityPath].Should().Be(FirstPartPath);
            }
        }

        [Fact]
        public void WriteMessage_QuotedLineThatFits_WritesIt()
        {
            // arrange
            var message = Mailboxes.ReadMessage(ShorterFromLineEml);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message);

            // assert
            testResult.Should().Contain(">" + ShorterFromLine + "\n");
        }

        [Fact]
        public void WriteMessage_LongestLineInMboxcl2_WritesItWithNoQuote()
        {
            // arrange
            var message = Mailboxes.ReadMessage(LongestFromLineEml);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message, MboxVariant.Mboxcl2);

            // assert
            testResult.Should().Contain("\n" + LongestFromLine + "\n");
        }

        [Fact]
        public void WriteMessage_QuoteTakesALineOfABinaryEntityPastTheLimit_WritesIt()
        {
            // arrange
            // ref. RFC 2045 §2.9: binary data has no lines.
            var message = Mailboxes.ReadMessage(LongestFromLineBinaryEml);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message, MboxVariant.Mboxrd, MboxLineBreak.CarriageReturnLineFeed);

            // assert
            testResult.Should().Contain(">" + LongestFromLine + "\r\n");
        }

        [Theory]
        [InlineData(MboxVariant.Mboxrd, MboxrdFile)]
        [InlineData(MboxVariant.Mboxo, MboxoFile)]
        [InlineData(MboxVariant.Mboxcl, MboxclFile)]
        [InlineData(MboxVariant.Mboxcl2, Mboxcl2File)]
        public void WriteMessage_EachVariant_QuotesAndCountsAsItsRulesSay(MboxVariant variant, string expected)
        {
            // arrange
            var entry = Mailboxes.ReadFirst(MboxrdFile);

            // act
            var testResult = Mailboxes.WriteOne(entry.Envelope, entry.Message, variant);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(MboxVariant.Mboxrd, MboxrdMessage)]
        [InlineData(MboxVariant.Mboxo, MboxoMessage)]
        [InlineData(MboxVariant.Mboxcl, MboxclMessage)]
        [InlineData(MboxVariant.Mboxcl2, Mboxcl2Message)]
        public void RoundTrip_EachVariant_OnlyMboxrdAndMboxcl2KeepTheBody(MboxVariant variant, string expected)
        {
            // act
            var testResult = Mailboxes.RoundTrip(MboxrdFile, variant);

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void WriteMessage_Mboxrd_WritesAMailboxByteForByte()
        {
            // arrange
            var entry = Mailboxes.ReadFirst(QuotedAndEmptyLineFile);

            // act
            var testResult = Mailboxes.WriteOne(entry.Envelope, entry.Message);

            // assert
            testResult.Should().Be(QuotedAndEmptyLineFile);
        }

        [Fact]
        public void WriteMessage_Mboxcl2WithRightLength_WritesAMailboxByteForByte()
        {
            // arrange
            var entry = Mailboxes.ReadFirst(RightLengthFile, MboxVariant.Mboxcl2);

            // act
            var testResult = Mailboxes.WriteOne(entry.Envelope, entry.Message, MboxVariant.Mboxcl2);

            // assert
            testResult.Should().Be(RightLengthFile);
        }

        [Fact]
        public void WriteMessage_WrongContentLengthFolded_ReplacesIt()
        {
            // arrange
            var message = Mailboxes.ReadMessage(FoldedLengthEml);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message, MboxVariant.Mboxcl2);

            // assert
            testResult.Should().Be(OneLengthFile);
        }

        [Fact]
        public void WriteMessage_TwoContentLengthFields_LeavesOne()
        {
            // arrange
            var message = Mailboxes.ReadMessage(TwoLengthsEml);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message, MboxVariant.Mboxcl2);

            // assert
            testResult.Should().Be(OneLengthFile);
        }

        [Fact]
        public void WriteMessage_ContentLengthInAVariantThatDoesNotUseIt_LeavesIt()
        {
            // arrange
            var message = Mailboxes.ReadMessage(WrongLengthEml);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message);

            // assert
            testResult.Should().Be(WrongLengthKeptFile);
        }

        [Fact]
        public void WriteMessage_ContentLengthVariant_DoesNotChangeTheMessage()
        {
            // arrange
            var message = Mailboxes.ReadMessage(Eml);

            // act
            Mailboxes.WriteOne(Envelope, message, MboxVariant.Mboxcl2);

            // assert
            message.Fields.Contains(ContentLengthName).Should().BeFalse();
        }

        [Fact]
        public void WriteMessage_HeaderWithNoEmptyLineInContentLengthVariant_GetsOne()
        {
            // arrange
            var message = Mailboxes.ReadMessage(CrLfHeader);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message, MboxVariant.Mboxcl2);

            // assert
            testResult.Should().Be(ZeroLengthFile);
        }

        [Fact]
        public void WriteMessage_HeaderWithNoEmptyLine_ReadsBackTheSame()
        {
            // arrange
            var message = Mailboxes.ReadMessage(CrLfHeader);
            var written = Mailboxes.WriteOne(Envelope, message);

            // act
            var testResult = Mailboxes.ReadAll(written);

            // assert
            testResult.Should().Equal(Header);
        }

        [Fact]
        public void WriteMessage_ObsoleteFromField_IsQuotedAndReadsBack()
        {
            // arrange
            var written = Mailboxes.WriteOne(Envelope, Mailboxes.ReadMessage(ObsoleteFromEml));

            // act
            var testResult = Mailboxes.ReadAll(written);

            // assert
            testResult.Should().Equal(ObsoleteFromMessage);
        }

        [Fact]
        public void WriteMessage_LastLineHasNoLineBreak_AddsOne()
        {
            // arrange
            var message = Mailboxes.ReadMessage(LastLineEml);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message);

            // assert
            testResult.Should().Be(LastLineFile);
        }

        [Fact]
        public void WriteMessage_LastLineHasNoLineBreakInContentLengthVariant_DoesNotCountIt()
        {
            // arrange
            var message = Mailboxes.ReadMessage(LastLineEml);
            var written = Mailboxes.WriteOne(Envelope, message, MboxVariant.Mboxcl2);

            // act
            var testResult = Mailboxes.ReadAll(written, MboxVariant.Mboxcl2);

            // assert
            testResult.Should().Equal(LastLineLengthMessage);
        }

        [Fact]
        public void WriteMessage_MessageFromAnEmlFile_ChangesItsLineBreaks()
        {
            // arrange
            var message = Mailboxes.ReadMessage(Eml);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message);

            // assert
            testResult.Should().Be(MessageInFile);
        }

        [Fact]
        public void WriteMessage_CrLfFile_WritesThoseLineBreaks()
        {
            // arrange
            var message = Mailboxes.ReadMessage(Eml);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message, MboxVariant.Mboxrd, MboxLineBreak.CarriageReturnLineFeed);

            // assert
            testResult.Should().Be(EmlInCrLfFile);
        }

        [Fact]
        public void WriteMessage_MessageFromALineFeedFileToACrLfFile_ChangesItsLineBreaks()
        {
            // arrange
            var entry = Mailboxes.ReadFirst(MessageInFile);

            // act
            var testResult = Mailboxes.WriteOne(entry.Envelope, entry.Message, MboxVariant.Mboxrd, MboxLineBreak.CarriageReturnLineFeed);

            // assert
            testResult.Should().Be(EmlInCrLfFile);
        }

        [Fact]
        public void WriteMessage_CrLfFileContentLength_CountsThoseLineBreaks()
        {
            // arrange
            var message = Mailboxes.ReadMessage(Eml);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message, MboxVariant.Mboxcl2, MboxLineBreak.CarriageReturnLineFeed);

            // assert
            testResult.Should().Be(CrLfLengthFile);
        }

        [Fact]
        public void WriteMessage_CrLfFileContentLength_ReadsBack()
        {
            // arrange
            var written = Mailboxes.WriteOne(Envelope, Mailboxes.ReadMessage(Eml), MboxVariant.Mboxcl2, MboxLineBreak.CarriageReturnLineFeed);

            // act
            var testResult = Mailboxes.ReadAll(written, MboxVariant.Mboxcl2, AllowCrLf);

            // assert
            testResult.Should().Equal(CrLfLengthMessage);
        }

        [Fact]
        public void WriteMessage_BinaryEntityThatHasToChange_ThrowsAndWritesNothing()
        {
            // arrange
            var message = Mailboxes.ReadMessage(BinaryEml);
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                testAction.Should().Throw<ImfFileException>();
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_BinaryEntityThatNeedNotChange_WritesIt()
        {
            // arrange
            var message = Mailboxes.ReadMessage(BinaryEml);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message, MboxVariant.Mboxrd, MboxLineBreak.CarriageReturnLineFeed);

            // assert
            testResult.Should().Be(BinaryInCrLfFile);
        }

        [Fact]
        public void WriteMessage_BuiltMessage_ReadsBackWithTheSameText()
        {
            // arrange
            var builder = new ImfMessageBuilder().SetText(BuiltText);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            var written = Mailboxes.WriteOne(Envelope, builder.Build());

            // act
            var testResult = Mailboxes.ReadFirst(written);

            // assert
            testResult.Message.Body.GetText().Should().Be(BuiltText);
        }

        [Fact]
        public void WriteMessage_BuiltAttachmentWithBinaryAllowed_ReadsBackWithTheSameBytes()
        {
            // arrange
            var options = new ImfOptions { AllowBinaryTransferEncoding = true };
            var builder = new ImfMessageBuilder(options).SetText("hi").AddAttachment(AttachmentFileName, MimeMediaType.Parse(AttachmentMediaType), AttachmentWithLineBreaks);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            var written = Mailboxes.WriteOne(Envelope, builder.Build(), options: options);
            var body = (ImfMultipart)Mailboxes.ReadFirst(written, options: options).Message.Body;

            // act
            var testResult = body.Parts[AttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(AttachmentWithLineBreaks);
        }

        [Fact]
        public void WriteMessage_ImfWriterWithABinaryPartAddedToAMessageFromAMailbox_ReadsBackWithTheSameBytes()
        {
            // arrange
            var options = new ImfOptions { AllowBinaryTransferEncoding = true };
            var message = Mailboxes.ReadFirst(MultipartInFile, options: options).Message;
            var part = new ImfEntity(options) { ContentType = MimeMediaType.Parse(AttachmentMediaType) };
            part.SetContent(AttachmentWithLineBreaks);
            ((ImfMultipart)message.Body).Parts.Add(part);
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output, options))
            {
                writer.WriteMessage(message);
            }

            var body = (ImfMultipart)Mailboxes.ReadMessage(Octets.ToText(output.ToArray()), options).Body;

            // act
            var testResult = body.Parts[AttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(AttachmentWithLineBreaks);
        }

        [Fact]
        public void WriteMessage_BuiltPartLabelledBinary_ThrowsAndWritesNothing()
        {
            // arrange
            var options = new ImfOptions { AllowBinaryTransferEncoding = true };
            var builder = new ImfMessageBuilder(options).AddAttachment(AttachmentFileName, MimeMediaType.Parse(AttachmentMediaType), AttachmentWithLineBreaks);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            var message = builder.Build();
            ((ImfMultipart)message.Body).Parts[OnlyAttachmentPartIndex].ContentTransferEncoding = MimeTransferEncoding.Binary;
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output, MboxVariant.Mboxrd, options))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be(FirstPartPath);
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAPartLabelledBinaryAddedToAMessageFromAMailbox_ThrowsAndWritesNothing()
        {
            // arrange
            var options = new ImfOptions { AllowBinaryTransferEncoding = true };
            var message = Mailboxes.ReadFirst(MultipartInFile, options: options).Message;
            var part = new ImfEntity(options) { ContentType = MimeMediaType.Parse(AttachmentMediaType) };
            part.SetContent(AttachmentWithLineBreaks);
            part.ContentTransferEncoding = MimeTransferEncoding.Binary;
            ((ImfMultipart)message.Body).Parts.Add(part);
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output, options))
            {
                // act
                Action testAction = () => subject.WriteMessage(message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be(SecondPartPath);
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_CrLfFileWithAPartOfSevenBitOctetsWithLineFeedAddedToAMessageFromAMailbox_ReadsBackWithTheSameBytes()
        {
            // arrange
            var message = Mailboxes.ReadFirst(MultipartInFile).Message;
            var part = new ImfEntity { ContentType = MimeMediaType.Parse(DataMediaType) };
            part.SetContent(SevenBitDataWithLineFeed);
            ((ImfMultipart)message.Body).Parts.Add(part);

            // act
            var written = Mailboxes.WriteOne(Envelope, message, MboxVariant.Mboxrd, MboxLineBreak.CarriageReturnLineFeed);
            var testResult = ((ImfMultipart)Mailboxes.ReadFirst(written, options: AllowCrLf).Message.Body).Parts[AttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(SevenBitDataWithLineFeed);
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAPartOfSevenBitOctetsWithLineFeedAddedToAMessageFromAMailbox_ReadsBackWithTheSameBytes()
        {
            // arrange
            var message = Mailboxes.ReadFirst(MultipartInFile).Message;
            var part = new ImfEntity { ContentType = MimeMediaType.Parse(DataMediaType) };
            part.SetContent(SevenBitDataWithLineFeed);
            ((ImfMultipart)message.Body).Parts.Add(part);

            // act
            var written = WriteStandalone(message);
            var testResult = ((ImfMultipart)Mailboxes.ReadMessage(written).Body).Parts[AttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(SevenBitDataWithLineFeed);
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAPartWithLineFeedLabelledSevenBitAddedToAMessageFromAMailbox_ThrowsAndWritesNothing()
        {
            // arrange
            var message = Mailboxes.ReadFirst(MultipartInFile).Message;
            var part = new ImfEntity { ContentType = MimeMediaType.Parse(DataMediaType) };
            part.SetContent(SevenBitDataWithLineFeed);
            part.ContentTransferEncoding = MimeTransferEncoding.SevenBit;
            ((ImfMultipart)message.Body).Parts.Add(part);
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output, new ImfOptions()))
            {
                // act
                Action testAction = () => subject.WriteMessage(message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be(SecondPartPath);
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_LineFeedFileWithABuiltAttachmentWithCrLfLabelledSevenBit_ThrowsAndWritesNothing()
        {
            // arrange
            var builder = new ImfMessageBuilder().AddAttachment(DataFileName, MimeMediaType.Parse(DataMediaType), SevenBitDataWithCrLf);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            var message = builder.Build();
            ((ImfMultipart)message.Body).Parts[OnlyAttachmentPartIndex].ContentTransferEncoding = MimeTransferEncoding.SevenBit;
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output, MboxVariant.Mboxrd, new ImfOptions()))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be(FirstPartPath);
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_LineFeedFileWithABuiltAttachmentOfSevenBitOctetsWithCrLf_ReadsBackWithTheSameBytes()
        {
            // arrange
            var builder = new ImfMessageBuilder().AddAttachment(DataFileName, MimeMediaType.Parse(DataMediaType), SevenBitDataWithCrLf);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            var message = builder.Build();

            // act
            var written = Mailboxes.WriteOne(Envelope, message);
            var testResult = ((ImfMultipart)Mailboxes.ReadFirst(written).Message.Body).Parts[OnlyAttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(SevenBitDataWithCrLf);
        }

        [Fact]
        public void WriteMessage_ImfWriterForwardingAMessageFromAMailboxWithAPartWithLineFeedLabelledSevenBit_ThrowsAndWritesNothing()
        {
            // arrange
            var options = new ImfOptions();
            var forwarded = Mailboxes.ReadFirst(MultipartInFile).Message;
            var part = new ImfEntity { ContentType = MimeMediaType.Parse(DataMediaType) };
            part.SetContent(SevenBitDataWithLineFeed);
            part.ContentTransferEncoding = MimeTransferEncoding.SevenBit;
            ((ImfMultipart)forwarded.Body).Parts.Add(part);
            var message = BuildForwarding(forwarded, options);
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output, options))
            {
                // act
                Action testAction = () => subject.WriteMessage(message);

                // assert
                testAction.Should().Throw<ImfFileException>();
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_ImfWriterForwardingAMessageFromAMailboxWithAPartWithLineFeedLabelledSevenBit_ThrowsWithThePathOfThePartInTheMessage()
        {
            // arrange
            var options = new ImfOptions();
            var forwarded = Mailboxes.ReadFirst(MultipartInFile).Message;
            var part = new ImfEntity { ContentType = MimeMediaType.Parse(DataMediaType) };
            part.SetContent(SevenBitDataWithLineFeed);
            part.ContentTransferEncoding = MimeTransferEncoding.SevenBit;
            ((ImfMultipart)forwarded.Body).Parts.Add(part);
            var message = BuildForwarding(forwarded, options);
            using (var subject = new ImfWriter(new MemoryStream(), options))
            {
                // act
                Action testAction = () => subject.WriteMessage(message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be(HeldSecondPartPath);
            }
        }

        [Fact]
        public void WriteMessage_ImfWriterWithABodyHoldingAMessageFromAMailboxWithAPartWithLineFeedLabelledSevenBit_ThrowsWithThePathOfThePartInTheMessage()
        {
            // arrange
            var forwarded = Mailboxes.ReadFirst(MultipartInFile).Message;
            var part = new ImfEntity { ContentType = MimeMediaType.Parse(DataMediaType) };
            part.SetContent(SevenBitDataWithLineFeed);
            part.ContentTransferEncoding = MimeTransferEncoding.SevenBit;
            ((ImfMultipart)forwarded.Body).Parts.Add(part);
            var message = Mailboxes.ReadMessage(Eml);
            message.Body = new ImfMessagePart(forwarded);
            using (var subject = new ImfWriter(new MemoryStream(), new ImfOptions()))
            {
                // act
                Action testAction = () => subject.WriteMessage(message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be(BodyMessageSecondPartPath);
            }
        }

        [Fact]
        public void WriteMessage_ImfWriterForwardingAMessageFromAMailboxWithAPartOfSevenBitOctetsWithLineFeed_ReadsBackWithTheSameBytes()
        {
            // arrange
            var options = new ImfOptions();
            var forwarded = Mailboxes.ReadFirst(MultipartInFile).Message;
            var part = new ImfEntity { ContentType = MimeMediaType.Parse(DataMediaType) };
            part.SetContent(SevenBitDataWithLineFeed);
            ((ImfMultipart)forwarded.Body).Parts.Add(part);
            var message = BuildForwarding(forwarded, options);

            // act
            var written = WriteStandalone(message, options);
            var held = (ImfMessagePart)((ImfMultipart)Mailboxes.ReadMessage(written).Body).Parts[AttachmentPartIndex];
            var testResult = ((ImfMultipart)held.Message.Body).Parts[AttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(SevenBitDataWithLineFeed);
        }

        [Theory]
        [InlineData(MessageInFile, Eml)]
        [InlineData(HeaderOnlyInFile, CrLfHeader)]
        public void WriteMessage_ImfWriterWithAMessageFromAMailboxAttached_WritesItWithCrLfLineBreaks(string mailbox, string expected)
        {
            // arrange
            var written = WriteForwarded(Mailboxes.ReadFirst(mailbox).Message, new ImfOptions());
            var body = (ImfMultipart)Mailboxes.ReadMessage(written).Body;

            // act
            var testResult = body.Parts[AttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(Octets.Of(expected));
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAMessageFromAMailboxAttachedAndBareLineFeedsAccepted_WritesItWithCrLfLineBreaks()
        {
            // arrange
            var options = new ImfOptions { AcceptBareLineFeed = true, AllowInvalidBodyOctets = true };
            var written = WriteForwarded(Mailboxes.ReadFirst(MessageInFile, options: options).Message, options);
            var body = (ImfMultipart)Mailboxes.ReadMessage(written).Body;

            // act
            var testResult = body.Parts[AttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(Octets.Of(Eml));
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAMessageFromAMailboxAttached_LabelsTheMultipartSevenBit()
        {
            // arrange
            var written = WriteForwarded(Mailboxes.ReadFirst(MessageInFile).Message, new ImfOptions());
            var message = Mailboxes.ReadMessage(written);

            // act
            var testResult = message.Body.ContentTransferEncoding;

            // assert
            testResult.Should().Be(MimeTransferEncoding.SevenBit);
        }

        [Fact]
        public void WriteMessage_ImfWriterWithABinaryMessageFromAMailboxAttached_ThrowsAndWritesNothing()
        {
            // arrange
            var message = BuildForwarding(Mailboxes.ReadFirst(BinaryInLineFeedFile).Message, new ImfOptions());
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output))
            {
                // act
                Action testAction = () => subject.WriteMessage(message);

                // assert
                testAction.Should().Throw<ImfFileException>().WithMessage(ImfLineBreaks.OpaqueEntityMessage);
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_ImfWriterWithABinaryMessageFromAMailboxAttached_ThrowsWithThePathOfItsBody()
        {
            // arrange
            var message = BuildForwarding(Mailboxes.ReadFirst(BinaryInLineFeedFile).Message, new ImfOptions());
            using (var subject = new ImfWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => subject.WriteMessage(message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be(SecondPartPath);
            }
        }

        [Fact]
        public void GetContent_PartHoldingAMessageFromAMailbox_GivesItWithCrLfLineBreaks()
        {
            // arrange
            var subject = new ImfMessagePart(Mailboxes.ReadFirst(MessageInFile).Message);

            // act
            var testResult = subject.GetContent().ToArray();

            // assert
            testResult.Should().Equal(Octets.Of(Eml));
        }

        [Fact]
        public void WriteMessage_MboxWriterWithCrLfLineBreaksAndAMessageFromAMailboxAttached_WritesItWithCrLfLineBreaks()
        {
            // arrange
            var options = new ImfOptions { AllowMboxCarriageReturnLineFeed = true };
            var written = Mailboxes.WriteOne(Envelope, BuildForwarding(Mailboxes.ReadFirst(MessageInFile).Message, options), lineBreak: MboxLineBreak.CarriageReturnLineFeed, options: options);
            var body = (ImfMultipart)Mailboxes.ReadFirst(written, options: options).Message.Body;

            // act
            var testResult = body.Parts[AttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(Octets.Of(Eml));
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAChangedMessageFromAMailboxAttachedAndEightBitAllowed_LabelsItsNewPartEightBit()
        {
            // arrange
            var forwarded = ReadMultipartWithAddedPart(EightBitText);
            var options = new ImfOptions { AllowEightBitTransferEncoding = true };
            var written = WriteForwarded(forwarded, options);
            var part = (ImfMessagePart)((ImfMultipart)Mailboxes.ReadMessage(written, options).Body).Parts[AttachmentPartIndex];

            // act
            var testResult = ((ImfMultipart)part.Message.Body).Parts[AttachmentPartIndex].ContentTransferEncoding;

            // assert
            testResult.Should().Be(MimeTransferEncoding.EightBit);
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAChangedMessageFromAMailboxAttachedOverMaxPartCount_ThrowsAndWritesNothing()
        {
            // arrange
            var message = BuildForwarding(ReadMultipartWithAddedPart(SevenBitText), new ImfOptions());
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output, new ImfOptions { MaxPartCount = OneFewerThanForwardingEntityCount }))
            {
                // act
                Action testAction = () => subject.WriteMessage(message);

                // assert
                testAction.Should().Throw<ImfFileException>().WithMessage(MaxPartCountMessagePattern);
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAChangedMessageFromAMailboxAttachedAtMaxPartCount_DoesNotThrow()
        {
            // arrange
            var message = BuildForwarding(ReadMultipartWithAddedPart(SevenBitText), new ImfOptions());
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output, new ImfOptions { MaxPartCount = ForwardingEntityCount }))
            {
                // act
                Action testAction = () => subject.WriteMessage(message);

                // assert
                testAction.Should().NotThrow();
            }
        }

        [Fact]
        public void WriteMessage_MessageFromAMailboxHoldingAnEmlMessage_WritesItWithLineFeedLineBreaks()
        {
            // arrange
            var message = Mailboxes.ReadFirst(MultipartInFile).Message;
            ((ImfMultipart)message.Body).Parts.Add(new ImfMessagePart(Mailboxes.ReadMessage(Eml)));
            var written = Mailboxes.WriteOne(Envelope, message);
            var body = (ImfMultipart)Mailboxes.ReadFirst(written).Message.Body;

            // act
            var testResult = body.Parts[AttachmentPartIndex].GetContent().ToArray();

            // assert
            testResult.Should().Equal(Octets.Of(Message));
        }

        [Fact]
        public void WriteMessage_MessageFromAMailboxHoldingABinaryEmlMessage_ThrowsAndWritesNothing()
        {
            // arrange
            var message = Mailboxes.ReadFirst(MultipartInFile).Message;
            ((ImfMultipart)message.Body).Parts.Add(new ImfMessagePart(Mailboxes.ReadMessage(BinaryEml)));
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output, MboxVariant.Mboxrd, new ImfOptions()))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                testAction.Should().Throw<ImfFileException>().WithMessage(ImfLineBreaks.OpaqueEntityMessage);
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_MessageFromAMailboxHoldingABinaryEmlMessage_ThrowsWithThePathOfItsBody()
        {
            // arrange
            var message = Mailboxes.ReadFirst(MultipartInFile).Message;
            ((ImfMultipart)message.Body).Parts.Add(new ImfMessagePart(Mailboxes.ReadMessage(BinaryEml)));
            using (var subject = new MboxWriter(new MemoryStream(), MboxVariant.Mboxrd, new ImfOptions()))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be(SecondPartPath);
            }
        }

        /// <summary>
        /// Reads a multipart message from a mailbox, and adds a text part to it, so that its body has changed.
        /// </summary>
        private static ImfMessage ReadMultipartWithAddedPart(string text)
        {
            var message = Mailboxes.ReadFirst(MultipartInFile).Message;
            var part = new ImfEntity(message.Options) { ContentType = MimeMediaType.Parse(TextMediaType) };
            part.SetContent(Octets.Of(text));
            ((ImfMultipart)message.Body).Parts.Add(part);
            return message;
        }

        /// <summary>
        /// Builds a message that forwards another, after a text part.
        /// </summary>
        private static ImfMessage BuildForwarding(ImfMessage forwarded, ImfOptions options)
        {
            var builder = new ImfMessageBuilder(options).SetText(BuiltTextPart).AddAttachment(forwarded);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            return builder.Build();
        }

        /// <summary>
        /// Writes a message that forwards another with an <see cref="ImfWriter"/>, and gives its bytes.
        /// </summary>
        private static string WriteForwarded(ImfMessage forwarded, ImfOptions options)
        {
            return WriteStandalone(BuildForwarding(forwarded, options), options);
        }

        /// <summary>
        /// Writes a message with an <see cref="ImfWriter"/>, and gives its bytes.
        /// </summary>
        private static string WriteStandalone(ImfMessage message, ImfOptions options = null)
        {
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output, options ?? new ImfOptions()))
            {
                writer.WriteMessage(message);
            }

            return Octets.ToText(output.ToArray());
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAFieldFromAMailboxAddedToAReadMessage_WritesItWithCrLfLineBreaks()
        {
            // arrange
            var message = Mailboxes.ReadMessage(Eml);
            message.Fields.Add(Mailboxes.ReadFirst(SubjectInFile).Message.Fields[FirstFieldIndex]);

            // act
            var testResult = WriteStandalone(message);

            // assert
            testResult.Should().Be(EmlWithAddedSubject);
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAFieldFromAMailboxAddedToANewMessage_WritesItWithCrLfLineBreaks()
        {
            // arrange
            var message = new ImfMessage { Date = ImfDateTime.Parse(BuiltDate), From = ImfAddressList.Parse(BuiltFrom) };
            message.Fields.Add(Mailboxes.ReadFirst(SubjectInFile).Message.Fields[FirstFieldIndex]);

            // act
            var testResult = WriteStandalone(message);

            // assert
            testResult.Should().Be(NewMessageWithAddedSubject);
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAFieldFromAMailboxAddedToABuiltMessage_WritesItWithCrLfLineBreaks()
        {
            // arrange
            var builder = new ImfMessageBuilder().SetText(BuiltTextPart);
            builder.Header.Date = ImfDateTime.Parse(BuiltDate);
            builder.Header.From = ImfAddressList.Parse(BuiltFrom);
            builder.Header.Fields.Add(Mailboxes.ReadFirst(SubjectInFile).Message.Fields[FirstFieldIndex]);
            var message = builder.Build();

            // act
            var testResult = WriteStandalone(message);

            // assert
            testResult.Should().Be(BuiltMessageWithAddedSubject);
        }

        [Fact]
        public void WriteMessage_ImfEntityWriterWithAFieldViewFromAMailboxAdded_WritesItWithCrLfLineBreaks()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.Parse(TextMediaType), Octets.Of(SevenBitText));
            Mailboxes.WithFirstField(SubjectInFile, field => source.AddField(field));

            // act
            var testResult = MessageWrites.Write(source);

            // assert
            testResult.Should().Be(SourceWithAddedSubject);
        }

        [Fact]
        public void WriteField_ImfHeaderWriterWithAFieldViewFromAMailbox_WritesItWithCrLfLineBreaks()
        {
            // arrange
            var output = new MemoryStream();
            using (var subject = new ImfHeaderWriter(output))
            {
                // act
                Mailboxes.WithFirstField(SubjectInFile, subject.WriteField);
                var testResult = Octets.ToText(output.ToArray());

                // assert
                testResult.Should().Be(CrLfSubject);
            }
        }

        [Fact]
        public async Task WriteFieldAsync_ImfHeaderWriterWithAFieldViewFromAMailbox_WritesItWithCrLfLineBreaks()
        {
            // arrange
            var output = new MemoryStream();
            var subject = new ImfHeaderWriter(output);
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                await Mailboxes.WithFirstFieldAsync(SubjectInFile, field => subject.WriteFieldAsync(field).AsTask());
                var testResult = Octets.ToText(output.ToArray());

                // assert
                testResult.Should().Be(CrLfSubject);
            });
        }

        [Fact]
        public void WriteMessage_MboxWriterWithAFieldFromAStandaloneMessageAdded_WritesItWithLineFeedLineBreaks()
        {
            // arrange
            var message = Mailboxes.ReadFirst(MessageInFile).Message;
            message.Fields.Add(Mailboxes.ReadMessage(SubjectEml).Fields[FirstFieldIndex]);

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message);

            // assert
            testResult.Should().Be(FileWithAddedSubject);
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAMessageFromAMailbox_WritesCrLf()
        {
            // arrange
            var entry = Mailboxes.ReadFirst(MessageInFile);
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output))
            {
                // act
                subject.WriteMessage(entry.Message);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(Eml);
                subject.FieldsWritten.Should().Be(TwoWritten);
                subject.EntitiesWritten.Should().Be(1);
            }
        }

        [Fact]
        public async Task WriteMessageAsync_ImfWriterWithAMessageFromAMailbox_WritesCrLf()
        {
            // arrange
            var entry = Mailboxes.ReadFirst(MessageInFile);
            var output = new MemoryStream();
            var subject = new ImfWriter(output);
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                await subject.WriteMessageAsync(entry.Message);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(Eml);
            });
        }

        [Fact]
        public void WriteMessage_ImfWriterWithABinaryEntityFromAMailbox_ThrowsAndWritesNothing()
        {
            // arrange
            var message = Mailboxes.ReadFirst(BinaryInLineFeedFile).Message;
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output))
            {
                // act
                Action testAction = () => subject.WriteMessage(message);

                // assert
                testAction.Should().Throw<ImfFileException>();
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_MhtmlWriterWithADocumentFromAMailbox_WritesCrLf()
        {
            // arrange
            var document = new MhtmlDocument(Mailboxes.ReadFirst(MessageInFile).Message);
            var output = new MemoryStream();
            using (var subject = new MhtmlWriter(output))
            {
                // act
                subject.WriteDocument(document);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(Eml);
            }
        }

        [Fact]
        public void WriteMessage_EightBitOctet_ThrowsAndWritesNothing()
        {
            // arrange
            var message = Mailboxes.ReadMessage(EightBitEml);
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.EightBitMboxContent);
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_EightBitOctetAllowed_WritesItWithADiagnostic()
        {
            // arrange
            var message = Mailboxes.ReadMessage(EightBitEml);
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output, MboxVariant.Mboxrd, AllowEightBit))
            {
                // act
                subject.WriteMessage(Envelope, message);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(EightBitInFile);
                subject.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.EightBitMboxContent);
            }
        }

        [Fact]
        public void WriteMessage_CrLfLineInLineFeedFile_ThrowsByDefault()
        {
            // arrange
            var entry = Mailboxes.ReadFirst(CrLfLineInFile, MboxVariant.Mboxrd, AllowCrLf);
            using (var subject = new MboxWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => subject.WriteMessage(entry.Envelope, entry.Message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MboxCarriageReturnLineFeed);
            }
        }

        [Fact]
        public void WriteMessage_CrLfLineInLineFeedFileAllowed_WritesItAsItWas()
        {
            // arrange
            var entry = Mailboxes.ReadFirst(CrLfLineInFile, MboxVariant.Mboxrd, AllowCrLf);
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output, MboxVariant.Mboxrd, AllowCrLf))
            {
                // act
                subject.WriteMessage(entry.Envelope, entry.Message);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(CrLfLineInFile);
            }
        }

        [Fact]
        public void WriteMessages_ContentLengthAdded_CountsTheField()
        {
            // arrange
            var entry = new MboxEntry(Envelope, Mailboxes.ReadMessage(Eml));
            var progress = new RecordingProgress();
            using (var subject = new MboxWriter(new MemoryStream(), MboxVariant.Mboxcl2, new ImfOptions()))
            {
                // act
                subject.WriteMessages(new[] { entry }, progress);

                // assert
                progress.Last.Should().Be(new ImfWriteProgress(CountedFieldCount, 1, CountedFileLength));
            }
        }

        [Fact]
        public void WriteMessages_TwoContentLengthFieldsReplaced_CountsTheOneLeft()
        {
            // arrange
            var entry = new MboxEntry(Envelope, Mailboxes.ReadMessage(TwoLengthsEml));
            var progress = new RecordingProgress();
            using (var subject = new MboxWriter(new MemoryStream(), MboxVariant.Mboxcl2, new ImfOptions()))
            {
                // act
                subject.WriteMessages(new[] { entry }, progress);

                // assert
                progress.Last.Should().Be(new ImfWriteProgress(CountedFieldCount, 1, CountedFileLength));
            }
        }

        [Fact]
        public void WriteMessage_ExtensionMechanismThatHasToChange_ThrowsAndWritesNothing()
        {
            // arrange
            var message = Mailboxes.ReadMessage(ExtensionEml);
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                testAction.Should().Throw<ImfFileException>();
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_ImfWriterWithAnExtensionMechanismFromAMailbox_ThrowsAndWritesNothing()
        {
            // arrange
            var message = Mailboxes.ReadFirst(ExtensionInFile).Message;
            var output = new MemoryStream();
            using (var subject = new ImfWriter(output))
            {
                // act
                Action testAction = () => subject.WriteMessage(message);

                // assert
                testAction.Should().Throw<ImfFileException>();
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_ImfWriterWithALargeMessageFromAMailbox_ReportsProgressInBlocks()
        {
            // arrange
            var message = Mailboxes.ReadFirst(LargeInFile).Message;
            var progress = new RecordingProgress();
            using (var subject = new ImfWriter(new MemoryStream()))
            {
                // act
                subject.WriteMessage(message, progress);

                // assert
                progress.LargestStep.Should().Be(BlockSize);
                progress.Last.Should().Be(new ImfWriteProgress(TwoFieldsWritten, 1, LargeEmlLength));
            }
        }

        [Fact]
        public async Task WriteMessageAsync_ImfWriterWithALargeMessageFromAMailbox_ReportsProgressInBlocks()
        {
            // arrange
            var message = Mailboxes.ReadFirst(LargeInFile).Message;
            var progress = new RecordingProgress();
            var subject = new ImfWriter(new MemoryStream());
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                await subject.WriteMessageAsync(message, progress);

                // assert
                progress.LargestStep.Should().Be(BlockSize);
                progress.Last.Should().Be(new ImfWriteProgress(TwoFieldsWritten, 1, LargeEmlLength));
            });
        }

        [Fact]
        public void WriteMessage_ProgressThrowsPartWayThroughAConvertedMessage_LeavesTheWriterUnableToContinue()
        {
            // arrange
            var message = Mailboxes.ReadFirst(LargeInFile).Message;
            using (var subject = new ImfWriter(new MemoryStream()))
            {
                Mailboxes.WriteFailing(subject, message, new ThrowingProgress());

                // act
                Action testAction = () => subject.WriteMessage(message);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void WriteMessage_NonconformingEnvelope_ThrowsByDefault()
        {
            // arrange
            var entry = Mailboxes.ReadFirst(ThunderbirdFile, MboxVariant.Mboxrd, AllowSeparator);
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output))
            {
                // act
                Action testAction = () => subject.WriteMessage(entry.Envelope, entry.Message);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidMboxSeparator);
                output.Length.Should().Be(0);
            }
        }

        [Fact]
        public void WriteMessage_NonconformingEnvelopeAllowed_WritesItAsItWasWithADiagnostic()
        {
            // arrange
            var entry = Mailboxes.ReadFirst(ThunderbirdFile, MboxVariant.Mboxrd, AllowSeparator);
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output, MboxVariant.Mboxrd, AllowSeparator))
            {
                // act
                subject.WriteMessage(entry.Envelope, entry.Message);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(ThunderbirdFile);
                subject.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.InvalidMboxSeparator);
            }
        }

        [Fact]
        public void WriteMessage_MessageRefused_WritesNothingAndCanGoOn()
        {
            // arrange
            var bad = Mailboxes.ReadMessage(NoRequiredFieldsEml, AllowMissingFields);
            var good = Mailboxes.ReadMessage(Eml);
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output))
            {
                Mailboxes.WriteRefused(subject, Envelope, bad);

                // act
                subject.WriteMessage(Envelope, good);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(MessageInFile);
            }
        }

        [Fact]
        public void WriteMessage_MessageRefused_GivesItsIndex()
        {
            // arrange
            var bad = Mailboxes.ReadMessage(NoRequiredFieldsEml, AllowMissingFields);
            using (var subject = new MboxWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, bad);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.MessageIndex].Should().Be(FirstIndex);
            }
        }

        [Fact]
        public void WriteMessages_Entries_ReportsProgressAfterEach()
        {
            // arrange
            var entry = new MboxEntry(Envelope, Mailboxes.ReadMessage(Eml));
            var progress = new RecordingProgress();
            using (var subject = new MboxWriter(new MemoryStream()))
            {
                // act
                subject.WriteMessages(new[] { entry, entry }, progress);

                // assert
                progress.Reports.Should().Equal(new ImfWriteProgress(2, 1, MessageFileLength), new ImfWriteProgress(4, 2, TwoMessagesFileLength));
                subject.MessagesWritten.Should().Be(TwoWritten);
                subject.BytePosition.Should().Be(TwoMessagesFileLength);
            }
        }

        [Fact]
        public void WriteMessages_NullEntry_ThrowsAfterTheOnesBeforeIt()
        {
            // arrange
            var entry = new MboxEntry(Envelope, Mailboxes.ReadMessage(Eml));
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output))
            {
                // act
                Action testAction = () => subject.WriteMessages(new[] { entry, null });

                // assert
                testAction.Should().Throw<ArgumentException>();
                output.Length.Should().Be(MessageFileLength);
            }
        }

        [Fact]
        public void WriteMessages_FromAReader_CopiesTheMailbox()
        {
            // arrange
            var read = new[] { Mailboxes.ReadFirst(MessageInFile), Mailboxes.ReadFirst(SecondMessageInFile) };
            var output = new MemoryStream();
            using (var subject = new MboxWriter(output))
            {
                // act
                subject.WriteMessages(read);

                // assert
                Octets.ToText(output.ToArray()).Should().Be(TwoDistinctMessagesInFile);
            }
        }

        [Fact]
        public async Task WriteMessageAsync_Message_WritesTheSameAsTheSynchronousWrite()
        {
            // arrange
            var output = new MemoryStream();
            var subject = new MboxWriter(output);
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                await subject.WriteMessageAsync(Envelope, Mailboxes.ReadMessage(Eml));

                // assert
                Octets.ToText(output.ToArray()).Should().Be(MessageInFile);
            });
        }

        [Fact]
        public async Task WriteMessagesAsync_Entries_ReportsProgressAfterEach()
        {
            // arrange
            var entry = new MboxEntry(Envelope, Mailboxes.ReadMessage(Eml));
            var progress = new RecordingProgress();
            var subject = new MboxWriter(new MemoryStream());
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                await subject.WriteMessagesAsync(new[] { entry, entry }, progress);

                // assert
                progress.Reports.Should().Equal(new ImfWriteProgress(2, 1, MessageFileLength), new ImfWriteProgress(4, 2, TwoMessagesFileLength));
            });
        }

        [Fact]
        public async Task WriteMessageAsync_Refused_FailsTheTask()
        {
            // arrange
            var bad = Mailboxes.ReadMessage(NoRequiredFieldsEml, AllowMissingFields);
            var subject = new MboxWriter(new MemoryStream());
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                Func<Task> testAction = () => subject.WriteMessageAsync(Envelope, bad);

                // assert
                await testAction.Should().ThrowAsync<ImfFileException>();
            });
        }

        [Fact]
        public async Task WriteMessage_AfterACanceledWrite_Throws()
        {
            // arrange
            var message = Mailboxes.ReadMessage(Eml);
            var subject = new MboxWriter(new MemoryStream());
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                await Mailboxes.WriteCanceledAsync(subject, Envelope, message);

                // act
                Action testAction = () => subject.WriteMessage(Envelope, message);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task WriteMessagesAsync_Canceled_CancelsTheTask()
        {
            // arrange
            var entry = new MboxEntry(Envelope, Mailboxes.ReadMessage(Eml));
            var subject = new MboxWriter(new MemoryStream());
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                Func<Task> testAction = () => subject.WriteMessagesAsync(new[] { entry }, null, new CancellationToken(true));

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            });
        }

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // act
            Action testAction = () => new MboxWriter((Stream)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_StreamThatCannotWrite_Throws()
        {
            // act
            Action testAction = () => new MboxWriter(new ReadOnlyMemoryStream());

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_UnknownVariant_Throws()
        {
            // arrange
            const MboxVariant unknown = (MboxVariant)99;

            // act
            Action testAction = () => new MboxWriter(new MemoryStream(), unknown, new ImfOptions());

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_UnknownLineBreak_Throws()
        {
            // arrange
            const MboxLineBreak unknown = (MboxLineBreak)99;

            // act
            Action testAction = () => new MboxWriter(new MemoryStream(), MboxVariant.Mboxrd, new ImfOptions(), unknown);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void WriteMessage_NullEnvelope_Throws()
        {
            // arrange
            var message = Mailboxes.ReadMessage(Eml);
            using (var subject = new MboxWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => subject.WriteMessage(null, message);

                // assert
                testAction.Should().Throw<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_NullMessage_Throws()
        {
            // arrange
            using (var subject = new MboxWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => subject.WriteMessage(Envelope, null);

                // assert
                testAction.Should().Throw<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessages_NullEntries_Throws()
        {
            // arrange
            using (var subject = new MboxWriter(new MemoryStream()))
            {
                // act
                Action testAction = () => subject.WriteMessages(null);

                // assert
                testAction.Should().Throw<ArgumentNullException>();
            }
        }

        [Fact]
        public void WriteMessage_Disposed_Throws()
        {
            // arrange
            var subject = new MboxWriter(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => subject.WriteMessage(Envelope, null);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void WriteMessages_Disposed_Throws()
        {
            // arrange
            var subject = new MboxWriter(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => subject.WriteMessages(null);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task WriteMessageAsync_Disposed_Throws()
        {
            // arrange
            var subject = new MboxWriter(new MemoryStream());
            subject.Dispose();

            // act
            Func<Task> testAction = () => subject.WriteMessageAsync(Envelope, null);

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public async Task WriteMessagesAsync_Disposed_Throws()
        {
            // arrange
            var subject = new MboxWriter(new MemoryStream());
            subject.Dispose();

            // act
            Func<Task> testAction = () => subject.WriteMessagesAsync(null);

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public void Flush_Disposed_Throws()
        {
            // arrange
            var subject = new MboxWriter(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => subject.Flush();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task FlushAsync_Disposed_Throws()
        {
            // arrange
            var subject = new MboxWriter(new MemoryStream());
            subject.Dispose();

            // act
            Func<Task> testAction = () => subject.FlushAsync();

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public void BytePosition_Disposed_Throws()
        {
            // arrange
            var subject = new MboxWriter(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => { var ignored = subject.BytePosition; };

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Diagnostics_Disposed_Throws()
        {
            // arrange
            var subject = new MboxWriter(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => { var ignored = subject.Diagnostics; };

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void MessagesWritten_Disposed_Throws()
        {
            // arrange
            var subject = new MboxWriter(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => { var ignored = subject.MessagesWritten; };

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Constructor_NullEnvelopeForEntry_Throws()
        {
            // arrange
            var message = Mailboxes.ReadMessage(Eml);

            // act
            Action testAction = () => new MboxEntry(null, message);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NullMessageForEntry_Throws()
        {
            // act
            Action testAction = () => new MboxEntry(Envelope, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }
    }
}
