using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// An unpaired surrogate is not text at all, and UTF-8 cannot hold one.  These cases are delegates because a test runner replaces an unpaired
    /// surrogate in a string theory argument when it serializes it, so the code would never see one.
    /// </summary>
    public class UnpairedSurrogateTests
    {
        private const string HighSurrogate = "\xD800";
        private const string LowSurrogate = "\xDC00";
        private const string MailboxWithSurrogate = "a" + LowSurrogate + "@b";
        private const string MailboxStartingWithSurrogate = HighSurrogate + "@b";

        public static TheoryData<Action> ParseCalls => new TheoryData<Action>
        {
            () => ImfAddress.Parse(MailboxStartingWithSurrogate),
            () => ImfMailbox.Parse(MailboxStartingWithSurrogate),
            () => ImfMailbox.Parse(MailboxWithSurrogate),
            () => ImfGroup.Parse(HighSurrogate + ":;"),
            () => ImfAddressList.Parse(MailboxStartingWithSurrogate),
            () => ImfPhraseList.Parse(HighSurrogate),
            () => ImfMessageId.Parse("<" + MailboxStartingWithSurrogate + ">"),
            () => ImfMessageId.ParseList(HighSurrogate),
        };

        public static TheoryData<Func<bool>> TryParseCalls => new TheoryData<Func<bool>>
        {
            () => ImfAddress.TryParse(MailboxStartingWithSurrogate, out _),
            () => ImfMailbox.TryParse(MailboxStartingWithSurrogate, out _),
            () => ImfMailbox.TryParse(MailboxWithSurrogate, out _),
            () => ImfGroup.TryParse(HighSurrogate + ":;", out _),
            () => ImfAddressList.TryParse(MailboxStartingWithSurrogate, out _),
            () => ImfPhraseList.TryParse(HighSurrogate, out _),
            () => ImfMessageId.TryParse("<" + MailboxStartingWithSurrogate + ">", out _),
            () => ImfMessageId.TryParseList(HighSurrogate, out _),
        };

        public static TheoryData<Action> ConstructorCalls => new TheoryData<Action>
        {
            () => new ImfMailbox(HighSurrogate, "a", "b"),
            () => new ImfMailbox(HighSurrogate + "a", "b"),
            () => new ImfMailbox(LowSurrogate, "b"),
            () => new ImfMailbox("a", HighSurrogate),
            () => new ImfMailbox("a", "[" + HighSurrogate + "]"),
            () => new ImfGroup(HighSurrogate, new ImfMailbox[0]),
            () => new ImfPhraseList(new[] { HighSurrogate }),
            () => new ImfMessageId(HighSurrogate, "b"),
            () => new ImfMessageId("a", LowSurrogate),
            () => new ImfMessageId("a", "[" + HighSurrogate + "]"),
        };

        [Theory]
        [MemberData(nameof(ParseCalls))]
        public void Parse_UnpairedSurrogate_ThrowsFormatException(Action call)
        {
            // act
            Action testAction = call;

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(TryParseCalls))]
        public void TryParse_UnpairedSurrogate_ReturnsFalse(Func<bool> call)
        {
            // act
            var testResult = call();

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(ConstructorCalls))]
        public void Constructor_UnpairedSurrogate_ThrowsArgumentException(Action call)
        {
            // act
            Action testAction = call;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }
    }
}
