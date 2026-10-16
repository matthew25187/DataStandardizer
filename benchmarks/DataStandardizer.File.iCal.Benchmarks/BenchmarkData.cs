using System.Globalization;
using System.Text;

namespace DataStandardizer.File.iCal.Benchmarks;

/// <summary>
/// Generates the seeded, in-memory iCalendar input shared by all benchmarks.
/// </summary>
public static class BenchmarkData
{
    public const int EventCount = 5_000;
    public const int RecurringEventCount = 140;
    public const int Seed = 20260101;

    /// <summary>
    /// The start of the expansion window, inclusive: 2026-01-01T00:00:00Z.
    /// </summary>
    public static readonly DateTime WindowStartUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// The end of the expansion window, exclusive: ten years after <see cref="WindowStartUtc"/>.
    /// </summary>
    public static readonly DateTime WindowEndUtc = new(2036, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private const string NewYork = "America/New_York";
    private const string Auckland = "Pacific/Auckland";
    private const int FoldOctets = 75;

    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private static readonly string[] Summaries =
    [
        "Team stand-up",
        "Budget review, Q3",
        "Planning; follow-up from last week",
        "Lunch with the Wellington office",
        "1:1 catch-up",
        "Release readiness review",
        "Customer call: Acme Ltd",
        "Hui at Te Papa",
        "Board meeting \\ minutes to follow",
        "Quarterly all-hands"
    ];

    private static readonly string[] Sentences =
    [
        "Please read the attached notes before the meeting.",
        "We will cover the roadmap, the budget and the hiring plan.",
        "Dial-in details: +64 9 555 0100, then the PIN; the host will admit you.",
        "Bring your laptop, a charger and any open questions.",
        "Agenda:\n1. Welcome\n2. Updates from each team\n3. Next steps",
        "The room has a projector; the screen is shared over the conference link.",
        "If you cannot attend, send a delegate or reply with your comments.",
        "Coffee, tea and kai will be provided.",
        "Path to the shared folder: \\\\fileserver\\projects\\2026",
        "Minutes from the last session are in the team space, under \"Meetings\"."
    ];

    private static readonly string[] Locations =
    [
        "Room 4.12, Level 4",
        "Boardroom",
        "Ōtautahi office; ground floor",
        "Café Māori, 12 Queen Street, Auckland",
        "https://meet.example.com/abc-defg-hij",
        "Main auditorium"
    ];

    private static readonly string[] Categories = ["MEETING", "PROJECT", "PERSONAL", "TRAVEL", "HOLIDAY", "REVIEW", "TRAINING"];

    private static readonly (string Name, string Email)[] People =
    [
        ("Smith, Alice", "alice.smith@example.com"),
        ("Bob Jones", "bob.jones@example.com"),
        ("Carol Brown", "carol.brown@example.org"),
        ("Taylor, Dave", "dave.taylor@example.net"),
        ("Erin Wilson", "erin.wilson@example.com"),
        ("Frank Walker", "frank.walker@example.org"),
        ("Grace Te Rangi", "grace.terangi@example.co.nz"),
        ("Heidi White", "heidi.white@example.com")
    ];

    private static readonly string[] Roles = ["REQ-PARTICIPANT", "OPT-PARTICIPANT", "CHAIR"];
    private static readonly string[] ParticipationStatuses = ["ACCEPTED", "DECLINED", "TENTATIVE", "NEEDS-ACTION"];

    /// <summary>
    /// The kinds of recurring event in the corpus, chosen in turn.  Each fixes the rule, the time zone and the weekday or day of the
    /// month that DTSTART falls on, so that DTSTART is always an instance of the rule.
    /// </summary>
    private enum RecurrenceKind
    {
        Daily,
        DailyIntervalCount,
        WeeklyByDay,
        FortnightlyByDayUntil,
        MonthlyByMonthDay,
        MonthlyLastDayCountUtc,
        MonthlyLastWeekdayBySetPos,
        MonthlySecondTuesday,
        YearlyLastSundayOfMarch,
        YearlyDate,
        WeeklySaturdayCountUtc,
        WeekdaysUntil,
        QuarterlyFirstMonday,
        YearlyByMonthByMonthDay
    }

    /// <summary>
    /// Builds the main corpus: a VCALENDAR with two VTIMEZONE components and <see cref="EventCount"/> VEVENT components.  The events start
    /// in a time zone (about 60%), in UTC (about 25%) or on a date (about 15%), and 1 in 6 has an RRULE.
    /// </summary>
    public static byte[] CreateIcs()
    {
        var random = new Random(Seed);
        var builder = new StringBuilder(EventCount * 1_200);
        AppendCalendarStart(builder);
        for (var i = 0; i < EventCount; i++)
        {
            var start = new DateTime(2026, 1, 5).AddDays(random.Next(0, 720)).AddMinutes(15 * random.Next(28, 76));
            var kindRoll = random.Next(100);
            if (i % 6 == 0)
            {
                AppendRecurringEvent(builder, random, i, (RecurrenceKind)(i / 6 % Enum.GetValues<RecurrenceKind>().Length), start);
            }
            else if (kindRoll < 60)
            {
                var tzid = random.Next(2) == 0 ? NewYork : Auckland;
                AppendEvent(builder, random, i,
                    $"DTSTART;TZID={tzid}:{FormatLocal(start)}",
                    $"DTEND;TZID={tzid}:{FormatLocal(start.AddMinutes(30 * random.Next(1, 7)))}",
                    rule: null, exceptionDatesLine: null, extraDatesLine: null);
            }
            else if (kindRoll < 85)
            {
                AppendEvent(builder, random, i,
                    $"DTSTART:{FormatUtc(start)}",
                    $"DTEND:{FormatUtc(start.AddMinutes(30 * random.Next(1, 7)))}",
                    rule: null, exceptionDatesLine: null, extraDatesLine: null);
            }
            else
            {
                AppendEvent(builder, random, i,
                    $"DTSTART;VALUE=DATE:{FormatDate(start)}",
                    $"DTEND;VALUE=DATE:{FormatDate(start.AddDays(random.Next(1, 4)))}",
                    rule: null, exceptionDatesLine: null, extraDatesLine: null);
            }
        }

        AppendCalendarEnd(builder);
        return Utf8NoBom.GetBytes(builder.ToString());
    }

    /// <summary>
    /// Builds the expansion corpus: a VCALENDAR with the same two VTIMEZONE components and <see cref="RecurringEventCount"/> recurring
    /// VEVENT components, cycling through every kind of recurrence.  Every event starts in the first half of 2026, inside the window.
    /// </summary>
    public static byte[] CreateRecurringIcs()
    {
        var random = new Random(Seed + 1);
        var builder = new StringBuilder(RecurringEventCount * 1_200);
        AppendCalendarStart(builder);
        var kindCount = Enum.GetValues<RecurrenceKind>().Length;
        for (var i = 0; i < RecurringEventCount; i++)
        {
            var start = new DateTime(2026, 1, 5).AddDays(random.Next(0, 150)).AddMinutes(15 * random.Next(28, 76));
            AppendRecurringEvent(builder, random, i, (RecurrenceKind)(i % kindCount), start);
        }

        AppendCalendarEnd(builder);
        return Utf8NoBom.GetBytes(builder.ToString());
    }

    /// <summary>
    /// Counts the logical (unfolded) content lines of a corpus.
    /// </summary>
    public static int CountContentLines(byte[] ics)
    {
        using var stream = new MemoryStream(ics, writable: false);
        using var reader = new IcalContentLineReader(stream);
        var count = 0;
        while (reader.ReadContentLine())
        {
            count++;
        }

        return count;
    }

    private static void AppendCalendarStart(StringBuilder builder)
    {
        AppendLine(builder, "BEGIN:VCALENDAR");
        AppendLine(builder, "VERSION:2.0");
        AppendLine(builder, "PRODID:-//DataStandardizer//iCal Benchmarks//EN");
        AppendLine(builder, "CALSCALE:GREGORIAN");
        AppendLine(builder, "METHOD:PUBLISH");

        AppendLine(builder, "BEGIN:VTIMEZONE");
        AppendLine(builder, $"TZID:{NewYork}");
        AppendLine(builder, "BEGIN:DAYLIGHT");
        AppendLine(builder, "TZOFFSETFROM:-0500");
        AppendLine(builder, "TZOFFSETTO:-0400");
        AppendLine(builder, "TZNAME:EDT");
        AppendLine(builder, "DTSTART:20070311T020000");
        AppendLine(builder, "RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=2SU");
        AppendLine(builder, "END:DAYLIGHT");
        AppendLine(builder, "BEGIN:STANDARD");
        AppendLine(builder, "TZOFFSETFROM:-0400");
        AppendLine(builder, "TZOFFSETTO:-0500");
        AppendLine(builder, "TZNAME:EST");
        AppendLine(builder, "DTSTART:20071104T020000");
        AppendLine(builder, "RRULE:FREQ=YEARLY;BYMONTH=11;BYDAY=1SU");
        AppendLine(builder, "END:STANDARD");
        AppendLine(builder, "END:VTIMEZONE");

        AppendLine(builder, "BEGIN:VTIMEZONE");
        AppendLine(builder, $"TZID:{Auckland}");
        AppendLine(builder, "BEGIN:DAYLIGHT");
        AppendLine(builder, "TZOFFSETFROM:+1200");
        AppendLine(builder, "TZOFFSETTO:+1300");
        AppendLine(builder, "TZNAME:NZDT");
        AppendLine(builder, "DTSTART:20070930T020000");
        AppendLine(builder, "RRULE:FREQ=YEARLY;BYMONTH=9;BYDAY=-1SU");
        AppendLine(builder, "END:DAYLIGHT");
        AppendLine(builder, "BEGIN:STANDARD");
        AppendLine(builder, "TZOFFSETFROM:+1300");
        AppendLine(builder, "TZOFFSETTO:+1200");
        AppendLine(builder, "TZNAME:NZST");
        AppendLine(builder, "DTSTART:20080406T030000");
        AppendLine(builder, "RRULE:FREQ=YEARLY;BYMONTH=4;BYDAY=1SU");
        AppendLine(builder, "END:STANDARD");
        AppendLine(builder, "END:VTIMEZONE");
    }

    private static void AppendCalendarEnd(StringBuilder builder)
    {
        AppendLine(builder, "END:VCALENDAR");
    }

    /// <summary>
    /// Appends a recurring event of the given kind, moving <paramref name="start"/> onto a day that the rule gives.
    /// </summary>
    private static void AppendRecurringEvent(StringBuilder builder, Random random, int index, RecurrenceKind kind, DateTime start)
    {
        var time = start.TimeOfDay;
        var date = start.Date;
        string? tzid;
        string rule;
        string[]? exceptionDates = null;
        string[]? extraDates = null;
        switch (kind)
        {
            case RecurrenceKind.Daily:
                tzid = NewYork;
                rule = "FREQ=DAILY";
                exceptionDates = [FormatLocal(date.AddDays(3) + time), FormatLocal(date.AddDays(10) + time)];
                break;
            case RecurrenceKind.DailyIntervalCount:
                tzid = Auckland;
                rule = "FREQ=DAILY;INTERVAL=2;COUNT=500";
                break;
            case RecurrenceKind.WeeklyByDay:
                tzid = NewYork;
                date = NextWeekday(date, DayOfWeek.Monday);
                rule = "FREQ=WEEKLY;BYDAY=MO,WE,FR";
                exceptionDates = [FormatLocal(date.AddDays(7) + time), FormatLocal(date.AddDays(14) + time)];
                extraDates = [FormatLocal(date.AddDays(5).AddHours(16))];
                break;
            case RecurrenceKind.FortnightlyByDayUntil:
                tzid = Auckland;
                date = NextWeekday(date, DayOfWeek.Tuesday);
                rule = "FREQ=WEEKLY;INTERVAL=2;BYDAY=TU,TH;UNTIL=20311231T000000Z";
                break;
            case RecurrenceKind.MonthlyByMonthDay:
                tzid = NewYork;
                date = new DateTime(date.Year, date.Month, 15).AddMonths(date.Day > 15 ? 1 : 0);
                rule = "FREQ=MONTHLY;BYMONTHDAY=15";
                extraDates = [FormatLocal(date.AddDays(3) + time)];
                break;
            case RecurrenceKind.MonthlyLastDayCountUtc:
                tzid = null;
                date = new DateTime(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month));
                rule = "FREQ=MONTHLY;BYMONTHDAY=-1;COUNT=60";
                break;
            case RecurrenceKind.MonthlyLastWeekdayBySetPos:
                tzid = Auckland;
                date = LastWeekdayOfMonth(date.Year, date.Month);
                rule = "FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1";
                break;
            case RecurrenceKind.MonthlySecondTuesday:
                tzid = NewYork;
                date = NthWeekdayOfMonth(date.Year, date.Month, DayOfWeek.Tuesday, 2);
                rule = "FREQ=MONTHLY;BYDAY=2TU";
                exceptionDates = [FormatLocal(NthWeekdayOfMonth(date.Year, date.Month + 1, DayOfWeek.Tuesday, 2) + time)];
                break;
            case RecurrenceKind.YearlyLastSundayOfMarch:
                tzid = Auckland;
                date = LastWeekdayOfMonth(2026, 3, DayOfWeek.Sunday);
                rule = "FREQ=YEARLY;BYMONTH=3;BYDAY=-1SU";
                break;
            case RecurrenceKind.YearlyDate:
                AppendEvent(builder, random, index,
                    $"DTSTART;VALUE=DATE:{FormatDate(date)}",
                    $"DTEND;VALUE=DATE:{FormatDate(date.AddDays(1))}",
                    "FREQ=YEARLY", exceptionDatesLine: null, extraDatesLine: null);
                return;
            case RecurrenceKind.WeeklySaturdayCountUtc:
                tzid = null;
                date = NextWeekday(date, DayOfWeek.Saturday);
                rule = "FREQ=WEEKLY;BYDAY=SA;COUNT=100";
                break;
            case RecurrenceKind.WeekdaysUntil:
                tzid = NewYork;
                date = NextWeekday(date, DayOfWeek.Wednesday);
                rule = "FREQ=DAILY;BYDAY=MO,TU,WE,TH,FR;UNTIL=20300630T000000Z";
                exceptionDates = [FormatLocal(date.AddDays(1) + time), FormatLocal(date.AddDays(2) + time)];
                break;
            case RecurrenceKind.QuarterlyFirstMonday:
                tzid = Auckland;
                date = NthWeekdayOfMonth(date.Year, date.Month, DayOfWeek.Monday, 1);
                rule = "FREQ=MONTHLY;INTERVAL=3;BYDAY=1MO";
                break;
            case RecurrenceKind.YearlyByMonthByMonthDay:
                tzid = NewYork;
                date = new DateTime(2026, 6, 1);
                rule = "FREQ=YEARLY;BYMONTH=6,12;BYMONTHDAY=1";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }

        start = date + time;
        var end = start.AddMinutes(30 * random.Next(1, 5));
        var parameters = tzid is null ? string.Empty : $";TZID={tzid}";
        var suffix = tzid is null ? "Z" : string.Empty;
        AppendEvent(builder, random, index, $"DTSTART{parameters}:{FormatLocal(start)}{suffix}", $"DTEND{parameters}:{FormatLocal(end)}{suffix}",
            rule,
            exceptionDates is null ? null : $"EXDATE{parameters}:{string.Join(",", exceptionDates.Select(d => d + suffix))}",
            extraDates is null ? null : $"RDATE{parameters}:{string.Join(",", extraDates.Select(d => d + suffix))}");
    }

