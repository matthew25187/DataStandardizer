---
title: IcalReader Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalReader Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A reader of iCalendar data (RFC 5545) into the component object model. Each call to
`ReadCalendar` returns the next `VCALENDAR` as an [IcalCalendar](IcalCalendar.md), with
its components and properties.

**.NET:**

```csharp
public sealed class IcalReader : IDisposable, IAsyncDisposable
```

**.NET Standard 1.1:**

```csharp
public sealed class IcalReader : IDisposable
```

## Remarks

The data is read with an [IcalContentLineReader](IcalContentLineReader.md), so the same
constructors, encodings, options and content line rules apply: CRLF line breaks
unless `IcalOptions.AcceptBareLineFeed` is set, unfolding, RFC 6868 caret decoding of
parameter values, and `IcalOptions.MaxLineLength`. A caller-supplied `Stream` or
`TextReader` is left open when the reader is disposed.

**Structure.** Each `BEGIN` line must be matched by an `END` line, and components must
be properly nested. `BEGIN` and `END` lines cannot have parameters, and a `BEGIN` must
name a component of letters, digits and hyphens. Names are compared
case-insensitively. A property outside a `VCALENDAR` throws `IcalFileException`, and
so does data that holds no `VCALENDAR` at all (RFC 5545 §3.4). Calendars are read one
at a time, so the whole data does not have to fit in memory.

**Typed components.** Components RFC 5545 defines become typed components:
[IcalEvent](IcalEvent.md), [IcalTodo](IcalTodo.md), [IcalJournal](IcalJournal.md),
[IcalFreeBusy](IcalFreeBusy.md), [IcalTimeZone](IcalTimeZone.md),
[IcalAlarm](IcalAlarm.md), and [IcalTimeZoneObservance](IcalTimeZoneObservance.md) for
`STANDARD` and `DAYLIGHT` inside a `VTIMEZONE`. Any other component, including
`STANDARD` or `DAYLIGHT` elsewhere, becomes an [IcalComponent](IcalComponent.md).
Unknown and `X-` properties are kept, in order, with their parameters. Each property
keeps its parameters exactly as read, so `IcalWriter` writes them back unchanged.

**Version.** Only iCalendar 2.0 is read. A `VERSION` property of the calendar that
does not include 2.0 (`2.0` or `minver;maxver` spanning 2.0), such as the
`VERSION:1.0` of vCalendar 1.0, throws `IcalFileException` as soon as it is read, with
`DiagnosticKind` set to `UnsupportedVersion` in its `Data` (RFC 5545 §3.7.4). To read
the content lines of such data, use `IcalContentLineReader`, which does not check
`VERSION`.

**Conformance.** Each calendar is checked once its `END:VCALENDAR` has been read, as
`IcalComponent.Validate` does. The first problem that the options do not accept throws
`IcalFileException`, and the calendar is discarded. Its `Data` holds `ComponentPath`
and `DiagnosticKind`, plus `LineNumber` and `PropertyName` where they apply. Reading
can then continue with the next calendar. The accommodations are
`IcalOptions.AllowMissingRequiredProperties`, `AllowCardinalityViolations`,
`AllowUndefinedTimeZones` and `AllowInvalidValues`, all off by default.
`InvalidComponentNesting` and `UnsupportedVersion` problems are never accepted. See
[IcalDiagnosticKind](IcalDiagnosticKind.md).

**Faults.** Any other exception from a read leaves the reader unable to continue, and
later reads throw `InvalidOperationException`. That includes a content line that is
not valid, a `BEGIN` or `END` that is not matched, an unsupported `VERSION`, an I/O
error, and a cancellation.

**Disposal.** `Dispose` and `DisposeAsync` may be called more than once and share
their state. They return the buffers and close a stream or file the reader opened
itself. After disposal, every member throws `ObjectDisposedException`. If a read is in
progress on another thread, it completes with `ObjectDisposedException`.

