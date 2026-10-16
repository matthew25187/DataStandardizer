---
title: The layered model
parent: File.iCal
grand_parent: Packages
nav_order: 10
---

# The layered model

The package is built in four layers. Each layer can be used on its own, so you
can stop at the lowest layer that does the job. The lower layers are fast and
avoid allocation. The higher layers are easier to use and allocate by design.

All public types here are in the `DataStandardizer.File.iCal` namespace.

## The four layers

```text
             .ics                      jCal (JSON)          xCal (XML)
              │                            │                    │
Layer 1   IcalContentLineReader            │                    │
          IcalContentLineWriter            │                    │
          IcalContentLineView              │                    │
          IcalContentLine                  │                    │
              │                            │                    │
Layer 2   value types: IcalDateTime, IcalDuration, IcalRecurrenceRule, ...
          (IcalValueFormat.Text)  (IcalValueFormat.Json)  (IcalValueFormat.Xml)
              │                            │                    │
Layer 3   IcalReader / IcalWriter     JcalReader / JcalWriter   XcalReader / XcalWriter
              │                            │                    │
              └───────────► IcalCalendar, IcalEvent, ... ◄──────┘
                                     │
Layer 4   IcalRecurrenceRule.GetOccurrences          local time, no time zones
          IcalEvent / IcalTodo / IcalJournal         instances resolved to UTC
            .GetOccurrences
```

jCal and xCal sit beside Layer 1. They have no content lines, so `JcalReader`,
`JcalWriter`, `XcalReader` and `XcalWriter` map their documents straight to and
from the Layer 3 model. A calendar read from any of the three formats can be
written in any of the others.

## Layer 1: content lines

An iCalendar file is a sequence of content lines (RFC 5545 §3.1). Each line has
a name, any parameters and a value. Long lines are folded over several physical
lines.

- `IcalContentLineReader` reads logical (unfolded) lines, forward only. It checks
  each line's structure against RFC 5545 §3.1 and decodes the RFC 6868 caret
  encoding of parameter values. It doesn't check what properties and values
  mean. TEXT unescaping depends on the value type, so it is done only when you
  ask for it.
- `IcalContentLineWriter` writes lines. It formats each line into a pooled
  buffer, caret encodes parameter values and quotes those that need it, and
  folds the line.

There are two ways to read a line:

- **Zero-copy:** `ReadContentLine()` moves to the next line and returns `false`
  at the end. `CurrentLine` then gives an `IcalContentLineView`, a
  `readonly struct` whose name, parameters and value are spans over the reader's
  buffer. A view is valid only until the reader reads another line. Its members
  throw after that, but a span you took out of it can't be checked, so copy
  anything you need to keep.
- **Allocated:** `ReadLine()` and `ReadLines()` return `IcalContentLine` objects,
  which keep their data. `IcalContentLineView.ToContentLine()` makes one from a
  view.

## Layer 2: value types

Each value type of RFC 5545 §3.3 has a type of its own, such as `IcalDateTime`,
`IcalDuration`, `IcalPeriod`, `IcalUtcOffset` and `IcalRecurrenceRule`. The value
types that map to .NET types (BINARY, BOOLEAN, CAL-ADDRESS, FLOAT, INTEGER, TEXT
and URI) are parsed and formatted by the static `IcalValue` class.

- **Format-neutral:** a value type holds structured data, not text. The same
  value can be parsed from and formatted to the `.ics`, jCal or xCal form, chosen
  with `IcalValueFormat.Text`, `Json` or `Xml`.
- **Span parsing:** the value types parse from a `ReadOnlySpan<char>` with
  `TryParse` and format into a `Span<char>` with `TryFormat`. `IcalValue` has
  the same pairs for each type it handles, such as `TryParseInteger` and
  `TryFormatInteger`. Parsing a date-time, a recurrence rule and the
  other span-based values allocates nothing on the heap; the package's tests
  check this.
- **No time-zone lookup:** a zoned `IcalDateTime` carries its `TZID` as a string.
  Turning it into an instant is the job of a time-zone resolver. See
  [Time-zone resolution](time-zone-resolution.md).

