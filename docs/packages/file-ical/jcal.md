---
title: jCal (JSON)
parent: File.iCal
grand_parent: Packages
nav_order: 6
---

# Read and write jCal

jCal ([RFC 7265](https://www.rfc-editor.org/rfc/rfc7265)) is the JSON form of
iCalendar. `JcalReader` and `JcalWriter` read and write it into the same component
object model (`IcalCalendar`, `IcalEvent` and so on) that `IcalReader` and
`IcalWriter` use for `.ics` files, and that `XcalReader` and `XcalWriter` use for
xCal. So a calendar read from any of the three formats can be written in the others.

jCal uses System.Text.Json, so `JcalReader` and `JcalWriter` are available on .NET
Standard 2.0 and later only, **not on .NET Standard 1.1**. On .NET 8 and .NET 10,
System.Text.Json is part of the framework. On .NET Standard 2.0 and 2.1, the package
depends on the System.Text.Json NuGet package, which NuGet installs for you.

## Read a jCal document

```csharp
using DataStandardizer.File.iCal;

using var reader = new JcalReader("calendar.json");
foreach (var calendar in reader.ReadCalendars())
{
    foreach (var calendarEvent in calendar.Events)
    {
        Console.WriteLine($"{calendarEvent.DtStart}: {calendarEvent.Summary}");
    }
}
```

The reader takes a file path or a `Stream`, each with an optional `IcalOptions`.
JSON is always UTF-8, so there's no `TextReader` overload. A byte order mark at the
start is skipped.

A jCal document is either one calendar, `["vcalendar", [...], [...]]`, or an array
of them, `[["vcalendar", ...], ["vcalendar", ...]]`. The reader accepts both. It
reads one calendar at a time, so a large document doesn't have to fit in memory.

## Write a jCal document

```csharp
using var writer = new JcalWriter("calendar.json");
writer.WriteCalendars(calendars);
```

The writer takes a file path or a `Stream`. The output is compact UTF-8 JSON without
a byte order mark.

- **One calendar** is written as its component array, `["vcalendar", ...]`. Most
  jCal consumers expect this form.
- **Two or more calendars** are written as an array of component arrays.

Until the writer knows which of the two it is writing, it holds the first calendar
in memory. It writes the first calendar when the second arrives or when the writer
is disposed, so dispose the writer, with `using` or `Dispose`, before you read the
output. `Flush` doesn't write a calendar that is still held. A writer that writes no
calendars writes nothing.

## Convert between `.ics`, xCal and jCal

Read with one format and write with another:

```csharp
using (var reader = new IcalReader("calendar.ics"))
using (var writer = new JcalWriter("calendar.json"))
{
    writer.WriteCalendars(reader.ReadCalendars());
}
```

A round trip keeps every property, parameter and value. It doesn't keep JSON
formatting or the order of an object's keys. A few values can come back spelled
differently but with the same meaning:

- TEXT escaping is applied again, so an escaped `\N` comes back as `\n`.
- BOOLEAN values come back in upper case.
- INTEGER and FLOAT values are JSON numbers, which have no plus sign or leading
  zeros, so `+7` comes back as `7`.
- A `VALUE` parameter comes back after the property's other parameters.
- A `VALUE` parameter that names the property's default type, such as
  `DTSTART;VALUE=DATE-TIME`, doesn't come back, since jCal can't tell it apart from
  none.

## How iCalendar maps to jCal

| iCalendar | jCal |
| --- | --- |
| A component | An array of three elements: the name in lower case, an array of its properties and an array of its sub-components. The last is there even if it is empty. |
| A property | An array: the name in lower case, an object holding the parameters, the type identifier, such as `"date-time"`, and the value or values. Names are converted to upper case when read. |
| Parameters | An object whose keys are the parameter names in lower case. A parameter with more than one value, such as `DELEGATED-TO`, is an array of strings. |
| The `VALUE` parameter | Not written, since the type identifier gives the type. When reading, it is added if the type isn't the property's default. |
| A value whose type isn't known, such as `X-FOO` with no `VALUE` | The type `unknown`, with the raw text as a string, so it converts without loss. |
| A `VALUE` that names a type RFC 5545 doesn't define, such as `X-MYTYPE` | That type identifier in lower case, with the raw text as a string. When reading, `VALUE=X-MYTYPE` is added back. |
| TEXT, DATE, DATE-TIME, TIME, UTC-OFFSET, DURATION, URI and CAL-ADDRESS | A string. Dates, times and UTC offsets use the extended forms of ISO 8601, such as `"2011-05-17T12:00:00Z"` and `"-05:00"`. |
| BINARY | A string of base64. When reading, `ENCODING=BASE64` is added if the property has no ENCODING parameter, as RFC 5545 requires. |
| BOOLEAN | `true` or `false`. |
| INTEGER and FLOAT | A number. |
| CATEGORIES, RESOURCES, EXDATE, RDATE and FREEBUSY | One value for each item in the list. |
| PERIOD | An array of two strings: the start, and the end or the duration. |
| RECUR | An object with one key for each rule part, such as `{"freq": "WEEKLY", "byday": ["MO", "FR"], "count": 5}`. COUNT and INTERVAL are numbers, and a BYxxx part with more than one value is an array. The rule parts keep their order. |
| GEO | An array of two numbers: the latitude and the longitude. |
| REQUEST-STATUS | An array of two or three strings: the code, the description and, if present, the data. |

When reading, the reader also accepts some forms that other writers produce:

- **PERIOD as one string:** a period as one string with its parts joined by a
  solidus, such as `"1997-03-08T16:00:00Z/P1D"`. The RFC's own Appendix B example
  uses this form.
- **A single value in place of an array:** a single value where an array is allowed,
  such as `"byday": "MO"` or `"delegated-to": "mailto:a@example.com"`.
- **A string for any type:** a string as the value of any type, so that a value that
  doesn't follow its type reaches the conformance check rather than failing the read.

## Value types in jCal form

Each value type parses and formats its jCal form with `IcalValueFormat.Json`, the
same way it does the `.ics` form with `IcalValueFormat.Text`:

```csharp
var start = IcalDateTime.Parse("2011-05-17T12:00:00Z", IcalValueFormat.Json);
var text = start.ToString(IcalValueFormat.Text);   // "20110517T120000Z"
```

The jCal form is the text of the JSON value, with its JSON escaping removed:

- **TEXT:** values have no iCalendar escaping, and a list is one item, since each
  item is its own JSON string.
- **BOOLEAN:** values are `true` or `false`, in lower case.
- **INTEGER and FLOAT:** values are JSON numbers. An INTEGER may have a fraction or
  an exponent as long as the number is a whole one, such as `1.0` or `1e2`.
- **DATE, TIME, DATE-TIME and UTC-OFFSET:** the same as their xCal forms.
- **DURATION, BINARY, URI and CAL-ADDRESS:** the same as their `.ics` forms.
- **PERIOD:** its jCal form joins the start and the end or duration with a solidus,
  as its xCal form does. In a jCal document they are the two strings of an array.
- **RECUR:** has no single jCal string, because it is a JSON object. So
  `IcalRecurrenceRule` throws `NotSupportedException` for `IcalValueFormat.Json`.

`IcalValueFormat.Json` needs no JSON library, so the value types support it on every
target, .NET Standard 1.1 included. Only `JcalReader` and `JcalWriter` need .NET
Standard 2.0 or later.

## Errors and compliance

The checks are on by default, as they are for `.ics`:

- **Conformance:** each calendar is checked against RFC 5545 when it is read and
  before it is written. A calendar that breaks a rule throws `IcalFileException`
  unless an option, such as `AllowMissingRequiredProperties`, accepts that kind of
  problem. The reader can then go on to the next calendar.
- **Invalid values:** a value that isn't valid for its type is kept, and the check
  reports it unless `AllowInvalidValues` is set. When writing, a value that isn't
  valid for its type is written with the type `unknown` and its `VALUE` parameter.
- **JSON errors:** JSON that isn't valid, or doesn't follow the structure of jCal,
  throws `IcalFileException`. Its `Data` holds:
  - `BytePosition`: the offset in bytes of the JSON value at fault.
  - `JsonPath`: where the value has been read, its path, such as `$[2][0][1][3]`
    for the fourth property of the first sub-component.
  - `LineNumber` and `LinePosition`: for JSON that isn't valid, the line and the
    byte position on that line.

  After such an error, the reader can't continue.
- **What the writer refuses:** a calendar that jCal can't hold. That is a name that
  isn't letters, digits and hyphens, two parameters of the same name on one property
  (a JSON object can't hold both), or a value with a lone surrogate. The writer
  checks and encodes each calendar in full before it writes any of it.

## Asynchronous reading and writing

`ReadCalendarAsync`, `ReadCalendarsAsync`, `WriteCalendarAsync`,
`WriteCalendarsAsync`, `FlushAsync` and `DisposeAsync` work as they do on
`IcalReader` and `IcalWriter`.

- **Reading:** the reader works through the data in pooled blocks of bytes, and the
  asynchronous members fill each block with `Stream.ReadAsync`.
- **Writing:** the writer encodes each calendar into a pooled buffer, then the
  asynchronous members write it with `Stream.WriteAsync`.

Neither does any synchronous I/O. Cancellation is checked before each block is read,
and between calendars when writing, so a canceled write never leaves part of a
calendar.

## Limitations

- **Base64 values:** a non-binary value with `ENCODING=BASE64` is written as it is,
  not base64-decoded as RFC 7265 §3.1 asks.
- **Characters outside the Basic Multilingual Plane:** characters such as emoji are
  written as `\u` escape pairs, since System.Text.Json always escapes them. Other
  characters outside ASCII are written as they are.
- **No raw JSON view:** the reader always builds the component object model. There
  is no lower-level, allocation-free view of the JSON.
