using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests;

public class IcalRecurrenceRuleTests
{
    /// <summary>
    /// The rules of RFC 5545 §3.3.10 and the examples of §3.8.5.3, and the VTIMEZONE examples of §3.6.5.
    /// </summary>
    public static TheoryData<string> RfcRules => new()
    {
        "FREQ=YEARLY;INTERVAL=2;BYMONTH=1;BYDAY=SU;BYHOUR=8,9;BYMINUTE=30",
        "FREQ=DAILY;COUNT=10;INTERVAL=2",
        "FREQ=DAILY;COUNT=10",
        "FREQ=DAILY;UNTIL=19971224T000000Z",
        "FREQ=DAILY;INTERVAL=2",
        "FREQ=DAILY;INTERVAL=10;COUNT=5",
        "FREQ=YEARLY;UNTIL=20000131T140000Z;BYMONTH=1;BYDAY=SU,MO,TU,WE,TH,FR,SA",
        "FREQ=DAILY;UNTIL=20000131T140000Z;BYMONTH=1",
        "FREQ=WEEKLY;COUNT=10",
        "FREQ=WEEKLY;UNTIL=19971224T000000Z",
        "FREQ=WEEKLY;INTERVAL=2;WKST=SU",
        "FREQ=WEEKLY;UNTIL=19971007T000000Z;WKST=SU;BYDAY=TU,TH",
        "FREQ=WEEKLY;COUNT=10;WKST=SU;BYDAY=TU,TH",
        "FREQ=WEEKLY;INTERVAL=2;UNTIL=19971224T000000Z;WKST=SU;BYDAY=MO,WE,FR",
        "FREQ=WEEKLY;INTERVAL=2;COUNT=8;WKST=SU;BYDAY=TU,TH",
        "FREQ=MONTHLY;COUNT=10;BYDAY=1FR",
        "FREQ=MONTHLY;UNTIL=19971224T000000Z;BYDAY=1FR",
        "FREQ=MONTHLY;INTERVAL=2;COUNT=10;BYDAY=1SU,-1SU",
        "FREQ=MONTHLY;COUNT=6;BYDAY=-2MO",
        "FREQ=MONTHLY;BYMONTHDAY=-3",
        "FREQ=MONTHLY;COUNT=10;BYMONTHDAY=2,15",
        "FREQ=MONTHLY;COUNT=10;BYMONTHDAY=1,-1",
        "FREQ=MONTHLY;INTERVAL=18;COUNT=10;BYMONTHDAY=10,11,12,13,14,15",
        "FREQ=MONTHLY;INTERVAL=2;BYDAY=TU",
        "FREQ=YEARLY;COUNT=10;BYMONTH=6,7",
        "FREQ=YEARLY;INTERVAL=2;COUNT=10;BYMONTH=1,2,3",
        "FREQ=YEARLY;INTERVAL=3;COUNT=10;BYYEARDAY=1,100,200",
        "FREQ=YEARLY;BYDAY=20MO",
        "FREQ=YEARLY;BYWEEKNO=20;BYDAY=MO",
        "FREQ=YEARLY;BYMONTH=3;BYDAY=TH",
        "FREQ=YEARLY;BYDAY=TH;BYMONTH=6,7,8",
        "FREQ=MONTHLY;BYDAY=FR;BYMONTHDAY=13",
        "FREQ=MONTHLY;BYDAY=SA;BYMONTHDAY=7,8,9,10,11,12,13",
        "FREQ=YEARLY;INTERVAL=4;BYMONTH=11;BYDAY=TU;BYMONTHDAY=2,3,4,5,6,7,8",
        "FREQ=MONTHLY;COUNT=3;BYDAY=TU,WE,TH;BYSETPOS=3",
        "FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-2",
        "FREQ=HOURLY;INTERVAL=3;UNTIL=19970902T170000Z",
        "FREQ=MINUTELY;INTERVAL=15;COUNT=6",
        "FREQ=MINUTELY;INTERVAL=90;COUNT=4",
        "FREQ=DAILY;BYHOUR=9,10,11,12,13,14,15,16;BYMINUTE=0,20,40",
        "FREQ=MINUTELY;INTERVAL=20;BYHOUR=9,10,11,12,13,14,15,16",
        "FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=MO",
        "FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=SU",
        "FREQ=MONTHLY;BYMONTHDAY=15,30;COUNT=5",
        "FREQ=YEARLY;BYDAY=-1SU;BYMONTH=10",
        "FREQ=YEARLY;BYDAY=1SU;BYMONTH=4;UNTIL=19980404T070000Z",
        "FREQ=YEARLY;BYDAY=-1SU;BYMONTH=4;UNTIL=19980404T070000Z",
        "FREQ=YEARLY;BYMONTH=4;BYDAY=1SU;UNTIL=19730429T070000Z",
        "FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU;UNTIL=20061029T060000Z"
    };