**Thread safety and async rules.** A single reader instance is not thread-safe. Only
one read may be in progress at a time. Synchronous and asynchronous reads may follow
one another, but a read started while another is in progress throws
`InvalidOperationException`. `ReadCalendarAsync` completes synchronously when the
buffers already hold the whole calendar.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalReader(Stream icalStream)` | Reads as UTF-8 unless the stream starts with a byte order mark. The stream is left open. Throws `ArgumentNullException` if `icalStream` is `null`, or `ArgumentException` if the stream cannot be read. |
| `IcalReader(Stream icalStream, IcalOptions options)` | Throws `ArgumentNullException` if `icalStream` or `options` is `null`, or `ArgumentException` if the stream cannot be read or `options.MaxLineLength` is less than 1. |
| `IcalReader(string icalFilePath)` | *(netstandard2.0+/.NET)* Opens the file for asynchronous, sequential reading, and reads it as UTF-8 unless it starts with a byte order mark. Throws `ArgumentNullException` if `icalFilePath` is `null`. File system errors come from `FileStream`. |
| `IcalReader(string icalFilePath, IcalOptions options)` | *(netstandard2.0+/.NET)* Throws `ArgumentNullException` if `icalFilePath` or `options` is `null`, or `ArgumentException` if `options.MaxLineLength` is less than 1. |
| `IcalReader(TextReader reader)` | The text reader is left open. Throws `ArgumentNullException` if `reader` is `null`. |
| `IcalReader(TextReader reader, IcalOptions options)` | Throws `ArgumentNullException` if `reader` or `options` is `null`, or `ArgumentException` if `options.Encoding` is set or `options.MaxLineLength` is less than 1. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CalendarsRead` | `long CalendarsRead { get; }` | The number of `VCALENDAR` objects read so far, including any that failed a conformance check. Throws `ObjectDisposedException` after `Dispose`. |
| `ComponentsRead` | `long ComponentsRead { get; }` | The number of components read so far, at any depth and not counting the `VCALENDAR` objects, including any in a calendar that failed a conformance check. Throws `ObjectDisposedException` after `Dispose`. |
| `LinesRead` | `long LinesRead { get; }` | The number of logical (unfolded) lines read so far. Throws `ObjectDisposedException` after `Dispose`. |
| `PhysicalLinesRead` | `long PhysicalLinesRead { get; }` | The number of physical lines read so far. Throws `ObjectDisposedException` after `Dispose`. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `ReadCalendar()` | `IcalCalendar?` | Reads the next calendar, or `null` at the end of the data. Throws `IcalFileException` if the data is not valid iCalendar (the reader then cannot continue), or if the calendar breaks a rule the options do not accept (reading can continue); `ObjectDisposedException` after `Dispose`; `InvalidOperationException` if another read is in progress or an earlier read failed or was canceled. |
| `ReadCalendars()` | `IEnumerable<IcalCalendar>` | The remaining calendars, read as the sequence is enumerated. Throws `ObjectDisposedException` from the call itself if the reader is disposed; during enumeration, throws as `ReadCalendar`. |
| `Dispose()` | `void` | Returns the buffers and closes a stream or file the reader opened itself. Calling it again has no effect. |

### Asynchronous *(netstandard2.0+/.NET)*

Errors in how the reader is used (`ObjectDisposedException`, `InvalidOperationException`)
are thrown by the call. Errors in the data (`IcalFileException`) and cancellation
(`OperationCanceledException`) are thrown by the task.

| Method | Returns | Notes |
| --- | --- | --- |
| `ReadCalendarAsync(CancellationToken cancellationToken = default)` | `ValueTask<IcalCalendar?>` | As `ReadCalendar`. If canceled, the reader cannot continue. |
| `ReadCalendarsAsync(CancellationToken cancellationToken = default)` | `IAsyncEnumerable<IcalCalendar>` | The remaining calendars. The token is combined with any token passed to `GetAsyncEnumerator`. Throws `ObjectDisposedException` from the call itself if the reader is disposed. |
| `DisposeAsync()` | `ValueTask` | Same as `Dispose`; returns a completed task. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable`
are available on `netstandard2.0`, `netstandard2.1`, and the .NET targets only; on
`netstandard1.1` construct the reader from a `Stream` or `TextReader` and read
synchronously. The nullable reference annotations apply to the `netstandard2.1`,
`net8.0`, and `net10.0` builds.

## See also

- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [Read asynchronously](../how-to/read-asynchronously.md)
- [Read non-compliant feeds](../how-to/read-non-compliant-feeds.md)
- [Standards compliance](../concepts/standards-compliance.md)
- [The layered model](../concepts/layered-model.md)
- [IcalWriter](IcalWriter.md)
- [IcalCalendar](IcalCalendar.md)
- [IcalContentLineReader](IcalContentLineReader.md)
- [IcalOptions](IcalOptions.md)
- [IcalFileException](IcalFileException.md)
- [File.iCal API reference](index.md)