    /// <summary>
    /// Appends a VEVENT with a mix of properties.  The time properties are given as complete content lines, before folding.
    /// </summary>
    private static void AppendEvent(StringBuilder builder, Random random, int index, string dtStart, string dtEnd, string? rule,
        string? exceptionDatesLine, string? extraDatesLine)
    {
        AppendLine(builder, "BEGIN:VEVENT");
        AppendLine(builder, $"UID:{index:D6}-{random.Next():x8}@benchmarks.datastandardizer.example");
        AppendLine(builder, $"DTSTAMP:{FormatUtc(new DateTime(2026, 1, 1, 12, 0, 0).AddMinutes(random.Next(0, 500_000)))}");
        AppendLine(builder, dtStart);
        AppendLine(builder, dtEnd);
        if (rule is not null)
        {
            AppendLine(builder, $"RRULE:{rule}");
        }

        if (exceptionDatesLine is not null)
        {
            AppendLine(builder, exceptionDatesLine);
        }

        if (extraDatesLine is not null)
        {
            AppendLine(builder, extraDatesLine);
        }

        AppendLine(builder, $"SUMMARY:{EscapeText(Summaries[random.Next(Summaries.Length)])}");

        // Long enough, at 1 to 6 sentences, that most descriptions are folded.
        var sentenceCount = random.Next(1, 7);
        var description = new StringBuilder();
        for (var i = 0; i < sentenceCount; i++)
        {
            if (i > 0)
            {
                description.Append(i % 3 == 0 ? "\n\n" : " ");
            }

            description.Append(Sentences[random.Next(Sentences.Length)]);
        }

        AppendLine(builder, $"DESCRIPTION:{EscapeText(description.ToString())}");
        AppendLine(builder, $"LOCATION:{EscapeText(Locations[random.Next(Locations.Length)])}");

        var categoryCount = random.Next(0, 4);
        if (categoryCount > 0)
        {
            var categories = Enumerable.Range(0, categoryCount).Select(_ => Categories[random.Next(Categories.Length)]).Distinct();
            AppendLine(builder, $"CATEGORIES:{string.Join(",", categories)}");
        }

        var organizer = People[random.Next(People.Length)];
        AppendLine(builder, $"ORGANIZER;CN={QuoteParameter(organizer.Name)}:mailto:{organizer.Email}");
        var attendeeCount = random.Next(0, 5);
        for (var i = 0; i < attendeeCount; i++)
        {
            var attendee = People[random.Next(People.Length)];
            AppendLine(builder,
                $"ATTENDEE;CN={QuoteParameter(attendee.Name)};ROLE={Roles[random.Next(Roles.Length)]};" +
                $"PARTSTAT={ParticipationStatuses[random.Next(ParticipationStatuses.Length)]};RSVP={(random.Next(2) == 0 ? "TRUE" : "FALSE")}:" +
                $"mailto:{attendee.Email}");
        }

        AppendLine(builder, $"SEQUENCE:{random.Next(0, 4)}");
        AppendLine(builder, random.Next(4) == 0 ? "STATUS:TENTATIVE" : "STATUS:CONFIRMED");
        AppendLine(builder, random.Next(3) == 0 ? "TRANSP:TRANSPARENT" : "TRANSP:OPAQUE");
        AppendLine(builder, "END:VEVENT");
    }

