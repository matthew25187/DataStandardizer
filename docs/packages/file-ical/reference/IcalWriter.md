---
title: IcalWriter Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# IcalWriter Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A writer of the component object model as iCalendar data (RFC 5545). Each call to
`WriteCalendar` writes one [IcalCalendar](IcalCalendar.md), with its components and
properties, as content lines.

**.NET:**

```csharp
public sealed class IcalWriter : IDisposable, IAsyncDisposable
```

**.NET Standard 1.1:**

```csharp
public sealed class IcalWriter : IDisposable
```

## Remarks

Each calendar is written by an [IcalContentLineWriter](IcalContentLineWriter.md), so
the same constructors, encodings, options and line rules apply: folding at
`IcalOptions.FoldLength` octets, `IcalOptions.FoldWhitespace`, `IcalOptions.LineBreak`,
and RFC 6868 caret encoding of parameter values. The default output is UTF-8 without a
byte order mark, with CRLF line breaks. A caller-supplied `Stream` is left open. A
caller-supplied `TextWriter` is neither flushed nor closed on disposal, so call `Flush`
first.

**Order.** Properties are written in order, then the components nested in the
component, each between its `BEGIN` and `END` lines. A property read by
[IcalReader](IcalReader.md) keeps its parameters exactly as they were read.

**Version.** The calendar's `VERSION` is always written as `VERSION:2.0`, in the place
of the calendar's own first `VERSION`, or straight after `BEGIN:VCALENDAR` if it has
none. Any further `VERSION` properties are not written.

**Conformance.** Before a calendar is written, it is checked as `IcalComponent.Validate`
does, except that a missing `VERSION` is not a problem. If it breaks a rule of
RFC 5545 that the options do not accept, `IcalFileException` is thrown and nothing of
that calendar is written. Its `Data` holds `ComponentPath` and `DiagnosticKind`, plus
`LineNumber` (the line the property was read from, if any) and `PropertyName` where they
apply. The accommodations are `IcalOptions.AllowMissingRequiredProperties`,
`AllowCardinalityViolations`, `AllowUndefinedTimeZones` and `AllowInvalidValues`, all
off by default. `InvalidComponentNesting` and `UnsupportedVersion` problems are never
accepted.

**Content line errors.** A property that cannot be written as a content line (a name
that is not letters, digits and hyphens, or a value holding a control character other
than tab) throws `IcalFileException` from `IcalContentLineWriter`. The lines of the
calendar before it have already been written, so the output then holds part of a
calendar. That exception's `LineNumber` is the number the line would have had in the
output. The writer can still be used afterwards.

**Disposal.** `Dispose` and `DisposeAsync` may be called more than once and share
their state. They flush and close a stream writer or file the writer created itself.
After disposal, every member throws `ObjectDisposedException`. If an operation is in
progress on another thread, it completes with `ObjectDisposedException`.

**Thread safety and async rules.** A single writer instance is not thread-safe. Only
one operation may be in progress at a time. Synchronous and asynchronous calls may
follow one another, but a call made while another is in progress throws
`InvalidOperationException`. A canceled asynchronous operation leaves the writer
unable to continue. In the bulk methods, the whole call is one operation, so neither
the calendars being enumerated nor the progress callback may call the writer. The
lines of each calendar are gathered before any is written, so a calendar changed by
another thread during the write cannot make it fail part way through for that reason.

## Constructors

| Constructor | Notes |
| --- | --- |
| `IcalWriter(Stream icalStream)` | Writes UTF-8 without a byte order mark. The stream is left open. Throws `ArgumentNullException` if `icalStream` is `null`, or `ArgumentException` if the stream cannot be written. |
| `IcalWriter(Stream icalStream, IcalOptions options)` | Honours `options.Encoding`. Throws `ArgumentNullException` if `icalStream` or `options` is `null`, or `ArgumentException` if the stream cannot be written or `FoldLength`, `FoldWhitespace` or `LineBreak` is not valid. |
| `IcalWriter(string icalFilePath)` | *(netstandard2.0+/.NET)* Creates or overwrites the file for asynchronous writing, as UTF-8 without a byte order mark. Throws `ArgumentNullException` if `icalFilePath` is `null`. File system errors come from `FileStream`. |
| `IcalWriter(string icalFilePath, IcalOptions options)` | *(netstandard2.0+/.NET)* Honours `options.Encoding`. Throws `ArgumentNullException` if `icalFilePath` or `options` is `null`, or `ArgumentException` if `FoldLength`, `FoldWhitespace` or `LineBreak` is not valid. |
| `IcalWriter(TextWriter writer)` | The text writer is neither flushed nor closed on disposal. Throws `ArgumentNullException` if `writer` is `null`. |
| `IcalWriter(TextWriter writer, IcalOptions options)` | Throws `ArgumentNullException` if `writer` or `options` is `null`, or `ArgumentException` if `options.Encoding` is set or `FoldLength`, `FoldWhitespace` or `LineBreak` is not valid. |

