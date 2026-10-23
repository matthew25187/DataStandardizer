---
title: Read non-compliant feeds
parent: File.iCal
grand_parent: Packages
nav_order: 6
---

# Read non-compliant feeds

By default, the readers reject data that breaks a MUST or MUST NOT rule of RFC 5545,
and the writers refuse to write it. Many real feeds break a rule or two. Each common
kind of problem has its own option on `IcalOptions` that accepts it. All of them are
off by default.

There is no single "lenient" switch. Turn on only the options your data needs, so
that other problems are still reported. See
[Standards compliance by default](../concepts/standards-compliance.md) for why.

## Find out what is wrong

Read the feed with the default options first. The `IcalFileException` names the
problem, and its `Data` says where it is:

```csharp
using DataStandardizer.File.iCal;

try
{
    using var reader = new IcalReader("feed.ics");
    var calendars = reader.ReadCalendars().ToList();
}
catch (IcalFileException exception)
{
    Console.WriteLine(exception.Message);
    foreach (System.Collections.DictionaryEntry item in exception.Data)
    {
        Console.WriteLine($"  {item.Key} = {item.Value}");
    }
}
```

For a broken RFC 5545 rule, `Data` holds a `DiagnosticKind`, an
`IcalDiagnosticKind` that names the option that accepts it:

| `IcalDiagnosticKind` | Accepted with |
| --- | --- |
| `MissingRequiredProperty` | `AllowMissingRequiredProperties` |
| `CardinalityViolation` | `AllowCardinalityViolations` |
| `UndefinedTimeZone` | `AllowUndefinedTimeZones` |
| `InvalidValue` | `AllowInvalidValues` |
| `InvalidComponentNesting` | Never accepted |
| `UnsupportedVersion` | Never accepted |

`Data` also holds the `ComponentPath`, such as `VCALENDAR/VEVENT[1]`, the
`LineNumber` and, where one property is at fault, the `PropertyName`. After such an
error, the reader can go on to the next calendar.

A problem in the line syntax itself, such as a bare line feed, has no
`DiagnosticKind`. Its `Data` holds the `LineNumber` and `PhysicalLineNumber`.

## The options

| Option | Reading | Writing | Formats |
| --- | --- | --- | --- |
| `AcceptBareLineFeed` | Yes | No (see `LineBreak`) | `.ics` |
| `AllowInvalidValues` | Yes | Yes | All |
| `AllowCardinalityViolations` | Yes | Yes | All |
| `AllowMissingRequiredProperties` | Yes | Yes | All |
| `AllowUndefinedTimeZones` | Yes | Yes | All |
| `MaxLineLength` | Yes | No | `.ics` |

An option that applies to writing works the same way there: without it, the writer
refuses a calendar with that problem and writes nothing of it. So if you read with
an option and then write what you read, give the writer the same option.

### AcceptBareLineFeed

RFC 5545 §3.1 requires a CRLF at the end of each line. Files written on Unix, or by
hand, often end their lines with a bare line feed (`\n`) instead. Without the
option:

```text
IcalFileException: Found a line feed without a carriage return.  RFC 5545 requires a CRLF line break.
    LineNumber = 1
    PhysicalLineNumber = 1
```

With `AcceptBareLineFeed`, a bare line feed ends a line, and also works in a fold.
A bare carriage return is never a line break.

After this error, the reader can't tell where the line ends, so it can't continue:
later reads throw `InvalidOperationException`.

This option only affects reading `.ics`. What a writer ends lines with is set by
`IcalOptions.LineBreak`, CRLF by default.

### AllowInvalidValues

A property value that doesn't follow the syntax of its value type, for example an
ISO 8601 date-time with hyphens and colons where iCalendar has none:

```text
DTSTART:2026-11-02T09:00:00Z
```

Without the option:

```text
IcalFileException: The value of the DTSTART property is not a valid DATE-TIME.
    ComponentPath = VCALENDAR/VEVENT[1]
    DiagnosticKind = InvalidValue
    LineNumber = 7
    PropertyName = DTSTART
```

