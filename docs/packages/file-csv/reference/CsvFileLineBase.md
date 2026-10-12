---
title: CsvFileLineBase Class
parent: File.CSV
grand_parent: Packages
nav_exclude: true
---

# CsvFileLineBase Class

## Definition

Namespace: `DataStandardizer.File.CSV`

The abstract base of the CSV line types. It backs a line with an ordered,
name-keyed field bag and implements the `IOrderedDictionary` surface required by
[ICsvFileLine](ICsvFileLine.md).

**Syntax**

```csharp
public abstract class CsvFileLineBase : ICsvFileLine
```

## Remarks

`CsvFileLineBase` exposes a single public `Add` method for populating a line; the
rest of the `IOrderedDictionary` / `IDictionary` / `ICollection` / `IEnumerable`
surface is implemented **explicitly**, so those members are reached only through an
interface reference (typically `ICsvFileLine`). The field-access helpers
(`GetFieldValue`, `SetFieldValue`, `GetFieldCount`, `GetPropertyKey`) are `protected`
and used by the derived line types. Concrete line types are
[CsvFileRecordLine](CsvFileRecordLine.md) and [CsvFileHeaderLine](CsvFileHeaderLine.md).

## Methods

### Implicit implementation

| Method | Returns | Notes |
| --- | --- | --- |
| `Add(object key, object? value)` | `void` | Appends a field. `value` is `object` (not nullable) on `netstandard1.3` / `netstandard2.0`. On `net10.0`, `key` must be a `string` (see [Field name keys](#field-name-keys)). |

### Explicit implementation

`CsvFileLineBase` implements the ordered-dictionary surface explicitly; each member
is callable only through the corresponding interface reference. *(All targets.)*

| Member | Returns | Notes |
| --- | --- | --- |
| `ICollection.CopyTo(Array array, int index)` | `void` | Copies `DictionaryEntry` items in field order. |
| `ICollection.Count` | `int` | |
| `ICollection.IsSynchronized` | `bool` | |
| `ICollection.SyncRoot` | `object` | |
| `IDictionary.Clear()` | `void` | |
| `IDictionary.Contains(object key)` | `bool` | |
| `IDictionary.GetEnumerator()` | `IDictionaryEnumerator` | |
| `IDictionary.IsFixedSize` | `bool` | |
| `IDictionary.IsReadOnly` | `bool` | |
| `IDictionary.Keys` | `ICollection` | |
| `IDictionary.Remove(object key)` | `void` | |
| `IDictionary.this[object key]` | `object?` | |
| `IDictionary.Values` | `ICollection` | |
| `IEnumerable.GetEnumerator()` | `IEnumerator` | |
| `IOrderedDictionary.GetEnumerator()` | `IDictionaryEnumerator` | |
| `IOrderedDictionary.Insert(int index, object key, object? value)` | `void` | |
| `IOrderedDictionary.RemoveAt(int index)` | `void` | |
| `IOrderedDictionary.this[int index]` | `object?` | |

### Field name keys

Field names are always strings. On `net10.0`, the members that take an `object key`
accept only `string` keys:

- `Add`, `Insert`, and setting `IDictionary.this[object key]` throw
  `ArgumentException` for a key that is not a `string`.
- `Contains` returns `false`, getting `IDictionary.this[object key]` returns
  `null`, and `Remove` does nothing for a key that is not a `string`, because no
  such key can be present.

The other targets accept a key of any type. On every target, a `null` key throws
`ArgumentNullException`.

## Applies to

Targets `netstandard1.3`, `netstandard2.0`, `netstandard2.1`, `net8.0`, and `net10.0`.
The nullable annotations on the `object?` members apply to the `netstandard2.1`,
`net8.0`, and `net10.0` builds.

The `net10.0` build stores the fields in a generic
`OrderedDictionary<string, object?>`; the other builds use the non-generic
`System.Collections.Specialized.OrderedDictionary`. The two behave the same
through the interfaces above, apart from the [field name keys](#field-name-keys)
restriction.

## See also

- [The CSV line model](../concepts/csv-line-model.md)
- [ICsvFileLine](ICsvFileLine.md)
- [CsvFileRecordLine](CsvFileRecordLine.md)
- [CsvFileHeaderLine](CsvFileHeaderLine.md)
- [File.CSV API reference](index.md)
</content>