    #region Parsing and formatting

    [Theory]
    [MemberData(nameof(RfcRules))]
    public void TryParse_RfcExample_ParsesAndRoundTrips(string value)
    {
        // act
        var testResult = IcalRecurrenceRule.TryParse(value, IcalValueFormat.Text, out var rule);

        // assert
        testResult.Should().BeTrue();
        rule.IsValid.Should().BeTrue();
        var formatted = rule.ToString(IcalValueFormat.Text);
        formatted.Should().StartWith("FREQ=");
        IcalRecurrenceRule.Parse(formatted, IcalValueFormat.Text).Should().Be(rule);
        SortParts(formatted).Should().Be(SortParts(value));
    }

    [Theory]
    [InlineData("FREQ=DAILY;COUNT=10")]
    [InlineData("FREQ=MONTHLY;COUNT=10;INTERVAL=2;BYDAY=1SU,-1SU")]
    [InlineData("FREQ=MONTHLY;COUNT=10;BYMONTHDAY=1,-1")]
    [InlineData("FREQ=YEARLY;UNTIL=20000131T140000Z;BYDAY=SU,MO,TU,WE,TH,FR,SA;BYMONTH=1")]
    [InlineData("FREQ=WEEKLY;UNTIL=19971007;BYDAY=TU,TH;WKST=SU")]
    [InlineData("FREQ=YEARLY;BYSECOND=0,60;BYMINUTE=0,59;BYHOUR=0,23;BYDAY=MO,1TU,53TU,-1TU,-53TU;BYMONTHDAY=1,31,-1,-31;BYYEARDAY=1,366,-1,-366;BYMONTH=1,12;BYSETPOS=1,366,-1,-366;WKST=MO")]
    [InlineData("FREQ=YEARLY;BYWEEKNO=1,53,-1,-53")]
    public void ToString_CanonicalRule_IsWrittenAsItIs(string value)
    {
        // act
        var testResult = IcalRecurrenceRule.Parse(value, IcalValueFormat.Text).ToString(IcalValueFormat.Text);

        // assert
        testResult.Should().Be(value);
    }

    [Fact]
    public void ToString_PartsInAnyOrder_WritesFrequencyFirst()
    {
        // act
        var testResult = IcalRecurrenceRule.Parse("BYMONTH=10;BYDAY=-1SU;FREQ=YEARLY", IcalValueFormat.Text).ToString();

        // assert
        testResult.Should().Be("FREQ=YEARLY;BYDAY=-1SU;BYMONTH=10");
    }

    [Fact]
    public void ToString_ListValues_AreSortedAndDeduplicated()
    {
        // act
        var testResult = IcalRecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=-2,15,-1,2,15;BYDAY=FR,MO,-1MO,1MO", IcalValueFormat.Text).ToString();

        // assert
        testResult.Should().Be("FREQ=MONTHLY;BYDAY=MO,1MO,-1MO,FR;BYMONTHDAY=2,15,-1,-2");
    }

    [Fact]
    public void TryParse_LowerCaseAndPlusSigns_AreAccepted()
    {
        // act
        var testResult = IcalRecurrenceRule.Parse("freq=monthly;byday=+1mo;bymonthday=+5;until=19971224t000000z;wkst=su", IcalValueFormat.Text);

        // assert
        testResult.ToString().Should().Be("FREQ=MONTHLY;UNTIL=19971224T000000Z;BYDAY=1MO;BYMONTHDAY=5;WKST=SU");
    }

