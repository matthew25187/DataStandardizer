using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

/// <summary>
/// Tests that the default value of <see cref="TzDataTimezone"/> is a valid timezone, equal to <see cref="TzDataTimezone.Etc.UTC"/>.
/// </summary>
public class TzDataTimezoneDefaultTests
{
    // The Zone of Coordinated Universal Time in the etcetera file of TZ Database 2026e, and the name that ToString gives its field.
    private const string EtcUtcIdentifier = "Etc/UTC";
    private const string EtcUtcFieldName = "UTC";

    // The STDOFF and FORMAT columns of the Zone line of Etc/UTC in the etcetera file of TZ Database 2026e.
    private static readonly TimeSpan EtcUtcStandardOffset = TimeSpan.Zero;
    private const string EtcUtcFormat = "UTC";

    public static TheoryData<string, TzDataTimezone> EtcUtc_TestCases => new()
    {
        { "field", TzDataTimezone.Etc.UTC },
        { "default instance", default },
    };

    public static TheoryData<string, Func<TzDataTimezone, object?>> Member_TestCases => new()
    {
        { nameof(TzDataTimezone.ZoneLines), timezone => timezone.ZoneLines },
        { nameof(TzDataTimezone.Latitude), timezone => timezone.Latitude },
        { nameof(TzDataTimezone.Longitude), timezone => timezone.Longitude },
        { nameof(TzDataTimezone.IsoCountryCodes), timezone => timezone.IsoCountryCodes },
        { nameof(TzDataTimezone.Comment), timezone => timezone.Comment },
        { nameof(TzDataTimezone.IsLink), timezone => timezone.IsLink },
        { nameof(TzDataTimezone.Canonical), timezone => timezone.Canonical },
        { nameof(TzDataTimezone.ToString), timezone => timezone.ToString() },
        { nameof(TzDataTimezone.GetHashCode), timezone => timezone.GetHashCode() },
        { nameof(TzDataTimezone.GetTypeCode), timezone => timezone.GetTypeCode() },
        { nameof(TzDataTimezone.CompareTo), timezone => timezone.CompareTo(TzDataTimezone.Etc.UTC) },
        { "implicit conversion to string", timezone => (string)timezone },
        { nameof(IConvertible.ToType), timezone => ((IConvertible)timezone).ToType(typeof(string), null) },
    };

    [Theory]
    [MemberData(nameof(Member_TestCases))]
    public void Member_DefaultInstance_DoesNotThrow(string testMemberName, Func<TzDataTimezone, object?> testMember)
    {
        // arrange
        var testTimezone = default(TzDataTimezone);

        // act
        Action testAction = () => testMember(testTimezone);

        // assert
        testAction.Should().NotThrow("the default instance is Etc/UTC, which has {0}", testMemberName);
    }

    [Fact]
    public void EqualityOperator_DefaultInstanceAndEtcUtc_ReturnsTrue()
    {
        // act
        var testResult = default(TzDataTimezone) == TzDataTimezone.Etc.UTC;

        // assert
        testResult.Should().BeTrue("the default instance stands for Etc/UTC");
    }

    [Fact]
    public void InequalityOperator_DefaultInstanceAndEtcUtc_ReturnsFalse()
    {
        // act
        var testResult = default(TzDataTimezone) != TzDataTimezone.Etc.UTC;

        // assert
        testResult.Should().BeFalse("the default instance stands for Etc/UTC");
    }

    [Fact]
    public void Equals_DefaultInstanceAndBoxedEtcUtc_ReturnsTrue()
    {
        // arrange
        object testOther = TzDataTimezone.Etc.UTC;

        // act
        var testResult = default(TzDataTimezone).Equals(testOther);

        // assert
        testResult.Should().BeTrue("the default instance stands for Etc/UTC");
    }

    [Fact]
    public void GetHashCode_DefaultInstanceAndEtcUtc_AreEqual()
    {
        // act
        var testResult = (Default: default(TzDataTimezone).GetHashCode(), Field: TzDataTimezone.Etc.UTC.GetHashCode());

        // assert
        testResult.Default.Should().Be(testResult.Field, "equal timezones have equal hash codes");
    }

    [Theory]
    [MemberData(nameof(EtcUtc_TestCases))]
    public void ImplicitConversionToString_EtcUtc_ReturnsEtcUtcIdentifier(string testInstance, TzDataTimezone testTimezone)
    {
        // act
        string testResult = testTimezone;

        // assert
        testResult.Should().Be(EtcUtcIdentifier, "the {0} is Etc/UTC", testInstance);
    }

