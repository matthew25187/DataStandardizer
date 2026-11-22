using System.Threading.Tasks;
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
        private const string RejectedThenGood = "From a@b.test Thu Jan  1 00:00:00 2026\nDate: yesterday\n\nx\n\nFrom a@b.test Thu Jan  1 00:00:00 2026\nDate: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\n\ny\n\n";
        private const string RejectedIndexZero = "rejected 0";
        private const string FirstField = "Subject: in\n ner\n";
        private const string FieldsInFile = "From a@b.test Thu Jan  1 00:00:00 2026\n" + FirstField + "Date: Thu, 1 Jan 2026 00:00:00 +0000\nFrom: a@b.test\n\ny\n\n";
        private const string SecondMessage = "Date: Fri, 2 Jan 2026 12:30:45 +0000\nFrom: c@d.test\n\ntwo\n";
        private const string TwoGoodMessages = "From a@b.test Thu Jan  1 00:00:00 2026\n" + GoodMessage + "\nFrom c@d.test Fri Jan  2 12:30:45 2026\n" + SecondMessage + "\n";

        [Fact]
        public void ReadSecondAfterSkipping_TwoMessages_DescribesTheSecond()
        {
            // act
            var testResult = Mailboxes.ReadSecondAfterSkipping(TwoGoodMessages, new ImfOptions());

            // assert
            testResult.Should().Be(SecondMessage);
        }

        [Fact]
        public async Task ReadSecondAfterSkippingAsync_TwoMessages_DescribesTheSecond()
        {
            // act
            var testResult = await Mailboxes.ReadSecondAfterSkippingAsync(TwoGoodMessages, new ImfOptions());

            // assert
            testResult.Should().Be(SecondMessage);
        }

        [Fact]
        public void WithFirstField_Mailbox_GivesTheFirstFieldOfItsFirstMessage()
        {
            // arrange
            var testResult = string.Empty;

            // act
            Mailboxes.WithFirstField(FieldsInFile, field => testResult = Octets.ToText(field.RawBytes));

            // assert
            testResult.Should().Be(FirstField);
        }

        [Fact]
        public async Task WithFirstFieldAsync_Mailbox_GivesTheFirstFieldOfItsFirstMessage()
        {
            // arrange
            var testResult = string.Empty;

            // act
            await Mailboxes.WithFirstFieldAsync(FieldsInFile, field =>
            {
                testResult = Octets.ToText(field.RawBytes);
                return Task.CompletedTask;
            });

            // assert
            testResult.Should().Be(FirstField);
        }

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
