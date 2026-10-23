---
title: Convert between .ics, jCal and xCal
parent: File.iCal
grand_parent: Packages
nav_order: 5
---

# Convert between .ics, jCal and xCal

All three formats read into, and write from, the same component object model:
`IcalCalendar`, `IcalEvent` and so on. So to convert, read with the reader for one
format and write with the writer for another. There is no separate converter.

| Format | Reader | Writer |
| --- | --- | --- |
| `.ics` (RFC 5545) | `IcalReader` | `IcalWriter` |
| xCal, XML (RFC 6321) | `XcalReader` | `XcalWriter` |
| jCal, JSON (RFC 7265) | `JcalReader` | `JcalWriter` |

> **Applies to:** `JcalReader` and `JcalWriter` need .NET Standard 2.0 or later.
> They aren't available on .NET Standard 1.1. The file-path constructors used below
> need .NET Standard 2.0 or later too; on .NET Standard 1.1, use a `Stream`, or for
> `.ics` and xCal a `TextReader` or `TextWriter`.

## .ics to jCal

```csharp
using DataStandardizer.File.iCal;

using (var reader = new IcalReader("calendar.ics"))
using (var writer = new JcalWriter("calendar.json"))
{
    writer.WriteCalendars(reader.ReadCalendars());
}
```

## .ics to xCal

```csharp
using (var reader = new IcalReader("calendar.ics"))
using (var writer = new XcalWriter("calendar.xml"))
{
    writer.WriteCalendars(reader.ReadCalendars());
}
```

## jCal to .ics

```csharp
using (var reader = new JcalReader("calendar.json"))
using (var writer = new IcalWriter("calendar.ics"))
{
    writer.WriteCalendars(reader.ReadCalendars());
}
```

## xCal to jCal

```csharp
using (var reader = new XcalReader("calendar.xml"))
using (var writer = new JcalWriter("calendar.json"))
{
    writer.WriteCalendars(reader.ReadCalendars());
}
```

The other directions work the same way.

`ReadCalendars` reads one calendar at a time as `WriteCalendars` enumerates it, so
a large document doesn't have to fit in memory. The jCal and xCal writers finish the
document when they are disposed, so dispose the writer before you read the output.

## Convert asynchronously

Each writer's `WriteCalendarsAsync` accepts the `IAsyncEnumerable<IcalCalendar>`
that a reader's `ReadCalendarsAsync` returns:

```csharp
await using var reader = new IcalReader("calendar.ics");
await using var writer = new JcalWriter("calendar.json");
await writer.WriteCalendarsAsync(reader.ReadCalendarsAsync(cancellationToken), null, cancellationToken);
```

See [Read asynchronously](read-asynchronously.md).

## Options and checks

Every reader and writer checks each calendar against RFC 5545, whatever the
format. A calendar that a reader accepts only with an option, such as
`AllowUndefinedTimeZones`, is refused by the writer unless you give it the same
option:

```csharp
var options = new IcalOptions { AllowUndefinedTimeZones = true };
using (var reader = new IcalReader("feed.ics", options))
using (var writer = new JcalWriter("feed.json", options))
{
    writer.WriteCalendars(reader.ReadCalendars());
}
```

See [Read non-compliant feeds](read-non-compliant-feeds.md).

## What a round trip keeps

Converting from one format to another and back keeps every component, property,
parameter and value. It doesn't keep:

- **Layout:** where `.ics` lines were folded, XML white space, comments and namespace
  prefixes, or JSON formatting and the order of an object's keys.
- **Spelling:** a few values come back with the same meaning but spelled
  differently. For example, TEXT escaping is applied again, so `\N` comes back as
  `\n`, and BOOLEAN values come back in upper case.
- **The `VALUE` parameter as written:** after a trip through jCal or xCal, a `VALUE`
  parameter comes back after the property's other parameters, and one that names
  the property's default type doesn't come back at all.

The full lists are in [Read and write xCal](read-write-xcal.md) and
[Read and write jCal](read-write-jcal.md), along with how each part of iCalendar maps
to each format.

## Related

- [Read and write .ics files](read-write-ics-files.md)
- [Read and write xCal](read-write-xcal.md)
- [Read and write jCal](read-write-jcal.md)
- [IcalReader](../reference/IcalReader.md), [XcalReader](../reference/XcalReader.md),
  [JcalReader](../reference/JcalReader.md), [IcalWriter](../reference/IcalWriter.md),
  [XcalWriter](../reference/XcalWriter.md) and [JcalWriter](../reference/JcalWriter.md)