    [Fact]
    public void Parse_RfcExample_ExposesItsParts()
    {
        // act
        var testResult = IcalRecurrenceRule.Parse("FREQ=YEARLY;INTERVAL=2;BYMONTH=1;BYDAY=SU;BYHOUR=8,9;BYMINUTE=30", IcalValueFormat.Text);

        // assert
        testResult.Frequency.Should().Be(IcalRecurrenceFrequency.Yearly);
        testResult.Interval.Should().Be(2);
        testResult.Count.Should().BeNull();
        testResult.UntilDate.Should().BeNull();
        testResult.UntilDateTime.Should().BeNull();
        testResult.WeekStart.Should().BeNull();
        testResult.GetByMonth().Should().Equal(1);
        testResult.GetByDay().Should().Equal(new IcalRecurrenceDay(DayOfWeek.Sunday));
        testResult.GetByHour().Should().Equal(8, 9);
        testResult.GetByMinute().Should().Equal(30);
        testResult.GetBySecond().Should().BeEmpty();
        testResult.GetByMonthDay().Should().BeEmpty();
        testResult.GetByYearDay().Should().BeEmpty();
        testResult.GetByWeekNo().Should().BeEmpty();
        testResult.GetBySetPos().Should().BeEmpty();
    }

    [Fact]
    public void Parse_SignedValues_AreReturnedPositiveThenNegative()
    {
        // act
        var testResult = IcalRecurrenceRule.Parse("FREQ=YEARLY;BYDAY=-1SU,2MO;BYYEARDAY=-1,100,1;BYSETPOS=-366,366", IcalValueFormat.Text);

        // assert
        testResult.GetByDay().Should().Equal(new IcalRecurrenceDay(DayOfWeek.Sunday, -1), new IcalRecurrenceDay(DayOfWeek.Monday, 2));
        testResult.GetByYearDay().Should().Equal(1, 100, -1);
        testResult.GetBySetPos().Should().Equal(366, -366);
    }

    [Fact]
    public void Parse_UntilDate_IsADate()
    {
        // act
        var testResult = IcalRecurrenceRule.Parse("FREQ=DAILY;UNTIL=19971224", IcalValueFormat.Text);

        // assert
        testResult.UntilDate.Should().Be(new IcalDate(1997, 12, 24));
        testResult.UntilDateTime.Should().BeNull();
        testResult.ToString().Should().Be("FREQ=DAILY;UNTIL=19971224");
    }

    [Fact]
    public void Parse_UntilFloatingDateTime_IsFloating()
    {
        // act
        var testResult = IcalRecurrenceRule.Parse("FREQ=DAILY;UNTIL=19971224T000000", IcalValueFormat.Text);

        // assert
        testResult.UntilDateTime!.Value.Kind.Should().Be(IcalDateTimeKind.Floating);
    }

