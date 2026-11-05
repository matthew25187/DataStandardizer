using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    public class VcardTimeTests
    {
        [Theory]
        [InlineData(VcardValueExamples.TimeComplete, 10, 22, 0)]
        [InlineData(VcardValueExamples.TimeHourAndMinute, 10, 22, null)]
        [InlineData(VcardValueExamples.TimeHour, 10, null, null)]
        [InlineData(VcardValueExamples.TimeMinuteAndSecond, null, 22, 0)]
        [InlineData("-22", null, 22, null)]
        [InlineData(VcardValueExamples.TimeSecond, null, null, 0)]
        public void TryParse_Rfc6350Form_GivesItsParts(string value, int? hour, int? minute, int? second)
        {
            // act
            VcardTime result;
            var testResult = VcardTime.TryParse(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Hour.Should().Be(hour);
            result.Minute.Should().Be(minute);
            result.Second.Should().Be(second);
            result.IsUtc.Should().BeFalse();
            result.UtcOffset.Should().BeNull();
        }

        [Fact]
        public void TryParse_UtcDesignator_IsUtc()
        {
            // act
            VcardTime result;
            var testResult = VcardTime.TryParse(VcardValueExamples.TimeUtc.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.IsUtc.Should().BeTrue();
            result.UtcOffset.Should().BeNull();
        }

        [Fact]
        public void TryParse_UtcOffset_GivesTheOffset()
        {
            // arrange
            var expected = new VcardUtcOffset(true, 8, 0);

            // act
            VcardTime result;
            var testResult = VcardTime.TryParse(VcardValueExamples.TimeWithOffset.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.IsUtc.Should().BeFalse();
            result.UtcOffset.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardValueExamples.TimeComplete)]
        [InlineData(VcardValueExamples.TimeHourAndMinute)]
        [InlineData(VcardValueExamples.TimeHour)]
        [InlineData(VcardValueExamples.TimeMinuteAndSecond)]
        [InlineData("-22")]
        [InlineData(VcardValueExamples.TimeSecond)]
        [InlineData(VcardValueExamples.TimeUtc)]
        [InlineData(VcardValueExamples.TimeWithOffset)]
        [InlineData("10-08")]
        [InlineData("-22+01")]
        [InlineData("--00Z")]
        public void ToString_ParsedRfc6350Form_WritesTheSameText(string value)
        {
            // arrange
            var time = VcardTime.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = time.ToString(VcardValueFormat.Text);

            // assert
            testResult.Should().Be(value);
        }

        [Theory]
        [InlineData("25")]
        [InlineData("24")]
        [InlineData("1060")]
        [InlineData("102261")]
        [InlineData("-60")]
        [InlineData("--61")]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("10220")]
        [InlineData("10:22:00")]
        [InlineData("102200z")]
        [InlineData("102200,5")]
        [InlineData("102200-08:00")]
        [InlineData("102200-2400")]
        [InlineData("102200-0860")]
        [InlineData("102200Z-08")]
        [InlineData("---00")]
        public void TryParse_InvalidVersion40Time_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardTime.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParse_LeapSecond_IsValid()
        {
            // act
            VcardTime result;
            var testResult = VcardTime.TryParse("235960".AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Second.Should().Be(60);
        }

        [Theory]
        [InlineData("10:22:00", VcardVersion.V3_0)]
        [InlineData("102200", VcardVersion.V3_0)]
        [InlineData("10:2200", VcardVersion.V3_0)]
        [InlineData("1022:00", VcardVersion.V3_0)]
        [InlineData("10:22:00Z", VcardVersion.V3_0)]
        [InlineData("10:22:00-05:00", VcardVersion.V3_0)]
        [InlineData("102200-0500", VcardVersion.V3_0)]
        [InlineData("10:22:00,5", VcardVersion.V3_0)]
        [InlineData("10:22:00", VcardVersion.V2_1)]
        [InlineData("102200", VcardVersion.V2_1)]
        [InlineData("102200-05", VcardVersion.V2_1)]
        [InlineData("102200-0500", VcardVersion.V2_1)]
        [InlineData("10:22:00-05:00", VcardVersion.V2_1)]
        [InlineData("10:22:00Z", VcardVersion.V2_1)]
        public void TryParse_LegacyVersion_AcceptsWhatItsGrammarAllows(string value, VcardVersion version)
        {
            // act
            var testResult = VcardTime.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [InlineData("10:22:00-05:00", VcardVersion.V3_0)]
        [InlineData("102200-0500", VcardVersion.V3_0)]
        [InlineData("10:22:00-05:00", VcardVersion.V2_1)]
        [InlineData("102200-0500", VcardVersion.V2_1)]
        public void TryParse_LegacyVersion_KeepsTheUtcOffset(string value, VcardVersion version)
        {
            // arrange
            var expected = new VcardUtcOffset(true, 5, 0);

            // act
            VcardTime result;
            var testResult = VcardTime.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out result);

            // assert
            testResult.Should().BeTrue();
            result.UtcOffset.Should().Be(expected);
        }

        [Fact]
        public void TryParse_Version30LowerCaseUtcDesignator_IsUtcAndWritesUpperCase()
        {
            // act
            var testResult = VcardTime.Parse("10:22:00z".AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0);

            // assert
            testResult.IsUtc.Should().BeTrue();
            testResult.ToString(VcardValueFormat.Text, VcardVersion.V3_0).Should().Be("10:22:00Z");
        }

        [Theory]
        [InlineData("10:22:00z", VcardVersion.V2_1)]
        [InlineData("102200z", VcardVersion.V4_0)]
        public void TryParse_LowerCaseUtcDesignatorOutsideVersion30_ReturnsFalse(string value, VcardVersion version)
        {
            // act
            var testResult = VcardTime.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("24:00:00")]
        [InlineData("240000")]
        public void TryParse_EndOfDayInVersion21_HasAnHourOf24(string value)
        {
            // act
            VcardTime result;
            var testResult = VcardTime.TryParse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V2_1, out result);

            // assert
            testResult.Should().BeTrue();
            result.Hour.Should().Be(24);
        }

        [Theory]
        [InlineData("24:00:01")]
        [InlineData("24:01:00")]
        [InlineData("25:00:00")]
        public void TryParse_Hour24OtherThanTheEndOfDayInVersion21_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardTime.TryParse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V2_1, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(VcardVersion.V4_0, false)]
        [InlineData(VcardVersion.V3_0, false)]
        [InlineData(VcardVersion.V2_1, true)]
        public void IsDefinedIn_EndOfDay_IsOnlyInVersion21(VcardVersion version, bool expected)
        {
            // arrange
            var time = VcardTime.Parse("24:00:00".AsSpan(), VcardValueFormat.Text, VcardVersion.V2_1);

            // act
            var testResult = time.IsDefinedIn(version);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToString_EndOfDay_WritesTheVersion21Form()
        {
            // arrange
            var time = VcardTime.Parse("240000".AsSpan(), VcardValueFormat.Text, VcardVersion.V2_1);

            // act
            var testResult = time.ToString();

            // assert
            testResult.Should().Be("24:00:00");
        }

        [Theory]
        [InlineData("10", VcardVersion.V3_0)]
        [InlineData("1022", VcardVersion.V3_0)]
        [InlineData("10:22", VcardVersion.V3_0)]
        [InlineData("-2200", VcardVersion.V3_0)]
        [InlineData("10:22:00-05", VcardVersion.V3_0)]
        [InlineData("10:22:00,", VcardVersion.V3_0)]
        [InlineData("24:00:00", VcardVersion.V3_0)]
        [InlineData("10:2200", VcardVersion.V2_1)]
        [InlineData("1022:00", VcardVersion.V2_1)]
        [InlineData("10:22:00,5", VcardVersion.V2_1)]
        [InlineData("102200-05:00", VcardVersion.V2_1)]
        [InlineData("10:22:00-0500", VcardVersion.V2_1)]
        [InlineData("10:22:00+24:00", VcardVersion.V2_1)]
        [InlineData("102200+2400", VcardVersion.V2_1)]
        public void TryParse_LegacyVersion_RejectsWhatItsGrammarDoesNotAllow(string value, VcardVersion version)
        {
            // act
            var testResult = VcardTime.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(VcardValueExamples.TimeWithOffset, VcardVersion.V3_0, "10:22:00-08:00")]
        [InlineData(VcardValueExamples.TimeWithOffset, VcardVersion.V2_1, "10:22:00-08:00")]
        [InlineData(VcardValueExamples.TimeUtc, VcardVersion.V3_0, "10:22:00Z")]
        [InlineData("102200-08", VcardVersion.V3_0, "10:22:00-08:00")]
        [InlineData("102200-08", VcardVersion.V2_1, "10:22:00-08")]
        public void ToString_Version40TimeInLegacyVersion_WritesTheFormOfTheVersion(string value, VcardVersion version, string expected)
        {
            // arrange
            var time = VcardTime.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = time.ToString(VcardValueFormat.Text, version);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void SecondFraction_Rfc2425Fraction_GivesTheFractionAndWritesItBack()
        {
            // arrange
            const decimal expected = 0.25m;
            const string value = "10:22:00,25";

            // act
            var testResult = VcardTime.Parse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0);

            // assert
            testResult.SecondFraction.Should().Be(expected);
            testResult.ToString().Should().Be(value);
        }

        [Fact]
        public void SecondFraction_MoreDigitsThanKept_KeepsTheFirstEighteen()
        {
            // arrange
            const string value = "10:22:00,1234567890123456789";
            const string expected = "10:22:00,123456789012345678";

            // act
            var testResult = VcardTime.Parse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0);

            // assert
            testResult.ToString().Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardVersion.V4_0, false)]
        [InlineData(VcardVersion.V3_0, true)]
        [InlineData(VcardVersion.V2_1, false)]
        public void IsDefinedIn_TimeWithFraction_IsOnlyInVersion30(VcardVersion version, bool expected)
        {
            // arrange
            var time = VcardTime.Parse("10:22:00,5".AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0);

            // act
            var testResult = time.IsDefinedIn(version);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardVersion.V4_0, true)]
        [InlineData(VcardVersion.V3_0, false)]
        [InlineData(VcardVersion.V2_1, false)]
        public void IsDefinedIn_ReducedTime_IsOnlyInVersion40(VcardVersion version, bool expected)
        {
            // arrange
            var time = new VcardTime(10, null, null);

            // act
            var testResult = time.IsDefinedIn(version);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void TryFormat_TimeWithFractionInVersion40_Throws()
        {
            // arrange
            var time = VcardTime.Parse("10:22:00,5".AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0);
            var destination = new char[32];

            // act
            Action testAction = () => time.TryFormat(destination, out _, VcardValueFormat.Text);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        public static TheoryData<Func<VcardTime>, string> Constructors()
        {
            return new TheoryData<Func<VcardTime>, string>
            {
                { () => new VcardTime(10, 22, 0), VcardValueExamples.TimeComplete },
                { () => new VcardTime(10, null, null), VcardValueExamples.TimeHour },
                { () => new VcardTime(null, 22, 0), VcardValueExamples.TimeMinuteAndSecond },
                { () => new VcardTime(null, null, 0), VcardValueExamples.TimeSecond },
                { () => new VcardTime(10, 22, 0, true), VcardValueExamples.TimeUtc },
                { () => new VcardTime(10, 22, 0, new VcardUtcOffset(true, 8, 0)), VcardValueExamples.TimeWithOffset },
            };
        }

        [Theory]
        [MemberData(nameof(Constructors), DisableDiscoveryEnumeration = true)]
        public void ToString_TimeFromConstructor_WritesItsForm(Func<VcardTime> constructor, string expected)
        {
            // arrange
            var time = constructor();

            // act
            var testResult = time.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<Action> OutOfRangeConstructors()
        {
            return new TheoryData<Action>
            {
                () => new VcardTime(24, null, null),
                () => new VcardTime(-1, null, null),
                () => new VcardTime(10, 60, null),
                () => new VcardTime(10, 22, 61),
            };
        }

        [Theory]
        [MemberData(nameof(OutOfRangeConstructors), DisableDiscoveryEnumeration = true)]
        public void Constructor_PartOutOfRange_Throws(Action testAction)
        {
            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        public static TheoryData<Action> InvalidCombinations()
        {
            return new TheoryData<Action>
            {
                () => new VcardTime(null, null, null),
                () => new VcardTime(10, null, 0),
                () => new VcardTime(10, 22, 0, default(VcardUtcOffset)),
                () => new VcardTime(10, 22, 0, VcardUtcOffset.Parse("+2400".AsSpan(), VcardValueFormat.Text, VcardVersion.V2_1)),
            };
        }

        [Theory]
        [MemberData(nameof(InvalidCombinations), DisableDiscoveryEnumeration = true)]
        public void Constructor_NoFormForTheCombination_Throws(Action testAction)
        {
            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Equals_UtcDesignatorAndZeroOffset_IsFalse()
        {
            // arrange
            var utc = VcardTime.Parse("10Z".AsSpan(), VcardValueFormat.Text);
            var zeroOffset = VcardTime.Parse("10+00".AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = utc == zeroOffset;

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Equals_SameText_IsTrue()
        {
            // arrange
            var time = VcardTime.Parse(VcardValueExamples.TimeWithOffset.AsSpan(), VcardValueFormat.Text);
            var other = VcardTime.Parse(VcardValueExamples.TimeWithOffset.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = time == other;

            // assert
            testResult.Should().BeTrue();
            time.GetHashCode().Should().Be(other.GetHashCode());
        }

        public static TheoryData<Action> UninitializedMembers()
        {
            return new TheoryData<Action>
            {
                () => _ = default(VcardTime).Hour,
                () => _ = default(VcardTime).Minute,
                () => _ = default(VcardTime).Second,
                () => _ = default(VcardTime).SecondFraction,
                () => _ = default(VcardTime).IsUtc,
                () => _ = default(VcardTime).UtcOffset,
                () => default(VcardTime).IsDefinedIn(VcardVersion.V4_0),
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
            var testResult = default(VcardTime).ToString();

            // assert
            testResult.Should().Be(typeof(VcardTime).FullName);
        }
    }
}
