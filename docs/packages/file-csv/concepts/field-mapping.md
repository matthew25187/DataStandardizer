---
title: Field mapping
parent: File.CSV
grand_parent: Packages
nav_order: 11
---

# Field mapping

Field mapping connects the properties of a model object to the fields of a CSV
file, and configures how each value is converted, validated, or generated as it
moves between them. A fluent builder makes that configuration read like a
sentence.

All public types here are in the `DataStandardizer.File.CSV` namespace.

## What a mapping holds

Each property-to-field relationship is captured by a `CsvFieldMapping`. It
records the property type and the various behaviours the reader and writer
apply: the target field (`FieldName` or `FieldIndex`), whether the field is
optional, a constant or generated value, type/delegate converters, and a
validator. You don't populate a `CsvFieldMapping` directly — its setters are
internal — you build it through the fluent API or via attributes.

A complete set of mappings for a model is exposed as an `ICsvFileMapper`, which
is just an `IReadOnlyDictionary<string, CsvFieldMapping>` keyed by property name.
`CsvFileMapper` is the concrete collection the reader and writer consume.

## Two ways to define mappings

**Declaratively**, with `[CsvFieldMapping]` attributes on the model's
properties (optionally alongside `[TypeConverter]`). These are discovered
automatically via reflection — no registration needed. Attributes can express a
useful subset of the configuration: field name or index, optionality, a constant
value, and a type converter.

**Imperatively**, by subclassing one of the mapper base classes and chaining the
fluent builder in the constructor:

- `CsvFileMapperBase<TRecordLine>` — maps a `CsvFileRecordLine`-derived model.
  Register it on a reader or writer with `RegisterMapper<TMapper>()`.
- `CsvFileCustomMapperBase<TModel>` — maps a plain object that does **not**
  derive from `CsvFileRecordLine`, used with the `ToObject` / `ToCsvLine`
  conversion helpers. (The two mapper base types are not interchangeable.)

Both base classes expose a protected `Map()` method that returns a
`CsvFileMappingBuilder<TModel>` to start each property's mapping.

## The fluent builder pipeline

`CsvFileMappingBuilder<TModel>.Property(x => x.SomeProperty)` selects a property
(from a lambda expression) and returns the first step interface. From there the
mapping is configured by chaining methods, each returning the *next* allowed set
of steps. Rather than one big interface, the surface is split into small
step interfaces (`ICsvFieldMapping*Builder`) so that only the operations valid at
each point in the chain are offered:

```text
Property(expr)
  └─ HasFieldName(name) / HasFieldIndex(index)      // identity
       └─ ConvertUsing(...)                          // transformation
            └─ ValidateUsing(...)                    // validation
                 └─ IsOptional()                     // terminal
  ├─ HasConstantValue(value)                         // terminal alternatives
  └─ HasVariableValue(generator)                     //   available early
```

The concrete `CsvFieldMappingBuilder<TModel, T>` implements every step
interface; the interfaces exist purely to guide the chain. The steps map to the
fields on `CsvFieldMapping` as follows:

| Builder step | Configures |
| --- | --- |
| `HasFieldName(string)` / `HasFieldIndex(int)` | The target CSV field by name or position. |
| `ConvertUsing(...)` | A converter: a `Type` of `TypeConverter`, a generic `<TConverter>`, or a `CsvFieldConvertFromString` / `CsvFieldConvertToString` delegate. |
| `ValidateUsing(CsvFieldValidate<TModel>)` | A delegate that returns `false` for invalid incoming values. |
| `IsOptional()` | Marks the field optional, so a missing field does not error. |
| `HasConstantValue(T)` | A fixed value for the field (terminal). |
| `HasVariableValue(CsvFieldGenerate<T>)` | A delegate that generates the value (terminal). |

A typical imperative mapping therefore reads as a short pipeline per property:

```csharp
public class PersonRecordLineMapper : CsvFileMapperBase<PersonRecordLine>
{
    public PersonRecordLineMapper()
    {
        this.Map()
            .Property(x => x.Id)
            .HasFieldName("person_id")
            .ConvertUsing(typeof(Int32Converter));
        this.Map()
            .Property(x => x.Description)
            .HasFieldName("person_description")
            .IsOptional();
    }
}
```

## How a mapper is chosen

When the reader or writer processes a line it resolves the mapper for the model
type in this order: an imperative mapper registered with
`RegisterMapper<TMapper>()`, then a declarative mapper built from
`[CsvFieldMapping]` attributes, otherwise no mapping (direct field access only).
Resolved mappers are cached per model type, so reflection happens at most once.
The converters, validators, and generators run as each field is read or written;
on a validation failure the value is routed to the configured bad-value handler
or, if none, surfaces as a `CsvFileException`.

## Related

- [Map CSV files](../how-to/map-csv-files.md)
- [The CSV line model](csv-line-model.md)
- [API reference](../reference/index.md)
