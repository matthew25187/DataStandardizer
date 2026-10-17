---
title: XcalReader Class
parent: File.iCal
grand_parent: Packages
nav_exclude: true
---

# XcalReader Class

## Definition

Namespace: `DataStandardizer.File.iCal`

A reader of xCal data, the XML form of iCalendar (RFC 6321), into the component
object model. Each call to `ReadCalendar` returns the next `vcalendar` element as an
[IcalCalendar](IcalCalendar.md). The calendars are the same as those
[IcalReader](IcalReader.md) reads from the same data in `.ics` form.

**Syntax**

```csharp
public sealed class XcalReader : IDisposable, IAsyncDisposable
```

## Remarks

The data is an XML document whose root is the `icalendar` element in the namespace
`urn:ietf:params:xml:ns:icalendar-2.0`. The reader reads one `vcalendar` element at a
time, so a document doesn't have to fit in memory. Component, property and parameter
names are converted to upper case, and each value to the text form of RFC 5545
(RFC 6321 §4). [Read and write xCal](../how-to/read-write-xcal.md) has the full
mapping.

- A value element whose type isn't the property's default adds a `VALUE` parameter,
  after the other parameters (RFC 6321 §3.5.1).
- An `unknown` value element is kept as raw text, with no `VALUE` parameter
  (RFC 6321 §5).
- The values of a multi-valued property, such as CATEGORIES or EXDATE, are joined
  into a list (RFC 6321 §3.4.1.1).
- A RECUR value keeps the order of its rule parts.
- Elements in another XML namespace are extensions. They are skipped with their
  content, not converted to the `XML` property of RFC 6321 §4.2.

**Sources.** The reader takes a file path, a `Stream`, a `TextReader` or an
`XmlReader`. For a path, a stream or a text reader it creates its own `XmlReader`,
which prohibits a document type definition (DTD), ignores comments and processing
instructions, and on .NET Standard 2.0 and later allows asynchronous reads and
resolves no external resources. A caller-supplied `XmlReader` is used with its own
settings, and may be positioned inside a larger document, such as a CalDAV response.
Reading starts at its current node, or at its first node if it hasn't started, and
stops at the end of the `icalendar` element, leaving the `XmlReader` on that end tag.
A document the reader opened itself is read to its end, so anything after the root
element is checked too.

**Encoding.** A stream or file is read in the encoding the XML document declares, or
UTF-8 if it declares none. When `IcalOptions.Encoding` is set, it is used in place of
the declared encoding, unless the data starts with a byte order mark. A `TextReader`
or `XmlReader` has already decoded the data, so `IcalOptions.Encoding` must be `null`
with them. Of the other options, only the conformance options apply.

**Errors.** XML that isn't well-formed, or doesn't follow the structure of xCal,
throws [IcalFileException](IcalFileException.md). Its `Data` holds the line of the
XML document under `LineNumber` and the position on that line under `LinePosition`.
Such an error, and any other exception from a read except a conformance error,
leaves the reader unable to continue: later reads throw `InvalidOperationException`.
That includes a canceled asynchronous read.

**Conformance.** Each calendar is checked once its `vcalendar` element has been read,
as `IcalComponent.Validate` does, against the same rules and
[IcalOptions](IcalOptions.md) as `IcalReader`. The line numbers in its diagnostics are
those of the XML document. The first problem the options don't accept throws
`IcalFileException`, and reading can then continue with the next calendar. A value
that isn't valid for its value element is kept as it is, so the check reports it
unless `IcalOptions.AllowInvalidValues` is set. See
[Standards compliance](../concepts/standards-compliance.md).

**Disposal.** `Dispose` and `DisposeAsync` may be called more than once and share
their state. They dispose the `XmlReader` the reader created, and with it a file the
reader opened. A caller-supplied stream, text reader or `XmlReader` is left open.
After disposal, every member except `Dispose` and `DisposeAsync` throws
`ObjectDisposedException`. If a read is in progress on another thread, it completes
with `ObjectDisposedException`, and the `XmlReader` is released once that read has
finished with it.

**Thread safety.** Only one read may be in progress at a time. Synchronous and
asynchronous reads may follow one another, but a read started while another is in
progress throws `InvalidOperationException`. `Dispose` may be called from another
thread at any time.

**Asynchronous reads.** The asynchronous members need an `XmlReader` whose
`XmlReaderSettings.Async` is `true`. The readers `XcalReader` creates itself are. For
a caller-supplied `XmlReader` created with `Async = false`, `ReadCalendarAsync`
throws `InvalidOperationException`. A read completes synchronously, without
allocating a task, while the `XmlReader` completes synchronously. Cancellation is
checked before each node of the XML document is read.

## Constructors

