using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of <see cref="VcardDateTime"/>, <see cref="VcardTimestamp"/> and <see cref="VcardDateAndOrTime"/>, the values that combine a date and a time.
    /// </summary>
    public class VcardDateTimeTests
    {
        [Theory]
        [InlineData(VcardValueExamples.DateTimeComplete)]
        [InlineData(VcardValueExamples.DateTimeMonthAndDay)]
        [InlineData(VcardValueExamples.DateTimeDay)]
        public void ToString_ParsedRfc6350DateTime_WritesTheSameText(string value)
        {
            // arrange
            var dateTime = VcardDateTime.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = dateTime.ToString(VcardValueFormat.Text);

            // assert
            testResult.Should().Be(value);
        }

        [Fact]
        public void TryParse_Rfc6350DateTime_GivesItsDateAndTime()
        {
            // arrange
            var expectedDate = VcardDate.FromMonthAndDay(10, 22);
            var expectedTime = new VcardTime(14, 0, null);

            // act
            VcardDateTime result;
            var testResult = VcardDateTime.TryParse(VcardValueExamples.DateTimeMonthAndDay.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Date.Should().Be(expectedDate);
            result.Time.Should().Be(expectedTime);
        }

        [Theory]
        [InlineData("1996-10T14")]
        [InlineData("1996T14")]
        [InlineData("--10T14")]
        [InlineData("19961022T-00")]
        [InlineData("19961022T--00")]
        [InlineData("19961022T")]
        [InlineData("T14")]
        [InlineData("19961022")]
        [InlineData("19961022t140000")]
        [InlineData("1996-10-22T14:00:00")]
        [InlineData("19961322T14")]
        [InlineData("19961022T25")]
        public void TryParse_InvalidVersion40DateTime_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardDateTime.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(VcardValueExamples.Version30DateTimeUtc, VcardVersion.V3_0)]
        [InlineData(VcardValueExamples.Version30DateTimeWithOffset, VcardVersion.V3_0)]
        [InlineData("19530915T231000", VcardVersion.V3_0)]
        [InlineData("1953-09-15T231000", VcardVersion.V3_0)]
        [InlineData(VcardValueExamples.Version21DateTimeBasic, VcardVersion.V2_1)]
        [InlineData(VcardValueExamples.Version21DateTimeExtended, VcardVersion.V2_1)]
        public void TryParse_LegacyVersion_AcceptsWhatItsGrammarAllows(string value, VcardVersion version)
        {
            // act
            var testResult = VcardDateTime.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [InlineData(VcardValueExamples.DateTimeMonthAndDay, VcardVersion.V3_0)]
        [InlineData("19961022T14", VcardVersion.V3_0)]
        [InlineData("1953-09-15T231000", VcardVersion.V2_1)]
        [InlineData("19530915T23:10:00", VcardVersion.V2_1)]
        public void TryParse_LegacyVersion_RejectsWhatItsGrammarDoesNotAllow(string value, VcardVersion version)
        {
            // act
            var testResult = VcardDateTime.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void ToString_Version21DateTimeInVersion40_WritesTheBasicFormat()
        {
            // arrange
            const string expected = "19951031T222710Z";
            var dateTime = VcardDateTime.Parse(VcardValueExamples.Version21DateTimeExtended.AsSpan(), VcardValueFormat.Text, VcardVersion.V2_1);

            // act
            var testResult = dateTime.ToString(VcardValueFormat.Text);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToString_Version40DateTimeInVersion30_WritesTheExtendedFormat()
        {
            // arrange
            const string expected = "1996-10-22T14:00:00";
            var dateTime = VcardDateTime.Parse(VcardValueExamples.DateTimeComplete.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = dateTime.ToString(VcardValueFormat.Text, VcardVersion.V3_0);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardVersion.V4_0, true)]
        [InlineData(VcardVersion.V3_0, false)]
        [InlineData(VcardVersion.V2_1, false)]
        public void IsDefinedIn_TruncatedDate_IsOnlyInVersion40(VcardVersion version, bool expected)
        {
            // arrange
            var dateTime = VcardDateTime.Parse(VcardValueExamples.DateTimeDay.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = dateTime.IsDefinedIn(version);

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<Action> InvalidDateTimeConstructors()
        {
            return new TheoryData<Action>
            {
                () => new VcardDateTime(VcardDate.FromYear(1996), new VcardTime(14, null, null)),
                () => new VcardDateTime(VcardDate.FromMonth(10), new VcardTime(14, null, null)),
                () => new VcardDateTime(new VcardDate(1996, 10, 22), new VcardTime(null, 22, null)),
                () => new VcardDateTime(default(VcardDate), new VcardTime(14, null, null)),
                () => new VcardDateTime(new VcardDate(1996, 10, 22), default(VcardTime)),
                () => new VcardTimestamp(VcardDate.FromMonthAndDay(10, 22), new VcardTime(14, 0, 0)),
                () => new VcardTimestamp(new VcardDate(1996, 10, 22), new VcardTime(14, 0, null)),
                () => new VcardTimestamp(default(VcardDate), new VcardTime(14, 0, 0)),
                () => new VcardDateAndOrTime(default(VcardDate)),
                () => new VcardDateAndOrTime(default(VcardTime)),
                () => new VcardDateAndOrTime(default(VcardDateTime)),
            };
        }

        [Theory]
        [MemberData(nameof(InvalidDateTimeConstructors), DisableDiscoveryEnumeration = true)]
        public void Constructor_PartsWithoutTheFormRequired_Throws(Action testAction)
        {
            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(VcardValueExamples.TimestampLocal)]
        [InlineData(VcardValueExamples.TimestampUtc)]
        [InlineData(VcardValueExamples.TimestampOffsetHour)]
        [InlineData(VcardValueExamples.TimestampOffsetHourAndMinute)]
        public void ToString_ParsedRfc6350Timestamp_WritesTheSameText(string value)
        {
            // arrange
            var timestamp = VcardTimestamp.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = timestamp.ToString(VcardValueFormat.Text);

            // assert
            testResult.Should().Be(value);
        }

        [Theory]
        [InlineData(VcardValueExamples.DateTimeMonthAndDay)]
        [InlineData(VcardValueExamples.DateTimeDay)]
        [InlineData("19961022T1400")]
        [InlineData("19961022T14")]
        [InlineData(VcardValueExamples.DateComplete)]
        public void TryParse_ValueThatIsNotComplete_IsNotATimestamp(string value)
        {
            // act
            var testResult = VcardTimestamp.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParse_Version30Revision_IsATimestamp()
        {
            // arrange
            const string value = "1995-10-31T22:27:10Z";

            // act
            VcardTimestamp result;
            var testResult = VcardTimestamp.TryParse(value.AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0, out result);

            // assert
            testResult.Should().BeTrue();
            result.ToString(VcardValueFormat.Text, VcardVersion.V3_0).Should().Be(value);
        }

        [Theory]
        [InlineData(VcardValueExamples.DateTimeComplete, VcardDateAndOrTimeKind.DateTime)]
        [InlineData(VcardValueExamples.DateTimeMonthAndDay, VcardDateAndOrTimeKind.DateTime)]
        [InlineData(VcardValueExamples.DateTimeDay, VcardDateAndOrTimeKind.DateTime)]
        [InlineData(VcardValueExamples.DateComplete, VcardDateAndOrTimeKind.Date)]
        [InlineData(VcardValueExamples.DateYearAndMonth, VcardDateAndOrTimeKind.Date)]
        [InlineData(VcardValueExamples.DateYear, VcardDateAndOrTimeKind.Date)]
        [InlineData(VcardValueExamples.DateMonthAndDay, VcardDateAndOrTimeKind.Date)]
        [InlineData(VcardValueExamples.DateDay, VcardDateAndOrTimeKind.Date)]
        [InlineData(VcardValueExamples.StandaloneTimeComplete, VcardDateAndOrTimeKind.Time)]
        [InlineData(VcardValueExamples.StandaloneTimeHourAndMinute, VcardDateAndOrTimeKind.Time)]
        [InlineData(VcardValueExamples.StandaloneTimeHour, VcardDateAndOrTimeKind.Time)]
        [InlineData(VcardValueExamples.StandaloneTimeMinuteAndSecond, VcardDateAndOrTimeKind.Time)]
        [InlineData(VcardValueExamples.StandaloneTimeSecond, VcardDateAndOrTimeKind.Time)]
        [InlineData(VcardValueExamples.StandaloneTimeUtc, VcardDateAndOrTimeKind.Time)]
        [InlineData(VcardValueExamples.StandaloneTimeWithOffset, VcardDateAndOrTimeKind.Time)]
        public void TryParse_Rfc6350DateAndOrTime_GivesItsKindAndWritesItBack(string value, VcardDateAndOrTimeKind expectedKind)
        {
            // act
            VcardDateAndOrTime result;
            var testResult = VcardDateAndOrTime.TryParse(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Kind.Should().Be(expectedKind);
            result.ToString().Should().Be(value);
        }

        [Theory]
        [InlineData("T25")]
        [InlineData("--1301")]
        [InlineData(VcardValueExamples.DateExtended)]
        [InlineData("T")]
        [InlineData("")]
        [InlineData("10")]
        [InlineData("1996-10T14")]
        public void TryParse_InvalidVersion40DateAndOrTime_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardDateAndOrTime.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(VcardValueExamples.DateExtended, VcardVersion.V3_0, true)]
        [InlineData(VcardValueExamples.Version30DateTimeUtc, VcardVersion.V3_0, true)]
        [InlineData(VcardValueExamples.StandaloneTimeComplete, VcardVersion.V3_0, false)]
        [InlineData(VcardValueExamples.DateYear, VcardVersion.V3_0, false)]
        [InlineData(VcardValueExamples.Version21DateBasic, VcardVersion.V2_1, true)]
        [InlineData(VcardValueExamples.Version21DateTimeExtended, VcardVersion.V2_1, true)]
        [InlineData(VcardValueExamples.StandaloneTimeComplete, VcardVersion.V2_1, false)]
        public void TryParse_LegacyDateAndOrTime_FollowsTheGrammarOfTheVersion(string value, VcardVersion version, bool expected)
        {
            // act
            var testResult = VcardDateAndOrTime.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardVersion.V4_0, true)]
        [InlineData(VcardVersion.V3_0, false)]
        [InlineData(VcardVersion.V2_1, false)]
        public void IsDefinedIn_TimeAlone_IsOnlyInVersion40(VcardVersion version, bool expected)
        {
            // arrange
            var value = new VcardDateAndOrTime(new VcardTime(10, 22, 0));

            // act
            var testResult = value.IsDefinedIn(version);

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<Func<VcardDateAndOrTime>, string> DateAndOrTimeConstructors()
        {
            return new TheoryData<Func<VcardDateAndOrTime>, string>
            {
                { () => new VcardDateAndOrTime(new VcardDate(1985, 4, 12)), VcardValueExamples.DateComplete },
                { () => new VcardDateAndOrTime(new VcardTime(10, 22, 0)), VcardValueExamples.StandaloneTimeComplete },
                { () => new VcardDateAndOrTime(new VcardDateTime(new VcardDate(1996, 10, 22), new VcardTime(14, 0, 0))), VcardValueExamples.DateTimeComplete },
            };
        }

        [Theory]
        [MemberData(nameof(DateAndOrTimeConstructors), DisableDiscoveryEnumeration = true)]
        public void ToString_DateAndOrTimeFromConstructor_WritesItsForm(Func<VcardDateAndOrTime> constructor, string expected)
        {
            // arrange
            var value = constructor();

            // act
            var testResult = value.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Date_TimeAlone_IsNull()
        {
            // arrange
            var value = new VcardDateAndOrTime(new VcardTime(10, 22, 0));

            // act
            var testResult = value.Date;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void Time_DateAlone_IsNull()
        {
            // arrange
            var value = new VcardDateAndOrTime(new VcardDate(1985, 4, 12));

            // act
            var testResult = value.Time;

            // assert
            testResult.Should().BeNull();
        }

        public static TheoryData<Action> UninitializedMembers()
        {
            return new TheoryData<Action>
            {
                () => _ = default(VcardDateTime).Date,
                () => _ = default(VcardDateTime).Time,
                () => default(VcardDateTime).IsDefinedIn(VcardVersion.V4_0),
                () => _ = default(VcardTimestamp).Date,
                () => _ = default(VcardTimestamp).Time,
                () => default(VcardTimestamp).IsDefinedIn(VcardVersion.V4_0),
                () => _ = default(VcardDateAndOrTime).Kind,
                () => _ = default(VcardDateAndOrTime).Date,
                () => _ = default(VcardDateAndOrTime).Time,
                () => default(VcardDateAndOrTime).IsDefinedIn(VcardVersion.V4_0),
            };
        }

        [Theory]
        [MemberData(nameof(UninitializedMembers), DisableDiscoveryEnumeration = true)]
        public void Member_UninitializedInstance_Throws(Action testAction)
        {
            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        public static TheoryData<Func<string>, string> UninitializedToString()
        {
            return new TheoryData<Func<string>, string>
            {
                { () => default(VcardDateTime).ToString(), typeof(VcardDateTime).FullName },
                { () => default(VcardTimestamp).ToString(), typeof(VcardTimestamp).FullName },
                { () => default(VcardDateAndOrTime).ToString(), typeof(VcardDateAndOrTime).FullName },
            };
        }

        [Theory]
        [MemberData(nameof(UninitializedToString), DisableDiscoveryEnumeration = true)]
        public void ToString_UninitializedInstance_GivesTheTypeName(Func<string> toStringCall, string expected)
        {
            // act
            var testResult = toStringCall();

            // assert
            testResult.Should().Be(expected);
        }
    }
}
