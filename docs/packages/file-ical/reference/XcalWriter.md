---
title: XcalWriter Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# XcalWriter Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A writer of the component object model as xCal data, the XML form of iCalendar
(RFC 6321). Each call to `WriteCalendar` writes an [IcalCalendar](IcalCalendar.md) as
a `vcalendar` element.

**Syntax**

```csharp
public sealed class XcalWriter : IDisposable, IAsyncDisposable
```

## Remarks

The calendars are written in an `icalendar` element in the namespace
`urn:ietf:params:xml:ns:icalendar-2.0`. The writer starts that element with the first
calendar and ends it when the writer is disposed, so dispose the writer before you
read the output. A writer that writes no calendars writes nothing. Component,
property and parameter names are written in lower case, and each value in its typed
value element, such as `date-time`, converted from the text form of RFC 5545
(RFC 6321 §3). [Read and write xCal](../how-to/read-write-xcal.md) has the full
mapping.

- The `VALUE` parameter isn't written, since the value element gives the type
  (RFC 6321 §3.5.1).
- The value of a property whose type isn't known, such as an `X-` property with no
  `VALUE` parameter, is written as it is in an `unknown` element (RFC 6321 §5).
- Each value of a multi-valued property, such as CATEGORIES or EXDATE, is an element
  of its own (RFC 6321 §3.4.1.1).
- PERIOD, RECUR, GEO and REQUEST-STATUS values are written as child elements. A RECUR
  value keeps the order of its rule parts.
- A value that isn't valid for its type, which `IcalOptions.AllowInvalidValues` lets
  a calendar hold, is written as it is in an `unknown` element, keeping any `VALUE`
  parameter.
- The calendar's `VERSION` is always written as 2.0, in the place of the calendar's
  own `VERSION`, or first if it has none.

A calendar written by this writer and read back by [XcalReader](XcalReader.md) has
the same properties with the same values. Only the spelling of a value may change,
such as TEXT escaping, the case of a BOOLEAN, or the place of a `VALUE` parameter,
which is read back after the other parameters. A `VALUE` parameter that names the
property's default type isn't read back, since xCal can't tell it apart from none.

**Output.** For a path, a stream or a text writer, the writer creates its own
`XmlWriter`. It writes an XML declaration, indents with two spaces, and uses
`IcalOptions.LineBreak` for its line breaks, which must be CRLF (the default) or a
bare line feed. A stream or file is written in `IcalOptions.Encoding`, or UTF-8
without a byte order mark if that is `null`. The default encoding throws on a
character it can't encode. A `TextWriter` or `XmlWriter` does its own encoding, so
`IcalOptions.Encoding` must be `null` with them. A caller-supplied `XmlWriter` is
used with its own settings, such as its indentation, and the `icalendar` element is
written at its current position, so it can be part of a larger document. The options
that concern `.ics` content lines don't apply.

**Errors.** Before a calendar is written, it is checked as `IcalComponent.Validate`
does, against the same rules and [IcalOptions](IcalOptions.md) as
[IcalWriter](IcalWriter.md). If it breaks a rule the options don't accept,
[IcalFileException](IcalFileException.md) is thrown, carrying the component path in
its `Data`, and nothing of that calendar is written. The calendar is also refused if
a name can't be an XML element name, or a value holds a character that XML can't
hold. The writer builds every element of a calendar before it writes any of them.
After such an error the writer can go on to the next calendar.

**Disposal.** `Dispose` and `DisposeAsync` may be called more than once and share
their state. They end the `icalendar` element and any element left open. Then they
flush and close the `XmlWriter` the writer created, with a file it opened. A
caller-supplied stream or text writer is flushed but left open. A caller-supplied
`XmlWriter` is neither flushed nor closed. After disposal, every member except
`Dispose` and `DisposeAsync` throws `ObjectDisposedException`. If an operation is in
progress on another thread, it completes with `ObjectDisposedException`, and the
document is ended once that operation has finished.

