---
title: JcalReader Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# JcalReader Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A reader of jCal data, the JSON form of iCalendar (RFC 7265), into the component
object model. Each call to `ReadCalendar` returns the next calendar as an
[IcalCalendar](IcalCalendar.md). The calendars are the same as those
[IcalReader](IcalReader.md) reads from the same data in `.ics` form.

**Syntax**

```csharp
public sealed class JcalReader : IDisposable, IAsyncDisposable
```

## Remarks

The data is a JSON document in UTF-8, which may start with a byte order mark. It is
either the component array of one calendar, `["vcalendar", [...], [...]]`, or an
array of them (RFC 7265 §3.2). The reader accepts both, and reads one calendar at a
time, so a document doesn't have to fit in memory. Component, property and parameter
names are converted to upper case, and each value to the text form of RFC 5545
(RFC 7265 §4). [Read and write jCal](../how-to/read-write-jcal.md) has the full
mapping.

- A value whose type identifier isn't the property's default adds a `VALUE`
  parameter, after the other parameters (RFC 7265 §3.5.1).
- A value with the `unknown` type is kept as raw text, with no `VALUE` parameter
  (RFC 7265 §5). A type identifier that isn't one of RFC 5545's, such as `x-mytype`,
  is an extension value type: the value is kept as raw text, with a `VALUE` parameter
  naming the type.
- The values of a multi-valued property, such as CATEGORIES or EXDATE, are joined
  into a list (RFC 7265 §3.4).
- A RECUR object keeps the order of its members as the order of the rule parts.
- A PERIOD may be an array of two strings, as RFC 7265 §3.6.9 gives it, or one string
  with the parts joined by a solidus, as its Appendix B gives it.
- A string is accepted as the value of any type, so that a value that doesn't follow
  its type is kept for the conformance check. A number is accepted only for INTEGER
  and FLOAT, `true` and `false` only for BOOLEAN, an array only for PERIOD, GEO and
  REQUEST-STATUS, and an object only for RECUR.

**Encoding.** jCal is always UTF-8, so the reader takes a file path or a `Stream`,
not a `TextReader`. `IcalOptions.Encoding` must be `null` or UTF-8. Of the other
options, only the conformance options apply.

**Allocation.** The reader reads the JSON with `Utf8JsonReader` over pooled blocks of
bytes. The first block is 16 KB, and a block doubles in size when a single JSON token
doesn't fit it. A read does no I/O of its own beyond filling the next block. The
reader always builds the component object model; there is no lower-level view of the
JSON.

**Errors.** JSON that isn't valid, or doesn't follow the structure of jCal, throws
[IcalFileException](IcalFileException.md). Its `Data` holds:

- `BytePosition`: the 0-based offset in bytes of the JSON token at fault.
- `JsonPath`: where the token has been read, the JSONPath of the value, such as
  `$[2][0][1][3]`.
- `LineNumber` and `LinePosition`: for data that isn't valid JSON, the line and the
  byte position on that line.

A JSON string that isn't valid UTF-8, or escapes a lone surrogate, also throws
`IcalFileException`. Such an error, and any other exception from a read except a
conformance error, leaves the reader unable to continue: later reads throw
`InvalidOperationException`. That includes a canceled asynchronous read.

**Conformance.** Each calendar is checked once its component array has been read, as
`IcalComponent.Validate` does, against the same rules and
[IcalOptions](IcalOptions.md) as `IcalReader`. The diagnostics have no line numbers,
since a jCal document has no content lines; their component path gives the place.
The first problem the options don't accept throws `IcalFileException`, and reading
can then continue with the next calendar. A value that doesn't follow its type is
kept as it is, so the check reports it unless `IcalOptions.AllowInvalidValues` is
set. See [Standards compliance](../concepts/standards-compliance.md).

**Disposal.** `Dispose` and `DisposeAsync` may be called more than once and share
their state. They return the pooled buffer and close a file the reader opened. A
caller-supplied stream is left open. After disposal, every member except `Dispose`
and `DisposeAsync` throws `ObjectDisposedException`. If a read is in progress on
another thread, it completes with `ObjectDisposedException`, and the reader is
released once that read has finished.

**Thread safety.** Only one read may be in progress at a time. Synchronous and
asynchronous reads may follow one another, but a read started while another is in
progress throws `InvalidOperationException`. `Dispose` may be called from another
thread at any time.

**Asynchronous reads.** The asynchronous members fill each block with
`Stream.ReadAsync`, and do no synchronous I/O. A read completes synchronously,
without allocating a task, while the buffer already holds the whole calendar.
Cancellation is checked before each block is read.

