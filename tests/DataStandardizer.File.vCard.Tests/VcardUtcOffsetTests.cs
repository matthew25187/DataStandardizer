using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    public class VcardUtcOffsetTests
    {
        // ref. RFC 6350 §4.7: "+hhmm", and RFC 6350 §4: utc-offset = sign hour [minute].
        private const string HourAndMinute = "-0500";
        private const string HourOnly = "+01";

        // ref. RFC 2426 §2.4.4: utc-offset = ("+" / "-") time-hour ":" time-minute
        private const string Extended = "-05:00";

        [Theory]
        [InlineData(HourAndMinute, true, 5, 0)]
        [InlineData(HourOnly, false, 1, null)]
        [InlineData("-00", true, 0, null)]
        [InlineData("+2359", false, 23, 59)]
        public void TryParse_Version40Form_GivesItsParts(string value, bool isNegative, int hours, int? minutes)
        {
            // act
            VcardUtcOffset result;
            var testResult = VcardUtcOffset.TryParse(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.IsNegative.Should().Be(isNegative);
            result.Hours.Should().Be(hours);
            result.Minutes.Should().Be(minutes);
        }

        [Theory]
        [InlineData(HourAndMinute)]
        [InlineData(HourOnly)]
        [InlineData("-00")]
        public void ToString_ParsedVersion40Form_WritesTheSameText(string value)
        {
            // arrange
            var offset = VcardUtcOffset.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = offset.ToString(VcardValueFormat.Text);

            // assert
            testResult.Should().Be(value);
        }

        [Theory]
        [InlineData(Extended)]
        [InlineData("0500")]
        [InlineData("05")]
        [InlineData("-5")]
        [InlineData("-050")]
        [InlineData("-05000")]
        [InlineData("-2400")]
        [InlineData("-0560")]
        [InlineData("")]
        [InlineData("Z")]
        public void TryParse_InvalidVersion40Offset_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardUtcOffset.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(Extended, VcardVersion.V3_0, true)]
        [InlineData(HourAndMinute, VcardVersion.V3_0, false)]
        [InlineData(HourOnly, VcardVersion.V3_0, false)]
        [InlineData("-24:00", VcardVersion.V3_0, false)]
        [InlineData(VcardValueExamples.Version21OffsetBasic, VcardVersion.V2_1, true)]
        [InlineData(VcardValueExamples.Version21OffsetExtended, VcardVersion.V2_1, true)]
        [InlineData(HourOnly, VcardVersion.V2_1, true)]
        [InlineData("+2400", VcardVersion.V2_1, true)]
        [InlineData("+2500", VcardVersion.V2_1, false)]
        [InlineData("-05:0", VcardVersion.V2_1, false)]
        public void TryParse_LegacyVersion_FollowsItsGrammar(string value, VcardVersion version, bool expected)
        {
            // act
            var testResult = VcardUtcOffset.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardVersion.V4_0, HourAndMinute)]
        [InlineData(VcardVersion.V3_0, Extended)]
        [InlineData(VcardVersion.V2_1, Extended)]
        public void ToString_OffsetWithMinutes_WritesTheFormOfTheVersion(VcardVersion version, string expected)
        {
            // arrange
            var offset = new VcardUtcOffset(true, 5, 0);

            // act
            var testResult = offset.ToString(VcardValueFormat.Text, version);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardVersion.V4_0, HourOnly)]
        [InlineData(VcardVersion.V3_0, "+01:00")]
        [InlineData(VcardVersion.V2_1, HourOnly)]
        public void ToString_OffsetWithoutMinutes_WritesTheFormOfTheVersion(VcardVersion version, string expected)
        {
            // arrange
            var offset = new VcardUtcOffset(false, 1, null);

            // act
            var testResult = offset.ToString(VcardValueFormat.Text, version);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardVersion.V4_0, false)]
        [InlineData(VcardVersion.V3_0, false)]
        [InlineData(VcardVersion.V2_1, true)]
        public void IsDefinedIn_HourOf24_IsOnlyInVersion21(VcardVersion version, bool expected)
        {
            // arrange
            var offset = VcardUtcOffset.Parse("+2400".AsSpan(), VcardValueFormat.Text, VcardVersion.V2_1);

            // act
            var testResult = offset.IsDefinedIn(version);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToString_HourOf24_WritesTheVersion21Form()
        {
            // arrange
            var offset = VcardUtcOffset.Parse("+2400".AsSpan(), VcardValueFormat.Text, VcardVersion.V2_1);

            // act
            var testResult = offset.ToString();

            // assert
            testResult.Should().Be("+24:00");
        }

        [Fact]
        public void Offset_NegativeOffset_IsBehindUtc()
        {
            // arrange
            var expected = new TimeSpan(-5, -30, 0);
            var offset = new VcardUtcOffset(true, 5, 30);

            // act
            var testResult = offset.Offset;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Equals_OffsetWithAndWithoutItsMinutes_IsFalse()
        {
            // arrange
            var withMinutes = new VcardUtcOffset(true, 5, 0);
            var withoutMinutes = new VcardUtcOffset(true, 5, null);

            // act
            var testResult = withMinutes != withoutMinutes;

            // assert
            testResult.Should().BeTrue();
        }

        public static TheoryData<Action> OutOfRangeConstructors()
        {
            return new TheoryData<Action>
            {
                () => new VcardUtcOffset(false, 24, null),
                () => new VcardUtcOffset(false, -1, null),
                () => new VcardUtcOffset(false, 1, 60),
                () => new VcardUtcOffset(false, 1, -1),
            };
        }

        [Theory]
        [MemberData(nameof(OutOfRangeConstructors), DisableDiscoveryEnumeration = true)]
        public void Constructor_PartOutOfRange_Throws(Action testAction)
        {
            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        public static TheoryData<Action> UninitializedMembers()
        {
            return new TheoryData<Action>
            {
                () => _ = default(VcardUtcOffset).IsNegative,
                () => _ = default(VcardUtcOffset).Hours,
                () => _ = default(VcardUtcOffset).Minutes,
                () => _ = default(VcardUtcOffset).Offset,
                () => default(VcardUtcOffset).IsDefinedIn(VcardVersion.V4_0),
            };
        }

        [Theory]
        [MemberData(nameof(UninitializedMembers), DisableDiscoveryEnumeration = true)]
        public void Member_UninitializedInstance_Throws(Action testAction)
        {
            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ToString_UninitializedInstance_GivesTheTypeName()
        {
            // act
            var testResult = default(VcardUtcOffset).ToString();

            // assert
            testResult.Should().Be(typeof(VcardUtcOffset).FullName);
        }
    }
}