| Constructor | Notes |
| --- | --- |
| `XcalReader(Stream xcalStream)` | Reads in the encoding the document declares, or UTF-8. The stream is left open when the reader is disposed. Throws `ArgumentNullException` if `xcalStream` is `null`, or `ArgumentException` if the stream can't be read. |
| `XcalReader(Stream xcalStream, IcalOptions options)` | Uses `options.Encoding`, if set, in place of the declared encoding, unless the stream starts with a byte order mark. Throws `ArgumentNullException` if `xcalStream` or `options` is `null`, or `ArgumentException` if the stream can't be read. |
| `XcalReader(string xcalFilePath)` | *(netstandard2.0+/.NET)* Opens the file for asynchronous, sequential reading with a 4 KB `FileStream` buffer, sharing it for reading. Throws `ArgumentNullException` if `xcalFilePath` is `null`. |
| `XcalReader(string xcalFilePath, IcalOptions options)` | *(netstandard2.0+/.NET)* Uses `options.Encoding` as the stream overload does. Throws `ArgumentNullException` if `xcalFilePath` or `options` is `null`. |
| `XcalReader(TextReader reader)` | The text reader is left open when the reader is disposed. Throws `ArgumentNullException` if `reader` is `null`. |
| `XcalReader(TextReader reader, IcalOptions options)` | Throws `ArgumentNullException` if `reader` or `options` is `null`, or `ArgumentException` if `options.Encoding` is set. |
| `XcalReader(XmlReader reader)` | Reads from the `XmlReader`'s current position, with its settings. It is left open when the reader is disposed. Throws `ArgumentNullException` if `reader` is `null`. |
| `XcalReader(XmlReader reader, IcalOptions options)` | Throws `ArgumentNullException` if `reader` or `options` is `null`, or `ArgumentException` if `options.Encoding` is set. |

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
| `ReadCalendar()` | `IcalCalendar?` | Reads the next calendar, or returns `null` at the end of the `icalendar` element. Throws `IcalFileException` if the data isn't valid xCal (not well-formed XML, an element that isn't allowed where it is or isn't valid, a `VERSION` that doesn't include 2.0, or no calendar at all), or if the calendar breaks a rule the options don't accept; in that case reading can continue. Throws `ObjectDisposedException` after disposal, or `InvalidOperationException` if another read is in progress or an earlier read failed. |
| `ReadCalendars()` | `IEnumerable<IcalCalendar>` | Reads the remaining calendars as the sequence is enumerated. Throws `ObjectDisposedException` from this call if the reader has been disposed. During enumeration, throws what `ReadCalendar` throws. |

### Asynchronous reading

| Method | Returns | Notes |
| --- | --- | --- |
| `ReadCalendarAsync(CancellationToken cancellationToken = default)` | `ValueTask<IcalCalendar?>` | *(netstandard2.0+/.NET)* Reads the next calendar, or `null` at the end of the `icalendar` element. This call throws `ObjectDisposedException` after disposal, and `InvalidOperationException` if another read is in progress, an earlier read failed or was canceled, or the `XmlReader` doesn't allow asynchronous reads. The task throws `IcalFileException` as `ReadCalendar` does, `OperationCanceledException` if canceled, or `ObjectDisposedException` if the reader is disposed during the read. A canceled read leaves the reader unable to continue. |
| `ReadCalendarsAsync(CancellationToken cancellationToken = default)` | `IAsyncEnumerable<IcalCalendar>` | *(netstandard2.0+/.NET)* Reads the remaining calendars as the sequence is enumerated. The token is combined with any token passed to `GetAsyncEnumerator`. Throws `ObjectDisposedException` from this call if the reader has been disposed. During enumeration, throws what `ReadCalendarAsync` throws. |

### Disposal

| Method | Returns | Notes |
| --- | --- | --- |
| `Dispose()` | `void` | Disposes the `XmlReader` the reader created, and the stream or file it opened. A caller-supplied stream, text reader or `XmlReader` is left open. Calling it again has no effect. |
| `DisposeAsync()` | `ValueTask` | *(netstandard2.0+/.NET)* Does what `Dispose` does, and returns a task that has already completed. |

## Applies to

Targets `netstandard1.1`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The `string` file-path constructors, `IAsyncDisposable`, `DisposeAsync` and the
asynchronous reading members are available on `netstandard2.0`, `netstandard2.1`,
and the .NET targets only. On `netstandard1.1`, construct the reader from a `Stream`,
`TextReader` or `XmlReader`. The nullable reference annotations apply to the
`netstandard2.1`, `net8.0`, and `net10.0` builds.

## See also

- [Read and write xCal](../how-to/read-write-xcal.md)
- [Convert between formats](../how-to/convert-formats.md)
- [Read asynchronously](../how-to/read-asynchronously.md)
- [Standards compliance](../concepts/standards-compliance.md)
- [XcalWriter](XcalWriter.md)
- [IcalReader](IcalReader.md)
- [JcalReader](JcalReader.md)
- [IcalCalendar](IcalCalendar.md)
- [IcalOptions](IcalOptions.md)
- [IcalFileException](IcalFileException.md)
- [File.iCal API reference](index.md)
