# Writing CSV files

Writing CSV files is similar to reading files.  The CSV writer can write directly to a file or make use of a stream or text writer.  Records are then written to the file according to the configuration in effect.

## Write records

If your file does not have a header line, you can just output your record lines directly to the file.
```
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

## Write header

The header line is just a different type of line to write to the CSV file.  It is up to your application to make sure the header line is written first, before the record lines.
```
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