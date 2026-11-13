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
value, and a type converter. A model's inherited properties are mapped with
their attributes, so a mapping declared on a base class applies to every class
that derives from it. A base class's properties come before the derived
class's own, so a field mapped without an index takes the next position after
the inherited ones. A property hidden with `new` is mapped by its own
attributes, and an override keeps the attributes of the property it overrides
unless it declares its own. A base class's property whose accessors are all
private isn't inherited, so it isn't mapped on a derived class.

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
| `ConvertUsing(...)` | A converter: a `Type` of `TypeConverter`, a generic `<TConverter>`, or a `CsvFieldConvertFromString` / `CsvFieldConvertToString` delegate. Optional for the types the reader parses itself; see [Converting field values](#converting-field-values). |
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
            .HasFieldName("person_id");
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
Declarative mappers are cached per model type, so reflection happens at most once.
An imperative mapper belongs to the reader or writer it is registered on.
Property-to-field key resolution is cached the same way, so typed property
access on a line and conversions with `ToObject`/`ToCsvLine` reflect at most
once per type.
A reader works out the field names and how each mapping applies to them once, and
reuses that for every line until a mapper is registered on it or unregistered from it.
A change takes effect from the next line read.
The converters, validators, and generators run as each field is read or written;
on a validation failure the value is routed to the configured bad-value handler
or, if none, surfaces as a `CsvFileException`. An exception thrown by a converter
or generator delegate propagates unchanged.

## Where a mapped value is held

A line the reader returns holds each mapped value under its field name, such as
`person_id` for `[CsvFieldMapping("person_id")] int Id`, so
`line["person_id"]` finds it. The line also remembers which field each of its
properties is mapped to, so `line.Id` reads and writes `person_id`. A line made
with `ToCsvLine` does the same.

A mapping by index, such as `HasFieldIndex(2)` or `[CsvFieldMapping(2)]`, reads
the field at that zero-based position of the record, whatever the header line
or the mapper's other mappings call it. The value is held under that field's
name, so with the header `x,y,z` the property mapped to index 2 reads and
writes `z`. Without a header line, the field is named after the mapping: its
field name if it has one, else the property name. A
converter delegate's context gets the name of the field the index picked.

A record with no field at that position has no field for the mapping, even if
another of its fields has the mapping's name.

A mapping reads only the field it's mapped to. A mapping whose field the record
doesn't have, by name or by index, has nothing to read, even if the record has a
field named like the property. If the mapping is optional, it's skipped and the
property isn't set, even if it has a constant or generated value. If it's
required, the reader throws `CsvFileException`, unless it has a constant or
generated value. That value is added to the line under the mapping's name. If
the mapping is by index and the record already has a field with that name, the
reader throws `CsvFileException` rather than overwrite it.

A line built from the model, such as `new PersonLine { Id = 5 }`, has no mapping
yet, so it holds the value under the property key: the name given by
`[CsvField]`, or else the property name. The writer looks a mapped value up by
its field name, and if the line has no such field, by the property key.

## Converting field values

When a reader maps a field to a property, it converts the field's text to the
property type. The first of these that applies to the mapping is used:

1. A `CsvFieldConvertFromString` delegate, set with `ConvertUsing(delegate)`.
2. A `TypeConverter`, set with `ConvertUsing(Type)`, `ConvertUsing<TConverter>()`
   or a `[TypeConverter]` attribute on the property, if it converts from a
   string. One that doesn't, such as a converter meant only for writing, doesn't
   apply, and the next step is used.
3. The reader's built-in parse for the property type, described below.
4. Otherwise the property gets the default value of its type.

A constant value of the property type, or a generated value, is used as it is.
A constant that is text is converted as a field's text is. A constant that is a
number of another type, as an attribute's `ConstantValue` often is, is converted
to a property of a built-in number type (`sbyte` to `ulong`, `float`, `double`,
`decimal`, or a nullable one) if that type holds the number as it is written.
So `5` reads into a `long` or `byte` property, and `0.1` or `0.1f` into a
`decimal`, `double` or `float` property as 0.1. A `float` or `double` is taken to
be written with the fewest significant digits that convert back to it, up to 7
or 15, and a whole number keeps its exact value. Any other constant, such as
`-1` for a `byte` or `1.5` for an `int`, makes the reader throw
`CsvFileException`. The writer writes a constant as it is given.
A field's text is given to a configured converter even when the property is a
`string`, or another type a string can be assigned to, such as `object`, so a
converter can trim or normalise a string column. The converter gets the text as
it was read from the file, even when an earlier mapping of the same field has
converted it. A `TypeConverter` that doesn't convert from a string isn't used
for such a property, and the property gets the field's text as it is, as it
does with no converter configured.

The writer works the same way. A `CsvFieldConvertToString` delegate configured
for a mapping, or a `TypeConverter` that converts to a string, is given the
line's value even when it's a string. A constant or generated string is written
as it is, and so is a string when no such converter is configured.

So a numeric, date, time, `Guid`, `bool` or enum property needs no converter. The
built-in parse handles these types, and their `Nullable<T>` forms, in the same way
on every target:

| Property type | Parsed with |
| --- | --- |
| `int`, `long`, `short`, `sbyte`, `uint`, `ulong`, `ushort`, `byte` | `TryParse` with `NumberStyles.Integer` |
| `decimal` | `TryParse` with `NumberStyles.Number` |
| `double`, `float` | `TryParse` with `NumberStyles.Float \| NumberStyles.AllowThousands`. Infinity and NaN only as the culture's symbols, such as `Infinity`, `-Infinity` and `NaN`, written in the same case. A number too large for the type is a bad value. |
| `DateTime`, `DateTimeOffset` | `TryParse` with `DateTimeStyles.None` |
| `TimeSpan` | `TryParse` |
| `bool`, `char`, `Guid` | `TryParse`, which doesn't depend on the culture |
| An enum | `Enum.TryParse`, case-sensitive: a member name, a comma-separated list of names, or a number, which needn't be a defined value. |

These are the styles the types' own `IParsable<T>` implementations use on .NET 7
and later. On the `net10.0` build, any other type that implements
`ISpanParsable<T>` or `IParsable<T>` is parsed with it. On every build, a type
that has none of these is parsed with the `TypeConverter` that
`TypeDescriptor.GetConverter` returns for it, if that converter converts from a
string, such as a type with a `[TypeConverter]` attribute on its declaration.

The culture is the reader's `CsvFileOptions.Culture`, or the invariant culture
if it isn't set, as it is for a configured `TypeConverter`. The current culture
is never used.

When the reader parses a field itself, because no converter is configured, an
empty field gives the default value of the property type: `null` for a
`Nullable<T>` or reference type, and `0`, `false` or the type's other default for
a value type. Only an empty field does: a field of white space is parsed like any
other value. A configured converter gets an empty field like any other, and
decides what it gives.

### Values that fail to convert

A field the built-in parse rejects, such as `abc` or `1.5` for an `int`, is a bad
value. So is a field a `TypeConverter` throws for, whether it's configured on the
mapping or found by `TypeDescriptor`. If `CsvFileOptions.BadValueHandler` is set,
the reader calls it, with the field's text in `RawFieldValue` and its position in
the line in `FieldIndex`, and carries on with the property left at its default
value. `FieldIndex` is `null` for a constant or generated value, which has no
field. Otherwise it throws a `CsvFileException`
whose `Data` holds the `Property` and `FieldValue`, and the `FieldName` when the
mapping names one. When a `TypeConverter` threw, its exception is the
`InnerException`.

An exception thrown by a `CsvFieldConvertFromString` delegate still propagates
unchanged, without a call to the bad-value handler. So does an exception from
creating a configured `TypeConverter`, which is a fault in the mapping rather than
in the field.

## Related

- [Map CSV files](../how-to/map-csv-files.md)
- [The CSV line model](csv-line-model.md)
- [API reference](../reference/index.md)
