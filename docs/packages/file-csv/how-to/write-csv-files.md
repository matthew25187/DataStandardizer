---
title: Write CSV files
parent: File.CSV
grand_parent: Packages
nav_order: 4
---

# Write CSV files

Write record lines — and an optional header line — to a CSV file with a
`CsvFileWriter<TRecordLine>`.

Writing CSV files is similar to reading files. The CSV writer can write directly
to a file or make use of a stream or text writer. Records are then written to
the file according to the configuration in effect.

## Write records

If your file does not have a header line, you can just output your record lines
directly to the file.

```csharp
var lines = new List<ICsvFileLine>();
// load lines

using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>("file_name.csv"))
{
    foreach (var line in lines)
    {
        csvWriter.WriteLine(line);
    }
}
```

## Write a header

The header line is just a different type of line to write to the CSV file. It is
up to your application to make sure the header line is written first, before the
record lines.

```csharp
var headerLine = new CsvFileHeaderLine
{
    { "person_id", "person_id" },
    { "person_name", "person_name" },
    { "person_description", "person_description" }
};
var lines = new List<ICsvFileLine>();
// load lines

using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>("file_name.csv"))
{
    csvWriter.WriteLine(headerLine);    // write the header line first

    foreach (var line in lines)
    {
        csvWriter.WriteLine(line);
    }
}
```

When a header line has been written, the writer uses its field names to order
the fields of the record lines that follow.
