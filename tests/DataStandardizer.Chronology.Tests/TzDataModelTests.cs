using System.Reflection;
using FluentAssertions;
using static DataStandardizer.Chronology.Tests.TzAbbreviations;
using static DataStandardizer.Chronology.Tests.TzOffsets;

namespace DataStandardizer.Chronology.Tests;

public class TzDataModelTests
{
    private const string FixedSaveParameterName = "fixedSave";
    private const string RulesParameterName = "rules";
    private const string ValidFromUtcParameterName = "validFromUtc";
    private const string ValidUntilUtcParameterName = "validUntilUtc";

    // A value that is not a member of TzDataZoneRuleKind.
    private const TzDataZoneRuleKind UndefinedRuleKind = (TzDataZoneRuleKind)3;

    // The start of the name of every System.ValueTuple type, generic or not.
    private const string ValueTupleTypeNamePrefix = nameof(ValueTuple);

    // The FORMAT column of a Zone line for the US Eastern zone (zic(8)).
    private const string EasternFormat = "E%sT";

    // An UNTIL column of the US Eastern zone in TZ Database 2026e: 1918 Mar lastSun 2:00s.
    private const int UntilYear = 1918;
    private const int UntilMonth = 3;

    // The start of British Summer Time in 2024, and its end.
    private static readonly DateTime BritishSummerTimeStart2024 = new(2024, 3, 31, 1, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime BritishSummerTimeEnd2024 = new(2024, 10, 27, 1, 0, 0, DateTimeKind.Utc);

    private static TzDataRule CreateRule()
    {
        return new TzDataRule("Test", 2007, int.MaxValue, 3, TzDataDayKind.WeekdayOnOrAfter, 8, DayOfWeek.Sunday, TimeSpan.FromHours(2), TzDataTimeReference.Wall, UtcPlus0100, true, "D");
    }

    private static TzDataUntil CreateUntil()
    {
        return new TzDataUntil(UntilYear, UntilMonth, TzDataDayKind.LastWeekday, null, DayOfWeek.Sunday, UtcPlus0200, TzDataTimeReference.Standard);
    }

    private static TzDataOffsetInfo CreateGreenwichMeanTimeUntil2024SummerTime()
    {
        return new TzDataOffsetInfo(UtcZero, UtcZero, false, Gmt, null, BritishSummerTimeStart2024);
    }

    private static TzDataOffsetInfo CreateBritishSummerTimeFrom2024()
    {
        return new TzDataOffsetInfo(UtcZero, UtcPlus0100, true, Bst, BritishSummerTimeStart2024, null);
    }

    public static TheoryData<Type> ModelType_TestCases => new()
    {
        typeof(TzDataZoneLine),
        typeof(TzDataRule),
        typeof(TzDataUntil),
        typeof(TzDataOffsetInfo),
        typeof(TzDataTransition),
    };

    [Theory]
    [MemberData(nameof(ModelType_TestCases))]
    public void GetConstructors_ModelType_ReturnsNoPublicConstructors(Type testType)
    {
        // act
        var testResult = testType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // assert
        testResult.Should().BeEmpty("only generated data may create instances of {0}", testType);
    }

    /// <summary>
    /// Gets a test case for every type in the signature of every public member of the assembly, and every type that such a type is built from.
    /// </summary>
    public static TheoryData<string, Type> PublicSignatureType_TestCases
    {
        get
        {
            const BindingFlags publicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            var testCases = new TheoryData<string, Type>();
            foreach (var type in typeof(TzDataTimezone).Assembly.GetExportedTypes())
            {
                foreach (var member in type.GetMembers(publicMembers))
                {
                    foreach (var signatureType in GetSignatureTypes(member))
                    {
                        foreach (var constituentType in GetConstituentTypes(signatureType))
                            testCases.Add($"{type.FullName}.{member.Name}", constituentType);
                    }
                }
            }

            return testCases;
        }
    }

    private static IEnumerable<Type> GetSignatureTypes(MemberInfo member)
    {
        switch (member)
        {
            case FieldInfo field:
                yield return field.FieldType;
                break;
            case PropertyInfo property:
                yield return property.PropertyType;
                break;
            case MethodBase method:
                if (method is MethodInfo methodInfo)
                    yield return methodInfo.ReturnType;
                foreach (var parameter in method.GetParameters())
                    yield return parameter.ParameterType;
                break;
        }
    }

    private static IEnumerable<Type> GetConstituentTypes(Type type)
    {
        yield return type;

        if (type.HasElementType)
        {
            foreach (var elementType in GetConstituentTypes(type.GetElementType()!))
                yield return elementType;
        }

        if (type.IsGenericType)
        {
            foreach (var argumentType in type.GetGenericArguments().SelectMany(GetConstituentTypes))
                yield return argumentType;
        }
    }

    [Theory]
    [MemberData(nameof(PublicSignatureType_TestCases), DisableDiscoveryEnumeration = true)]
    public void FullName_PublicSignatureType_IsNotValueTuple(string testMember, Type testType)
    {
        // act
        var testResult = testType.Name;

        // assert
        testResult.Should().NotStartWith(ValueTupleTypeNamePrefix, "System.ValueTuple is a dependency only on netstandard1.0, and its version is pinned, so {0} must not use it", testMember);
    }

    [Fact]
    public void ZoneLine_NoRules_HasEmptyRulesAndNoFixedSave()
    {
        // act
        var testResult = new TzDataZoneLine(UtcMinus0500, TzDataZoneRuleKind.None, null, null, Est, null);

        // assert
        testResult.StandardOffset.Should().Be(UtcMinus0500);
        testResult.RuleKind.Should().Be(TzDataZoneRuleKind.None);
        testResult.FixedSave.Should().BeNull();
        testResult.Rules.Should().BeEmpty();
        testResult.Format.Should().Be(Est);
        testResult.Until.Should().BeNull();
    }

    [Fact]
    public void ZoneLine_FixedSave_HasFixedSaveAndEmptyRules()
    {
        // arrange
        var testUntil = new TzDataUntil(1945, 9, TzDataDayKind.DayOfMonth, 30, null, UtcPlus0200, TzDataTimeReference.Wall);

        // act
        var testResult = new TzDataZoneLine(UtcMinus0500, TzDataZoneRuleKind.FixedSave, UtcPlus0100, null, Ewt, testUntil);

        // assert
        testResult.FixedSave.Should().Be(UtcPlus0100);
        testResult.Rules.Should().BeEmpty();
        testResult.Until.Should().Be(testUntil);
    }

    [Fact]
    public void ZoneLine_RuleSet_ExposesRulesReadOnly()
    {
        // arrange
        var testRules = new[] { CreateRule() };

        // act
        var testResult = new TzDataZoneLine(UtcMinus0500, TzDataZoneRuleKind.RuleSet, null, testRules, EasternFormat, null);

        // assert
        testResult.Rules.Should().Equal(testRules);
        testResult.Rules.Should().NotBeAssignableTo<TzDataRule[]>();
        testResult.Rules.Should().BeAssignableTo<ICollection<TzDataRule>>().Which.IsReadOnly.Should().BeTrue();
    }

    public static TheoryData<TzDataZoneRuleKind, TimeSpan?, TzDataRule[]?, string> ZoneLine_InconsistentRules_TestCases => new()
    {
        { TzDataZoneRuleKind.None, UtcPlus0100, null, FixedSaveParameterName },
        { TzDataZoneRuleKind.None, null, [CreateRule()], RulesParameterName },
        { TzDataZoneRuleKind.FixedSave, null, null, FixedSaveParameterName },
        { TzDataZoneRuleKind.FixedSave, UtcPlus0100, [CreateRule()], RulesParameterName },
        { TzDataZoneRuleKind.RuleSet, UtcPlus0100, [CreateRule()], FixedSaveParameterName },
        { TzDataZoneRuleKind.RuleSet, null, null, RulesParameterName },
        { UndefinedRuleKind, null, null, "ruleKind" },
    };

    [Theory]
    [MemberData(nameof(ZoneLine_InconsistentRules_TestCases))]
    public void ZoneLine_InconsistentRules_ThrowsArgumentException(TzDataZoneRuleKind testRuleKind, TimeSpan? testFixedSave, TzDataRule[]? testRules, string expectedParamName)
    {
        // act
        Action testAction = () => new TzDataZoneLine(UtcZero, testRuleKind, testFixedSave, testRules, Gmt, null);

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be(expectedParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ZoneLine_MissingFormat_ThrowsArgumentException(string? testFormat)
    {
        // act
        Action testAction = () => new TzDataZoneLine(UtcZero, TzDataZoneRuleKind.None, null, null, testFormat!, null);

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("format");
    }

    [Fact]
    public void Until_ValidArguments_SetsProperties()
    {
        // act
        var testResult = new TzDataUntil(UntilYear, UntilMonth, TzDataDayKind.LastWeekday, null, DayOfWeek.Sunday, UtcPlus0200, TzDataTimeReference.Standard);

        // assert
        testResult.Year.Should().Be(UntilYear);
        testResult.Month.Should().Be(UntilMonth);
        testResult.DayKind.Should().Be(TzDataDayKind.LastWeekday);
        testResult.Day.Should().BeNull();
        testResult.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        testResult.Time.Should().Be(UtcPlus0200);
        testResult.TimeReference.Should().Be(TzDataTimeReference.Standard);
    }

    [Fact]
    public void Equality_UntilsWithSameValues_AreEqual()
    {
        // arrange
        var testLeft = CreateUntil();
        var testRight = CreateUntil();

        // act
        var testResult = testLeft == testRight;

        // assert
        testResult.Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_UntilsWithSameValues_ReturnsSameHashCode()
    {
        // arrange
        var testLeft = CreateUntil();
        var testRight = CreateUntil();

        // act
        var testResult = (Left: testLeft.GetHashCode(), Right: testRight.GetHashCode());

        // assert
        testResult.Left.Should().Be(testResult.Right);
    }

    [Fact]
    public void Until_InvalidMonth_ThrowsArgumentOutOfRangeException()
    {
        // act
        Action testAction = () => new TzDataUntil(UntilYear, 13, TzDataDayKind.DayOfMonth, 1, null, TimeSpan.Zero, TzDataTimeReference.Wall);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("month");
    }

    [Fact]
    public void OffsetInfo_ValidArguments_SetsPropertiesAndSumsUtcOffset()
    {
        // act
        var testResult = new TzDataOffsetInfo(UtcZero, UtcPlus0100, true, Bst, BritishSummerTimeStart2024, BritishSummerTimeEnd2024);

        // assert
        testResult.UtcOffset.Should().Be(UtcPlus0100);
        testResult.StandardOffset.Should().Be(UtcZero);
        testResult.DaylightSavings.Should().Be(UtcPlus0100);
        testResult.IsDaylightSavingTime.Should().BeTrue();
        testResult.Abbreviation.Should().Be(Bst);
        testResult.ValidFromUtc.Should().Be(BritishSummerTimeStart2024);
        testResult.ValidUntilUtc.Should().Be(BritishSummerTimeEnd2024);
    }

    [Fact]
    public void OffsetInfo_NegativeDaylightSavings_SubtractsFromStandardOffset()
    {
        // act
        var testResult = new TzDataOffsetInfo(UtcPlus0100, UtcMinus0100, true, Gmt, null, null);

        // assert
        testResult.UtcOffset.Should().Be(UtcZero);
    }

    [Fact]
    public void OffsetInfo_Default_HasEmptyAbbreviation()
    {
        // act
        var testResult = default(TzDataOffsetInfo);

        // assert
        testResult.Abbreviation.Should().BeEmpty();
    }

    [Theory]
    [InlineData(DateTimeKind.Local, DateTimeKind.Utc, ValidFromUtcParameterName)]
    [InlineData(DateTimeKind.Unspecified, DateTimeKind.Utc, ValidFromUtcParameterName)]
    [InlineData(DateTimeKind.Utc, DateTimeKind.Local, ValidUntilUtcParameterName)]
    [InlineData(DateTimeKind.Utc, DateTimeKind.Unspecified, ValidUntilUtcParameterName)]
    public void OffsetInfo_NonUniversalPeriod_ThrowsArgumentException(DateTimeKind testFromKind, DateTimeKind testUntilKind, string expectedParamName)
    {
        // arrange
        var testFrom = new DateTime(2024, 1, 1, 0, 0, 0, testFromKind);
        var testUntil = new DateTime(2025, 1, 1, 0, 0, 0, testUntilKind);

        // act
        Action testAction = () => new TzDataOffsetInfo(UtcZero, UtcZero, false, Gmt, testFrom, testUntil);

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be(expectedParamName);
    }

    [Fact]
    public void OffsetInfo_PeriodEndNotAfterStart_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var testInstant = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // act
        Action testAction = () => new TzDataOffsetInfo(UtcZero, UtcZero, false, Gmt, testInstant, testInstant);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be(ValidUntilUtcParameterName);
    }

    [Fact]
    public void Transition_ValidArguments_SetsProperties()
    {
        // arrange
        var testBefore = CreateGreenwichMeanTimeUntil2024SummerTime();
        var testAfter = CreateBritishSummerTimeFrom2024();

        // act
        var testResult = new TzDataTransition(BritishSummerTimeStart2024, testBefore, testAfter);

        // assert
        testResult.InstantUtc.Should().Be(BritishSummerTimeStart2024);
        testResult.Before.Should().Be(testBefore);
        testResult.After.Should().Be(testAfter);
    }

    [Fact]
    public void Equality_TransitionsWithSameValues_AreEqual()
    {
        // arrange
        var testLeft = new TzDataTransition(BritishSummerTimeStart2024, CreateGreenwichMeanTimeUntil2024SummerTime(), CreateBritishSummerTimeFrom2024());
        var testRight = new TzDataTransition(BritishSummerTimeStart2024, CreateGreenwichMeanTimeUntil2024SummerTime(), CreateBritishSummerTimeFrom2024());

        // act
        var testResult = testLeft == testRight;

        // assert
        testResult.Should().BeTrue();
    }

    [Fact]
    public void Inequality_TransitionsWithOffsetsSwapped_AreNotEqual()
    {
        // arrange
        var testLeft = new TzDataTransition(BritishSummerTimeStart2024, CreateGreenwichMeanTimeUntil2024SummerTime(), CreateBritishSummerTimeFrom2024());
        var testRight = new TzDataTransition(BritishSummerTimeStart2024, CreateBritishSummerTimeFrom2024(), CreateGreenwichMeanTimeUntil2024SummerTime());

        // act
        var testResult = testLeft != testRight;

        // assert
        testResult.Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_TransitionsWithSameValues_ReturnsSameHashCode()
    {
        // arrange
        var testLeft = new TzDataTransition(BritishSummerTimeStart2024, CreateGreenwichMeanTimeUntil2024SummerTime(), CreateBritishSummerTimeFrom2024());
        var testRight = new TzDataTransition(BritishSummerTimeStart2024, CreateGreenwichMeanTimeUntil2024SummerTime(), CreateBritishSummerTimeFrom2024());

        // act
        var testResult = (Left: testLeft.GetHashCode(), Right: testRight.GetHashCode());

        // assert
        testResult.Left.Should().Be(testResult.Right);
    }

    [Fact]
    public void Transition_NonUniversalInstant_ThrowsArgumentException()
    {
        // arrange
        var testInstant = DateTime.SpecifyKind(BritishSummerTimeStart2024, DateTimeKind.Local);

        // act
        Action testAction = () => new TzDataTransition(testInstant, default, default);

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("instantUtc");
    }
}
