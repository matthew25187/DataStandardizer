---
title: Tutorial: normalize a non-standard CSV file
parent: File.CSV
grand_parent: Packages
nav_order: 30
---

# Tutorial: normalize a non-standard CSV file

In this tutorial you'll take a CSV file that uses a non-standard line break and
inconsistent quoting, and rewrite it as a clean, RFC 4180–compliant file —
streaming line by line so the whole file never has to sit in memory. It's a
common first step in any data pipeline: get untidy input into a predictable
shape before anything downstream touches it.

> **Applies to:** this walkthrough uses a `with` expression to copy options,
> which requires .NET 5 or later (`CsvFileOptions` is a `record` there). On
> earlier targets, build the second options object explicitly instead.

## What you'll build

A small program that:

1. Reads `data.csv`, whose lines are separated by a bare `\n` (Unix-style)
   instead of the `\r\n` that RFC 4180 calls for.
2. Writes `normalized.csv` with standard `\r\n` line endings and quoting applied
   only where the standard actually requires it.

## Before you start

Install the package and make sure you have a CSV file to work with:

```shell
dotnet add package DataStandardizer.File.CSV
```

Add the namespaces you'll need:

```csharp
using System.IO;
using DataStandardizer.File.CSV;
```

## Step 1 — Describe the input format

The source file doesn't follow the standard: its lines end in `\n` rather than
`\r\n`. The reader detects line ends using its `TerminatorLineBreak` option, so
tell it what to expect:

```csharp
var inputPath = "data.csv";
var outputPath = "normalized.csv";

var csvInputOptions = new CsvFileOptions
{
    TerminatorLineBreak = "\n"  // source file has non-standard line breaks
};
```

Everything else stays at its default, RFC 4180–compliant value — so the reader
still parses commas as delimiters and honours double-quote escaping.

## Step 2 — Describe the output format

The output should be standard. Copy the input options with a `with` expression
and override just the two settings that differ — the line break and the quoting
rule:

```csharp
var csvOutputOptions = csvInputOptions with
{
    TerminatorLineBreak = "\r\n",                    // standard line breaks on output
    QuoteHandling = CsvFieldQuoteHandling.Required   // quote field values only when needed
};
```

`CsvFieldQuoteHandling.Required` is the standard-compliant choice: the writer
quotes a field only when its value contains a line break, a double-quote, or the
field delimiter, and leaves every other value bare.

## Step 3 — Stream lines from input to output

Open the input and output files and chain a reader and a writer over them. Read
one line at a time and hand each line straight to the writer. Because you read
and write a line at a time, memory use stays flat no matter how large the file
is:

```csharp
using (var input = File.OpenRead(inputPath))
using (var csvReader = new CsvFileReader<CsvFileRecordLine>(input, csvInputOptions))
using (var output = File.Create(outputPath))
using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>(output, csvOutputOptions))
{
    var line = csvReader.ReadLine();
    while (line is not null)
    {
        csvWriter.WriteLine(line);

        line = csvReader.ReadLine();
    }
}
```

`ReadLine()` returns `null` at the end of the stream, which ends the loop. The
`using` statements guarantee the files, reader, and writer are all disposed when
the block exits — see [Prerequisites](../how-to/prerequisites.md) for why that
matters.

## What just happened

- The reader split the input on `\n`, parsing each record into a
  `CsvFileRecordLine`.
- Each line was passed unchanged to the writer, which re-serialised it using the
  output options: `\r\n` terminators and standard quoting.
- The result, `normalized.csv`, is a faithful, RFC 4180–compliant copy of the
  data — ready for any consumer that expects well-formed CSV.

## Where to go next

- Add a header by setting `HeaderHandling = CsvFileHeaderHandling.Use` on the
  input options — see [Configure a reader or writer](../how-to/configure-csv.md).
- Replace `CsvFileRecordLine` with your own strongly-typed model and a mapper to
  transform values, not just reformat them — see
  [Map CSV files](../how-to/map-csv-files.md) and
  [Field mapping](../concepts/field-mapping.md).
