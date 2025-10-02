# Reading CSV files

The reading of CSV files has been designed to be as easy to do as reading regular text files.  There are several options available for how the CSV records are materialised and the functionality they offer for working with individual fields.

## Load CSV lines with raw values

To read a CSV file using the default configuration, you can process the file in a simple loop.
```
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

If your CSV file has a header line, you must advise the reader to expect a header.
```
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

N.B. The reader will automatically return a line of a type corresponding to what it was expecting.  Once you've configured the reader to expect a header line, you don't need to instruct the reader to read the line differently depending on what type of line will be read.

Once you have acquired a line from a CSV file you can access its individual fields to make decisions on how to handle the line or extract its data for further processing.  For example,
```
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

## Load CSV lines using custom record model

You can define your own record line model that uses properties to access indivisual fields. These custom record line models will derive from the base `CsvFileRecordLine` implementation.
```
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

To use this custom line model, you would then specify this type when instantiating the CSV reader.  The field names, in this case, would match the property names on the line model.
```
using (var streamReader = new StreamReader("file_name.csv"))
using (var csvReader = new CsvFileReader<MyRecordLine>(streamReader))
{
    var line = csvReader.ReadLine();
    while (line is not null)
    {
        if (line is not MyRecordLine myLine)
            continue;

        if (myLine.Id > 0)
        {
            // process line with valid identifier
        }

        line = csvReader.ReadLine();
    }
}
```

If you want to customise the field names associated with properties on the record line model, you can specify the field names in the metadata associated with the properties.
```
public class MyRecordLine : CsvFileRecordLine
{
    [CsvField("person_id")]
    public int Id
    {
        get => GetPropertyValue<int>();
        set => SetPropertyValue(value);
    }

    [CsvField("person_name")]
    public string? Name
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }

    [CsvField("person_description")]
    public string? Description
    {
        get => GetPropertyValue<string?>();
        set => SetPropertyValue(value);
    }
}
```