The check covers the value types of RFC 5545 §3.3, and the rules that tie a value to
its property:

- A `VALUE` parameter that the property doesn't allow, or one it requires.
- A DATE-TIME that must be in UTC.
- Date-times that must have the same value type as the component's `DTSTART`.

With `AllowInvalidValues`, the value is kept as it was written. It is still there in
`GetProperty("DTSTART").Value`, but a typed accessor such as `IcalEvent.DtStart`
throws `IcalFileException` when it reads it.

When the xCal or jCal writer writes such a value, it writes it as an unknown type
with its `VALUE` parameter.

### AllowCardinalityViolations

A property that RFC 5545 allows at most once appears more than once, or two
properties that exclude each other both appear:

```text
SUMMARY:First
SUMMARY:Second
```

```text
IcalFileException: The VEVENT component has 2 SUMMARY properties; it allows at most one.
    DiagnosticKind = CardinalityViolation
```

```text
DTEND:20261102T100000Z
DURATION:PT1H
```

```text
IcalFileException: The VEVENT component has both a DTEND and a DURATION property, which exclude each other.
    DiagnosticKind = CardinalityViolation
```

With `AllowCardinalityViolations`, both properties are kept. A typed accessor reads
the first, so `Summary` is `First`. `GetProperties("SUMMARY")` gives both.

### AllowMissingRequiredProperties

A required property or component is missing, such as the `UID` or `DTSTAMP` of a
VEVENT, or the STANDARD or DAYLIGHT parts of a VTIMEZONE:

```text
BEGIN:VEVENT
UID:1@example.com
DTSTART:20261102T090000Z
END:VEVENT
```

```text
IcalFileException: The VEVENT component has no DTSTAMP property, which it requires.
    ComponentPath = VCALENDAR/VEVENT[1]
    DiagnosticKind = MissingRequiredProperty
    LineNumber = 4
    PropertyName = DTSTAMP
```

The `LineNumber` of a missing property is the line of the component's `BEGIN`.

A missing `VERSION` is covered too. The writer always writes `VERSION:2.0`, so it
never refuses a calendar for lacking one.

### AllowUndefinedTimeZones

RFC 5545 §3.2.19 requires a VTIMEZONE in the calendar for every `TZID` that is
used. Many feeds leave it out and name a zone from the TZ Database instead:

```text
DTSTART;TZID=Europe/Berlin:20261102T090000
```

Without the option:

```text
IcalFileException: The TZID parameter of the DTSTART property names the time zone 'Europe/Berlin', which has no VTIMEZONE in the calendar.
    DiagnosticKind = UndefinedTimeZone
```

With `AllowUndefinedTimeZones`, the calendar is read. But the default time-zone
resolver reads only the calendar's VTIMEZONE components, so expanding the event, or
converting its times to UTC, still fails. Pass a resolver chain that fills in the
missing zones from the TZ Database:

```csharp
var options = new IcalOptions { AllowUndefinedTimeZones = true };
var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData);

using var reader = new IcalReader("feed.ics", options);
foreach (var calendar in reader.ReadCalendars())
{
    foreach (var calendarEvent in calendar.Events)
    {
        if (calendarEvent.RecurrenceId is not null) continue;

        foreach (var occurrence in calendarEvent.GetOccurrences(calendar, null, null, resolver, options))
        {
            Console.WriteLine($"{occurrence.StartUtc}: {calendarEvent.Summary}");
        }
    }
}
```

`Calendar` comes first, so a VTIMEZONE the calendar does have still wins.
`TzData` then resolves names such as `Europe/Berlin`, and vendor-prefixed names such
as `/mozilla.org/20050126_1/America/New_York`. It doesn't know Windows names such as
`Eastern Standard Time`. For those, add `IcalTimeZoneResolver.System` at the end of
the chain (.NET Standard 2.0 and later), but note that its data depends on the
machine. See [Time-zone resolution](../concepts/time-zone-resolution.md).

To write such a calendar, give the writer `AllowUndefinedTimeZones` too. To make it
compliant instead, add a VTIMEZONE for each zone, which `IcalTimeZone.FromTzData`
can generate.

