---
title: The CSV line model
parent: File.CSV
grand_parent: Packages
nav_order: 10
---

# The CSV line model

A line in a CSV file is represented as an ordered bag of named fields, with two
concrete line types — one for the header and one for records — and an extension
point for your own strongly-typed record models.

All public types here are in the `DataStandardizer.File.CSV` namespace.

## A line is an ordered collection of named fields

Every CSV line implements the `ICsvFileLine` interface: an ordered collection of
fields, each with a `string` name and an `object?` value. Enumerating a line gives
its fields in order, as `KeyValuePair<string, object?>` items:

```text
ICsvFileLine                             // ordered name → value fields
└── CsvFileLineBase (abstract)           // holds the fields
    ├── CsvFileHeaderLine (sealed)       // a header line; values are the field names
    └── CsvFileRecordLine                // a data record; base for custom models
```

You can read or set a field by name or by position, and the order of the fields
is kept, which is what lets the reader and writer line columns up consistently:

```csharp
ICsvFileLine line = /* read from a file */;
var name = line["person_name"] as string;   // by name; null if the line has no such field
var first = line[0];                        // by position: the value of the first field
var position = line.IndexOf("person_name"); // -1 if the line has no such field

foreach (var field in line)
{
    Console.WriteLine($"{field.Key} = {field.Value}");
}
```

The field at position *N* is always the *N*th field of the line. `Insert` puts a
field at a position and moves the fields after it up by one; `RemoveAt` and
`Remove` take a field out and move the fields after it down by one. `Keys` and
`Values` are read-only lists of the names and values, in the same order. Field
names are compared by ordinal, so they are case-sensitive, and a line can't hold
two fields with the same name.

A value is an `object?`, because the fields of one line can hold values of
different types: the raw `string` that the reader read, or the typed value that a
mapping converted it to. Use a pattern or a cast to read a value as its type.

`CsvFileLineBase` holds the fields and provides the protected `GetFieldValue` /
`SetFieldValue` helpers and the public `Add(fieldName, value)` method. It
implements the other members of `ICsvFileLine` explicitly, so they don't clash
with the properties of your own record model; reach them through an
`ICsvFileLine` reference.

> **Applies to:** on the `net10.0` build, the fields are stored in the generic
> `OrderedDictionary<string, object?>`. On the `netstandard1.1`,
> `netstandard2.0` and `netstandard2.1` builds, they are stored in an ordered
> store of the package's own with the same layout. The two behave the same on
> every target: the same positions, the same order and the same exceptions.

## Header lines

`CsvFileHeaderLine` is a sealed line whose field *values* are the field names
themselves. When the reader is configured with
`CsvFileHeaderHandling.Use`, it materialises the first line as a
`CsvFileHeaderLine`, adding each name as both key and value, and checks for
duplicate names (throwing a `CsvFileException` if any are found). Its
`FieldNames` property exposes the names as an `IReadOnlyList<string>`, which the
reader then uses to name the fields of subsequent record lines.

Records can't be read without valid field names. If reading the header line
fails, for example because of duplicate names or an invalid value, or a
`CsvFileHeader` delegate throws, every later `ReadLine` call throws the same
exception and reads no further lines. The reader never treats a record as the
header line in place of the one that failed.

How the header is treated is governed by the `CsvFileHeaderHandling` enum:

| Value | Meaning |
| --- | --- |
| `None` | The file has no header line (the default). |
| `Use` | The first line is a header; its values become the field names. |
| `Ignore` | The first line is a header but is skipped; supply names via a `CsvFileHeader` delegate instead. |

## Record lines

`CsvFileRecordLine` is the base type for data records. Used directly, it carries
raw field values you access by name or position through the `ICsvFileLine`
indexers. Its real purpose, though, is to be subclassed so individual fields can
be exposed as strongly-typed properties.

A custom record model derives from `CsvFileRecordLine` and implements each
property in terms of the protected `GetPropertyValue<T>` /
`SetPropertyValue<T>` helpers. These use `[CallerMemberName]` to discover the
property name and then resolve it to a field name — by default the property name
itself, or the name given by a `[CsvField("...")]` attribute on the property:

```csharp
public class PersonRecordLine : CsvFileRecordLine
{
    [CsvField("person_id")]
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
}
```

`GetPropertyValue<T>` returns the field's value if it is a `T`, and the default
value of `T` if the line has no such field or its value is `null`. It throws an
`InvalidCastException` for a value of any other type. A reader stores the text of
a field that no mapping converts, so a property of any type other than `string`,
such as `Id` here, needs a mapping to be read from a file.

`CsvFileReader<TRecordLine>` is generic over your record type
(`where TRecordLine : CsvFileRecordLine, new()`), so each line it returns is
already an instance of your model with its fields populated. The single
constraint is that the model has a public parameterless constructor — the reader
creates each line with `new TRecordLine()`.

> The property-to-field resolution shown here (`[CsvField]` → property name)
> covers *direct field access*. Richer behaviour — type conversion, validation,
> optional fields, generated/constant values — is layered on through the mapping
> API. See [Field mapping](field-mapping.md).

## Related

- [Read CSV files](../how-to/read-csv-files.md)
- [Field mapping](field-mapping.md)
- [API reference](../reference/index.md)