    [Fact]
    public void Parse_IntervalOfOne_IsKept()
    {
        // act
        var testResult = IcalRecurrenceRule.Parse("FREQ=DAILY;INTERVAL=1", IcalValueFormat.Text);

        // assert
        testResult.Interval.Should().Be(1);
        testResult.ToString().Should().Be("FREQ=DAILY;INTERVAL=1");
        testResult.Should().NotBe(new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily));
    }

    [Fact]
    public void TryFormat_DestinationTooShort_ReturnsFalse()
    {
        // arrange
        var rule = IcalRecurrenceRule.Parse("FREQ=DAILY;COUNT=10", IcalValueFormat.Text);
        var destination = new char[18];

        // act
        var testResult = rule.TryFormat(destination, out var charsWritten, IcalValueFormat.Text);

        // assert
        testResult.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Fact]
    public void ToString_LongRule_GrowsItsBuffer()
    {
        // arrange
        var yearDays = Enumerable.Range(1, 366).Concat(Enumerable.Range(1, 366).Select(day => -day)).ToArray();
        var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Yearly).WithByYearDay(yearDays);

        // act
        var testResult = rule.ToString(IcalValueFormat.Text);

        // assert
        IcalRecurrenceRule.Parse(testResult, IcalValueFormat.Text).GetByYearDay().Should().Equal(yearDays);
    }

    #endregion

    #region Invalid values

    [Theory]
    [InlineData("")]
    [InlineData("FREQ=")]
    [InlineData("FREQ=FORTNIGHTLY")]
    [InlineData("FREQ=DAILY;")]
    [InlineData(";FREQ=DAILY")]
    [InlineData("FREQ=DAILY;;COUNT=1")]
    [InlineData("FREQ=DAILY;COUNT")]
    [InlineData("FREQ=DAILY;=1")]
    [InlineData("FREQ=DAILY;FOO=1")]
    [InlineData("FREQ=DAILY;RSCALE=GREGORIAN")]
    [InlineData("FREQ = DAILY")]
    [InlineData("FREQ=DAILY;COUNT=0")]
    [InlineData("FREQ=DAILY;COUNT=-1")]
    [InlineData("FREQ=DAILY;COUNT=1.5")]
    [InlineData("FREQ=DAILY;COUNT=99999999999")]
    [InlineData("FREQ=DAILY;INTERVAL=0")]
    [InlineData("FREQ=DAILY;UNTIL=2026023")]
    [InlineData("FREQ=DAILY;UNTIL=20260230")]
    [InlineData("FREQ=DAILY;UNTIL=20260101T250000Z")]
    [InlineData("FREQ=DAILY;WKST=XX")]
    [InlineData("FREQ=DAILY;WKST=MO,TU")]
    [InlineData("FREQ=DAILY;BYSECOND=61")]
    [InlineData("FREQ=DAILY;BYSECOND=-1")]
    [InlineData("FREQ=DAILY;BYSECOND=005")]
    [InlineData("FREQ=DAILY;BYMINUTE=60")]
    [InlineData("FREQ=DAILY;BYHOUR=24")]
    [InlineData("FREQ=DAILY;BYHOUR=1,,2")]
    [InlineData("FREQ=DAILY;BYHOUR=1,")]
    [InlineData("FREQ=DAILY;BYMONTH=0")]
    [InlineData("FREQ=DAILY;BYMONTH=13")]
    [InlineData("FREQ=DAILY;BYMONTH=+1")]
    [InlineData("FREQ=MONTHLY;BYMONTHDAY=0")]
    [InlineData("FREQ=MONTHLY;BYMONTHDAY=32")]
    [InlineData("FREQ=MONTHLY;BYMONTHDAY=-32")]
    [InlineData("FREQ=MONTHLY;BYMONTHDAY=-")]
    [InlineData("FREQ=YEARLY;BYYEARDAY=0")]
    [InlineData("FREQ=YEARLY;BYYEARDAY=367")]
    [InlineData("FREQ=YEARLY;BYYEARDAY=-367")]
    [InlineData("FREQ=YEARLY;BYYEARDAY=0001")]
    [InlineData("FREQ=YEARLY;BYWEEKNO=0")]
    [InlineData("FREQ=YEARLY;BYWEEKNO=54")]
    [InlineData("FREQ=YEARLY;BYSETPOS=0;BYMONTH=1")]
    [InlineData("FREQ=YEARLY;BYSETPOS=367;BYMONTH=1")]
    [InlineData("FREQ=MONTHLY;BYDAY=0MO")]
    [InlineData("FREQ=MONTHLY;BYDAY=54MO")]
    [InlineData("FREQ=MONTHLY;BYDAY=-54MO")]
    [InlineData("FREQ=MONTHLY;BYDAY=001MO")]
    [InlineData("FREQ=MONTHLY;BYDAY=+MO")]
    [InlineData("FREQ=MONTHLY;BYDAY=M")]
    [InlineData("FREQ=MONTHLY;BYDAY=MONDAY")]
    [InlineData("FREQ=MONTHLY;BYDAY=1 MO")]
    public void TryParse_MalformedValue_Fails(string value)
    {
        // act
        var testResult = IcalRecurrenceRule.TryParse(value, IcalValueFormat.Text, out _);

        // assert
        testResult.Should().BeFalse();
    }

    [Theory]
    [InlineData("FREQ=DAILY;FREQ=WEEKLY")]
    [InlineData("FREQ=DAILY;COUNT=1;COUNT=2")]
    [InlineData("FREQ=DAILY;INTERVAL=1;INTERVAL=2")]
    [InlineData("FREQ=DAILY;BYHOUR=1;BYHOUR=2")]
    [InlineData("FREQ=DAILY;BYDAY=MO;BYDAY=TU")]
    [InlineData("FREQ=DAILY;WKST=MO;WKST=SU")]
    [InlineData("FREQ=DAILY;UNTIL=19971224;until=19971225")]
    public void TryParse_RulePartTwice_Fails(string value)
    {
        // act
        var testResult = IcalRecurrenceRule.TryParse(value, IcalValueFormat.Text, out _);

        // assert
        testResult.Should().BeFalse();
    }

    #endregion

    #region §3.3.10 constraints

    [Theory]
    [InlineData("COUNT=10", false)]
    [InlineData("FREQ=DAILY;UNTIL=19971224T000000Z;COUNT=10", false)]
    [InlineData("FREQ=SECONDLY;BYDAY=1MO", false)]
    [InlineData("FREQ=MINUTELY;BYDAY=1MO", false)]
    [InlineData("FREQ=HOURLY;BYDAY=1MO", false)]
    [InlineData("FREQ=DAILY;BYDAY=1MO", false)]
    [InlineData("FREQ=WEEKLY;BYDAY=-1MO", false)]
    [InlineData("FREQ=WEEKLY;BYDAY=MO", true)]
    [InlineData("FREQ=MONTHLY;BYDAY=1MO", true)]
    [InlineData("FREQ=YEARLY;BYDAY=1MO", true)]
    [InlineData("FREQ=YEARLY;BYWEEKNO=20;BYDAY=1MO", false)]
    [InlineData("FREQ=YEARLY;BYWEEKNO=20;BYDAY=MO", true)]
    [InlineData("FREQ=WEEKLY;BYMONTHDAY=1", false)]
    [InlineData("FREQ=DAILY;BYMONTHDAY=1", true)]
    [InlineData("FREQ=DAILY;BYYEARDAY=1", false)]
    [InlineData("FREQ=WEEKLY;BYYEARDAY=1", false)]
    [InlineData("FREQ=MONTHLY;BYYEARDAY=1", false)]
    [InlineData("FREQ=HOURLY;BYYEARDAY=1", true)]
    [InlineData("FREQ=YEARLY;BYYEARDAY=1", true)]
    [InlineData("FREQ=MONTHLY;BYWEEKNO=1", false)]
    [InlineData("FREQ=DAILY;BYWEEKNO=1", false)]
    [InlineData("FREQ=YEARLY;BYWEEKNO=1", true)]
    [InlineData("FREQ=MONTHLY;BYSETPOS=1", false)]
    [InlineData("FREQ=MONTHLY;COUNT=3;BYSETPOS=1", false)]
    [InlineData("FREQ=MONTHLY;BYSETPOS=1;BYDAY=MO", true)]
    [InlineData("FREQ=DAILY;BYSETPOS=1;BYSECOND=0", true)]
    public void TryParse_Constraint_IsEnforced(string value, bool isValid)
    {
        // act
        var testResult = IcalRecurrenceRule.TryParse(value, IcalValueFormat.Text, out _);

        // assert
        testResult.Should().Be(isValid);
    }

    [Fact]
    public void IsValid_BuiltRuleBreakingConstraint_IsFalseAndFormatThrows()
    {
        // arrange
        var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Weekly).WithByDay(new IcalRecurrenceDay(DayOfWeek.Monday, 1));

        // act
        var act = () => rule.ToString(IcalValueFormat.Text);

        // assert
        rule.IsValid.Should().BeFalse();
        act.Should().Throw<InvalidOperationException>().WithMessage("*BYDAY*MONTHLY or YEARLY*");
        rule.ToString().Should().Be("FREQ=WEEKLY;BYDAY=1MO");
    }

    [Fact]
    public void IsValid_Default_IsFalse()
    {
        // act
        var rule = default(IcalRecurrenceRule);

        // assert
        rule.IsValid.Should().BeFalse();
        rule.Invoking(value => value.TryFormat(new char[64], out _, IcalValueFormat.Text)).Should().Throw<InvalidOperationException>();
        rule.ToString().Should().BeEmpty();
    }

    #endregion

    #region Building

    [Fact]
    public void With_BuildsTheRfcExample()
    {
        // act
        var testResult = new IcalRecurrenceRule(IcalRecurrenceFrequency.Yearly)
            .WithInterval(2)
            .WithByMonth(1)
            .WithByDay(new IcalRecurrenceDay(DayOfWeek.Sunday))
            .WithByHour(8, 9)
            .WithByMinute(30);

        // assert
        testResult.Should().Be(IcalRecurrenceRule.Parse("FREQ=YEARLY;INTERVAL=2;BYMONTH=1;BYDAY=SU;BYHOUR=8,9;BYMINUTE=30", IcalValueFormat.Text));
    }

    [Fact]
    public void WithCount_ReplacesUntil_AndWithUntilReplacesCount()
    {
        // arrange
        var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily);
        var until = IcalDateTime.Parse("19971224T000000Z", IcalValueFormat.Text);

        // act
        var withCount = rule.WithUntil(until).WithCount(10);
        var withUntil = withCount.WithUntil(until);
        var withoutEnd = withUntil.WithoutEnd();

        // assert
        withCount.ToString(IcalValueFormat.Text).Should().Be("FREQ=DAILY;COUNT=10");
        withUntil.ToString(IcalValueFormat.Text).Should().Be("FREQ=DAILY;UNTIL=19971224T000000Z");
        withoutEnd.ToString(IcalValueFormat.Text).Should().Be("FREQ=DAILY");
    }

    [Fact]
    public void WithByList_NullOrEmpty_RemovesThePart()
    {
        // arrange
        var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).WithByHour(9).WithByDay(new IcalRecurrenceDay(DayOfWeek.Monday));

        // act
        var testResult = rule.WithByHour().WithByDay(null!);

        // assert
        testResult.ToString(IcalValueFormat.Text).Should().Be("FREQ=DAILY");
    }

    [Fact]
    public void WithWeekStart_Null_RemovesWkst()
    {
        // act
        var testResult = new IcalRecurrenceRule(IcalRecurrenceFrequency.Weekly).WithWeekStart(DayOfWeek.Sunday).WithWeekStart(null);

        // assert
        testResult.WeekStart.Should().BeNull();
    }

    [Fact]
    public void With_OutOfRangeValue_Throws()
    {
        // arrange
        var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Yearly);

        // assert
        rule.Invoking(value => value.WithBySecond(61)).Should().Throw<ArgumentOutOfRangeException>();
        rule.Invoking(value => value.WithByMonthDay(0)).Should().Throw<ArgumentOutOfRangeException>();
        rule.Invoking(value => value.WithByYearDay(-367)).Should().Throw<ArgumentOutOfRangeException>();
        rule.Invoking(value => value.WithByWeekNo(54)).Should().Throw<ArgumentOutOfRangeException>();
        rule.Invoking(value => value.WithByMonth(13)).Should().Throw<ArgumentOutOfRangeException>();
        rule.Invoking(value => value.WithBySetPos(0)).Should().Throw<ArgumentOutOfRangeException>();
        rule.Invoking(value => value.WithCount(0)).Should().Throw<ArgumentOutOfRangeException>();
        rule.Invoking(value => value.WithInterval(0)).Should().Throw<ArgumentOutOfRangeException>();
        rule.Invoking(value => value.WithFrequency(0)).Should().Throw<ArgumentOutOfRangeException>();
        rule.Invoking(value => value.WithUntil(IcalDateTime.FromDateTime(DateTime.Now, "Europe/London"))).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new IcalRecurrenceDay(DayOfWeek.Monday, 54)).Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    /// <summary>
    /// Sorts the rule parts, and the values of each, so rules can be compared whatever order they were written in.
    /// </summary>
    private static string SortParts(string rule)
    {
        var parts = rule.ToUpperInvariant().Split(';').Select(part =>
        {
            var nameAndValue = part.Split('=');
            var value = nameAndValue[1];
            if (nameAndValue[0] == "INTERVAL" && value == "1") return string.Empty;

            return nameAndValue[0] + "=" + string.Join(",", value.Split(',').OrderBy(item => item, StringComparer.Ordinal));
        });
        return string.Join(";", parts.Where(part => part.Length > 0).OrderBy(part => part, StringComparer.Ordinal));
    }
}
