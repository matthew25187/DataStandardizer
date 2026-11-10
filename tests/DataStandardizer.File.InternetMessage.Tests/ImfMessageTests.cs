using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of the typed accessors of <see cref="ImfMessage"/>, over the messages of RFC 5322 Appendix A.
    /// </summary>
    public class ImfMessageTests
    {
        private const string MinimalHeader = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\n";
        private const string MinimalMessage = MinimalHeader + "\r\n";
        private const string FoldedSubjectMessage = "From: a@b.test\r\nSubject: Hello\r\n  folded   world\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\n\r\nBody\r\n";
        private const string NewMessage = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\nSubject: Hi\r\n\r\n";

        private const string SampleDate = "Fri, 21 Nov 1997 09:55:06 -0600";
        private const string SampleAddress = "a@b.test";
        private const string OtherAddress = "c@d.test";
        private const string SampleId = "<1@b.test>";
        private const string OtherId = "<2@b.test>";
        private const string TwoAddresses = "a@b.test, c@d.test";

        // "\x00E9" in UTF-8, as RFC 2047 "Q" encoding has it.
        private const string EncodedAcute = "=?UTF-8?Q?=C3=A9?=";
        private const string Acute = "\x00E9";
        private const string AcuteOctets = "\x00C3\x00A9";

        // e-acute as a writer encodes it.
        private const string WrittenAcute = "=?UTF-8?B?w6k=?=";

        private const string SeveralComments = MinimalHeader + "Comments: One\r\nComments: " + EncodedAcute + "\r\n\r\n";
        private const string SeveralKeywords = MinimalHeader + "Keywords: one, two\r\nKeywords: three\r\n\r\n";
        private const string EncodedSubject = MinimalHeader + "Subject: " + EncodedAcute + "\r\n\r\n";
        private const string Utf8Subject = MinimalHeader + "Subject: " + AcuteOctets + "\r\n\r\n";
        private const string RepeatedFrom = "Date: " + SampleDate + "\r\nFrom: a@b.test\r\nFrom: c@d.test\r\n\r\n";
        private const string InvalidFrom = "Date: " + SampleDate + "\r\nFrom: nobody\r\n\r\n";
        private const string Utf8To = MinimalHeader + "To: " + AcuteOctets + " <c@d.test>\r\n\r\n";
        private const string EncodedTo = MinimalHeader + "To: " + WrittenAcute + " <c@d.test>\r\n\r\n";
        private const string RepeatedFromApart = "From: a@b.test\r\nX-A: 1\r\nFrom: c@d.test\r\nDate: " + SampleDate + "\r\n\r\n";
        private const string RepeatedFromReplaced = "From: a@b.test, c@d.test\r\nX-A: 1\r\nDate: " + SampleDate + "\r\nSender: a@b.test\r\n\r\n";
        private const string TwoResentBlocks = "Resent-Date: " + SampleDate + "\r\nResent-From: a@b.test\r\nResent-Cc: c@d.test\r\nResent-Date: " + Rfc5322Examples.A3ResentDate + "\r\nResent-From: c@d.test\r\nResent-Sender: a@b.test\r\nResent-Bcc:\r\n" + MinimalMessage;
        private const string SenderGroupOfTwo = MinimalHeader + "Sender: G: a@b.test, c@d.test;\r\n\r\n";
        private const string SenderGroupOfOne = MinimalHeader + "Sender: G: a@b.test;\r\n\r\n";
        private const string ResentSenderGroupOfTwo = "Resent-Date: " + SampleDate + "\r\nResent-From: a@b.test\r\nResent-Sender: G: a@b.test, c@d.test;\r\n" + MinimalMessage;
        private const string ResentFieldsApart = "Resent-Date: " + SampleDate + "\r\nX-A: 1\r\nResent-From: a@b.test\r\n" + MinimalMessage;
        private const string ResentReplyTo = "Resent-Date: " + SampleDate + "\r\nResent-From: a@b.test\r\nResent-Reply-To: c@d.test\r\n" + MinimalMessage;

        public static TheoryData<string, Func<ImfMessage, string>, string> Accessors => new TheoryData<string, Func<ImfMessage, string>, string>
        {
            { Rfc5322Messages.A11, m => m.From.ToString(), Rfc5322Examples.A11From },
            { Rfc5322Messages.A11, m => m.To.ToString(), "Mary Smith <mary@example.net>" },
            { Rfc5322Messages.A11, m => m.Subject, "Saying Hello" },
            { Rfc5322Messages.A11, m => m.Date.ToString(), Rfc5322Examples.A11Date },
            { Rfc5322Messages.A11, m => m.MessageId.ToString(), Rfc5322Examples.A11MessageId },
            { Rfc5322Messages.A11Sender, m => m.Sender.ToString(), "Michael Jones <mjones@machine.example>" },
            { Rfc5322Messages.A12, m => m.From.ToString(), Rfc5322Examples.A12From },
            { Rfc5322Messages.A12, m => m.To.ToString(), Rfc5322Examples.A12To },
            { Rfc5322Messages.A12, m => m.Cc.ToString(), Rfc5322Examples.A12CcModern },
            { Rfc5322Messages.A12, m => m.Date.ToString(), Rfc5322Examples.A12Date },
            { Rfc5322Messages.A12, m => m.MessageId.ToString(), Rfc5322Examples.A12MessageId },
            { Rfc5322Messages.A13, m => m.To.ToString(), Rfc5322Examples.A13ToModern },
            { Rfc5322Messages.A13, m => m.Cc.ToString(), Rfc5322Examples.A13Cc },
            { Rfc5322Messages.A2Reply, m => m.ReplyTo.ToString(), "\"Mary Smith: Personal Account\" <smith@home.example>" },
            { Rfc5322Messages.A2Reply, m => m.Date.ToString(), Rfc5322Examples.A2ReplyDate },
            { Rfc5322Messages.A2Reply, m => string.Join(" ", m.InReplyTo), Rfc5322Examples.A11MessageId },
            { Rfc5322Messages.A2Reply, m => string.Join(" ", m.References), Rfc5322Examples.A11MessageId },
            { Rfc5322Messages.A2ReplyToReply, m => string.Join(" ", m.InReplyTo), "<3456@example.net>" },
            { Rfc5322Messages.A2ReplyToReply, m => string.Join(" ", m.References), Rfc5322Examples.A2References },
            { Rfc5322Messages.A5, m => m.From.ToString(), Rfc5322Examples.A5FromModern },
            { Rfc5322Messages.A5, m => m.To.ToString(), Rfc5322Examples.A5ToModern },
            { Rfc5322Messages.A5, m => m.Cc.ToString(), Rfc5322Examples.A5CcModern },
            { Rfc5322Messages.A5, m => m.Date.ToString(), Rfc5322Examples.A5DateModern },
            { Rfc5322Messages.A5, m => m.MessageId.ToString(), "<testabcd.1234@silly.test>" },
            { Rfc5322Messages.A61, m => m.From.ToString(), Rfc5322Examples.A61FromModern },
            { Rfc5322Messages.A61, m => m.To.ToString(), Rfc5322Examples.A61ToModern },
            { Rfc5322Messages.A62, m => m.Date.ToString(), Rfc5322Examples.A62DateModern },
            { Rfc5322Messages.A63, m => m.From.ToString(), Rfc5322Examples.A63FromModern },
            { Rfc5322Messages.A63, m => m.To.ToString(), Rfc5322Examples.A63ToModern },
            { Rfc5322Messages.A63, m => m.Subject, "Saying Hello" },
            { Rfc5322Messages.A63, m => m.Date.ToString(), Rfc5322Examples.A11Date },
            { Rfc5322Messages.A63, m => m.MessageId.ToString(), Rfc5322Examples.A11MessageId },
        };

        public static TheoryData<Func<ImfMessage, object>> AbsentAccessors => new TheoryData<Func<ImfMessage, object>>
        {
            m => m.Sender,
            m => m.ReplyTo,
            m => m.To,
            m => m.Cc,
            m => m.Bcc,
            m => m.MessageId,
            m => m.InReplyTo,
            m => m.References,
            m => m.Subject,
        };

        public static TheoryData<Action<ImfMessage>, string> Changes => new TheoryData<Action<ImfMessage>, string>
        {
            { m => m.Subject = "New", "From: a@b.test\r\nSubject: New\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\n\r\nBody\r\n" },
            { m => m.Subject = null, "From: a@b.test\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\n\r\nBody\r\n" },
            { m => m.From = ImfAddressList.Parse(OtherAddress), "From: c@d.test\r\nSubject: Hello\r\n  folded   world\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\n\r\nBody\r\n" },
            { m => m.Date = ImfDateTime.Parse("1 Jul 2003 10:52:37 +0200"), "From: a@b.test\r\nSubject: Hello\r\n  folded   world\r\nDate: 1 Jul 2003 10:52:37 +0200\r\nX-Custom:   kept \r\n as it is\r\n\r\nBody\r\n" },
            { m => m.Sender = ImfAddress.Parse(OtherAddress), "From: a@b.test\r\nSubject: Hello\r\n  folded   world\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\nSender: c@d.test\r\n\r\nBody\r\n" },
            { m => m.ReplyTo = ImfAddressList.Parse(OtherAddress), "From: a@b.test\r\nSubject: Hello\r\n  folded   world\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\nReply-To: c@d.test\r\n\r\nBody\r\n" },
            { m => m.To = ImfAddressList.Parse(OtherAddress), "From: a@b.test\r\nSubject: Hello\r\n  folded   world\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\nTo: c@d.test\r\n\r\nBody\r\n" },
            { m => m.Cc = ImfAddressList.Parse(OtherAddress), "From: a@b.test\r\nSubject: Hello\r\n  folded   world\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\nCc: c@d.test\r\n\r\nBody\r\n" },
            { m => m.Bcc = ImfAddressList.Parse(OtherAddress), "From: a@b.test\r\nSubject: Hello\r\n  folded   world\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\nBcc: c@d.test\r\n\r\nBody\r\n" },
            { m => m.MessageId = ImfMessageId.Parse(SampleId), "From: a@b.test\r\nSubject: Hello\r\n  folded   world\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\nMessage-ID: <1@b.test>\r\n\r\nBody\r\n" },
            { m => m.InReplyTo = new[] { ImfMessageId.Parse(SampleId) }, "From: a@b.test\r\nSubject: Hello\r\n  folded   world\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\nIn-Reply-To: <1@b.test>\r\n\r\nBody\r\n" },
            { m => m.References = new[] { ImfMessageId.Parse(SampleId), ImfMessageId.Parse(OtherId) }, "From: a@b.test\r\nSubject: Hello\r\n  folded   world\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\nReferences: <1@b.test> <2@b.test>\r\n\r\nBody\r\n" },
            { m => m.Comments = new[] { "One", "Two" }, "From: a@b.test\r\nSubject: Hello\r\n  folded   world\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\nComments: One\r\nComments: Two\r\n\r\nBody\r\n" },
            { m => m.Keywords = new[] { ImfPhraseList.Parse("one, two"), ImfPhraseList.Parse("three") }, "From: a@b.test\r\nSubject: Hello\r\n  folded   world\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\nKeywords: one, two\r\nKeywords: three\r\n\r\nBody\r\n" },
            { m => m.Subject = Acute, "From: a@b.test\r\nSubject: " + WrittenAcute + "\r\nDate: Fri, 21 Nov 1997 09:55:06 -0600\r\nX-Custom:   kept \r\n as it is\r\n\r\nBody\r\n" },
        };

        public static TheoryData<string> ValidMessagesWithNoBccAddresses => new TheoryData<string>
        {
            MinimalHeader + "Bcc:\r\n\r\n",
            MinimalHeader + "Bcc: (none)\r\n\r\n",
            MinimalHeader + "Bcc: , ,\r\n\r\n",
        };

        [Theory]
        [MemberData(nameof(Accessors))]
        public void Accessor_AppendixAMessage_GivesTheValue(string message, Func<ImfMessage, string> accessor, string expected)
        {
            // arrange
            var subject = ImfMessages.Read(message);

            // act
            var testResult = accessor(subject);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(AbsentAccessors))]
        public void Accessor_FieldAbsent_GivesNull(Func<ImfMessage, object> accessor)
        {
            // arrange
            var subject = ImfMessages.Read(MinimalMessage);

            // act
            var testResult = accessor(subject);

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void Date_FieldAbsent_GivesNull()
        {
            // arrange
            var subject = new ImfMessage();

            // act
            var testResult = subject.Date;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void Comments_SeveralFields_GivesTheDecodedTextOfEach()
        {
            // arrange
            var subject = ImfMessages.Read(SeveralComments);

            // act
            var testResult = subject.Comments;

            // assert
            testResult.Should().Equal("One", Acute);
        }

        [Fact]
        public void Keywords_SeveralFields_GivesThePhrasesOfEach()
        {
            // arrange
            var subject = ImfMessages.Read(SeveralKeywords);

            // act
            var testResult = subject.Keywords;

            // assert
            testResult.Should().HaveCount(2);
            testResult[0].Should().Equal("one", "two");
            testResult[1].Should().Equal("three");
        }

        [Fact]
        public void Subject_EncodedWord_IsDecoded()
        {
            // arrange
            var subject = ImfMessages.Read(EncodedSubject);

            // act
            var testResult = subject.Subject;

            // assert
            testResult.Should().Be(Acute);
        }

        [Fact]
        public void Subject_Utf8Field_IsDecoded()
        {
            // arrange
            var subject = ImfMessages.Read(Utf8Subject);

            // act
            var testResult = subject.Subject;

            // assert
            testResult.Should().Be(Acute);
        }

        [Fact]
        public void From_RepeatedFieldAllowed_GivesTheFirst()
        {
            // arrange
            var subject = ImfMessages.Read(RepeatedFrom, ImfMessages.AcceptAll());

            // act
            var testResult = subject.From;

            // assert
            testResult.ToString().Should().Be(SampleAddress);
        }

        [Fact]
        public void From_InvalidBodyAllowed_GivesNull()
        {
            // arrange
            var subject = ImfMessages.Read(InvalidFrom, ImfMessages.AcceptAll());

            // act
            var testResult = subject.From;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void Sender_GroupOfTwoAllowed_GivesNull()
        {
            // arrange
            var subject = ImfMessages.Read(SenderGroupOfTwo, ImfMessages.AcceptAll());

            // act
            var testResult = subject.Sender;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void Sender_GroupOfOne_GivesTheGroup()
        {
            // arrange
            var subject = ImfMessages.Read(SenderGroupOfOne);

            // act
            var testResult = subject.Sender;

            // assert
            testResult.Should().BeOfType<ImfGroup>();
        }

        [Fact]
        public void Sender_ResentGroupOfTwoAllowed_GivesNull()
        {
            // arrange
            var subject = ImfMessages.Read(ResentSenderGroupOfTwo, ImfMessages.AcceptAll());

            // act
            var testResult = subject.ResentBlocks[0].Sender;

            // assert
            testResult.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(ValidMessagesWithNoBccAddresses))]
        public void Bcc_NoAddresses_GivesNullAndKeepsTheField(string message)
        {
            // arrange
            var subject = ImfMessages.Read(message);

            // act
            var testResult = subject.Bcc;
            var hasField = subject.Fields.Contains("Bcc");

            // assert
            testResult.Should().BeNull();
            hasField.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(Changes))]
        public void WriteMessage_OneAccessorSet_RewritesThatFieldOnly(Action<ImfMessage> change, string expected)
        {
            // arrange
            var subject = ImfMessages.Read(FoldedSubjectMessage);
            change(subject);

            // act
            var testResult = ImfMessages.Write(subject);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void WriteMessage_NewMessage_WritesWhatWasSetAndTheEmptyLine()
        {
            // arrange
            var subject = new ImfMessage
            {
                Date = ImfDateTime.Parse(SampleDate),
                From = ImfAddressList.Parse(SampleAddress),
                Subject = "Hi",
            };

            // act
            var testResult = ImfMessages.Write(subject);

            // assert
            testResult.Should().Be(NewMessage);
        }

        [Fact]
        public void WriteMessage_SubjectSetAndUtf8Writer_WritesUtf8()
        {
            // arrange
            var subject = ImfMessages.Read(MinimalMessage);
            subject.Subject = Acute;
            var options = new ImfOptions { HeaderEncoding = ImfHeaderEncoding.Utf8 };

            // act
            var testResult = ImfMessages.Write(subject, options);

            // assert
            testResult.Should().Be(Utf8Subject);
        }

        [Fact]
        public void WriteMessage_DisplayNameSetAndUtf8Writer_WritesUtf8()
        {
            // arrange
            var subject = ImfMessages.Read(MinimalMessage);
            subject.To = new ImfAddressList(new[] { new ImfMailbox(Acute, "c", "d.test") });
            var options = new ImfOptions { HeaderEncoding = ImfHeaderEncoding.Utf8 };

            // act
            var testResult = ImfMessages.Write(subject, options);

            // assert
            testResult.Should().Be(Utf8To);
        }

        [Fact]
        public void WriteMessage_DisplayNameSetAndDefaultWriter_WritesEncodedWords()
        {
            // arrange
            var subject = ImfMessages.Read(MinimalMessage);
            subject.To = new ImfAddressList(new[] { new ImfMailbox(Acute, "c", "d.test") });

            // act
            var testResult = ImfMessages.Write(subject);

            // assert
            testResult.Should().Be(EncodedTo);
        }

        [Fact]
        public void From_SetOverRepeatedFields_LeavesOneWhereTheFirstWas()
        {
            // arrange
            var subject = ImfMessages.Read(RepeatedFromApart, ImfMessages.AcceptAll());
            subject.From = ImfAddressList.Parse(TwoAddresses);
            subject.Sender = ImfAddress.Parse(SampleAddress);

            // act
            var testResult = ImfMessages.Write(subject);

            // assert
            testResult.Should().Be(RepeatedFromReplaced);
        }

        [Fact]
        public void Date_SetToNull_RemovesTheField()
        {
            // arrange
            var subject = ImfMessages.Read(MinimalMessage);
            subject.Date = null;

            // act
            var testResult = subject.Fields.Contains("Date");

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Date_SetToDefault_Throws()
        {
            // arrange
            var subject = new ImfMessage();

            // act
            Action testAction = () => subject.Date = default(ImfDateTime);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void InReplyTo_SetToEmpty_RemovesTheField()
        {
            // arrange
            var subject = ImfMessages.Read(Rfc5322Messages.A2Reply);
            subject.InReplyTo = new ImfMessageId[0];

            // act
            var testResult = subject.Fields.Contains("In-Reply-To");

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void InReplyTo_SetWithNullElement_Throws()
        {
            // arrange
            var subject = new ImfMessage();

            // act
            Action testAction = () => subject.InReplyTo = new ImfMessageId[] { null };

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Comments_SetToNull_Throws()
        {
            // arrange
            var subject = new ImfMessage();

            // act
            Action testAction = () => subject.Comments = null;

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Keywords_SetToNull_Throws()
        {
            // arrange
            var subject = new ImfMessage();

            // act
            Action testAction = () => subject.Keywords = null;

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Subject_SetWithLineBreak_Throws()
        {
            // arrange
            var subject = new ImfMessage();

            // act
            Action testAction = () => subject.Subject = "a\r\nBcc: x@y.test";

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void Constructor_NullOptions_Throws()
        {
            // act
            Action testAction = () => new ImfMessage(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ResentBlocks_AppendixAMessage_GivesTheOneBlock()
        {
            // arrange
            var subject = ImfMessages.Read(Rfc5322Messages.A3);

            // act
            var testResult = subject.ResentBlocks;

            // assert
            testResult.Should().ContainSingle();
            testResult[0].From.ToString().Should().Be("Mary Smith <mary@example.net>");
            testResult[0].To.ToString().Should().Be("Jane Brown <j-brown@other.example>");
            testResult[0].Date.ToString().Should().Be(Rfc5322Examples.A3ResentDate);
            testResult[0].MessageId.ToString().Should().Be("<78910@example.net>");
            testResult[0].Sender.Should().BeNull();
            testResult[0].Cc.Should().BeNull();
            testResult[0].Bcc.Should().BeNull();
            testResult[0].Fields.Should().HaveCount(4);
        }

        [Fact]
        public void ResentBlocks_NoResentFields_GivesNone()
        {
            // arrange
            var subject = ImfMessages.Read(Rfc5322Messages.A11);

            // act
            var testResult = subject.ResentBlocks;

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void ResentBlocks_TwoBlocks_GivesThemInOrder()
        {
            // arrange
            var subject = ImfMessages.Read(TwoResentBlocks);

            // act
            var testResult = subject.ResentBlocks;

            // assert
            testResult.Should().HaveCount(2);
            testResult[0].From.ToString().Should().Be(SampleAddress);
            testResult[0].Cc.ToString().Should().Be(OtherAddress);
            testResult[1].From.ToString().Should().Be(OtherAddress);
            testResult[1].Sender.ToString().Should().Be(SampleAddress);
            testResult[1].Date.ToString().Should().Be(Rfc5322Examples.A3ResentDate);
        }

        [Fact]
        public void ResentBlocks_ResentFieldsSeparatedByAnotherField_GivesTwoBlocks()
        {
            // arrange
            var subject = ImfMessages.Read(ResentFieldsApart, ImfMessages.AcceptAll());

            // act
            var testResult = subject.ResentBlocks;

            // assert
            testResult.Should().HaveCount(2);
        }

        [Fact]
        public void ResentBlocks_ObsoleteResentReplyTo_IsAmongTheFieldsOfTheBlock()
        {
            // arrange
            var subject = ImfMessages.Read(ResentReplyTo);

            // act
            var testResult = subject.ResentBlocks;

            // assert
            testResult.Should().ContainSingle();
            testResult[0].Fields.Should().HaveCount(3);
        }

        [Fact]
        public void ResentBlocks_ListCast_CannotBeChanged()
        {
            // arrange
            var subject = ImfMessages.Read(Rfc5322Messages.A3);

            // act
            var testResult = subject.ResentBlocks;

            // assert
            testResult.Should().BeAssignableTo<ICollection<ImfResentBlock>>().Which.IsReadOnly.Should().BeTrue();
        }
    }
}
