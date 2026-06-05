---
title: Map CSV files
parent: File.CSV
grand_parent: Packages
nav_order: 5
---

# Map CSV files

Relate the properties on a model object to fields in a CSV file — declaratively
with attributes or imperatively with a fluent mapper — to control how values are
read and written.

When you are working with custom models that use properties for field accessors,
you can use mappers to relate the properties to the fields in a file. A property
mapping can configure not only the field name for the property but also certain
behaviours that a reader or writer should apply when transferring data into or
out of a line object.

Field mappings can be applied in either of two ways: declaratively or
imperatively. There are pros and cons for both approaches; what type of mapping
you use will depend on your preferences or application architecture
requirements. See [Field mapping](../concepts/field-mapping.md) for the design
behind the fluent API used below.

## Declarative mapping

Declarative mapping involves associating mapping metadata with the properties on
a line model using attributes.

```csharp
public class MyRecordLine : CsvFileRecordLine
{
    [CsvFieldMapping("person_id")]
    [TypeConverter(typeof(Int32Converter))]
    public int Id
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    [CsvFieldMapping("person_name", IsOptional = false)]
    public string? Name
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    [CsvFieldMapping("person_description", IsOptional = true)]
    public string? Description
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }
}
```

This approach to mapping keeps the mapping configuration together with the model
so no separate object is required. However, the technical limitations of
attributes means they can only be used for a limited subset of mapping
functionality.

There is no need to take any additional action to cause this mapping to be used
by a reader or writer; declarative mapping is discovered and used automatically.

## Imperative mapping

Imperative mapping makes use of a mapper implementation that is separate from the
model on which the mapping is established. This can enable a "cleaner" model
implementation allowing you to maintain a separation of concerns.

With this record line model:

```csharp
public class MyRecordLine : CsvFileRecordLine
{
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

You could write a mapper for it that is functionally equivalent to the earlier
declarative mapping example.

```csharp
public class MyRecordLineMapper : CsvFileMapperBase<MyRecordLine>
{
    public MyRecordLineMapper()
    {
        this.Map()
            .Property(x => x.Id)
            .HasFieldName("person_id")
            .ConvertUsing(typeof(Int32Converter));
        this.Map()
            .Property(x => x.Name)
            .HasFieldName("person_name");
        this.Map()
            .Property(x => x.Description)
            .HasFieldName("person_description")
            .IsOptional();
    }
}
```

To use the mapper when reading lines from a file, you would then register the
mapper with the CSV reader (the same goes for using a mapper with a writer).

```csharp
using (var streamReader = new StreamReader("file_name.csv"))
using (var csvReader = new CsvFileReader<MyRecordLine>(streamReader))
{
    csvReader.RegisterMapper<MyRecordLineMapper>();

    var line = csvReader.ReadLine();
    while (line is not null)
    {
        // process line

        line = csvReader.ReadLine();
    }
}
```

This approach to mapping CSV fields allows for the use of the full range of
mapping functionality, but requires the implementation of mapping configuration
in a separate class.

## Convert record lines to custom objects

You may already have your own model to represent the record lines in a CSV file.
In this case, the record lines loaded from the file can be converted to your own
model that is not based on the `CsvFileRecordLine` base implementation.

Given this custom model:

```csharp
public class CustomRecordLine
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}
```

Create a mapper for the model:

```csharp
public class CustomRecordLineMapping : CsvFileCustomMapperBase<CustomRecordLine>
{
    public CustomRecordLineMapping()
    {
        this.Map()
            .Property(x => x.Id)
            .HasFieldName("person_id")
            .ConvertUsing(typeof(Int32Converter));
        this.Map()
            .Property(x => x.Name)
            .HasFieldName("person_name");
        this.Map()
            .Property(x => x.Description)
            .HasFieldName("person_description")
            .IsOptional();
    }
}
```

N.B. Mappers for custom objects (based on `CsvFileCustomMapperBase`) are
different from mappers for record lines (based on `CsvFileMapperBase`) and not
interchangeable.

You can convert the record lines read from the file to your own model like so:

```csharp
using (var streamReader = new StreamReader("file_name.csv"))
using (var csvReader = new CsvFileReader<MyRecordLine>(streamReader))
{
    var mapper = new CustomRecordLineMapping();

    var line = csvReader.ReadLine();
    while (line is not null)
    {
        if (line is MyRecordLine recordLine)
        {
            var myObject = recordLine.ToObject(mapper);
            if (myObject.Id > 0)
            {
                // process object
            }
        }

        line = csvReader.ReadLine();
    }
}
```

## Convert custom objects to record lines

If you have a collection of records to write to a CSV file, you can use a mapper
to convert the record objects to CSV record lines for use with a CSV writer.

For example, using this mapper:

```csharp
public class MyRecordLineMapper : CsvFileMapperBase<MyRecordLine>
{
    public MyRecordLineMapper()
    {
        this.Map()
            .Property(x => x.Id)
            .HasFieldName("person_id")
            .ConvertUsing(typeof(Int32Converter));
        this.Map()
            .Property(x => x.Name)
            .HasFieldName("person_name");
        this.Map()
            .Property(x => x.Description)
            .HasFieldName("person_description")
            .IsOptional();
    }
}
```

You could then convert your objects for writing like so:

```csharp
var myRecords = new List<CustomRecordLine>();
// load lines

var mapper = new MyRecordLineMapper();
using (var csvWriter = new CsvFileWriter<CsvFileRecordLine>("file_name.csv"))
{
    foreach (var myRecord in myRecords)
    {
        var line = myRecord.ToCsvLine(mapper);
        csvWriter.WriteLine(line);
    }
}
```
