using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of the test helper <see cref="Mailboxes"/>.
    /// </summary>
    public class MailboxesTests
    {
        private const int RepeatCount = 3;
        private const string GoodMessage = "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\n\ny\n";
        private const string RejectedThenGood = "From a@b.test Thu Jan  1 00:00:00 2026\nSubject: no date or from\n\nx\n\nFrom a@b.test Thu Jan  1 00:00:00 2026\nDate: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\n\ny\n\n";
        private const string RejectedIndexZero = "rejected 0";

        [Fact]
        public void Repeat_Entry_GivesItThatManyTimes()
        {
            // act
            var testResult = Mailboxes.Repeat("ab", RepeatCount);

            // assert
            Octets.ToText(testResult).Should().Be("ababab");
        }

        [Fact]
        public void ReadEach_FailedRead_GivesItsIndexAndCarriesOn()
        {
            // act
            var testResult = Mailboxes.ReadEach(RejectedThenGood);

            // assert
            testResult.Should().Equal(RejectedIndexZero, GoodMessage);
        }
    }
}
