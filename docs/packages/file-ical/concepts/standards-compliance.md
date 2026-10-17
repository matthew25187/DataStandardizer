---
title: Standards compliance by default
parent: File.iCal
grand_parent: Packages
nav_order: 11
---

# Standards compliance by default

Every default in the package follows RFC 5545, RFC 6868 and RFC 7986. Readers
reject data that breaks a MUST or MUST NOT rule, and writers refuse to write a
model that breaks one. Real-world calendars often break the rules, so each kind
of accommodation is a separate option that you turn on by name.

All public types here are in the `DataStandardizer.File.iCal` namespace.

## What the defaults mean

With a default `IcalOptions`:

- **Readers reject non-compliant input.** `IcalReader`, `XcalReader` and
  `JcalReader` check each calendar against RFC 5545 when they have read it. The
  first problem throws `IcalFileException`, and reading can then continue with
  the next calendar.
- **Writers refuse non-compliant models.** `IcalWriter`, `XcalWriter` and
  `JcalWriter` check each calendar before they write it. A calendar that breaks a
  rule throws `IcalFileException`, and nothing of that calendar is written.
- **Line breaks are CRLF**, as RFC 5545 §3.1 requires, on every operating system.

For example, a calendar whose `DTSTART;TZID=America/New_York:...` has no
VTIMEZONE for `America/New_York` is rejected by the reader:

```text
The TZID parameter of the DTSTART property names the time zone 'America/New_York',
which has no VTIMEZONE in the calendar.
```

and an `IcalWriter` refuses to write the same calendar with the same message.

The exception's `Data` holds where the problem is, under the keys in
`IcalFileException.DataItemName`: `ComponentPath`, such as
`VCALENDAR/VEVENT[1]`, `DiagnosticKind`, and, where they apply, `LineNumber` and
`PropertyName`.

## One option for each accommodation

Each accommodation for common non-compliant data is a separate property of
`IcalOptions`. All of them default to `false`. There is no single "lenient
mode" switch, so you accept only the problems you expect and still catch the
rest.

| Option | What it accepts | Rule it relaxes |
| --- | --- | --- |
| `AcceptBareLineFeed` | A bare line feed as a line break, both to end a line and in a fold. A bare carriage return is never a line break. | RFC 5545 §3.1: lines end with CRLF. |
| `AllowMissingRequiredProperties` | A missing required property or component, such as the `UID` or `DTSTAMP` of a VEVENT, or the STANDARD or DAYLIGHT parts of a VTIMEZONE. | The "MUST include" rules of each component. |
| `AllowCardinalityViolations` | A property that may appear once appearing more than once, or two properties that exclude each other both appearing, such as `DTEND` and `DURATION` in a VEVENT. A typed accessor reads the first. | The "MUST NOT occur more than once" rules. |
| `AllowUndefinedTimeZones` | A `TZID` parameter that names a time zone with no VTIMEZONE in the calendar. | RFC 5545 §3.2.19: a VTIMEZONE MUST be specified for each `TZID`. |
| `AllowInvalidValues` | A value that doesn't follow the syntax of its value type, or breaks a rule that ties it to its property. The value is kept as written, and a typed accessor such as `IcalEvent.DtStart` throws `IcalFileException` when it reads it. | RFC 5545 §3.3 and the value rules of each property. |

The same options apply to reading and writing:

```csharp
var options = new IcalOptions
{
    AcceptBareLineFeed = true,
    AllowUndefinedTimeZones = true
};

using var reader = new IcalReader("feed.ics", options);
```

`AllowUndefinedTimeZones` lets you read such a calendar, but its times still
need a time zone. The default resolver reads only the calendar's own
VTIMEZONE components, so add a resolver that fills in the rest. See
[Time-zone resolution](time-zone-resolution.md) and
[Read non-compliant feeds](../how-to/read-non-compliant-feeds.md).

## What is never accepted

Two kinds of problem have no option:

- **Component nesting:** a component inside one where RFC 5545 doesn't allow it,
  such as a VEVENT inside a VTODO.
- **vCalendar 1.0:** only iCalendar 2.0 is read. A `VERSION` that doesn't
  include 2.0, such as `VERSION:1.0`, throws as soon as it is read:

  ```text
  The calendar has VERSION:1.0, which does not include iCalendar 2.0, the only
  version RFC 5545 defines.  vCalendar 1.0 (VERSION:1.0) is not supported.
  ```

  vCalendar 1.0 differs in more than its version: it has quoted-printable
  encoding, a different RRULE grammar, and `DALARM`, `AALARM`, `TZ` and
  `DAYLIGHT` properties. To get at the lines of such data, use
  `IcalContentLineReader`, which doesn't check `VERSION`.