    /// <summary>
    /// Appends a content line, folded so that no physical line is longer than 75 octets of UTF-8 (RFC 5545 §3.1).
    /// </summary>
    private static void AppendLine(StringBuilder builder, string line)
    {
        var octets = 0;
        var limit = FoldOctets;
        for (var i = 0; i < line.Length; i++)
        {
            var width = char.IsHighSurrogate(line[i]) ? 4 : char.IsLowSurrogate(line[i]) ? 0 : line[i] < 0x80 ? 1 : line[i] < 0x800 ? 2 : 3;
            if (octets + width > limit)
            {
                builder.Append("\r\n ");
                octets = 0;
                limit = FoldOctets - 1;
            }

            builder.Append(line[i]);
            octets += width;
        }

        builder.Append("\r\n");
    }

    /// <summary>
    /// Escapes a TEXT value (RFC 5545 §3.3.11).
    /// </summary>
    private static string EscapeText(string value)
    {
        return value.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\n", "\\n");
    }

    private static string QuoteParameter(string value)
    {
        return value.IndexOfAny([',', ';', ':']) < 0 ? value : $"\"{value}\"";
    }

    private static DateTime NextWeekday(DateTime date, DayOfWeek dayOfWeek)
    {
        return date.AddDays(((int)dayOfWeek - (int)date.DayOfWeek + 7) % 7);
    }

    private static DateTime NthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, int n)
    {
        var first = new DateTime(year, 1, 1).AddMonths(month - 1);
        return NextWeekday(first, dayOfWeek).AddDays(7 * (n - 1));
    }

    private static DateTime LastWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek)
    {
        var last = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        return last.AddDays(-(((int)last.DayOfWeek - (int)dayOfWeek + 7) % 7));
    }

    private static DateTime LastWeekdayOfMonth(int year, int month)
    {
        var last = new DateTime(year, month, DateTime.DaysInMonth(year, month));
        return last.DayOfWeek switch
        {
            DayOfWeek.Saturday => last.AddDays(-1),
            DayOfWeek.Sunday => last.AddDays(-2),
            _ => last
        };
    }

    private static string FormatLocal(DateTime value) => value.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture);

    private static string FormatUtc(DateTime value) => FormatLocal(value) + "Z";

    private static string FormatDate(DateTime value) => value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
}