    [Theory]
    [MemberData(nameof(EtcUtc_TestCases))]
    public void ToString_EtcUtc_ReturnsFieldName(string testInstance, TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.ToString();

        // assert
        testResult.Should().Be(EtcUtcFieldName, "the {0} is Etc/UTC, whose field is Etc.UTC", testInstance);
    }

    [Theory]
    [MemberData(nameof(EtcUtc_TestCases))]
    public void ZoneLines_EtcUtc_HasOneLineWithOffsetZeroAndFormatUtc(string testInstance, TzDataTimezone testTimezone)
    {
        // act
        var testResult = testTimezone.ZoneLines;

        // assert
        testResult.Should().ContainSingle("the etcetera file gives Etc/UTC, the {0}, one zone line", testInstance)
            .Which.Should().BeEquivalentTo(new { StandardOffset = EtcUtcStandardOffset, RuleKind = TzDataZoneRuleKind.None, Format = EtcUtcFormat, Until = (TzDataUntil?)null });
    }

    [Fact]
    public void Parse_EtcUtcIdentifier_ReturnsEtcUtcField()
    {
        // act
        var testResult = TzDataTimezone.Parse(EtcUtcIdentifier);

        // assert
        testResult.Should().Be(TzDataTimezone.Etc.UTC);
    }

    [Fact]
    public void CompareTo_DefaultInstanceAndEtcUtc_ReturnsZero()
    {
        // act
        var testResult = default(TzDataTimezone).CompareTo(TzDataTimezone.Etc.UTC);

        // assert
        testResult.Should().Be(0, "the default instance stands for Etc/UTC");
    }

    [Fact]
    public void CompareTo_DefaultInstanceAndLaterIdentifier_ReturnsNegative()
    {
        // act
        var testResult = default(TzDataTimezone).CompareTo(TzDataTimezone.Europe.Zurich);

        // assert
        testResult.Should().BeNegative("Etc/UTC comes before Europe/Zurich in ordinal order");
    }

    [Fact]
    public void CompareTo_DefaultInstanceAndEarlierIdentifier_ReturnsPositive()
    {
        // act
        var testResult = default(TzDataTimezone).CompareTo(TzDataTimezone.Africa.Abidjan);

        // assert
        testResult.Should().BePositive("Etc/UTC comes after Africa/Abidjan in ordinal order");
    }

    [Fact]
    public void CompareTo_Null_ThrowsArgumentNullException()
    {
        // act
        Func<int> testAction = () => TzDataTimezone.Europe.Zurich.CompareTo(null);

        // assert
        testAction.Should().Throw<ArgumentNullException>().WithParameterName("obj");
    }

    [Fact]
    public void CompareTo_OtherType_ThrowsArgumentException()
    {
        // act
        Func<int> testAction = () => TzDataTimezone.Europe.Zurich.CompareTo(EtcUtcIdentifier);

        // assert
        testAction.Should().Throw<ArgumentException>().WithParameterName("obj");
    }

    [Fact]
    public void Sort_TimezonesIncludingDefaultInstance_OrdersByIdentifier()
    {
        // arrange
        var testTimezones = new List<TzDataTimezone> { TzDataTimezone.Europe.Zurich, default, TzDataTimezone.Africa.Abidjan };

        // act
        testTimezones.Sort();

        // assert
        testTimezones.Should().Equal(TzDataTimezone.Africa.Abidjan, TzDataTimezone.Etc.UTC, TzDataTimezone.Europe.Zurich);
    }

    [Fact]
    public void GetTypeCode_DefaultInstance_ReturnsString()
    {
        // act
        var testResult = default(TzDataTimezone).GetTypeCode();

        // assert
        testResult.Should().Be(TypeCode.String, "the default instance converts as its identifier, Etc/UTC");
    }

    [Fact]
    public void ToType_DefaultInstanceToString_ReturnsEtcUtcIdentifier()
    {
        // arrange
        IConvertible testTimezone = default(TzDataTimezone);

        // act
        var testResult = testTimezone.ToType(typeof(string), null);

        // assert
        testResult.Should().Be(EtcUtcIdentifier, "the default instance converts as its identifier, Etc/UTC");
    }
}
