using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="MboxEnvelope"/>, with lines in the form of RFC 4155 Appendix A: "From", a space, an addr-spec, a space and the output of ctime with no time zone.
    /// </summary>
    public class MboxEnvelopeTests
    {
        private const string Line = "From a@b.test Thu Jan  1 00:00:00 2026";
        private const string Address = "a@b.test";

        private const int Year = 2026;
        private const int Month = 1;
        private const int Day = 1;
        private const int TwoDigitDay = 31;
        private const int Hour = 13;
        private const int Minute = 5;
        private const int Second = 9;

        private static readonly DateTimeOffset FirstOfJanuary = new DateTimeOffset(Year, Month, Day, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public void Constructor_SenderAndDate_FormatsTheLine()
        {
            // arrange
            var date = new DateTimeOffset(Year, Month, Day, Hour, Minute, Second, TimeSpan.Zero);

            // act
            var testResult = new MboxEnvelope(new ImfMailbox("a", "b.test"), date);

            // assert
            testResult.ToString().Should().Be("From a@b.test Thu Jan  1 13:05:09 2026");
        }

        [Fact]
        public void Constructor_TwoDigitDay_HasNoPadding()
        {
            // arrange
            var date = new DateTimeOffset(Year, Month, TwoDigitDay, 0, 0, 0, TimeSpan.Zero);

            // act
            var testResult = new MboxEnvelope(new ImfMailbox("a", "b.test"), date);

            // assert
            testResult.ToString().Should().Be("From a@b.test Sat Jan 31 00:00:00 2026");
        }

        [Fact]
        public void Constructor_OffsetDate_WritesUtc()
        {
            // arrange
            var date = new DateTimeOffset(Year, Month, Day, Hour, 0, 0, TimeSpan.FromHours(Hour));

            // act
            var testResult = new MboxEnvelope(new ImfMailbox("a", "b.test"), date);

            // assert
            testResult.ToString().Should().Be(Line);
        }

        [Fact]
        public void Constructor_DisplayName_UsesOnlyTheAddress()
        {
            // act
            var testResult = new MboxEnvelope(new ImfMailbox("Someone", "a", "b.test"), FirstOfJanuary);

            // assert
            testResult.Sender.Address.Should().Be(Address);
        }

        [Fact]
        public void Constructor_NullSender_Throws()
        {
            // act
            Action testAction = () => new MboxEnvelope(null, FirstOfJanuary);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_ThreeDigitYear_Throws()
        {
            // arrange
            const int threeDigitYear = 999;
            var date = new DateTimeOffset(threeDigitYear, Month, Day, 0, 0, 0, TimeSpan.Zero);

            // act
            Action testAction = () => new MboxEnvelope(new ImfMailbox("a", "b.test"), date);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Constructor_NonAsciiAddress_Throws()
        {
            // act
            Action testAction = () => new MboxEnvelope(new ImfMailbox("é", "b.test"), FirstOfJanuary);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TryParse_ConformingLine_GivesSenderAndUtcDate()
        {
            // act
            MboxEnvelope.TryParse(Octets.Of(Line), false, out var testResult);

            // assert
            testResult.Sender.Address.Should().Be(Address);
            testResult.Date.Should().Be(FirstOfJanuary);
            testResult.IsConforming.Should().BeTrue();
            Octets.ToText(testResult.RawLine.Span).Should().Be(Line);
        }

        [Theory]
        [InlineData("From \"a b\"@c.test Thu Jan  1 00:00:00 2026")]
        [InlineData("From a@[192.0.2.1] Thu Jan  1 00:00:00 2026")]
        [InlineData("From a@b.test Sat Jan 31 23:59:59 2026")]
        public void TryParse_OtherConformingLines_AreSeparators(string line)
        {
            // act
            var testResult = MboxEnvelope.TryParse(Octets.Of(line), false, out _);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [InlineData("From a@b.test")]
        [InlineData("From  Thu Jan  1 00:00:00 2026")]
        [InlineData("From - Thu Jan  1 00:00:00 2026")]
        [InlineData("From MAILER-DAEMON Thu Jan  1 00:00:00 2026")]
        [InlineData("From a(comment)@b.test Thu Jan  1 00:00:00 2026")]
        [InlineData("From Name <a@b.test> Thu Jan  1 00:00:00 2026")]
        [InlineData("From a@b.test  Thu Jan  1 00:00:00 2026")]
        [InlineData("From a@b.test Thu Jan 01 00:00:00 2026")]
        [InlineData("From a@b.test Thu Jan 1 00:00:00 2026")]
        [InlineData("From a@b.test Fri Jan  1 00:00:00 2026")]
        [InlineData("From a@b.test Xxx Jan  1 00:00:00 2026")]
        [InlineData("From a@b.test Thu Xxx  1 00:00:00 2026")]
        [InlineData("From a@b.test Sat Feb 30 00:00:00 2026")]
        [InlineData("From a@b.test Thu Jan  1 24:00:00 2026")]
        [InlineData("From a@b.test Thu Jan  1 00:60:00 2026")]
        [InlineData("From a@b.test Thu Jan  1 00:00:60 2026")]
        [InlineData("From a@b.test Thu Jan  1 00:00:00 +0000 2026")]
        [InlineData("From a@b.test Thu Jan  1 00:00 2026")]
        [InlineData("From a@b.test Thu Jan  1 00:00:00 2026 ")]
        [InlineData("from a@b.test Thu Jan  1 00:00:00 2026")]
        public void TryParse_StrictLineBreakingTheForm_IsNotASeparator(string line)
        {
            // act
            var testResult = MboxEnvelope.TryParse(Octets.Of(line), false, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParse_ThunderbirdLine_HasNoSender()
        {
            // act
            MboxEnvelope.TryParse(Octets.Of("From - Thu Jan  1 00:00:00 2026"), true, out var testResult);

            // assert
            testResult.Sender.Should().BeNull();
            testResult.Date.Should().Be(FirstOfJanuary);
            testResult.IsConforming.Should().BeFalse();
        }

        [Theory]
        [InlineData("From a@b.test Thu Jan 1 00:00:00 2026")]
        [InlineData("From a@b.test Thu Jan 01 00:00:00 2026")]
        [InlineData("From a@b.test Thu Jan  1 00:00 2026")]
        [InlineData("From a@b.test Thu Jan  1 00:00:00 +0000 2026")]
        [InlineData("From a@b.test Thu Jan  1 00:00:00 2026 +0000")]
        [InlineData("From a@b.test Thu Jan  1 00:00:00 UTC 2026")]
        [InlineData("From a@b.test Thu Jan  1 00:00:00 GMT 2026")]
        [InlineData("From a@b.test\tThu Jan  1 00:00:00 2026")]
        [InlineData("From a@b.test Fri Jan  1 00:00:00 2026")]
        public void TryParse_RelaxedLine_GivesSenderAndDate(string line)
        {
            // act
            MboxEnvelope.TryParse(Octets.Of(line), true, out var testResult);

            // assert
            testResult.Sender.Address.Should().Be(Address);
            testResult.Date.Should().Be(FirstOfJanuary);
        }

        [Fact]
        public void TryParse_RelaxedNumericOffset_AppliesIt()
        {
            // arrange
            const int offsetHours = 5;
            var expected = new DateTimeOffset(Year, Month, Day, 0, 0, 0, TimeSpan.FromHours(-offsetHours));

            // act
            MboxEnvelope.TryParse(Octets.Of("From a@b.test Thu Jan  1 00:00:00 -0500 2026"), true, out var testResult);

            // assert
            testResult.Date.Should().Be(expected);
        }

        [Theory]
        [InlineData("From a@b.test Thu Jan  1 00:00:00 PST 2026")]
        [InlineData("From a@b.test Sun Feb 30 00:00:00 2026")]
        [InlineData("From a@b.test Thu Jan  1 25:00:00 2026")]
        [InlineData("From a@b.test Thu Jan  1 00:00:00 +1500 2026")]
        public void TryParse_RelaxedDateNotUnderstood_HasNoDate(string line)
        {
            // act
            MboxEnvelope.TryParse(Octets.Of(line), true, out var testResult);

            // assert
            testResult.Date.Should().BeNull();
        }

        [Theory]
        [InlineData("From here on we go")]
        [InlineData("From a@b.test")]
        [InlineData("From a@b.test Thu Jan")]
        [InlineData("From a@b.test Thu Xxx  1 00:00:00 2026")]
        [InlineData("From a@b.test Xxx Jan  1 00:00:00 2026")]
        [InlineData("From a@b.test Thu Jan 111 00:00:00 2026")]
        [InlineData("From a@b.test Thu Jan  1 0000 2026")]
        [InlineData("From a@b.test Thu Jan  1 00:00:00 20266")]
        [InlineData("From a@b.test Thu Jan  1 00:00:00 +00 2026")]
        [InlineData("From a@b.test Thu Jan  1 00:00:00 2026 extra words")]
        [InlineData("From a@b.test Thu Jan  1 00:00:00 2026\r")]
        public void TryParse_RelaxedLineWithNoDateAndTime_IsNotASeparator(string line)
        {
            // act
            var testResult = MboxEnvelope.TryParse(Octets.Of(line), true, out _);

            // assert
            testResult.Should().BeFalse();
        }
    }
}
