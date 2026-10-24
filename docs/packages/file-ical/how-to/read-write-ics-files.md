---
title: Read and write .ics files
parent: File.iCal
grand_parent: Packages
nav_order: 1
---

# Read and write .ics files

`IcalReader` reads `.ics` data (RFC 5545) into a component object model:
`IcalCalendar`, `IcalEvent`, `IcalTodo` and so on. `IcalWriter` writes that model
back out. Both check each calendar against RFC 5545 by default.

## Read a file

```csharp
using DataStandardizer.File.iCal;

using var reader = new IcalReader("calendar.ics");
foreach (var calendar in reader.ReadCalendars())
{
    foreach (var calendarEvent in calendar.Events)
    {
        Console.WriteLine($"{calendarEvent.DtStart}: {calendarEvent.Summary}");
    }
}
```

`ReadCalendars` reads one calendar at a time as you enumerate it, so a large file
doesn't have to fit in memory. A file can hold more than one VCALENDAR.

To read one calendar at a time, call `ReadCalendar`. It returns `null` at the end of
the data:

```csharp
using var reader = new IcalReader("calendar.ics");
var calendar = reader.ReadCalendar();
if (calendar is null)
{
    Console.WriteLine("The file holds no calendar.");
}
```

### Constructors

| Source | Notes |
| --- | --- |
| A file path | Opens the file and closes it when the reader is disposed. Needs .NET Standard 2.0 or later. |
| A `Stream` | Left open when the reader is disposed. |
| A `TextReader` | Left open when the reader is disposed. `IcalOptions.Encoding` must be `null`, since the text reader has already decoded the data. |

Each constructor has an overload that takes an `IcalOptions`. A file or stream is
read as UTF-8, the default charset of RFC 5545, or in `IcalOptions.Encoding` if you
set it. A byte order mark at the start of the data takes precedence over both.

When the data comes from a file or stream as UTF-8, a fold that splits a multi-byte
character is repaired before decoding. A `TextReader` has already decoded the data,
so it can't be repaired there.

Only iCalendar 2.0 is read. A calendar whose `VERSION` doesn't include 2.0, such as
the `VERSION:1.0` of vCalendar 1.0, throws `IcalFileException` as soon as the
`VERSION` line is read.

## The model at a glance

- **`IcalCalendar`:** the VCALENDAR. `Events`, `Todos`, `Journals`, `FreeBusy` and
  `TimeZones` give its components by type. Calendar properties such as `ProductId`,
  `Method` and the RFC 7986 properties (`CalendarName`, `Color` and so on) have typed
  accessors.
- **`IcalEvent`, `IcalTodo`, `IcalJournal`, `IcalFreeBusy`, `IcalTimeZone` and
  `IcalAlarm`:** typed components. For example, `IcalEvent` has `Uid`, `DtStamp`,
  `DtStart`, `DtEnd`, `Duration`, `Summary`, `RecurrenceRules` and `Alarms`.
- **`IcalComponent`:** the base type. A component the reader doesn't recognise,
  such as an `X-` component, is an `IcalComponent` itself.

Every component keeps its properties, in order, in `Properties`, as `IcalContentLine`
objects. A typed accessor reads and writes that list: a getter reads the first
property of its name, and a setter replaces every property of its name. Unknown and
`X-` properties are kept, with their parameters, and written back as they were read.

For a property with no typed accessor, use `GetProperty` (the first of a name) or
`GetProperties` (all of a name). Names are compared case-insensitively.

```csharp
var calendarName = calendar.GetProperty("X-WR-CALNAME");
Console.WriteLine(calendarName?.Value);

foreach (var attendee in calendarEvent.GetProperties("ATTENDEE"))
{
    var commonName = attendee.GetParameter("CN");
    Console.WriteLine($"{attendee.Value} ({commonName?.Values[0]})");
}
```

`IcalContentLine.Value` is the value as it appears in the file, with any TEXT
escaping in place. `GetTextValue()` removes the escaping. `SetProperty` and
`RemoveProperties` change the list by name.

## Write a file