The options are valid when `FoldLength` is at least 4, `FoldWhitespace` is a space or
a horizontal tab, and `LineBreak` is `"\r\n"` or `"\n"`.

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CalendarsWritten` | `long CalendarsWritten { get; }` | The number of `VCALENDAR` objects written so far. Throws `ObjectDisposedException` after `Dispose`. |
| `ComponentsWritten` | `long ComponentsWritten { get; }` | The number of components written so far, at any depth and not counting the `VCALENDAR` objects. Throws `ObjectDisposedException` after `Dispose`. |
| `LinesWritten` | `long LinesWritten { get; }` | The number of logical (unfolded) content lines written so far. Throws `ObjectDisposedException` after `Dispose`. |

## Methods

### Synchronous

| Method | Returns | Notes |
| --- | --- | --- |
| `WriteCalendar(IcalCalendar calendar)` | `void` | Checks and writes one calendar. Throws `ObjectDisposedException` after `Dispose`; `ArgumentNullException` if `calendar` is `null`; `IcalFileException` if the calendar breaks a rule the options do not accept (nothing written) or a content line cannot be written (earlier lines written); `InvalidOperationException` if another operation is in progress or an earlier asynchronous operation was canceled. |
| `WriteCalendars(IEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress>? progress = null)` | `void` | Writes the calendars in order. `progress` receives an [IcalWriteProgress](IcalWriteProgress.md) with all three counts after each calendar, synchronously on the writing thread; an exception it throws ends the call, and the calendar it reported has been written. Throws `ArgumentNullException` if `calendars` is `null`; `ArgumentException` if `calendars` contains `null`, or `IcalFileException` as `WriteCalendar`, in both cases after the calendars before it have been written; `ObjectDisposedException` if the writer is disposed before or during the call; `InvalidOperationException` as `WriteCalendar`. |
| `Flush()` | `void` | Flushes the underlying text writer, whether the writer created it or it was supplied. Throws `ObjectDisposedException` after `Dispose`, or `InvalidOperationException` as `WriteCalendar`. |
| `Dispose()` | `void` | Flushes and closes a stream writer or file the writer created itself. Calling it again has no effect. |

### Asynchronous *(netstandard2.0+/.NET)*

Errors in how the writer is used (`ObjectDisposedException`, `ArgumentNullException`,
`InvalidOperationException`) are thrown by the call. Errors in the calendars
(`IcalFileException`, and `ArgumentException` for a `null` item), I/O errors, and
cancellation (`OperationCanceledException`) are thrown by the task. If the writer is
disposed while an operation is pending, the task throws `ObjectDisposedException`. If
an operation is canceled, the writer cannot continue.

| Method | Returns | Notes |
| --- | --- | --- |
| `WriteCalendarAsync(IcalCalendar calendar, CancellationToken cancellationToken = default)` | `ValueTask` | As `WriteCalendar`. Completes synchronously when the underlying text writer takes every line without waiting. |
| `WriteCalendarsAsync(IEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | As `WriteCalendars`. |
| `WriteCalendarsAsync(IAsyncEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | As `WriteCalendars`, for an asynchronous sequence. `cancellationToken` is passed to its enumerator. |
| `FlushAsync(CancellationToken cancellationToken = default)` | `Task` | As `Flush`. |
| `DisposeAsync()` | `ValueTask` | As `Dispose`, flushing asynchronously. The writer is disposed even if the flush throws. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The `string` file-path constructors, the asynchronous methods, and `IAsyncDisposable`
are available on `netstandard2.0`, `netstandard2.1`, and the .NET targets only; on
`netstandard1.1` construct the writer from a `Stream` or `TextWriter` and write
synchronously. The nullable reference annotations apply to the `netstandard2.1`,
`net8.0`, and `net10.0` builds.

## See also

- [Read and write .ics files](../how-to/read-write-ics-files.md)
- [Convert between formats](../how-to/convert-formats.md)
- [Standards compliance](../concepts/standards-compliance.md)
- [IcalReader](IcalReader.md)
- [IcalCalendar](IcalCalendar.md)
- [IcalContentLineWriter](IcalContentLineWriter.md)
- [IcalWriteProgress](IcalWriteProgress.md)
- [IcalOptions](IcalOptions.md)
- [IcalFileException](IcalFileException.md)
- [File.iCal API reference](index.md)
