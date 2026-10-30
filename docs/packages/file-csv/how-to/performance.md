---
title: Performance
parent: File.CSV
grand_parent: Packages
nav_order: 6
---

# Performance

Get the best throughput from `CsvFileReader<TRecordLine>` and
`CsvFileWriter<TRecordLine>`, and understand what the reader allocates for each
line it reads.

The reader scans its input once, a block at a time, and both the reader and the
writer work out how each field is mapped the first time they need to, then reuse
that plan for every line. The guidance below follows from how that work is done.

## Let the package create the stream reader or writer

Construct the reader or writer from a file path or a `Stream` where you can.
The package then creates the `StreamReader` or `StreamWriter` itself, with a
16 KB buffer, and when you give it a file path it opens the `FileStream` with a
16 KB buffer too. The defaults it replaces are 1 KB for the stream reader or
writer and 4 KB for the file stream.

```csharp
using (var csvReader = new CsvFileReader<CsvFileRecordLine>(@"file_name.csv", options))
{
    // read lines
}
```

A `TextReader` or `TextWriter` you supply is used as it is, so it keeps control of
its own buffering. Pass one when you need a buffer size, encoding or sharing mode
of your own.

> **Applies to:** the file-path constructors require .NET Standard 2.0 or later,
> or a .NET Core / modern .NET target. On .NET Standard 1.3, construct the reader
> or writer from a `Stream`.

## Read or write a whole file with one instance

A reader or writer resolves the mapper for its record-line type, and builds the
plan that maps fields to properties, the first time it reads or writes a record
line. It keeps both for the rest of the file. Use one reader or writer for a whole
file rather than creating a new one part-way through.

## Register mappers once

Mappers are held in a process-wide cache keyed by the record-line type.

- **Declarative mappers**, built from `[CsvFieldMapping]` attributes on the
  record line model, are created the first time the type is read or written, then
  cached for the life of the process.
- **Imperative mappers**, registered with `RegisterMapper<TMapper>()`, stay
  registered until you unregister them. Register each one once, for example at
  application start-up. Registering a mapper that is already registered does
  nothing.

Registering or unregistering a mapper, for any type, makes every open reader and
writer resolve its mapper and build its field plan again on its next record line.
Don't register or unregister mappers in the middle of reading or writing a file.

Both caches are safe to use from several threads at once.

See [Map CSV files](map-csv-files.md) for both kinds of mapper.

## Use typed record line models freely

The property-to-field-name lookup behind `GetPropertyValue<T>()` and
`SetPropertyValue(value)` on a custom record line model is resolved once per
model type and property, then cached, so a typed property no longer does any
reflection when you read or write it, and reading one allocates nothing.

What remains is a cache lookup on each access. In the package's benchmarks a
typed property read takes about 19 ns and a write about 22 ns, against about 7 ns
and 13 ns for the same field through the line's indexer. The difference is about
10 ns per access, so a typed model is only worth giving up in a tight loop over
lines already in memory that does little else with each field.

## What the reader allocates per line

`ReadLine` is forward-only. It returns a new line object for each line it reads
and keeps no reference to lines it has already returned, so a line you have
finished with can be garbage-collected straight away. Memory use doesn't grow
with the number of lines in the file, unless you keep the lines yourself.

For each line, the reader allocates:

- one line object (a `CsvFileHeaderLine`, or your `TRecordLine`), with the field
  bag that holds its fields; and
- one string for each field value that isn't empty. Empty fields share
  `string.Empty`.

The reader's own buffers (the block it scans and the list it collects field
values in) are created once and reused for every line.

On a mapped model, the value converted for each mapped property is stored in the
field bag as an `object`, so a value-type property, such as an `int` or a
`decimal`, adds one boxed value per field.

> **Applies to:** on the `net10.0` build, the field bag is the generic
> `OrderedDictionary<string, object?>`, which stores each field inline. On the
> `netstandard1.3`, `netstandard2.0`, `netstandard2.1` and `net8.0` builds, it is
> the non-generic `OrderedDictionary`, which also allocates an entry object for
> each field. The fields and their order are the same on every target.

## Measured results

The figures below come from the
[File.CSV benchmark project](https://github.com/matthew25187/DataStandardizer/tree/master/benchmarks/DataStandardizer.File.CSV.Benchmarks).
Each scenario reads or writes 100,000 record lines plus a header, with six
fields per line, to or from a `MemoryStream`, so disk I/O is excluded. The model
benchmarks work on the same lines, already in memory. The benchmark README
describes the data and every scenario.

| Scenario | Benchmark | Version 1.1.1 | After the performance work |
| --- | --- | ---: | ---: |
| Read raw lines, header used | `ReadBenchmarks.Read_Raw` | 143.6 ms, 1,006 MB | 16.7 ms, 55.3 MB |
| Read a mapped model (imperative mapper) | `ReadBenchmarks.Read_Mapped` | 481.6 ms, 1,742 MB | 28.5 ms, 66.0 MB |
| Write raw lines | `WriteBenchmarks.Write_Raw` | 53.2 ms, 112.9 MB | 15.2 ms, 22 KB |
| Write a mapped model (imperative mapper) | `WriteBenchmarks.Write_Mapped` | 73.9 ms, 145.0 MB | 22.5 ms, 6.9 MB |
| 600,000 typed property reads | `ModelBenchmarks.Model_PropertyGet` | 34.6 ms, 359 B | 11.3 ms, 0 B |
| 600,000 typed property writes | `ModelBenchmarks.Model_PropertySet` | 69.3 ms, 23.7 MB | 13.3 ms, 5.3 MB |

Each cell gives the mean time for the whole scenario and the memory allocated.

- **Version 1.1.1** was measured on commit `2cc6c77`, which added the benchmark
  project. Its `DataStandardizer.File.CSV` source is the same as version 1.1.1.
- **After the performance work** was measured on commit `7fb24e5`, which includes
  every change released in version 1.2.0.
- Both were run one straight after the other on the same machine: Windows 11,
  Intel Core i9-14900K, .NET SDK 10.0.114, .NET 10.0.14 runtime, using the
  `net10.0` build of the package.

Your own figures will depend on your hardware, your data and the target
framework you build for.

To measure the package on your own machine, or to compare a change, run the
benchmark project. Its README explains how to run it and how to report results.

## Related

- [Read CSV files](read-csv-files.md)
- [Write CSV files](write-csv-files.md)
- [Map CSV files](map-csv-files.md)
- [The CSV line model](../concepts/csv-line-model.md)
- [API reference](../reference/index.md)
