# DataStandardizer.File package

Support for standards-based file formats.

## CSV

### Features

- Configurable

    Supports both declarative and imperative mapping to relate fields in your files to properties on your objects.

- Wide runtime support

    Supports .Net Standard 1.x and 2.0 for use in legacy applications, as well as in-support modern .Net runtimes.

- RFC 4180 compliance

    Supports both reading and writing files compliant with the CSV file standard RFC 4180.

- Compatibility

    Will work with RFC 4180-compliant files by default but can be configured to work with common variants in CSV formatting.

- Open source

    Available for free commercial and personal use under the 3-clause BSD license.

- Efficient processing

    Records can be processed as they are read from a file so they don't need to be stored in memory for later use.

- Flexible

    You can make use of the provided CSV record objects to deal with raw CSV field values or make use of your own with strongly-typed properties for field access.

### How to...

Choose from the following topics.

- [Prerequisites](rfc1480-standard/csv-prerequisites.md)
- [Reading CSV files](rfc1480-standard/read-csv-files.md)
- [Writing CSV files](rfc1480-standard/write-csv-files.md)
- [Configuration](rfc1480-standard/csv-configuration.md)
- [Mapping](rfc1480-standard/map-csv-files.md)