---
title: IcalOptions Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalOptions Class

## Definition

Namespace: `DataStandardizer.File.iCal`

Options to configure the behaviour of an iCalendar reader or writer, and of
recurrence expansion. The defaults follow RFC 5545. Each accommodation for common
non-compliant input is a separate option, off by default. Pass an instance to a reader
or writer constructor, or to a `GetOccurrences` overload.

**.NET:**

```csharp
public sealed record IcalOptions
```

**.NET Standard:**

```csharp
public sealed class IcalOptions
```

## Remarks

On .NET 5 and later the type is a `record` whose properties are `init`-only, so
configure it with an object initializer or a `with` expression. On .NET Standard the
type is a plain `class` whose properties are settable. Construct it with the implicit
parameterless constructor and set the properties you need; the rest keep their
defaults.

Options are checked when they are used, not when they are set. A reader or writer
checks the options it uses in its constructor and throws `ArgumentException` for a
value it does not accept. Recurrence expansion checks `MaxRecurrenceIterations` when it
is called.

**Where each option applies.**

- **Reading .ics** ([IcalContentLineReader](IcalContentLineReader.md),
  [IcalReader](IcalReader.md)): `AcceptBareLineFeed`, `MaxLineLength`, `Encoding`.
- **Writing .ics** ([IcalContentLineWriter](IcalContentLineWriter.md),
  [IcalWriter](IcalWriter.md)): `FoldLength`, `FoldWhitespace`, `LineBreak`, `Encoding`.
- **Conformance, reading and writing** ([IcalReader](IcalReader.md),
  [IcalWriter](IcalWriter.md), [XcalReader](XcalReader.md), [XcalWriter](XcalWriter.md),
  [JcalReader](JcalReader.md), [JcalWriter](JcalWriter.md)): `AllowInvalidValues`,
  `AllowCardinalityViolations`, `AllowMissingRequiredProperties`,
  `AllowUndefinedTimeZones`. The content line reader and writer do not check
  conformance, so these do not affect them.
- **xCal**: `Encoding` for reading and writing, and `LineBreak` for writing.
- **jCal**: `Encoding`, which must be `null` or UTF-8.
- **Recurrence expansion** (`IcalRecurrenceRule.GetOccurrences` and the
  `GetOccurrences` overloads of [IcalEvent](IcalEvent.md), [IcalTodo](IcalTodo.md) and
  [IcalJournal](IcalJournal.md) that take options): `MaxRecurrenceIterations`.

