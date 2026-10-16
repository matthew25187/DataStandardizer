---
title: JcalWriter Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# JcalWriter Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A writer of the component object model as jCal data, the JSON form of iCalendar
(RFC 7265). Each call to `WriteCalendar` writes an [IcalCalendar](IcalCalendar.md) as
a jCal component array.

**Syntax**

```csharp
public sealed class JcalWriter : IDisposable, IAsyncDisposable
```

## Remarks

The data is written as compact UTF-8 JSON with no indentation and no byte order mark.
Characters outside ASCII are written as they are, except characters outside the
Basic Multilingual Plane, which System.Text.Json always writes as `\u` escape pairs.

- **One calendar** is written as its component array, `["vcalendar", [...], [...]]`.
- **Two or more calendars** are written as an array of component arrays
  (RFC 7265 §3.2).

Which of the two the document is depends on whether a second calendar follows. So
the writer holds the first calendar in memory until a second is written or the
writer is disposed. `Flush` doesn't write a calendar that is still held, so dispose
the writer before you read the output. A writer that writes no calendars writes
nothing.

Component, property and parameter names are written in lower case, and each value as
the JSON value of its type, converted from the text form of RFC 5545 (RFC 7265 §3).
[Read and write jCal](../how-to/read-write-jcal.md) has the full mapping.

- The `VALUE` parameter isn't written, since the type identifier gives the type
  (RFC 7265 §3.5.1).
- The value of a property whose type isn't known, such as an `X-` property with no
  `VALUE` parameter, is written as it is with the `unknown` type (RFC 7265 §5). A
  `VALUE` parameter that names a type RFC 5545 doesn't define, such as `X-MYTYPE`, is
  written as the type identifier, and the value as it is.
- Each value of a multi-valued property, such as CATEGORIES or EXDATE, is an element
  of the property array (RFC 7265 §3.4). A parameter with more than one value is an
  array of strings (RFC 7265 §3.5.2).
- INTEGER and FLOAT values are JSON numbers, and BOOLEAN values JSON literals. A
  PERIOD is an array of two strings, a RECUR value an object whose members keep the
  order of its rule parts, and GEO and REQUEST-STATUS values arrays.
- A value that isn't valid for its type, which `IcalOptions.AllowInvalidValues` lets
  a calendar hold, is written as it is with the `unknown` type, keeping any `VALUE`
  parameter.
- The calendar's `VERSION` is always written as 2.0, in the place of the calendar's
  own `VERSION`, or first if it has none.

A calendar written by this writer and read back by [JcalReader](JcalReader.md) has
the same properties with the same values. Only the spelling of a value may change,
such as TEXT escaping, the case of a BOOLEAN, the digits of an INTEGER, or the place
of a `VALUE` parameter, which is read back after the other parameters. A `VALUE`
parameter that names the property's default type isn't read back, since jCal can't
tell it apart from none.

**Options.** `IcalOptions.Encoding` must be `null` or UTF-8. The options that concern
`.ics` content lines, such as `IcalOptions.LineBreak`, don't apply.

