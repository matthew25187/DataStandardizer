using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// Reads and writes calendars for the tests of the component object model.
/// </summary>
internal static class ModelTestData
{
    public static IcalReader CreateCalendarReader(string text, IcalOptions? options = null, int bufferSize = 16 * 1024)
    {
        return new IcalReader(new StringReader(text), options ?? new IcalOptions(), bufferSize);
    }

    public static List<IcalCalendar> ReadCalendars(string text, IcalOptions? options = null, int bufferSize = 16 * 1024)
    {
        using var reader = CreateCalendarReader(text, options, bufferSize);
        return reader.ReadCalendars().ToList();
    }

    public static IcalCalendar ReadCalendar(string text, IcalOptions? options = null)
    {
        return ReadCalendars(text, options).Should().ContainSingle().Subject;
    }

    public static string WriteCalendars(IEnumerable<IcalCalendar> calendars, IcalOptions? options = null)
    {
        var text = new StringWriter();
        using (var writer = new IcalWriter(text, options ?? new IcalOptions()))
        {
            writer.WriteCalendars(calendars);
        }

        return text.ToString();
    }

    /// <summary>
    /// Gets the content lines of iCalendar text, without their line numbers, so that texts that differ only in folding compare equal.
    /// </summary>
    public static List<LineSnapshot> ContentLines(string text)
    {
        using var reader = new IcalContentLineReader(new StringReader(text));
        return reader.ReadLines().Select(line => LineSnapshot.FromLine(line) with { LineNumber = 0, PhysicalLineNumber = 0 }).ToList();
    }

    /// <summary>
    /// Builds a calendar around the lines of one component, or other calendar content.
    /// </summary>
    public static string Calendar(params string[] lines)
    {
        return IcalTestData.Crlf(["BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//Test//Test//EN", .. lines, "END:VCALENDAR"]);
    }

    /// <summary>
    /// Builds a calendar holding one VEVENT with the required properties and more.  A DTSTART is added unless the lines have one.
    /// </summary>
    public static string EventCalendar(params string[] eventLines)
    {
        string[] start = eventLines.Any(line => line.StartsWith("DTSTART", StringComparison.Ordinal)) ? [] : ["DTSTART:20260101T090000Z"];
        return Calendar(["BEGIN:VEVENT", "UID:event@example.com", "DTSTAMP:20260101T000000Z", .. start, .. eventLines, "END:VEVENT"]);
    }
}
