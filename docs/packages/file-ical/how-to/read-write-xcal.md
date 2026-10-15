---
title: xCal (XML)
parent: File.iCal
grand_parent: Packages
nav_order: 7
---

# Read and write xCal

xCal ([RFC 6321](https://www.rfc-editor.org/rfc/rfc6321)) is the XML form of
iCalendar. `XcalReader` and `XcalWriter` read and write it into the same
component object model (`IcalCalendar`, `IcalEvent` and so on) that `IcalReader`
and `IcalWriter` use for `.ics` files. So a calendar read from either format can be
written in the other.

xCal uses the in-box `XmlReader` and `XmlWriter`, so it needs no extra package and
works on every target framework of `DataStandardizer.File.iCal`.

## Read an xCal document

```csharp
using DataStandardizer.File.iCal;

using var reader = new XcalReader("calendar.xml");
foreach (var calendar in reader.ReadCalendars())
{
    foreach (var calendarEvent in calendar.Events)
    {
        Console.WriteLine($"{calendarEvent.DtStart}: {calendarEvent.Summary}");
    }
}
```

The reader takes a file path, a `Stream`, a `TextReader` or an `XmlReader`, each
with an optional `IcalOptions`. It reads one `vcalendar` element at a time, so a
large document doesn't have to fit in memory.

To read xCal that is part of a larger XML document, such as a CalDAV response, pass
an `XmlReader` positioned on the `icalendar` element. Reading stops at the end of
that element and leaves the `XmlReader` on its end tag.

## Write an xCal document

```csharp
using var writer = new XcalWriter("calendar.xml");
writer.WriteCalendars(calendars);
```

The writer takes a file path, a `Stream`, a `TextWriter` or an `XmlWriter`. It
writes the `icalendar` root element with the first calendar and ends it when the
writer is disposed, so dispose the writer, with `using` or `Dispose`, before you read
the output. A writer that writes no calendars writes nothing.

The output is indented UTF-8 without a byte order mark. `IcalOptions.Encoding`
changes the encoding, and `IcalOptions.LineBreak` the line breaks.

## Convert between `.ics` and xCal

Read with one format and write with the other:

```csharp
using (var reader = new IcalReader("calendar.ics"))
using (var writer = new XcalWriter("calendar.xml"))
{
    writer.WriteCalendars(reader.ReadCalendars());
}
```

A round trip keeps every property, parameter and value. It doesn't keep XML
formatting, such as white space, comments or namespace prefixes. A few values can
come back spelled differently but with the same meaning:

- TEXT escaping is applied again, so an escaped `\N` comes back as `\n`.
- BOOLEAN values come back in upper case.
- A `VALUE` parameter comes back after the property's other parameters.
- A `VALUE` parameter that names the property's default type, such as
  `DTSTART;VALUE=DATE-TIME`, doesn't come back, since xCal can't tell it apart from
  none.

## How iCalendar maps to xCal

| iCalendar | xCal |
| --- | --- |
| Component, property and parameter names | The same names in lower case, in the namespace `urn:ietf:params:xml:ns:icalendar-2.0`. Names are converted to upper case when read. |
| A value | An element named for its value type, such as `<date-time>2011-05-17T12:00:00Z</date-time>`. Dates, times and UTC offsets use the extended forms of XML Schema. |
| The `VALUE` parameter | Not written, since the value element gives the type. When reading, it is added if the type isn't the property's default. |
| A value whose type isn't known, such as `X-FOO` with no `VALUE` | An `<unknown>` element holding the raw text, so it converts without loss. |
| BINARY | A `<binary>` element. When reading, `ENCODING=BASE64` is added if the property has no ENCODING parameter, as RFC 5545 requires. |
| CATEGORIES, RESOURCES, EXDATE, RDATE and FREEBUSY | One value element for each item in the list. |
| PERIOD | A `<period>` element with `<start>`, and `<end>` or `<duration>`. |
| RECUR | A `<recur>` element with one element for each rule part, and one for each value of a BYxxx part. The rule parts keep their order. |
| GEO | `<latitude>` and `<longitude>` elements. |
| REQUEST-STATUS | `<code>`, `<description>` and, if present, `<data>` elements. |

Elements in other XML namespaces are extensions. The reader skips them and their
content.

## Value types in xCal form

Each value type parses and formats its xCal form with `IcalValueFormat.Xml`, the
same way it does the `.ics` form with `IcalValueFormat.Text`:

```csharp
var start = IcalDateTime.Parse("2011-05-17T12:00:00Z", IcalValueFormat.Xml);
var text = start.ToString(IcalValueFormat.Text);   // "20110517T120000Z"
```

- **TEXT:** values have no escaping.
- **BOOLEAN:** values are `true` or `false`.
- **Parsing:** whatever the XML Schema type allows is accepted, such as white space
  around a number or inside base64 data.
- **PERIOD:** its xCal form joins the start and the end or duration with a solidus,
  as the `.ics` form does.
- **RECUR:** has no single xCal string, because each rule part is its own
  element. So `IcalRecurrenceRule` throws `NotSupportedException` for
  `IcalValueFormat.Xml`.

## Errors and compliance

The checks are on by default, as they are for `.ics`:

- **Conformance:** each calendar is checked against RFC 5545 when it is read and
  before it is written. A calendar that breaks a rule throws `IcalFileException`
  unless an option, such as `AllowMissingRequiredProperties`, accepts that kind of
  problem.
- **Invalid values:** a value element whose text isn't valid for its type is kept,
  and the check reports it unless `AllowInvalidValues` is set. When writing, a value
  that isn't valid for its type is written in an `<unknown>` element with its `VALUE`
  parameter.
- **XML errors:** XML that isn't well-formed, or doesn't follow the structure of
  xCal, throws `IcalFileException`. The exception's `Data` holds the line of the XML
  document under `LineNumber` and the position on that line under `LinePosition`. A
  document type definition (DTD) isn't allowed.

## Asynchronous reading and writing

`ReadCalendarAsync`, `ReadCalendarsAsync`, `WriteCalendarAsync`,
`WriteCalendarsAsync` and `FlushAsync` work as they do on `IcalReader` and
`IcalWriter`. They need .NET Standard 2.0 or later.

An `XmlReader` or `XmlWriter` that you supply must be created with
`Async = true` in its settings to use them. Readers and writers that
`XcalReader` and `XcalWriter` create themselves already are.

## Limitations

- **The `XML` property:** extension elements in another namespace are skipped, not
  converted to the `XML` property that RFC 6321 §4.2 describes.
- **Base64 values:** a non-binary value with `ENCODING=BASE64` is written as it is,
  not base64-decoded.
- **Path constructors:** the file-path constructors need .NET Standard 2.0 or later.
  On .NET Standard 1.1, use a `Stream`, `TextReader`/`TextWriter` or
  `XmlReader`/`XmlWriter`.

## See also

- [Convert between .ics, jCal and xCal](convert-formats.md)