Layers 1 and 2 together give a zero-copy path. This loop finds each `DTSTART`
and parses it without building any objects for the other lines:

```csharp
using var reader = new IcalContentLineReader("calendar.ics");
while (reader.ReadContentLine())
{
    var line = reader.CurrentLine;
    if (!line.NameEquals("DTSTART")) continue;

    var tzidIndex = line.IndexOfParameter("TZID");
    var tzid = tzidIndex >= 0 ? line.GetParameterValue(tzidIndex, 0).ToString() : null;
    if (IcalDateTime.TryParse(line.RawValue, tzid, IcalValueFormat.Text, out var start))
    {
        Console.WriteLine($"{start} ({start.Kind})");
    }
}
```

## Layer 3: the component object model

`IcalReader` reads `.ics` data into `IcalCalendar` objects, one calendar at a
time, and `IcalWriter` writes them back. Both are built on the Layer 1 reader
and writer.

`IcalComponent` is a name, an ordered list of properties and the components
nested in it. Each property is held as an `IcalContentLine`, so its parameters
stay reachable, and unknown and `X-` properties are written back as they were
read. The derived types add typed accessors for the components RFC 5545 and
RFC 7986 define:

| Type | Component |
| --- | --- |
| `IcalCalendar` | VCALENDAR |
| `IcalEvent` | VEVENT |
| `IcalTodo` | VTODO |
| `IcalJournal` | VJOURNAL |
| `IcalFreeBusy` | VFREEBUSY |
| `IcalTimeZone` | VTIMEZONE |
| `IcalTimeZoneObservance` | STANDARD and DAYLIGHT |
| `IcalAlarm` | VALARM |

A component the reader doesn't recognise, such as an `X-` component, is read as a
plain `IcalComponent`.

The object model allocates by design: every component, property and parameter
is an object. Zero-copy ends at Layer 2.

```csharp
using var reader = new IcalReader("calendar.ics");
foreach (var calendar in reader.ReadCalendars())
{
    foreach (var calendarEvent in calendar.Events)
    {
        Console.WriteLine($"{calendarEvent.DtStart}: {calendarEvent.Summary}");
    }
}
```

The reader checks each calendar against RFC 5545, and the writer checks it again
before writing. See [Standards compliance by default](standards-compliance.md).

## Layer 4: recurrence expansion

Expansion comes in two parts:

- **The rule engine:** `IcalRecurrenceRule.GetOccurrences` generates the
  occurrences of one `RRULE` from a start, in local wall-clock time. It is part
  of a Layer 2 value type and needs no object model and no time zones.
- **Component expansion:** `GetOccurrences` on `IcalEvent`, `IcalTodo` and
  `IcalJournal` builds the full recurrence set from `DTSTART`, `RRULE`, `RDATE`,
  `EXDATE` and `RECURRENCE-ID` overrides. It resolves each instance to UTC with a
  time-zone resolver and returns `IcalOccurrence<T>` objects.

Both are lazy and synchronous: occurrences are generated as you enumerate them.
See [Expand recurrences](../how-to/expand-recurrences.md) and
[DST gaps and overlaps](dst-rules.md).

## Which layer to use

| You want to | Use |
| --- | --- |
| Scan a large file for a few properties, or filter and copy lines, with as little allocation as possible | Layer 1, with `ReadContentLine` and `CurrentLine` |
| Parse or format individual values, such as a date-time or a duration, from your own source | Layer 2 |
| Work with events, to-dos and the rest as objects, or convert between `.ics`, jCal and xCal | Layer 3 |
| Repair or pass through data that breaks the RFCs, line by line | Layer 1, which checks only the line structure |
| List the dates of a recurrence rule, without time zones | Layer 4, `IcalRecurrenceRule.GetOccurrences` |
| List the instances of an event in UTC | Layer 4, `IcalEvent.GetOccurrences` |

## Related

- [Read and write `.ics` files](../how-to/read-write-ics-files.md)
- [Read with zero copy](../how-to/read-zero-copy.md)
- [Expand recurrences](../how-to/expand-recurrences.md)
- [Convert between formats](../how-to/convert-formats.md)
- [Standards compliance by default](standards-compliance.md)
- [API reference](../reference/index.md)
