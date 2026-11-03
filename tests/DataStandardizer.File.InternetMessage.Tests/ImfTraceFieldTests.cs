using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="ImfTraceField"/>, through <see cref="ImfMessage.TraceFields"/>.
    /// </summary>
    public class ImfTraceFieldTests
    {
        private const string MinimalMessage = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\n\r\n";

        // ref. RFC 5322 §A.4
        private const string A4SecondReceivedDate = "21 Nov 1997 10:01:22 -0600";

        private const string ObsoleteReceived = "Received: from a.test by b.test\r\n" + MinimalMessage;
        private const string ReturnPathMailbox = "Return-Path: <x@y.test>\r\n" + MinimalMessage;
        private const string ReturnPathEmpty = "Return-Path: <>\r\n" + MinimalMessage;
        private const string FoldedReceivedField = "Received: from a.test\r\n by b.test; 21 Nov 1997 10:01:22 -0600\r\n";
        private const string FoldedReceived = FoldedReceivedField + MinimalMessage;
        private const string InvalidReceived = "Received: by 2002:a05::1; 21 Nov 1997 10:01:22 -0600\r\n" + MinimalMessage;
        private const string Prefix = "Received: ";
        private const string Suffix = "; 21 Nov 1997 10:01:22 -0600\r\n" + MinimalMessage;

        public static TheoryData<string, KeyValuePair<string, string>[]> ReceivedBodies => new TheoryData<string, KeyValuePair<string, string>[]>
        {
            { Prefix + "" + Suffix, new KeyValuePair<string, string>[0] },
            { Prefix + "from a.test" + Suffix, new[] { Pair("from", "a.test") } },
            { Prefix + "from a.test (comment [1.2.3.4]) by b.test" + Suffix, new[] { Pair("from", "a.test"), Pair("by", "b.test") } },
            { Prefix + "from [192.0.2.1] for x@y.test" + Suffix, new[] { Pair("from", "[192.0.2.1]"), Pair("for", "x@y.test") } },
            { Prefix + "for <x@y.test>" + Suffix, new[] { Pair("for", "<x@y.test>") } },
            { Prefix + "with \"quoted word\"" + Suffix, new[] { Pair("with", "quoted word") } },
            { Prefix + "from" + Suffix, new[] { Pair("from", string.Empty) } },
        };

        [Fact]
        public void TraceFields_AppendixAMessage_GivesEachInOrder()
        {
            // arrange
            var subject = ImfMessages.Read(Rfc5322Messages.A4);

            // act
            var testResult = subject.TraceFields;

            // assert
            testResult.Should().HaveCount(2);
            testResult[0].Kind.Should().Be(ImfTraceFieldKind.Received);
            testResult[0].ReceivedPairs.Should().Equal(
                Pair("from", "x.y.test"), Pair("by", "example.net"), Pair("via", "TCP"), Pair("with", "ESMTP"), Pair("id", "ABC12345"), Pair("for", "<mary@example.net>"));
            testResult[0].ReceivedDate.ToString().Should().Be(Rfc5322Examples.A4ReceivedDate);
            testResult[1].ReceivedPairs.Should().Equal(Pair("from", "node.example"), Pair("by", "x.y.test"));
            testResult[1].ReceivedDate.ToString().Should().Be(A4SecondReceivedDate);
        }

        [Theory]
        [MemberData(nameof(ReceivedBodies))]
        public void ReceivedPairs_ReceivedField_GivesTheTokensInPairs(string message, KeyValuePair<string, string>[] expected)
        {
            // arrange
            var subject = ImfMessages.Read(message);

            // act
            var testResult = subject.TraceFields[0].ReceivedPairs;

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void ReceivedDate_ObsoleteFieldWithNoDate_GivesNull()
        {
            // arrange
            var subject = ImfMessages.Read(ObsoleteReceived);

            // act
            var testResult = subject.TraceFields[0].ReceivedDate;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void ReturnPath_Mailbox_GivesTheMailbox()
        {
            // arrange
            var subject = ImfMessages.Read(ReturnPathMailbox);

            // act
            var testResult = subject.TraceFields[0];

            // assert
            testResult.Kind.Should().Be(ImfTraceFieldKind.ReturnPath);
            testResult.ReturnPath.Address.Should().Be("x@y.test");
            testResult.ReceivedPairs.Should().BeEmpty();
            testResult.ReceivedDate.Should().BeNull();
        }

        [Fact]
        public void ReturnPath_EmptyPath_GivesNull()
        {
            // arrange
            var subject = ImfMessages.Read(ReturnPathEmpty);

            // act
            var testResult = subject.TraceFields[0];

            // assert
            testResult.Kind.Should().Be(ImfTraceFieldKind.ReturnPath);
            testResult.ReturnPath.Should().BeNull();
        }

        [Fact]
        public void Field_TraceFieldFolded_KeepsItsRawBytes()
        {
            // arrange
            var subject = ImfMessages.Read(FoldedReceived);

            // act
            var testResult = subject.TraceFields[0].Field.RawBytes;

            // assert
            Octets.ToText(testResult.Span).Should().Be(FoldedReceivedField);
        }

        [Fact]
        public void TraceFields_NoTraceFields_GivesNone()
        {
            // arrange
            var subject = ImfMessages.Read(MinimalMessage);

            // act
            var testResult = subject.TraceFields;

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void ReceivedPairs_InvalidBodyAllowed_GivesNone()
        {
            // arrange
            var subject = ImfMessages.Read(InvalidReceived, ImfMessages.AcceptAll());

            // act
            var testResult = subject.TraceFields[0].ReceivedPairs;

            // assert
            testResult.Should().BeEmpty();
        }

        private static KeyValuePair<string, string> Pair(string name, string value)
        {
            return new KeyValuePair<string, string>(name, value);
        }
    }
}