```csharp
using var writer = new IcalWriter("calendar.ics");
writer.WriteCalendars(calendars);
```

`WriteCalendar` writes one calendar, and `WriteCalendars` writes a sequence. The
writer takes a file path (.NET Standard 2.0 or later), a `Stream` or a `TextWriter`,
each with an optional `IcalOptions`. A file or stream is written as UTF-8 without a
byte order mark.

The writer always writes `VERSION:2.0`, in place of the calendar's own `VERSION`, or
first if it has none.

### Line breaks and folding

Line breaks and folding come from `IcalOptions`, never from
`Environment.NewLine`, so the output is the same on every operating system:

- **`LineBreak`:** CRLF (`"\r\n"`) by default, as RFC 5545 requires. `"\n"` is the
  only other value accepted.
- **`FoldLength`:** 75 octets of UTF-8 by default. A fold never splits a character.
  The minimum is 4.
- **`FoldWhitespace`:** a space by default, or a horizontal tab.

```csharp
var options = new IcalOptions { LineBreak = "\n", FoldLength = 60 };
using var writer = new IcalWriter("calendar.ics", options);
```

On .NET 5 and later, `IcalOptions` is a record with `init` properties, so you can
also use `with` to copy one. On older targets it is a class with `set` properties.

### Flush and dispose

- **`Flush`:** flushes the underlying text writer, whether the writer created it or
  you supplied it.
- **`Dispose`:** flushes and closes the file or stream writer that the writer
  created itself. A `Stream` you supplied is left open. A `TextWriter` you supplied
  is neither flushed nor closed, so flush it yourself.

### Report progress

`WriteCalendars` takes an optional `IProgress<IcalWriteProgress>`. It is called
synchronously, on the writing thread, after each calendar has been handed to the
underlying text writer. The counts are totals for the writer, not for the call.

```csharp
using var writer = new IcalWriter("calendar.ics");
writer.WriteCalendars(calendars, new ConsoleProgress());

sealed class ConsoleProgress : IProgress<IcalWriteProgress>
{
    public void Report(IcalWriteProgress value) => Console.WriteLine(value);
}
```

This prints, for example,
`LinesWritten = 12, ComponentsWritten = 1, CalendarsWritten = 1`.

## Build a calendar in code

Create the components and add them to `Components`:

```csharp
var calendar = new IcalCalendar { ProductId = "-//Example Corp//Calendar 1.0//EN" };
var review = new IcalEvent
{
    Uid = "20261015T090000Z-1@example.com",
    DtStamp = new IcalDateTime(new IcalDate(2026, 10, 15), new IcalTime(9, 0, 0, isUtc: true)),
    DtStart = new IcalDateTime(new IcalDate(2026, 11, 2), new IcalTime(14, 0, 0, isUtc: true)),
    Duration = IcalDuration.Parse("PT1H", IcalValueFormat.Text),
    Summary = "Project review"
};
review.Properties.Add(new IcalContentLine("X-EXAMPLE-ROOM", "4.12"));
calendar.Components.Add(review);

using var writer = new IcalWriter("review.ics");
writer.WriteCalendar(calendar);
```

`new IcalCalendar()` starts with a `VERSION:2.0` property. The file holds:

```text
BEGIN:VCALENDAR
VERSION:2.0
PRODID:-//Example Corp//Calendar 1.0//EN
BEGIN:VEVENT
UID:20261015T090000Z-1@example.com
DTSTAMP:20261015T090000Z
DTSTART:20261102T140000Z
DURATION:PT1H
SUMMARY:Project review
X-EXAMPLE-ROOM:4.12
END:VEVENT
END:VCALENDAR
```

### The writer refuses a calendar that breaks RFC 5545

Before it writes a calendar, the writer checks it as the reader does. If the
calendar breaks a rule that the options don't accept, the writer throws
`IcalFileException` and writes nothing of that calendar. For example, a VEVENT needs
a `UID` and a `DTSTAMP`:

```csharp
var calendar = new IcalCalendar { ProductId = "-//Example Corp//Calendar 1.0//EN" };
calendar.Components.Add(new IcalEvent { Summary = "No UID or DTSTAMP" });

using var writer = new IcalWriter(new StringWriter());
try
{
    writer.WriteCalendar(calendar);
}
catch (IcalFileException exception)
{
    // The VEVENT component has no DTSTAMP property, which it requires.
    Console.WriteLine(exception.Message);
}
```

To see every problem at once, before writing, call `Validate()`. It returns a list
of `IcalDiagnostic` and throws nothing:

```csharp
foreach (var diagnostic in calendar.Validate())
{
    Console.WriteLine(diagnostic);
}
```

```text
VCALENDAR/VEVENT[1]: The VEVENT component has no DTSTAMP property, which it requires.
VCALENDAR/VEVENT[1]: The VEVENT component has no UID property, which it requires.
VCALENDAR/VEVENT[1]: The VEVENT component has no DTSTART property, which it requires when the calendar has no METHOD property.
```

A content line can also fail when it is written, for example if a value holds a
character that isn't allowed. Then the lines before it have already been written.

## Counters

The reader and writer count what they have handled so far:

| Reader | Writer |
| --- | --- |
| `CalendarsRead`, including any that failed a check | `CalendarsWritten` |
| `ComponentsRead`, at any depth, not counting VCALENDAR | `ComponentsWritten`, at any depth, not counting VCALENDAR |
| `LinesRead`, logical (unfolded) lines | `LinesWritten`, logical (unfolded) lines |
| `PhysicalLinesRead`; a folded line spans several | |

```csharp
using var reader = new IcalReader("calendar.ics");
var calendars = reader.ReadCalendars().ToList();
Console.WriteLine($"{reader.LinesRead} lines ({reader.PhysicalLinesRead} physical)");
```

## Errors

Problems with the data throw `IcalFileException`. Its `Data` holds where the
problem was found, under the keys in `IcalFileException.DataItemName`:

| Key | Holds |
| --- | --- |
| `LineNumber` | The 1-based number of the logical line, as a `long`. |
| `PhysicalLineNumber` | The 1-based number of the physical line, for a problem in the content-line syntax. |
| `PropertyName` | The name of the property, once it has been read. |
| `ComponentPath` | The component, such as `VCALENDAR/VEVENT[3]`, for a conformance problem. |
| `DiagnosticKind` | The `IcalDiagnosticKind` of a conformance problem. |

What the reader can do after an error depends on the error:

- **A calendar breaks an RFC 5545 rule:** the calendar has been read in full, so
  reading can go on with the next calendar. The exception's `Data` has a
  `DiagnosticKind`.
- **Anything else:** for example a `BEGIN` without a matching `END`, a bare line
  feed, or a cancellation. The reader can't tell where the next calendar starts, so
  later reads throw `InvalidOperationException`.

```csharp
using var reader = new IcalReader("calendars.ics");
while (true)
{
    IcalCalendar? calendar;
    try
    {
        calendar = reader.ReadCalendar();
    }
    catch (IcalFileException exception)
        when (exception.Data.Contains(IcalFileException.DataItemName.DiagnosticKind))
    {
        Console.WriteLine($"Skipped: {exception.Message}");
        continue;
    }

    if (calendar is null) break;
    Console.WriteLine($"Read {calendar.Events.Count()} events");
}
```

To accept data that breaks a rule rather than skip it, see
[Read non-compliant feeds](read-non-compliant-feeds.md).

Only one read or write may be in progress at a time on a reader or writer. Starting
another throws `InvalidOperationException`.

## Related

- [Read asynchronously](read-asynchronously.md)
- [Read with zero copies](read-zero-copy.md)
- [Expand recurrences](expand-recurrences.md)
- [Read non-compliant feeds](read-non-compliant-feeds.md)
- [Convert between .ics, jCal and xCal](convert-formats.md)
- [The layered model](../concepts/layered-model.md)
- [Standards compliance by default](../concepts/standards-compliance.md)
- [IcalReader](../reference/IcalReader.md), [IcalWriter](../reference/IcalWriter.md),
  [IcalOptions](../reference/IcalOptions.md) and
  [IcalFileException](../reference/IcalFileException.md)
