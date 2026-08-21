---
title: File.CSV
parent: Packages
nav_order: 3
has_children: true
---

# DataStandardizer.File.CSV

Reading and writing of CSV files compliant with the RFC 4180 standard, with
configurable handling of the common real-world variations on that format.

```shell
dotnet add package DataStandardizer.File.CSV
```

## Standards

| Standard | What it provides |
| --- | --- |
| **RFC 4180** | *Common Format and MIME Type for Comma-Separated Values (CSV) Files* — the de facto standard for CSV: comma-delimited fields, CRLF line breaks, and double-quote escaping. |

The reader and writer behave in an RFC 4180–compliant way by default, but every
deviation the standard leaves room for in practice — alternative delimiters,
line breaks, quoting rules, headers — can be configured.

## Platform support

Targets .NET Standard 1.x and 2.0 for use in legacy applications, as well as
in-support modern .NET runtimes.

> **Applies to:** the `string` file-path constructors on `CsvFileReader<T>` /
> `CsvFileWriter<T>` require .NET Standard 2.0 or a .NET Core / modern .NET
> target; on .NET Standard 1.3 construct the reader/writer from a `Stream` or a
> `TextReader` / `TextWriter`. The `with`-expression syntax shown in some
> examples requires .NET 5 or later (`CsvFileOptions` is a `record` there and a
> plain `class` on earlier targets).

## How-to guides

- [Prerequisites](how-to/prerequisites.md)
- [Configure a reader or writer](how-to/configure-csv.md)
- [Read CSV files](how-to/read-csv-files.md)
- [Write CSV files](how-to/write-csv-files.md)
- [Map CSV files](how-to/map-csv-files.md)

## Concepts

- [The CSV line model](concepts/csv-line-model.md)
- [Field mapping](concepts/field-mapping.md)

## Tutorial

- [Normalize a non-standard CSV file](tutorial/normalize-a-csv.md)

## Reference

- [API reference](reference/index.md)