**Errors.** Before a calendar is written, it is checked as `IcalComponent.Validate`
does, against the same rules and [IcalOptions](IcalOptions.md) as
[IcalWriter](IcalWriter.md). If it breaks a rule the options don't accept,
[IcalFileException](IcalFileException.md) is thrown, carrying the component path in
its `Data`, and nothing of that calendar is written. The calendar is also refused if
a name isn't a valid iCalendar name, a property has two parameters of the same name
(a JSON object can't hold both), or a value holds a lone surrogate. After such an
error the writer can go on to the next calendar, and a calendar already held is
kept.

**Allocation.** Each calendar is encoded in full with `Utf8JsonWriter` into pooled
blocks of bytes before any of it is written to the stream.

**Disposal.** `Dispose` and `DisposeAsync` may be called more than once and share
their state. They end the document, writing the first calendar if it is the only one,
then flush the stream, close a file the writer opened and return the pooled buffers.
A caller-supplied stream is flushed but left open. After disposal, every member
except `Dispose` and `DisposeAsync` throws `ObjectDisposedException`. If an operation
is in progress on another thread, it completes with `ObjectDisposedException`, and
the document is ended once that operation has finished.

**Thread safety.** Only one operation may be in progress at a time. Synchronous and
asynchronous calls may follow one another, but a call made while another is in
progress throws `InvalidOperationException`. `WriteCalendars` and
`WriteCalendarsAsync` are one operation each, so neither the calendars being
enumerated nor the progress callback may call the writer.

**Asynchronous writes.** The asynchronous members write each encoded calendar with
`Stream.WriteAsync`, and do no synchronous I/O. Cancellation is checked before each
calendar is written, so a canceled write never leaves part of a calendar. A canceled
operation leaves the writer unable to continue: later calls throw
`InvalidOperationException`.

**Progress.** `WriteCalendars` and `WriteCalendarsAsync` report an
[IcalWriteProgress](IcalWriteProgress.md) after each calendar has been written or
held, synchronously on the writing thread. Its `LinesWritten` is the number of
content lines the calendars written so far would have in `.ics` form, so it matches
what `IcalWriter` reports. An exception from the progress callback ends the call; the
calendar it was reporting has been written.

## Constructors

| Constructor | Notes |
| --- | --- |
| `JcalWriter(Stream jcalStream)` | Writes UTF-8 without a byte order mark. The stream is flushed but left open when the writer is disposed. Throws `ArgumentNullException` if `jcalStream` is `null`, or `ArgumentException` if the stream can't be written. |
| `JcalWriter(Stream jcalStream, IcalOptions options)` | Throws `ArgumentNullException` if `jcalStream` or `options` is `null`, or `ArgumentException` if the stream can't be written or `options.Encoding` isn't UTF-8. |
| `JcalWriter(string jcalFilePath)` | Creates (or overwrites) the file for asynchronous writing with a 4 KB `FileStream` buffer, sharing it for reading. Throws `ArgumentNullException` if `jcalFilePath` is `null`. |
| `JcalWriter(string jcalFilePath, IcalOptions options)` | Throws `ArgumentNullException` if `jcalFilePath` or `options` is `null`, or `ArgumentException` if `options.Encoding` isn't UTF-8. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CalendarsWritten` | `long CalendarsWritten { get; }` | The number of VCALENDAR objects written so far, including a first calendar that is still held. Throws `ObjectDisposedException` after disposal. |
| `ComponentsWritten` | `long ComponentsWritten { get; }` | The number of components written so far, at any depth, not counting the VCALENDAR objects. Throws `ObjectDisposedException` after disposal. |
| `PropertiesWritten` | `long PropertiesWritten { get; }` | The number of properties written so far, in all components and calendars. Throws `ObjectDisposedException` after disposal. |

## Methods

### Writing

| Method | Returns | Notes |
| --- | --- | --- |
| `Flush()` | `void` | Flushes the stream. A first calendar that is still held isn't written. Throws `ObjectDisposedException` after disposal, or `InvalidOperationException` if another operation is in progress or an earlier one was canceled. |
| `WriteCalendar(IcalCalendar calendar)` | `void` | Writes a calendar, or holds it if it is the first. Throws `ArgumentNullException` if `calendar` is `null`; `IcalFileException` if the calendar breaks a rule the options don't accept or can't be written in jCal, in which case nothing has been written; `ObjectDisposedException` after disposal; or `InvalidOperationException` if another operation is in progress or an earlier one was canceled. |
| `WriteCalendars(IEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress>? progress = null)` | `void` | Writes each calendar in turn. Throws `ArgumentNullException` if `calendars` is `null`, or `ArgumentException` if it contains `null`. Throws `IcalFileException` as `WriteCalendar` does; the calendars before the one at fault have been written. Throws `ObjectDisposedException` if the writer is disposed before or during the call, or `InvalidOperationException` as `WriteCalendar` does. |

### Asynchronous writing

| Method | Returns | Notes |
| --- | --- | --- |
| `FlushAsync(CancellationToken cancellationToken = default)` | `Task` | Flushes the stream asynchronously. A first calendar that is still held isn't written. This call throws `ObjectDisposedException` after disposal, and `InvalidOperationException` if another operation is in progress or an earlier one was canceled. The task throws `OperationCanceledException` if canceled. |
| `WriteCalendarAsync(IcalCalendar calendar, CancellationToken cancellationToken = default)` | `ValueTask` | Writes a calendar, or holds it if it is the first. This call throws `ArgumentNullException` if `calendar` is `null`, and `ObjectDisposedException` or `InvalidOperationException` as `FlushAsync` does. The task throws `IcalFileException` as `WriteCalendar` does, `OperationCanceledException` if canceled, or `ObjectDisposedException` if the writer is disposed during the write. |
| `WriteCalendarsAsync(IEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | Writes each calendar in turn. This call throws `ArgumentNullException` if `calendars` is `null`, and `ObjectDisposedException` or `InvalidOperationException` as `FlushAsync` does. The task throws `ArgumentException` if `calendars` contains `null`, `IcalFileException` as `WriteCalendars` does, or `OperationCanceledException` if canceled. |
| `WriteCalendarsAsync(IAsyncEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | As the `IEnumerable` overload. `cancellationToken` is also passed to the calendars' enumerator. |

### Disposal

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Ends the document, writing the first calendar if it is the only one, then flushes the stream, closes the file the writer opened and returns the pooled buffers. A caller-supplied stream is flushed but left open. Calling it again has no effect. |
| `DisposeAsync()` | `ValueTask` | Does what `Dispose` does, with asynchronous I/O. The file is closed even if ending the document throws. |

## Applies to

Targets `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0` only. jCal needs
System.Text.Json, so `JcalWriter` isn't available on `netstandard1.1`. On .NET 8 and
.NET 10, System.Text.Json is part of the framework; on .NET Standard 2.0 and 2.1, the
package depends on the System.Text.Json NuGet package. The nullable reference
annotations apply to the `netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Read and write jCal](../how-to/read-write-jcal.md)
- [Convert between formats](../how-to/convert-formats.md)
- [Standards compliance](../concepts/standards-compliance.md)
- [JcalReader](JcalReader.md)
- [IcalWriter](IcalWriter.md)
- [XcalWriter](XcalWriter.md)
- [IcalCalendar](IcalCalendar.md)
- [IcalOptions](IcalOptions.md)
- [IcalWriteProgress](IcalWriteProgress.md)
- [IcalFileException](IcalFileException.md)
- [File.iCal API reference](index.md)
