using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of moving a message between mbox and EMLX files, with no <c>.eml</c> file between them.
    /// </summary>
    public class MboxEmlxConversionTests
    {
        private const string Separator = "From a@b.test Thu Jan  1 00:00:00 2026\n";
        private const string EmptyMetadata = EmlxFiles.PlistStart + "<dict/>\n" + EmlxFiles.PlistEnd;

        private const string PlainInMbox = Separator + EmlxFiles.Message + "\n";
        private const string PlainInEmlx = "57\n" + EmlxFiles.Message + EmptyMetadata;

        // Lines that mboxrd quotes in the file, and EMLX holds as they are, in a message of 96 octets.
        private const string FromLinesHeader = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\n\n";
        private const string FromLinesInMbox = Separator + FromLinesHeader + ">From here\n>>From there\n>>>From far\n\nnot From\n\n";
        private const string FromLinesInEmlx = "96\n" + FromLinesHeader + "From here\n>From there\n>>From far\n\nnot From\n" + EmptyMetadata;

        private const string AttachmentInMbox = Separator + EmlxFiles.AttachmentMessage + "\n";
        private const string AttachmentInEmlx = "267\n" + EmlxFiles.AttachmentMessage + EmptyMetadata;

        private static readonly MboxEnvelope Envelope = new MboxEnvelope(new ImfMailbox("a", "b.test"), new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        public static TheoryData<string, string> SameMessageInEachFormat => new TheoryData<string, string>
        {
            { PlainInMbox, PlainInEmlx },
            { FromLinesInMbox, FromLinesInEmlx },
            { AttachmentInMbox, AttachmentInEmlx },
        };

        [Theory]
        [MemberData(nameof(SameMessageInEachFormat))]
        public void WriteEntry_MessageFromAnMboxFile_WritesTheEmlxFileOfTheSameMessage(string mailbox, string expected)
        {
            // arrange
            var entry = new EmlxEntry(Mailboxes.ReadFirst(mailbox).Message, new EmlxMetadata());

            // act
            var testResult = EmlxFiles.Write(entry);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(SameMessageInEachFormat))]
        public void WriteMessage_MessageFromAnEmlxFile_WritesTheMboxFileOfTheSameMessage(string expected, string emlxFile)
        {
            // arrange
            var message = EmlxFiles.Read(emlxFile).Message;

            // act
            var testResult = Mailboxes.WriteOne(Envelope, message);

            // assert
            testResult.Should().Be(expected);
        }
    }
}
