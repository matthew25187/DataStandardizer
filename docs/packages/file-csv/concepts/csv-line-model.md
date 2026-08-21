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

## A line is an ordered dictionary of fields

Every CSV line implements the `ICsvFileLine` interface, which is simply an
`IOrderedDictionary` — an ordered map from field name (key) to field value:

```text
ICsvFileLine : IOrderedDictionary        // ordered name → value field bag
└── CsvFileLineBase (abstract)           // backs the bag with an OrderedDictionary
    ├── CsvFileHeaderLine (sealed)       // a header line; values are the field names
    └── CsvFileRecordLine                // a data record; base for custom models
```

Because a line *is* an ordered dictionary, you can index into it by field name
to read a raw value, and the field order is preserved — which is what lets the
reader and writer line columns up consistently:

```csharp
ICsvFileLine line = /* read from a file */;
var name = line["person_name"] as string;
```

`CsvFileLineBase` holds the actual fields in a private `OrderedDictionary` (the
"field bag") and provides the protected `GetFieldValue` / `SetFieldValue`
helpers, the `Add(key, value)` method, and the full `IOrderedDictionary`
surface.

## Header lines

`CsvFileHeaderLine` is a sealed line whose field *values* are the field names
themselves. When the reader is configured with
`CsvFileHeaderHandling.Use`, it materialises the first line as a
`CsvFileHeaderLine`, adding each name as both key and value, and checks for
duplicate names (throwing a `CsvFileException` if any are found). Its
`FieldNames` property exposes the names as an `IReadOnlyList<string>`, which the
reader then uses to name the fields of subsequent record lines.

How the header is treated is governed by the `CsvFileHeaderHandling` enum:

| Value | Meaning |
| --- | --- |
| `None` | The file has no header line (the default). |
| `Use` | The first line is a header; its values become the field names. |
| `Ignore` | The first line is a header but is skipped; supply names via a `CsvFileHeader` delegate instead. |

## Record lines

`CsvFileRecordLine` is the base type for data records. Used directly, it carries
raw field values you access by name through the dictionary indexer. Its real
purpose, though, is to be subclassed so individual fields can be exposed as
strongly-typed properties.

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
