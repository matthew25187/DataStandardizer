using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    public class VcardDateTests
    {
        [Theory]
        [InlineData(VcardValueExamples.DateComplete, 1985, 4, 12)]
        [InlineData(VcardValueExamples.DateYearAndMonth, 1985, 4, null)]
        [InlineData(VcardValueExamples.DateYear, 1985, null, null)]
        [InlineData(VcardValueExamples.DateMonthAndDay, null, 4, 12)]
        [InlineData("--04", null, 4, null)]
        [InlineData(VcardValueExamples.DateDay, null, null, 12)]
        public void TryParse_Rfc6350Form_GivesItsParts(string value, int? year, int? month, int? day)
        {
            // act
            VcardDate result;
            var testResult = VcardDate.TryParse(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Year.Should().Be(year);
            result.Month.Should().Be(month);
            result.Day.Should().Be(day);
        }

        [Theory]
        [InlineData(VcardValueExamples.DateComplete)]
        [InlineData(VcardValueExamples.DateYearAndMonth)]
        [InlineData(VcardValueExamples.DateYear)]
        [InlineData(VcardValueExamples.DateMonthAndDay)]
        [InlineData("--04")]
        [InlineData(VcardValueExamples.DateDay)]
        public void ToString_ParsedRfc6350Form_WritesTheSameText(string value)
        {
            // arrange
            var date = VcardDate.Parse(value.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = date.ToString(VcardValueFormat.Text);

            // assert
            testResult.Should().Be(value);
        }

        [Theory]
        [InlineData("--1301")]
        [InlineData("--0001")]
        [InlineData("--0100")]
        [InlineData("--0132")]
        [InlineData("--0431")]
        [InlineData("--0230")]
        [InlineData("---00")]
        [InlineData("---32")]
        [InlineData("19850229")]
        [InlineData("19850431")]
        [InlineData("198504")]
        [InlineData("1985-13")]
        [InlineData("85")]
        [InlineData("")]
        [InlineData("1985041")]
        [InlineData("198504121")]
        [InlineData("-0412")]
        [InlineData("--041")]
        [InlineData("abcd")]
        [InlineData(VcardValueExamples.DateExtended)]
        public void TryParse_InvalidVersion40Date_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardDate.TryParse(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("--0229", null, 2, 29)]
        [InlineData("---31", null, null, 31)]
        [InlineData("19840229", 1984, 2, 29)]
        [InlineData("20000229", 2000, 2, 29)]
        [InlineData("00000229", 0, 2, 29)]
        [InlineData("0000", 0, null, null)]
        [InlineData("9999", 9999, null, null)]
        public void TryParse_EdgeOfTheRanges_IsValid(string value, int? year, int? month, int? day)
        {
            // act
            VcardDate result;
            var testResult = VcardDate.TryParse(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Year.Should().Be(year);
            result.Month.Should().Be(month);
            result.Day.Should().Be(day);
        }

        [Fact]
        public void TryParse_CenturyThatIsNotALeapYear_RejectsTheTwentyNinthOfFebruary()
        {
            // act
            var testResult = VcardDate.TryParse("19000229".AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(VcardValueExamples.Version30DateExtended, VcardVersion.V3_0, 1996, 4, 15)]
        [InlineData("19960415", VcardVersion.V3_0, 1996, 4, 15)]
        [InlineData("1996-0415", VcardVersion.V3_0, 1996, 4, 15)]
        [InlineData("199604-15", VcardVersion.V3_0, 1996, 4, 15)]
        [InlineData(VcardValueExamples.Version21DateBasic, VcardVersion.V2_1, 1995, 4, 15)]
        [InlineData(VcardValueExamples.Version21DateExtended, VcardVersion.V2_1, 1995, 4, 15)]
        public void TryParse_LegacyVersion_AcceptsWhatItsGrammarAllows(string value, VcardVersion version, int year, int month, int day)
        {
            // act
            VcardDate result;
            var testResult = VcardDate.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out result);

            // assert
            testResult.Should().BeTrue();
            result.Year.Should().Be(year);
            result.Month.Should().Be(month);
            result.Day.Should().Be(day);
        }

        [Theory]
        [InlineData(VcardValueExamples.DateYear, VcardVersion.V3_0)]
        [InlineData(VcardValueExamples.DateYearAndMonth, VcardVersion.V3_0)]
        [InlineData(VcardValueExamples.DateMonthAndDay, VcardVersion.V3_0)]
        [InlineData(VcardValueExamples.DateDay, VcardVersion.V3_0)]
        [InlineData("1996-13-15", VcardVersion.V3_0)]
        [InlineData("1996-04-15x", VcardVersion.V3_0)]
        [InlineData(VcardValueExamples.DateYear, VcardVersion.V2_1)]
        [InlineData("1996-0415", VcardVersion.V2_1)]
        [InlineData("199604-15", VcardVersion.V2_1)]
        public void TryParse_LegacyVersion_RejectsWhatItsGrammarDoesNotAllow(string value, VcardVersion version)
        {
            // act
            var testResult = VcardDate.TryParse(value.AsSpan(), VcardValueFormat.Text, version, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(VcardVersion.V3_0, "1985-04-12")]
        [InlineData(VcardVersion.V2_1, "1985-04-12")]
        [InlineData(VcardVersion.V4_0, VcardValueExamples.DateComplete)]
        public void ToString_CompleteDate_WritesTheFormOfTheVersion(VcardVersion version, string expected)
        {
            // arrange
            var date = new VcardDate(1985, 4, 12);

            // act
            var testResult = date.ToString(VcardValueFormat.Text, version);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardVersion.V4_0, true)]
        [InlineData(VcardVersion.V3_0, false)]
        [InlineData(VcardVersion.V2_1, false)]
        public void IsDefinedIn_ReducedDate_IsOnlyInVersion40(VcardVersion version, bool expected)
        {
            // arrange
            var date = VcardDate.FromYear(1985);

            // act
            var testResult = date.IsDefinedIn(version);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void TryFormat_ReducedDateInVersion30_Throws()
        {
            // arrange
            var date = VcardDate.FromYear(1985);
            var destination = new char[16];

            // act
            Action testAction = () => date.TryFormat(destination, out _, VcardValueFormat.Text, VcardVersion.V3_0);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void TryFormat_DestinationTooShort_ReturnsFalseAndWritesNothing()
        {
            // arrange
            var date = new VcardDate(1985, 4, 12);
            var destination = new char[7];

            // act
            int charsWritten;
            var testResult = date.TryFormat(destination, out charsWritten, VcardValueFormat.Text);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        public static TheoryData<Func<VcardDate>, string> Factories()
        {
            return new TheoryData<Func<VcardDate>, string>
            {
                { () => new VcardDate(1985, 4, 12), VcardValueExamples.DateComplete },
                { () => VcardDate.FromYear(1985), VcardValueExamples.DateYear },
                { () => VcardDate.FromYearAndMonth(1985, 4), VcardValueExamples.DateYearAndMonth },
                { () => VcardDate.FromMonth(4), "--04" },
                { () => VcardDate.FromMonthAndDay(4, 12), VcardValueExamples.DateMonthAndDay },
                { () => VcardDate.FromDay(12), VcardValueExamples.DateDay },
            };
        }

        [Theory]
        [MemberData(nameof(Factories), DisableDiscoveryEnumeration = true)]
        public void ToString_DateFromFactory_WritesItsForm(Func<VcardDate> factory, string expected)
        {
            // arrange
            var date = factory();

            // act
            var testResult = date.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        public static TheoryData<Action> OutOfRangeFactories()
        {
            return new TheoryData<Action>
            {
                () => new VcardDate(-1, 1, 1),
                () => new VcardDate(10000, 1, 1),
                () => new VcardDate(1985, 0, 1),
                () => new VcardDate(1985, 13, 1),
                () => new VcardDate(1985, 2, 29),
                () => VcardDate.FromYear(-1),
                () => VcardDate.FromYearAndMonth(1985, 13),
                () => VcardDate.FromMonth(0),
                () => VcardDate.FromMonthAndDay(2, 30),
                () => VcardDate.FromDay(0),
                () => VcardDate.FromDay(32),
            };
        }

        [Theory]
        [MemberData(nameof(OutOfRangeFactories), DisableDiscoveryEnumeration = true)]
        public void Constructor_PartOutOfRange_Throws(Action testAction)
        {
            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Equals_SameForm_IsTrue()
        {
            // arrange
            var date = VcardDate.FromMonthAndDay(4, 12);
            var other = VcardDate.Parse(VcardValueExamples.DateMonthAndDay.AsSpan(), VcardValueFormat.Text);

            // act
            var testResult = date == other;

            // assert
            testResult.Should().BeTrue();
            date.GetHashCode().Should().Be(other.GetHashCode());
        }

        [Fact]
        public void Equals_ADateWithAndWithoutItsYear_IsFalse()
        {
            // arrange
            var date = new VcardDate(1985, 4, 12);
            var other = VcardDate.FromMonthAndDay(4, 12);

            // act
            var testResult = date != other;

            // assert
            testResult.Should().BeTrue();
        }

        public static TheoryData<Action> UninitializedMembers()
        {
            return new TheoryData<Action>
            {
                () => _ = default(VcardDate).Year,
                () => _ = default(VcardDate).Month,
                () => _ = default(VcardDate).Day,
                () => _ = default(VcardDate).IsComplete,
                () => default(VcardDate).IsDefinedIn(VcardVersion.V4_0),
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
        public void TryFormat_UninitializedInstance_WritesNothing()
        {
            // arrange
            var destination = new char[16];

            // act
            int charsWritten;
            var testResult = default(VcardDate).TryFormat(destination, out charsWritten, VcardValueFormat.Text);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void ToString_UninitializedInstance_GivesTheTypeName()
        {
            // act
            var testResult = default(VcardDate).ToString();

            // assert
            testResult.Should().Be(typeof(VcardDate).FullName);
        }
    }
}
