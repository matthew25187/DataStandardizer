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

Constructing the writer from a file path or a `Stream` lets the package create the
`StreamWriter` and size its I/O buffer. A `TextWriter` you supply keeps control of
its own buffering.

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

## Field positions

A field mapped by index, with `HasFieldIndex` or `[CsvFieldMapping(index)]`, is
written at its zero-based index, on the header line and on every record line,
so a reader with the same mapper reads it from there. The writer writes whole
lines, so every other field, mapped by name or not mapped at all, fills the
positions that are left, in turn from position 0:

- With a header line, they are the header line's other fields: first those
  mapped by name, then those that no mapping covers, each in the header line's
  order.
- Without one, they are the fields mapped by name, in mapper order, followed by
  the fields of the first record line that no mapping covers.

A position that is still free below the highest index is written as a blank
field. With this mapper:

```csharp
this.Map().Property(x => x.FirstName);
this.Map().Property(x => x.LastName);
this.Map().Property(x => x.Title).HasFieldIndex(0);
this.Map().Property(x => x.City);
this.Map().Property(x => x.Country).HasFieldIndex(9);
```

the header line `FirstName,Phone,LastName,Title,City,Country` is written as
`Title,FirstName,LastName,City,Phone,,,,,Country`, with the unmapped `Phone`
after the mapped fields, and a record is written in the same layout, such as
`Dr,Ann,Lee,Oslo,555,,,,,Norway`. Without a header line, the mapped fields are
written the same way. If the header line doesn't have the name of a field mapped
by index, the mapping's field name, or else its property name, is written at the
index.

A field mapped by name that the header line doesn't have is left out if the
mapping is optional. If it's required, and has no constant or generated value,
writing the header line throws `CsvFileException`, as a reader with the mapper
couldn't read the file.