## MaxLineLength

`MaxLineLength` isn't an accommodation but a limit. RFC 5545 sets no maximum line
length, so the default is `null`, meaning no limit. The reader keeps each unfolded
line in memory, so when you read data you don't trust, set a limit:

```csharp
var options = new IcalOptions { MaxLineLength = 100_000 };
```

A longer line throws `IcalFileException`:

```text
IcalFileException: The content line is longer than the maximum of 100 characters.
    LineNumber = 8
    PhysicalLineNumber = 8
```

The length is in characters of the unfolded line, not counting its line break. The
value must be at least 1. As with a bare line feed, the reader can't continue after
this error.

## Problems no option accepts

- **`InvalidComponentNesting`:** a component inside one where RFC 5545 doesn't allow
  it, such as a VTODO inside a VEVENT.
- **`UnsupportedVersion`:** a `VERSION` that doesn't include 2.0, such as the
  `VERSION:1.0` of vCalendar 1.0. This is reported as soon as the `VERSION` line is
  read.
- **Broken structure:** a `BEGIN` without a matching `END`, or a line that doesn't
  follow the content-line syntax.

## Combine options

Turn on each option you need:

```csharp
var options = new IcalOptions
{
    AcceptBareLineFeed = true,
    AllowMissingRequiredProperties = true,
    AllowUndefinedTimeZones = true,
    MaxLineLength = 100_000
};
```

On .NET 5 and later, `IcalOptions` is a record, so you can derive one set of
options from another with `with`. On older targets, set the properties.

## Check a model with Validate

`Validate()` on any component checks it, and the components in it, against the same
rules the readers and writers enforce. It returns every problem as an
`IcalDiagnostic`, whatever the options, and throws nothing. Use it to see what a
calendar read with accommodations still breaks, or to check a calendar you built in
code before you write it:

```csharp
var options = new IcalOptions { AllowMissingRequiredProperties = true, AllowUndefinedTimeZones = true };
using var reader = new IcalReader("feed.ics", options);
foreach (var calendar in reader.ReadCalendars())
{
    foreach (var diagnostic in calendar.Validate())
    {
        Console.WriteLine($"{diagnostic.Kind} at {diagnostic.ComponentPath}, line {diagnostic.LineNumber}: " +
            diagnostic.Message);
    }
}
```

```text
MissingRequiredProperty at VCALENDAR/VEVENT[1], line 4: The VEVENT component has no DTSTAMP property, which it requires.
UndefinedTimeZone at VCALENDAR/VEVENT[1], line 6: The TZID parameter of the DTSTART property names the time zone 'Europe/Berlin', which has no VTIMEZONE in the calendar.
```

Each `IcalDiagnostic` has:

| Member | Holds |
| --- | --- |
| `Kind` | The `IcalDiagnosticKind`. |
| `Message` | A description of the problem. |
| `ComponentPath` | The component, starting with the one validated, such as `VCALENDAR/VEVENT[3]/VALARM[1]`. Each index is 1-based among components of the same name. |
| `PropertyName` | The property at fault, or `null` if the problem isn't with one property. |
| `LineNumber` | The line of the property, or of the component's `BEGIN`; 0 if the model wasn't read from a source. |

The diagnostics come in the order of the lines they were found on. Whether a `TZID`
has a VTIMEZONE, and whether a VEVENT needs a `DTSTART`, depend on the calendar, so
those are checked only when you call `Validate()` on an `IcalCalendar`.

## Related

- [Standards compliance by default](../concepts/standards-compliance.md)
- [Time-zone resolution](../concepts/time-zone-resolution.md)
- [Expand recurrences](expand-recurrences.md)
- [Read and write .ics files](read-write-ics-files.md)
- [IcalOptions](../reference/IcalOptions.md),
  [IcalFileException](../reference/IcalFileException.md),
  [IcalDiagnostic](../reference/IcalDiagnostic.md) and
  [IcalDiagnosticKind](../reference/IcalDiagnosticKind.md)
