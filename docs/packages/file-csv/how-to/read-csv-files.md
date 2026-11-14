---
title: Read CSV files
parent: File.CSV
grand_parent: Packages
nav_order: 3
---

# Read CSV files

Read a CSV file line by line with a `CsvFileReader<TRecordLine>`, working with
either raw field values or your own strongly-typed record model.

The reading of CSV files has been designed to be as easy to do as reading
regular text files. There are several options available for how the CSV records
are materialised and the functionality they offer for working with individual
fields.

## Load CSV lines with raw values

To read a CSV file using the default configuration, you can process the file in
a simple loop.

```csharp
using (var streamReader = new StreamReader(@"file_name.csv"))
using (var csvReader = new CsvFileReader<CsvFileRecordLine>(streamReader))
{
    var line = csvReader.ReadLine();
    while (line is not null)
    {
        // process line

        line = csvReader.ReadLine();
    }
}
```

You can also construct the reader from a file path or a `Stream`, which lets the
package create the `StreamReader` and size its I/O buffer. A `TextReader` you
supply, as above, keeps control of its own buffering.

If your CSV file has a header line, you must advise the reader to expect a
header.

```csharp
var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };

using (var streamReader = new StreamReader(@"file_name.csv"))
using (var csvReader = new CsvFileReader<CsvFileRecordLine>(streamReader, options))
{
    var line = csvReader.ReadLine();
    while (line is not null)
    {
        if (line is CsvFileHeaderLine headerLine)
        {
            // process header line
        }
        else if (line is CsvFileRecordLine recordLine)
        {
            // process record line
        }

        line = csvReader.ReadLine();
    }
}
```

N.B. The reader will automatically return a line of a type corresponding to what
it was expecting. Once you've configured the reader to expect a header line, you
don't need to instruct the reader to read the line differently depending on what
type of line will be read.

Once you have acquired a line from a CSV file you can access its individual
fields to make decisions on how to handle the line or extract its data for
further processing. For example:

```csharp
var lines = new List<ICsvFileLine>();

// load lines from CSV file

foreach (var line in lines)
{
    if (string.Equals(line["field_name"] as string, bool.TrueString, StringComparison.OrdinalIgnoreCase))
    {
        // process line if it has a "true" value in a specific field
    }
}
```

## Load CSV lines using a custom record model

You can define your own record line model that uses properties to access
individual fields. These custom record line models will derive from the base
`CsvFileRecordLine` implementation.

The reader stores each field's text as a `string`. It converts a field to its
property's type only when the property is mapped to the field, so a property of
any type other than `string` needs a mapping. The simplest is a
`[CsvFieldMapping]` attribute, which maps the property to the field with the
property's name.

```csharp
public class MyRecordLine : CsvFileRecordLine
{
    [CsvFieldMapping]
    public int Id
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    public string? Name
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    public string? Description
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }
}
```

To use this custom line model, you would then specify this type when
instantiating the CSV reader. The field names, in this case, would match the
property names on the line model, so the file needs a header line that names
its fields `Id`, `Name` and `Description`, and the reader needs to be told to
use it.

```csharp
var options = new CsvFileOptions { HeaderHandling = CsvFileHeaderHandling.Use };

using (var streamReader = new StreamReader("file_name.csv"))
using (var csvReader = new CsvFileReader<MyRecordLine>(streamReader, options))
{
    var line = csvReader.ReadLine();
    while (line is not null)
    {
        if (line is not MyRecordLine myLine)
        {
            line = csvReader.ReadLine();
            continue;
        }

        if (myLine.Id > 0)
        {
            // process line with valid identifier
        }

        line = csvReader.ReadLine();
    }
}
```

Without a header line, the reader names the fields `Field 1`, `Field 2` and so
on, so no property finds its field by name. Map each property to a field by its
position instead, with `[CsvFieldMapping(0)]` and so on.

A property that isn't mapped reads the field's text as it is. If the property's
type is anything other than `string`, such as the `int` of `Id`, reading it
throws an `InvalidCastException`, because the field holds a `string` that the
property can't return. A property whose field the line doesn't have, or whose
value is `null`, reads as the default value of its type.

If you want to customise the field names associated with properties on the
record line model, you can name the fields in the mappings.

```csharp
public class MyRecordLine : CsvFileRecordLine
{
    [CsvFieldMapping("person_id")]
    public int Id
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    [CsvFieldMapping("person_name")]
    public string? Name
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    [CsvFieldMapping("person_description")]
    public string? Description
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }
}
```

A `[CsvField("person_id")]` attribute also names a property's field, but it
doesn't map the property, so the reader doesn't convert the field's value. Use it
only on a `string` property. See [Map CSV files](map-csv-files.md) for the other
ways to map a property, including a mapper class that keeps the mappings out of
the model.

See [The CSV line model](../concepts/csv-line-model.md) for more on how record
lines and the field bag behind them work.