## Constructors

| Constructor | Notes |
| --- | --- |
| `JcalReader(Stream jcalStream)` | The stream is left open when the reader is disposed. Throws `ArgumentNullException` if `jcalStream` is `null`, or `ArgumentException` if the stream can't be read. |
| `JcalReader(Stream jcalStream, IcalOptions options)` | Throws `ArgumentNullException` if `jcalStream` or `options` is `null`, or `ArgumentException` if the stream can't be read or `options.Encoding` isn't UTF-8. |
| `JcalReader(string jcalFilePath)` | Opens the file for asynchronous, sequential reading with a 4 KB `FileStream` buffer, sharing it for reading. Throws `ArgumentNullException` if `jcalFilePath` is `null`. |
| `JcalReader(string jcalFilePath, IcalOptions options)` | Throws `ArgumentNullException` if `jcalFilePath` or `options` is `null`, or `ArgumentException` if `options.Encoding` isn't UTF-8. |

## Properties

| Property | Signature | Notes |
| --- | --- | --- |
| `CalendarsRead` | `long CalendarsRead { get; }` | The number of VCALENDAR objects read so far, including any that failed a conformance check. Throws `ObjectDisposedException` after disposal. |
| `ComponentsRead` | `long ComponentsRead { get; }` | The number of components read so far, at any depth, not counting the VCALENDAR objects. Includes those in a calendar that failed a conformance check. Throws `ObjectDisposedException` after disposal. |
| `PropertiesRead` | `long PropertiesRead { get; }` | The number of properties read so far, in all components and calendars. Throws `ObjectDisposedException` after disposal. |

## Methods

### Reading

| Method | Returns | Notes |
| --- | --- | --- |
| `ReadCalendar()` | `IcalCalendar?` | Reads the next calendar, or returns `null` at the end of the data. Throws `IcalFileException` if the data isn't valid jCal (not valid JSON, a value that isn't allowed where it is or isn't valid, a `VERSION` that doesn't include 2.0, or no calendar at all), or if the calendar breaks a rule the options don't accept; in that case reading can continue. Throws `ObjectDisposedException` after disposal, or `InvalidOperationException` if another read is in progress or an earlier read failed. |
| `ReadCalendars()` | `IEnumerable<IcalCalendar>` | Reads the remaining calendars as the sequence is enumerated. Throws `ObjectDisposedException` from this call if the reader has been disposed. During enumeration, throws what `ReadCalendar` throws. |

### Asynchronous reading

| Method | Returns | Notes |
| --- | --- | --- |
| `ReadCalendarAsync(CancellationToken cancellationToken = default)` | `ValueTask<IcalCalendar?>` | Reads the next calendar, or `null` at the end of the data. This call throws `ObjectDisposedException` after disposal, and `InvalidOperationException` if another read is in progress or an earlier read failed or was canceled. The task throws `IcalFileException` as `ReadCalendar` does, `OperationCanceledException` if canceled, or `ObjectDisposedException` if the reader is disposed during the read. A canceled read leaves the reader unable to continue. |
| `ReadCalendarsAsync(CancellationToken cancellationToken = default)` | `IAsyncEnumerable<IcalCalendar>` | Reads the remaining calendars as the sequence is enumerated. The token is combined with any token passed to `GetAsyncEnumerator`. Throws `ObjectDisposedException` from this call if the reader has been disposed. During enumeration, throws what `ReadCalendarAsync` throws. |

### Disposal

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Returns the pooled buffer, and closes the file the reader opened. A caller-supplied stream is left open. Calling it again has no effect. |
| `DisposeAsync()` | `ValueTask` | Does what `Dispose` does, and returns a task that has already completed. |

## Applies to

Targets `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0` only. jCal needs
System.Text.Json, so `JcalReader` isn't available on `netstandard1.1`. On .NET 8 and
.NET 10, System.Text.Json is part of the framework; on .NET Standard 2.0 and 2.1, the
package depends on the System.Text.Json NuGet package. The nullable reference
annotations apply to the `netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Read and write jCal](../how-to/read-write-jcal.md)
- [Convert between formats](../how-to/convert-formats.md)
- [Read asynchronously](../how-to/read-asynchronously.md)
- [Standards compliance](../concepts/standards-compliance.md)
- [JcalWriter](JcalWriter.md)
- [IcalReader](IcalReader.md)
- [XcalReader](XcalReader.md)
- [IcalCalendar](IcalCalendar.md)
- [IcalOptions](IcalOptions.md)
- [IcalFileException](IcalFileException.md)
- [File.iCal API reference](index.md)
