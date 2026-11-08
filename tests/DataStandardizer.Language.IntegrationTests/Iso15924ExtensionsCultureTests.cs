using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Language.IntegrationTests;

/// <summary>
/// Tests of the <see cref="Iso15924Extensions"/> methods under cultures whose default calendar isn't Gregorian. These
/// depend on the runtime's culture data, which gives th-TH and ar-SA their calendars, so they are integration tests.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Iso15924ExtensionsCultureTests
{
    // Cultures whose default calendar isn't Gregorian: th-TH uses the Thai Buddhist calendar, and ar-SA the Um Al
    // Qura calendar.
    private const string ThaiCultureName = "th-TH";
    private const string SaudiArabicCultureName = "ar-SA";

    // Values for Bopo from the ISO 15924 code list of the ISO 15924 Registration Authority (Unicode, Inc.):
    // https://www.unicode.org/iso15924/iso15924-codes.html
    // "Date" column: the date the entry was last changed.
    private static readonly DateOnly BopomofoDate = new(2004, 5, 1);

    // "Unicode Version" column: the Unicode version in which the script was added.
    private const double BopomofoUnicodeVersion = 1.1;

    // "PVA" column: the Unicode property value alias.
    private const string BopomofoPropertyValueAlias = "Bopomofo";

    // "English Name" column.
    private const string BopomofoEnglishName = "Bopomofo";

    // "Nom français" column.
    private const string BopomofoFrenchName = "bopomofo";

    #region Test: Iso15924Extensions_UnderCultureWithNonGregorianCalendar

    [Theory]
    [MemberData(nameof(Iso15924Extensions_UnderCultureWithNonGregorianCalendar_TestCaseGenerator.TestCases), MemberType = typeof(Iso15924Extensions_UnderCultureWithNonGregorianCalendar_TestCaseGenerator))]
    public void Iso15924Extensions_UnderCultureWithNonGregorianCalendar_ReturnsScriptValue(string cultureName, Type expectedCalendarType, Func<object?> testMethod, object expectedResult)
    {
        // arrange
        using var cultureScope = new CultureScope(cultureName);
        var scopedCalendar = CultureInfo.CurrentCulture.Calendar;

        // act
        var testResult = testMethod();

        // assert
        scopedCalendar.Should().BeOfType(expectedCalendarType);
        testResult.Should().Be(expectedResult);
    }

    private class Iso15924Extensions_UnderCultureWithNonGregorianCalendar_TestCaseGenerator
    {
        public static TheoryData<string, Type, Func<object?>, object> TestCases => new()
        {
            { ThaiCultureName, typeof(ThaiBuddhistCalendar), () => Iso15924Script.Bopo.GetDate(), BopomofoDate },
            { ThaiCultureName, typeof(ThaiBuddhistCalendar), () => Iso15924Script.Bopo.GetAge(), BopomofoUnicodeVersion },
            { ThaiCultureName, typeof(ThaiBuddhistCalendar), () => Iso15924Script.Bopo.GetAlias(), BopomofoPropertyValueAlias },
            { ThaiCultureName, typeof(ThaiBuddhistCalendar), () => Iso15924Script.Bopo.GetEnglishName(), BopomofoEnglishName },
            { ThaiCultureName, typeof(ThaiBuddhistCalendar), () => Iso15924Script.Bopo.GetFrenchName(), BopomofoFrenchName },
            { SaudiArabicCultureName, typeof(UmAlQuraCalendar), () => Iso15924Script.Bopo.GetDate(), BopomofoDate },
            { SaudiArabicCultureName, typeof(UmAlQuraCalendar), () => Iso15924Script.Bopo.GetAge(), BopomofoUnicodeVersion },
            { SaudiArabicCultureName, typeof(UmAlQuraCalendar), () => Iso15924Script.Bopo.GetAlias(), BopomofoPropertyValueAlias },
            { SaudiArabicCultureName, typeof(UmAlQuraCalendar), () => Iso15924Script.Bopo.GetEnglishName(), BopomofoEnglishName },
            { SaudiArabicCultureName, typeof(UmAlQuraCalendar), () => Iso15924Script.Bopo.GetFrenchName(), BopomofoFrenchName },
        };
    }

    #endregion
}
