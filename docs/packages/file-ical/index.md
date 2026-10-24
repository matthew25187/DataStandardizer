---
title: File.iCal
parent: Packages
nav_order: 4
has_children: true
---

# DataStandardizer.File.iCal

Reading and writing of iCalendar data, in the `.ics` format and in its JSON (jCal)
and XML (xCal) forms, with recurrence expansion and time-zone resolution.

```shell
dotnet add package DataStandardizer.File.iCal
```

## Standards

| Standard | What it provides |
| --- | --- |
| **RFC 5545** | *Internet Calendaring and Scheduling Core Object Specification (iCalendar)*: the `.ics` content-line syntax, the VCALENDAR, VEVENT, VTODO, VJOURNAL, VFREEBUSY, VTIMEZONE and VALARM components, value types and recurrence rules. |
| **RFC 6868** | *Parameter Value Encoding in iCalendar and vCard*: caret encoding of double quotes and line breaks in parameter values. |
| **RFC 7986** | *New Properties for iCalendar*: `NAME`, `DESCRIPTION`, `COLOR`, `IMAGE`, `REFRESH-INTERVAL`, `SOURCE` and others. |
| **RFC 7265** | *jCal: The JSON Format for iCalendar*. |
| **RFC 6321** | *xCal: The XML Format for iCalendar*. |

The readers and writers follow the RFCs by default. A reader rejects input that
breaks a MUST or MUST NOT rule, and a writer refuses to write a model that does.
Each accommodation for common non-compliant input is a separate option that you
turn on by name. See [Standards compliance by default](concepts/standards-compliance.md).

The package doesn't support vCalendar 1.0 (`VERSION:1.0`), iTIP (RFC 5546) or
RSCALE (RFC 7529). A vCalendar 1.0 file is rejected with an error.

## Platform support

Targets .NET Standard 1.1, 2.0 and 2.1, .NET 8 and .NET 10.

> **Applies to:** the async API, the file-path constructors and the `System`
> time-zone resolver need .NET Standard 2.0 or later. jCal (`JcalReader` and
> `JcalWriter`) needs .NET Standard 2.0 or later too. On .NET Standard 1.1,
> read and write synchronously, from a `Stream`, `TextReader` or `TextWriter`.
> `IcalOptions` is a `record` on .NET 5 and later, so `with` expressions work
> there, and a plain `class` on the .NET Standard targets.

See [Platform support](../../overview/platform-support.md) for the full matrix.

## How-to guides

- [Read and write .ics files](how-to/read-write-ics-files.md)
- [Read asynchronously](how-to/read-asynchronously.md)
- [Read with zero copies](how-to/read-zero-copy.md)
- [Expand recurrences](how-to/expand-recurrences.md)
- [Convert between .ics, jCal and xCal](how-to/convert-formats.md)
- [Read non-compliant feeds](how-to/read-non-compliant-feeds.md)
- [Read and write xCal](how-to/read-write-xcal.md)
- [Read and write jCal](how-to/read-write-jcal.md)

## Concepts

- [The layered model](concepts/layered-model.md)
- [Standards compliance by default](concepts/standards-compliance.md)
- [Time-zone resolution](concepts/time-zone-resolution.md)
- [DST gaps and overlaps](concepts/dst-rules.md)

## Reference

- [API reference](reference/index.md)