Any option not listed for a reader, writer or method is ignored by it.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalOptions()` | Implicit parameterless constructor; all properties take their defaults. |

## Properties

On .NET Standard each property is `get; set;` instead of `get; init;`.

| Property | Signature | Default | Applies to | Notes |
| --- | --- | --- | --- | --- |
| `AcceptBareLineFeed` | `bool AcceptBareLineFeed { get; init; }` | `false` | Reading .ics | Accept a bare line feed as a line break, both to end a content line and in a fold. RFC 5545 §3.1 requires CRLF; when `false`, a bare line feed throws `IcalFileException` and the reader cannot continue. A bare carriage return is never a line break. |
| `AllowCardinalityViolations` | `bool AllowCardinalityViolations { get; init; }` | `false` | Reading and writing | Accept a property that appears more often than RFC 5545 allows, or two properties that exclude each other, such as `DTEND` and `DURATION` in a VEVENT. When `false`, a reader rejects such a calendar and a writer refuses to write it, with `IcalFileException`. A typed accessor reads the first of the properties. |
| `AllowInvalidValues` | `bool AllowInvalidValues { get; init; }` | `false` | Reading and writing | Accept a property value that does not follow the syntax of its value type (RFC 5545 §3.3), or that breaks a rule tying it to its property: a `VALUE` parameter the property does not allow, or one it requires; a DATE-TIME that must be in UTC; date-times that must have the same value type as `DTSTART`. When `false`, a reader rejects the calendar and a writer refuses to write it, with `IcalFileException`. When `true`, the value is kept as written, and a typed accessor such as `IcalEvent.DtStart` throws `IcalFileException` when it reads it. |
| `AllowMissingRequiredProperties` | `bool AllowMissingRequiredProperties { get; init; }` | `false` | Reading and writing | Accept a missing required property or component, such as the `UID` or `DTSTAMP` of a VEVENT, or the STANDARD or DAYLIGHT parts of a VTIMEZONE. A missing `VERSION` is covered too, but `IcalWriter` always writes `VERSION:2.0`, so it never refuses a calendar for lacking one. When `false`, a reader rejects such a calendar and a writer refuses to write it, with `IcalFileException`. |
| `AllowUndefinedTimeZones` | `bool AllowUndefinedTimeZones { get; init; }` | `false` | Reading and writing | Accept a `TZID` parameter that names a time zone the calendar has no VTIMEZONE for. RFC 5545 §3.2.19 requires one. When `false`, a reader rejects such a calendar and a writer refuses to write it, with `IcalFileException`. |
| `Encoding` | `Encoding? Encoding { get; init; }` | `null` | Reading and writing | The encoding of the data. Applies only when the reader or writer is created from a `Stream` or file path; with a `TextReader`, `TextWriter` or `XmlReader` it must be `null`, or the constructor throws `ArgumentException`. When `null`, a .ics reader uses UTF-8 (the default charset of RFC 5545 §3.1.4), letting a byte order mark take precedence, and a .ics or xCal writer writes UTF-8 without a byte order mark. An xCal reader with `null` lets the XML reader detect the encoding. jCal accepts only `null` or UTF-8, and throws `ArgumentException` for anything else. |
| `FoldLength` | `int FoldLength { get; init; }` | `75` | Writing .ics | The length, in octets of the UTF-8 encoding and not counting the line break, at which a writer folds content lines. 75 is the length RFC 5545 §3.1 recommends. A fold never splits a character. The minimum is 4, the longest UTF-8 sequence; a continuation line that starts with a 4-octet character is then one octet longer. A smaller value makes the writer constructor throw `ArgumentException`. |
| `FoldWhitespace` | `char FoldWhitespace { get; init; }` | `' '` | Writing .ics | The white space at the start of each continuation line of a folded line: a space or a horizontal tab, the two RFC 5545 §3.1 allows. Any other character makes the writer constructor throw `ArgumentException`. A reader accepts both regardless. |
| `LineBreak` | `string LineBreak { get; init; }` | `"\r\n"` | Writing .ics and xCal | The line break a writer ends each physical line with, including the lines of a fold. RFC 5545 §3.1 requires CRLF. `"\n"` may be chosen for consumers that expect it. Any other value makes the writer constructor throw `ArgumentException`. Never depends on `Environment.NewLine`. For reading, see `AcceptBareLineFeed`. |
| `MaxLineLength` | `int? MaxLineLength { get; init; }` | `null` | Reading .ics | The maximum length, in characters and not counting the line break, of a logical (unfolded) content line. `null` means no limit, since RFC 5545 sets none. The reader keeps each logical line in memory, so set a limit when reading untrusted data. A longer line throws `IcalFileException` and the reader cannot continue. A value less than 1 makes the reader constructor throw `ArgumentException`. |
| `MaxRecurrenceIterations` | `int MaxRecurrenceIterations { get; init; }` | `100000` | Recurrence expansion | The number of consecutive periods a recurrence rule may step through without producing an occurrence before expansion gives up with `InvalidOperationException`. A period is one step of the rule's frequency, such as a year for `FREQ=YEARLY`; a run of periods the rule skips as a whole counts as one. This stops a rule that can never match, such as `FREQ=SECONDLY;BYMONTH=2;BYMONTHDAY=30`, from running for a very long time. The default is far more than any rule that can match needs. A value less than 1 makes the call throw `ArgumentException`. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
`IcalOptions` is a `record` with `init`-only properties on the `net8.0` and `net10.0`
builds, and a plain `class` with settable properties on the `netstandard1.1`,
`netstandard2.0`, and `netstandard2.1` builds. The nullable annotation on `Encoding`
applies to the `netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Read non-compliant feeds](../how-to/read-non-compliant-feeds.md)
- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [Expand recurrences](../how-to/expand-recurrences.md)
- [Standards compliance](../concepts/standards-compliance.md)
- [IcalReader](IcalReader.md)
- [IcalWriter](IcalWriter.md)
- [IcalDiagnosticKind](IcalDiagnosticKind.md)
- [File.iCal API reference](index.md)
