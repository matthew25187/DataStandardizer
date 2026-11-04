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
    /// Tests of <see cref="MboxReader"/>.
    /// </summary>
    public class MboxReaderTests
    {
        private const string Envelope1 = "From a@b.test Thu Jan  1 00:00:00 2026";
        private const string Envelope2 = "From c@d.test Fri Jan  2 12:30:45 2026";
        private const string Separator1 = Envelope1 + "\n";
        private const string Separator2 = Envelope2 + "\n";
        private const string Header = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\n";
        private const string Message1 = Header + "\none\n";
        private const string Message2 = "Date: Fri, 2 Jan 2026 12:30:45 +0000\nFrom: c@d.test\n\ntwo\n";
        private const string Address1 = "a@b.test";

        private const string OneMessage = Separator1 + Message1;
        private const string OneMessageWithEmptyLine = OneMessage + "\n";
        private const string TwoMessages = OneMessageWithEmptyLine + Separator2 + Message2 + "\n";
        private const string OnlyHeader = Separator1 + Header;
        private const string NotAField = Separator1 + "not a field\n\n";
        private const string EmptyMailbox = "";

        private const string CrLfSeparator = Envelope1 + "\r\n";
        private const string CrLfHeader = "Date: Thu, 1 Jan 2026 00:00:00 +0000\r\nFrom: a@b.test\r\n\r\n";
        private const string CrLfMessage1 = CrLfHeader + "one\r\n";
        private const string CrLfMessage2 = "Date: Fri, 2 Jan 2026 12:30:45 +0000\r\nFrom: c@d.test\r\n\r\ntwo\r\n";
        private const string CrLfTwoMessages = CrLfSeparator + CrLfMessage1 + "\r\n" + Envelope2 + "\r\n" + CrLfMessage2 + "\r\n";
        private const string BareLineFeedMessage = CrLfHeader + "a\nFrom here\r\n";
        private const string BareLineFeedMailbox = CrLfSeparator + BareLineFeedMessage + "\r\n";
        private const string CrLfSeparatorLater = OneMessageWithEmptyLine + CrLfSeparator + Message2 + "\n";

        private const string LastLineMessage = Header + "\nlast";
        private const string LastLineMailbox = Separator1 + LastLineMessage;
        private const string EmptyLinesMessage = Header + "\nx\n\n\n";
        private const string EmptyLinesMailbox = Separator1 + EmptyLinesMessage + "\n" + Separator2 + Message2 + "\n";

        private const string JunkFirst = "junk\n" + OneMessageWithEmptyLine;
        private const string JunkLinesFirst = "junk\nmore junk\n\n" + OneMessageWithEmptyLine;
        private const string OnlyJunk = "junk with no line break";
        private const string InvalidFirstLine = "From nobody\n" + Message1 + "\n";
        private const string NoEmptyLineBeforeSecond = OneMessage + Separator2 + Message2 + "\n";
        private const string EndsAtSecondSeparator = OneMessage + Separator2;
        private const string UnquotedFromLineMessage = Header + "\nFrom here on\n";
        private const string UnquotedFromLine = Separator1 + UnquotedFromLineMessage + "\n";
        private const string ThunderbirdLaterMessage = Message1 + "\nFrom - Fri Jan  2 12:30:45 2026\n" + Message2;
        private const string ThunderbirdLater = Separator1 + ThunderbirdLaterMessage + "\n";
        private const string UnquotedMessage = Header + "\nFrom here on\nand From there\n";
        private const string UnquotedMailbox = Separator1 + UnquotedMessage + "\n";
        private const string ManyUnquotedMailbox = Separator1 + Header + "\nFrom one\nFrom two\nFrom three\n\n";
        private const string ThunderbirdMailbox = "From - Thu Jan  1 00:00:00 2026\n" + Message1 + "\n";

        // A body with lines that each variant treats differently, of 43 octets as it stands, and 44 with the quote that mboxo adds to its first line.
        private const string QuotableBody = "From here\n>From there\n>>From far\n\nnot From\n";
        private const string MboxoBody = ">From here\n>From there\n>>From far\n\nnot From\n";
        private const string MboxrdMessage = Header + "\n" + QuotableBody;
        private const string MboxoMessage = Header + "\n" + MboxoBody;
        private const string MboxclMessage = Header + "Content-Length: 44\n\n" + MboxoBody;
        private const string Mboxcl2Message = Header + "Content-Length: 43\n\n" + QuotableBody;
        private const string Mboxrd = Separator1 + Header + "\n>From here\n>>From there\n>>>From far\n\nnot From\n\n";
        private const string Mboxo = Separator1 + MboxoMessage + "\n";
        private const string Mboxcl = Separator1 + MboxclMessage + "\n";
        private const string Mboxcl2 = Separator1 + Mboxcl2Message + "\n";
        private const string OtherQuotesMessage = Header + "\n> quoted\n>> From\n>Fromage\n>\n";
        private const string OtherQuotesMailbox = Separator1 + OtherQuotesMessage + "\n";
        private const string CrLfLineInLineFeedFileMessage = Header + "\none\r\n";
        private const string CrLfLineInLineFeedFile = Separator1 + CrLfLineInLineFeedFileMessage + "\n";
        private const string EightBitMessage = Header + "MIME-Version: 1.0\nContent-Transfer-Encoding: 8bit\n\ncaf\u00E9\n";
        private const string EightBitMailbox = Separator1 + EightBitMessage + "\n";
        private const string EightBitInCountedBody = Separator1 + Header + "Content-Length: 5\n\ncaf\u00E9\n\n";

        private const string Cl1Message = Header + "Content-Length: 4\n\none\n";
        private const string Cl1 = Separator1 + Cl1Message + "\n";
        private const string UpperCaseLengthMessage = "Date: Fri, 2 Jan 2026 12:30:45 +0000\nFrom: c@d.test\nCONTENT-LENGTH : 4\n\ntwo\n";
        private const string TwoLengthsMailbox = Cl1 + Separator2 + UpperCaseLengthMessage + "\n";
        private const string SeparatorInBodyMessage = Header + "Content-Length: 40\n\n\nFrom c@d.test Fri Jan  2 12:30:45 2026\n";
        private const string SeparatorInBodyMailbox = Separator1 + SeparatorInBodyMessage + "\n";
        private const string ShortLengthMessage = Header + "Content-Length: 3\n\none";
        private const string ZeroLengthMessage = "Date: Fri, 2 Jan 2026 12:30:45 +0000\nFrom: c@d.test\nContent-Length: 0\n\n";
        private const string ShortLengthMailbox = Separator1 + ShortLengthMessage + "\n\n" + Separator2 + ZeroLengthMessage + "\n";
        private const string NoSeparatorAfterLength = Cl1 + "not a separator\n";
        private const string NoEmptyLineAfterLength = Separator1 + Cl1Message;
        private const string WrongLengthMessage = Header + "Content-Length: 500\n\none\n";
        private const string WrongLengthMailbox = Separator1 + WrongLengthMessage + "\n" + Separator2 + Message2 + "\n";
        private const string UnquotedSeparatorMessage = Header + "Content-Length: 43\n\nx\nFrom c@d.test Fri Jan  2 12:30:45 2026\ny\n";
        private const string UnquotedSeparatorMailbox = Separator1 + UnquotedSeparatorMessage + "\n";
        private const string ObsoleteFromMessage = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom : a@b.test\nContent-Length: 4\n\none\n";
        private const string FromLineInCountedBody = Separator1 + Header + "Content-Length: 13\n\nFrom here on\n\n";
        private const string ObsoleteFromMailbox = Separator1 + ObsoleteFromMessage + "\n";

        private const string ShortLengthRest = Separator1 + Header + "Content-Length: 3\n\none\n\nmore";
        private const string LongLengthRest = Separator1 + Header + "Content-Length: 5\n\none\n\nmore";
        private const string TooLongLengthRest = Separator1 + Header + "Content-Length: 500\n\none\n\nmore";
        private const string NotANumberRest = Separator1 + Header + "Content-Length: x\n\none\n\nmore";
        private const string EmptyLengthRest = Separator1 + Header + "Content-Length:\n\none\n\nmore";
        private const string TwoNumbersRest = Separator1 + Header + "Content-Length: 4 4\n\none\n\nmore";
        private const string TwoFieldsRest = Separator1 + Header + "Content-Length: 4\nContent-Length: 4\n\none\n\nmore";
        private const string HugeNumberRest = Separator1 + Header + "Content-Length: 9999999999999999999\n\none\n\nmore";

        private const string TooShortMailbox = Separator1 + Header + "Content-Length: 1\n\none\n\n" + Separator2 + Message2 + "\n";
        private const string NoLengthMailbox = Separator1 + Header + "\none\n\n" + Separator2 + Message2 + "\n";
        private const string NotANumberMailbox = Separator1 + Header + "Content-Length: x\n\none\n\n" + Separator2 + Message2 + "\n";

        private const string InvalidMiddleMailbox = OneMessageWithEmptyLine + Separator2 + "Subject: nothing required\n\nx\n\n" + OneMessageWithEmptyLine;
        private const string RejectedIndexOne = "rejected 1";

        // ref. RFC 5322 §2.1.1: a line is at most 998 octets, of which ">From " takes 6, so this one is 999 in the file and 998 in the message.
        private const int LongestFromLinePadding = 993;
        private const int ShorterFromLinePadding = 992;
        private static readonly string TooLongQuotedLine = ">From " + new string('x', LongestFromLinePadding);
        private static readonly string TooLongQuotedLineMailbox = Separator1 + Header + "\n" + TooLongQuotedLine + "\n\n";
        private static readonly string TooLongQuotedLineThenGood = TooLongQuotedLineMailbox + OneMessageWithEmptyLine;
        private static readonly string LongestQuotedLineMailbox = Separator1 + Header + "\n>From " + new string('x', ShorterFromLinePadding) + "\n\n";
        private static readonly string TooLongQuotedLineInBinary = Separator1 + Header + "MIME-Version: 1.0\nContent-Transfer-Encoding: binary\n\n" + TooLongQuotedLine + "\n\n";

        private const string EnvelopeAndBody1 = Envelope1 + "|one\n";
        private const string EnvelopeAndBody2 = Envelope2 + "|two\n";
        private const string EnvelopeAndQuotableBody = Envelope1 + "|" + QuotableBody;

        // ref. RFC 5322 §2.1.1: longer than a line may be.
        private const int LongAddressLength = 1000;

        // Longer than the block of the reader, in lines as long as RFC 5322 §2.1.1 allows.
        private const int LargeBodyLineLength = 998;
        private const int LargeBodyLineCount = 50;

        // The most the reader holds from its stream between messages, which is its block.
        private const int MostHeldBytes = 16 * 1024;
        private const int ManyMessages = 2000;

        // The position, in octets and lines, of the separator line that has no empty line before it in EndsAtSecondSeparator: 39 of the first separator line and 57 of the message.
        private const long SecondSeparatorPosition = 96;
        private const long SecondSeparatorLine = 6;

        private const long FirstIndex = 0;
        private const long SecondIndex = 1;
        private const int TwoRead = 2;

        private static readonly string LongSeparatorMailbox = "From " + new string('a', LongAddressLength) + "@b.test Thu Jan  1 00:00:00 2026\n" + Message1 + "\n";
        private static readonly string LargeMessage = Header + "\n" + string.Concat(Enumerable.Repeat(new string('x', LargeBodyLineLength) + "\n", LargeBodyLineCount));
        private static readonly string LargeMailbox = Separator1 + LargeMessage + "\n" + Separator2 + Message2 + "\n";
        private static readonly byte[] ManyMessagesMailbox = Mailboxes.Repeat(OneMessageWithEmptyLine, ManyMessages);

        private static readonly ImfOptions AllowCrLf = new ImfOptions { AllowMboxCarriageReturnLineFeed = true };
        private static readonly ImfOptions AllowEightBit = new ImfOptions { AllowEightBitMboxContent = true };
        private static readonly ImfOptions AllowBlankLine = new ImfOptions { AllowMissingMboxBlankLine = true };
        private static readonly ImfOptions AllowContentLength = new ImfOptions { AllowInvalidMboxContentLength = true };
        private static readonly ImfOptions AllowInvalidSeparator = new ImfOptions { AllowInvalidMboxSeparator = true };
        private static readonly ImfOptions AllowMissingSeparator = new ImfOptions { AllowMissingMboxSeparator = true };
        private static readonly ImfOptions OneMessageOnly = new ImfOptions { MaxMessageCount = 1 };
        private static readonly ImfOptions SmallEntity = new ImfOptions { MaxEntitySize = 1 };
        private static readonly ImfOptions NoMessages = new ImfOptions { MaxMessageCount = 0 };

        [Fact]
        public void ReadMessage_TwoMessages_GivesEachWithoutTheEmptyLine()
        {
            // act
            var testResult = Mailboxes.ReadAll(TwoMessages);

            // assert
            testResult.Should().Equal(Message1, Message2);
        }

        [Fact]
        public void ReadMessage_FirstMessage_GivesItsEnvelope()
        {
            // act
            var testResult = Mailboxes.ReadFirst(TwoMessages);

            // assert
            testResult.Envelope.ToString().Should().Be(Envelope1);
            testResult.Envelope.Sender.Address.Should().Be(Address1);
            testResult.Diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void ReadMessage_EmptyFile_GivesNothing()
        {
            // act
            var testResult = Mailboxes.ReadAll(EmptyMailbox);

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void ReadMessage_NoTrailingEmptyLine_ThrowsByDefault()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(OneMessage);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MissingMboxBlankLine);
        }

        [Fact]
        public void ReadMessage_NoTrailingEmptyLineAllowed_EndsAtTheEndOfTheFile()
        {
            // act
            var testResult = Mailboxes.ReadAll(OneMessage, MboxVariant.Mboxrd, AllowBlankLine);

            // assert
            testResult.Should().Equal(Message1);
        }

        [Fact]
        public void ReadMessage_NoTrailingEmptyLineAllowed_RecordsADiagnostic()
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(OneMessage, MboxVariant.Mboxrd, AllowBlankLine);

            // assert
            testResult.Should().Equal(ImfDiagnosticKind.MissingMboxBlankLine);
        }

        [Fact]
        public void ReadMessage_NoLineBreakAtTheEndAllowed_KeepsTheLastLineAsItIs()
        {
            // act
            var testResult = Mailboxes.ReadAll(LastLineMailbox, MboxVariant.Mboxrd, AllowBlankLine);

            // assert
            testResult.Should().Equal(LastLineMessage);
        }

        [Fact]
        public void ReadMessage_BodyEndingInEmptyLines_RemovesOnlyTheLast()
        {
            // act
            var testResult = Mailboxes.ReadAll(EmptyLinesMailbox);

            // assert
            testResult.Should().Equal(EmptyLinesMessage, Message2);
        }

        [Fact]
        public void ReadMessage_CrLfFile_ThrowsByDefault()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(CrLfTwoMessages);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MboxCarriageReturnLineFeed);
        }

        [Fact]
        public void ReadMessage_CrLfFileAllowed_RecordsADiagnostic()
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(CrLfTwoMessages, MboxVariant.Mboxrd, AllowCrLf);

            // assert
            testResult.Should().Equal(ImfDiagnosticKind.MboxCarriageReturnLineFeed);
        }

        [Fact]
        public void ReadMessage_CrLfLineInLineFeedFile_ThrowsByDefault()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(CrLfLineInLineFeedFile);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MboxCarriageReturnLineFeed);
        }

        [Fact]
        public void ReadMessage_CrLfLineInLineFeedFileAllowed_KeepsTheCarriageReturnInTheLine()
        {
            // act
            var testResult = Mailboxes.ReadAll(CrLfLineInLineFeedFile, MboxVariant.Mboxrd, AllowCrLf);

            // assert
            testResult.Should().Equal(CrLfLineInLineFeedFileMessage);
        }

        [Fact]
        public void ReadMessage_EightBitOctet_ThrowsByDefault()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(EightBitMailbox);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.EightBitMboxContent);
        }

        [Fact]
        public void ReadMessage_EightBitOctetInCountedBody_ThrowsByDefault()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(EightBitInCountedBody, MboxVariant.Mboxcl2);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.EightBitMboxContent);
        }

        [Fact]
        public void ReadMessage_EightBitOctetAllowed_KeepsItWithADiagnostic()
        {
            // act
            var testResult = Mailboxes.ReadFirst(EightBitMailbox, MboxVariant.Mboxrd, AllowEightBit);

            // assert
            Mailboxes.Describe(testResult.Message).Should().Be(EightBitMessage);
            testResult.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.EightBitMboxContent);
        }

        [Fact]
        public void ReadMessage_CrLfFileAllowedTwoMessages_ReadsWithItsLineBreaks()
        {
            // act
            var testResult = Mailboxes.ReadAll(CrLfTwoMessages, MboxVariant.Mboxrd, AllowCrLf);

            // assert
            testResult.Should().Equal(CrLfMessage1, CrLfMessage2);
        }

        [Fact]
        public void ReadMessage_CrLfFileWithBareLineFeedBeforeFrom_IsNotALineStart()
        {
            // act
            var testResult = Mailboxes.ReadAll(BareLineFeedMailbox, MboxVariant.Mboxrd, AllowCrLf);

            // assert
            testResult.Should().Equal(BareLineFeedMessage);
        }

        [Fact]
        public void ReadMessage_LineFeedFileWithCrLfSeparatorLater_IsNotASeparator()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(CrLfSeparatorLater);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MboxCarriageReturnLineFeed);
        }

        [Fact]
        public void ReadMessage_FirstLineNotASeparator_ThrowsByDefault()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(JunkFirst);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MissingMboxSeparator);
        }

        [Fact]
        public void ReadMessage_FirstLineNotASeparatorAllowed_SkipsToTheFirst()
        {
            // act
            var testResult = Mailboxes.ReadAll(JunkLinesFirst, MboxVariant.Mboxrd, AllowMissingSeparator);

            // assert
            testResult.Should().Equal(Message1);
        }

        [Fact]
        public void ReadMessage_FirstLineNotASeparatorAllowed_RecordsADiagnostic()
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(JunkFirst, MboxVariant.Mboxrd, AllowMissingSeparator);

            // assert
            testResult.Should().Equal(ImfDiagnosticKind.MissingMboxSeparator);
        }

        [Fact]
        public void ReadMessage_NoSeparatorAtAllAllowed_GivesNothingAndADiagnostic()
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(OnlyJunk, MboxVariant.Mboxrd, AllowMissingSeparator);

            // assert
            testResult.Should().Equal(ImfDiagnosticKind.MissingMboxSeparator);
        }

        [Fact]
        public void ReadMessage_FirstLineStartsFromAndIsInvalid_ThrowsInvalidSeparator()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(InvalidFirstLine);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidMboxSeparator);
        }

        [Fact]
        public void ReadMessage_MissingEmptyLineBeforeSeparator_ThrowsByDefault()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(NoEmptyLineBeforeSecond);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MissingMboxBlankLine);
        }

        [Fact]
        public void ReadMessage_MissingEmptyLineBeforeSeparatorAllowed_EndsTheMessageThere()
        {
            // act
            var testResult = Mailboxes.ReadAll(NoEmptyLineBeforeSecond, MboxVariant.Mboxrd, AllowBlankLine);

            // assert
            testResult.Should().Equal(Message1, Message2);
        }

        [Fact]
        public void ReadMessage_MissingEmptyLineBeforeSeparatorAllowed_RecordsADiagnostic()
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(NoEmptyLineBeforeSecond, MboxVariant.Mboxrd, AllowBlankLine);

            // assert
            testResult.Should().Equal(ImfDiagnosticKind.MissingMboxBlankLine);
        }

        [Fact]
        public void ReadMessage_UnquotedFromLineInBody_IsContentByDefault()
        {
            // act
            var testResult = Mailboxes.ReadAll(UnquotedFromLine);

            // assert
            testResult.Should().Equal(UnquotedFromLineMessage);
        }

        [Fact]
        public void ReadMessage_UnquotedFromLinesUnderTheOption_AreContent()
        {
            // act
            var testResult = Mailboxes.ReadAll(UnquotedMailbox, MboxVariant.Mboxrd, AllowInvalidSeparator);

            // assert
            testResult.Should().Equal(UnquotedMessage);
        }

        [Fact]
        public void ReadMessage_ManyUnquotedFromLines_RecordsOneDiagnostic()
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(ManyUnquotedMailbox, MboxVariant.Mboxrd, new ImfOptions());

            // assert
            testResult.Should().Equal(ImfDiagnosticKind.InvalidMboxSeparator);
        }

        [Fact]
        public void ReadMessage_NonstandardSeparatorLaterByDefault_IsContent()
        {
            // act
            var testResult = Mailboxes.ReadAll(ThunderbirdLater);

            // assert
            testResult.Should().Equal(ThunderbirdLaterMessage);
        }

        [Fact]
        public void ReadMessage_NonstandardSeparatorLaterUnderTheOption_StartsAMessage()
        {
            // act
            var testResult = Mailboxes.ReadAll(ThunderbirdLater, MboxVariant.Mboxrd, AllowInvalidSeparator);

            // assert
            testResult.Should().Equal(Message1, Message2);
        }

        [Fact]
        public void ReadMessage_NonstandardSeparatorAllowed_GivesWhatItCanWithADiagnostic()
        {
            // act
            var testResult = Mailboxes.ReadFirst(ThunderbirdMailbox, MboxVariant.Mboxrd, AllowInvalidSeparator);

            // assert
            testResult.Envelope.Sender.Should().BeNull();
            testResult.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.InvalidMboxSeparator);
        }

        [Fact]
        public void ReadMessage_SeparatorLongerThanALine_IsNotASeparator()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(LongSeparatorMailbox);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidMboxSeparator);
        }

        [Fact]
        public void ReadMessage_SeparatorWithNoLineBreakAtTheEnd_IsInvalid()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(Envelope1);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidMboxSeparator);
        }

        [Fact]
        public void ReadMessage_Mboxrd_RemovesOneQuoteFromEachLine()
        {
            // act
            var testResult = Mailboxes.ReadAll(Mboxrd);

            // assert
            testResult.Should().Equal(MboxrdMessage);
        }

        [Fact]
        public void ReadMessage_MboxrdLinesThatAreNotFromLines_KeepTheirQuotes()
        {
            // act
            var testResult = Mailboxes.ReadAll(OtherQuotesMailbox);

            // assert
            testResult.Should().Equal(OtherQuotesMessage);
        }

        [Fact]
        public void ReadMessage_Mboxo_KeepsTheQuotes()
        {
            // act
            var testResult = Mailboxes.ReadAll(Mboxo, MboxVariant.Mboxo);

            // assert
            testResult.Should().Equal(MboxoMessage);
        }

        [Fact]
        public void ReadMessage_Mboxcl_UsesTheLengthAndKeepsTheQuotes()
        {
            // act
            var testResult = Mailboxes.ReadAll(Mboxcl, MboxVariant.Mboxcl);

            // assert
            testResult.Should().Equal(MboxclMessage);
        }

        [Fact]
        public void ReadMessage_Mboxcl2_UsesTheLengthWithNoQuoting()
        {
            // act
            var testResult = Mailboxes.ReadAll(Mboxcl2, MboxVariant.Mboxcl2);

            // assert
            testResult.Should().Equal(Mboxcl2Message);
        }

        [Fact]
        public void ReadMessage_Mboxcl2BodyHoldingASeparator_StaysInTheBody()
        {
            // act
            var testResult = Mailboxes.ReadAll(SeparatorInBodyMailbox, MboxVariant.Mboxcl2);

            // assert
            testResult.Should().Equal(SeparatorInBodyMessage);
        }

        [Fact]
        public void ReadMessage_ContentLengthRightFollowedByAnother_ReadsBoth()
        {
            // act
            var testResult = Mailboxes.ReadAll(TwoLengthsMailbox, MboxVariant.Mboxcl2);

            // assert
            testResult.Should().Equal(Cl1Message, UpperCaseLengthMessage);
        }

        [Fact]
        public void ReadMessage_ContentLengthNotCountingALastLineBreak_ReadsToThatLength()
        {
            // act
            var testResult = Mailboxes.ReadAll(ShortLengthMailbox, MboxVariant.Mboxcl2);

            // assert
            testResult.Should().Equal(ShortLengthMessage, ZeroLengthMessage);
        }

        [Theory]
        [InlineData(MboxVariant.Mboxcl)]
        [InlineData(MboxVariant.Mboxcl2)]
        public void ReadMessage_ContentLengthMissing_ThrowsByDefault(MboxVariant variant)
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(OneMessageWithEmptyLine, variant);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidMboxContentLength);
        }

        [Theory]
        [InlineData(ShortLengthRest)]
        [InlineData(LongLengthRest)]
        [InlineData(TooLongLengthRest)]
        [InlineData(NotANumberRest)]
        [InlineData(EmptyLengthRest)]
        [InlineData(TwoNumbersRest)]
        [InlineData(TwoFieldsRest)]
        [InlineData(HugeNumberRest)]
        [InlineData(NoSeparatorAfterLength)]
        [InlineData(OnlyHeader)]
        public void ReadMessage_ContentLengthWrong_ThrowsByDefault(string mailbox)
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(mailbox, MboxVariant.Mboxcl2);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.InvalidMboxContentLength);
        }

        [Fact]
        public void ReadMessage_ContentLengthRightAndNoEmptyLineAtTheEnd_ThrowsByDefault()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(NoEmptyLineAfterLength, MboxVariant.Mboxcl2);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.MissingMboxBlankLine);
        }

        [Fact]
        public void ReadMessage_ContentLengthRightAndNoEmptyLineAtTheEndAllowed_RecordsADiagnostic()
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(NoEmptyLineAfterLength, MboxVariant.Mboxcl2, AllowBlankLine);

            // assert
            testResult.Should().Equal(ImfDiagnosticKind.MissingMboxBlankLine);
        }

        [Theory]
        [InlineData(MboxVariant.Mboxcl, TooShortMailbox)]
        [InlineData(MboxVariant.Mboxcl2, TooShortMailbox)]
        [InlineData(MboxVariant.Mboxcl2, WrongLengthMailbox)]
        [InlineData(MboxVariant.Mboxcl2, NoLengthMailbox)]
        [InlineData(MboxVariant.Mboxcl2, NotANumberMailbox)]
        public void ReadMessage_ContentLengthWrongAllowed_RecordsADiagnostic(MboxVariant variant, string mailbox)
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(mailbox, variant, AllowContentLength);

            // assert
            testResult.Should().Contain(ImfDiagnosticKind.InvalidMboxContentLength);
        }

        [Fact]
        public void ReadMessage_ContentLengthWrongAllowed_EndsAtTheNextSeparator()
        {
            // act
            var testResult = Mailboxes.ReadAll(WrongLengthMailbox, MboxVariant.Mboxcl2, AllowContentLength);

            // assert
            testResult.Should().Equal(WrongLengthMessage, Message2);
        }

        [Fact]
        public void ReadMessage_ContentLengthRightUnderTheOption_RecordsNoDiagnostic()
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(Cl1, MboxVariant.Mboxcl2, AllowContentLength);

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void ReadMessage_ContentLengthOptionWithUnquotedSeparatorAndNoEmptyLine_IsContent()
        {
            // act
            var testResult = Mailboxes.ReadAll(UnquotedSeparatorMailbox, MboxVariant.Mboxcl2, AllowContentLength);

            // assert
            testResult.Should().Equal(UnquotedSeparatorMessage);
        }

        [Fact]
        public void ReadMessage_ContentLengthOptionWithInvalidFromLineInBody_RecordsADiagnostic()
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(FromLineInCountedBody, MboxVariant.Mboxcl2, AllowContentLength);

            // assert
            testResult.Should().Equal(ImfDiagnosticKind.InvalidMboxSeparator);
        }

        [Fact]
        public void ReadMessage_ContentLengthOptionWithObsoleteFromField_RecordsNoDiagnostic()
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(ObsoleteFromMailbox, MboxVariant.Mboxcl2, AllowContentLength);

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void ReadMessage_ObsoleteFromFieldInContentLengthVariant_IsAField()
        {
            // act
            var testResult = Mailboxes.ReadAll(ObsoleteFromMailbox, MboxVariant.Mboxcl2);

            // assert
            testResult.Should().Equal(ObsoleteFromMessage);
        }

        [Fact]
        public void ReadMessage_MoreThanTheMaximumOfMessages_Throws()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(TwoMessages, MboxVariant.Mboxrd, OneMessageOnly);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.MessageIndex].Should().Be(SecondIndex);
        }

        [Fact]
        public void ReadMessage_MessagePassesAMessageLimit_Throws()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(TwoMessages, MboxVariant.Mboxrd, SmallEntity);

            // assert
            testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.MessageIndex].Should().Be(FirstIndex);
        }

        [Fact]
        public void ReadMessage_InvalidMessage_GivesItsIndexAndGoesOnToTheNext()
        {
            // act
            var testResult = Mailboxes.ReadEach(InvalidMiddleMailbox);

            // assert
            testResult.Should().Equal(Message1, RejectedIndexOne, Message1);
        }

        [Fact]
        public void ReadMessage_AfterAnErrorInTheFormat_Throws()
        {
            // arrange
            using (var subject = new MboxReader(new MemoryStream(Octets.Of(JunkFirst))))
            {
                Mailboxes.ReadUntilRejected(subject);

                // act
                Action testAction = () => subject.ReadMessage();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Fact]
        public void ReadMessage_ErrorInTheFormat_GivesThePositionInTheFile()
        {
            // act
            Action testAction = () => Mailboxes.ReadAll(EndsAtSecondSeparator);

            // assert
            var exception = testAction.Should().Throw<ImfFileException>().Which;
            exception.Data[ImfFileException.DataItemName.BytePosition].Should().Be(SecondSeparatorPosition);
            exception.Data[ImfFileException.DataItemName.LineNumber].Should().Be(SecondSeparatorLine);
            exception.Data[ImfFileException.DataItemName.MessageIndex].Should().Be(FirstIndex);
        }

        [Theory]
        [InlineData(MboxVariant.Mboxrd, Mboxrd, MboxrdMessage)]
        [InlineData(MboxVariant.Mboxo, Mboxo, MboxoMessage)]
        [InlineData(MboxVariant.Mboxcl, Mboxcl, MboxclMessage)]
        [InlineData(MboxVariant.Mboxcl2, Mboxcl2, Mboxcl2Message)]
        public void ReadMessage_OneByteAtATime_GivesTheSameAsAllAtOnce(MboxVariant variant, string mailbox, string expected)
        {
            // arrange
            var stream = new BlockStream(Octets.Of(mailbox), BlockStreamMode.SynchronousOnly, 1, 1);
            using (var subject = new MboxReader(stream, variant, new ImfOptions()))
            {
                // act
                var testResult = Mailboxes.ReadAll(subject);

                // assert
                testResult.Should().Equal(expected);
            }
        }

        [Fact]
        public void ReadMessage_CrLfOneByteAtATime_GivesTheSameAsAllAtOnce()
        {
            // arrange
            var stream = new BlockStream(Octets.Of(CrLfTwoMessages), BlockStreamMode.SynchronousOnly, 1, 1);
            using (var subject = new MboxReader(stream, MboxVariant.Mboxrd, AllowCrLf))
            {
                // act
                var testResult = Mailboxes.ReadAll(subject);

                // assert
                testResult.Should().Equal(CrLfMessage1, CrLfMessage2);
            }
        }

        [Fact]
        public void ReadMessage_BodyLongerThanTheBlock_IsWhole()
        {
            // act
            var testResult = Mailboxes.ReadAll(LargeMailbox);

            // assert
            testResult.Should().Equal(LargeMessage, Message2);
        }

        [Theory]
        [InlineData(MboxVariant.Mboxrd, Mboxrd, MboxrdMessage)]
        [InlineData(MboxVariant.Mboxo, Mboxo, MboxoMessage)]
        [InlineData(MboxVariant.Mboxcl, Mboxcl, MboxclMessage)]
        [InlineData(MboxVariant.Mboxcl2, Mboxcl2, Mboxcl2Message)]
        public async Task ReadMessageAsync_EachVariant_GivesTheSameAsTheSynchronousRead(MboxVariant variant, string mailbox, string expected)
        {
            // arrange
            var subject = new MboxReader(new BlockStream(Octets.Of(mailbox), BlockStreamMode.AsynchronousOnly, 1, 1), variant, new ImfOptions());
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                var testResult = await Mailboxes.ReadAllAsync(subject);

                // assert
                testResult.Should().Equal(expected);
            });
        }

        [Fact]
        public async Task ReadMessageAsync_ErrorInTheData_FailsTheTask()
        {
            // arrange
            var subject = new MboxReader(new MemoryStream(Octets.Of(JunkFirst)));
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                Func<Task> testAction = () => subject.ReadMessageAsync();

                // assert
                await testAction.Should().ThrowAsync<ImfFileException>();
            });
        }

        [Fact]
        public async Task ReadMessageAsync_Canceled_CancelsTheTask()
        {
            // arrange
            var subject = new MboxReader(new MemoryStream(Octets.Of(TwoMessages)));
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                Func<Task> testAction = () => subject.ReadMessageAsync(new CancellationToken(true));

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            });
        }

        [Fact]
        public async Task ReadMessage_AfterACanceledRead_Throws()
        {
            // arrange
            var subject = new MboxReader(new MemoryStream(Octets.Of(TwoMessages)));
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                await Mailboxes.ReadCanceledAsync(subject);

                // act
                Action testAction = () => subject.ReadMessage();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task ReadMessage_WhileAnotherReadIsInProgress_Throws()
        {
            // arrange
            var stream = new GatedStream();
            var subject = new MboxReader(stream);
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                var pending = subject.ReadMessageAsync();

                // act
                Action testAction = () => subject.ReadMessage();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
                stream.OpenGate();
                await pending;
            });
        }

        [Fact]
        public async Task Dispose_WhileAReadIsInProgress_EndsThatRead()
        {
            // arrange
            var stream = new GatedStream();
            var subject = new MboxReader(stream);
            var pending = subject.ReadMessageAsync();
            subject.Dispose();
            stream.OpenGate();

            // act
            Func<Task> testAction = () => pending;

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public void ReadEvent_AfterAnErrorInTheFormat_Throws()
        {
            // arrange
            using (var subject = new MboxReader(new MemoryStream(Octets.Of(NoEmptyLineBeforeSecond))))
            {
                var events = subject.ReadEntityEvents();
                EntityEvents.ReadUntilRejected(events);

                // act
                Action testAction = () => subject.ReadEntityEvents();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
        }

        [Theory]
        [InlineData(MboxVariant.Mboxrd)]
        [InlineData(MboxVariant.Mboxo)]
        public void ReadMessage_LineTooLongInTheFile_ThrowsByDefault(MboxVariant variant)
        {
            // arrange
            using (var subject = new MboxReader(new MemoryStream(Octets.Of(TooLongQuotedLineMailbox)), variant, new ImfOptions()))
            {
                // act
                Action testAction = () => subject.ReadMessage();

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.LongBodyLine);
            }
        }

        [Theory]
        [InlineData(MboxVariant.Mboxrd)]
        [InlineData(MboxVariant.Mboxo)]
        public void ReadMessage_LineTooLongInTheFileWithOption_RecordsADiagnostic(MboxVariant variant)
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(TooLongQuotedLineMailbox, variant, new ImfOptions { AllowLongLines = true });

            // assert
            testResult.Should().Equal(ImfDiagnosticKind.LongBodyLine);
        }

        [Fact]
        public void ReadMessage_LineTooLongInTheFile_GivesItsIndexAndGoesOnToTheNext()
        {
            // act
            var testResult = Mailboxes.ReadEach(TooLongQuotedLineThenGood);

            // assert
            testResult.Should().Equal("rejected 0", Message1);
        }

        [Fact]
        public void ReadMessage_QuotedLineThatFitsInTheFile_RecordsNothing()
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(LongestQuotedLineMailbox, MboxVariant.Mboxrd, new ImfOptions());

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void ReadMessage_LineTooLongInTheFileOfABinaryEntity_RecordsNothing()
        {
            // act
            var testResult = Mailboxes.ReadDiagnosticKinds(TooLongQuotedLineInBinary, MboxVariant.Mboxrd, new ImfOptions());

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void ReadEntityEvents_LineTooLongInTheFile_ThrowsByDefault()
        {
            // arrange
            using (var subject = new MboxReader(new MemoryStream(Octets.Of(TooLongQuotedLineMailbox))))
            {
                var events = subject.ReadEntityEvents();

                // act
                Action testAction = () => EntityEvents.ReadToEnd(events);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.DiagnosticKind].Should().Be(ImfDiagnosticKind.LongBodyLine);
            }
        }

        [Fact]
        public void ReadEntityEvents_LineTooLongInTheFileWithOption_RecordsADiagnostic()
        {
            // arrange
            using (var subject = new MboxReader(new MemoryStream(Octets.Of(TooLongQuotedLineMailbox)), MboxVariant.Mboxrd, new ImfOptions { AllowLongLines = true }))
            {
                var events = subject.ReadEntityEvents();

                // act
                EntityEvents.ReadToEnd(events);

                // assert
                events.Diagnostics.Should().ContainSingle().Which.Kind.Should().Be(ImfDiagnosticKind.LongBodyLine);
            }
        }

        [Fact]
        public void ReadEntityEvents_LineTooLongInTheFileOfABinaryEntity_RecordsNothing()
        {
            // arrange
            using (var subject = new MboxReader(new MemoryStream(Octets.Of(TooLongQuotedLineInBinary))))
            {
                var events = subject.ReadEntityEvents();

                // act
                EntityEvents.ReadToEnd(events);

                // assert
                events.Diagnostics.Should().BeEmpty();
            }
        }

        [Fact]
        public void ReadEntityEvents_EachMessage_GivesItsEnvelopeAndBody()
        {
            // act
            var testResult = Mailboxes.ReadBodiesFromEvents(TwoMessages);

            // assert
            testResult.Should().Equal(EnvelopeAndBody1, EnvelopeAndBody2);
        }

        [Fact]
        public void ReadEntityEvents_QuotedLine_GivesItWithOneQuoteRemoved()
        {
            // act
            var testResult = Mailboxes.ReadBodiesFromEvents(Mboxrd);

            // assert
            testResult.Should().Equal(EnvelopeAndQuotableBody);
        }

        [Fact]
        public void ReadEntityEvents_MessageNotReadToItsEnd_SkipsTheRest()
        {
            // act
            var testResult = Mailboxes.ReadEnvelopesSkippingEvents(TwoMessages);

            // assert
            testResult.Should().Equal(Envelope1, Envelope2);
        }

        [Fact]
        public void ReadEntityEvents_EndOfTheFile_GivesNull()
        {
            // arrange
            using (var subject = new MboxReader(new MemoryStream()))
            {
                // act
                var testResult = subject.ReadEntityEvents();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void ReadEntityEvents_NextMessage_DisposesTheEarlierReader()
        {
            // arrange
            using (var subject = new MboxReader(new MemoryStream(Octets.Of(TwoMessages))))
            {
                var first = subject.ReadEntityEvents();
                subject.ReadEntityEvents();

                // act
                Action testAction = () => first.ReadEvent();

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Fact]
        public void ReadEvent_InvalidMessage_GivesTheMessageIndex()
        {
            // arrange
            using (var subject = new MboxReader(new MemoryStream(Octets.Of(NotAField))))
            {
                var events = subject.ReadEntityEvents();

                // act
                Action testAction = () => EntityEvents.ReadToEnd(events);

                // assert
                testAction.Should().Throw<ImfFileException>().Which.Data[ImfFileException.DataItemName.MessageIndex].Should().Be(FirstIndex);
            }
        }

        [Fact]
        public async Task ReadEntityEventsAsync_EachMessage_GivesItsEvents()
        {
            // arrange
            var subject = new MboxReader(new BlockStream(Octets.Of(TwoMessages), BlockStreamMode.AsynchronousOnly, 1, 1));
            await AsyncDisposal.UsingAsync(subject, async () =>
            {
                // act
                var testResult = await Mailboxes.ReadAllEventsAsync(subject);

                // assert
                testResult.Should().Be(TwoRead);
            });
        }

        [Fact]
        public void ReadMessage_ManyMessages_HoldsNoMoreThanItsBlocks()
        {
            // arrange
            var stream = new BlockStream(ManyMessagesMailbox, BlockStreamMode.SynchronousOnly);
            using (var subject = new MboxReader(stream))
            {
                // act
                var testResult = Mailboxes.ReadAndMeasure(subject, () => stream.BytesGiven);

                // assert
                testResult.Count.Should().Be(ManyMessages);
                testResult.MostHeld.Should().BeLessOrEqualTo(MostHeldBytes);
            }
        }

        [Fact]
        public void Diagnostics_NextMessage_StartAgain()
        {
            // arrange
            using (var subject = new MboxReader(new MemoryStream(Octets.Of(NoEmptyLineBeforeSecond)), MboxVariant.Mboxrd, AllowBlankLine))
            {
                subject.ReadMessage();
                subject.ReadMessage();

                // act
                var testResult = subject.Diagnostics;

                // assert
                testResult.Should().BeEmpty();
            }
        }

        [Fact]
        public void Constructor_NullStream_Throws()
        {
            // act
            Action testAction = () => new MboxReader((Stream)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_StreamThatCannotRead_Throws()
        {
            // act
            Action testAction = () => new MboxReader(new WriteOnlyStream());

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullOptions_Throws()
        {
            // act
            Action testAction = () => new MboxReader(new MemoryStream(), MboxVariant.Mboxrd, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_UnknownVariant_Throws()
        {
            // arrange
            const MboxVariant unknown = (MboxVariant)99;

            // act
            Action testAction = () => new MboxReader(new MemoryStream(), unknown, new ImfOptions());

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_MaxMessageCountBelowOne_Throws()
        {
            // act
            Action testAction = () => new MboxReader(new MemoryStream(), MboxVariant.Mboxrd, NoMessages);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ReadMessage_Disposed_Throws()
        {
            // arrange
            var subject = new MboxReader(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => subject.ReadMessage();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void ReadEntityEvents_Disposed_Throws()
        {
            // arrange
            var subject = new MboxReader(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => subject.ReadEntityEvents();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task ReadMessageAsync_Disposed_Throws()
        {
            // arrange
            var subject = new MboxReader(new MemoryStream());
            subject.Dispose();

            // act
            Func<Task> testAction = () => subject.ReadMessageAsync();

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public async Task ReadEntityEventsAsync_Disposed_Throws()
        {
            // arrange
            var subject = new MboxReader(new MemoryStream());
            subject.Dispose();

            // act
            Func<Task> testAction = () => subject.ReadEntityEventsAsync();

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public void BytePosition_Disposed_Throws()
        {
            // arrange
            var subject = new MboxReader(new MemoryStream());
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
            var subject = new MboxReader(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => { var ignored = subject.Diagnostics; };

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Envelope_Disposed_Throws()
        {
            // arrange
            var subject = new MboxReader(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => { var ignored = subject.Envelope; };

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void MessagesRead_Disposed_Throws()
        {
            // arrange
            var subject = new MboxReader(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = () => { var ignored = subject.MessagesRead; };

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            // arrange
            var subject = new MboxReader(new MemoryStream());
            subject.Dispose();

            // act
            Action testAction = subject.Dispose;

            // assert
            testAction.Should().NotThrow();
        }
    }
}