`IcalWriter` always writes `VERSION:2.0`, in place of the calendar's own
`VERSION`, so it never refuses a calendar for lacking one.

## Line breaks and folding

Line breaks never come from `Environment.NewLine`. A file written on Windows,
Linux or macOS is the same. The options that control them are:

| Option | Default | Meaning |
| --- | --- | --- |
| `LineBreak` | `"\r\n"` | The line break a writer ends each physical line with, including the lines of a fold. RFC 5545 §3.1 requires CRLF. `"\n"` may be chosen for consumers that expect it. No other value is accepted. |
| `FoldLength` | 75 | The length at which a writer folds lines, in octets of UTF-8, not counting the line break. RFC 5545 §3.1 says lines SHOULD NOT be longer than 75 octets. A fold never splits a character. The minimum is 4. |
| `FoldWhitespace` | `' '` | The character at the start of each continuation line: a space or a horizontal tab, the two that RFC 5545 §3.1 allows. A reader accepts both. |
| `AcceptBareLineFeed` | `false` | Whether a reader accepts a bare line feed. See above. |

`LineBreak`, `FoldLength` and `FoldWhitespace` are ignored by readers.

The other options aren't accommodations. `Encoding` defaults to UTF-8, the
default charset of RFC 5545 §3.1.4. `MaxLineLength` defaults to no limit,
because RFC 5545 sets none; set one when you read untrusted data.
`MaxRecurrenceIterations` stops a recurrence rule that can never match.

## Checking a model with `Validate`

`IcalComponent.Validate()` runs the same checks the readers and writers make,
and returns every problem found as a list of `IcalDiagnostic`, without
throwing. Use it on a model built in code, or one read with accommodations on:

```csharp
foreach (var diagnostic in calendar.Validate())
{
    Console.WriteLine($"{diagnostic.Kind}: {diagnostic}");
}
```

```text
UndefinedTimeZone: VCALENDAR/VEVENT[1] (line 7): The TZID parameter of the DTSTART
property names the time zone 'America/New_York', which has no VTIMEZONE in the calendar.
```

Each diagnostic has a `Kind`, a `Message`, a `ComponentPath`, the `PropertyName`
at fault if there is one, and the `LineNumber` it was found on, or 0 for a model
not read from a source. Its `Kind` says which option, if any, accepts it:

| `IcalDiagnosticKind` | Accepted with |
| --- | --- |
| `MissingRequiredProperty` | `AllowMissingRequiredProperties` |
| `CardinalityViolation` | `AllowCardinalityViolations` |
| `UndefinedTimeZone` | `AllowUndefinedTimeZones` |
| `InvalidValue` | `AllowInvalidValues` |
| `InvalidComponentNesting` | Never accepted |
| `UnsupportedVersion` | Never accepted |

Whether a `TZID` names a VTIMEZONE, and whether a VEVENT needs a `DTSTART`,
depend on the calendar, so they are checked only when you validate an
`IcalCalendar`.

## What the checks cover

The checks cover the rules of RFC 5545 and RFC 7986 for each component. In
summary:

- **Required properties and components**, such as `UID` and `DTSTAMP`, at least
  one component in a VCALENDAR, and `DTSTART` in a VEVENT when the calendar has
  no `METHOD`. Pairs that must appear together are checked too, such as
  `DURATION` and `REPEAT` in a VALARM.
- **Cardinality:** properties that may appear at most once, or exactly once, and
  pairs that exclude each other.
- **Value syntax:** each value against its value type (RFC 5545 §3.3), including
  a `VALUE` parameter the property doesn't allow or one it requires, and
  `ENCODING=BASE64` on inline BINARY values.
- **Values tied to their property:** date-times that must be in UTC; a `DTEND`,
  `DUE` or `RECURRENCE-ID` with the same value type as `DTSTART`; a duration from
  a DATE given in days or weeks; recurrence rules checked against `DTSTART`, with
  a `UNTIL` in UTC inside a VTIMEZONE; an observance `DTSTART` given as local
  time; and a VTIMEZONE `TZID` that isn't empty.
- **Time zones:** each `TZID` parameter names a VTIMEZONE in the calendar.
- **Nesting** and **version**, as above.

Layer 1 doesn't make these checks. `IcalContentLineReader` checks only that each
line follows the content-line syntax of RFC 5545 §3.1, and its line breaks. See
[The layered model](layered-model.md).

## Related

- [Read non-compliant feeds](../how-to/read-non-compliant-feeds.md)
- [Time-zone resolution](time-zone-resolution.md)
- [The layered model](layered-model.md)
- [IcalOptions reference](../reference/IcalOptions.md)
- [API reference](../reference/index.md)
