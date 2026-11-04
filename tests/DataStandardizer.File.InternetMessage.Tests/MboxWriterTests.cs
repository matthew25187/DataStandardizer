using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        private const string BinaryEml = "Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\nContent-Transfer-Encoding: binary\r\n\r\nx\r\n";

        private const string CrLfSeparator = SeparatorLine + "\r\n";
        private const string BinaryInCrLfFile = CrLfSeparator + BinaryEml + "\r\n";
        private const string BinaryInLineFeedFile = Separator + Header + "MIME-Version: 1.0\nContent-Transfer-Encoding: binary\n\nx\n\n";
        private const string EmlInCrLfFile = CrLfSeparator + Eml + "\r\n";
        private const string MessageInFile = Separator + Message + "\n";
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
            builder.Header.Date = ImfDateTime.Parse("Thu, 1 Jan 2026 00:00:00 +0000");
            builder.Header.From = ImfAddressList.Parse("a@b.test");
            var written = Mailboxes.WriteOne(Envelope, builder.Build());

            // act
            var testResult = Mailboxes.ReadFirst(written);

            // assert
            testResult.Message.Body.GetText().Should().Be(BuiltText);
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