**Thread safety.** Only one operation may be in progress at a time. Synchronous and
asynchronous calls may follow one another, but a call made while another is in
progress throws `InvalidOperationException`. `WriteCalendars` and
`WriteCalendarsAsync` are one operation each, so neither the calendars being
enumerated nor the progress callback may call the writer.

**Asynchronous writes.** The asynchronous members need an `XmlWriter` whose
`XmlWriterSettings.Async` is `true`. The writers `XcalWriter` creates itself are. For
a caller-supplied `XmlWriter` created with `Async = false`, they throw
`InvalidOperationException`. Cancellation is checked only before a calendar is
started, so a canceled write never leaves part of a calendar. A canceled operation
leaves the writer unable to continue: later calls throw `InvalidOperationException`.

**Progress.** `WriteCalendars` and `WriteCalendarsAsync` report an
[IcalWriteProgress](IcalWriteProgress.md) after each calendar has been handed to the
`XmlWriter`, synchronously on the writing thread. Its `LinesWritten` is the number of
content lines the calendars written so far would have in `.ics` form, so it matches
what `IcalWriter` reports. An exception from the progress callback ends the call; the
calendar it was reporting has been written.

## Constructors

| Constructor | Notes |
| --- | --- |
| `XcalWriter(Stream xcalStream)` | Writes UTF-8 without a byte order mark. The stream is left open when the writer is disposed. Throws `ArgumentNullException` if `xcalStream` is `null`, or `ArgumentException` if the stream can't be written. |
| `XcalWriter(Stream xcalStream, IcalOptions options)` | Honours `options.Encoding` and `options.LineBreak`. Throws `ArgumentNullException` if `xcalStream` or `options` is `null`, or `ArgumentException` if the stream can't be written or `options.LineBreak` isn't CRLF or LF. |
| `XcalWriter(string xcalFilePath)` | *(netstandard2.0+/.NET)* Creates (or overwrites) the file for asynchronous writing with a 4 KB `FileStream` buffer, sharing it for reading, and writes UTF-8 without a byte order mark. Throws `ArgumentNullException` if `xcalFilePath` is `null`. |
| `XcalWriter(string xcalFilePath, IcalOptions options)` | *(netstandard2.0+/.NET)* Honours `options.Encoding` and `options.LineBreak`. Throws `ArgumentNullException` if `xcalFilePath` or `options` is `null`, or `ArgumentException` if `options.LineBreak` isn't CRLF or LF. |
| `XcalWriter(TextWriter writer)` | The text writer is flushed but not closed when the writer is disposed. Throws `ArgumentNullException` if `writer` is `null`. |
| `XcalWriter(TextWriter writer, IcalOptions options)` | Throws `ArgumentNullException` if `writer` or `options` is `null`, or `ArgumentException` if `options.Encoding` is set or `options.LineBreak` isn't CRLF or LF. |
| `XcalWriter(XmlWriter writer)` | Writes at the `XmlWriter`'s current position, with its settings. It is neither flushed nor closed when the writer is disposed. Throws `ArgumentNullException` if `writer` is `null`. |
| `XcalWriter(XmlWriter writer, IcalOptions options)` | Throws `ArgumentNullException` if `writer` or `options` is `null`, or `ArgumentException` if `options.Encoding` is set. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CalendarsWritten` | `long CalendarsWritten { get; }` | The number of VCALENDAR objects written so far. Throws `ObjectDisposedException` after disposal. |
| `ComponentsWritten` | `long ComponentsWritten { get; }` | The number of components written so far, at any depth, not counting the VCALENDAR objects. Throws `ObjectDisposedException` after disposal. |
| `PropertiesWritten` | `long PropertiesWritten { get; }` | The number of properties written so far, in all components and calendars. Throws `ObjectDisposedException` after disposal. |

## Methods

### Writing

| Method | Returns | Notes |
| --- | --- | --- |
| `Flush()` | `void` | Flushes the `XmlWriter`, whether the writer created it or not. The `icalendar` element isn't ended. Throws `ObjectDisposedException` after disposal, or `InvalidOperationException` if another operation is in progress or an earlier one was canceled. |
| `WriteCalendar(IcalCalendar calendar)` | `void` | Writes a calendar. Throws `ArgumentNullException` if `calendar` is `null`; `IcalFileException` if the calendar breaks a rule the options don't accept or can't be written in xCal, in which case nothing has been written; `ObjectDisposedException` after disposal; or `InvalidOperationException` if another operation is in progress or an earlier one was canceled. |
| `WriteCalendars(IEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress>? progress = null)` | `void` | Writes each calendar in turn. Throws `ArgumentNullException` if `calendars` is `null`, or `ArgumentException` if it contains `null`. Throws `IcalFileException` as `WriteCalendar` does; the calendars before the one at fault have been written. Throws `ObjectDisposedException` if the writer is disposed before or during the call, or `InvalidOperationException` as `WriteCalendar` does. |

### Asynchronous writing

| Method | Returns | Notes |
| --- | --- | --- |
| `FlushAsync(CancellationToken cancellationToken = default)` | `Task` | *(netstandard2.0+/.NET)* Flushes the `XmlWriter` asynchronously. This call throws `ObjectDisposedException` after disposal, and `InvalidOperationException` if another operation is in progress, an earlier one was canceled, or the `XmlWriter` doesn't allow asynchronous writes. The task throws `OperationCanceledException` if canceled. |
| `WriteCalendarAsync(IcalCalendar calendar, CancellationToken cancellationToken = default)` | `ValueTask` | *(netstandard2.0+/.NET)* Writes a calendar. The task completes once the calendar has been handed to the `XmlWriter`. This call throws `ArgumentNullException`, `ObjectDisposedException` and `InvalidOperationException` as `FlushAsync` does. The task throws `IcalFileException` as `WriteCalendar` does, `OperationCanceledException` if canceled, or `ObjectDisposedException` if the writer is disposed during the write. |
| `WriteCalendarsAsync(IEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | *(netstandard2.0+/.NET)* Writes each calendar in turn. This call throws `ArgumentNullException` if `calendars` is `null`, and `ObjectDisposedException` or `InvalidOperationException` as `FlushAsync` does. The task throws `ArgumentException` if `calendars` contains `null`, `IcalFileException` as `WriteCalendars` does, or `OperationCanceledException` if canceled. |
| `WriteCalendarsAsync(IAsyncEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress>? progress = null, CancellationToken cancellationToken = default)` | `Task` | *(netstandard2.0+/.NET)* As the `IEnumerable` overload. `cancellationToken` is also passed to the calendars' enumerator. |

### Disposal

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Ends the `icalendar` element, then flushes and closes the `XmlWriter` the writer created, with the stream or file it opened. A caller-supplied stream or text writer is left open, and a caller-supplied `XmlWriter` is neither flushed nor closed. Calling it again has no effect. |
| `DisposeAsync()` | `ValueTask` | *(netstandard2.0+/.NET)* Does what `Dispose` does, asynchronously when the `XmlWriter` allows it. The `XmlWriter` the writer created is disposed even if ending the document throws. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The `string` file-path constructors, `IAsyncDisposable`, `DisposeAsync` and the
asynchronous writing members are available on `netstandard2.0`, `netstandard2.1`,
and the .NET targets only. On `netstandard1.1`, construct the writer from a `Stream`,
`TextWriter` or `XmlWriter`. The nullable reference annotations apply to the
`netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Read and write xCal](../how-to/read-write-xcal.md)
- [Convert between formats](../how-to/convert-formats.md)
- [Standards compliance](../concepts/standards-compliance.md)
- [XcalReader](XcalReader.md)
- [IcalWriter](IcalWriter.md)
- [JcalWriter](JcalWriter.md)
- [IcalCalendar](IcalCalendar.md)
- [IcalOptions](IcalOptions.md)
- [IcalWriteProgress](IcalWriteProgress.md)
- [IcalFileException](IcalFileException.md)
- [File.iCal API reference](index.md)
