using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TzDataModelTests
{
    private static readonly Type[] ModelTypes =
    [
        typeof(TzDataZoneLine),
        typeof(TzDataRule),
        typeof(TzDataUntil),
        typeof(TzDataOffsetInfo),
        typeof(TzDataTransition)
    ];

    private static TzDataRule CreateRule()
    {
        return new TzDataRule("Test", 2007, int.MaxValue, 3, TzDataDayKind.WeekdayOnOrAfter, 8, DayOfWeek.Sunday, TimeSpan.FromHours(2), TzDataTimeReference.Wall, TimeSpan.FromHours(1), true, "D");
    }

    public static IEnumerable<object[]> ModelType_TestCases => ModelTypes.Select(type => new object[] { type });

    [Theory]
    [MemberData(nameof(ModelType_TestCases))]
    public void ModelType_HasNoPublicConstructors(Type testType)
    {
        // act
        var testResult = testType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        // assert
        testResult.Should().BeEmpty("only generated data may create instances of {0}", testType);
    }

    [Fact]
    public void PublicApi_ContainsNoValueTuple()
    {
        // arrange
        var publicTypes = typeof(TzDataTimezone).Assembly.GetExportedTypes();
        const BindingFlags publicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        // act
        var testResult = publicTypes
            .SelectMany(type => type.GetMembers(publicMembers).SelectMany(GetSignatureTypes).Select(signatureType => (Member: $"{type.FullName}.{signatureType.Member}", signatureType.Type)))
            .Where(signature => IsValueTuple(signature.Type))
            .Select(signature => signature.Member)
            .ToList();

        // assert
        testResult.Should().BeEmpty("System.ValueTuple is a dependency only on netstandard1.0, and its version is pinned");
    }

    private static IEnumerable<(string Member, Type Type)> GetSignatureTypes(MemberInfo member)
    {
        switch (member)
        {
            case FieldInfo field:
                yield return (field.Name, field.FieldType);
                break;
            case PropertyInfo property:
                yield return (property.Name, property.PropertyType);
                break;
            case MethodBase method:
                if (method is MethodInfo methodInfo)
                    yield return (method.Name, methodInfo.ReturnType);
                foreach (var parameter in method.GetParameters())
                    yield return (method.Name, parameter.ParameterType);
                break;
        }
    }

    private static bool IsValueTuple(Type type)
    {
        if (type.HasElementType)
            return IsValueTuple(type.GetElementType()!);

        if (type.FullName?.StartsWith("System.ValueTuple", StringComparison.Ordinal) == true)
            return true;

        return type.IsGenericType && type.GetGenericArguments().Any(IsValueTuple);
    }

    [Fact]
    public void ZoneLine_NoRules_HasEmptyRulesAndNoFixedSave()
    {
        // act
        var testResult = new TzDataZoneLine(TimeSpan.FromHours(-5), TzDataZoneRuleKind.None, null, null, "EST", null);

        // assert
        testResult.StandardOffset.Should().Be(TimeSpan.FromHours(-5));
        testResult.RuleKind.Should().Be(TzDataZoneRuleKind.None);
        testResult.FixedSave.Should().BeNull();
        testResult.Rules.Should().BeEmpty();
        testResult.Format.Should().Be("EST");
        testResult.Until.Should().BeNull();
    }

    [Fact]
    public void ZoneLine_FixedSave_HasFixedSaveAndEmptyRules()
    {
        // arrange
        var testUntil = new TzDataUntil(1945, 9, TzDataDayKind.DayOfMonth, 30, null, TimeSpan.FromHours(2), TzDataTimeReference.Wall);

        // act
        var testResult = new TzDataZoneLine(TimeSpan.FromHours(-5), TzDataZoneRuleKind.FixedSave, TimeSpan.FromHours(1), null, "EWT", testUntil);

        // assert
        testResult.FixedSave.Should().Be(TimeSpan.FromHours(1));
        testResult.Rules.Should().BeEmpty();
        testResult.Until.Should().Be(testUntil);
    }

    [Fact]
    public void ZoneLine_RuleSet_ExposesRulesReadOnly()
    {
        // arrange
        var testRules = new[] { CreateRule() };

        // act
        var testResult = new TzDataZoneLine(TimeSpan.FromHours(-5), TzDataZoneRuleKind.RuleSet, null, testRules, "E%sT", null);

        // assert
        testResult.Rules.Should().Equal(testRules);
        testResult.Rules.Should().NotBeAssignableTo<TzDataRule[]>();
        testResult.Rules.Should().BeAssignableTo<ICollection<TzDataRule>>().Which.IsReadOnly.Should().BeTrue();
    }

    public static IEnumerable<object?[]> ZoneLine_InconsistentRules_TestCases
    {
        get
        {
            yield return new object?[] { TzDataZoneRuleKind.None, TimeSpan.FromHours(1), null, "fixedSave" };
            yield return new object?[] { TzDataZoneRuleKind.None, null, new[] { CreateRule() }, "rules" };
            yield return new object?[] { TzDataZoneRuleKind.FixedSave, null, null, "fixedSave" };
            yield return new object?[] { TzDataZoneRuleKind.FixedSave, TimeSpan.FromHours(1), new[] { CreateRule() }, "rules" };
            yield return new object?[] { TzDataZoneRuleKind.RuleSet, TimeSpan.FromHours(1), new[] { CreateRule() }, "fixedSave" };
            yield return new object?[] { TzDataZoneRuleKind.RuleSet, null, null, "rules" };
            yield return new object?[] { (TzDataZoneRuleKind)3, null, null, "ruleKind" };
        }
    }

    [Theory]
    [MemberData(nameof(ZoneLine_InconsistentRules_TestCases))]
    public void ZoneLine_InconsistentRules_ThrowsArgumentException(TzDataZoneRuleKind testRuleKind, TimeSpan? testFixedSave, TzDataRule[]? testRules, string expectedParamName)
    {
        // act
        var testAction = () => new TzDataZoneLine(TimeSpan.Zero, testRuleKind, testFixedSave, testRules, "GMT", null);

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be(expectedParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ZoneLine_MissingFormat_ThrowsArgumentException(string? testFormat)
    {
        // act
        var testAction = () => new TzDataZoneLine(TimeSpan.Zero, TzDataZoneRuleKind.None, null, null, testFormat!, null);

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("format");
    }

    [Fact]
    public void Until_ValidArguments_SetsPropertiesAndComparesByValue()
    {
        // act
        var testResult = new TzDataUntil(1918, 3, TzDataDayKind.LastWeekday, null, DayOfWeek.Sunday, TimeSpan.FromHours(2), TzDataTimeReference.Standard);
        var testOther = new TzDataUntil(1918, 3, TzDataDayKind.LastWeekday, null, DayOfWeek.Sunday, TimeSpan.FromHours(2), TzDataTimeReference.Standard);

        // assert
        testResult.Year.Should().Be(1918);
        testResult.Month.Should().Be(3);
        testResult.DayKind.Should().Be(TzDataDayKind.LastWeekday);
        testResult.Day.Should().BeNull();
        testResult.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        testResult.Time.Should().Be(TimeSpan.FromHours(2));
        testResult.TimeReference.Should().Be(TzDataTimeReference.Standard);
        (testResult == testOther).Should().BeTrue();
        testResult.GetHashCode().Should().Be(testOther.GetHashCode());
    }

    [Fact]
    public void Until_InvalidMonth_ThrowsArgumentOutOfRangeException()
    {
        // act
        var testAction = () => new TzDataUntil(1918, 13, TzDataDayKind.DayOfMonth, 1, null, TimeSpan.Zero, TzDataTimeReference.Wall);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("month");
    }

    [Fact]
    public void OffsetInfo_ValidArguments_SetsPropertiesAndSumsUtcOffset()
    {
        // arrange
        var testFrom = new DateTime(2024, 3, 31, 1, 0, 0, DateTimeKind.Utc);
        var testUntil = new DateTime(2024, 10, 27, 1, 0, 0, DateTimeKind.Utc);

        // act
        var testResult = new TzDataOffsetInfo(TimeSpan.Zero, TimeSpan.FromHours(1), true, "BST", testFrom, testUntil);

        // assert
        testResult.UtcOffset.Should().Be(TimeSpan.FromHours(1));
        testResult.StandardOffset.Should().Be(TimeSpan.Zero);
        testResult.DaylightSavings.Should().Be(TimeSpan.FromHours(1));
        testResult.IsDaylightSavingTime.Should().BeTrue();
        testResult.Abbreviation.Should().Be("BST");
        testResult.ValidFromUtc.Should().Be(testFrom);
        testResult.ValidUntilUtc.Should().Be(testUntil);
    }

    [Fact]
    public void OffsetInfo_NegativeDaylightSavings_SubtractsFromStandardOffset()
    {
        // act
        var testResult = new TzDataOffsetInfo(TimeSpan.FromHours(1), TimeSpan.FromHours(-1), true, "GMT", null, null);

        // assert
        testResult.UtcOffset.Should().Be(TimeSpan.Zero);
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
    [InlineData(DateTimeKind.Local, DateTimeKind.Utc, "validFromUtc")]
    [InlineData(DateTimeKind.Unspecified, DateTimeKind.Utc, "validFromUtc")]
    [InlineData(DateTimeKind.Utc, DateTimeKind.Local, "validUntilUtc")]
    [InlineData(DateTimeKind.Utc, DateTimeKind.Unspecified, "validUntilUtc")]
    public void OffsetInfo_NonUniversalPeriod_ThrowsArgumentException(DateTimeKind testFromKind, DateTimeKind testUntilKind, string expectedParamName)
    {
        // act
        var testAction = () => new TzDataOffsetInfo(TimeSpan.Zero, TimeSpan.Zero, false, "GMT", new DateTime(2024, 1, 1, 0, 0, 0, testFromKind), new DateTime(2025, 1, 1, 0, 0, 0, testUntilKind));

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be(expectedParamName);
    }

    [Fact]
    public void OffsetInfo_PeriodEndNotAfterStart_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var testInstant = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // act
        var testAction = () => new TzDataOffsetInfo(TimeSpan.Zero, TimeSpan.Zero, false, "GMT", testInstant, testInstant);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("validUntilUtc");
    }

    [Fact]
    public void Transition_ValidArguments_SetsPropertiesAndComparesByValue()
    {
        // arrange
        var testInstant = new DateTime(2024, 3, 31, 1, 0, 0, DateTimeKind.Utc);
        var testBefore = new TzDataOffsetInfo(TimeSpan.Zero, TimeSpan.Zero, false, "GMT", null, testInstant);
        var testAfter = new TzDataOffsetInfo(TimeSpan.Zero, TimeSpan.FromHours(1), true, "BST", testInstant, null);

        // act
        var testResult = new TzDataTransition(testInstant, testBefore, testAfter);
        var testOther = new TzDataTransition(testInstant, testBefore, testAfter);

        // assert
        testResult.InstantUtc.Should().Be(testInstant);
        testResult.Before.Should().Be(testBefore);
        testResult.After.Should().Be(testAfter);
        (testResult == testOther).Should().BeTrue();
        (testResult != new TzDataTransition(testInstant, testAfter, testBefore)).Should().BeTrue();
        testResult.GetHashCode().Should().Be(testOther.GetHashCode());
    }

    [Fact]
    public void Transition_NonUniversalInstant_ThrowsArgumentException()
    {
        // act
        var testAction = () => new TzDataTransition(new DateTime(2024, 3, 31, 1, 0, 0, DateTimeKind.Local), default, default);

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("instantUtc");
    }
}
